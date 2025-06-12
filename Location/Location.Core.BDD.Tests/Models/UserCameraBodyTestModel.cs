using Location.Photography.Application.Commands.CameraEvaluation;
using Location.Photography.Domain.Entities;
using Location.Photography.Domain.Enums;

namespace Location.Photography.BDD.Tests.Models
{
    /// <summary>
    /// Test model for user camera body management scenarios
    /// </summary>
    public class UserCameraBodyTestModel
    {
        public int? Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public int CameraBodyId { get; set; }
        public bool IsFavorite { get; set; }
        public string Notes { get; set; } = string.Empty;
        public DateTime DateSaved { get; set; } = DateTime.UtcNow;

        // Camera body details for testing (denormalized for convenience)
        public string CameraName { get; set; } = string.Empty;
        public string SensorType { get; set; } = string.Empty;
        public double SensorWidth { get; set; }
        public double SensorHeight { get; set; }
        public MountType MountType { get; set; }
        public bool IsUserCreated { get; set; }

        // Additional test properties
        public string CustomName { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new();
        public int UsageCount { get; set; }
        public double Rating { get; set; }
        public DateTime? LastUsed { get; set; }

        // Error handling
        public string ErrorMessage { get; set; } = string.Empty;
        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        // Validation properties
        public bool IsValidForSaving =>
            !string.IsNullOrEmpty(UserId) &&
            CameraBodyId > 0;

        public bool HasBeenUsed => LastUsed.HasValue;
        public TimeSpan? TimeSinceLastUse => LastUsed.HasValue ? DateTime.UtcNow - LastUsed.Value : null;
        public TimeSpan TimeOwned => DateTime.UtcNow - DateSaved;

        /// <summary>
        /// Creates a UserCameraBody entity from this test model
        /// </summary>
        public UserCameraBody ToUserCameraBody()
        {
            return new UserCameraBody
            {
                Id = Id ?? 0,
                UserId = UserId,
                CameraBodyId = CameraBodyId,
                IsFavorite = IsFavorite,
                Notes = Notes,
                DateSaved = DateSaved
            };
        }

        /// <summary>
        /// Updates this model from a UserCameraBody entity
        /// </summary>
        public void UpdateFromEntity(UserCameraBody entity)
        {
            Id = entity.Id;
            UserId = entity.UserId;
            CameraBodyId = entity.CameraBodyId;
            IsFavorite = entity.IsFavorite;
            Notes = entity.Notes;
            DateSaved = entity.DateSaved;
        }

        /// <summary>
        /// Updates camera details from CameraBodyDto
        /// </summary>
        public void UpdateCameraDetails(CameraBodyDto cameraDto)
        {
            CameraName = cameraDto.Name;
            SensorType = cameraDto.SensorType;
            SensorWidth = cameraDto.SensorWidth;
            SensorHeight = cameraDto.SensorHeight;
            MountType = cameraDto.MountType;
            IsUserCreated = cameraDto.IsUserCreated;
        }

        /// <summary>
        /// Validates user camera body data
        /// </summary>
        public bool ValidateForSaving(out List<string> errors)
        {
            errors = new List<string>();

            if (string.IsNullOrEmpty(UserId))
                errors.Add("User ID is required");

            if (CameraBodyId <= 0)
                errors.Add("Valid camera body ID is required");

            if (Rating < 0 || Rating > 5)
                errors.Add("Rating must be between 0 and 5");

            if (Notes.Length > 1000)
                errors.Add("Notes cannot exceed 1000 characters");

            return errors.Count == 0;
        }

        /// <summary>
        /// Calculates usage statistics
        /// </summary>
        public UserCameraStats GetUsageStats()
        {
            return new UserCameraStats
            {
                DaysOwned = (int)TimeOwned.TotalDays,
                UsageCount = UsageCount,
                DaysSinceLastUse = TimeSinceLastUse?.TotalDays ?? 0,
                IsFavorite = IsFavorite,
                HasNotes = !string.IsNullOrEmpty(Notes),
                Rating = Rating
            };
        }

        /// <summary>
        /// Gets compatibility information for lenses
        /// </summary>
        public List<string> GetCompatibleLensMounts()
        {
            var compatibleMounts = new List<string> { MountType.ToString() };

            // Add adapter compatibility
            switch (MountType)
            {
                case MountType.CanonRF:
                    compatibleMounts.Add("Canon EF (with adapter)");
                    compatibleMounts.Add("Canon EF-S (with adapter)");
                    break;
                case MountType.NikonZ:
                    compatibleMounts.Add("Nikon F (with adapter)");
                    break;
                case MountType.SonyFE:
                    compatibleMounts.Add("Sony E");
                    break;
            }

            return compatibleMounts;
        }

        /// <summary>
        /// Gets recommended photography types based on camera specs
        /// </summary>
        public List<string> GetRecommendedUses()
        {
            var recommendations = new List<string>();

            // Full frame recommendations
            if (Math.Abs(SensorWidth - 36.0) < 1.0)
            {
                recommendations.Add("Portrait Photography");
                recommendations.Add("Landscape Photography");
                recommendations.Add("Low Light Photography");
            }

            // Crop sensor recommendations
            if (SensorWidth < 30.0)
            {
                recommendations.Add("Wildlife Photography");
                recommendations.Add("Sports Photography");
                recommendations.Add("Travel Photography");
            }

            return recommendations;
        }

        /// <summary>
        /// Compares with another user camera for upgrade analysis
        /// </summary>
        public CameraComparisonResult CompareTo(UserCameraBodyTestModel other)
        {
            var result = new CameraComparisonResult
            {
                SensorSizeComparison = CompareSensorSize(other),
                AgeComparison = CompareAge(other),
                UsageComparison = CompareUsage(other),
                OverallRecommendation = "Keep both cameras for different purposes"
            };

            return result;
        }

        /// <summary>
        /// Marks camera as recently used
        /// </summary>
        public void MarkAsUsed()
        {
            LastUsed = DateTime.UtcNow;
            UsageCount++;
        }

        /// <summary>
        /// Adds a tag to the camera
        /// </summary>
        public void AddTag(string tag)
        {
            if (!string.IsNullOrEmpty(tag) && !Tags.Contains(tag))
            {
                Tags.Add(tag);
            }
        }

        /// <summary>
        /// Removes a tag from the camera
        /// </summary>
        public void RemoveTag(string tag)
        {
            Tags.Remove(tag);
        }

        private string CompareSensorSize(UserCameraBodyTestModel other)
        {
            var thisArea = SensorWidth * SensorHeight;
            var otherArea = other.SensorWidth * other.SensorHeight;

            if (thisArea > otherArea * 1.1) return "Larger sensor";
            if (thisArea < otherArea * 0.9) return "Smaller sensor";
            return "Similar sensor size";
        }

        private string CompareAge(UserCameraBodyTestModel other)
        {
            if (DateSaved < other.DateSaved.AddDays(-30)) return "Older camera";
            if (DateSaved > other.DateSaved.AddDays(30)) return "Newer camera";
            return "Similar age";
        }

        private string CompareUsage(UserCameraBodyTestModel other)
        {
            if (UsageCount > other.UsageCount * 1.5) return "More frequently used";
            if (UsageCount < other.UsageCount * 0.5) return "Less frequently used";
            return "Similar usage patterns";
        }

        /// <summary>
        /// Creates a test model with default valid values
        /// </summary>
        public static UserCameraBodyTestModel CreateValid(string userId = "user123", int cameraBodyId = 1)
        {
            return new UserCameraBodyTestModel
            {
                UserId = userId,
                CameraBodyId = cameraBodyId,
                IsFavorite = false,
                Notes = "Test camera body",
                DateSaved = DateTime.UtcNow.AddDays(-30),
                CameraName = "Canon EOS R5",
                SensorType = "CMOS",
                SensorWidth = 36.0,
                SensorHeight = 24.0,
                MountType = MountType.CanonRF,
                IsUserCreated = false,
                Rating = 4.5,
                UsageCount = 10
            };
        }

        /// <summary>
        /// Creates a test model with invalid values for testing validation
        /// </summary>
        public static UserCameraBodyTestModel CreateInvalid()
        {
            return new UserCameraBodyTestModel
            {
                UserId = "",
                CameraBodyId = 0,
                Rating = 10, // Invalid rating
                Notes = new string('x', 1500) // Too long notes
            };
        }
    }

    public class UserCameraStats
    {
        public int DaysOwned { get; set; }
        public int UsageCount { get; set; }
        public double DaysSinceLastUse { get; set; }
        public bool IsFavorite { get; set; }
        public bool HasNotes { get; set; }
        public double Rating { get; set; }
    }

    public class CameraComparisonResult
    {
        public string SensorSizeComparison { get; set; } = string.Empty;
        public string AgeComparison { get; set; } = string.Empty;
        public string UsageComparison { get; set; } = string.Empty;
        public string OverallRecommendation { get; set; } = string.Empty;
    }
}