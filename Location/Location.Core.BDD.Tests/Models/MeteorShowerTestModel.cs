using Location.Photography.Domain.Entities;

namespace Location.Photography.BDD.Tests.Models
{
    /// <summary>
    /// Test model for meteor shower tracking scenarios
    /// </summary>
    public class MeteorShowerTestModel
    {
        public int? Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Designation { get; set; } = string.Empty;
        public MeteorShowerActivity Activity { get; set; } = new();
        public double RadiantRA { get; set; }
        public double RadiantDec { get; set; }
        public int SpeedKmS { get; set; }
        public string ParentBody { get; set; } = string.Empty;

        // Additional test properties
        public double Latitude { get; set; } = 40.7128;
        public double Longitude { get; set; } = -74.0060;
        public DateTime ObservationDate { get; set; } = DateTime.Today;
        public int MinZHR { get; set; } = 20;
        public double MoonPhase { get; set; }
        public string WeatherConditions { get; set; } = string.Empty;
        public List<string> RecommendedCameraSettings { get; set; } = new();
        public List<string> PhotographyTips { get; set; } = new();

        // Calculated properties
        public bool IsActive => Activity.IsActiveOn(ObservationDate);
        public bool IsPeak => GetDaysFromPeak() <= 1;
        public bool IsPhotographyWorthy => Activity.GetExpectedZHR(ObservationDate) >= MinZHR;
        public double ExpectedZHR => Activity.GetExpectedZHR(ObservationDate);

        // Error handling
        public string ErrorMessage { get; set; } = string.Empty;
        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        // Validation properties
        public bool IsValidShower =>
            !string.IsNullOrEmpty(Code) &&
            !string.IsNullOrEmpty(Designation) &&
            !string.IsNullOrEmpty(Activity.Start) &&
            !string.IsNullOrEmpty(Activity.Peak) &&
            !string.IsNullOrEmpty(Activity.Finish) &&
            Activity.ZHR > 0;

        /// <summary>
        /// Creates a MeteorShower entity from this test model
        /// </summary>
        public MeteorShower ToMeteorShower()
        {
            return new MeteorShower
            {
                Code = Code,
                Designation = Designation,
                Activity = Activity,
                RadiantRA = RadiantRA,
                RadiantDec = RadiantDec,
                SpeedKmS = SpeedKmS,
                ParentBody = ParentBody
            };
        }

        /// <summary>
        /// Updates this model from a MeteorShower entity
        /// </summary>
        public void UpdateFromEntity(MeteorShower entity)
        {
            Code = entity.Code;
            Designation = entity.Designation;
            Activity = entity.Activity;
            RadiantRA = entity.RadiantRA;
            RadiantDec = entity.RadiantDec;
            SpeedKmS = entity.SpeedKmS;
            ParentBody = entity.ParentBody;
        }

        /// <summary>
        /// Validates meteor shower data
        /// </summary>
        public bool ValidateShowerData(out List<string> errors)
        {
            errors = new List<string>();

            if (string.IsNullOrEmpty(Code))
                errors.Add("Shower code is required");

            if (Code.Length != 3)
                errors.Add("Shower code must be exactly 3 characters");

            if (string.IsNullOrEmpty(Designation))
                errors.Add("Shower designation is required");

            if (string.IsNullOrEmpty(Activity.Start))
                errors.Add("Activity start date is required");

            if (string.IsNullOrEmpty(Activity.Peak))
                errors.Add("Activity peak date is required");

            if (string.IsNullOrEmpty(Activity.Finish))
                errors.Add("Activity finish date is required");

            if (Activity.ZHR <= 0)
                errors.Add("Expected ZHR must be positive");

            if (RadiantRA < 0 || RadiantRA >= 360)
                errors.Add("Radiant RA must be between 0 and 360 degrees");

            if (RadiantDec < -90 || RadiantDec > 90)
                errors.Add("Radiant declination must be between -90 and 90 degrees");

            if (SpeedKmS <= 0)
                errors.Add("Speed must be positive");

            return errors.Count == 0;
        }

        /// <summary>
        /// Calculates visibility for given location and date
        /// </summary>
        public VisibilityResult CalculateVisibility(double latitude, double longitude, DateTime date)
        {
            var result = new VisibilityResult
            {
                Date = date,
                Latitude = latitude,
                Longitude = longitude,
                IsVisible = false,
                BestViewingHours = new List<int>(),
                Reasoning = "Shower not active"
            };

            // Check if shower is active
            if (!Activity.IsActiveOn(date))
            {
                return result;
            }

            // Get radiant position
            var radiantPos = ToMeteorShower().GetRadiantPosition(date, latitude, longitude);
            result.IsVisible = radiantPos.IsVisible;

            if (result.IsVisible)
            {
                result.Reasoning = "Shower is active and radiant is visible";

                // Calculate optimality based on days from peak
                var daysFromPeak = GetDaysFromPeak();
                if (daysFromPeak <= 1)
                {
                    result.Reasoning = "Peak activity period - excellent visibility";
                    result.OptimalityScore = 1.0;
                }
                else
                {
                    result.OptimalityScore = Math.Max(0.1, 1.0 - (daysFromPeak / 10.0));
                }

                // Best viewing is typically after midnight
                result.BestViewingHours = new List<int> { 1, 2, 3, 4, 5 };

                // Adjust for moon phase
                if (MoonPhase > 0.7)
                {
                    result.Reasoning += " - moon interference expected";
                    result.OptimalityScore *= 0.6;
                }
            }
            else
            {
                result.Reasoning = "Radiant below horizon";
            }

            return result;
        }

        /// <summary>
        /// Gets photography recommendations
        /// </summary>
        public PhotographyRecommendations GetPhotographyRecommendations()
        {
            var recommendations = new PhotographyRecommendations();
            var currentZHR = Activity.GetExpectedZHR(ObservationDate);

            // Camera settings based on shower characteristics
            if (currentZHR >= 100)
            {
                recommendations.ExposureTime = 15; // Shorter to avoid overexposure
                recommendations.ISO = 1600;
                recommendations.Aperture = "f/2.8";
            }
            else if (currentZHR >= 50)
            {
                recommendations.ExposureTime = 20;
                recommendations.ISO = 3200;
                recommendations.Aperture = "f/2.8";
            }
            else
            {
                recommendations.ExposureTime = 30;
                recommendations.ISO = 6400;
                recommendations.Aperture = "f/2.8";
            }

            // Lens recommendations
            recommendations.RecommendedFocalLength = "14-24mm";
            recommendations.LensType = "Wide angle";

            // Session recommendations
            recommendations.SessionDuration = 2; // hours
            recommendations.FrameInterval = recommendations.ExposureTime + 5; // seconds between frames

            // Additional tips
            recommendations.Tips.Add("Use a sturdy tripod");
            recommendations.Tips.Add("Focus on infinity");
            recommendations.Tips.Add("Use interval timer");
            recommendations.Tips.Add("Point camera toward radiant");

            if (SpeedKmS > 50)
                recommendations.Tips.Add("Fast meteors - use shorter exposures");
            else
                recommendations.Tips.Add("Slow meteors - longer exposures acceptable");

            return recommendations;
        }

        /// <summary>
        /// Compares with another shower for planning
        /// </summary>
        public ShowerComparisonResult CompareTo(MeteorShowerTestModel other)
        {
            var result = new ShowerComparisonResult
            {
                ZHRComparison = CompareZHR(other),
                TimingComparison = CompareTiming(other),
                OverallRecommendation = "Both showers offer different opportunities"
            };

            // Determine which is better for photography
            var thisZHR = Activity.GetExpectedZHR(ObservationDate);
            var otherZHR = other.Activity.GetExpectedZHR(other.ObservationDate);

            if (thisZHR > otherZHR * 1.5)
                result.OverallRecommendation = $"{Designation} is significantly better for photography";
            else if (otherZHR > thisZHR * 1.5)
                result.OverallRecommendation = $"{other.Designation} is significantly better for photography";

            return result;
        }

        /// <summary>
        /// Gets optimal shooting window around peak
        /// </summary>
        public ShootingWindow GetOptimalShootingWindow()
        {
            var peakDate = GetPeakDate();
            return new ShootingWindow
            {
                StartDate = peakDate.AddDays(-1),
                EndDate = peakDate.AddDays(1),
                PeakDate = peakDate,
                ExpectedZHR = Activity.ZHR,
                OptimalHours = new List<int> { 1, 2, 3, 4, 5 },
                Description = $"Peak activity window for {Designation}"
            };
        }

        /// <summary>
        /// Checks if shower conflicts with another
        /// </summary>
        public bool ConflictsWith(MeteorShowerTestModel other)
        {
            // Check for overlapping activity periods by testing sample dates
            var thisStart = GetActivityStartDate();
            var thisEnd = GetActivityEndDate();
            var otherStart = other.GetActivityStartDate();
            var otherEnd = other.GetActivityEndDate();

            return !(thisEnd < otherStart || thisStart > otherEnd);
        }

        /// <summary>
        /// Gets days from peak for current observation date
        /// </summary>
        public double GetDaysFromPeak()
        {
            var peakDate = GetPeakDate();
            return Math.Abs((ObservationDate - peakDate).TotalDays);
        }

        /// <summary>
        /// Gets peak date for current year
        /// </summary>
        public DateTime GetPeakDate()
        {
            var parts = Activity.Peak.Split('-');
            if (parts.Length == 2 && int.TryParse(parts[0], out var month) && int.TryParse(parts[1], out var day))
            {
                return new DateTime(ObservationDate.Year, month, day);
            }
            return ObservationDate;
        }

        /// <summary>
        /// Gets activity start date for current year
        /// </summary>
        public DateTime GetActivityStartDate()
        {
            var parts = Activity.Start.Split('-');
            if (parts.Length == 2 && int.TryParse(parts[0], out var month) && int.TryParse(parts[1], out var day))
            {
                return new DateTime(ObservationDate.Year, month, day);
            }
            return ObservationDate;
        }

        /// <summary>
        /// Gets activity end date for current year
        /// </summary>
        public DateTime GetActivityEndDate()
        {
            var parts = Activity.Finish.Split('-');
            if (parts.Length == 2 && int.TryParse(parts[0], out var month) && int.TryParse(parts[1], out var day))
            {
                return new DateTime(ObservationDate.Year, month, day);
            }
            return ObservationDate;
        }

        private string CompareZHR(MeteorShowerTestModel other)
        {
            var thisZHR = Activity.ZHR;
            var otherZHR = other.Activity.ZHR;

            if (thisZHR > otherZHR * 1.2) return "Significantly higher ZHR";
            if (thisZHR < otherZHR * 0.8) return "Significantly lower ZHR";
            return "Similar ZHR";
        }

        private string CompareTiming(MeteorShowerTestModel other)
        {
            var thisPeak = GetPeakDate();
            var otherPeak = other.GetPeakDate();
            var timeDiff = Math.Abs((thisPeak - otherPeak).TotalDays);

            if (timeDiff < 30) return "Similar timing";
            if (timeDiff < 90) return "Different seasons";
            return "Opposite times of year";
        }

        /// <summary>
        /// Creates a test model with default valid values (Perseids)
        /// </summary>
        public static MeteorShowerTestModel CreatePerseids()
        {
            return new MeteorShowerTestModel
            {
                Id = 1,
                Code = "PER",
                Designation = "Perseids",
                Activity = new MeteorShowerActivity
                {
                    Start = "07-17",
                    Peak = "08-12",
                    Finish = "08-24",
                    ZHR = 100
                },
                RadiantRA = 46.0,
                RadiantDec = 58.0,
                SpeedKmS = 59,
                ParentBody = "109P/Swift-Tuttle",
                ObservationDate = new DateTime(2024, 8, 12),
                MinZHR = 20,
                MoonPhase = 0.3
            };
        }

        /// <summary>
        /// Creates a test model with default valid values (Geminids)
        /// </summary>
        public static MeteorShowerTestModel CreateGeminids()
        {
            return new MeteorShowerTestModel
            {
                Id = 2,
                Code = "GEM",
                Designation = "Geminids",
                Activity = new MeteorShowerActivity
                {
                    Start = "12-04",
                    Peak = "12-14",
                    Finish = "12-20",
                    ZHR = 120
                },
                RadiantRA = 112.0,
                RadiantDec = 33.0,
                SpeedKmS = 35,
                ParentBody = "3200 Phaethon",
                ObservationDate = new DateTime(2024, 12, 14),
                MinZHR = 20,
                MoonPhase = 0.8
            };
        }

        /// <summary>
        /// Creates a test model with invalid values for testing validation
        /// </summary>
        public static MeteorShowerTestModel CreateInvalid()
        {
            return new MeteorShowerTestModel
            {
                Code = "",
                Designation = "",
                Activity = new MeteorShowerActivity
                {
                    Start = "",
                    Peak = "",
                    Finish = "",
                    ZHR = -10 // Invalid: negative ZHR
                },
                RadiantRA = 400, // Invalid: > 360
                RadiantDec = 100, // Invalid: > 90
                SpeedKmS = -1 // Invalid: negative speed
            };
        }
    }

    public class VisibilityResult
    {
        public DateTime Date { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public bool IsVisible { get; set; }
        public List<int> BestViewingHours { get; set; } = new();
        public double OptimalityScore { get; set; }
        public string Reasoning { get; set; } = string.Empty;
    }

    public class PhotographyRecommendations
    {
        public int ExposureTime { get; set; }
        public int ISO { get; set; }
        public string Aperture { get; set; } = string.Empty;
        public string RecommendedFocalLength { get; set; } = string.Empty;
        public string LensType { get; set; } = string.Empty;
        public int SessionDuration { get; set; }
        public int FrameInterval { get; set; }
        public List<string> Tips { get; set; } = new();
    }

    public class ShowerComparisonResult
    {
        public string ZHRComparison { get; set; } = string.Empty;
        public string TimingComparison { get; set; } = string.Empty;
        public string OverallRecommendation { get; set; } = string.Empty;
    }

    public class ShootingWindow
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime PeakDate { get; set; }
        public int ExpectedZHR { get; set; }
        public List<int> OptimalHours { get; set; } = new();
        public string Description { get; set; } = string.Empty;
    }
}