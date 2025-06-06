using Location.Core.Application.Common.Models;
using Location.Core.Application.Services;

namespace Location.Core.Maui.Services
{
    public class GeolocationService : IGeolocationService
    {
        private readonly IAlertService _alertService;
        private bool _isTracking = false;
        private CancellationTokenSource? _cts;

        public GeolocationService(IAlertService alertService)
        {
            _alertService = alertService;
        }

        public async Task<Result<GeolocationDto>> GetCurrentLocationAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var locationPermissionStatus = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                if (locationPermissionStatus != PermissionStatus.Granted)
                {
                    locationPermissionStatus = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                    if (locationPermissionStatus != PermissionStatus.Granted)
                    {
                        return Result<GeolocationDto>.Failure("Location permission is required");
                    }
                }

                var location = await Geolocation.GetLocationAsync(new GeolocationRequest
                {
                    DesiredAccuracy = ConvertToMauiAccuracy(Application.Services.GeolocationAccuracy.Best),
                    Timeout = TimeSpan.FromSeconds(30)
                }, cancellationToken);

                if (location == null)
                {
                    return Result<GeolocationDto>.Failure("Unable to get current location");
                }

                var locationDto = new GeolocationDto
                {
                    Latitude = location.Latitude,
                    Longitude = location.Longitude,
                    Altitude = location.Altitude,
                    Accuracy = location.Accuracy,
                    Timestamp = DateTime.UtcNow
                };

                return Result<GeolocationDto>.Success(locationDto);
            }
            catch (FeatureNotSupportedException)
            {
                return Result<GeolocationDto>.Failure("Geolocation is not supported on this device");
            }
            catch (PermissionException)
            {
                return Result<GeolocationDto>.Failure("Location permission is required");
            }
            catch (Exception ex)
            {
                return Result<GeolocationDto>.Failure($"Error getting location: {ex.Message}");
            }
        }

        public async Task<Result<bool>> IsLocationEnabledAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // MAUI doesn't have a direct IsGeolocationEnabled property
                // We'll try to get location availability instead
                var isEnabled = await Task.FromResult(true);

                try
                {
                    var locationPermissionStatus = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                    isEnabled = locationPermissionStatus == PermissionStatus.Granted;
                }
                catch
                {
                    isEnabled = false;
                }

                return Result<bool>.Success(isEnabled);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure($"Error checking location status: {ex.Message}");
            }
        }

        public async Task<Result<bool>> RequestPermissionsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                return Result<bool>.Success(status == PermissionStatus.Granted);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure($"Error requesting permissions: {ex.Message}");
            }
        }

        public async Task<Result<bool>> StartTrackingAsync(Application.Services.GeolocationAccuracy accuracy = Application.Services.GeolocationAccuracy.Medium, CancellationToken cancellationToken = default)
        {
            try
            {
                if (_isTracking)
                {
                    return Result<bool>.Success(true);
                }

                var permissionResult = await RequestPermissionsAsync(cancellationToken);
                if (!permissionResult.IsSuccess || !permissionResult.Data)
                {
                    return Result<bool>.Failure("Location permission not granted");
                }

                _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                _isTracking = true;

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure($"Error starting location tracking: {ex.Message}");
            }
        }

        public async Task<Result<bool>> StopTrackingAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _cts?.Cancel();
                _cts = null;
                _isTracking = false;

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure($"Error stopping location tracking: {ex.Message}");
            }
        }

        // Helper method to convert between accuracy enums
        private Microsoft.Maui.Devices.Sensors.GeolocationAccuracy ConvertToMauiAccuracy(Application.Services.GeolocationAccuracy accuracy)
        {
            return accuracy switch
            {
                Application.Services.GeolocationAccuracy.Lowest => Microsoft.Maui.Devices.Sensors.GeolocationAccuracy.Lowest,
                Application.Services.GeolocationAccuracy.Low => Microsoft.Maui.Devices.Sensors.GeolocationAccuracy.Low,
                Application.Services.GeolocationAccuracy.Medium => Microsoft.Maui.Devices.Sensors.GeolocationAccuracy.Medium,
                Application.Services.GeolocationAccuracy.High => Microsoft.Maui.Devices.Sensors.GeolocationAccuracy.High,
                Application.Services.GeolocationAccuracy.Best => Microsoft.Maui.Devices.Sensors.GeolocationAccuracy.Best,
                _ => Microsoft.Maui.Devices.Sensors.GeolocationAccuracy.Default
            };
        }
    }
}