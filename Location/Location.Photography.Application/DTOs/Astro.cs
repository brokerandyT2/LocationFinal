namespace Location.Photography.Application.DTOs
{
    public class AstroHourlyPredictionDto
    {
        public DateTime Hour { get; set; }
        public string TimeDisplay { get; set; } = string.Empty;
        public string SolarEvent { get; set; } = string.Empty;
        public string SolarEventsDisplay { get; set; } = string.Empty;
        public double QualityScore { get; set; }
        public string QualityDisplay { get; set; } = string.Empty;
        public string QualityDescription { get; set; } = string.Empty;
        public List<AstroEventDto> AstroEvents { get; set; } = new();
        public WeatherDto Weather { get; set; } = new();
    }

    public class AstroEventDto
    {
        public string TargetName { get; set; } = string.Empty;
        public string Visibility { get; set; } = string.Empty;
        public string RecommendedEquipment { get; set; } = string.Empty;
        public string CameraSettings { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
    }

    public class WeatherDto
    {
        public double CloudCover { get; set; }
        public double Humidity { get; set; }
        public double WindSpeed { get; set; }
        public double Visibility { get; set; }
        public string Description { get; set; } = string.Empty;
        public string WeatherDisplay { get; set; } = string.Empty;
        public string WeatherSuitability { get; set; } = string.Empty;
    }
}
