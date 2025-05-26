using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Services;
using Location.Core.Application.Weather.DTOs;
using Location.Core.Domain.ValueObjects;
using Location.Core.Infrastructure.External.Models;
using Location.Core.Infrastructure.Services;
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

                // Fetch weather data from API
                var forecastResult = await GetForecastFromApiAsync(
                    location.Coordinate.Latitude,
                    location.Coordinate.Longitude,
                    cancellationToken);

                if (!forecastResult.IsSuccess || forecastResult.Data == null)
                {
                    return Result<WeatherDto>.Failure(forecastResult.ErrorMessage ?? "Failed to fetch weather");
                }

                var apiData = forecastResult.Data;

                // Create or update weather entity
                var coordinate = new Coordinate(location.Coordinate.Latitude, location.Coordinate.Longitude);

                var existingWeather = await _unitOfWork.Weather.GetByLocationIdAsync(locationId, cancellationToken);
                Domain.Entities.Weather weather;

                if (existingWeather != null)
                {
                    weather = existingWeather;
                }
                else
                {
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
                        new WindInfo(dailyForecast.WindSpeed, dailyForecast.WindDirection, dailyForecast.WindGust), // Store raw wind direction
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

                // Update weather with new forecasts
                weather.UpdateForecasts(forecasts);

                // Save to database
                if (existingWeather != null)
                {
                    _unitOfWork.Weather.Update(existingWeather);
                }
                else
                {
                    await _unitOfWork.Weather.AddAsync(weather, cancellationToken);
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Map current weather data to DTO and return (with proper async wind direction handling)
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
            return await GetForecastFromApiAsync(latitude, longitude, cancellationToken);
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

        private async Task<Result<WeatherForecastDto>> GetForecastFromApiAsync(
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
                    return Result<WeatherForecastDto>.Failure("Weather API key not configured");
                }

                var apiKey = apiKeyResult.Data;
                var client = _httpClientFactory.CreateClient();
                var tempS = tempScale.Data?.Value == "F" ? "imperial" : "metric";
                var requestUrl = $"{BASE_URL}?lat={latitude}&lon={longitude}&appid={apiKey}&units={tempS}&exclude=minutely,hourly";

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

                var forecastDto = MapToForecastDto(weatherData, 7);
                return Result<WeatherForecastDto>.Success(forecastDto);
            }
            catch (Exception ex)
            {
                var domainException = _exceptionMapper.MapToWeatherDomainException(ex, "GetForecastFromApi");
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

        private async Task<WeatherDto> MapToWeatherDtoAsync(Domain.Entities.Weather weather, WeatherForecastDto apiData, CancellationToken cancellationToken)
        {
            var currentForecast = weather.GetCurrentForecast();
            var currentApiData = apiData.DailyForecasts.FirstOrDefault();

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
                    WindDirection = daily.WindDeg, // Store raw wind direction from API
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

        Task<Result<WeatherDto>> IWeatherService.GetWeatherAsync(double latitude, double longitude, CancellationToken cancellationToken)
        {
            throw new NotImplementedException("Use UpdateWeatherForLocationAsync for offline-first persistence");
        }
    }
}