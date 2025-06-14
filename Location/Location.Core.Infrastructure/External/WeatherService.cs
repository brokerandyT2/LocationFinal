﻿using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Services;
using Location.Core.Application.Weather.DTOs;
using Location.Core.Domain.ValueObjects;
using Location.Core.Infrastructure.External.Models;
using Location.Core.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using System.Text.Json;

namespace Location.Core.Infrastructure.External
{
    public class WeatherService : IWeatherService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<WeatherService> _logger;
        private readonly IInfrastructureExceptionMappingService _exceptionMapper;
        private readonly IAsyncPolicy<HttpResponseMessage> _retryPolicy;

        private const string API_KEY_SETTING = "WeatherApiKey";
        private const string BASE_URL = "https://api.openweathermap.org/data/3.0/onecall";
        private const int MAX_FORECAST_DAYS = 7;

        public WeatherService(
            IHttpClientFactory httpClientFactory,
            IUnitOfWork unitOfWork,
            ILogger<WeatherService> logger,
            IInfrastructureExceptionMappingService exceptionMapper)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _exceptionMapper = exceptionMapper ?? throw new ArgumentNullException(nameof(exceptionMapper));

            _retryPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(
                    3,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetryAsync: async (outcome, timespan, retryCount, context) =>
                    {
                        _logger.LogWarning("Retry {RetryCount} after {Timespan} seconds", retryCount, timespan.TotalSeconds);
                        await Task.CompletedTask;
                    });
        }
        private bool HasCompleteForecastData(Domain.Entities.Weather weather)
        {
            if (weather.Forecasts == null || weather.Forecasts.Count == 0)
                return false;

            var today = DateTime.Today;
            var requiredDates = Enumerable.Range(0, 5)
                .Select(i => today.AddDays(i))
                .ToList();

            // Check if we have forecast data for all required dates
            foreach (var requiredDate in requiredDates)
            {
                if (!weather.Forecasts.Any(f => f.Date.Date == requiredDate.Date))
                {
                    return false;
                }
            }

            return true;
        }
        public async Task<Result<WeatherDto>> UpdateWeatherForLocationAsync(
           int locationId,
           CancellationToken cancellationToken = default)
        {
            try
            {
                // Get location details
                var locationResult = await _unitOfWork.Locations.GetByIdAsync(locationId, cancellationToken);
                if (!locationResult.IsSuccess || locationResult.Data == null)
                {
                    return Result<WeatherDto>.Failure("Location not found");
                }

                var location = locationResult.Data;

                // Check if existing weather data is fresh (less than 2 days old)
                var existingWeather = await _unitOfWork.Weather.GetByLocationIdAsync(locationId, cancellationToken);

                if (existingWeather != null &&
                    existingWeather.LastUpdate >= DateTime.UtcNow.AddDays(-2) &&
                    HasCompleteForecastData(existingWeather))
                {
                    // Data is fresh, return existing data without API call
                    var existingDto = await MapToWeatherDtoAsync(existingWeather, null, cancellationToken);
                    return Result<WeatherDto>.Success(existingDto);
                }

                // Data is stale or missing, fetch from API
                var weatherResult = await GetWeatherFromApiAsync(
                    location.Coordinate.Latitude,
                    location.Coordinate.Longitude,
                    cancellationToken);

                if (!weatherResult.IsSuccess || weatherResult.Data == null)
                {
                    // If API fails but we have existing data, return it
                    if (existingWeather != null)
                    {
                        var fallbackDto = await MapToWeatherDtoAsync(existingWeather, null, cancellationToken);
                        return Result<WeatherDto>.Success(fallbackDto);
                    }

                    return Result<WeatherDto>.Failure(weatherResult.ErrorMessage ?? "Failed to fetch weather");
                }

                var apiData = weatherResult.Data;

                // Create or update weather entity
                var coordinate = new Coordinate(location.Coordinate.Latitude, location.Coordinate.Longitude);
                Domain.Entities.Weather weather;

                if (existingWeather != null)
                {
                    // Update existing weather record
                    weather = existingWeather;
                }
                else
                {
                    // Create new weather record
                    weather = new Domain.Entities.Weather(
                        locationId,
                        coordinate,
                        apiData.Timezone,
                        apiData.TimezoneOffset);
                }

                // Create forecast entities from API data (store raw wind direction)
                var forecasts = new List<Domain.Entities.WeatherForecast>();

                foreach (var dailyForecast in apiData.DailyForecasts.Take(7))
                {
                    var forecastEntity = new Domain.Entities.WeatherForecast(
                        weather.Id,
                        dailyForecast.Date,
                        dailyForecast.Sunrise,
                        dailyForecast.Sunset,
                        dailyForecast.Temperature,
                        dailyForecast.MinTemperature,
                        dailyForecast.MaxTemperature,
                        dailyForecast.Description,
                        dailyForecast.Icon,
                        new WindInfo(dailyForecast.WindSpeed, dailyForecast.WindDirection, dailyForecast.WindGust),
                        dailyForecast.Humidity,
                        dailyForecast.Pressure,
                        dailyForecast.Clouds,
                        dailyForecast.UvIndex);

                    // Set optional data
                    forecastEntity.SetMoonData(dailyForecast.MoonRise, dailyForecast.MoonSet, dailyForecast.MoonPhase);
                    if (dailyForecast.Precipitation.HasValue)
                    {
                        forecastEntity.SetPrecipitation(dailyForecast.Precipitation.Value);
                    }

                    forecasts.Add(forecastEntity);
                }

                // Create hourly forecast entities from API data (store raw wind direction)
                var hourlyForecasts = new List<Domain.Entities.HourlyForecast>();

                foreach (var hourlyData in apiData.HourlyForecasts.Take(48))
                {
                    var hourlyForecast = new Domain.Entities.HourlyForecast(
                        weather.Id,
                        hourlyData.DateTime,
                        hourlyData.Temperature,
                        hourlyData.FeelsLike,
                        hourlyData.Description,
                        hourlyData.Icon,
                        new WindInfo(hourlyData.WindSpeed, hourlyData.WindDirection, hourlyData.WindGust),
                        hourlyData.Humidity,
                        hourlyData.Pressure,
                        hourlyData.Clouds,
                        hourlyData.UvIndex,
                        hourlyData.ProbabilityOfPrecipitation,
                        hourlyData.Visibility,
                        hourlyData.DewPoint);

                    hourlyForecasts.Add(hourlyForecast);
                }

                // Update weather with new forecasts
                weather.UpdateForecasts(forecasts);
                weather.UpdateHourlyForecasts(hourlyForecasts);

                // Save to database
                if (existingWeather != null)
                {
                    // Update existing record
                    _unitOfWork.Weather.Update(existingWeather);
                }
                else
                {
                    // Add new record
                    await _unitOfWork.Weather.AddAsync(weather, cancellationToken);
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Map current weather data to DTO and return
                var weatherDto = await MapToWeatherDtoAsync(weather, apiData, cancellationToken);
                return Result<WeatherDto>.Success(weatherDto);
            }
            catch (Exception ex)
            {
                var domainException = _exceptionMapper.MapToWeatherDomainException(ex, "UpdateWeatherForLocation");
                throw domainException;
            }
        }

        public async Task<Result<WeatherForecastDto>> GetForecastAsync(
            double latitude,
            double longitude,
            int days = 7,
            CancellationToken cancellationToken = default)
        {
            var result = await GetWeatherFromApiAsync(latitude, longitude, cancellationToken);
            if (!result.IsSuccess || result.Data == null)
            {
                return Result<WeatherForecastDto>.Failure(result.ErrorMessage ?? "Failed to get forecast");
            }

            var forecastDto = new WeatherForecastDto
            {
                Timezone = result.Data.Timezone,
                TimezoneOffset = result.Data.TimezoneOffset,
                LastUpdate = DateTime.UtcNow,
                DailyForecasts = result.Data.DailyForecasts.Take(days).ToList()
            };

            return Result<WeatherForecastDto>.Success(forecastDto);
        }

        public async Task<Result<HourlyWeatherForecastDto>> GetHourlyForecastAsync(
            double latitude,
            double longitude,
            CancellationToken cancellationToken = default)
        {
            var result = await GetWeatherFromApiAsync(latitude, longitude, cancellationToken);
            if (!result.IsSuccess || result.Data == null)
            {
                return Result<HourlyWeatherForecastDto>.Failure(result.ErrorMessage ?? "Failed to get hourly forecast");
            }

            var hourlyForecastDto = new HourlyWeatherForecastDto
            {
                Timezone = result.Data.Timezone,
                TimezoneOffset = result.Data.TimezoneOffset,
                LastUpdate = DateTime.UtcNow,
                HourlyForecasts = result.Data.HourlyForecasts.Take(48).ToList()
            };

            return Result<HourlyWeatherForecastDto>.Success(hourlyForecastDto);
        }

        public async Task<Result<int>> UpdateAllWeatherAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var locationsResult = await _unitOfWork.Locations.GetActiveAsync(cancellationToken);
                if (!locationsResult.IsSuccess || locationsResult.Data == null)
                {
                    return Result<int>.Failure("Failed to retrieve active locations");
                }

                var locations = locationsResult.Data;
                int successCount = 0;

                foreach (var location in locations)
                {
                    try
                    {
                        var result = await UpdateWeatherForLocationAsync(location.Id, cancellationToken);
                        if (result.IsSuccess)
                        {
                            successCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to update weather for location {LocationId}", location.Id);
                    }
                }

                return Result<int>.Success(successCount);
            }
            catch (Exception ex)
            {
                var domainException = _exceptionMapper.MapToWeatherDomainException(ex, "UpdateAllWeather");
                throw domainException;
            }
        }

        private async Task<Result<WeatherApiResponse>> GetWeatherFromApiAsync(
            double latitude,
            double longitude,
            CancellationToken cancellationToken)
        {
            try
            {
                var apiKeyResult = await GetApiKeyAsync(cancellationToken);
                var tempScale = await _unitOfWork.Settings.GetByKeyAsync("TemperatureType");

                if (!apiKeyResult.IsSuccess || string.IsNullOrWhiteSpace(apiKeyResult.Data))
                {
                    return Result<WeatherApiResponse>.Failure("Weather API key not configured");
                }

                var apiKey = apiKeyResult.Data;
                var client = _httpClientFactory.CreateClient();
                var tempS = tempScale.Data?.Value == "F" ? "imperial" : "metric";
                var requestUrl = $"{BASE_URL}?lat={latitude}&lon={longitude}&appid={apiKey}&units={tempS}&exclude=minutely";

                var response = await _retryPolicy.ExecuteAsync(async () =>
                    await client.GetAsync(requestUrl, cancellationToken));

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Weather API request failed with status {StatusCode}", response.StatusCode);
                    return Result<WeatherApiResponse>.Failure($"Weather API request failed: {response.StatusCode}");
                }

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var weatherData = JsonSerializer.Deserialize<OpenWeatherResponse>(json);

                if (weatherData == null)
                {
                    return Result<WeatherApiResponse>.Failure("Failed to parse weather data");
                }

                var apiResponse = MapToApiResponse(weatherData);
                return Result<WeatherApiResponse>.Success(apiResponse);
            }
            catch (Exception ex)
            {
                var domainException = _exceptionMapper.MapToWeatherDomainException(ex, "GetWeatherFromApi");
                throw domainException;
            }
        }

        private async Task<Result<string>> GetApiKeyAsync(CancellationToken cancellationToken)
        {
            var settingResult = await _unitOfWork.Settings.GetByKeyAsync(API_KEY_SETTING, cancellationToken);

            if (!settingResult.IsSuccess || settingResult.Data == null)
            {
                _logger.LogWarning("Weather API key not found in settings");
                return Result<string>.Failure("API key not found");
            }

            return Result<string>.Success(settingResult.Data.Value);
        }

        private async Task<WeatherDto> MapToWeatherDtoAsync(Domain.Entities.Weather weather, WeatherApiResponse apiData, CancellationToken cancellationToken)
        {
            var currentForecast = weather.GetCurrentForecast();
            var currentApiData = apiData?.DailyForecasts?.FirstOrDefault();

            // Get raw wind direction from stored data
            var rawWindDirection = currentForecast?.Wind.Direction ?? currentApiData?.WindDirection ?? 0;

            // Transform based on user preference
            var displayWindDirection = await GetDisplayWindDirectionAsync(rawWindDirection, cancellationToken);

            return new WeatherDto
            {
                Id = weather.Id,
                LocationId = weather.LocationId,
                Latitude = weather.Coordinate.Latitude,
                Longitude = weather.Coordinate.Longitude,
                Timezone = weather.Timezone,
                TimezoneOffset = weather.TimezoneOffset,
                LastUpdate = weather.LastUpdate,
                Temperature = currentForecast?.Temperature ?? currentApiData?.Temperature ?? 0,
                MinimumTemp = currentForecast?.MinTemperature ?? currentApiData?.MinTemperature ?? 0,
                MaximumTemp = currentForecast?.MaxTemperature ?? currentApiData?.MaxTemperature ?? 0,
                Description = currentForecast?.Description ?? currentApiData?.Description ?? string.Empty,
                Icon = currentForecast?.Icon ?? currentApiData?.Icon ?? string.Empty,
                WindSpeed = currentForecast?.Wind.Speed ?? currentApiData?.WindSpeed ?? 0,
                WindDirection = displayWindDirection,
                WindGust = currentForecast?.Wind.Gust ?? currentApiData?.WindGust,
                Humidity = currentForecast?.Humidity ?? currentApiData?.Humidity ?? 0,
                Pressure = currentForecast?.Pressure ?? currentApiData?.Pressure ?? 0,
                Clouds = currentForecast?.Clouds ?? currentApiData?.Clouds ?? 0,
                UvIndex = currentForecast?.UvIndex ?? currentApiData?.UvIndex ?? 0,
                Precipitation = currentForecast?.Precipitation ?? currentApiData?.Precipitation,
                Sunrise = currentForecast?.Sunrise ?? currentApiData?.Sunrise ?? DateTime.MinValue,
                Sunset = currentForecast?.Sunset ?? currentApiData?.Sunset ?? DateTime.MinValue,
                MoonRise = currentForecast?.MoonRise ?? currentApiData?.MoonRise,
                MoonSet = currentForecast?.MoonSet ?? currentApiData?.MoonSet,
                MoonPhase = currentForecast?.MoonPhase ?? currentApiData?.MoonPhase ?? 0
            };
        }

        private async Task<double> GetDisplayWindDirectionAsync(double rawDirection, CancellationToken cancellationToken)
        {
            try
            {
                var windDirectionSetting = await _unitOfWork.Settings.GetByKeyAsync("WindDirection", cancellationToken);

                if (windDirectionSetting.IsSuccess && windDirectionSetting.Data?.Value == "towardsWind")
                {
                    // Inverse the direction (add 180 degrees, wrap around)
                    return (rawDirection + 180) % 360;
                }

                // Default: withWind (use raw API direction)
                return rawDirection;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get wind direction setting, using raw direction");
                return rawDirection;
            }
        }

        private WeatherApiResponse MapToApiResponse(OpenWeatherResponse response)
        {
            var apiResponse = new WeatherApiResponse
            {
                Timezone = response.Timezone,
                TimezoneOffset = response.TimezoneOffset,
                DailyForecasts = new List<DailyForecastDto>(),
                HourlyForecasts = new List<HourlyForecastDto>()
            };

            // Map daily forecasts
            for (int i = 0; i < Math.Min(response.Daily.Count, 7); i++)
            {
                var daily = response.Daily[i];
                var dailyDto = new DailyForecastDto
                {
                    Date = DateTimeOffset.FromUnixTimeSeconds(daily.Dt).DateTime,
                    Sunrise = DateTimeOffset.FromUnixTimeSeconds(daily.Sunrise).DateTime,
                    Sunset = DateTimeOffset.FromUnixTimeSeconds(daily.Sunset).DateTime,
                    Temperature = daily.Temp.Day,
                    MinTemperature = daily.Temp.Min,
                    MaxTemperature = daily.Temp.Max,
                    Description = daily.Weather.FirstOrDefault()?.Description ?? string.Empty,
                    Icon = daily.Weather.FirstOrDefault()?.Icon ?? string.Empty,
                    WindSpeed = daily.WindSpeed,
                    WindDirection = daily.WindDeg,
                    WindGust = daily.WindGust,
                    Humidity = daily.Humidity,
                    Pressure = daily.Pressure,
                    Clouds = daily.Clouds,
                    UvIndex = daily.Uvi,
                    Precipitation = daily.Pop,
                    MoonRise = daily.MoonRise > 0 ? DateTimeOffset.FromUnixTimeSeconds(daily.MoonRise).DateTime : null,
                    MoonSet = daily.MoonSet > 0 ? DateTimeOffset.FromUnixTimeSeconds(daily.MoonSet).DateTime : null,
                    MoonPhase = daily.MoonPhase
                };

                apiResponse.DailyForecasts.Add(dailyDto);
            }

            // Map hourly forecasts
            for (int i = 0; i < Math.Min(response.Hourly.Count, 48); i++)
            {
                var hourly = response.Hourly[i];
                var hourlyDto = new HourlyForecastDto
                {
                    DateTime = DateTimeOffset.FromUnixTimeSeconds(hourly.Dt).DateTime,
                    Temperature = hourly.Temp,
                    FeelsLike = hourly.FeelsLike,
                    Description = hourly.Weather.FirstOrDefault()?.Description ?? string.Empty,
                    Icon = hourly.Weather.FirstOrDefault()?.Icon ?? string.Empty,
                    WindSpeed = hourly.WindSpeed,
                    WindDirection = hourly.WindDeg,
                    WindGust = hourly.WindGust,
                    Humidity = hourly.Humidity,
                    Pressure = hourly.Pressure,
                    Clouds = hourly.Clouds,
                    UvIndex = hourly.Uvi,
                    ProbabilityOfPrecipitation = hourly.Pop,
                    Visibility = hourly.Visibility,
                    DewPoint = hourly.DewPoint
                };

                apiResponse.HourlyForecasts.Add(hourlyDto);
            }

            return apiResponse;
        }

        Task<Result<WeatherDto>> IWeatherService.GetWeatherAsync(double latitude, double longitude, CancellationToken cancellationToken)
        {
            throw new NotImplementedException("Use UpdateWeatherForLocationAsync for offline-first persistence");
        }
    }

    internal class WeatherApiResponse
    {
        public string Timezone { get; set; } = string.Empty;
        public int TimezoneOffset { get; set; }
        public List<DailyForecastDto> DailyForecasts { get; set; } = new List<DailyForecastDto>();
        public List<HourlyForecastDto> HourlyForecasts { get; set; } = new List<HourlyForecastDto>();
    }
}