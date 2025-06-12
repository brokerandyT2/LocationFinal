// Location.Photography.Domain/Models/EnhancedSunDomainModels.cs
namespace Location.Photography.Domain.Models
{
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

    public enum LightQuality
    {
        Unknown,
        Harsh,
        Soft,
        GoldenHour,
        BlueHour,
        Overcast,
        Dramatic, Night,
        Flat, Direct
        
    }

    // Predictive light models
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
        public SunPositionDto SunPosition { get; set; } = new();
        public bool IsMoonVisible { get; set; } = false;
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
        public double Distance { get; set; } = 1.0; // AU (for seasonal variations)
        public bool IsAboveHorizon { get; set; }
    }

    public enum ShadowIntensity
    {
        None,
        Soft,
        Medium,
        Hard,
        VeryHard
    }
}