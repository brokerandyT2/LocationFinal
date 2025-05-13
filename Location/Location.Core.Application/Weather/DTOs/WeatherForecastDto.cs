using System;
using System.Collections.Generic;

namespace Location.Core.Application.Weather.DTOs
{
    /// <summary>
    /// Data transfer object for weather forecast
    /// </summary>
    public class WeatherForecastDto
    {
        public int WeatherId { get; set; }
        public DateTime LastUpdate { get; set; }
        public string Timezone { get; set; } = string.Empty;
        public int TimezoneOffset { get; set; }
        public List<DailyForecastDto> DailyForecasts { get; set; } = new List<DailyForecastDto>();
    }

    /// <summary>
    /// Data transfer object for daily forecast
    /// </summary>
    public class DailyForecastDto
    {
        public DateTime Date { get; set; }
        public DateTime Sunrise { get; set; }
        public DateTime Sunset { get; set; }
        public double Temperature { get; set; }
        public double MinTemperature { get; set; }
        public double MaxTemperature { get; set; }
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
        public DateTime? MoonRise { get; set; }
        public DateTime? MoonSet { get; set; }
        public double MoonPhase { get; set; }
    }
}