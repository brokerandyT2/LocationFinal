using Location.Photography.Application.Commands.CameraEvaluation;
using Location.Photography.Domain.Enums;

namespace Location.Photography.BDD.Tests.Models
{
    /// <summary>
    /// Test model for camera body management scenarios
    /// </summary>
    public class CameraBodyTestModel
    {
        public int? Id { get; set; }

        // Basic camera specifications from actual CameraBodyDto
        public string Name { get; set; } = string.Empty;
        public string SensorType { get; set; } = string.Empty;
        public double SensorWidth { get; set; }
        public double SensorHeight { get; set; }
        public MountType MountType { get; set; }
        public bool IsUserCreated { get; set; }
        public DateTime DateAdded { get; set; }
        public string DisplayName { get; set; } = string.Empty;

        // Additional test-specific properties for extended scenarios
        public int ResolutionWidth { get; set; }
        public int ResolutionHeight { get; set; }
        public double PixelPitch { get; set; }
        public string IsoRange { get; set; } = string.Empty;

        // Additional specifications for testing
        public double CropFactor { get; set; } = 1.0;
        public int MaxUsableIso { get; set; }
        public string VideoCapability { get; set; } = string.Empty;
        public bool WeatherSealing { get; set; }
        public bool InBodyStabilization { get; set; }

        // Calculated properties
        public double AspectRatio => SensorHeight > 0 ? SensorWidth / SensorHeight : 0;
        public double MegapixelCount => (ResolutionWidth * ResolutionHeight) / 1000000.0;
        public double PixelDensity => ResolutionWidth > 0 && SensorWidth > 0 ? ResolutionWidth / SensorWidth : 0;

        // Test validation properties
        public bool IsValidSpecifications =>
            !string.IsNullOrEmpty(Name) &&
            !string.IsNullOrEmpty(SensorType) &&
            SensorWidth > 0 &&
            SensorHeight > 0;

        public bool IsFullFrame => Math.Abs(SensorWidth - 36.0) < 1.0 && Math.Abs(SensorHeight - 24.0) < 1.0;
        public bool IsAPSC => SensorWidth >= 20.0 && SensorWidth <= 25.0 && SensorHeight >= 13.0 && SensorHeight <= 17.0;
        public bool IsMicroFourThirds => SensorWidth >= 17.0 && SensorWidth <= 18.0 && SensorHeight >= 13.0 && SensorHeight <= 14.0;

        // Error handling
        public string ErrorMessage { get; set; } = string.Empty;
        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        // Test metadata
        public string Notes { get; set; } = string.Empty;

        /// <summary>
        /// Creates a CameraBodyDto from this test model
        /// </summary>
        public CameraBodyDto ToCameraBodyDto()
        {
            return new CameraBodyDto
            {
                Id = Id ?? 0,
                Name = Name,
                SensorType = SensorType,
                SensorWidth = SensorWidth,
                SensorHeight = SensorHeight,
                MountType = MountType,
                IsUserCreated = IsUserCreated,
                DateAdded = DateAdded,
                DisplayName = DisplayName
            };
        }

        /// <summary>
        /// Updates this model from a CameraBodyDto
        /// </summary>
        public void UpdateFromDto(CameraBodyDto dto)
        {
            Id = dto.Id;
            Name = dto.Name;
            SensorType = dto.SensorType;
            SensorWidth = dto.SensorWidth;
            SensorHeight = dto.SensorHeight;
            MountType = dto.MountType;
            IsUserCreated = dto.IsUserCreated;
            DateAdded = dto.DateAdded;
            DisplayName = dto.DisplayName;
        }

        /// <summary>
        /// Validates camera body specifications
        /// </summary>
        public bool ValidateSpecifications(out List<string> errors)
        {
            errors = new List<string>();

            if (string.IsNullOrEmpty(Name))
                errors.Add("Camera name is required");

            if (string.IsNullOrEmpty(SensorType))
                errors.Add("Sensor type is required");

            if (SensorWidth <= 0)
                errors.Add("Sensor width must be positive");

            if (SensorHeight <= 0)
                errors.Add("Sensor height must be positive");

            // Validate ISO range format if provided
            if (!string.IsNullOrEmpty(IsoRange) && !ValidateIsoRangeFormat(IsoRange))
                errors.Add("ISO range format is invalid (expected format: '100-51200')");

            // Validate aspect ratio reasonableness
            if (AspectRatio < 1.0 || AspectRatio > 2.0)
                errors.Add("Sensor aspect ratio is outside reasonable range (1.0-2.0)");

            return errors.Count == 0;
        }

        /// <summary>
        /// Calculates crop factor relative to full frame
        /// </summary>
        public double CalculateCropFactor()
        {
            const double fullFrameDiagonal = 43.27; // mm
            var sensorDiagonal = Math.Sqrt(SensorWidth * SensorWidth + SensorHeight * SensorHeight);
            return sensorDiagonal > 0 ? fullFrameDiagonal / sensorDiagonal : 1.0;
        }

        /// <summary>
        /// Determines sensor format category
        /// </summary>
        public string DetermineSensorFormat()
        {
            if (IsFullFrame) return "Full Frame";
            if (IsAPSC) return "APS-C";
            if (IsMicroFourThirds) return "Micro 4/3";

            // Additional format detection
            if (SensorWidth < 10.0) return "1/2.3 inch";
            if (SensorWidth < 15.0) return "1 inch";
            if (SensorWidth < 20.0) return "APS-C";

            return "Unknown";
        }

        /// <summary>
        /// Compares sensor size with another camera
        /// </summary>
        public string CompareSensorSize(CameraBodyTestModel other)
        {
            var thisSensorArea = SensorWidth * SensorHeight;
            var otherSensorArea = other.SensorWidth * other.SensorHeight;

            var ratio = thisSensorArea / otherSensorArea;

            if (ratio > 1.1) return "Larger";
            if (ratio < 0.9) return "Smaller";
            return "Similar";
        }

        /// <summary>
        /// Gets photography type recommendations based on specs
        /// </summary>
        public List<string> GetRecommendedPhotographyTypes()
        {
            var recommendations = new List<string>();

            // High ISO performance recommendations
            if (MaxUsableIso >= 6400)
            {
                recommendations.Add("Low Light Photography");
                recommendations.Add("Astrophotography");
                recommendations.Add("Wedding Photography");
            }

            // High resolution recommendations
            if (MegapixelCount >= 40)
            {
                recommendations.Add("Landscape Photography");
                recommendations.Add("Studio Photography");
                recommendations.Add("Commercial Photography");
            }

            // Video capability recommendations
            if (VideoCapability.Contains("8K"))
            {
                recommendations.Add("Professional Video");
                recommendations.Add("Content Creation");
            }
            else if (VideoCapability.Contains("4K"))
            {
                recommendations.Add("Video Production");
                recommendations.Add("Social Media Content");
            }

            // Weather sealing recommendations
            if (WeatherSealing)
            {
                recommendations.Add("Outdoor Photography");
                recommendations.Add("Adventure Photography");
                recommendations.Add("Wildlife Photography");
            }

            // Crop factor recommendations
            if (CropFactor > 1.5)
            {
                recommendations.Add("Wildlife Photography");
                recommendations.Add("Sports Photography");
                recommendations.Add("Macro Photography");
            }

            return recommendations;
        }

        /// <summary>
        /// Validates ISO range format
        /// </summary>
        private bool ValidateIsoRangeFormat(string isoRange)
        {
            if (string.IsNullOrEmpty(isoRange)) return false;

            var parts = isoRange.Split('-');
            if (parts.Length != 2) return false;

            return int.TryParse(parts[0], out _) && int.TryParse(parts[1], out _);
        }

        /// <summary>
        /// Creates a test model with default valid values
        /// </summary>
        public static CameraBodyTestModel CreateValid(int? id = null)
        {
            return new CameraBodyTestModel
            {
                Id = id,
                Name = "Canon EOS R5",
                SensorType = "CMOS",
                SensorWidth = 36.0,
                SensorHeight = 24.0,
                MountType = MountType.CanonRF,
                IsUserCreated = true,
                DateAdded = DateTime.UtcNow,
                DisplayName = "Canon EOS R5",
                ResolutionWidth = 8192,
                ResolutionHeight = 5464,
                PixelPitch = 4.39,
                IsoRange = "100-51200",
                CropFactor = 1.0,
                MaxUsableIso = 6400,
                VideoCapability = "8K",
                WeatherSealing = true,
                InBodyStabilization = true
            };
        }

        /// <summary>
        /// Creates a test model with invalid values for testing validation
        /// </summary>
        public static CameraBodyTestModel CreateInvalid()
        {
            return new CameraBodyTestModel
            {
                Name = "",
                SensorType = "",
                SensorWidth = 0,
                SensorHeight = 0,
                MountType = MountType.Other,
                ResolutionWidth = -1,
                ResolutionHeight = -1,
                PixelPitch = 0,
                IsoRange = "invalid-range"
            };
        }
    }
}