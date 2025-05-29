// Location.Core.ViewModels/LocationViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Location.Core.Application.Commands.Locations;
using Location.Core.Application.Locations.Queries.GetLocationById;
using Location.Core.Application.Services;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Core.ViewModels
{
    public partial class LocationViewModel : BaseViewModel, INavigationAware
    {
        private readonly IMediator _mediator;
        private readonly IMediaService _mediaService;
        private readonly IGeolocationService _geolocationService;

        [ObservableProperty]
        private int _id;

        [ObservableProperty]
        private string _title = string.Empty;

        [ObservableProperty]
        private string _description = string.Empty;

        [ObservableProperty]
        private double _latitude;

        [ObservableProperty]
        private double _longitude;

        [ObservableProperty]
        private string _city = string.Empty;

        [ObservableProperty]
        private string _state = string.Empty;

        [ObservableProperty]
        private string _photo = string.Empty;

        [ObservableProperty]
        private DateTime _timestamp;

        [ObservableProperty]
        private string _dateFormat = "g";

        [ObservableProperty]
        private bool _isNewLocation = true;

        [ObservableProperty]
        private bool _isLocationTracking;

        // Default constructor for design-time
        public LocationViewModel() : base(null, null)
        {
        }

        // Main constructor with dependencies
        public LocationViewModel(
            IMediator mediator,
            IMediaService mediaService,
            IGeolocationService geolocationService,
            IErrorDisplayService errorDisplayService)
            : base(null, errorDisplayService)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _mediaService = mediaService ?? throw new ArgumentNullException(nameof(mediaService));
            _geolocationService = geolocationService ?? throw new ArgumentNullException(nameof(geolocationService));
        }

        [RelayCommand]
        private async Task SaveAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                IsBusy = true;
                ClearErrors();

                var command = new SaveLocationCommand
                {
                    Id = Id > 0 ? Id : null,
                    Title = Title,
                    Description = Description,
                    Latitude = Latitude,
                    Longitude = Longitude,
                    City = City,
                    State = State,
                    PhotoPath = Photo
                };

                var result = await _mediator.Send(command, cancellationToken);

                if (result.IsSuccess && result.Data != null)
                {
                    // Update properties from result if needed
                    Id = result.Data.Id;
                    Timestamp = result.Data.Timestamp;
                    IsNewLocation = false;
                }
                else
                {
                    // System error from MediatR - trigger ErrorOccurred event
                    OnSystemError(result.ErrorMessage ?? "Failed to save location");
                }
            }
            catch (Exception ex)
            {
                // System error - trigger ErrorOccurred event
                OnSystemError($"Error saving location: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task LoadLocationAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                IsBusy = true;
                ClearErrors();

                // Create query
                var query = new GetLocationByIdQuery { Id = id };

                // Send query using MediatR
                var result = await _mediator.Send(query, cancellationToken);

                if (result.IsSuccess && result.Data != null)
                {
                    // Update properties from result
                    Id = result.Data.Id;
                    Title = result.Data.Title;
                    Description = result.Data.Description;
                    Latitude = result.Data.Latitude;
                    Longitude = result.Data.Longitude;
                    City = result.Data.City;
                    State = result.Data.State;
                    Photo = result.Data.PhotoPath;
                    Timestamp = result.Data.Timestamp;
                    IsNewLocation = false;
                }
                else
                {
                    // System error from MediatR
                    OnSystemError(result.ErrorMessage ?? $"Failed to load location with ID {id}");
                }
            }
            catch (Exception ex)
            {
                // System error
                OnSystemError($"Error loading location: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task TakePhotoAsync()
        {
            try
            {
                IsBusy = true;
                ClearErrors();

                // Check if capture is supported
                var supportResult = await _mediaService.IsCaptureSupported();
                if (!supportResult.IsSuccess || !supportResult.Data)
                {
                    // Try to pick a photo instead
                    var pickResult = await _mediaService.PickPhotoAsync();
                    if (pickResult.IsSuccess)
                    {
                        Photo = pickResult.Data;
                    }
                    else
                    {
                        // Validation error - show in UI
                        SetValidationError(pickResult.ErrorMessage ?? "Failed to pick photo");
                    }
                    return;
                }

                // Capture a photo
                var result = await _mediaService.CapturePhotoAsync();
                if (result.IsSuccess)
                {
                    Photo = result.Data;
                }
                else
                {
                    // Validation error - show in UI
                    SetValidationError(result.ErrorMessage ?? "Failed to capture photo");
                }
            }
            catch (Exception ex)
            {
                // System error
                OnSystemError($"Error taking photo: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task StartLocationTrackingAsync()
        {
            try
            {
                if (IsLocationTracking)
                    return;

                IsBusy = true;

                // Request location permissions
                var permissionResult = await _geolocationService.RequestPermissionsAsync();
                if (!permissionResult.IsSuccess || !permissionResult.Data)
                {
                    // Validation error - user can fix by granting permission
                    SetValidationError("Location permission is required");
                    return;
                }

                // Start tracking
                var result = await _geolocationService.StartTrackingAsync();
                if (result.IsSuccess && result.Data)
                {
                    IsLocationTracking = true;

                    // Get current location immediately
                    var locationResult = await _geolocationService.GetCurrentLocationAsync();
                    if (locationResult.IsSuccess && locationResult.Data != null)
                    {
                        Latitude = Math.Round(locationResult.Data.Latitude, 6);
                        Longitude = Math.Round(locationResult.Data.Longitude, 6);
                    }
                }
                else
                {
                    // System error
                    OnSystemError(result.ErrorMessage ?? "Failed to start location tracking");
                }
            }
            catch (Exception ex)
            {
                // System error
                OnSystemError($"Error starting location tracking: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task StopLocationTrackingAsync()
        {
            try
            {
                if (!IsLocationTracking)
                    return;

                var result = await _geolocationService.StopTrackingAsync();
                if (result.IsSuccess)
                {
                    IsLocationTracking = false;
                }
                else
                {
                    // System error
                    OnSystemError(result.ErrorMessage ?? "Failed to stop location tracking");
                }
            }
            catch (Exception ex)
            {
                // System error
                OnSystemError($"Error stopping location tracking: {ex.Message}");
            }
        }

        public void OnNavigatedToAsync()
        {
            StartLocationTrackingAsync();
        }

        public void OnNavigatedFromAsync()
        {
            StopLocationTrackingAsync();
        }
    }
}