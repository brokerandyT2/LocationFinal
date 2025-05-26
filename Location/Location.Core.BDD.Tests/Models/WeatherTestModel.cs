using System;

namespace Location.Core.BDD.Tests.Models
{
    /// <summary>
    /// Model class for weather data in tests
    /// </summary>
    public class WeatherTestModel
    {
        /// <summary>
        /// Gets or sets the weather ID
        /// </summary>
        public int? Id { get; set; }

        /// <summary>
        /// Gets or sets the associated location ID
        /// </summary>
        public int LocationId { get; set; }

        /// <summary>
        /// Gets or sets the latitude
        /// </summary>
        public double Latitude { get; set; }

        /// <summary>
        /// Gets or sets the longitude
        /// </summary>
        public double Longitude { get; set; }

        /// <summary>
        /// Gets or sets the timezone name
        /// </summary>
        public string Timezone { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the timezone offset in seconds
        /// </summary>
        public int TimezoneOffset { get; set; }

        /// <summary>
        /// Gets or sets the last update time
        /// </summary>
        public DateTime LastUpdate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the current temperature in Celsius
        /// </summary>
        public double Temperature { get; set; }

        /// <summary>
        /// Gets or sets the weather description
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the weather icon code
        /// </summary>
        public string Icon { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the wind speed
        /// </summary>
        public double WindSpeed { get; set; }

        /// <summary>
        /// Gets or sets the wind direction in degrees
        /// </summary>
        public double WindDirection { get; set; }

        /// <summary>
        /// Gets or sets the wind gust speed
        /// </summary>
        public double? WindGust { get; set; }

        /// <summary>
        /// Gets or sets the humidity percentage
        /// </summary>
        public int Humidity { get; set; }

        /// <summary>
        /// Gets or sets the atmospheric pressure
        /// </summary>
        public int Pressure { get; set; }

        /// <summary>
        /// Gets or sets the cloud coverage percentage
        /// </summary>
        public int Clouds { get; set; }

        /// <summary>
        /// Gets or sets the UV index
        /// </summary>
        public double UvIndex { get; set; }

        /// <summary>
        /// Gets or sets the precipitation amount
        /// </summary>
        public double? Precipitation { get; set; }

        /// <summary>
        /// Gets or sets the sunrise time
        /// </summary>
        public DateTime Sunrise { get; set; }

        /// <summary>
        /// Gets or sets the sunset time
        /// </summary>
        public DateTime Sunset { get; set; }

        /// <summary>
        /// Gets or sets the moonrise time
        /// </summary>
        public DateTime? MoonRise { get; set; }

        /// <summary>
        /// Gets or sets the moonset time
        /// </summary>
        public DateTime? MoonSet { get; set; }

        /// <summary>
        /// Gets or sets the moon phase (0-1)
        /// </summary>
        public double MoonPhase { get; set; }

        /// <summary>
        /// Creates a test model from a domain entity
        /// </summary>
        public static WeatherTestModel FromDomainEntity(Domain.Entities.Weather weather)
        {
            var currentForecast = weather.GetCurrentForecast();
            var model = new WeatherTestModel
            {
                Id = weather.Id,
                LocationId = weather.LocationId,
                Latitude = weather.Coordinate.Latitude,
                Longitude = weather.Coordinate.Longitude,
                Timezone = weather.Timezone,
                TimezoneOffset = weather.TimezoneOffset,
                LastUpdate = weather.LastUpdate
            };

            if (currentForecast != null)
            {
                model.Temperature = currentForecast.Temperature;
                model.Description = currentForecast.Description;
                model.Icon = currentForecast.Icon;
                model.WindSpeed = currentForecast.Wind.Speed;
                model.WindDirection = currentForecast.Wind.Direction;
                model.WindGust = currentForecast.Wind.Gust;
                model.Humidity = currentForecast.Humidity;
                model.Pressure = currentForecast.Pressure;
                model.Clouds = currentForecast.Clouds;
                model.UvIndex = currentForecast.UvIndex;
                model.Precipitation = currentForecast.Precipitation;
                model.Sunrise = currentForecast.Sunrise;
                model.Sunset = currentForecast.Sunset;
                model.MoonRise = currentForecast.MoonRise;
                model.MoonSet = currentForecast.MoonSet;
                model.MoonPhase = currentForecast.MoonPhase;
            }

            return model;
        }

        /// <summary>
        /// Creates a test model from an application DTO
        /// </summary>
        public static WeatherTestModel FromDto(Application.Weather.DTOs.WeatherDto dto)
        {
            return new WeatherTestModel
            {
                Id = dto.Id,
                LocationId = dto.LocationId,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                Timezone = dto.Timezone,
                TimezoneOffset = dto.TimezoneOffset,
                LastUpdate = dto.LastUpdate,
                Temperature = dto.Temperature,
                Description = dto.Description,
                Icon = dto.Icon,
                WindSpeed = dto.WindSpeed,
                WindDirection = dto.WindDirection,
                WindGust = dto.WindGust,
                Humidity = dto.Humidity,
                Pressure = dto.Pressure,
                Clouds = dto.Clouds,
                UvIndex = dto.UvIndex,
                Precipitation = dto.Precipitation,
                Sunrise = dto.Sunrise,
                Sunset = dto.Sunset,
                MoonRise = dto.MoonRise,
                MoonSet = dto.MoonSet,
                MoonPhase = dto.MoonPhase
            };
        }
    }
}