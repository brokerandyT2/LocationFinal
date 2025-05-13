using System.Threading;
using System.Threading.Tasks;
using Location.Core.Application.Common.Models;

namespace Location.Core.Application.Services
{
    /// <summary>
    /// Interface for geolocation services
    /// </summary>
    public interface IGeolocationService
    {
        /// <summary>
        /// Gets the current device location
        /// </summary>
        Task<Result<GeolocationDto>> GetCurrentLocationAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if location services are enabled
        /// </summary>
        Task<Result<bool>> IsLocationEnabledAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Requests location permissions from the user
        /// </summary>
        Task<Result<bool>> RequestPermissionsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Starts continuous location tracking
        /// </summary>
        Task<Result<bool>> StartTrackingAsync(GeolocationAccuracy accuracy = GeolocationAccuracy.Medium, CancellationToken cancellationToken = default);

        /// <summary>
        /// Stops location tracking
        /// </summary>
        Task<Result<bool>> StopTrackingAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// DTO for geolocation data
    /// </summary>
    public class GeolocationDto
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double? Altitude { get; set; }
        public double? Accuracy { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Geolocation accuracy levels
    /// </summary>
    public enum GeolocationAccuracy
    {
        Lowest,
        Low,
        Medium,
        High,
        Best
    }
}