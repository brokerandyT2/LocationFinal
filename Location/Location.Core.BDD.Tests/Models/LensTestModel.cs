using Location.Photography.Domain.Entities;
using Location.Photography.Domain.Enums;
using Location.Photography.Application.Commands.CameraEvaluation;

namespace Location.Photography.BDD.Tests.Models
{
    /// <summary>
    /// Test model for lens management scenarios
    /// </summary>
    public class LensTestModel
    {
        public int? Id { get; set; }
        public double MinMM { get; set; }
        public double? MaxMM { get; set; }
        public double? MinFStop { get; set; }
        public double? MaxFStop { get; set; }
        public bool IsPrime { get; set; }
        public bool IsUserCreated { get; set; }
        public DateTime DateAdded { get; set; } = DateTime.UtcNow;
        public string NameForLens { get; set; } = string.Empty;

        // Additional lens properties for testing
        public string Make { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public MountType MountType { get; set; }
        public string LensType { get; set; } = string.Empty;
        public bool HasImageStabilization { get; set; }
        public bool WeatherSealing { get; set; }
        public double MinFocusDistance { get; set; }
        public double MaxMagnification { get; set; }
        public int FilterThreadSize { get; set; }
        public double Weight { get; set; }
        public double Length { get; set; }
        public string OpticalConstruction { get; set; } = string.Empty;

        // Compatibility and recommendations
        public List<int> CompatibleCameraIds { get; set; } = new();
        public List<string> RecommendedFor { get; set; } = new();

        // Error handling
        public string ErrorMessage { get; set; } = string.Empty;
        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        // Validation properties
        public bool IsValidSpecifications =>
            MinMM > 0 &&
            (MaxMM == null || MaxMM >= MinMM) &&
            (MinFStop == null || MinFStop > 0) &&
            (MaxFStop == null || MaxFStop >= MinFStop);

        // Calculated properties
        public bool IsPrimeLens => MaxMM == null || Math.Abs(MinMM - MaxMM.Value) < 0.1;
        public bool IsZoomLens => !IsPrimeLens;
        public double ZoomRatio => IsPrimeLens ? 1.0 : (MaxMM ?? MinMM) / MinMM;
        public string FocalLengthDescription => IsPrimeLens ? $"{MinMM}mm" : $"{MinMM}-{MaxMM}mm";
        public string ApertureDescription => MinFStop == null ? "Variable" :
            (MaxFStop == null || Math.Abs(MinFStop.Value - MaxFStop.Value) < 0.1) ? $"f/{MinFStop}" : $"f/{MinFStop}-{MaxFStop}";
        public string FullName => !string.IsNullOrEmpty(NameForLens) ? NameForLens :
            (!string.IsNullOrEmpty(Make) && !string.IsNullOrEmpty(Model)) ? $"{Make} {Model}" : FocalLengthDescription;

        /// <summary>
        /// Creates a Lens entity from this test model
        /// </summary>
        public Lens ToLens()
        {
            return new Lens(
                MinMM,
                MaxMM,
                MinFStop,
                MaxFStop,
                IsUserCreated,
                NameForLens)
            {
                Id = Id ?? 0,
                DateAdded = DateAdded
            };
        }

        /// <summary>
        /// Updates this model from a Lens entity
        /// </summary>
        public void UpdateFromEntity(Lens entity)
        {
            Id = entity.Id;
            MinMM = entity.MinMM;
            MaxMM = entity.MaxMM;
            MinFStop = entity.MinFStop;
            MaxFStop = entity.MaxFStop;
            IsPrime = entity.IsPrime;
            IsUserCreated = entity.IsUserCreated;
            DateAdded = entity.DateAdded;
            NameForLens = entity.NameForLens ?? string.Empty;
        }

        /// <summary>
        /// Updates this model from a LensDto
        /// </summary>
        public void UpdateFromDto(LensDto dto)
        {
            Id = dto.Id;
            MinMM = dto.MinMM;
            MaxMM = dto.MaxMM;
            MinFStop = dto.MinFStop;
            MaxFStop = dto.MaxFStop;
            IsPrime = dto.IsPrime;
            IsUserCreated = dto.IsUserCreated;
            DateAdded = dto.DateAdded;
            NameForLens = dto.DisplayName;
        }

        /// <summary>
        /// Validates lens specifications
        /// </summary>
        public bool ValidateSpecifications(out List<string> errors)
        {
            errors = new List<string>();

            if (MinMM <= 0)
                errors.Add("Minimum focal length must be positive");

            if (MaxMM.HasValue && MaxMM < MinMM)
                errors.Add("Maximum focal length must be greater than or equal to minimum");

            if (MinFStop.HasValue && MinFStop <= 0)
                errors.Add("Minimum aperture must be positive");

            if (MaxFStop.HasValue && MinFStop.HasValue && MaxFStop < MinFStop)
                errors.Add("Maximum aperture must be greater than or equal to minimum");

            // Validate focal length range reasonableness
            if (MinMM < 8 || (MaxMM.HasValue && MaxMM > 2000))
                errors.Add("Focal length range is outside reasonable bounds (8-2000mm)");

            // Validate aperture range reasonableness
            if (MinFStop.HasValue && (MinFStop < 0.5 || MinFStop > 64))
                errors.Add("Minimum aperture is outside reasonable bounds (f/0.5-f/64)");

            if (MaxFStop.HasValue && (MaxFStop < 0.5 || MaxFStop > 64))
                errors.Add("Maximum aperture is outside reasonable bounds (f/0.5-f/64)");

            return errors.Count == 0;
        }

        /// <summary>
        /// Determines lens category based on focal length
        /// </summary>
        public string DetermineLensCategory()
        {
            var avgFocalLength = MaxMM.HasValue ? (MinMM + MaxMM.Value) / 2 : MinMM;

            return avgFocalLength switch
            {
                < 24 => "Ultra Wide",
                >= 24 and < 35 => "Wide Angle",
                >= 35 and < 85 => "Standard",
                >= 85 and < 200 => "Short Telephoto",
                >= 200 and < 400 => "Telephoto",
                >= 400 => "Super Telephoto"
            };
        }

        /// <summary>
        /// Gets photography type recommendations based on lens specs
        /// </summary>
        public List<string> GetRecommendedPhotographyTypes()
        {
            var recommendations = new List<string>();
            var avgFocalLength = MaxMM.HasValue ? (MinMM + MaxMM.Value) / 2 : MinMM;

            // Focal length based recommendations
            if (avgFocalLength < 35)
            {
                recommendations.Add("Landscape Photography");
                recommendations.Add("Architecture Photography");
                recommendations.Add("Astrophotography");
            }
            else if (avgFocalLength >= 35 && avgFocalLength < 85)
            {
                recommendations.Add("Street Photography");
                recommendations.Add("Documentary Photography");
                recommendations.Add("Environmental Portraits");
            }
            else if (avgFocalLength >= 85 && avgFocalLength < 200)
            {
                recommendations.Add("Portrait Photography");
                recommendations.Add("Event Photography");
                recommendations.Add("Sports Photography");
            }
            else if (avgFocalLength >= 200)
            {
                recommendations.Add("Wildlife Photography");
                recommendations.Add("Sports Photography");
                recommendations.Add("Bird Photography");
            }

            // Aperture based recommendations
            if (MinFStop.HasValue && MinFStop <= 2.8)
            {
                recommendations.Add("Low Light Photography");
                recommendations.Add("Indoor Photography");
                if (avgFocalLength >= 85)
                    recommendations.Add("Portrait Photography");
            }

            // Macro capabilities
            if (MaxMagnification >= 0.5)
            {
                recommendations.Add("Macro Photography");
                recommendations.Add("Close-up Photography");
            }

            // Image stabilization benefits
            if (HasImageStabilization)
            {
                recommendations.Add("Handheld Photography");
                recommendations.Add("Video Recording");
            }

            return recommendations.Distinct().ToList();
        }

        /// <summary>
        /// Calculates field of view for given sensor size
        /// </summary>
        public FieldOfViewResult CalculateFieldOfView(double sensorWidth, double sensorHeight)
        {
            var result = new FieldOfViewResult();

            // Calculate for both min and max focal lengths
            var maxFocal = MaxMM ?? MinMM;

            var horizontalFovMin = 2 * Math.Atan(sensorWidth / (2 * maxFocal)) * 180 / Math.PI;
            var verticalFovMin = 2 * Math.Atan(sensorHeight / (2 * maxFocal)) * 180 / Math.PI;

            var horizontalFovMax = 2 * Math.Atan(sensorWidth / (2 * MinMM)) * 180 / Math.PI;
            var verticalFovMax = 2 * Math.Atan(sensorHeight / (2 * MinMM)) * 180 / Math.PI;

            result.HorizontalFovMin = horizontalFovMin;
            result.VerticalFovMin = verticalFovMin;
            result.HorizontalFovMax = horizontalFovMax;
            result.VerticalFovMax = verticalFovMax;

            return result;
        }

        /// <summary>
        /// Checks compatibility with camera mount type
        /// </summary>
        public bool IsCompatibleWith(MountType cameraMount)
        {
            if (MountType == cameraMount) return true;

            // Check adapter compatibility
            return (MountType, cameraMount) switch
            {
                (MountType.CanonEF, MountType.CanonRF) => true,
                (MountType.CanonEFS, MountType.CanonRF) => true,
                (MountType.NikonF, MountType.NikonZ) => true,
                (MountType.SonyE, MountType.SonyFE) => true,
                _ => false
            };
        }

        /// <summary>
        /// Compares with another lens for upgrade analysis
        /// </summary>
        public LensComparisonResult CompareTo(LensTestModel other)
        {
            var result = new LensComparisonResult
            {
                FocalLengthComparison = CompareFocalLength(other),
                ApertureComparison = CompareAperture(other),
                FeatureComparison = CompareFeatures(other),
                OverallRecommendation = "Both lenses serve different purposes"
            };

            return result;
        }

        /// <summary>
        /// Gets lens mount compatibility list
        /// </summary>
        public List<string> GetCompatibleMounts()
        {
            var compatibleMounts = new List<string> { MountType.ToString() };

            switch (MountType)
            {
                case MountType.CanonEF:
                    compatibleMounts.Add("Canon RF (with adapter)");
                    break;
                case MountType.NikonF:
                    compatibleMounts.Add("Nikon Z (with adapter)");
                    break;
                case MountType.SonyE:
                    compatibleMounts.Add("Sony FE");
                    break;
            }

            return compatibleMounts;
        }

        private string CompareFocalLength(LensTestModel other)
        {
            var thisMax = MaxMM ?? MinMM;
            var otherMax = other.MaxMM ?? other.MinMM;

            if (thisMax < other.MinMM) return "Wider focal length";
            if (MinMM > otherMax) return "Longer focal length";
            return "Overlapping focal length range";
        }

        private string CompareAperture(LensTestModel other)
        {
            if (!MinFStop.HasValue || !other.MinFStop.HasValue) return "Cannot compare apertures";

            if (MinFStop < other.MinFStop) return "Wider maximum aperture";
            if (MinFStop > other.MinFStop) return "Narrower maximum aperture";
            return "Similar maximum aperture";
        }

        private string CompareFeatures(LensTestModel other)
        {
            var features = new List<string>();

            if (HasImageStabilization && !other.HasImageStabilization)
                features.Add("Has image stabilization");
            if (WeatherSealing && !other.WeatherSealing)
                features.Add("Weather sealed");
            if (MaxMagnification > other.MaxMagnification)
                features.Add("Better macro capabilities");

            return features.Any() ? string.Join(", ", features) : "Similar features";
        }

        /// <summary>
        /// Creates a test model with default valid values
        /// </summary>
        public static LensTestModel CreateValid(int? id = null)
        {
            return new LensTestModel
            {
                Id = id,
                MinMM = 24,
                MaxMM = 70,
                MinFStop = 2.8,
                MaxFStop = 22.0,
                IsPrime = false,
                IsUserCreated = false,
                NameForLens = "24-70mm f/2.8",
                Make = "Canon",
                Model = "RF 24-70mm f/2.8L IS USM",
                MountType = MountType.CanonRF,
                LensType = "Zoom",
                HasImageStabilization = true,
                WeatherSealing = true,
                MinFocusDistance = 0.21,
                MaxMagnification = 0.3,
                FilterThreadSize = 82,
                Weight = 900,
                Length = 125,
                OpticalConstruction = "15 elements in 13 groups"
            };
        }

        /// <summary>
        /// Creates a test model with invalid values for testing validation
        /// </summary>
        public static LensTestModel CreateInvalid()
        {
            return new LensTestModel
            {
                MinMM = 0,
                MaxMM = -1,
                MinFStop = 0,
                MaxFStop = -1,
                MountType = MountType.Other
            };
        }
    }

    public class FieldOfViewResult
    {
        public double HorizontalFovMin { get; set; }
        public double VerticalFovMin { get; set; }
        public double HorizontalFovMax { get; set; }
        public double VerticalFovMax { get; set; }
    }

    public class LensComparisonResult
    {
        public string FocalLengthComparison { get; set; } = string.Empty;
        public string ApertureComparison { get; set; } = string.Empty;
        public string FeatureComparison { get; set; } = string.Empty;
        public string OverallRecommendation { get; set; } = string.Empty;
    }
}