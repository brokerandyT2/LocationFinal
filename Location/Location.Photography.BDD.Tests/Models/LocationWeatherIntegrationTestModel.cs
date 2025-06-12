using Location.Photography.Application.Services;
using Location.Photography.Domain.Models;

namespace Location.Photography.BDD.Tests.Models
{
    /// <summary>
    /// Test model for location-weather integration scenarios
    /// </summary>
    public class LocationWeatherIntegrationTestModel
    {
        public int? Id { get; set; }
        public string LocationName { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Elevation { get; set; }
        public DateTime TargetDate { get; set; } = DateTime.Today;
        public string LocationType { get; set; } = string.Empty;

        // Weather data
        public WeatherConditions CurrentWeather { get; set; } = new();
        public List<WeatherConditions> WeatherForecast { get; set; } = new();
        public List<WeatherConditions> HistoricalWeather { get; set; } = new();
        public bool WeatherDataAvailable { get; set; } = true;

        // Sun position integration
        public SunCalculationTestModel SunData { get; set; } = new();
        public EnhancedSunTimes SunTimes { get; set; } = new();
        public bool SunWeatherCorrelationCalculated { get; set; }

        // Location characteristics
        public string ClimateZone { get; set; } = string.Empty;
        public string TerrainType { get; set; } = string.Empty;
        public double DistanceToWater { get; set; } // km
        public bool IsCoastal { get; set; }
        public bool IsUrban { get; set; }
        public bool IsMountainous { get; set; }

        // Microclimate factors
        public MicroclimateFactor MicroclimateFactor { get; set; } = new();
        public double TemperatureVariation { get; set; }
        public double HumidityVariation { get; set; }
        public double WindVariation { get; set; }

        // Seasonal patterns
        public List<SeasonalPattern> SeasonalPatterns { get; set; } = new();
        public string OptimalSeason { get; set; } = string.Empty;
        public List<string> RecommendedMonths { get; set; } = new();

        // Photography suitability
        public double PhotographySuitabilityScore { get; set; }
        public List<string> BestPhotographyTypes { get; set; } = new();
        public List<string> WeatherChallenges { get; set; } = new();
        public List<string> WeatherAdvantages { get; set; } = new();

        // Multi-location comparison
        public List<LocationWeatherIntegrationTestModel> ComparisonLocations { get; set; } = new();
        public LocationComparisonResult? ComparisonResult { get; set; }

        // Error handling
        public string ErrorMessage { get; set; } = string.Empty;
        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        // Validation properties
        public bool IsValidLocation =>
            Latitude >= -90 && Latitude <= 90 &&
            Longitude >= -180 && Longitude <= 180 &&
            !string.IsNullOrEmpty(LocationName);

        /// <summary>
        /// Calculates weather impact on photography for this location
        /// </summary>
        public WeatherImpactAnalysis CalculateWeatherImpact()
        {
            var analysis = new WeatherImpactAnalysis
            {
                CurrentConditions = CurrentWeather,
                Summary = GenerateWeatherSummary(),
                OverallLightReductionFactor = CalculateLightReductionFactor()
            };

            // Generate hourly impacts
            analysis.HourlyImpacts = GenerateHourlyWeatherImpacts();

            // Generate alerts
            analysis.Alerts = GenerateWeatherAlerts();

            return analysis;
        }

        /// <summary>
        /// Correlates sun position with weather conditions
        /// </summary>
        public SunWeatherCorrelation CorrelateSunAndWeather()
        {
            var correlation = new SunWeatherCorrelation
            {
                Location = LocationName,
                Date = TargetDate,
                SunTimes = SunTimes,
                WeatherConditions = CurrentWeather
            };

            // Calculate optimal shooting windows considering both sun and weather
            correlation.OptimalWindows = CalculateOptimalShootingWindows();

            // Generate recommendations
            correlation.Recommendations = GenerateSunWeatherRecommendations();

            SunWeatherCorrelationCalculated = true;
            return correlation;
        }

        /// <summary>
        /// Analyzes elevation impact on weather patterns
        /// </summary>
        public ElevationWeatherAnalysis AnalyzeElevationImpact()
        {
            var analysis = new ElevationWeatherAnalysis
            {
                Elevation = Elevation,
                TemperatureAdjustment = CalculateTemperatureAdjustment(),
                PressureAdjustment = CalculatePressureAdjustment(),
                CloudFormationFactor = CalculateCloudFormationFactor()
            };

            // Expected changes based on elevation
            if (Elevation > 1000)
            {
                analysis.ExpectedChanges.Add("Lower temperatures");
                analysis.ExpectedChanges.Add("Higher UV exposure");
                analysis.ExpectedChanges.Add("More variable weather");
            }

            if (Elevation > 2000)
            {
                analysis.ExpectedChanges.Add("Increased wind speeds");
                analysis.ExpectedChanges.Add("Rapid weather changes");
            }

            return analysis;
        }

        /// <summary>
        /// Analyzes seasonal weather patterns for this location
        /// </summary>
        public void AnalyzeSeasonalPatterns()
        {
            SeasonalPatterns.Clear();

            // Generate patterns for each season
            foreach (var season in Enum.GetValues<Season>())
            {
                var pattern = new SeasonalPattern
                {
                    Season = season,
                    AverageTemperature = GetSeasonalTemperature(season),
                    AveragePrecipitation = GetSeasonalPrecipitation(season),
                    AverageCloudCover = GetSeasonalCloudCover(season),
                    PhotographyRating = CalculateSeasonalPhotographyRating(season),
                    RecommendedActivities = GetSeasonalPhotographyRecommendations(season)
                };

                SeasonalPatterns.Add(pattern);
            }

            // Determine optimal season
            OptimalSeason = SeasonalPatterns
                .OrderByDescending(p => p.PhotographyRating)
                .First().Season.ToString();
        }

        /// <summary>
        /// Compares weather conditions across multiple locations
        /// </summary>
        public LocationComparisonResult CompareLocations()
        {
            if (!ComparisonLocations.Any())
                return new LocationComparisonResult();

            var result = new LocationComparisonResult();
            var allLocations = new List<LocationWeatherIntegrationTestModel> { this }
                .Concat(ComparisonLocations).ToList();

            // Compare current weather
            result.BestCurrentWeather = allLocations
                .OrderByDescending(l => l.CalculateCurrentWeatherScore())
                .First().LocationName;

            // Compare seasonal suitability
            result.BestOverallLocation = allLocations
                .OrderByDescending(l => l.PhotographySuitabilityScore)
                .First().LocationName;

            // Generate comparison insights
            result.ComparisonInsights = GenerateComparisonInsights(allLocations);

            ComparisonResult = result;
            return result;
        }

        /// <summary>
        /// Detects microclimate effects
        /// </summary>
        public void DetectMicroclimates()
        {
            MicroclimateFactor = new MicroclimateFactor();

            // Urban heat island effect
            if (IsUrban)
            {
                MicroclimateFactor.TemperatureModifier += 2.0; // Urban areas typically 2°C warmer
                MicroclimateFactor.HumidityModifier -= 5.0;
                MicroclimateFactor.Description += "Urban heat island effect. ";
            }

            // Coastal moderation
            if (IsCoastal)
            {
                MicroclimateFactor.TemperatureModifier -= 1.0; // More moderate temperatures
                MicroclimateFactor.HumidityModifier += 10.0;
                MicroclimateFactor.Description += "Coastal temperature moderation. ";
            }

            // Elevation effects
            if (Elevation > 500)
            {
                var elevationEffect = (Elevation - 500) / 1000.0 * 6.5; // 6.5°C per 1000m
                MicroclimateFactor.TemperatureModifier -= elevationEffect;
                MicroclimateFactor.Description += $"Elevation cooling effect (-{elevationEffect:F1}°C). ";
            }

            // Water body proximity
            if (DistanceToWater < 5)
            {
                MicroclimateFactor.HumidityModifier += 5.0;
                MicroclimateFactor.Description += "Water body proximity effect. ";
            }
        }

        /// <summary>
        /// Calculates photography suitability score
        /// </summary>
        public void CalculatePhotographySuitability()
        {
            var score = 100.0;

            // Weather factors
            score -= CurrentWeather.CloudCover * 0.3; // Reduce for cloud cover
            score -= CurrentWeather.Precipitation * 2.0; // Heavily penalize precipitation
            score -= Math.Max(0, CurrentWeather.WindSpeed - 10) * 0.5; // Penalize high winds

            // Visibility bonus
            if (CurrentWeather.Visibility > 10)
                score += 10;

            // Location factors
            if (IsCoastal)
                score += 5; // Coastal locations often have interesting weather

            if (IsMountainous)
                score += 10; // Mountains provide dramatic conditions

            if (IsUrban && CurrentWeather.AirQualityIndex > 100)
                score -= 15; // Poor air quality in urban areas

            PhotographySuitabilityScore = Math.Max(0, Math.Min(100, score));
        }

        private string GenerateWeatherSummary()
        {
            var conditions = new List<string>();

            if (CurrentWeather.CloudCover < 30)
                conditions.Add("clear skies");
            else if (CurrentWeather.CloudCover > 70)
                conditions.Add("overcast");
            else
                conditions.Add("partly cloudy");

            if (CurrentWeather.Precipitation > 0)
                conditions.Add("precipitation expected");

            if (CurrentWeather.WindSpeed > 15)
                conditions.Add("windy conditions");

            return string.Join(", ", conditions);
        }

        private double CalculateLightReductionFactor()
        {
            var factor = 1.0;

            // Cloud cover impact
            factor -= (CurrentWeather.CloudCover / 100.0) * 0.4;

            // Precipitation impact
            if (CurrentWeather.Precipitation > 0)
                factor -= 0.3;

            // Air quality impact
            if (CurrentWeather.AirQualityIndex > 100)
                factor -= 0.1;

            return Math.Max(0.1, factor);
        }

        private List<HourlyWeatherImpact> GenerateHourlyWeatherImpacts()
        {
            var impacts = new List<HourlyWeatherImpact>();

            for (int i = 0; i < 24; i++)
            {
                var hour = DateTime.Today.AddHours(i);
                impacts.Add(new HourlyWeatherImpact
                {
                    Hour = hour,
                    LightReductionFactor = CalculateLightReductionFactor(),
                    PredictedQuality = DetermineLightQuality(hour),
                    Reasoning = $"Weather conditions at {hour:HH:mm}"
                });
            }

            return impacts;
        }

        private List<WeatherAlert> GenerateWeatherAlerts()
        {
            var alerts = new List<WeatherAlert>();

            if (CurrentWeather.Precipitation > 10)
            {
                alerts.Add(new WeatherAlert
                {
                    Type = AlertType.Weather,
                    Severity = AlertSeverity.Warning,
                    Message = "Heavy precipitation expected - protect equipment",
                    ValidFrom = DateTime.UtcNow,
                    ValidTo = DateTime.UtcNow.AddHours(6)
                });
            }

            if (CurrentWeather.WindSpeed > 25)
            {
                alerts.Add(new WeatherAlert
                {
                    Type = AlertType.Weather,
                    Severity = AlertSeverity.Warning,
                    Message = "High winds - secure tripod and equipment",
                    ValidFrom = DateTime.UtcNow,
                    ValidTo = DateTime.UtcNow.AddHours(4)
                });
            }

            return alerts;
        }

        private List<OptimalShootingWindow> CalculateOptimalShootingWindows()
        {
            var windows = new List<OptimalShootingWindow>();

            // Golden hour morning
            if (CurrentWeather.CloudCover < 70)
            {
                windows.Add(new OptimalShootingWindow
                {
                    StartTime = SunTimes.GoldenHourMorningStart,
                    EndTime = SunTimes.GoldenHourMorningEnd,
                    LightQuality = LightQuality.GoldenHour,
                    OptimalityScore = CalculateWindowScore(LightQuality.GoldenHour),
                    Description = "Morning golden hour with favorable weather"
                });
            }

            // Golden hour evening
            if (CurrentWeather.CloudCover < 70)
            {
                windows.Add(new OptimalShootingWindow
                {
                    StartTime = SunTimes.GoldenHourEveningStart,
                    EndTime = SunTimes.GoldenHourEveningEnd,
                    LightQuality = LightQuality.GoldenHour,
                    OptimalityScore = CalculateWindowScore(LightQuality.GoldenHour),
                    Description = "Evening golden hour with favorable weather"
                });
            }

            return windows.OrderByDescending(w => w.OptimalityScore).ToList();
        }

        private List<string> GenerateSunWeatherRecommendations()
        {
            var recommendations = new List<string>();

            if (CurrentWeather.CloudCover > 70)
                recommendations.Add("Overcast conditions provide even lighting for portraits");

            if (CurrentWeather.CloudCover < 30)
                recommendations.Add("Clear skies ideal for golden hour photography");

            if (CurrentWeather.Precipitation > 0)
                recommendations.Add("Rain creates opportunities for reflections and mood");

            return recommendations;
        }

        private double CalculateTemperatureAdjustment()
        {
            // Standard lapse rate: 6.5°C per 1000m
            return -(Elevation / 1000.0) * 6.5;
        }

        private double CalculatePressureAdjustment()
        {
            // Pressure decreases with altitude
            return Math.Exp(-Elevation / 8400.0); // Scale height approximation
        }

        private double CalculateCloudFormationFactor()
        {
            // Higher elevations tend to have more cloud formation
            return Math.Min(2.0, 1.0 + (Elevation / 2000.0));
        }

        private LightQuality DetermineLightQuality(DateTime hour)
        {
            var timeOfDay = hour.TimeOfDay.TotalHours;

            if (CurrentWeather.CloudCover > 70)
                return LightQuality.Overcast;

            if ((timeOfDay >= 6 && timeOfDay <= 8) || (timeOfDay >= 17 && timeOfDay <= 19))
                return LightQuality.GoldenHour;

            if ((timeOfDay >= 5 && timeOfDay < 6) || (timeOfDay > 19 && timeOfDay <= 20))
                return LightQuality.BlueHour;

            if (timeOfDay >= 11 && timeOfDay <= 15)
                return LightQuality.Harsh;

            return LightQuality.Soft;
        }

        private double CalculateWindowScore(LightQuality quality)
        {
            var baseScore = quality switch
            {
                LightQuality.GoldenHour => 1.0,
                LightQuality.BlueHour => 0.8,
                LightQuality.Soft => 0.6,
                LightQuality.Overcast => 0.5,
                LightQuality.Harsh => 0.3,
                _ => 0.4
            };

            // Adjust for weather conditions
            return baseScore * CalculateLightReductionFactor();
        }

        private double GetSeasonalTemperature(Season season)
        {
            // Simplified seasonal temperature calculation
            return season switch
            {
                Season.Spring => 15.0,
                Season.Summer => 25.0,
                Season.Autumn => 12.0,
                Season.Winter => 2.0
            };
        }

        private double GetSeasonalPrecipitation(Season season)
        {
            return season switch
            {
                Season.Spring => 80.0,
                Season.Summer => 60.0,
                Season.Autumn => 90.0,
                Season.Winter => 70.0
            };
        }

        private double GetSeasonalCloudCover(Season season)
        {
            return season switch
            {
                Season.Spring => 55.0,
                Season.Summer => 40.0,
                Season.Autumn => 65.0,
                Season.Winter => 75.0
            };
        }

        private double CalculateSeasonalPhotographyRating(Season season)
        {
            var rating = 50.0;
            rating += (30.0 - GetSeasonalPrecipitation(season)) * 0.3;
            rating += (50.0 - GetSeasonalCloudCover(season)) * 0.4;
            return Math.Max(0, Math.Min(100, rating));
        }

        private List<string> GetSeasonalPhotographyRecommendations(Season season)
        {
            return season switch
            {
                Season.Spring => new List<string> { "Flower photography", "Fresh green landscapes" },
                Season.Summer => new List<string> { "Golden hour portraits", "Beach photography" },
                Season.Autumn => new List<string> { "Fall foliage", "Dramatic skies" },
                Season.Winter => new List<string> { "Snow scenes", "Minimal landscapes" }
            };
        }

        private double CalculateCurrentWeatherScore()
        {
            var score = 100.0;
            score -= CurrentWeather.CloudCover * 0.5;
            score -= CurrentWeather.Precipitation * 3.0;
            score -= Math.Max(0, CurrentWeather.WindSpeed - 10) * 1.0;
            return Math.Max(0, score);
        }

        private List<string> GenerateComparisonInsights(List<LocationWeatherIntegrationTestModel> locations)
        {
            var insights = new List<string>();

            var bestWeather = locations.OrderByDescending(l => l.CalculateCurrentWeatherScore()).First();
            insights.Add($"{bestWeather.LocationName} has the best current weather conditions");

            var leastCloudy = locations.OrderBy(l => l.CurrentWeather.CloudCover).First();
            insights.Add($"{leastCloudy.LocationName} has the clearest skies");

            var calmest = locations.OrderBy(l => l.CurrentWeather.WindSpeed).First();
            insights.Add($"{calmest.LocationName} has the calmest conditions");

            return insights;
        }

        /// <summary>
        /// Creates a test model for coastal location
        /// </summary>
        public static LocationWeatherIntegrationTestModel CreateCoastalLocation()
        {
            var model = new LocationWeatherIntegrationTestModel
            {
                Id = 1,
                LocationName = "Coastal Photography Location",
                Latitude = 41.071228,
                Longitude = -71.857670,
                Elevation = 5,
                IsCoastal = true,
                DistanceToWater = 0.1,
                CurrentWeather = new WeatherConditions
                {
                    CloudCover = 40,
                    Precipitation = 0,
                    Humidity = 75,
                    Visibility = 12,
                    WindSpeed = 12,
                    Description = "Coastal breeze"
                }
            };

            model.DetectMicroclimates();
            model.CalculatePhotographySuitability();
            return model;
        }

        /// <summary>
        /// Creates a test model for mountain location
        /// </summary>
        public static LocationWeatherIntegrationTestModel CreateMountainLocation()
        {
            var model = new LocationWeatherIntegrationTestModel
            {
                Id = 2,
                LocationName = "Mountain Photography Location",
                Latitude = 44.267778,
                Longitude = -71.800556,
                Elevation = 1900,
                IsMountainous = true,
                DistanceToWater = 15,
                CurrentWeather = new WeatherConditions
                {
                    CloudCover = 60,
                    Precipitation = 0,
                    Humidity = 55,
                    Visibility = 20,
                    WindSpeed = 18,
                    Description = "Clear mountain air"
                }
            };

            model.DetectMicroclimates();
            model.CalculatePhotographySuitability();
            return model;
        }
    }

    public enum Season
    {
        Spring,
        Summer,
        Autumn,
        Winter
    }

    public class MicroclimateFactor
    {
        public double TemperatureModifier { get; set; }
        public double HumidityModifier { get; set; }
        public double WindModifier { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    public class SeasonalPattern
    {
        public Season Season { get; set; }
        public double AverageTemperature { get; set; }
        public double AveragePrecipitation { get; set; }
        public double AverageCloudCover { get; set; }
        public double PhotographyRating { get; set; }
        public List<string> RecommendedActivities { get; set; } = new();
    }

    public class SunWeatherCorrelation
    {
        public string Location { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public EnhancedSunTimes SunTimes { get; set; } = new();
        public WeatherConditions WeatherConditions { get; set; } = new();
        public List<OptimalShootingWindow> OptimalWindows { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
    }

    public class ElevationWeatherAnalysis
    {
        public double Elevation { get; set; }
        public double TemperatureAdjustment { get; set; }
        public double PressureAdjustment { get; set; }
        public double CloudFormationFactor { get; set; }
        public List<string> ExpectedChanges { get; set; } = new();
    }

    public class LocationComparisonResult
    {
        public string BestCurrentWeather { get; set; } = string.Empty;
        public string BestOverallLocation { get; set; } = string.Empty;
        public List<string> ComparisonInsights { get; set; } = new();
    }
}