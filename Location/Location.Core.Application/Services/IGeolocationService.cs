using Location.Core.Application.Common.Models;

namespace Location.Core.Application.Services
{
    /// <summary>
    /// Interface for geolocation services
    /// </summary>
    public interface IGeolocationService
    {
        Task<Result<GeolocationDto>> GetCurrentLocationAsync(CancellationToken cancellationToken = default);
        Task<Result<bool>> IsLocationEnabledAsync(CancellationToken cancellationToken = default);
        Task<Result<bool>> RequestPermissionsAsync(CancellationToken cancellationToken = default);
        Task<Result<bool>> StartTrackingAsync(GeolocationAccuracy accuracy = GeolocationAccuracy.Medium, CancellationToken cancellationToken = default);
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