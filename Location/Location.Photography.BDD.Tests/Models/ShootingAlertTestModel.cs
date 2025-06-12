using Location.Photography.Application.Services;
using Location.Photography.Domain.Models;

namespace Location.Photography.BDD.Tests.Models
{
    /// <summary>
    /// Test model for shooting alerts and notifications scenarios
    /// </summary>
    public class ShootingAlertTestModel
    {
        public int? Id { get; set; }
        public AlertType AlertType { get; set; }
        public AlertSeverity Severity { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime ValidFrom { get; set; }
        public DateTime ValidTo { get; set; }
        public int LocationId { get; set; }
        public DateTime AlertTime { get; set; }
        public DateTime ShootingWindowStart { get; set; }
        public DateTime ShootingWindowEnd { get; set; }
        public LightQuality LightQuality { get; set; }
        public string? RecommendedSettings { get; set; }

        // Additional test properties
        public string UserId { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public bool HasBeenSent { get; set; }
        public DateTime? SentAt { get; set; }
        public string NotificationMethod { get; set; } = string.Empty;
        public List<string> NotificationChannels { get; set; } = new();

        // User preferences
        public AlertSeverity MinimumSeverity { get; set; } = AlertSeverity.Info;
        public bool AlertsEnabled { get; set; } = true;
        public List<AlertType> EnabledAlertTypes { get; set; } = new();

        // Error handling
        public string ErrorMessage { get; set; } = string.Empty;
        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        // Validation properties
        public bool IsValidAlert =>
            !string.IsNullOrEmpty(Message) &&
            ValidFrom < ValidTo &&
            LocationId > 0;

        public bool IsCurrentlyActive =>
            IsActive &&
            DateTime.UtcNow >= ValidFrom &&
            DateTime.UtcNow <= ValidTo;

        public TimeSpan Duration => ValidTo - ValidFrom;
        public TimeSpan TimeUntilExpiry => ValidTo - DateTime.UtcNow;
        public bool IsExpired => DateTime.UtcNow > ValidTo;

        /// <summary>
        /// Creates a WeatherAlert from this test model
        /// </summary>
        public WeatherAlert ToWeatherAlert()
        {
            return new WeatherAlert
            {
                Type = AlertType,
                Message = Message,
                ValidFrom = ValidFrom,
                ValidTo = ValidTo,
                Severity = Severity
            };
        }

        /// <summary>
        /// Creates a ShootingAlertRequest from this test model
        /// </summary>
        public ShootingAlertRequest ToShootingAlertRequest()
        {
            return new ShootingAlertRequest
            {
                LocationId = LocationId,
                AlertTime = AlertTime,
                ShootingWindowStart = ShootingWindowStart,
                ShootingWindowEnd = ShootingWindowEnd,
                LightQuality = LightQuality,
                RecommendedSettings = RecommendedSettings,
                Message = Message
            };
        }

        /// <summary>
        /// Updates this model from a WeatherAlert
        /// </summary>
        public void UpdateFromWeatherAlert(WeatherAlert alert)
        {
            AlertType = alert.Type;
            Message = alert.Message;
            ValidFrom = alert.ValidFrom;
            ValidTo = alert.ValidTo;
            Severity = alert.Severity;
        }

        /// <summary>
        /// Updates this model from a ShootingAlertRequest
        /// </summary>
        public void UpdateFromShootingAlertRequest(ShootingAlertRequest request)
        {
            LocationId = request.LocationId;
            AlertTime = request.AlertTime;
            ShootingWindowStart = request.ShootingWindowStart;
            ShootingWindowEnd = request.ShootingWindowEnd;
            LightQuality = request.LightQuality;
            RecommendedSettings = request.RecommendedSettings;
            Message = request.Message;
        }

        /// <summary>
        /// Validates alert data
        /// </summary>
        public bool ValidateAlertData(out List<string> errors)
        {
            errors = new List<string>();

            if (string.IsNullOrEmpty(Message))
                errors.Add("Alert message is required");

            if (ValidFrom >= ValidTo)
                errors.Add("Valid from date must be before valid to date");

            if (LocationId <= 0)
                errors.Add("Valid location ID is required");

            if (AlertType == AlertType.Shooting)
            {
                if (ShootingWindowStart >= ShootingWindowEnd)
                    errors.Add("Shooting window start must be before end");

                if (AlertTime > ShootingWindowStart)
                    errors.Add("Alert time should be before shooting window starts");
            }

            if (Message.Length > 500)
                errors.Add("Alert message cannot exceed 500 characters");

            return errors.Count == 0;
        }

        /// <summary>
        /// Checks if alert should be sent based on user preferences
        /// </summary>
        public bool ShouldSendAlert(ShootingAlertTestModel userPreferences)
        {
            // Check if alerts are enabled
            if (!userPreferences.AlertsEnabled)
                return false;

            // Check severity threshold
            if (Severity < userPreferences.MinimumSeverity)
                return false;

            // Check if alert type is enabled
            if (!userPreferences.EnabledAlertTypes.Contains(AlertType))
                return false;

            // Check if alert is currently active
            if (!IsCurrentlyActive)
                return false;

            // Check if already sent
            if (HasBeenSent)
                return false;

            return true;
        }

        /// <summary>
        /// Marks alert as sent
        /// </summary>
        public void MarkAsSent(string notificationMethod = "")
        {
            HasBeenSent = true;
            SentAt = DateTime.UtcNow;
            if (!string.IsNullOrEmpty(notificationMethod))
                NotificationMethod = notificationMethod;
        }

        /// <summary>
        /// Gets alert priority score for sorting
        /// </summary>
        public int GetPriorityScore()
        {
            var score = 0;

            // Severity contributes most to priority
            score += Severity switch
            {
                AlertSeverity.Critical => 100,
                AlertSeverity.Warning => 50,
                AlertSeverity.Info => 10
            };

            // Alert type priority
            score += AlertType switch
            {
                AlertType.Weather => 30,
                AlertType.Shooting => 20,
                AlertType.Light => 15,
                AlertType.Calibration => 5
            };

            // Time sensitivity (closer alerts get higher priority)
            var hoursUntilExpiry = TimeUntilExpiry.TotalHours;
            if (hoursUntilExpiry < 1)
                score += 20;
            else if (hoursUntilExpiry < 6)
                score += 10;
            else if (hoursUntilExpiry < 24)
                score += 5;

            return score;
        }

        /// <summary>
        /// Gets recommended notification channels based on alert type and severity
        /// </summary>
        public List<string> GetRecommendedNotificationChannels()
        {
            var channels = new List<string>();

            switch (Severity)
            {
                case AlertSeverity.Critical:
                    channels.Add("Push");
                    channels.Add("Email");
                    channels.Add("SMS");
                    break;
                case AlertSeverity.Warning:
                    channels.Add("Push");
                    channels.Add("Email");
                    break;
                case AlertSeverity.Info:
                    channels.Add("Push");
                    break;
            }

            // Shooting alerts get additional notification options
            if (AlertType == AlertType.Shooting)
            {
                channels.Add("In-App");
            }

            return channels;
        }

        /// <summary>
        /// Generates alert summary for display
        /// </summary>
        public string GenerateAlertSummary()
        {
            var summary = $"{Severity} {AlertType} Alert";

            if (AlertType == AlertType.Shooting)
            {
                var windowDuration = ShootingWindowEnd - ShootingWindowStart;
                summary += $" - {LightQuality} light for {windowDuration.TotalHours:F1}h";
            }

            if (IsExpired)
                summary += " (Expired)";
            else if (IsCurrentlyActive)
                summary += " (Active)";

            return summary;
        }

        /// <summary>
        /// Checks if alert conflicts with another alert
        /// </summary>
        public bool ConflictsWith(ShootingAlertTestModel other)
        {
            // Check time overlap
            if (ValidTo <= other.ValidFrom || ValidFrom >= other.ValidTo)
                return false;

            // Check location conflict
            if (LocationId != other.LocationId)
                return false;

            // Shooting alerts conflict if they overlap
            if (AlertType == AlertType.Shooting && other.AlertType == AlertType.Shooting)
            {
                return !(ShootingWindowEnd <= other.ShootingWindowStart ||
                        ShootingWindowStart >= other.ShootingWindowEnd);
            }

            return false;
        }

        /// <summary>
        /// Creates alert for weather conditions
        /// </summary>
        public static ShootingAlertTestModel CreateWeatherAlert(int locationId, AlertSeverity severity = AlertSeverity.Warning)
        {
            return new ShootingAlertTestModel
            {
                Id = 1,
                AlertType = AlertType.Weather,
                Severity = severity,
                Message = severity == AlertSeverity.Critical ? "Severe weather warning" : "Weather alert",
                ValidFrom = DateTime.UtcNow,
                ValidTo = DateTime.UtcNow.AddHours(6),
                LocationId = locationId,
                IsActive = true,
                AlertsEnabled = true,
                EnabledAlertTypes = new List<AlertType> { AlertType.Weather, AlertType.Shooting }
            };
        }

        /// <summary>
        /// Creates alert for optimal shooting conditions
        /// </summary>
        public static ShootingAlertTestModel CreateShootingAlert(int locationId, LightQuality lightQuality = LightQuality.GoldenHour)
        {
            var startTime = DateTime.Today.AddHours(7);
            return new ShootingAlertTestModel
            {
                Id = 2,
                AlertType = AlertType.Shooting,
                Severity = AlertSeverity.Info,
                Message = $"Excellent {lightQuality} light conditions predicted",
                ValidFrom = DateTime.UtcNow,
                ValidTo = startTime.AddHours(2),
                LocationId = locationId,
                AlertTime = startTime.AddHours(-1),
                ShootingWindowStart = startTime,
                ShootingWindowEnd = startTime.AddHours(1),
                LightQuality = lightQuality,
                RecommendedSettings = "f/8, 1/125s, ISO 200",
                IsActive = true,
                AlertsEnabled = true,
                EnabledAlertTypes = new List<AlertType> { AlertType.Shooting, AlertType.Light }
            };
        }

        /// <summary>
        /// Creates calibration reminder alert
        /// </summary>
        public static ShootingAlertTestModel CreateCalibrationAlert(int locationId)
        {
            return new ShootingAlertTestModel
            {
                Id = 3,
                AlertType = AlertType.Calibration,
                Severity = AlertSeverity.Warning,
                Message = "Light meter requires recalibration",
                ValidFrom = DateTime.UtcNow,
                ValidTo = DateTime.UtcNow.AddDays(1),
                LocationId = locationId,
                IsActive = true,
                AlertsEnabled = true,
                EnabledAlertTypes = new List<AlertType> { AlertType.Calibration }
            };
        }

        /// <summary>
        /// Creates user preferences for testing
        /// </summary>
        public static ShootingAlertTestModel CreateUserPreferences(bool alertsEnabled = true, AlertSeverity minimumSeverity = AlertSeverity.Info)
        {
            return new ShootingAlertTestModel
            {
                UserId = "user123",
                AlertsEnabled = alertsEnabled,
                MinimumSeverity = minimumSeverity,
                EnabledAlertTypes = new List<AlertType>
                {
                    AlertType.Weather,
                    AlertType.Shooting,
                    AlertType.Light,
                    AlertType.Calibration
                },
                NotificationChannels = new List<string> { "Push", "Email" }
            };
        }

        /// <summary>
        /// Creates a test model with invalid values for testing validation
        /// </summary>
        public static ShootingAlertTestModel CreateInvalid()
        {
            return new ShootingAlertTestModel
            {
                Message = "", // Invalid: empty message
                ValidFrom = DateTime.UtcNow,
                ValidTo = DateTime.UtcNow.AddHours(-1), // Invalid: end before start
                LocationId = 0, // Invalid: no location
                ShootingWindowStart = DateTime.Today.AddHours(8),
                ShootingWindowEnd = DateTime.Today.AddHours(7), // Invalid: end before start
                AlertTime = DateTime.Today.AddHours(9) // Invalid: alert after shooting window
            };
        }
    }
}