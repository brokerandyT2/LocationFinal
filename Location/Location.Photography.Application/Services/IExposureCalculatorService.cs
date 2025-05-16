using Location.Core.Application.Common.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Photography.Application.Services
{
    public interface IExposureCalculatorService
    {
        /// <summary>
        /// Calculates a new shutter speed based on the base exposure and desired aperture and ISO
        /// </summary>
        Task<Result<ExposureSettingsDto>> CalculateShutterSpeedAsync(
            ExposureTriangleDto baseExposure,
            string targetAperture,
            string targetIso,
            ExposureIncrements increments,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Calculates a new aperture based on the base exposure and desired shutter speed and ISO
        /// </summary>
        Task<Result<ExposureSettingsDto>> CalculateApertureAsync(
            ExposureTriangleDto baseExposure,
            string targetShutterSpeed,
            string targetIso,
            ExposureIncrements increments,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Calculates a new ISO based on the base exposure and desired shutter speed and aperture
        /// </summary>
        Task<Result<ExposureSettingsDto>> CalculateIsoAsync(
            ExposureTriangleDto baseExposure,
            string targetShutterSpeed,
            string targetAperture,
            ExposureIncrements increments,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets available shutter speed values for the specified increment
        /// </summary>
        Task<Result<string[]>> GetShutterSpeedsAsync(ExposureIncrements increments, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets available aperture values for the specified increment
        /// </summary>
        Task<Result<string[]>> GetAperturesAsync(ExposureIncrements increments, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets available ISO values for the specified increment
        /// </summary>
        Task<Result<string[]>> GetIsosAsync(ExposureIncrements increments, CancellationToken cancellationToken = default);
    }

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
}