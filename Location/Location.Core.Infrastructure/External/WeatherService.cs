using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Interfaces.Persistence;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Services;
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
        private readonly ISettingRepository _settingRepository;
        private readonly ILogger<WeatherService> _logger;
        private readonly IAsyncPolicy<HttpResponseMessage> _retryPolicy;

        private const string API_KEY_SETTING = "WeatherApiKey";
        private const string BASE_URL = "https://api.openweathermap.org/data/3.0/onecall";
        private const int MAX_FORECAST_DAYS = 7;

        public WeatherService(
            IHttpClientFactory httpClientFactory,
            ISettingRepository settingRepository,
            ILogger<WeatherService> logger)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _settingRepository = settingRepository ?? throw new ArgumentNullException(nameof(settingRepository));
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

        public async Task<Result<Domain.Entities.Weather>> GetWeatherAsync(
            double latitude,
            double longitude,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var apiKey = await GetApiKeyAsync(cancellationToken);
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    return Result<Domain.Entities.Weather>.Failure("Weather API key not configured");
                }

                var client = _httpClientFactory.CreateClient();
                var requestUrl = $"{BASE_URL}?lat={latitude}&lon={longitude}&appid={apiKey}&units=metric&exclude=minutely,hourly";

                var response = await _retryPolicy.ExecuteAsync(async () =>
                    await client.GetAsync(requestUrl, cancellationToken));

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Weather API request failed with status {StatusCode}", response.StatusCode);
                    return Result<Domain.Entities.Weather>.Failure($"Weather API request failed: {response.StatusCode}");
                }

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var weatherData = JsonSerializer.Deserialize<OpenWeatherResponse>(json);

                if (weatherData == null)
                {
                    return Result<Domain.Entities.Weather>.Failure("Failed to parse weather data");
                }

                var weather = MapToDomain(weatherData, latitude, longitude);
                return Result<Domain.Entities.Weather>.Success(weather);
            }
            catch (TaskCanceledException)
            {
                _logger.LogWarning("Weather API request cancelled");
                return Result<Domain.Entities.Weather>.Failure("Request cancelled");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Network error while fetching weather data");
                return Result<Domain.Entities.Weather>.Failure($"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching weather data");
                return Result<Domain.Entities.Weather>.Failure($"Failed to fetch weather: {ex.Message}");
            }
        }

        public async Task<Result<bool>> UpdateWeatherForLocationAsync(
            int locationId,
            double latitude,
            double longitude,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var weatherResult = await GetWeatherAsync(latitude, longitude, cancellationToken);

                if (!weatherResult.IsSuccess)
                {
                    return Result<bool>.Failure(weatherResult.ErrorMessage);
                }

                // Note: In a complete implementation, this would save to the repository
                // For now, we're just fetching the data
                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating weather for location {LocationId}", locationId);
                return Result<bool>.Failure($"Failed to update weather: {ex.Message}");
            }
        }

        private async Task<string> GetApiKeyAsync(CancellationToken cancellationToken)
        {
            var result = await _settingRepository.GetByKeyAsync(API_KEY_SETTING, cancellationToken);

            if (result.IsSuccess && result.Data != null)
            {
                return result.Data.Value;
            }

            _logger.LogWarning("Weather API key not found in settings");
            return string.Empty;
        }

        private Domain.Entities.Weather MapToDomain(OpenWeatherResponse response, double latitude, double longitude)
        {
            var coordinate = new Coordinate(latitude, longitude);

            var weather = new Domain.Entities.Weather(
                0, // LocationId will be set later
                coordinate,
                response.Timezone,
                response.TimezoneOffset);

            // Map daily forecasts
            var forecasts = new List<Domain.Entities.WeatherForecast>();

            for (int i = 0; i < Math.Min(response.Daily.Count, MAX_FORECAST_DAYS); i++)
            {
                var daily = response.Daily[i];
                var forecast = CreateWeatherForecast(weather.Id, daily);
                forecasts.Add(forecast);
            }

            weather.UpdateForecasts(forecasts);
            return weather;
        }

        private Domain.Entities.WeatherForecast CreateWeatherForecast(int weatherId, DailyForecast daily)
        {
            var date = DateTimeOffset.FromUnixTimeSeconds(daily.Dt).DateTime;
            var sunrise = DateTimeOffset.FromUnixTimeSeconds(daily.Sunrise).DateTime;
            var sunset = DateTimeOffset.FromUnixTimeSeconds(daily.Sunset).DateTime;

            var temperature = Temperature.FromCelsius(daily.Temp.Day);
            var minTemp = Temperature.FromCelsius(daily.Temp.Min);
            var maxTemp = Temperature.FromCelsius(daily.Temp.Max);

            var wind = new WindInfo(daily.WindSpeed, daily.WindDeg, daily.WindGust);

            var forecast = new Domain.Entities.WeatherForecast(
                weatherId,
                date,
                sunrise,
                sunset,
                temperature,
                minTemp,
                maxTemp,
                daily.Weather.FirstOrDefault()?.Description ?? string.Empty,
                daily.Weather.FirstOrDefault()?.Icon ?? string.Empty,
                wind,
                daily.Humidity,
                daily.Pressure,
                daily.Clouds,
                daily.Uvi);

            // Set additional data
            if (daily.MoonRise > 0)
            {
                var moonRise = DateTimeOffset.FromUnixTimeSeconds(daily.MoonRise).DateTime;
                var moonSet = DateTimeOffset.FromUnixTimeSeconds(daily.MoonSet).DateTime;
                forecast.SetMoonData(moonRise, moonSet, daily.MoonPhase);
            }

            if (daily.Pop > 0)
            {
                forecast.SetPrecipitation(daily.Pop);
            }

            return forecast;
        }
    }
}