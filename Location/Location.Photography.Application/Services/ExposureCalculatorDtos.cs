namespace Location.Photography.Application.Services
{
    public class ExposureTriangleDto
    {
        public string ShutterSpeed { get; set; } = string.Empty;
        public string Aperture { get; set; } = string.Empty;
        public string Iso { get; set; } = string.Empty;
    }

    public class ExposureSettingsDto
    {
        public string ShutterSpeed { get; set; } = string.Empty;
        public string Aperture { get; set; } = string.Empty;
        public string Iso { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public enum ExposureIncrements
    {
        Full,
        Half,
        Third
    }

    /// <summary>
    /// Defines which part of the exposure triangle should be calculated
    /// </summary>
    public enum FixedValue
    {
        ShutterSpeeds = 0,
        ISO = 1,
        Empty = 2,
        Aperture = 3
    }
}