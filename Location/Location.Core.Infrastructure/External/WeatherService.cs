using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Services;
using Location.Core.Application.Weather.DTOs;
using Location.Core.Domain.ValueObjects;
using Location.Core.Infrastructure.External.Models;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Core.Infrastructure.External
{
    public class WeatherService : IWeatherService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<WeatherService> _logger;
        private readonly IAsyncPolicy<HttpResponseMessage> _retryPolicy;

        private const string API_KEY_SETTING = "WeatherApiKey";
        private const string BASE_URL = "https://api.openweathermap.org/data/3.0/onecall";
        private const int MAX_FORECAST_DAYS = 7;

        public WeatherService(
            IHttpClientFactory httpClientFactory,
            IUnitOfWork unitOfWork,
            ILogger<WeatherService> logger)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

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

        public async Task<Result<WeatherDto>> GetWeatherAsync(
            double latitude,
            double longitude,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var apiKeyResult = await GetApiKeyAsync(cancellationToken);
                if (!apiKeyResult.IsSuccess || string.IsNullOrWhiteSpace(apiKeyResult.Data))
                {
                    return Result<WeatherDto>.Failure("Weather API key not configured");
                }

                var apiKey = apiKeyResult.Data;
                var client = _httpClientFactory.CreateClient();
                var requestUrl = $"{BASE_URL}?lat={latitude}&lon={longitude}&appid={apiKey}&units=metric&exclude=minutely,hourly";

                var response = await _retryPolicy.ExecuteAsync(async () =>
                    await client.GetAsync(requestUrl, cancellationToken));

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Weather API request failed with status {StatusCode}", response.StatusCode);
                    return Result<WeatherDto>.Failure($"Weather API request failed: {response.StatusCode}");
                }

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var weatherData = JsonSerializer.Deserialize<OpenWeatherResponse>(json);

                if (weatherData == null)
                {
                    return Result<WeatherDto>.Failure("Failed to parse weather data");
                }

                var weatherDto = MapToDto(weatherData);
                return Result<WeatherDto>.Success(weatherDto);
            }
            catch (TaskCanceledException)
            {
                _logger.LogWarning("Weather API request cancelled");
                return Result<WeatherDto>.Failure("Request cancelled");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Network error while fetching weather data");
                return Result<WeatherDto>.Failure($"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching weather data");
                return Result<WeatherDto>.Failure($"Failed to fetch weather: {ex.Message}");
            }
        }

        public async Task<Result<WeatherDto>> UpdateWeatherForLocationAsync(
         int locationId,
         CancellationToken cancellationToken = default)
        {
            try
            {
                // Get location details - extract from Result<T>
                var locationResult = await _unitOfWork.Locations.GetByIdAsync(locationId, cancellationToken);
                if (!locationResult.IsSuccess || locationResult.Data == null)
                {
                    return Result<WeatherDto>.Failure("Location not found");
                }

                var location = locationResult.Data;

                // Fetch weather for the location's coordinates
                var weatherResult = await GetWeatherAsync(
                    location.Coordinate.Latitude,
                    location.Coordinate.Longitude,
                    cancellationToken);

                if (!weatherResult.IsSuccess || weatherResult.Data == null)
                {
                    return Result<WeatherDto>.Failure(weatherResult.ErrorMessage ?? "Failed to fetch weather");
                }

                // Create weather entity
                var coordinate = new Coordinate(location.Coordinate.Latitude, location.Coordinate.Longitude);
                var weather = new Domain.Entities.Weather(
                    locationId,
                    coordinate,
                    weatherResult.Data.Timezone,
                    weatherResult.Data.TimezoneOffset);

                // Map forecast data
                var forecasts = new List<Domain.Entities.WeatherForecast>();

                // Add current weather as first forecast
                var currentForecast = new Domain.Entities.WeatherForecast(
                    weather.Id,
                    DateTime.Today,
                    weatherResult.Data.Sunrise,
                    weatherResult.Data.Sunset,
                    Temperature.FromCelsius(weatherResult.Data.Temperature),
                    Temperature.FromCelsius(weatherResult.Data.Temperature - 5), // Approximate min
                    Temperature.FromCelsius(weatherResult.Data.Temperature + 5), // Approximate max
                    weatherResult.Data.Description,
                    weatherResult.Data.Icon,
                    new WindInfo(weatherResult.Data.WindSpeed, weatherResult.Data.WindDirection, weatherResult.Data.WindGust),
                    weatherResult.Data.Humidity,
                    weatherResult.Data.Pressure,
                    weatherResult.Data.Clouds,
                    weatherResult.Data.UvIndex);

                forecasts.Add(currentForecast);
                weather.UpdateForecasts(forecasts);

                // Save to database - both methods return entities directly, not Result<T>
                var existingWeather = await _unitOfWork.Weather.GetByLocationIdAsync(locationId, cancellationToken);
                if (existingWeather != null)
                {
                    existingWeather.UpdateForecasts(forecasts);
                    _unitOfWork.Weather.Update(existingWeather);
                }
                else
                {
                    await _unitOfWork.Weather.AddAsync(weather, cancellationToken);
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return weatherResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating weather for location {LocationId}", locationId);
                return Result<WeatherDto>.Failure($"Failed to update weather: {ex.Message}");
            }
        }

        public async Task<Result<WeatherForecastDto>> GetForecastAsync(
            double latitude,
            double longitude,
            int days = 7,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var apiKeyResult = await GetApiKeyAsync(cancellationToken);
                if (!apiKeyResult.IsSuccess || string.IsNullOrWhiteSpace(apiKeyResult.Data))
                {
                    return Result<WeatherForecastDto>.Failure("Weather API key not configured");
                }

                var apiKey = apiKeyResult.Data;
                var client = _httpClientFactory.CreateClient();
                var requestUrl = $"{BASE_URL}?lat={latitude}&lon={longitude}&appid={apiKey}&units=metric&exclude=minutely,hourly";

                var response = await _retryPolicy.ExecuteAsync(async () =>
                    await client.GetAsync(requestUrl, cancellationToken));

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Weather API request failed with status {StatusCode}", response.StatusCode);
                    return Result<WeatherForecastDto>.Failure($"Weather API request failed: {response.StatusCode}");
                }

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var weatherData = JsonSerializer.Deserialize<OpenWeatherResponse>(json);

                if (weatherData == null)
                {
                    return Result<WeatherForecastDto>.Failure("Failed to parse weather data");
                }

                var forecastDto = MapToForecastDto(weatherData, days);
                return Result<WeatherForecastDto>.Success(forecastDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching weather forecast");
                return Result<WeatherForecastDto>.Failure($"Failed to fetch forecast: {ex.Message}");
            }
        }

        public async Task<Result<int>> UpdateAllWeatherAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // Get active locations - extract from Result<T>
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
                _logger.LogError(ex, "Error updating all weather data");
                return Result<int>.Failure($"Failed to update all weather: {ex.Message}");
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

        private WeatherDto MapToDto(OpenWeatherResponse response)
        {
            var current = response.Current;
            return new WeatherDto
            {
                Temperature = current.Temp,
                Description = current.Weather.FirstOrDefault()?.Description ?? string.Empty,
                Icon = current.Weather.FirstOrDefault()?.Icon ?? string.Empty,
                WindSpeed = current.WindSpeed,
                WindDirection = current.WindDeg,
                WindGust = current.WindGust,
                Humidity = current.Humidity,
                Pressure = current.Pressure,
                Clouds = current.Clouds,
                UvIndex = current.Uvi,
                Sunrise = DateTimeOffset.FromUnixTimeSeconds(current.Sunrise).DateTime,
                Sunset = DateTimeOffset.FromUnixTimeSeconds(current.Sunset).DateTime,
                Timezone = response.Timezone,
                TimezoneOffset = response.TimezoneOffset,
                LastUpdate = DateTime.UtcNow
            };
        }

        private WeatherForecastDto MapToForecastDto(OpenWeatherResponse response, int days)
        {
            var forecast = new WeatherForecastDto
            {
                Timezone = response.Timezone,
                TimezoneOffset = response.TimezoneOffset,
                LastUpdate = DateTime.UtcNow,
                DailyForecasts = new List<DailyForecastDto>()
            };

            for (int i = 0; i < Math.Min(response.Daily.Count, days); i++)
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

                forecast.DailyForecasts.Add(dailyDto);
            }

            return forecast;
        }
    }
}