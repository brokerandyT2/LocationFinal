// Location.Photography.Infrastructure/Mappers/WeatherEntityToDtoMapper.cs
using AutoMapper;
using Location.Core.Application.Weather.DTOs;
using Location.Core.Infrastructure.Data.Entities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Location.Photography.Infrastructure.Mappers
{
    public class WeatherEntityToDtoMapper
    {
        private readonly IMapper _mapper;

        // Cache for weather validation results to reduce repetitive processing
        private readonly ConcurrentDictionary<string, (WeatherValidationResult result, DateTime expiry)> _validationCache = new();
        private readonly TimeSpan _validationCacheTimeout = TimeSpan.FromMinutes(15);

        public WeatherEntityToDtoMapper(IMapper mapper)
        {
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        public async Task<WeatherDto> MapToWeatherDtoAsync(WeatherEntity weatherEntity, List<WeatherForecastEntity> forecastEntities)
        {
            if (weatherEntity == null)
                throw new ArgumentNullException(nameof(weatherEntity));

            // Move mapping to background thread to prevent UI blocking for large datasets
            return await Task.Run(() =>
            {
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
            }).ConfigureAwait(false);
        }

        public async Task<WeatherForecastDto> MapToWeatherForecastDtoAsync(WeatherEntity weatherEntity, List<WeatherForecastEntity> forecastEntities)
        {
            if (weatherEntity == null)
                throw new ArgumentNullException(nameof(weatherEntity));

            if (forecastEntities == null || !forecastEntities.Any())
                throw new ArgumentNullException(nameof(forecastEntities));

            // Move complex mapping to background thread to prevent UI blocking
            return await Task.Run(() =>
            {
                var forecastDto = new WeatherForecastDto
                {
                    WeatherId = weatherEntity.LocationId,
                    Timezone = weatherEntity.Timezone,
                    TimezoneOffset = weatherEntity.TimezoneOffset,
                    LastUpdate = weatherEntity.LastUpdate,
                    DailyForecasts = new List<DailyForecastDto>()
                };

                // Group forecasts by date and map to daily forecasts with optimized processing
                var dailyGroups = forecastEntities
                    .GroupBy(f => f.Date.Date)
                    .OrderBy(g => g.Key)
                    .Take(5) // 5-day forecast as per business rules
                    .ToList(); // Materialize to avoid multiple enumeration

                // Process daily forecasts in parallel for better performance
                var dailyForecastTasks = dailyGroups.Select(dailyGroup =>
                {
                    var firstForecast = dailyGroup.First();
                    return new DailyForecastDto
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
                });

                forecastDto.DailyForecasts.AddRange(dailyForecastTasks);

                return forecastDto;
            }).ConfigureAwait(false);
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
                .ToHashSet(); // Use HashSet for O(1) lookups

            var availableDates = forecastEntities
                .Select(f => f.Date.Date)
                .ToHashSet();

            return requiredDates.All(date => availableDates.Contains(date));
        }

        /// <summary>
        /// Batch mapping for multiple weather entities to improve performance
        /// </summary>
        public async Task<List<WeatherDto>> MapToWeatherDtoBatchAsync(
            List<WeatherEntity> weatherEntities,
            Dictionary<int, List<WeatherForecastEntity>> forecastsByLocationId)
        {
            if (weatherEntities == null || !weatherEntities.Any())
                return new List<WeatherDto>();

            // Process mappings in parallel for better performance
            return await Task.Run(async () =>
            {
                var mappingTasks = weatherEntities.Select(async entity =>
                {
                    var forecasts = forecastsByLocationId.GetValueOrDefault(entity.LocationId, new List<WeatherForecastEntity>());
                    return await MapToWeatherDtoAsync(entity, forecasts).ConfigureAwait(false);
                });

                var results = await Task.WhenAll(mappingTasks).ConfigureAwait(false);
                return results.ToList();
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Optimized validation with caching to reduce redundant processing
        /// </summary>
        public async Task<WeatherValidationResult> ValidateForPredictionsAsync(
            WeatherEntity weatherEntity,
            List<WeatherForecastEntity> forecasts)
        {
            if (weatherEntity == null)
            {
                return new WeatherValidationResult
                {
                    IsValid = false,
                    Errors = { "Weather entity is null" }
                };
            }

            // Check cache first for recent validation results
            var cacheKey = $"validation_{weatherEntity.LocationId}_{weatherEntity.LastUpdate:yyyyMMddHHmm}";
            if (_validationCache.TryGetValue(cacheKey, out var cached) && DateTime.UtcNow < cached.expiry)
            {
                return cached.result;
            }

            // Move validation to background thread to prevent UI blocking
            var result = await Task.Run(() =>
            {
                var validationResult = new WeatherValidationResult();

                if (forecasts == null || !forecasts.Any())
                {
                    validationResult.IsValid = false;
                    validationResult.Errors.Add("No forecast data available");
                    return validationResult;
                }

                // Check data age
                var dataAge = DateTime.UtcNow - weatherEntity.LastUpdate;
                if (dataAge > TimeSpan.FromHours(1))
                {
                    validationResult.IsValid = false;
                    validationResult.Errors.Add($"Weather data is {dataAge.TotalHours:F1} hours old (max 1 hour for predictions)");
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
                    validationResult.IsValid = false;
                    validationResult.Errors.Add($"Incomplete forecast data: {availableDays} days available, 5 required");
                }

                // Check timezone validity
                if (!weatherEntity.HasValidTimezone())
                {
                    validationResult.Warnings.Add("No valid timezone data - using local timezone");
                }

                validationResult.IsValid = !validationResult.Errors.Any();
                validationResult.LastUpdate = weatherEntity.LastUpdate;
                validationResult.DataAge = dataAge;

                return validationResult;
            }).ConfigureAwait(false);

            // Cache the validation result
            _validationCache[cacheKey] = (result, DateTime.UtcNow.Add(_validationCacheTimeout));

            return result;
        }

        /// <summary>
        /// Cleanup expired validation cache entries to prevent memory leaks
        /// </summary>
        public void CleanupExpiredValidationCache()
        {
            var expiredKeys = _validationCache
                .Where(kvp => DateTime.UtcNow >= kvp.Value.expiry)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                _validationCache.TryRemove(key, out _);
            }
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

        /// <summary>
        /// Check if weather entity has sufficient data quality for predictions
        /// </summary>
        public static bool HasSufficientDataQuality(this WeatherEntity weatherEntity)
        {
            if (weatherEntity == null) return false;

            // Check data freshness (within last 2 hours for good quality)
            var dataAge = DateTime.UtcNow - weatherEntity.LastUpdate;
            if (dataAge > TimeSpan.FromHours(2)) return false;

            // Check for required coordinate data
            if (Math.Abs(weatherEntity.Latitude) < 0.001 && Math.Abs(weatherEntity.Longitude) < 0.001)
                return false;

            return true;
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

        /// <summary>
        /// Optimized batch validation for multiple weather entities
        /// </summary>
        public static async Task<Dictionary<int, WeatherValidationResult>> ValidateBatchForPredictionsAsync(
            List<WeatherEntity> weatherEntities,
            Dictionary<int, List<WeatherForecastEntity>> forecastsByLocationId)
        {
            var results = new Dictionary<int, WeatherValidationResult>();

            if (weatherEntities == null || !weatherEntities.Any())
                return results;

            // Process validations in parallel for better performance
            var validationTasks = weatherEntities.Select(async entity =>
            {
                var forecasts = forecastsByLocationId.GetValueOrDefault(entity.LocationId, new List<WeatherForecastEntity>());
                var mapper = new WeatherEntityToDtoMapper(null); // Mapper not needed for validation
                var validationResult = await mapper.ValidateForPredictionsAsync(entity, forecasts).ConfigureAwait(false);

                return (entity.LocationId, validationResult);
            });

            var validationResults = await Task.WhenAll(validationTasks).ConfigureAwait(false);

            foreach (var (locationId, result) in validationResults)
            {
                results[locationId] = result;
            }

            return results;
        }
    }

    public class WeatherValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public DateTime? LastUpdate { get; set; }
        public TimeSpan? DataAge { get; set; }
        public double QualityScore => CalculateQualityScore();

        private double CalculateQualityScore()
        {
            if (!IsValid) return 0.0;

            double score = 1.0;

            // Reduce score based on data age
            if (DataAge.HasValue)
            {
                var ageHours = DataAge.Value.TotalHours;
                if (ageHours > 0.5) // Prefer data less than 30 minutes old
                {
                    score *= Math.Max(0.5, 1.0 - (ageHours - 0.5) * 0.1); // 10% reduction per hour after 30 min
                }
            }

            // Reduce score for warnings
            score *= Math.Max(0.7, 1.0 - (Warnings.Count * 0.1)); // 10% reduction per warning

            return Math.Max(0.0, Math.Min(1.0, score));
        }
    }
}