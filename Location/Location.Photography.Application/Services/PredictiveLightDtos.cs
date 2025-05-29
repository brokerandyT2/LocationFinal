// Location.Photography.Application/Services/PredictiveLightDtos.cs
using Location.Core.Application.Weather.DTOs;
using Location.Photography.Domain.Models;
using System;
using System.Collections.Generic;

namespace Location.Photography.Application.Services
{
  /*  public class WeatherImpactAnalysisRequest
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
        public ExposureTriangle? RecommendedSettings { get; set; }
        public string Message { get; set; } = string.Empty;
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
    } */
}