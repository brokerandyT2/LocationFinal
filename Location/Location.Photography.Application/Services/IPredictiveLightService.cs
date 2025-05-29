using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Location.Core.Application.Weather.DTOs;
using Location.Photography.Domain.Models;

namespace Location.Photography.Application.Services
{
    public interface IPredictiveLightService
    {
        Task<WeatherImpactAnalysis> AnalyzeWeatherImpactAsync(WeatherImpactAnalysisRequest request, CancellationToken cancellationToken = default);

        Task<List<HourlyLightPrediction>> GenerateHourlyPredictionsAsync(PredictiveLightRequest request, CancellationToken cancellationToken = default);

        Task<PredictiveLightRecommendation> GenerateRecommendationAsync(PredictiveLightRequest request, CancellationToken cancellationToken = default);

        Task CalibrateWithActualReadingAsync(LightMeterCalibrationRequest request, CancellationToken cancellationToken = default);
    }

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
        public double Distance { get; set; } // AU (for seasonal variations)
        public bool IsAboveHorizon { get; set; }
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

    public enum LightQuality
    {
        Unknown,
        Harsh,
        Soft,
        GoldenHour,
        BlueHour,
        Overcast,
        Dramatic,
        Flat
    }

    public enum ShadowIntensity
    {
        None,
        Soft,
        Medium,
        Hard,
        VeryHard
    }

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

    // Enhanced Sun Times with precise calculations
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

    // Moon integration
    public class MoonPhaseData
    {
        public DateTime Date { get; set; }
        public double Phase { get; set; } // 0-1, 0 = new moon, 0.5 = full moon
        public string PhaseName { get; set; } = string.Empty; // "New Moon", "Waxing Crescent", etc.
        public double IlluminationPercentage { get; set; } // 0-100
        public DateTime? MoonRise { get; set; }
        public DateTime? MoonSet { get; set; }
        public MoonPosition Position { get; set; } = new();
        public double Brightness { get; set; } // Magnitude
    }

    public class MoonPosition
    {
        public double Azimuth { get; set; }
        public double Elevation { get; set; }
        public double Distance { get; set; } // km
        public bool IsAboveHorizon { get; set; }
    }

    // Sun path for interactive visualization
    public class SunPathPoint
    {
        public DateTime Time { get; set; }
        public double Azimuth { get; set; }
        public double Elevation { get; set; }
        public bool IsVisible { get; set; } // Above horizon
    }

    // Shadow calculations
    public class ShadowCalculationResult
    {
        public double ShadowLength { get; set; } // meters
        public double ShadowDirection { get; set; } // degrees from north
        public double ObjectHeight { get; set; } // meters
        public DateTime CalculationTime { get; set; }
        public TerrainType Terrain { get; set; }
        public List<ShadowTimePoint> ShadowProgression { get; set; } = new();
    }

    public class ShadowTimePoint
    {
        public DateTime Time { get; set; }
        public double Length { get; set; }
        public double Direction { get; set; }
    }

    public enum TerrainType
    {
        Flat,
        Urban,
        Forest,
        Mountain,
        Beach
    }

    // Optimal shooting times
    public class OptimalShootingTime
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public LightQuality LightQuality { get; set; }
        public double QualityScore { get; set; } // 0-1
        public string Description { get; set; } = string.Empty;
        public List<string> IdealFor { get; set; } = new();
        public HourlyLightPrediction? RecommendedExposure { get; set; }
    }
}