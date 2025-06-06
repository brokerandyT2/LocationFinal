using Location.Core.Application.Common.Interfaces.Persistence;
using Location.Core.Domain.Entities;
using Location.Core.Domain.ValueObjects;
using Location.Core.Infrastructure.Data.Entities;
using Location.Core.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using System.Reflection;

namespace Location.Core.Infrastructure.Data.Repositories
{
    public class WeatherRepository : IWeatherRepository
    {
        private readonly IDatabaseContext _context;
        private readonly ILogger<WeatherRepository> _logger;
        private readonly IInfrastructureExceptionMappingService _exceptionMapper;

        // Compiled mapping delegates for performance
        private static readonly Func<WeatherEntity, Weather> _compiledWeatherEntityToDomain;
        private static readonly Func<Weather, WeatherEntity> _compiledWeatherDomainToEntity;
        private static readonly Func<WeatherForecastEntity, WeatherForecast> _compiledForecastEntityToDomain;
        private static readonly Func<WeatherForecast, WeatherForecastEntity> _compiledForecastDomainToEntity;
        private static readonly Func<HourlyForecastEntity, HourlyForecast> _compiledHourlyEntityToDomain;
        private static readonly Func<HourlyForecast, HourlyForecastEntity> _compiledHourlyDomainToEntity;

        // Cached property setters for reflection performance
        private static readonly Dictionary<string, Action<object, object>> _propertySetters;

        static WeatherRepository()
        {
            _compiledWeatherEntityToDomain = CompileWeatherEntityToDomainMapper();
            _compiledWeatherDomainToEntity = CompileWeatherDomainToEntityMapper();
            _compiledForecastEntityToDomain = CompileForecastEntityToDomainMapper();
            _compiledForecastDomainToEntity = CompileForecastDomainToEntityMapper();
            _compiledHourlyEntityToDomain = CompileHourlyEntityToDomainMapper();
            _compiledHourlyDomainToEntity = CompileHourlyDomainToEntityMapper();
            _propertySetters = CreatePropertySetters();
        }

        public WeatherRepository(IDatabaseContext context, ILogger<WeatherRepository> logger, IInfrastructureExceptionMappingService exceptionMapper)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _exceptionMapper = exceptionMapper ?? throw new ArgumentNullException(nameof(exceptionMapper));
        }

        #region Core Operations (Optimized)

        public async Task<Weather?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    _logger.LogInformation("Retrieving weather with ID {WeatherId}", id);

                    // Get weather entity
                    var weatherEntity = await _context.GetAsync<WeatherEntity>(id);
                    if (weatherEntity == null)
                    {
                        _logger.LogInformation("Weather with ID {WeatherId} not found", id);
                        return null;
                    }

                    // Get related entities concurrently for better performance
                    var forecastTask = _context.QueryAsync<WeatherForecastEntity>(
                        "SELECT * FROM WeatherForecastEntity WHERE WeatherId = ? ORDER BY Date", id);
                    var hourlyTask = _context.QueryAsync<HourlyForecastEntity>(
                        "SELECT * FROM HourlyForecastEntity WHERE WeatherId = ? ORDER BY DateTime", id);

                    await Task.WhenAll(forecastTask, hourlyTask);

                    var forecastEntities = await forecastTask;
                    var hourlyForecastEntities = await hourlyTask;

                    var weather = MapToDomainWithRelations(weatherEntity, forecastEntities, hourlyForecastEntities);

                    _logger.LogInformation("Successfully retrieved weather with ID {WeatherId}", id);
                    return weather;
                },
                _exceptionMapper,
                "GetById",
                "weather",
                _logger);
        }

        public async Task<Weather?> GetByLocationIdAsync(int locationId, CancellationToken cancellationToken = default)
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    // Get most recent weather for location
                    var weatherEntities = await _context.QueryAsync<WeatherEntity>(
                        "SELECT * FROM WeatherEntity WHERE LocationId = ? ORDER BY LastUpdate DESC LIMIT 1", locationId);

                    if (!weatherEntities.Any())
                    {
                        return null;
                    }

                    var weatherEntity = weatherEntities.First();

                    // Get related entities concurrently
                    var forecastTask = _context.QueryAsync<WeatherForecastEntity>(
                        "SELECT * FROM WeatherForecastEntity WHERE WeatherId = ? ORDER BY Date", weatherEntity.Id);
                    var hourlyTask = _context.QueryAsync<HourlyForecastEntity>(
                        "SELECT * FROM HourlyForecastEntity WHERE WeatherId = ? ORDER BY DateTime", weatherEntity.Id);

                    await Task.WhenAll(forecastTask, hourlyTask);

                    var forecastEntities = await forecastTask;
                    var hourlyForecastEntities = await hourlyTask;

                    return MapToDomainWithRelations(weatherEntity, forecastEntities, hourlyForecastEntities);
                },
                _exceptionMapper,
                "GetByLocationId",
                "weather",
                _logger);
        }

        public async Task<Weather> AddAsync(Weather weather, CancellationToken cancellationToken = default)
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    return await _context.ExecuteInTransactionAsync(async () =>
                    {
                        // Insert main weather entity
                        var weatherEntity = _compiledWeatherDomainToEntity(weather);
                        await _context.InsertAsync(weatherEntity);
                        SetOptimizedProperty(weather, "Id", weatherEntity.Id);

                        // Bulk insert forecasts
                        if (weather.Forecasts.Any())
                        {
                            var forecastEntities = weather.Forecasts.Select(f =>
                            {
                                var entity = _compiledForecastDomainToEntity(f);
                                entity.WeatherId = weatherEntity.Id;
                                return entity;
                            }).ToList();

                            await _context.BulkInsertAsync(forecastEntities, 50);

                            // Update forecast IDs
                            for (int i = 0; i < weather.Forecasts.Count; i++)
                            {
                                SetOptimizedProperty(weather.Forecasts.ElementAt(i), "Id", forecastEntities[i].Id);
                            }
                        }

                        // Bulk insert hourly forecasts
                        if (weather.HourlyForecasts.Any())
                        {
                            var hourlyEntities = weather.HourlyForecasts.Select(h =>
                            {
                                var entity = _compiledHourlyDomainToEntity(h);
                                entity.WeatherId = weatherEntity.Id;
                                return entity;
                            }).ToList();

                            await _context.BulkInsertAsync(hourlyEntities, 100);

                            // Update hourly forecast IDs
                            for (int i = 0; i < weather.HourlyForecasts.Count; i++)
                            {
                                SetOptimizedProperty(weather.HourlyForecasts.ElementAt(i), "Id", hourlyEntities[i].Id);
                            }
                        }

                        _logger.LogInformation("Created weather with ID {WeatherId} for location {LocationId}",
                            weatherEntity.Id, weather.LocationId);

                        return weather;
                    });
                },
                _exceptionMapper,
                "Add",
                "weather",
                _logger);
        }

        public async Task UpdateAsync(Weather weather, CancellationToken cancellationToken = default)
        {
            await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    await _context.ExecuteInTransactionAsync(async () =>
                    {
                        // Update main weather entity
                        var weatherEntity = _compiledWeatherDomainToEntity(weather);
                        await _context.UpdateAsync(weatherEntity);

                        // Delete existing related entities efficiently
                        await _context.ExecuteAsync("DELETE FROM WeatherForecastEntity WHERE WeatherId = ?", weather.Id);
                        await _context.ExecuteAsync("DELETE FROM HourlyForecastEntity WHERE WeatherId = ?", weather.Id);

                        // Bulk insert new forecasts
                        if (weather.Forecasts.Any())
                        {
                            var forecastEntities = weather.Forecasts.Select(f =>
                            {
                                var entity = _compiledForecastDomainToEntity(f);
                                entity.WeatherId = weather.Id;
                                entity.Id = 0; // Reset ID for insert
                                return entity;
                            }).ToList();

                            await _context.BulkInsertAsync(forecastEntities, 50);

                            // Update forecast IDs
                            for (int i = 0; i < weather.Forecasts.Count; i++)
                            {
                                SetOptimizedProperty(weather.Forecasts.ElementAt(i), "Id", forecastEntities[i].Id);
                            }
                        }

                        // Bulk insert new hourly forecasts
                        if (weather.HourlyForecasts.Any())
                        {
                            var hourlyEntities = weather.HourlyForecasts.Select(h =>
                            {
                                var entity = _compiledHourlyDomainToEntity(h);
                                entity.WeatherId = weather.Id;
                                entity.Id = 0; // Reset ID for insert
                                return entity;
                            }).ToList();

                            await _context.BulkInsertAsync(hourlyEntities, 100);

                            // Update hourly forecast IDs
                            for (int i = 0; i < weather.HourlyForecasts.Count; i++)
                            {
                                SetOptimizedProperty(weather.HourlyForecasts.ElementAt(i), "Id", hourlyEntities[i].Id);
                            }
                        }

                        _logger.LogInformation("Updated weather with ID {WeatherId}", weather.Id);
                    });
                },
                _exceptionMapper,
                "Update",
                "weather",
                _logger);
        }

        public async Task DeleteAsync(Weather weather, CancellationToken cancellationToken = default)
        {
            await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    await _context.ExecuteInTransactionAsync(async () =>
                    {
                        // Delete related entities first (foreign key constraints)
                        await _context.ExecuteAsync("DELETE FROM HourlyForecastEntity WHERE WeatherId = ?", weather.Id);
                        await _context.ExecuteAsync("DELETE FROM WeatherForecastEntity WHERE WeatherId = ?", weather.Id);

                        // Delete main weather entity
                        var weatherEntity = _compiledWeatherDomainToEntity(weather);
                        await _context.DeleteAsync(weatherEntity);

                        _logger.LogInformation("Deleted weather with ID {WeatherId}", weather.Id);
                    });
                },
                _exceptionMapper,
                "Delete",
                "weather",
                _logger);
        }

        #endregion

        #region Query Operations (Optimized)

        public async Task<IEnumerable<Weather>> GetRecentAsync(int count = 10, CancellationToken cancellationToken = default)
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    // Use optimized query to get recent weather with minimal data transfer
                    var weatherEntities = await _context.QueryAsync<WeatherEntity>(
                        "SELECT * FROM WeatherEntity ORDER BY LastUpdate DESC LIMIT ?", count);

                    var weatherList = new List<Weather>();

                    // Process each weather entity
                    foreach (var weatherEntity in weatherEntities)
                    {
                        // Get related entities concurrently for each weather record
                        var forecastTask = _context.QueryAsync<WeatherForecastEntity>(
                            "SELECT * FROM WeatherForecastEntity WHERE WeatherId = ? ORDER BY Date", weatherEntity.Id);
                        var hourlyTask = _context.QueryAsync<HourlyForecastEntity>(
                            "SELECT * FROM HourlyForecastEntity WHERE WeatherId = ? ORDER BY DateTime LIMIT 24", weatherEntity.Id);

                        await Task.WhenAll(forecastTask, hourlyTask);

                        var forecastEntities = await forecastTask;
                        var hourlyForecastEntities = await hourlyTask;

                        var weather = MapToDomainWithRelations(weatherEntity, forecastEntities, hourlyForecastEntities);
                        weatherList.Add(weather);
                    }

                    return weatherList;
                },
                _exceptionMapper,
                "GetRecent",
                "weather",
                _logger);
        }

        public async Task<IEnumerable<Weather>> GetExpiredAsync(TimeSpan maxAge, CancellationToken cancellationToken = default)
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    var cutoffDate = DateTime.UtcNow - maxAge;
                    var cutoffTicks = cutoffDate.Ticks;

                    // Use parameterized query for better performance
                    var weatherEntities = await _context.QueryAsync<WeatherEntity>(
                        "SELECT * FROM WeatherEntity WHERE LastUpdate < ?", cutoffTicks);

                    var weatherList = new List<Weather>();

                    // Process in batches to avoid memory issues with large datasets
                    const int batchSize = 10;
                    var batches = weatherEntities.Chunk(batchSize);

                    foreach (var batch in batches)
                    {
                        var batchTasks = batch.Select(async weatherEntity =>
                        {
                            // Get related entities concurrently
                            var forecastTask = _context.QueryAsync<WeatherForecastEntity>(
                                "SELECT * FROM WeatherForecastEntity WHERE WeatherId = ? ORDER BY Date", weatherEntity.Id);
                            var hourlyTask = _context.QueryAsync<HourlyForecastEntity>(
                                "SELECT * FROM HourlyForecastEntity WHERE WeatherId = ? ORDER BY DateTime", weatherEntity.Id);

                            await Task.WhenAll(forecastTask, hourlyTask);

                            var forecastEntities = await forecastTask;
                            var hourlyForecastEntities = await hourlyTask;

                            return MapToDomainWithRelations(weatherEntity, forecastEntities, hourlyForecastEntities);
                        });

                        var batchResults = await Task.WhenAll(batchTasks);
                        weatherList.AddRange(batchResults);
                    }

                    return weatherList;
                },
                _exceptionMapper,
                "GetExpired",
                "weather",
                _logger);
        }

        #endregion

        #region Bulk Operations

        public async Task<IEnumerable<Weather>> CreateBulkAsync(IEnumerable<Weather> weatherRecords, CancellationToken cancellationToken = default)
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    var weatherList = weatherRecords.ToList();
                    if (!weatherList.Any()) return weatherList;

                    return await _context.ExecuteInTransactionAsync(async () =>
                    {
                        // Bulk insert weather entities
                        var weatherEntities = weatherList.Select(_compiledWeatherDomainToEntity).ToList();
                        await _context.BulkInsertAsync(weatherEntities, 50);

                        // Update weather IDs
                        for (int i = 0; i < weatherList.Count; i++)
                        {
                            SetOptimizedProperty(weatherList[i], "Id", weatherEntities[i].Id);
                        }

                        // Bulk insert all forecasts
                        var allForecasts = new List<WeatherForecastEntity>();
                        var allHourlyForecasts = new List<HourlyForecastEntity>();

                        for (int i = 0; i < weatherList.Count; i++)
                        {
                            var weather = weatherList[i];
                            var weatherId = weatherEntities[i].Id;

                            // Prepare forecast entities
                            var forecastEntities = weather.Forecasts.Select(f =>
                            {
                                var entity = _compiledForecastDomainToEntity(f);
                                entity.WeatherId = weatherId;
                                return entity;
                            });
                            allForecasts.AddRange(forecastEntities);

                            // Prepare hourly forecast entities
                            var hourlyEntities = weather.HourlyForecasts.Select(h =>
                            {
                                var entity = _compiledHourlyDomainToEntity(h);
                                entity.WeatherId = weatherId;
                                return entity;
                            });
                            allHourlyForecasts.AddRange(hourlyEntities);
                        }

                        // Bulk insert all forecasts and hourly forecasts
                        if (allForecasts.Any())
                        {
                            await _context.BulkInsertAsync(allForecasts, 100);
                        }

                        if (allHourlyForecasts.Any())
                        {
                            await _context.BulkInsertAsync(allHourlyForecasts, 200);
                        }

                        _logger.LogInformation("Bulk created {Count} weather records with forecasts", weatherList.Count);
                        return weatherList;
                    });
                },
                _exceptionMapper,
                "CreateBulk",
                "weather",
                _logger);
        }

        public async Task<int> DeleteExpiredAsync(TimeSpan maxAge, CancellationToken cancellationToken = default)
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    var cutoffDate = DateTime.UtcNow - maxAge;
                    var cutoffTicks = cutoffDate.Ticks;

                    return await _context.ExecuteInTransactionAsync(async () =>
                    {
                        // Get weather IDs to delete
                        var weatherIds = await _context.QueryAsync<int>(
                            "SELECT Id FROM WeatherEntity WHERE LastUpdate < ?", cutoffTicks);

                        if (!weatherIds.Any()) return 0;

                        // Delete related entities first (in batches for large datasets)
                        const int batchSize = 100;
                        var idBatches = weatherIds.Chunk(batchSize);

                        foreach (var batch in idBatches)
                        {
                            var placeholders = string.Join(",", batch.Select(_ => "?"));
                            var batchArray = batch.Cast<object>().ToArray();

                            await _context.ExecuteAsync(
                                $"DELETE FROM HourlyForecastEntity WHERE WeatherId IN ({placeholders})", batchArray);
                            await _context.ExecuteAsync(
                                $"DELETE FROM WeatherForecastEntity WHERE WeatherId IN ({placeholders})", batchArray);
                        }

                        // Delete weather entities
                        var deletedCount = await _context.ExecuteAsync(
                            "DELETE FROM WeatherEntity WHERE LastUpdate < ?", cutoffTicks);

                        _logger.LogInformation("Deleted {Count} expired weather records", deletedCount);
                        return deletedCount;
                    });
                },
                _exceptionMapper,
                "DeleteExpired",
                "weather",
                _logger);
        }

        #endregion

        #region Compiled Mapping Methods

        private static Func<WeatherEntity, Weather> CompileWeatherEntityToDomainMapper()
        {
            var entityParam = Expression.Parameter(typeof(WeatherEntity), "entity");

            // Create Coordinate
            var coordinateConstructor = typeof(Coordinate).GetConstructor(new[] { typeof(double), typeof(double) });
            var coordinateNew = Expression.New(coordinateConstructor!,
                Expression.Property(entityParam, nameof(WeatherEntity.Latitude)),
                Expression.Property(entityParam, nameof(WeatherEntity.Longitude)));

            // Create Weather using constructor
            var weatherConstructor = typeof(Weather).GetConstructor(
                new[] { typeof(int), typeof(Coordinate), typeof(string), typeof(int) });

            var weatherNew = Expression.New(weatherConstructor!,
                Expression.Property(entityParam, nameof(WeatherEntity.LocationId)),
                coordinateNew,
                Expression.Property(entityParam, nameof(WeatherEntity.Timezone)),
                Expression.Property(entityParam, nameof(WeatherEntity.TimezoneOffset)));

            // Create block to set additional properties
            var weatherVar = Expression.Variable(typeof(Weather), "weather");
            var initExpressions = new List<Expression>
           {
               Expression.Assign(weatherVar, weatherNew),
               weatherVar
           };

            var body = Expression.Block(new[] { weatherVar }, initExpressions);
            return Expression.Lambda<Func<WeatherEntity, Weather>>(body, entityParam).Compile();
        }

        private static Func<Weather, WeatherEntity> CompileWeatherDomainToEntityMapper()
        {
            var weatherParam = Expression.Parameter(typeof(Weather), "weather");

            var entityNew = Expression.MemberInit(
                Expression.New(typeof(WeatherEntity)),
                Expression.Bind(typeof(WeatherEntity).GetProperty(nameof(WeatherEntity.Id))!,
                    Expression.Property(weatherParam, "Id")),
                Expression.Bind(typeof(WeatherEntity).GetProperty(nameof(WeatherEntity.LocationId))!,
                    Expression.Property(weatherParam, "LocationId")),
                Expression.Bind(typeof(WeatherEntity).GetProperty(nameof(WeatherEntity.Latitude))!,
                    Expression.Property(Expression.Property(weatherParam, "Coordinate"), "Latitude")),
                Expression.Bind(typeof(WeatherEntity).GetProperty(nameof(WeatherEntity.Longitude))!,
                    Expression.Property(Expression.Property(weatherParam, "Coordinate"), "Longitude")),
                Expression.Bind(typeof(WeatherEntity).GetProperty(nameof(WeatherEntity.Timezone))!,
                    Expression.Property(weatherParam, "Timezone")),
                Expression.Bind(typeof(WeatherEntity).GetProperty(nameof(WeatherEntity.TimezoneOffset))!,
                    Expression.Property(weatherParam, "TimezoneOffset")),
                Expression.Bind(typeof(WeatherEntity).GetProperty(nameof(WeatherEntity.LastUpdate))!,
                    Expression.Property(weatherParam, "LastUpdate"))
            );

            return Expression.Lambda<Func<Weather, WeatherEntity>>(entityNew, weatherParam).Compile();
        }

        private static Func<WeatherForecastEntity, WeatherForecast> CompileForecastEntityToDomainMapper()
        {
            var entityParam = Expression.Parameter(typeof(WeatherForecastEntity), "entity");

            // Create WindInfo
            var windConstructor = typeof(WindInfo).GetConstructor(new[] { typeof(double), typeof(double), typeof(double?) });
            var windNew = Expression.New(windConstructor!,
                Expression.Property(entityParam, nameof(WeatherForecastEntity.WindSpeed)),
                Expression.Property(entityParam, nameof(WeatherForecastEntity.WindDirection)),
                Expression.Property(entityParam, nameof(WeatherForecastEntity.WindGust)));

            // Create WeatherForecast using constructor
            var forecastConstructor = typeof(WeatherForecast).GetConstructor(new[]
            {
               typeof(int), typeof(DateTime), typeof(DateTime), typeof(DateTime),
               typeof(double), typeof(double), typeof(double),
               typeof(string), typeof(string), typeof(WindInfo),
               typeof(int), typeof(int), typeof(int), typeof(double)
           });

            var forecastNew = Expression.New(forecastConstructor!,
                Expression.Property(entityParam, nameof(WeatherForecastEntity.WeatherId)),
                Expression.Property(entityParam, nameof(WeatherForecastEntity.Date)),
                Expression.Property(entityParam, nameof(WeatherForecastEntity.Sunrise)),
                Expression.Property(entityParam, nameof(WeatherForecastEntity.Sunset)),
                Expression.Property(entityParam, nameof(WeatherForecastEntity.Temperature)),
                Expression.Property(entityParam, nameof(WeatherForecastEntity.MinTemperature)),
                Expression.Property(entityParam, nameof(WeatherForecastEntity.MaxTemperature)),
                Expression.Property(entityParam, nameof(WeatherForecastEntity.Description)),
                Expression.Property(entityParam, nameof(WeatherForecastEntity.Icon)),
                windNew,
                Expression.Property(entityParam, nameof(WeatherForecastEntity.Humidity)),
                Expression.Property(entityParam, nameof(WeatherForecastEntity.Pressure)),
                Expression.Property(entityParam, nameof(WeatherForecastEntity.Clouds)),
                Expression.Property(entityParam, nameof(WeatherForecastEntity.UvIndex)));

            return Expression.Lambda<Func<WeatherForecastEntity, WeatherForecast>>(forecastNew, entityParam).Compile();
        }

        private static Func<WeatherForecast, WeatherForecastEntity> CompileForecastDomainToEntityMapper()
        {
            var forecastParam = Expression.Parameter(typeof(WeatherForecast), "forecast");

            var entityNew = Expression.MemberInit(
                Expression.New(typeof(WeatherForecastEntity)),
                Expression.Bind(typeof(WeatherForecastEntity).GetProperty(nameof(WeatherForecastEntity.Id))!,
                    Expression.Property(forecastParam, "Id")),
                Expression.Bind(typeof(WeatherForecastEntity).GetProperty(nameof(WeatherForecastEntity.WeatherId))!,
                    Expression.Property(forecastParam, "WeatherId")),
                Expression.Bind(typeof(WeatherForecastEntity).GetProperty(nameof(WeatherForecastEntity.Date))!,
                    Expression.Property(forecastParam, "Date")),
                Expression.Bind(typeof(WeatherForecastEntity).GetProperty(nameof(WeatherForecastEntity.Sunrise))!,
                    Expression.Property(forecastParam, "Sunrise")),
                Expression.Bind(typeof(WeatherForecastEntity).GetProperty(nameof(WeatherForecastEntity.Sunset))!,
                    Expression.Property(forecastParam, "Sunset")),
                Expression.Bind(typeof(WeatherForecastEntity).GetProperty(nameof(WeatherForecastEntity.Temperature))!,
                    Expression.Property(forecastParam, "Temperature")),
                Expression.Bind(typeof(WeatherForecastEntity).GetProperty(nameof(WeatherForecastEntity.MinTemperature))!,
                    Expression.Property(forecastParam, "MinTemperature")),
                Expression.Bind(typeof(WeatherForecastEntity).GetProperty(nameof(WeatherForecastEntity.MaxTemperature))!,
                    Expression.Property(forecastParam, "MaxTemperature")),
                Expression.Bind(typeof(WeatherForecastEntity).GetProperty(nameof(WeatherForecastEntity.Description))!,
                    Expression.Property(forecastParam, "Description")),
                Expression.Bind(typeof(WeatherForecastEntity).GetProperty(nameof(WeatherForecastEntity.Icon))!,
                    Expression.Property(forecastParam, "Icon")),
                Expression.Bind(typeof(WeatherForecastEntity).GetProperty(nameof(WeatherForecastEntity.WindSpeed))!,
                    Expression.Property(Expression.Property(forecastParam, "Wind"), "Speed")),
                Expression.Bind(typeof(WeatherForecastEntity).GetProperty(nameof(WeatherForecastEntity.WindDirection))!,
                    Expression.Property(Expression.Property(forecastParam, "Wind"), "Direction")),
                Expression.Bind(typeof(WeatherForecastEntity).GetProperty(nameof(WeatherForecastEntity.WindGust))!,
                    Expression.Property(Expression.Property(forecastParam, "Wind"), "Gust")),
                Expression.Bind(typeof(WeatherForecastEntity).GetProperty(nameof(WeatherForecastEntity.Humidity))!,
                    Expression.Property(forecastParam, "Humidity")),
                Expression.Bind(typeof(WeatherForecastEntity).GetProperty(nameof(WeatherForecastEntity.Pressure))!,
                    Expression.Property(forecastParam, "Pressure")),
                Expression.Bind(typeof(WeatherForecastEntity).GetProperty(nameof(WeatherForecastEntity.Clouds))!,
                    Expression.Property(forecastParam, "Clouds")),
                Expression.Bind(typeof(WeatherForecastEntity).GetProperty(nameof(WeatherForecastEntity.UvIndex))!,
                    Expression.Property(forecastParam, "UvIndex")),
                Expression.Bind(typeof(WeatherForecastEntity).GetProperty(nameof(WeatherForecastEntity.Precipitation))!,
                    Expression.Property(forecastParam, "Precipitation")),
                Expression.Bind(typeof(WeatherForecastEntity).GetProperty(nameof(WeatherForecastEntity.MoonRise))!,
                    Expression.Property(forecastParam, "MoonRise")),
                Expression.Bind(typeof(WeatherForecastEntity).GetProperty(nameof(WeatherForecastEntity.MoonSet))!,
                    Expression.Property(forecastParam, "MoonSet")),
                Expression.Bind(typeof(WeatherForecastEntity).GetProperty(nameof(WeatherForecastEntity.MoonPhase))!,
                    Expression.Property(forecastParam, "MoonPhase"))
            );

            return Expression.Lambda<Func<WeatherForecast, WeatherForecastEntity>>(entityNew, forecastParam).Compile();
        }

        private static Func<HourlyForecastEntity, HourlyForecast> CompileHourlyEntityToDomainMapper()
        {
            var entityParam = Expression.Parameter(typeof(HourlyForecastEntity), "entity");

            // Create WindInfo
            var windConstructor = typeof(WindInfo).GetConstructor(new[] { typeof(double), typeof(double), typeof(double?) });
            var windNew = Expression.New(windConstructor!,
                Expression.Property(entityParam, nameof(HourlyForecastEntity.WindSpeed)),
                Expression.Property(entityParam, nameof(HourlyForecastEntity.WindDirection)),
                Expression.Property(entityParam, nameof(HourlyForecastEntity.WindGust)));

            // Create HourlyForecast using constructor
            var hourlyConstructor = typeof(HourlyForecast).GetConstructor(new[]
            {
               typeof(int), typeof(DateTime), typeof(double), typeof(double),
               typeof(string), typeof(string), typeof(WindInfo),
               typeof(int), typeof(int), typeof(int), typeof(double),
               typeof(double), typeof(int), typeof(double)
           });

            var hourlyNew = Expression.New(hourlyConstructor!,
                Expression.Property(entityParam, nameof(HourlyForecastEntity.WeatherId)),
                Expression.Property(entityParam, nameof(HourlyForecastEntity.DateTime)),
                Expression.Property(entityParam, nameof(HourlyForecastEntity.Temperature)),
                Expression.Property(entityParam, nameof(HourlyForecastEntity.FeelsLike)),
                Expression.Property(entityParam, nameof(HourlyForecastEntity.Description)),
                Expression.Property(entityParam, nameof(HourlyForecastEntity.Icon)),
                windNew,
                Expression.Property(entityParam, nameof(HourlyForecastEntity.Humidity)),
                Expression.Property(entityParam, nameof(HourlyForecastEntity.Pressure)),
                Expression.Property(entityParam, nameof(HourlyForecastEntity.Clouds)),
                Expression.Property(entityParam, nameof(HourlyForecastEntity.UvIndex)),
                Expression.Property(entityParam, nameof(HourlyForecastEntity.ProbabilityOfPrecipitation)),
                Expression.Property(entityParam, nameof(HourlyForecastEntity.Visibility)),
                Expression.Property(entityParam, nameof(HourlyForecastEntity.DewPoint)));

            return Expression.Lambda<Func<HourlyForecastEntity, HourlyForecast>>(hourlyNew, entityParam).Compile();
        }

        private static Func<HourlyForecast, HourlyForecastEntity> CompileHourlyDomainToEntityMapper()
        {
            var hourlyParam = Expression.Parameter(typeof(HourlyForecast), "hourly");

            var entityNew = Expression.MemberInit(
                Expression.New(typeof(HourlyForecastEntity)),
                Expression.Bind(typeof(HourlyForecastEntity).GetProperty(nameof(HourlyForecastEntity.Id))!,
                    Expression.Property(hourlyParam, "Id")),
                Expression.Bind(typeof(HourlyForecastEntity).GetProperty(nameof(HourlyForecastEntity.WeatherId))!,
                    Expression.Property(hourlyParam, "WeatherId")),
                Expression.Bind(typeof(HourlyForecastEntity).GetProperty(nameof(HourlyForecastEntity.DateTime))!,
                    Expression.Property(hourlyParam, "DateTime")),
                Expression.Bind(typeof(HourlyForecastEntity).GetProperty(nameof(HourlyForecastEntity.Temperature))!,
                    Expression.Property(hourlyParam, "Temperature")),
                Expression.Bind(typeof(HourlyForecastEntity).GetProperty(nameof(HourlyForecastEntity.FeelsLike))!,
                    Expression.Property(hourlyParam, "FeelsLike")),
                Expression.Bind(typeof(HourlyForecastEntity).GetProperty(nameof(HourlyForecastEntity.Description))!,
                    Expression.Property(hourlyParam, "Description")),
                Expression.Bind(typeof(HourlyForecastEntity).GetProperty(nameof(HourlyForecastEntity.Icon))!,
                    Expression.Property(hourlyParam, "Icon")),
                Expression.Bind(typeof(HourlyForecastEntity).GetProperty(nameof(HourlyForecastEntity.WindSpeed))!,
                    Expression.Property(Expression.Property(hourlyParam, "Wind"), "Speed")),
                Expression.Bind(typeof(HourlyForecastEntity).GetProperty(nameof(HourlyForecastEntity.WindDirection))!,
                    Expression.Property(Expression.Property(hourlyParam, "Wind"), "Direction")),
                Expression.Bind(typeof(HourlyForecastEntity).GetProperty(nameof(HourlyForecastEntity.WindGust))!,
                    Expression.Property(Expression.Property(hourlyParam, "Wind"), "Gust")),
                Expression.Bind(typeof(HourlyForecastEntity).GetProperty(nameof(HourlyForecastEntity.Humidity))!,
                    Expression.Property(hourlyParam, "Humidity")),
                Expression.Bind(typeof(HourlyForecastEntity).GetProperty(nameof(HourlyForecastEntity.Pressure))!,
                    Expression.Property(hourlyParam, "Pressure")),
                Expression.Bind(typeof(HourlyForecastEntity).GetProperty(nameof(HourlyForecastEntity.Clouds))!,
                    Expression.Property(hourlyParam, "Clouds")),
                Expression.Bind(typeof(HourlyForecastEntity).GetProperty(nameof(HourlyForecastEntity.UvIndex))!,
                    Expression.Property(hourlyParam, "UvIndex")),
                Expression.Bind(typeof(HourlyForecastEntity).GetProperty(nameof(HourlyForecastEntity.ProbabilityOfPrecipitation))!,
                    Expression.Property(hourlyParam, "ProbabilityOfPrecipitation")),
                Expression.Bind(typeof(HourlyForecastEntity).GetProperty(nameof(HourlyForecastEntity.Visibility))!,
                    Expression.Property(hourlyParam, "Visibility")),
                Expression.Bind(typeof(HourlyForecastEntity).GetProperty(nameof(HourlyForecastEntity.DewPoint))!,
                    Expression.Property(hourlyParam, "DewPoint"))
            );

            return Expression.Lambda<Func<HourlyForecast, HourlyForecastEntity>>(entityNew, hourlyParam).Compile();
        }

        private static Dictionary<string, Action<object, object>> CreatePropertySetters()
        {
            var setters = new Dictionary<string, Action<object, object>>();

            // Weather property setters
            var weatherProps = typeof(Weather).GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var prop in weatherProps.Where(p => p.CanWrite))
            {
                var objParam = Expression.Parameter(typeof(object), "obj");
                var valueParam = Expression.Parameter(typeof(object), "value");

                var castObj = Expression.Convert(objParam, typeof(Weather));
                var castValue = Expression.Convert(valueParam, prop.PropertyType);
                var setProp = Expression.Call(castObj, prop.GetSetMethod(true)!, castValue);

                var lambda = Expression.Lambda<Action<object, object>>(setProp, objParam, valueParam);
                setters[$"Weather.{prop.Name}"] = lambda.Compile();
            }

            // WeatherForecast property setters
            var forecastProps = typeof(WeatherForecast).GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var prop in forecastProps.Where(p => p.CanWrite))
            {
                var objParam = Expression.Parameter(typeof(object), "obj");
                var valueParam = Expression.Parameter(typeof(object), "value");

                var castObj = Expression.Convert(objParam, typeof(WeatherForecast));
                var castValue = Expression.Convert(valueParam, prop.PropertyType);
                var setProp = Expression.Call(castObj, prop.GetSetMethod(true)!, castValue);

                var lambda = Expression.Lambda<Action<object, object>>(setProp, objParam, valueParam);
                setters[$"WeatherForecast.{prop.Name}"] = lambda.Compile();
            }

            // HourlyForecast property setters
            var hourlyProps = typeof(HourlyForecast).GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var prop in hourlyProps.Where(p => p.CanWrite))
            {
                var objParam = Expression.Parameter(typeof(object), "obj");
                var valueParam = Expression.Parameter(typeof(object), "value");

                var castObj = Expression.Convert(objParam, typeof(HourlyForecast));
                var castValue = Expression.Convert(valueParam, prop.PropertyType);
                var setProp = Expression.Call(castObj, prop.GetSetMethod(true)!, castValue);

                var lambda = Expression.Lambda<Action<object, object>>(setProp, objParam, valueParam);
                setters[$"HourlyForecast.{prop.Name}"] = lambda.Compile();
            }

            return setters;
        }

        #endregion

        #region Helper Methods

        private Weather MapToDomainWithRelations(WeatherEntity weatherEntity, IEnumerable<WeatherForecastEntity> forecastEntities, IEnumerable<HourlyForecastEntity> hourlyEntities)
        {
            // Map main weather entity
            var weather = _compiledWeatherEntityToDomain(weatherEntity);
            SetOptimizedProperty(weather, "Id", weatherEntity.Id);
            SetOptimizedProperty(weather, "_lastUpdate", weatherEntity.LastUpdate);

            // Map forecasts
            var forecasts = forecastEntities.Select(f =>
            {
                var forecast = _compiledForecastEntityToDomain(f);
                SetOptimizedProperty(forecast, "Id", f.Id);

                // Set optional properties if they exist
                if (f.Precipitation.HasValue)
                {
                    forecast.SetPrecipitation(f.Precipitation.Value);
                }
                forecast.SetMoonData(f.MoonRise, f.MoonSet, f.MoonPhase);

                return forecast;
            }).ToList();

            // Map hourly forecasts
            var hourlyForecasts = hourlyEntities.Select(h =>
            {
                var hourly = _compiledHourlyEntityToDomain(h);
                SetOptimizedProperty(hourly, "Id", h.Id);
                return hourly;
            }).ToList();

            // Update weather with forecasts
            weather.UpdateForecasts(forecasts);
            weather.UpdateHourlyForecasts(hourlyForecasts);

            return weather;
        }

        private static void SetOptimizedProperty(object obj, string propertyName, object value)
        {
            var key = $"{obj.GetType().Name}.{propertyName}";

            if (_propertySetters.TryGetValue(key, out var setter))
            {
                setter(obj, value);
            }
            else
            {
                // Fallback to reflection for properties not in cache
                var property = obj.GetType().GetProperty(propertyName);
                if (property != null && property.CanWrite)
                {
                    property.SetValue(obj, value);
                }
                else
                {
                    // Try private field access as last resort
                    var field = obj.GetType().GetField($"_{propertyName.ToLowerInvariant()}",
                        BindingFlags.NonPublic | BindingFlags.Instance);
                    field?.SetValue(obj, value);
                }
            }
        }

        #endregion

        #region Legacy Mapping Methods (Kept for Backward Compatibility)


        #endregion
    }
}