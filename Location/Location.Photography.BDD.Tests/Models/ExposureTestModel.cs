using Location.Photography.Application.Services;

namespace Location.Photography.BDD.Tests.Models
{
    /// <summary>
    /// Test model for exposure calculator scenarios
    /// </summary>
    public class ExposureTestModel
    {
        public int? Id { get; set; }

        // Base exposure settings
        public string BaseShutterSpeed { get; set; } = string.Empty;
        public string BaseAperture { get; set; } = string.Empty;
        public string BaseIso { get; set; } = string.Empty;

        // Target exposure settings
        public string TargetShutterSpeed { get; set; } = string.Empty;
        public string TargetAperture { get; set; } = string.Empty;
        public string TargetIso { get; set; } = string.Empty;

        // Result exposure settings
        public string ResultShutterSpeed { get; set; } = string.Empty;
        public string ResultAperture { get; set; } = string.Empty;
        public string ResultIso { get; set; } = string.Empty;

        // Calculation parameters
        public ExposureIncrements Increments { get; set; } = ExposureIncrements.Full;
        public FixedValue FixedValue { get; set; } = FixedValue.ShutterSpeeds;
        public double EvCompensation { get; set; } = 0.0;

        // Error handling
        public string ErrorMessage { get; set; } = string.Empty;
        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        // Validation
        public bool IsValid => !string.IsNullOrEmpty(BaseShutterSpeed) &&
                              !string.IsNullOrEmpty(BaseAperture) &&
                              !string.IsNullOrEmpty(BaseIso);

        /// <summary>
        /// Creates an ExposureTriangleDto from base values
        /// </summary>
        public ExposureTriangleDto ToBaseExposureTriangle()
        {
            return new ExposureTriangleDto
            {
                ShutterSpeed = BaseShutterSpeed,
                Aperture = BaseAperture,
                Iso = BaseIso
            };
        }

        /// <summary>
        /// Creates an ExposureTriangleDto from target values
        /// </summary>
        public ExposureTriangleDto ToTargetExposureTriangle()
        {
            return new ExposureTriangleDto
            {
                ShutterSpeed = TargetShutterSpeed,
                Aperture = TargetAperture,
                Iso = TargetIso
            };
        }

        /// <summary>
        /// Creates an ExposureTriangleDto from result values
        /// </summary>
        public ExposureTriangleDto ToResultExposureTriangle()
        {
            return new ExposureTriangleDto
            {
                ShutterSpeed = ResultShutterSpeed,
                Aperture = ResultAperture,
                Iso = ResultIso
            };
        }

        /// <summary>
        /// Updates result values from ExposureSettingsDto
        /// </summary>
        public void UpdateFromResult(ExposureSettingsDto result)
        {
            if (result != null)
            {
                ResultShutterSpeed = result.ShutterSpeed ?? ResultShutterSpeed;
                ResultAperture = result.Aperture ?? ResultAperture;
                ResultIso = result.Iso ?? ResultIso;
                ErrorMessage = result.ErrorMessage ?? ErrorMessage;
            }
        }

        /// <summary>
        /// Resets all result values
        /// </summary>
        public void ClearResults()
        {
            ResultShutterSpeed = string.Empty;
            ResultAperture = string.Empty;
            ResultIso = string.Empty;
            ErrorMessage = string.Empty;
        }

        /// <summary>
        /// Creates a copy of this model
        /// </summary>
        public ExposureTestModel Clone()
        {
            return new ExposureTestModel
            {
                Id = Id,
                BaseShutterSpeed = BaseShutterSpeed,
                BaseAperture = BaseAperture,
                BaseIso = BaseIso,
                TargetShutterSpeed = TargetShutterSpeed,
                TargetAperture = TargetAperture,
                TargetIso = TargetIso,
                ResultShutterSpeed = ResultShutterSpeed,
                ResultAperture = ResultAperture,
                ResultIso = ResultIso,
                Increments = Increments,
                FixedValue = FixedValue,
                EvCompensation = EvCompensation,
                ErrorMessage = ErrorMessage
            };
        }

        /// <summary>
        /// String representation for debugging
        /// </summary>
        public override string ToString()
        {
            return $"ExposureTest[{Id}]: Base({BaseShutterSpeed}, {BaseAperture}, {BaseIso}) " +
                   $"-> Target({TargetShutterSpeed}, {TargetAperture}, {TargetIso}) " +
                   $"= Result({ResultShutterSpeed}, {ResultAperture}, {ResultIso})";
        }
    }

    /// <summary>
    /// Enum extensions for test scenarios
    /// </summary>
    public static class ExposureTestModelExtensions
    {
        public static string ToDisplayString(this ExposureIncrements increment)
        {
            return increment switch
            {
                ExposureIncrements.Full => "Full stops",
                ExposureIncrements.Half => "Half stops",
                ExposureIncrements.Third => "Third stops",
                _ => "Unknown"
            };
        }

        public static string ToDisplayString(this FixedValue fixedValue)
        {
            return fixedValue switch
            {
                FixedValue.ShutterSpeeds => "Shutter Speed",
                FixedValue.Aperture => "Aperture",
                FixedValue.ISO => "ISO",
                FixedValue.Empty => "None",
                _ => "Unknown"
            };
        }
    }
}