namespace Location.Core.Application.Weather.DTOs
{
    /// <summary>
    /// Data transfer object for hourly weather forecast
    /// </summary>
    public class HourlyWeatherForecastDto
    {
        public int WeatherId { get; set; }
        public DateTime LastUpdate { get; set; }
        public string Timezone { get; set; } = string.Empty;
        public int TimezoneOffset { get; set; }
        public List<HourlyForecastDto> HourlyForecasts { get; set; } = new List<HourlyForecastDto>();
    }

    /// <summary>
    /// Data transfer object for individual hourly forecast
    /// </summary>
    public class HourlyForecastDto
    {
        public DateTime DateTime { get; set; }
        public double Temperature { get; set; }
        public double FeelsLike { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public double WindSpeed { get; set; }
        public double WindDirection { get; set; }
        public double? WindGust { get; set; }
        public int Humidity { get; set; }
        public int Pressure { get; set; }
        public int Clouds { get; set; }
        public double UvIndex { get; set; }
        public double ProbabilityOfPrecipitation { get; set; }
        public int Visibility { get; set; }
        public double DewPoint { get; set; }
    }
}