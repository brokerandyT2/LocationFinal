using Location.Core.Application.Common.Interfaces.Persistence;
using Location.Core.Domain.Entities;
using Location.Core.Domain.ValueObjects;
using Location.Core.Infrastructure.Data.Entities;
using Location.Core.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Core.Infrastructure.Data.Repositories
{
    public class WeatherRepository : IWeatherRepository
    {
        private readonly IDatabaseContext _context;
        private readonly ILogger<WeatherRepository> _logger;
        private readonly IInfrastructureExceptionMappingService _exceptionMapper;

        public WeatherRepository(IDatabaseContext context, ILogger<WeatherRepository> logger, IInfrastructureExceptionMappingService exceptionMapper)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _exceptionMapper = exceptionMapper ?? throw new ArgumentNullException(nameof(exceptionMapper));
        }

        public async Task<Weather?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    _logger.LogInformation("Retrieving weather with ID {WeatherId}", id);

                    var weatherEntity = await _context.GetAsync<WeatherEntity>(id);

                    if (weatherEntity == null)
                    {
                        _logger.LogInformation("Weather with ID {WeatherId} not found", id);
                        return null;
                    }

                    var forecastEntities = await _context.Table<WeatherForecastEntity>()
                        .Where(f => f.WeatherId == id)
                        .OrderBy(f => f.Date)
                        .ToListAsync();

                    var weather = MapToDomain(weatherEntity, forecastEntities);

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
                    var weatherEntities = await _context.Table<WeatherEntity>()
                        .Where(w => w.LocationId == locationId)
                        .ToListAsync();

                    if (!weatherEntities.Any())
                    {
                        return null;
                    }

                    var weatherEntity = weatherEntities
                        .OrderByDescending(w => w.LastUpdate)
                        .First();

                    var forecastEntities = await _context.Table<WeatherForecastEntity>()
                        .Where(f => f.WeatherId == weatherEntity.Id)
                        .OrderBy(f => f.Date)
                        .ToListAsync();

                    var weather = MapToDomain(weatherEntity, forecastEntities);
                    return weather;
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
                    var weatherEntity = MapToEntity(weather);
                    await _context.InsertAsync(weatherEntity);

                    SetPrivateProperty(weather, "Id", weatherEntity.Id);

                    foreach (var forecast in weather.Forecasts)
                    {
                        var forecastEntity = MapForecastToEntity(forecast, weatherEntity.Id);
                        await _context.InsertAsync(forecastEntity);
                        SetPrivateProperty(forecast, "Id", forecastEntity.Id);
                    }

                    _logger.LogInformation("Created weather with ID {WeatherId} for location {LocationId}",
                        weatherEntity.Id, weather.LocationId);

                    return weather;
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
                    var weatherEntity = MapToEntity(weather);
                    await _context.UpdateAsync(weatherEntity);

                    var existingForecasts = await _context.Table<WeatherForecastEntity>()
                        .Where(f => f.WeatherId == weather.Id)
                        .ToListAsync();

                    foreach (var forecast in existingForecasts)
                    {
                        await _context.DeleteAsync(forecast);
                    }

                    foreach (var forecast in weather.Forecasts)
                    {
                        var forecastEntity = MapForecastToEntity(forecast, weather.Id);
                        await _context.InsertAsync(forecastEntity);
                        SetPrivateProperty(forecast, "Id", forecastEntity.Id);
                    }

                    _logger.LogInformation("Updated weather with ID {WeatherId}", weather.Id);
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
                    var forecasts = await _context.Table<WeatherForecastEntity>()
                        .Where(f => f.WeatherId == weather.Id)
                        .ToListAsync();

                    foreach (var forecast in forecasts)
                    {
                        await _context.DeleteAsync(forecast);
                    }

                    var weatherEntity = MapToEntity(weather);
                    await _context.DeleteAsync(weatherEntity);

                    _logger.LogInformation("Deleted weather with ID {WeatherId}", weather.Id);
                },
                _exceptionMapper,
                "Delete",
                "weather",
                _logger);
        }

        public async Task<IEnumerable<Weather>> GetRecentAsync(int count = 10, CancellationToken cancellationToken = default)
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    var allWeatherEntities = await _context.Table<WeatherEntity>()
                        .ToListAsync();

                    var weatherEntities = allWeatherEntities
                        .OrderByDescending(w => w.LastUpdate)
                        .Take(count)
                        .ToList();

                    var weatherList = new List<Weather>();

                    foreach (var weatherEntity in weatherEntities)
                    {
                        var forecastEntities = await _context.Table<WeatherForecastEntity>()
                            .Where(f => f.WeatherId == weatherEntity.Id)
                            .OrderBy(f => f.Date)
                            .ToListAsync();

                        var weather = MapToDomain(weatherEntity, forecastEntities);
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

                    var weatherEntities = await _context.Table<WeatherEntity>()
                        .Where(w => w.LastUpdate < cutoffDate)
                        .ToListAsync();

                    var weatherList = new List<Weather>();

                    foreach (var weatherEntity in weatherEntities)
                    {
                        var forecastEntities = await _context.Table<WeatherForecastEntity>()
                            .Where(f => f.WeatherId == weatherEntity.Id)
                            .OrderBy(f => f.Date)
                            .ToListAsync();

                        var weather = MapToDomain(weatherEntity, forecastEntities);
                        weatherList.Add(weather);
                    }

                    return weatherList;
                },
                _exceptionMapper,
                "GetExpired",
                "weather",
                _logger);
        }

        #region Mapping Methods

        private Weather MapToDomain(WeatherEntity entity, List<WeatherForecastEntity> forecastEntities)
        {
            var coordinate = new Coordinate(entity.Latitude, entity.Longitude);
            var weather = CreateWeatherViaReflection(entity.LocationId, coordinate, entity.Timezone, entity.TimezoneOffset);

            SetPrivateProperty(weather, "Id", entity.Id);
            SetPrivateProperty(weather, "_lastUpdate", entity.LastUpdate);

            var forecasts = forecastEntities.Select(f => MapForecastToDomain(f)).ToList();
            weather.UpdateForecasts(forecasts);

            return weather;
        }

        private WeatherForecast MapForecastToDomain(WeatherForecastEntity entity)
        {
            var temperature = Temperature.FromCelsius(entity.Temperature);
            var minTemperature = Temperature.FromCelsius(entity.MinTemperature);
            var maxTemperature = Temperature.FromCelsius(entity.MaxTemperature);
            var wind = new WindInfo(entity.WindSpeed, entity.WindDirection, entity.WindGust);

            var forecast = CreateWeatherForecastViaReflection(
                entity.WeatherId, entity.Date, entity.Sunrise, entity.Sunset,
                temperature, minTemperature, maxTemperature, entity.Description, entity.Icon,
                wind, entity.Humidity, entity.Pressure, entity.Clouds, entity.UvIndex);

            SetPrivateProperty(forecast, "Id", entity.Id);
            if (entity.Precipitation.HasValue)
            {
                forecast.SetPrecipitation(entity.Precipitation.Value);
            }
            forecast.SetMoonData(entity.MoonRise, entity.MoonSet, entity.MoonPhase);

            return forecast;
        }

        private WeatherEntity MapToEntity(Weather weather)
        {
            return new WeatherEntity
            {
                Id = weather.Id,
                LocationId = weather.LocationId,
                Latitude = weather.Coordinate.Latitude,
                Longitude = weather.Coordinate.Longitude,
                Timezone = weather.Timezone,
                TimezoneOffset = weather.TimezoneOffset,
                LastUpdate = weather.LastUpdate
            };
        }

        private WeatherForecastEntity MapForecastToEntity(WeatherForecast forecast, int weatherId)
        {
            return new WeatherForecastEntity
            {
                Id = forecast.Id,
                WeatherId = weatherId,
                Date = forecast.Date,
                Sunrise = forecast.Sunrise,
                Sunset = forecast.Sunset,
                Temperature = forecast.Temperature.Celsius,
                MinTemperature = forecast.MinTemperature.Celsius,
                MaxTemperature = forecast.MaxTemperature.Celsius,
                Description = forecast.Description,
                Icon = forecast.Icon,
                WindSpeed = forecast.Wind.Speed,
                WindDirection = forecast.Wind.Direction,
                WindGust = forecast.Wind.Gust,
                Humidity = forecast.Humidity,
                Pressure = forecast.Pressure,
                Clouds = forecast.Clouds,
                UvIndex = forecast.UvIndex,
                Precipitation = forecast.Precipitation,
                MoonRise = forecast.MoonRise,
                MoonSet = forecast.MoonSet,
                MoonPhase = forecast.MoonPhase
            };
        }

        private Weather CreateWeatherViaReflection(int locationId, Coordinate coordinate, string timezone, int timezoneOffset)
        {
            var type = typeof(Weather);
            var constructor = type.GetConstructor(new[] { typeof(int), typeof(Coordinate), typeof(string), typeof(int) });
            if (constructor == null)
                throw new InvalidOperationException("Cannot find Weather constructor");
            return (Weather)constructor.Invoke(new object[] { locationId, coordinate, timezone, timezoneOffset });
        }

        private WeatherForecast CreateWeatherForecastViaReflection(int weatherId, DateTime date, DateTime sunrise, DateTime sunset,
            Temperature temperature, Temperature minTemperature, Temperature maxTemperature, string description, string icon,
            WindInfo wind, int humidity, int pressure, int clouds, double uvIndex)
        {
            var type = typeof(WeatherForecast);
            var constructor = type.GetConstructor(new[]
            {
                typeof(int), typeof(DateTime), typeof(DateTime), typeof(DateTime),
                typeof(Temperature), typeof(Temperature), typeof(Temperature),
                typeof(string), typeof(string), typeof(WindInfo),
                typeof(int), typeof(int), typeof(int), typeof(double)
            });
            if (constructor == null)
                throw new InvalidOperationException("Cannot find WeatherForecast constructor");
            return (WeatherForecast)constructor.Invoke(new object[]
            {
                weatherId, date, sunrise, sunset, temperature, minTemperature, maxTemperature,
                description, icon, wind, humidity, pressure, clouds, uvIndex
            });
        }

        private void SetPrivateProperty(object obj, string propertyName, object value)
        {
            var property = obj.GetType().GetProperty(propertyName);
            if (property == null)
            {
                var field = obj.GetType().GetField(propertyName,
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                field?.SetValue(obj, value);
            }
            else
            {
                property.SetValue(obj, value);
            }
        }

        #endregion
    }
}