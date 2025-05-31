using System;
using Location.Core.Domain.Common;
using Location.Core.Domain.ValueObjects;

namespace Location.Core.Domain.Entities
{
    /// <summary>
    /// Individual weather forecast for a single hour
    /// </summary>
    public class HourlyForecast : Entity
    {
        public int WeatherId { get; private set; }
        public DateTime DateTime { get; private set; }
        public double Temperature { get; private set; }
        public double FeelsLike { get; private set; }
        public string Description { get; private set; } = string.Empty;
        public string Icon { get; private set; } = string.Empty;
        public WindInfo Wind { get; private set; } = null!;
        public int Humidity { get; private set; }
        public int Pressure { get; private set; }
        public int Clouds { get; private set; }
        public double UvIndex { get; private set; }
        public double ProbabilityOfPrecipitation { get; private set; }
        public int Visibility { get; private set; }
        public double DewPoint { get; private set; }

        protected HourlyForecast() { } // For ORM

        public HourlyForecast(
            int weatherId,
            DateTime dateTime,
            double temperature,
            double feelsLike,
            string description,
            string icon,
            WindInfo wind,
            int humidity,
            int pressure,
            int clouds,
            double uvIndex,
            double probabilityOfPrecipitation,
            int visibility,
            double dewPoint)
        {
            WeatherId = weatherId;
            DateTime = dateTime;
            Temperature = temperature;
            FeelsLike = feelsLike;
            Description = description ?? string.Empty;
            Icon = icon ?? string.Empty;
            Wind = wind ?? throw new ArgumentNullException(nameof(wind));
            Humidity = ValidatePercentage(humidity, nameof(humidity));
            Pressure = pressure;
            Clouds = ValidatePercentage(clouds, nameof(clouds));
            UvIndex = Math.Max(0, uvIndex);
            ProbabilityOfPrecipitation = ValidateProbability(probabilityOfPrecipitation, nameof(probabilityOfPrecipitation));
            Visibility = Math.Max(0, visibility);
            DewPoint = dewPoint;
        }

        private static int ValidatePercentage(int value, string paramName)
        {
            if (value < 0 || value > 100)
                throw new ArgumentOutOfRangeException(paramName, "Percentage must be between 0 and 100");
            return value;
        }

        private static double ValidateProbability(double value, string paramName)
        {
            if (value < 0 || value > 1)
                throw new ArgumentOutOfRangeException(paramName, "Probability must be between 0 and 1");
            return value;
        }
    }
}