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
    public partial class LocationViewModel : BaseViewModel
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

        // Event to notify about errors
        public event EventHandler<OperationErrorEventArgs>? ErrorOccurred;

        // Default constructor for design-time
        public LocationViewModel() : base(null)
        {
        }

        // Main constructor with dependencies
        public LocationViewModel(
            IMediator mediator,
            IMediaService mediaService,
            IGeolocationService geolocationService,
            IAlertService alertingService) 
            : base(alertingService)
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
                IsError = false;
                ErrorMessage = string.Empty;

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
                    // Handle error - this will automatically publish the error
                    ErrorMessage = result.ErrorMessage ?? "Failed to save location";
                    IsError = true;
                    OnErrorOccurred(ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                // Handle error - this will automatically publish the error
                ErrorMessage = $"Error saving location: {ex.Message}";
                IsError = true;
                OnErrorOccurred(ErrorMessage);
            }
            finally
            {
                IsBusy = false;
            }
        }

        // Other methods remain the same...
        
        // Helper method to raise error event
        protected virtual void OnErrorOccurred(string message)
        {
            ErrorOccurred?.Invoke(this, new OperationErrorEventArgs(message));
        }
        // Addition to LocationViewModel.cs - implement LoadLocationAsync method

        [RelayCommand]
        private async Task LoadLocationAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                IsBusy = true;
                IsError = false;
                ErrorMessage = string.Empty;

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
                    // Handle error
                    ErrorMessage = result.ErrorMessage ?? $"Failed to load location with ID {id}";
                    IsError = true;
                    OnErrorOccurred(ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                // Handle error
                ErrorMessage = $"Error loading location: {ex.Message}";
                IsError = true;
                OnErrorOccurred(ErrorMessage);
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
                IsError = false;
                ErrorMessage = string.Empty;

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
                        ErrorMessage = pickResult.ErrorMessage ?? "Failed to pick photo";
                        IsError = true;
                        OnErrorOccurred(ErrorMessage);
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
                    ErrorMessage = result.ErrorMessage ?? "Failed to capture photo";
                    IsError = true;
                    OnErrorOccurred(ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error taking photo: {ex.Message}";
                IsError = true;
                OnErrorOccurred(ErrorMessage);
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
                    // Not critical, just log
                    System.Diagnostics.Debug.WriteLine("Location permission denied");
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
            }
            catch (Exception ex)
            {
                // Not critical, just log
                System.Diagnostics.Debug.WriteLine($"Error starting location tracking: {ex.Message}");
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
            }
            catch (Exception ex)
            {
                // Not critical, just log
                System.Diagnostics.Debug.WriteLine($"Error stopping location tracking: {ex.Message}");
            }
        }
    }

    // Event args for error notifications
    public class OperationErrorEventArgs : EventArgs
    {
        public string Message { get; }

        public OperationErrorEventArgs(string message)
        {
            Message = message;
        }
    }
}