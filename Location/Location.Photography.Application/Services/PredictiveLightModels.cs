using Location.Core.Application.Weather.DTOs;
using Location.Photography.Domain.Models;

namespace Location.Photography.Application.Services
{
    #region Service Interface
    public interface IPredictiveLightService
    {
        Task<WeatherImpactAnalysis> AnalyzeWeatherImpactAsync(WeatherImpactAnalysisRequest request, CancellationToken cancellationToken = default);
        Task<List<HourlyLightPrediction>> GenerateHourlyPredictionsAsync(PredictiveLightRequest request, CancellationToken cancellationToken = default);
        Task<PredictiveLightRecommendation> GenerateRecommendationAsync(PredictiveLightRequest request, CancellationToken cancellationToken = default);
        Task CalibrateWithActualReadingAsync(LightMeterCalibrationRequest request, CancellationToken cancellationToken = default);
    }
    #endregion

    #region Request Models
    public class WeatherImpactAnalysisRequest
    {
        public WeatherForecastDto WeatherForecast { get; set; } = new();
        public EnhancedSunTimes SunTimes { get; set; } = new();
        public MoonPhaseData MoonData { get; set; } = new();
    }

    public class PredictiveLightRequest
    {
        public int LocationId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime TargetDate { get; set; }
        public WeatherImpactAnalysis WeatherImpact { get; set; } = new();
        public EnhancedSunTimes SunTimes { get; set; } = new();
        public MoonPhaseData MoonPhase { get; set; } = new();
        public DateTime? LastCalibrationReading { get; set; }
        public int PredictionWindowHours { get; set; } = 24;
    }

    public class LightMeterCalibrationRequest
    {
        public int LocationId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime DateTime { get; set; }
        public double ActualEV { get; set; }
        public WeatherConditions? WeatherConditions { get; set; }
    }

    public class ShootingAlertRequest
    {
        public int LocationId { get; set; }
        public DateTime AlertTime { get; set; }
        public DateTime ShootingWindowStart { get; set; }
        public DateTime ShootingWindowEnd { get; set; }
        public LightQuality LightQuality { get; set; }
        public string? RecommendedSettings { get; set; }
        public string Message { get; set; } = string.Empty;
    }
    #endregion

    #region Weather Models
    public class WeatherImpactAnalysis
    {
        public WeatherConditions? CurrentConditions { get; set; }
        public List<HourlyWeatherImpact> HourlyImpacts { get; set; } = new();
        public double OverallLightReductionFactor { get; set; } = 1.0;
        public string Summary { get; set; } = string.Empty;
        public List<WeatherAlert> Alerts { get; set; } = new();
    }

    public class HourlyWeatherImpact
    {
        public DateTime Hour { get; set; }
        public double LightReductionFactor { get; set; } = 1.0;
        public double ColorTemperatureShift { get; set; } = 0;
        public double ContrastReduction { get; set; } = 0;
        public LightQuality PredictedQuality { get; set; } = LightQuality.Unknown;
        public string Reasoning { get; set; } = string.Empty;
    }

    public class WeatherConditions
    {
        public double CloudCover { get; set; }
        public double Precipitation { get; set; }
        public double Humidity { get; set; }
        public double Visibility { get; set; }
        public int AirQualityIndex { get; set; }
        public double WindSpeed { get; set; }
        public string Description { get; set; } = string.Empty;

        public double UvIndex { get; set; }
    }

    public class WeatherAlert
    {
        public AlertType Type { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime ValidFrom { get; set; }
        public DateTime ValidTo { get; set; }
        public AlertSeverity Severity { get; set; }
    }
    #endregion

    #region Light Prediction Models
    public class PredictiveLightRecommendation
    {
        public DateTime GeneratedAt { get; set; }
        public OptimalShootingWindow BestTimeWindow { get; set; } = new();
        public List<OptimalShootingWindow> AlternativeWindows { get; set; } = new();
        public string OverallRecommendation { get; set; } = string.Empty;
        public List<string> KeyInsights { get; set; } = new();
        public double CalibrationAccuracy { get; set; }
        public bool RequiresRecalibration { get; set; }
    }

    public class OptimalShootingWindow
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public LightQuality LightQuality { get; set; }
        public double OptimalityScore { get; set; }
        public string Description { get; set; } = string.Empty;
        public List<string> RecommendedFor { get; set; } = new();
        public HourlyLightPrediction? RecommendedExposure { get; set; }
        public List<string> Warnings { get; set; } = new();
    }
    #endregion

    #region Enums
    public enum AlertType
    {
        Weather,
        Light,
        Shooting,
        Calibration
    }

    public enum AlertSeverity
    {
        Info,
        Warning,
        Critical
    }
    #endregion
}