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
        /// <summary>
        /// Validates the specified <see cref="Weather"/> object and identifies any validation errors.
        /// </summary>
        /// <remarks>This method performs a series of checks to ensure the <paramref name="weather"/>
        /// object meets all required criteria: <list type="bullet"> <item><description>The <paramref name="weather"/>
        /// object must not be <see langword="null"/>.</description></item> <item><description>The <c>LocationId</c>
        /// property must be greater than 0.</description></item> <item><description>The <c>Coordinate</c> property must
        /// not be <see langword="null"/>.</description></item> <item><description>The <c>Timezone</c> property must not
        /// be <see langword="null"/> or whitespace.</description></item> <item><description>The number of daily
        /// forecasts must not exceed the maximum allowed days.</description></item> </list> Additionally, each forecast
        /// in the <c>Forecasts</c> collection is validated individually.</remarks>
        /// <param name="weather">The <see cref="Weather"/> object to validate. Cannot be <see langword="null"/>.</param>
        /// <param name="errors">When this method returns, contains a list of validation error messages, if any.  If the <paramref
        /// name="weather"/> object is valid, the list will be empty.</param>
        /// <returns><see langword="true"/> if the <paramref name="weather"/> object is valid; otherwise, <see
        /// langword="false"/>.</returns>
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
        /// <summary>
        /// Validates the specified weather forecast and collects any validation errors.
        /// </summary>
        /// <remarks>This method checks the weather forecast for various constraints, including: <list
        /// type="bullet"> <item><description>Temperature must be within the valid range defined by
        /// <c>MinTemperature</c> and <c>MaxTemperature</c>.</description></item> <item><description>Minimum temperature
        /// must not exceed maximum temperature.</description></item> <item><description>Humidity must be between 0 and
        /// 100 percent.</description></item> <item><description>Cloud coverage must be between 0 and 100
        /// percent.</description></item> <item><description>UV index must be between 0 and 15.</description></item>
        /// <item><description>Moon phase must be between 0 and 1.</description></item> </list> If any of these
        /// constraints are violated, a descriptive error message is added to the <paramref name="errors"/>
        /// list.</remarks>
        /// <param name="forecast">The weather forecast to validate. Must not be null.</param>
        /// <param name="errors">A list to which validation error messages will be added. Must not be null.</param>
        private static void ValidateForecast(WeatherForecast forecast, List<string> errors)
        {
            if (forecast.Temperature < MinTemperature || forecast.Temperature > MaxTemperature)
            {
                errors.Add($"Invalid temperature for {forecast.Date:yyyy-MM-dd}");
            }

            if (forecast.MinTemperature > forecast.MaxTemperature)
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
        /// <summary>
        /// Determines whether the specified weather data is considered stale based on the given maximum age.
        /// </summary>
        /// <param name="weather">The weather data to evaluate. Must not be <see langword="null"/>.</param>
        /// <param name="maxAge">The maximum allowable age for the weather data. Must be a positive <see cref="TimeSpan"/>.</param>
        /// <returns><see langword="true"/> if the weather data is older than the specified maximum age; otherwise, <see
        /// langword="false"/>.</returns>
        public static bool IsStale(Weather weather, TimeSpan maxAge)
        {
            return DateTime.UtcNow - weather.LastUpdate > maxAge;
        }
    }
}