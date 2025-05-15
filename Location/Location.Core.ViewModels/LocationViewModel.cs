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
            IAlertingService alertingService) 
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