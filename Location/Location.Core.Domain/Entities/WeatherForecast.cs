using System;
using Location.Core.Domain.Common;
using Location.Core.Domain.ValueObjects;

namespace Location.Core.Domain.Entities
{
    /// <summary>
    /// Individual weather forecast for a single day
    /// </summary>
    public class WeatherForecast : Entity
    {
        public int WeatherId { get; private set; }
        public DateTime Date { get; private set; }
        public DateTime Sunrise { get; private set; }
        public DateTime Sunset { get; private set; }
        public double Temperature { get; private set; }
        public double MinTemperature { get; private set; }
        public double MaxTemperature { get; private set; }
        public string Description { get; private set; } = string.Empty;
        public string Icon { get; private set; } = string.Empty;
        public WindInfo Wind { get; private set; } = null!;
        public int Humidity { get; private set; }
        public int Pressure { get; private set; }
        public int Clouds { get; private set; }
        public double UvIndex { get; private set; }
        public double? Precipitation { get; private set; }

        // Moon phase data
        public DateTime? MoonRise { get; private set; }
        public DateTime? MoonSet { get; private set; }
        public double MoonPhase { get; private set; }

        protected WeatherForecast() { } // For ORM

        public WeatherForecast(
            int weatherId,
            DateTime date,
            DateTime sunrise,
            DateTime sunset,
            double temperature,
            double minTemperature,
            double maxTemperature,
            string description,
            string icon,
            WindInfo wind,
            int humidity,
            int pressure,
            int clouds,
            double uvIndex)
        {
            WeatherId = weatherId;
            Date = date.Date;
            Sunrise = sunrise;
            Sunset = sunset;
            Temperature = temperature;
            MinTemperature = minTemperature;
            MaxTemperature = maxTemperature;
            Description = description ?? string.Empty;
            Icon = icon ?? string.Empty;
            Wind = wind ?? throw new ArgumentNullException(nameof(wind));
            Humidity = ValidatePercentage(humidity, nameof(humidity));
            Pressure = pressure;
            Clouds = ValidatePercentage(clouds, nameof(clouds));
            UvIndex = uvIndex;
        }

        public void SetMoonData(DateTime? moonRise, DateTime? moonSet, double moonPhase)
        {
            MoonRise = moonRise;
            MoonSet = moonSet;
            MoonPhase = Math.Max(0, Math.Min(1, moonPhase)); // Clamp between 0 and 1
        }

        public void SetPrecipitation(double precipitation)
        {
            Precipitation = Math.Max(0, precipitation);
        }

        private static int ValidatePercentage(int value, string paramName)
        {
            if (value < 0 || value > 100)
                throw new ArgumentOutOfRangeException(paramName, "Percentage must be between 0 and 100");
            return value;
        }

        /// <summary>
        /// Gets moon phase description
        /// </summary>
        public string GetMoonPhaseDescription()
        {
            return MoonPhase switch
            {
                < 0.03 => "New Moon",
                < 0.22 => "Waxing Crescent",
                < 0.28 => "First Quarter",
                < 0.47 => "Waxing Gibbous",
                < 0.53 => "Full Moon",
                < 0.72 => "Waning Gibbous",
                < 0.78 => "Last Quarter",
                < 0.97 => "Waning Crescent",
                _ => "New Moon"
            };
        }
    }
}