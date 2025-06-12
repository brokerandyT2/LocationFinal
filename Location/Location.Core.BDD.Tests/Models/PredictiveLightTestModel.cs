using Location.Photography.Application.Services;
using Location.Photography.Domain.Models;

namespace Location.Photography.BDD.Tests.Models
{
    /// <summary>
    /// Test model for predictive light scenarios
    /// </summary>
    public class PredictiveLightTestModel
    {
        public int? Id { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public double Latitude { get; set; } = 40.7128;
        public double Longitude { get; set; } = -74.0060;
        public DateTime TargetDate { get; set; } = DateTime.Today;

        // Light prediction results
        public OptimalShootingWindow BestTimeWindow { get; set; } = new();
        public List<OptimalShootingWindow> AlternativeWindows { get; set; } = new();
        public string OverallRecommendation { get; set; } = string.Empty;
        public List<string> KeyInsights { get; set; } = new();
        public double CalibrationAccuracy { get; set; }
        public bool RequiresRecalibration { get; set; }

        // Weather conditions
        public WeatherConditions CurrentWeather { get; set; } = new();
        public List<HourlyWeatherImpact> HourlyImpacts { get; set; } = new();
        public double OverallLightReductionFactor { get; set; } = 1.0;
        public string WeatherSummary { get; set; } = string.Empty;
        public List<WeatherAlert> WeatherAlerts { get; set; } = new();

        // Hourly predictions using domain model
        public List<HourlyLightPrediction> HourlyPredictions { get; set; } = new();
        public DateTime PredictionStartTime { get; set; } = DateTime.Today.AddHours(6);
        public int PredictionHours { get; set; } = 12;

        // Shooting alerts
        public List<ShootingAlertRequest> ShootingAlerts { get; set; } = new();
        public DateTime AlertTime { get; set; } = DateTime.UtcNow;
        public DateTime ShootingWindowStart { get; set; } = DateTime.Today.AddHours(7);
        public DateTime ShootingWindowEnd { get; set; } = DateTime.Today.AddHours(8);

        // Error handling
        public string ErrorMessage { get; set; } = string.Empty;
        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        // Validation properties
        public bool IsValidPrediction =>
            Latitude >= -90 && Latitude <= 90 &&
            Longitude >= -180 && Longitude <= 180 &&
            TargetDate >= DateTime.Today.AddDays(-1);

        /// <summary>
        /// Creates a PredictiveLightRecommendation from this test model
        /// </summary>
        public PredictiveLightRecommendation ToPredictiveLightRecommendation()
        {
            return new PredictiveLightRecommendation
            {
                GeneratedAt = GeneratedAt,
                BestTimeWindow = BestTimeWindow,
                AlternativeWindows = AlternativeWindows,
                OverallRecommendation = OverallRecommendation,
                KeyInsights = KeyInsights,
                CalibrationAccuracy = CalibrationAccuracy,
                RequiresRecalibration = RequiresRecalibration
            };
        }

        /// <summary>
        /// Creates a WeatherImpactAnalysis from this test model
        /// </summary>
        public WeatherImpactAnalysis ToWeatherImpactAnalysis()
        {
            return new WeatherImpactAnalysis
            {
                CurrentConditions = CurrentWeather,
                HourlyImpacts = HourlyImpacts,
                OverallLightReductionFactor = OverallLightReductionFactor,
                Summary = WeatherSummary,
                Alerts = WeatherAlerts
            };
        }

        /// <summary>
        /// Validates predictive light data
        /// </summary>
        public bool ValidatePredictionData(out List<string> errors)
        {
            errors = new List<string>();

            if (Latitude < -90 || Latitude > 90)
                errors.Add("Latitude must be between -90 and 90 degrees");

            if (Longitude < -180 || Longitude > 180)
                errors.Add("Longitude must be between -180 and 180 degrees");

            if (TargetDate < DateTime.Today.AddDays(-1))
                errors.Add("Target date cannot be more than 1 day in the past");

            if (CalibrationAccuracy < 0 || CalibrationAccuracy > 1)
                errors.Add("Calibration accuracy must be between 0 and 1");

            if (OverallLightReductionFactor < 0 || OverallLightReductionFactor > 2)
                errors.Add("Light reduction factor must be between 0 and 2");

            if (PredictionHours < 1 || PredictionHours > 24)
                errors.Add("Prediction hours must be between 1 and 24");

            return errors.Count == 0;
        }

        /// <summary>
        /// Generates hourly light predictions
        /// </summary>
        public void GenerateHourlyPredictions()
        {
            HourlyPredictions.Clear();

            for (int i = 0; i < PredictionHours; i++)
            {
                var hour = PredictionStartTime.AddHours(i);
                var prediction = new HourlyLightPrediction
                {
                    DateTime = hour,
                    PredictedEV = CalculatePredictedEV(hour),
                    EVConfidenceMargin = 0.5,
                    ConfidenceLevel = 0.85,
                    ConfidenceReason = GenerateConfidenceReason(hour),
                    SuggestedSettings = GenerateSuggestedSettings(hour),
                    LightQuality = GenerateLightCharacteristics(hour),
                    Recommendations = GenerateRecommendations(hour),
                    IsOptimalForPhotography = DetermineLightQuality(hour) == LightQuality.GoldenHour,
                    SunPosition = new SunPositionDto { Latitude = Latitude, Longitude = Longitude, DateTime = hour },
                    IsMoonVisible = false
                };

                HourlyPredictions.Add(prediction);
            }
        }

        /// <summary>
        /// Finds optimal shooting windows
        /// </summary>
        public void FindOptimalShootingWindows()
        {
            if (!HourlyPredictions.Any())
                GenerateHourlyPredictions();

            // Find best window (highest optimality score)
            var bestHour = HourlyPredictions.OrderByDescending(h => CalculateOptimalityScore(h.DateTime)).First();
            BestTimeWindow = new OptimalShootingWindow
            {
                StartTime = bestHour.DateTime,
                EndTime = bestHour.DateTime.AddHours(1),
                LightQuality = DetermineLightQuality(bestHour.DateTime),
                OptimalityScore = CalculateOptimalityScore(bestHour.DateTime),
                Description = GenerateReasoning(bestHour.DateTime),
                RecommendedFor = GetRecommendedPhotographyTypes(DetermineLightQuality(bestHour.DateTime)),
                RecommendedExposure = bestHour,
                Warnings = GenerateWarnings(bestHour.DateTime)
            };

            // Find alternative windows (score > 0.7)
            AlternativeWindows = HourlyPredictions
                .Where(h => CalculateOptimalityScore(h.DateTime) > 0.7 && h.DateTime != bestHour.DateTime)
                .Select(h => new OptimalShootingWindow
                {
                    StartTime = h.DateTime,
                    EndTime = h.DateTime.AddHours(1),
                    LightQuality = DetermineLightQuality(h.DateTime),
                    OptimalityScore = CalculateOptimalityScore(h.DateTime),
                    Description = GenerateReasoning(h.DateTime),
                    RecommendedFor = GetRecommendedPhotographyTypes(DetermineLightQuality(h.DateTime)),
                    RecommendedExposure = h,
                    Warnings = GenerateWarnings(h.DateTime)
                })
                .OrderByDescending(w => w.OptimalityScore)
                .Take(3)
                .ToList();
        }

        /// <summary>
        /// Generates shooting alerts for optimal conditions
        /// </summary>
        public void GenerateShootingAlerts()
        {
            ShootingAlerts.Clear();

            foreach (var window in new[] { BestTimeWindow }.Concat(AlternativeWindows))
            {
                if (window.OptimalityScore > 0.8)
                {
                    var alert = new ShootingAlertRequest
                    {
                        LocationId = Id ?? 1,
                        AlertTime = window.StartTime.AddHours(-1), // Alert 1 hour before
                        ShootingWindowStart = window.StartTime,
                        ShootingWindowEnd = window.EndTime,
                        LightQuality = window.LightQuality,
                        RecommendedSettings = GenerateRecommendedSettings(window.LightQuality),
                        Message = $"Excellent {window.LightQuality} light conditions predicted"
                    };

                    ShootingAlerts.Add(alert);
                }
            }
        }

        /// <summary>
        /// Calculates overall recommendation
        /// </summary>
        public void CalculateOverallRecommendation()
        {
            if (!HourlyPredictions.Any())
                GenerateHourlyPredictions();

            var goldenHours = HourlyPredictions.Count(h => DetermineLightQuality(h.DateTime) == LightQuality.GoldenHour);
            var blueHours = HourlyPredictions.Count(h => DetermineLightQuality(h.DateTime) == LightQuality.BlueHour);
            var averageScore = HourlyPredictions.Average(h => CalculateOptimalityScore(h.DateTime));

            if (goldenHours >= 3)
            {
                OverallRecommendation = "Excellent day for photography with multiple optimal windows";
            }
            else if (goldenHours >= 1)
            {
                OverallRecommendation = "Good day for photography with at least one excellent window";
            }
            else if (blueHours >= 3)
            {
                OverallRecommendation = "Moderate day for photography with several good windows";
            }
            else if (averageScore > 0.5)
            {
                OverallRecommendation = "Fair day for photography, but conditions are acceptable";
            }
            else
            {
                OverallRecommendation = "Poor day for photography, consider rescheduling";
            }

            // Add weather-specific insights
            KeyInsights.Clear();
            if (CurrentWeather.CloudCover > 80)
                KeyInsights.Add("Heavy cloud cover will reduce light quality");
            if (CurrentWeather.Precipitation > 0)
                KeyInsights.Add("Precipitation expected - protect equipment");
            if (OverallLightReductionFactor < 0.7)
                KeyInsights.Add("Significant light reduction due to weather conditions");
            if (CalibrationAccuracy < 0.8)
                KeyInsights.Add("Light predictions may be less accurate - consider recalibration");
        }

        private LightQuality DetermineLightQuality(DateTime hour)
        {
            var timeOfDay = hour.TimeOfDay.TotalHours;

            // Golden hours
            if ((timeOfDay >= 6 && timeOfDay <= 8) || (timeOfDay >= 17 && timeOfDay <= 19))
                return LightQuality.GoldenHour;

            // Blue hours
            if ((timeOfDay >= 5 && timeOfDay < 6) || (timeOfDay > 19 && timeOfDay <= 20))
                return LightQuality.BlueHour;

            // Harsh midday
            if (timeOfDay >= 11 && timeOfDay <= 15)
                return LightQuality.Harsh;

            // Overcast conditions
            if (CurrentWeather.CloudCover > 70)
                return LightQuality.Overcast;

            // Other times
            return LightQuality.Soft;
        }

        private double CalculatePredictedEV(DateTime hour)
        {
            var timeOfDay = hour.TimeOfDay.TotalHours;

            // Simplified EV calculation based on time of day
            var baseEV = timeOfDay switch
            {
                >= 6 and <= 8 => 12.0,   // Golden hour
                >= 9 and <= 11 => 14.0,  // Morning
                >= 12 and <= 14 => 16.0, // Midday
                >= 15 and <= 17 => 14.0, // Afternoon
                >= 18 and <= 19 => 12.0, // Golden hour
                _ => 8.0                  // Low light
            };

            // Apply weather reduction
            return baseEV * OverallLightReductionFactor;
        }

        private string GenerateConfidenceReason(DateTime hour)
        {
            var reasons = new List<string>();

            if (CalibrationAccuracy > 0.8)
                reasons.Add("Recent calibration");
            else
                reasons.Add("No recent calibration");

            if (CurrentWeather.CloudCover < 30)
                reasons.Add("Clear conditions");
            else if (CurrentWeather.CloudCover > 70)
                reasons.Add("Cloudy conditions");

            return string.Join(", ", reasons);
        }

        private ExposureTriangle GenerateSuggestedSettings(DateTime hour)
        {
            var quality = DetermineLightQuality(hour);

            return quality switch
            {
                LightQuality.GoldenHour => new ExposureTriangle { Aperture = "f/8", ShutterSpeed = "1/125s", ISO = "ISO 200" },
                LightQuality.BlueHour => new ExposureTriangle { Aperture = "f/5.6", ShutterSpeed = "1/60s", ISO = "ISO 400" },
                LightQuality.Harsh => new ExposureTriangle { Aperture = "f/11", ShutterSpeed = "1/250s", ISO = "ISO 100" },
                LightQuality.Overcast => new ExposureTriangle { Aperture = "f/4", ShutterSpeed = "1/125s", ISO = "ISO 400" },
                _ => new ExposureTriangle { Aperture = "f/8", ShutterSpeed = "1/125s", ISO = "ISO 400" }
            };
        }

        private LightCharacteristics GenerateLightCharacteristics(DateTime hour)
        {
            var quality = DetermineLightQuality(hour);

            return new LightCharacteristics
            {
                ColorTemperature = CalculateColorTemperature(hour),
                SoftnessFactor = quality == LightQuality.GoldenHour ? 0.9 : 0.5,
                ShadowHarshness = quality == LightQuality.Harsh ? ShadowIntensity.Hard : ShadowIntensity.Soft,
                OptimalFor = quality == LightQuality.GoldenHour ? "Portraits" : "General",
                DirectionalityFactor = quality == LightQuality.Harsh ? 0.9 : 0.3
            };
        }

        private List<string> GenerateRecommendations(DateTime hour)
        {
            var quality = DetermineLightQuality(hour);
            var recommendations = new List<string>();

            switch (quality)
            {
                case LightQuality.GoldenHour:
                    recommendations.Add("Perfect for portraits and landscapes");
                    recommendations.Add("Use a polarizing filter");
                    break;
                case LightQuality.Harsh:
                    recommendations.Add("Seek shade or use reflectors");
                    recommendations.Add("Consider HDR photography");
                    break;
                case LightQuality.Overcast:
                    recommendations.Add("Great for even lighting");
                    recommendations.Add("No harsh shadows");
                    break;
            }

            return recommendations;
        }

        private List<string> GenerateWarnings(DateTime hour)
        {
            var warnings = new List<string>();

            if (CurrentWeather.Precipitation > 0)
                warnings.Add("Rain expected - protect equipment");
            if (CurrentWeather.WindSpeed > 20)
                warnings.Add("High winds - secure tripod");

            return warnings;
        }

        private double CalculateColorTemperature(DateTime hour)
        {
            var timeOfDay = hour.TimeOfDay.TotalHours;

            // Color temperature changes throughout the day
            if (timeOfDay <= 6 || timeOfDay >= 19)
                return 3200; // Warm
            else if (timeOfDay <= 8 || timeOfDay >= 17)
                return 4000; // Golden
            else if (timeOfDay >= 11 && timeOfDay <= 15)
                return 5500; // Daylight
            else
                return 4800; // Slightly warm
        }

        private double GetWeatherImpact(DateTime hour)
        {
            // Find corresponding hourly weather impact
            var impact = HourlyImpacts.FirstOrDefault(h => h.Hour.Hour == hour.Hour);
            return impact?.LightReductionFactor ?? 1.0;
        }

        private double CalculateOptimalityScore(DateTime hour)
        {
            var lightQuality = DetermineLightQuality(hour);
            var baseScore = lightQuality switch
            {
                LightQuality.GoldenHour => 1.0,
                LightQuality.BlueHour => 0.8,
                LightQuality.Soft => 0.6,
                LightQuality.Harsh => 0.3,
                LightQuality.Overcast => 0.5,
                LightQuality.Dramatic => 0.7,
                LightQuality.Night => 0.4,
                _ => 0.5
            };

            // Apply weather impact
            var weatherImpact = GetWeatherImpact(hour);
            return baseScore * weatherImpact;
        }

        private string GenerateReasoning(DateTime hour)
        {
            var quality = DetermineLightQuality(hour);
            var weather = GetWeatherImpact(hour);
            var timeOfDay = hour.TimeOfDay.TotalHours;

            var reasons = new List<string>();

            if (quality == LightQuality.GoldenHour)
                reasons.Add("Golden hour lighting");
            else if (quality == LightQuality.BlueHour)
                reasons.Add("Blue hour lighting");
            else if (quality == LightQuality.Harsh)
                reasons.Add("Harsh midday sun");

            if (weather < 0.8)
                reasons.Add("Weather reducing light quality");
            if (weather > 1.1)
                reasons.Add("Weather enhancing light quality");

            return string.Join(", ", reasons);
        }

        private List<string> GetRecommendedPhotographyTypes(LightQuality quality)
        {
            return quality switch
            {
                LightQuality.GoldenHour => new List<string> { "Portraits", "Landscapes", "Golden Hour" },
                LightQuality.BlueHour => new List<string> { "Cityscapes", "Architecture", "Blue Hour" },
                LightQuality.Soft => new List<string> { "Street Photography", "Documentary" },
                LightQuality.Harsh => new List<string> { "High Contrast", "Shadows" },
                LightQuality.Overcast => new List<string> { "Even Lighting", "Portraits" },
                LightQuality.Dramatic => new List<string> { "Dramatic Lighting", "Moody Photography" },
                LightQuality.Night => new List<string> { "Night Photography", "Long Exposure" },
                _ => new List<string> { "General Photography" }
            };
        }

        private string GenerateRecommendedSettings(LightQuality quality)
        {
            return quality switch
            {
                LightQuality.GoldenHour => "f/8, 1/125s, ISO 200",
                LightQuality.BlueHour => "f/5.6, 1/60s, ISO 400",
                LightQuality.Soft => "f/4, 1/125s, ISO 400",
                LightQuality.Harsh => "f/11, 1/250s, ISO 100",
                LightQuality.Overcast => "f/5.6, 1/125s, ISO 400",
                LightQuality.Dramatic => "f/8, 1/60s, ISO 800",
                LightQuality.Night => "f/2.8, 30s, ISO 3200",
                _ => "f/8, 1/125s, ISO 400"
            };
        }

        /// <summary>
        /// Creates a test model with default valid values
        /// </summary>
        public static PredictiveLightTestModel CreateValid(int? id = null)
        {
            var model = new PredictiveLightTestModel
            {
                Id = id,
                GeneratedAt = DateTime.UtcNow,
                Latitude = 40.7128,
                Longitude = -74.0060,
                TargetDate = DateTime.Today,
                CalibrationAccuracy = 0.85,
                RequiresRecalibration = false,
                CurrentWeather = new WeatherConditions
                {
                    CloudCover = 30,
                    Precipitation = 0,
                    Humidity = 65,
                    Visibility = 15,
                    AirQualityIndex = 50,
                    WindSpeed = 8,
                    Description = "Partly cloudy"
                },
                OverallLightReductionFactor = 0.9,
                WeatherSummary = "Good conditions for photography",
                PredictionStartTime = DateTime.Today.AddHours(6),
                PredictionHours = 12
            };

            model.GenerateHourlyPredictions();
            model.FindOptimalShootingWindows();
            model.GenerateShootingAlerts();
            model.CalculateOverallRecommendation();

            return model;
        }

        /// <summary>
        /// Creates a test model with invalid values for testing validation
        /// </summary>
        public static PredictiveLightTestModel CreateInvalid()
        {
            return new PredictiveLightTestModel
            {
                Latitude = 100, // Invalid: > 90
                Longitude = 200, // Invalid: > 180
                TargetDate = DateTime.Today.AddDays(-5), // Invalid: too far in past
                CalibrationAccuracy = 1.5, // Invalid: > 1
                OverallLightReductionFactor = -0.5, // Invalid: < 0
                PredictionHours = 30 // Invalid: > 24
            };
        }
    }
}