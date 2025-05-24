using Location.Core.Application.Common.Interfaces.Persistence;
using Location.Core.Domain.Entities;
using Location.Core.Domain.ValueObjects;
using Location.Core.Infrastructure.Data.Entities;
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

        public WeatherRepository(IDatabaseContext context, ILogger<WeatherRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Weather?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                var weatherEntity = await _context.GetAsync<WeatherEntity>(id);

                if (weatherEntity == null)
                {
                    return null;
                }

                var forecastEntities = await _context.Table<WeatherForecastEntity>()
                    .Where(f => f.WeatherId == id)
                    .OrderBy(f => f.Date)
                    .ToListAsync();

                var weather = MapToDomain(weatherEntity, forecastEntities);
                return weather;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving weather with ID {WeatherId}", id);
                throw;
            }
        }

        public async Task<Weather?> GetByLocationIdAsync(int locationId, CancellationToken cancellationToken = default)
        {
            try
            {
                var weatherEntity = await _context.Table<WeatherEntity>()
                    .Where(w => w.LocationId == locationId)
                    .OrderByDescending(w => w.LastUpdate)
                    .FirstOrDefaultAsync();

                if (weatherEntity == null)
                {
                    return null;
                }

                var forecastEntities = await _context.Table<WeatherForecastEntity>()
                    .Where(f => f.WeatherId == weatherEntity.Id)
                    .OrderBy(f => f.Date)
                    .ToListAsync();

                var weather = MapToDomain(weatherEntity, forecastEntities);
                return weather;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving weather for location ID {LocationId}", locationId);
                throw;
            }
        }

        public async Task<Weather> AddAsync(Weather weather, CancellationToken cancellationToken = default)
        {
            try
            {
                // Create weather entity
                var weatherEntity = MapToEntity(weather);
                await _context.InsertAsync(weatherEntity);

                // Update domain object with generated ID
                SetPrivateProperty(weather, "Id", weatherEntity.Id);

                // Create forecast entities
                foreach (var forecast in weather.Forecasts)
                {
                    var forecastEntity = MapForecastToEntity(forecast, weatherEntity.Id);
                    await _context.InsertAsync(forecastEntity);
                    SetPrivateProperty(forecast, "Id", forecastEntity.Id);
                }

                _logger.LogInformation("Created weather with ID {WeatherId} for location {LocationId}",
                    weatherEntity.Id, weather.LocationId);

                return weather;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating weather");
                throw;
            }
        }

        public async Task UpdateAsync(Weather weather, CancellationToken cancellationToken = default)
        {
            try
            {
                // Update weather entity
                var weatherEntity = MapToEntity(weather);
                await _context.UpdateAsync(weatherEntity);

                // Delete existing forecasts
                var existingForecasts = await _context.Table<WeatherForecastEntity>()
                    .Where(f => f.WeatherId == weather.Id)
                    .ToListAsync();

                foreach (var forecast in existingForecasts)
                {
                    await _context.DeleteAsync(forecast);
                }

                // Create new forecasts
                foreach (var forecast in weather.Forecasts)
                {
                    var forecastEntity = MapForecastToEntity(forecast, weather.Id);
                    await _context.InsertAsync(forecastEntity);
                    SetPrivateProperty(forecast, "Id", forecastEntity.Id);
                }

                _logger.LogInformation("Updated weather with ID {WeatherId}", weather.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating weather with ID {WeatherId}", weather.Id);
                throw;
            }
        }

        public async Task DeleteAsync(Weather weather, CancellationToken cancellationToken = default)
        {
            try
            {
                // Delete forecasts first
                var forecasts = await _context.Table<WeatherForecastEntity>()
                    .Where(f => f.WeatherId == weather.Id)
                    .ToListAsync();

                foreach (var forecast in forecasts)
                {
                    await _context.DeleteAsync(forecast);
                }

                // Delete weather
                var weatherEntity = MapToEntity(weather);
                await _context.DeleteAsync(weatherEntity);

                _logger.LogInformation("Deleted weather with ID {WeatherId}", weather.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting weather with ID {WeatherId}", weather.Id);
                throw;
            }
        }

        public async Task<IEnumerable<Weather>> GetRecentAsync(int count = 10, CancellationToken cancellationToken = default)
        {
            try
            {
                var weatherEntities = await _context.Table<WeatherEntity>()
                    .OrderByDescending(w => w.LastUpdate)
                    .Take(count)
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
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving recent weather data");
                throw;
            }
        }

        public async Task<IEnumerable<Weather>> GetExpiredAsync(TimeSpan maxAge, CancellationToken cancellationToken = default)
        {
            try
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
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving expired weather data");
                throw;
            }
        }

        #region Mapping Methods

        private Weather MapToDomain(WeatherEntity entity, List<WeatherForecastEntity> forecastEntities)
        {
            var coordinate = new Coordinate(entity.Latitude, entity.Longitude);

            // Create weather using reflection
            var weather = CreateWeatherViaReflection(
                entity.LocationId,
                coordinate,
                entity.Timezone,
                entity.TimezoneOffset);

            // Set properties
            SetPrivateProperty(weather, "Id", entity.Id);
            SetPrivateProperty(weather, "_lastUpdate", entity.LastUpdate);

            // Map forecasts
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
                entity.WeatherId,
                entity.Date,
                entity.Sunrise,
                entity.Sunset,
                temperature,
                minTemperature,
                maxTemperature,
                entity.Description,
                entity.Icon,
                wind,
                entity.Humidity,
                entity.Pressure,
                entity.Clouds,
                entity.UvIndex);

            // Set additional properties
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
            var constructor = type.GetConstructor(
                new[] { typeof(int), typeof(Coordinate), typeof(string), typeof(int) });

            if (constructor == null)
            {
                throw new InvalidOperationException("Cannot find Weather constructor");
            }

            return (Weather)constructor.Invoke(
                new object[] { locationId, coordinate, timezone, timezoneOffset });
        }

        private WeatherForecast CreateWeatherForecastViaReflection(
            int weatherId,
            DateTime date,
            DateTime sunrise,
            DateTime sunset,
            Temperature temperature,
            Temperature minTemperature,
            Temperature maxTemperature,
            string description,
            string icon,
            WindInfo wind,
            int humidity,
            int pressure,
            int clouds,
            double uvIndex)
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
            {
                throw new InvalidOperationException("Cannot find WeatherForecast constructor");
            }

            return (WeatherForecast)constructor.Invoke(new object[]
            {
                weatherId, date, sunrise, sunset,
                temperature, minTemperature, maxTemperature,
                description, icon, wind,
                humidity, pressure, clouds, uvIndex
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