using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Location.Photography.ViewModels
{
    public class AstroHourlyPredictionDisplayModel : INotifyPropertyChanged
    {
        // Header Properties
        public string TimeDisplay { get; set; } = string.Empty;
        public string SolarEventsDisplay { get; set; } = string.Empty;
        public double QualityScore { get; set; }
        public bool IsOptimalTime { get; set; }

        // Event Collections
        public List<AstroEventDisplayModel> AstroEvents { get; set; } = new();
        public List<SolarEventDisplayModel> SolarEvents { get; set; } = new();

        // Overall Assessment
        public string OverallQuality { get; set; } = string.Empty;
        public string ConfidenceDisplay { get; set; } = string.Empty;
        public string Recommendations { get; set; } = string.Empty;

        // Original Domain Data
        public DateTime Hour { get; set; }
        public AstroHourlyPrediction DomainModel { get; set; } = new();

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }

    public class AstroEventDisplayModel
    {
        // Event Identity
        public string TargetName { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public double QualityRank { get; set; }

        // Position Information
        public double Azimuth { get; set; }
        public double Altitude { get; set; }
        public string AzimuthDisplay => $"{Azimuth:F1}°";
        public string AltitudeDisplay => $"{Altitude:F1}°";
        public bool IsVisible => Altitude > 0;

        // Timing Information
        public DateTime? RiseTime { get; set; }
        public DateTime? SetTime { get; set; }
        public DateTime? OptimalTime { get; set; }
        public string RiseTimeDisplay => RiseTime?.ToString("HH:mm") ?? "N/A";
        public string SetTimeDisplay => SetTime?.ToString("HH:mm") ?? "N/A";
        public string OptimalTimeDisplay => OptimalTime?.ToString("HH:mm") ?? "N/A";

        // Equipment Recommendations
        public string RecommendedLens { get; set; } = string.Empty;
        public string RecommendedCamera { get; set; } = string.Empty;
        public bool IsUserEquipment { get; set; }
        public string EquipmentNote { get; set; } = string.Empty;

        // Photography Settings
        public string SuggestedAperture { get; set; } = string.Empty;
        public string SuggestedShutterSpeed { get; set; } = string.Empty;
        public string SuggestedISO { get; set; } = string.Empty;
        public string FocalLengthRecommendation { get; set; } = string.Empty;

        // Event-Specific Notes
        public string PhotographyNotes { get; set; } = string.Empty;
        public string DifficultyLevel { get; set; } = string.Empty;
    }

    public class SolarEventDisplayModel
    {
        // Event Identity
        public string EventName { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public DateTime EventTime { get; set; }

        // Position Information
        public double SunAzimuth { get; set; }
        public double SunAltitude { get; set; }
        public string SunAzimuthDisplay => $"{SunAzimuth:F1}°";
        public string SunAltitudeDisplay => $"{SunAltitude:F1}°";

        // Light Quality
        public string LightQuality { get; set; } = string.Empty;
        public string ColorTemperature { get; set; } = string.Empty;

        // Photography Impact
        public string ImpactOnAstro { get; set; } = string.Empty;
        public bool ConflictsWithAstro { get; set; }
    }
}
