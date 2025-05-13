using System;
using System.Collections.Generic;
using System.Linq;
using Location.Core.Domain.Entities;

namespace Location.Core.Domain.Rules
{
    /// <summary>
    /// Business rules for weather validation
    /// </summary>
    public static class WeatherValidationRules
    {
        private const int MaxForecastDays = 7;
        private const double MinTemperature = -273.15; // Absolute zero in Celsius
        private const double MaxTemperature = 70; // Reasonable max temperature in Celsius

        public static bool IsValid(Weather weather, out List<string> errors)
        {
            errors = new List<string>();

            if (weather == null)
            {
                errors.Add("Weather cannot be null");
                return false;
            }

            if (weather.LocationId <= 0)
            {
                errors.Add("Weather must be associated with a valid location");
            }

            if (weather.Coordinate == null)
            {
                errors.Add("Weather coordinates are required");
            }

            if (string.IsNullOrWhiteSpace(weather.Timezone))
            {
                errors.Add("Weather timezone is required");
            }

            if (weather.Forecasts.Count > MaxForecastDays)
            {
                errors.Add($"Weather cannot have more than {MaxForecastDays} daily forecasts");
            }

            // Validate each forecast
            foreach (var forecast in weather.Forecasts)
            {
                ValidateForecast(forecast, errors);
            }

            return errors.Count == 0;
        }

        private static void ValidateForecast(WeatherForecast forecast, List<string> errors)
        {
            if (forecast.Temperature?.Celsius < MinTemperature || forecast.Temperature?.Celsius > MaxTemperature)
            {
                errors.Add($"Invalid temperature for {forecast.Date:yyyy-MM-dd}");
            }

            if (forecast.MinTemperature?.Celsius > forecast.MaxTemperature?.Celsius)
            {
                errors.Add($"Min temperature cannot exceed max temperature for {forecast.Date:yyyy-MM-dd}");
            }

            if (forecast.Humidity < 0 || forecast.Humidity > 100)
            {
                errors.Add($"Invalid humidity percentage for {forecast.Date:yyyy-MM-dd}");
            }

            if (forecast.Clouds < 0 || forecast.Clouds > 100)
            {
                errors.Add($"Invalid cloud coverage percentage for {forecast.Date:yyyy-MM-dd}");
            }

            if (forecast.UvIndex < 0 || forecast.UvIndex > 15)
            {
                errors.Add($"Invalid UV index for {forecast.Date:yyyy-MM-dd}");
            }

            if (forecast.MoonPhase < 0 || forecast.MoonPhase > 1)
            {
                errors.Add($"Invalid moon phase for {forecast.Date:yyyy-MM-dd}");
            }
        }

        public static bool IsStale(Weather weather, TimeSpan maxAge)
        {
            return DateTime.UtcNow - weather.LastUpdate > maxAge;
        }
    }
}