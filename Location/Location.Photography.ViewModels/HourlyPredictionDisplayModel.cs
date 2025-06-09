// Enhanced Location.Photography.ViewModels/HourlyPredictionDisplayModel.cs
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Location.Photography.ViewModels
{
    public class HourlyPredictionDisplayModel : INotifyPropertyChanged
    {
        // Existing properties...
        public DateTime Time { get; set; }
        public string DeviceTimeDisplay { get; set; } = string.Empty;
        public string LocationTimeDisplay { get; set; } = string.Empty;
        public double PredictedEV { get; set; }
        public double EVConfidenceMargin { get; set; }
        public string SuggestedAperture { get; set; } = string.Empty;
        public string SuggestedShutterSpeed { get; set; } = string.Empty;
        public string SuggestedISO { get; set; } = string.Empty;
        public double ConfidenceLevel { get; set; }
        public string LightQuality { get; set; } = string.Empty;
        public double ColorTemperature { get; set; }
        public string Recommendations { get; set; } = string.Empty;
        public bool IsOptimalTime { get; set; }
        public string TimeFormat { get; set; } = "HH:mm";
        public double ShootingQualityScore { get; set; }

        // Enhanced weather integration properties
        private string _wd = string.Empty;
        private TextInfo textInfo = CultureInfo.CurrentCulture.TextInfo;
        public string WeatherDescription { get { return textInfo.ToTitleCase(_wd); } set { _wd = value; } }
        public int CloudCover { get; set; }
        public double PrecipitationProbability { get; set; }
        public string WindInfo { get; set; } = string.Empty;
        public double UvIndex { get; set; }
        public int Humidity { get; set; }

        // NEW: Equipment recommendation properties
        public HourlyEquipmentRecommendation? EquipmentRecommendation { get; set; }
        public bool HasUserEquipment => EquipmentRecommendation?.HasUserEquipment ?? false;
        public string EquipmentRecommendationText => EquipmentRecommendation?.Recommendation ?? "No equipment recommendation available";

        // User equipment display properties
        public string UserCameraLensRecommendation
        {
            get
            {
                if (EquipmentRecommendation?.RecommendedCombination != null)
                {
                    var combo = EquipmentRecommendation.RecommendedCombination;
                    return $"📷 Use {combo.Camera.Name} with {combo.Lens.NameForLens}";
                }
                return EquipmentRecommendationText;
            }
        }

        public string EquipmentMatchScore
        {
            get
            {
                if (EquipmentRecommendation?.RecommendedCombination != null)
                {
                    var score = EquipmentRecommendation.RecommendedCombination.MatchScore;
                    return score >= 85 ? "Excellent Match" :
                           score >= 70 ? "Very Good Match" :
                           score >= 50 ? "Good Match" : "Workable";
                }
                return "";
            }
        }

        public string EquipmentMatchColor
        {
            get
            {
                if (EquipmentRecommendation?.RecommendedCombination != null)
                {
                    var score = EquipmentRecommendation.RecommendedCombination.MatchScore;
                    return score >= 85 ? "#4CAF50" : // Green - Excellent
                           score >= 70 ? "#8BC34A" : // Light Green - Very Good
                           score >= 50 ? "#FF9800" : // Orange - Good
                           "#FF5722"; // Red - Workable
                }
                return "#9E9E9E"; // Gray - No recommendation
            }
        }

        public List<string> EquipmentStrengths => EquipmentRecommendation?.RecommendedCombination?.Strengths ?? new List<string>();
        public List<string> EquipmentLimitations => EquipmentRecommendation?.RecommendedCombination?.Limitations ?? new List<string>();

        public string DetailedEquipmentRecommendation
        {
            get
            {
                if (EquipmentRecommendation?.RecommendedCombination != null)
                {
                    return EquipmentRecommendation.RecommendedCombination.DetailedRecommendation;
                }
                return EquipmentRecommendationText;
            }
        }

        // Enhanced equipment display for UI
        public string EquipmentSummary
        {
            get
            {
                if (HasUserEquipment && EquipmentRecommendation?.RecommendedCombination != null)
                {
                    var combo = EquipmentRecommendation.RecommendedCombination;
                    var scoreText = EquipmentMatchScore;
                    return $"{combo.Camera.Name} + {combo.Lens.NameForLens} ({scoreText})";
                }
                return EquipmentRecommendationText;
            }
        }

        // Equipment recommendation priority for sorting
        public int EquipmentPriority
        {
            get
            {
                if (EquipmentRecommendation?.RecommendedCombination != null)
                {
                    var score = EquipmentRecommendation.RecommendedCombination.MatchScore;
                    if (score >= 85) return 4; // Excellent
                    if (score >= 70) return 3; // Very Good
                    if (score >= 50) return 2; // Good
                    return 1; // Workable
                }
                return 0; // No equipment
            }
        }

        // Combined equipment and weather assessment
        public string OverallRecommendation
        {
            get
            {
                var parts = new List<string>();

                // Light quality
                if (IsOptimalTime)
                    parts.Add($"✓ {LightQuality}");
                else
                    parts.Add($"○ {LightQuality}");

                // Equipment recommendation
                if (HasUserEquipment)
                    parts.Add($"📷 {EquipmentMatchScore}");
                else
                    parts.Add("📷 Generic equipment needed");

                // Weather impact
                var weatherImpact = ConfidenceLevel >= 0.8 ? "Clear conditions" :
                                  ConfidenceLevel >= 0.6 ? "Some weather impact" :
                                  "Significant weather impact";
                parts.Add($"🌤 {weatherImpact}");

                return string.Join(" • ", parts);
            }
        }

        // Display properties
        public string FormattedPrediction => $"EV {PredictedEV:F1} ±{EVConfidenceMargin:F1}";
        public string ConfidenceDisplay => $"Weather Prediction has a {ConfidenceLevel:P0} confidence level";
        public string WeatherSummary => !string.IsNullOrEmpty(WeatherDescription)
            ? $"{WeatherDescription}, {CloudCover}% clouds"
            : string.Empty;

        // Enhanced confidence color coding
        public string ConfidenceColor
        {
            get
            {
                return ConfidenceLevel switch
                {
                    >= 0.8 => "#4CAF50", // Green - High confidence
                    >= 0.6 => "#FF9800", // Orange - Medium confidence  
                    >= 0.4 => "#F44336", // Red - Low confidence
                    _ => "#9E9E9E"        // Gray - Very low confidence
                };
            }
        }

        // Light quality color coding
        public string LightQualityColor
        {
            get
            {
                return LightQuality.ToLower() switch
                {
                    var q when q.Contains("golden") => "#FFD700", // Gold
                    var q when q.Contains("blue") => "#87CEEB",   // Sky blue
                    var q when q.Contains("soft") => "#98FB98",   // Pale green
                    var q when q.Contains("harsh") => "#FF6347",  // Tomato red
                    var q when q.Contains("overcast") => "#D3D3D3", // Light gray
                    _ => "#FFFFFF" // White default
                };
            }
        }

        // Optimal time background color
        public string OptimalTimeBackgroundColor
        {
            get
            {
                if (IsOptimalTime)
                {
                    return ConfidenceLevel switch
                    {
                        >= 0.8 => "#E8F5E8", // Light green
                        >= 0.6 => "#FFF3E0", // Light orange
                        _ => "#FFEBEE"        // Light red
                    };
                }
                return "#F5F5F5"; // Light gray for non-optimal times
            }
        }

        // Time period classification
        public string TimePeriod
        {
            get
            {
                var hour = Time.Hour;
                return hour switch
                {
                    >= 5 and <= 7 => "Dawn",
                    >= 8 and <= 10 => "Morning",
                    >= 11 and <= 13 => "Midday",
                    >= 14 and <= 16 => "Afternoon",
                    >= 17 and <= 19 => "Evening",
                    >= 20 and <= 22 => "Dusk",
                    _ => "Night"
                };
            }
        }

        // Weather impact indicator
        public string WeatherImpactLevel
        {
            get
            {
                // Based on confidence level - lower confidence often indicates weather impact
                return ConfidenceLevel switch
                {
                    >= 0.8 => "Minimal", // High confidence = minimal weather impact
                    >= 0.6 => "Moderate",
                    >= 0.4 => "Significant",
                    _ => "Severe"
                };
            }
        }

        // Shooting recommendation priority
        public int ShootingPriority
        {
            get
            {
                if (!IsOptimalTime) return 0;

                return ConfidenceLevel switch
                {
                    >= 0.8 => 3, // High priority
                    >= 0.6 => 2, // Medium priority
                    >= 0.4 => 1, // Low priority
                    _ => 0       // Not recommended
                };
            }
        }

        // Light meter pre-population values
        public LightMeterPreset GetLightMeterPreset()
        {
            return new LightMeterPreset
            {
                PredictedEV = PredictedEV,
                SuggestedAperture = SuggestedAperture,
                SuggestedShutterSpeed = SuggestedShutterSpeed,
                SuggestedISO = SuggestedISO,
                ExpectedLightLevel = CalculateExpectedLightLevel(),
                OptimalForPhotography = IsOptimalTime,
                ConfidenceLevel = ConfidenceLevel,
                EquipmentRecommendation = DetailedEquipmentRecommendation
            };
        }

        private double CalculateExpectedLightLevel()
        {
            // Convert EV to approximate lux value for light meter
            return Math.Pow(2, PredictedEV) * 2.5; // Rough approximation
        }

        // Camera tip matching
        public CameraTipCriteria GetTipMatchingCriteria()
        {
            return new CameraTipCriteria
            {
                Aperture = SuggestedAperture,
                ShutterSpeed = SuggestedShutterSpeed,
                ISO = SuggestedISO,
                LightCondition = LightQuality,
                TimePeriod = TimePeriod,
                OptimalForPortraits = LightQuality.ToLower().Contains("soft") || LightQuality.ToLower().Contains("golden"),
                OptimalForLandscapes = IsOptimalTime && (LightQuality.ToLower().Contains("golden") || LightQuality.ToLower().Contains("blue")),
                WeatherCondition = WeatherImpactLevel,
                EquipmentRecommendation = DetailedEquipmentRecommendation
            };
        }

        // Formatted time helpers
        public string GetFormattedTime(string timeFormat)
        {
            return Time.ToString(timeFormat);
        }

        public string GetFormattedDate(string dateFormat)
        {
            return Time.ToString(dateFormat);
        }

        // Enhanced display properties for UI binding
        public string DetailedRecommendation
        {
            get
            {
                var recommendation = $"Best for: {LightQuality}";

                if (HasUserEquipment)
                {
                    recommendation += $"\n📷 Equipment: {UserCameraLensRecommendation}";
                    if (EquipmentStrengths.Any())
                    {
                        recommendation += $"\n✓ {string.Join(", ", EquipmentStrengths.Take(2))}";
                    }
                }
                else
                {
                    recommendation += $"\n📷 Recommended: {EquipmentRecommendationText}";
                }

                if (!string.IsNullOrEmpty(Recommendations))
                {
                    recommendation += $"\nTips: {Recommendations}";
                }
                if (ColorTemperature > 0)
                {
                    recommendation += $"\nColor: {ColorTemperature:F0}K";
                }
                return recommendation;
            }
        }

        public string CompactSummary => $"{FormattedPrediction} • {ConfidenceDisplay} • {LightQuality}";

        // INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }

    // Enhanced supporting classes for modal navigation
    public class LightMeterPreset
    {
        public double PredictedEV { get; set; }
        public string SuggestedAperture { get; set; } = string.Empty;
        public string SuggestedShutterSpeed { get; set; } = string.Empty;
        public string SuggestedISO { get; set; } = string.Empty;
        public double ExpectedLightLevel { get; set; }
        public bool OptimalForPhotography { get; set; }
        public double ConfidenceLevel { get; set; }
        public string EquipmentRecommendation { get; set; } = string.Empty; // NEW
    }

    public class CameraTipCriteria
    {
        public string Aperture { get; set; } = string.Empty;
        public string ShutterSpeed { get; set; } = string.Empty;
        public string ISO { get; set; } = string.Empty;
        public string LightCondition { get; set; } = string.Empty;
        public string TimePeriod { get; set; } = string.Empty;
        public bool OptimalForPortraits { get; set; }
        public bool OptimalForLandscapes { get; set; }
        public string WeatherCondition { get; set; } = string.Empty;
        public string EquipmentRecommendation { get; set; } = string.Empty; // NEW
    }
}