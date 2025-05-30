using AutoMapper;
using Location.Core.Application.Weather.DTOs;
using Location.Core.Infrastructure.Data.Entities;
using Location.Core.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
namespace Location.Photography.Infrastructure.Mappers
{
    public class WeatherEntityToDtoMapper
    {
        private readonly IMapper _mapper;
        public WeatherEntityToDtoMapper(IMapper mapper)
        {
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        public WeatherDto MapToWeatherDto(WeatherEntity weatherEntity, List<WeatherForecastEntity> forecastEntities)
        {
            if (weatherEntity == null)
                throw new ArgumentNullException(nameof(weatherEntity));

            var weatherDto = new WeatherDto
            {
                LocationId = weatherEntity.LocationId,
                Latitude = weatherEntity.Latitude,
                Longitude = weatherEntity.Longitude,
                Timezone = weatherEntity.Timezone,
                TimezoneOffset = weatherEntity.TimezoneOffset,
                LastUpdate = weatherEntity.LastUpdate
            };

            return weatherDto;
        }

        public WeatherForecastDto MapToWeatherForecastDto(WeatherEntity weatherEntity, List<WeatherForecastEntity> forecastEntities)
        {
            if (weatherEntity == null)
                throw new ArgumentNullException(nameof(weatherEntity));

            if (forecastEntities == null || !forecastEntities.Any())
                throw new ArgumentNullException(nameof(forecastEntities));

            var forecastDto = new WeatherForecastDto
            {
                WeatherId = weatherEntity.LocationId,
                Timezone = weatherEntity.Timezone,
                TimezoneOffset = weatherEntity.TimezoneOffset,
                LastUpdate = weatherEntity.LastUpdate,
                DailyForecasts = new List<DailyForecastDto>()
            };

            // Group forecasts by date and map to daily forecasts
            var dailyGroups = forecastEntities
                .GroupBy(f => f.Date.Date)
                .OrderBy(g => g.Key)
                .Take(5); // 5-day forecast as per business rules

            foreach (var dailyGroup in dailyGroups)
            {
                var firstForecast = dailyGroup.First();
                var dailyForecast = new DailyForecastDto
                {
                    Date = dailyGroup.Key,
                    Sunrise = firstForecast.Sunrise,
                    Sunset = firstForecast.Sunset,
                    Temperature = firstForecast.Temperature,
                    MinTemperature = firstForecast.MinTemperature,
                    MaxTemperature = firstForecast.MaxTemperature,
                    Description = firstForecast.Description,
                    Icon = firstForecast.Icon,
                    WindSpeed = firstForecast.WindSpeed,
                    WindDirection = firstForecast.WindDirection,
                    WindGust = firstForecast.WindGust,
                    Humidity = firstForecast.Humidity,
                    Pressure = firstForecast.Pressure,
                    Clouds = firstForecast.Clouds,
                    UvIndex = firstForecast.UvIndex,
                    Precipitation = firstForecast.Precipitation,
                    MoonRise = firstForecast.MoonRise,
                    MoonSet = firstForecast.MoonSet,
                    MoonPhase = firstForecast.MoonPhase
                };

                forecastDto.DailyForecasts.Add(dailyForecast);
            }

            return forecastDto;
        }

        public bool IsWeatherDataStale(WeatherEntity weatherEntity, TimeSpan maxAge)
        {
            if (weatherEntity == null)
                return true;

            return (DateTime.UtcNow - weatherEntity.LastUpdate) > maxAge;
        }

        public bool HasCompleteForecastData(List<WeatherForecastEntity> forecastEntities, DateTime targetDate)
        {
            if (forecastEntities == null || !forecastEntities.Any())
                return false;

            // Check if we have forecast data for target date + next 4 days (5-day total)
            var requiredDates = Enumerable.Range(0, 5)
                .Select(i => targetDate.Date.AddDays(i))
                .ToList();

            foreach (var requiredDate in requiredDates)
            {
                if (!forecastEntities.Any(f => f.Date.Date == requiredDate))
                {
                    return false;
                }
            }

            return true;
        }
    }

    // Extension methods for enhanced weather integration
    public static class WeatherEntityExtensions
    {
        public static bool NeedsUpdate(this WeatherEntity weatherEntity, TimeSpan maxAge)
        {
            if (weatherEntity == null)
                return true;

            return (DateTime.UtcNow - weatherEntity.LastUpdate) > maxAge;
        }

        public static bool HasValidTimezone(this WeatherEntity weatherEntity)
        {
            return weatherEntity != null && !string.IsNullOrEmpty(weatherEntity.Timezone);
        }

        public static TimeZoneInfo GetTimeZoneInfo(this WeatherEntity weatherEntity)
        {
            if (!weatherEntity.HasValidTimezone())
                return TimeZoneInfo.Local;

            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(weatherEntity.Timezone);
            }
            catch
            {
                return TimeZoneInfo.Local;
            }
        }
    }

    // Weather data validation helper
    public static class WeatherDataValidator
    {
        public static bool IsValidWeatherData(WeatherEntity weatherEntity, List<WeatherForecastEntity> forecasts)
        {
            if (weatherEntity == null)
                return false;

            if (forecasts == null || !forecasts.Any())
                return false;

            // Check if weather data is not too old (1 hour max age for predictions)
            if (weatherEntity.NeedsUpdate(TimeSpan.FromHours(1)))
                return false;

            // Check if we have minimum required forecast data
            var today = DateTime.Today;
            var requiredDays = 5; // 5-day forecast business rule

            var availableDays = forecasts
                .Where(f => f.Date.Date >= today)
                .Select(f => f.Date.Date)
                .Distinct()
                .Count();

            return availableDays >= requiredDays;
        }

        public static WeatherValidationResult ValidateForPredictions(WeatherEntity weatherEntity, List<WeatherForecastEntity> forecasts)
        {
            var result = new WeatherValidationResult();

            if (weatherEntity == null)
            {
                result.IsValid = false;
                result.Errors.Add("Weather entity is null");
                return result;
            }

            if (forecasts == null || !forecasts.Any())
            {
                result.IsValid = false;
                result.Errors.Add("No forecast data available");
                return result;
            }

            // Check data age
            var dataAge = DateTime.UtcNow - weatherEntity.LastUpdate;
            if (dataAge > TimeSpan.FromHours(1))
            {
                result.IsValid = false;
                result.Errors.Add($"Weather data is {dataAge.TotalHours:F1} hours old (max 1 hour for predictions)");
            }

            // Check forecast completeness
            var today = DateTime.Today;
            var availableDays = forecasts
                .Where(f => f.Date.Date >= today)
                .Select(f => f.Date.Date)
                .Distinct()
                .Count();

            if (availableDays < 5)
            {
                result.IsValid = false;
                result.Errors.Add($"Incomplete forecast data: {availableDays} days available, 5 required");
            }

            // Check timezone validity
            if (!weatherEntity.HasValidTimezone())
            {
                result.Warnings.Add("No valid timezone data - using local timezone");
            }

            result.IsValid = !result.Errors.Any();
            return result;
        }
    }

    public class WeatherValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public DateTime? LastUpdate { get; set; }
        public TimeSpan? DataAge { get; set; }
    }
}