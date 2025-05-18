// Location.Photography.Infrastructure/Services/IExposureTriangleService.cs
namespace Location.Photography.Infrastructure.Services
{
    /// <summary>
    /// Service for performing calculations on the exposure triangle (shutter speed, aperture, ISO)
    /// </summary>
    public interface IExposureTriangleService
    {
        /// <summary>
        /// Calculates the required shutter speed to maintain equivalent exposure
        /// </summary>
        /// <param name="baseShutterSpeed">Current shutter speed</param>
        /// <param name="baseAperture">Current aperture (f-stop)</param>
        /// <param name="baseIso">Current ISO</param>
        /// <param name="targetAperture">Target aperture (f-stop)</param>
        /// <param name="targetIso">Target ISO</param>
        /// <param name="scale">Scale factor: 1 for full stops, 2 for half stops, 3 for third stops</param>
        /// <param name="evCompensation">EV compensation value (positive = brighter, negative = darker)</param>
        /// <returns>The calculated shutter speed as a string</returns>
        string CalculateShutterSpeed(
            string baseShutterSpeed,
            string baseAperture,
            string baseIso,
            string targetAperture,
            string targetIso,
            int scale,
            double evCompensation = 0);

        /// <summary>
        /// Calculates the required aperture to maintain equivalent exposure
        /// </summary>
        /// <param name="baseShutterSpeed">Current shutter speed</param>
        /// <param name="baseAperture">Current aperture (f-stop)</param>
        /// <param name="baseIso">Current ISO</param>
        /// <param name="targetShutterSpeed">Target shutter speed</param>
        /// <param name="targetIso">Target ISO</param>
        /// <param name="scale">Scale factor: 1 for full stops, 2 for half stops, 3 for third stops</param>
        /// <param name="evCompensation">EV compensation value (positive = brighter, negative = darker)</param>
        /// <returns>The calculated aperture as a string</returns>
        string CalculateAperture(
            string baseShutterSpeed,
            string baseAperture,
            string baseIso,
            string targetShutterSpeed,
            string targetIso,
            int scale,
            double evCompensation = 0);

        /// <summary>
        /// Calculates the required ISO to maintain equivalent exposure
        /// </summary>
        /// <param name="baseShutterSpeed">Current shutter speed</param>
        /// <param name="baseAperture">Current aperture (f-stop)</param>
        /// <param name="baseIso">Current ISO</param>
        /// <param name="targetShutterSpeed">Target shutter speed</param>
        /// <param name="targetAperture">Target aperture (f-stop)</param>
        /// <param name="scale">Scale factor: 1 for full stops, 2 for half stops, 3 for third stops</param>
        /// <param name="evCompensation">EV compensation value (positive = brighter, negative = darker)</param>
        /// <returns>The calculated ISO as a string</returns>
        string CalculateIso(
            string baseShutterSpeed,
            string baseAperture,
            string baseIso,
            string targetShutterSpeed,
            string targetAperture,
            int scale,
            double evCompensation = 0);
    }
}