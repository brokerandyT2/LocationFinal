using System;

namespace Location.Core.Application.Weather.DTOs
{
    /// <summary>
    /// Data transfer object for weather information
    /// </summary>
    public class WeatherDto
    {
        public int Id { get; set; }
        public int LocationId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Timezone { get; set; } = string.Empty;
        public int TimezoneOffset { get; set; }
        public DateTime LastUpdate { get; set; }

        // Current conditions
        public double Temperature { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public double WindSpeed { get; set; }
        public double WindDirection { get; set; }
        public double? WindGust { get; set; }
        public int Humidity { get; set; }
        public int Pressure { get; set; }
        public int Clouds { get; set; }
        public double UvIndex { get; set; }
        public double? Precipitation { get; set; }

        // Sun data
        public DateTime Sunrise { get; set; }
        public DateTime Sunset { get; set; }

        // Moon data
        public DateTime? MoonRise { get; set; }
        public DateTime? MoonSet { get; set; }
        public double MoonPhase { get; set; }
        public double MinimumTemp { get; set; }
        public double MaximumTemp { get; set; }
    }
}