// Location.Photography.Application/Services/PredictiveLightModels.cs
using Location.Core.Application.Weather.DTOs;
using Location.Photography.Domain.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

// Note: This file uses LightQuality and ShadowIntensity from Location.Photography.Domain.Models
// to avoid namespace conflicts

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
        public double OverallLightReductionFactor { get; set; } = 1.0; // 1.0 = no reduction, 0.5 = 50% reduction
        public string Summary { get; set; } = string.Empty;
        public List<WeatherAlert> Alerts { get; set; } = new();
    }

    public class HourlyWeatherImpact
    {
        public DateTime Hour { get; set; }
        public double LightReductionFactor { get; set; } = 1.0;
        public double ColorTemperatureShift { get; set; } = 0; // Kelvin shift from clear sky
        public double ContrastReduction { get; set; } = 0; // 0-1 scale
        public LightQuality PredictedQuality { get; set; } = LightQuality.Unknown;
        public string Reasoning { get; set; } = string.Empty;
    }

    public class WeatherConditions
    {
        public double CloudCover { get; set; } // 0-1
        public double Precipitation { get; set; } // mm/hour
        public double Humidity { get; set; } // 0-1
        public double Visibility { get; set; } // km
        public int AirQualityIndex { get; set; } // 0-500
        public double WindSpeed { get; set; } // m/s
        public string Description { get; set; } = string.Empty;
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
    public class HourlyLightPrediction
    {
        public DateTime DateTime { get; set; }
        public double PredictedEV { get; set; }
        public double EVConfidenceMargin { get; set; } // ±margin
        public double ConfidenceLevel { get; set; } // 0-1
        public string ConfidenceReason { get; set; } = string.Empty;
        public ExposureTriangle SuggestedSettings { get; set; } = new();
        public LightCharacteristics LightQuality { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
        public bool IsOptimalForPhotography { get; set; }
        public SunPosition SunPosition { get; set; } = new();
    }

    public class PredictiveLightRecommendation
    {
        public DateTime GeneratedAt { get; set; }
        public OptimalShootingWindow BestTimeWindow { get; set; } = new();
        public List<OptimalShootingWindow> AlternativeWindows { get; set; } = new();
        public string OverallRecommendation { get; set; } = string.Empty;
        public List<string> KeyInsights { get; set; } = new();
        public double CalibrationAccuracy { get; set; } // 0-1, only if calibrated
        public bool RequiresRecalibration { get; set; }
    }

    public class OptimalShootingWindow
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public LightQuality LightQuality { get; set; }
        public double OptimalityScore { get; set; } // 0-1
        public string Description { get; set; } = string.Empty;
        public List<string> RecommendedFor { get; set; } = new(); // "Portraits", "Landscapes", etc.
        public HourlyLightPrediction? RecommendedExposure { get; set; }
        public List<string> Warnings { get; set; } = new();
    }
    #endregion

    #region Supporting Models
    public class ExposureTriangle
    {
        public string Aperture { get; set; } = string.Empty; // "f/4"
        public string ShutterSpeed { get; set; } = string.Empty; // "1/125s"
        public string ISO { get; set; } = string.Empty; // "ISO 100"
        public string FormattedSettings => $"{Aperture}, {ShutterSpeed}, {ISO}";
    }

    public class LightCharacteristics
    {
        public double ColorTemperature { get; set; } // Kelvin
        public double SoftnessFactor { get; set; } // 0-1, 1 = very soft
        public ShadowIntensity ShadowHarshness { get; set; } = ShadowIntensity.Medium;
        public string OptimalFor { get; set; } = string.Empty; // "Portraits", "Landscapes"
        public double DirectionalityFactor { get; set; } // 0-1, 1 = very directional
    }

    public class SunPosition
    {
        public double Azimuth { get; set; } // 0-360 degrees
        public double Elevation { get; set; } // -90 to 90 degrees
        public double Distance { get; set; } = 1.0; // AU (for seasonal variations)
        public bool IsAboveHorizon { get; set; }
    }

    public class EnhancedSunTimes
    {
        public DateTime Sunrise { get; set; }
        public DateTime Sunset { get; set; }
        public DateTime SolarNoon { get; set; }
        public DateTime CivilDawn { get; set; }
        public DateTime CivilDusk { get; set; }
        public DateTime NauticalDawn { get; set; }
        public DateTime NauticalDusk { get; set; }
        public DateTime AstronomicalDawn { get; set; }
        public DateTime AstronomicalDusk { get; set; }

        // Enhanced calculations
        public DateTime BlueHourMorning { get; set; }
        public DateTime BlueHourEvening { get; set; }
        public DateTime GoldenHourMorningStart { get; set; }
        public DateTime GoldenHourMorningEnd { get; set; }
        public DateTime GoldenHourEveningStart { get; set; }
        public DateTime GoldenHourEveningEnd { get; set; }

        public TimeZoneInfo TimeZone { get; set; } = TimeZoneInfo.Local;
        public bool IsDaylightSavingTime { get; set; }
        public TimeSpan UtcOffset { get; set; }
        public TimeSpan SolarTimeOffset { get; set; } // Difference between solar noon and clock noon
    }
    #endregion

    #region Enums
    // Note: LightQuality and ShadowIntensity are already defined in Location.Photography.Domain.Models
    // Using those existing enums instead of duplicating here

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