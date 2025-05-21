using Location.Core.Application.Locations.Queries.GetLocationById;
using Location.Core.Application.Services;
using Location.Core.ViewModels;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Core.Maui.Views
{
    public partial class AddLocation : ContentPage
    {
        #region Services

        private readonly IMediator _mediator;
        private readonly IMediaService _mediaService;
        private readonly IGeolocationService _geolocationService;
        private readonly IAlertService _alertService;
        private readonly int _locationId;
        private readonly bool _isEditMode;
        private CancellationTokenSource _cts = new CancellationTokenSource();

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor for design-time and XAML preview
        /// </summary>
        public AddLocation()
        {
            InitializeComponent();

            _locationId = 0;
            _isEditMode = false;

            // Set an empty view model for design-time
            BindingContext = new LocationViewModel();
           
            // Configure UI based on mode
            CloseModal.IsVisible = _isEditMode;
        }

        /// <summary>
        /// Main constructor with DI
        /// </summary>
        public AddLocation(
            IMediator mediator,
            IMediaService mediaService,
            IGeolocationService geolocationService,
            IAlertService alertService,
            int locationId = 0,
            bool isEditMode = false)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _mediaService = mediaService ?? throw new ArgumentNullException(nameof(mediaService));
            _geolocationService = geolocationService ?? throw new ArgumentNullException(nameof(geolocationService));
            _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
            _locationId = locationId;
            _isEditMode = isEditMode;

            InitializeComponent();

            // Load data and setup UI
            InitializeViewModel();
        }

        #endregion

        #region Setup and Initialization

        /// <summary>
        /// Initializes the ViewModel with proper services and data
        /// </summary>
        private void InitializeViewModel()
        {
            if (_locationId > 0)
            {
                // When editing an existing location, load it
                LoadLocationAsync(_locationId);
            }
            else
            {
                // Create new location ViewModel with services
                var viewModel = new LocationViewModel(_mediator, _mediaService, _geolocationService, _alertService);
                viewModel.Photo = string.IsNullOrEmpty(viewModel.Photo) ? "landscape.png" : viewModel.Photo;
                // Subscribe to error events
                viewModel.ErrorOccurred += ViewModel_ErrorOccurred;

                // Set as binding context
                BindingContext = viewModel;

                // We'll start location tracking when the page appears
            }

            // Configure UI based on mode
            CloseModal.IsVisible = _isEditMode;
        }

        /// <summary>
        /// Loads a location by ID
        /// </summary>
        private async void LoadLocationAsync(int id)
        {
            try
            {
                // Show loading indicator
                var loadingViewModel = new LocationViewModel(_mediator, _mediaService, _geolocationService, _alertService)
                {
                    IsBusy = true
                };
                BindingContext = loadingViewModel;

                // Create a fully initialized view model with services
                var viewModel = new LocationViewModel(_mediator, _mediaService, _geolocationService, _alertService);

                // Subscribe to error events
                viewModel.ErrorOccurred += ViewModel_ErrorOccurred;

                // Set as binding context
                BindingContext = viewModel;

                // Create query
                var query = new GetLocationByIdQuery { Id = id };

                // Send query using MediatR
                var result = await _mediator.Send(query, _cts.Token);

                if (result.IsSuccess && result.Data != null)
                {
                    // Update properties from result
                    viewModel.Id = result.Data.Id;
                    viewModel.Title = result.Data.Title;
                    viewModel.Description = result.Data.Description;
                    viewModel.Latitude = result.Data.Latitude;
                    viewModel.Longitude = result.Data.Longitude;
                    viewModel.City = result.Data.City;
                    viewModel.State = result.Data.State;
                    viewModel.Photo = result.Data.PhotoPath;
                    viewModel.Timestamp = result.Data.Timestamp;
                    viewModel.IsNewLocation = false;
                }
                else
                {
                    // Handle error - setting these properties will trigger the error event
                    viewModel.ErrorMessage = result.ErrorMessage ?? $"Failed to load location with ID {id}";
                    viewModel.IsError = true;
                    // Don't call OnErrorOccurred directly as it's protected
                }
            }
            catch (Exception ex)
            {
                // Handle error loading location
                await _alertService.ShowErrorAlertAsync($"Error loading location: {ex.Message}", "Error");

                // Create a new view model with error
                var errorViewModel = new LocationViewModel(_mediator, _mediaService, _geolocationService, _alertService)
                {
                    IsError = true,
                    ErrorMessage = $"Error loading location: {ex.Message}"
                };
                errorViewModel.ErrorOccurred += ViewModel_ErrorOccurred;
                BindingContext = errorViewModel;
            }
            finally
            {
                if (BindingContext is LocationViewModel vm)
                {
                    vm.IsBusy = false;
                }
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handle save button press
        /// </summary>
        private async void Save_Pressed(object sender, EventArgs e)
        {
            if (BindingContext is LocationViewModel viewModel)
            {
                // Execute the save command
                await viewModel.SaveCommand.ExecuteAsync(null);

                // If save was successful (no error message), reset view or close modal
                if (!viewModel.IsError)
                {
                    if (_isEditMode)
                    {
                        await Navigation.PopModalAsync();
                    }
                    else
                    {
                        // Create a new view model with services for a new location
                        var newViewModel = new LocationViewModel(_mediator, _mediaService, _geolocationService, _alertService);

                        // Subscribe to error events
                        newViewModel.ErrorOccurred += ViewModel_ErrorOccurred;

                        // Set as binding context
                        BindingContext = newViewModel;

                        // Start getting location for the new entry
                        await GetCurrentLocationAsync();

                        // Show success message
                        await _alertService.ShowSuccessAlertAsync("Location saved successfully", "Success");
                    }
                }
            }
        }

        /// <summary>
        /// Handle add photo button press
        /// </summary>
        private async void AddPhoto_Pressed(object sender, EventArgs e)
        {
            if (BindingContext is LocationViewModel viewModel)
            {
                await viewModel.TakePhotoCommand.ExecuteAsync(null);
            }
        }

        /// <summary>
        /// Handle close modal button press
        /// </summary>
        private void CloseModal_Pressed(object sender, EventArgs e)
        {
            Navigation.PopModalAsync();
        }

        /// <summary>
        /// Handle errors from the view model
        /// </summary>
        private void ViewModel_ErrorOccurred(object sender, OperationErrorEventArgs e)
        {
            // Display error to user if it's not already displayed in the UI
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await _alertService.ShowErrorAlertAsync(e.Message, "Error");
            });
        }

        #endregion

        #region Location Handling

        /// <summary>
        /// Get the current location
        /// </summary>
        private async Task GetCurrentLocationAsync()
        {
            if (BindingContext is LocationViewModel viewModel && !_isEditMode)
            {
                try
                {
                    // Start location tracking on the view model
                    await viewModel.StartLocationTrackingCommand.ExecuteAsync(null);
                }
                catch (Exception ex)
                {
                    // Log the error but don't show it to the user, as it's not critical
                    System.Diagnostics.Debug.WriteLine($"Error getting current location: {ex.Message}");
                }
            }
        }

        #endregion

        #region Lifecycle Methods

        /// <summary>
        /// Called when the page appears
        /// </summary>
        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Re-subscribe to ViewModel events in case the binding context changed
            if (BindingContext is LocationViewModel viewModel)
            {
                // Make sure we only subscribe once
                viewModel.ErrorOccurred -= ViewModel_ErrorOccurred;
                viewModel.ErrorOccurred += ViewModel_ErrorOccurred;

                // If this is a new location (not edit mode), get the current location immediately
                if (!_isEditMode && viewModel.IsNewLocation && viewModel.Latitude == 0 && viewModel.Longitude == 0)
                {
                    await GetCurrentLocationAsync();
                }
            }
        }

        /// <summary>
        /// Called when the page disappears
        /// </summary>
        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            // Unsubscribe from ViewModel events
            if (BindingContext is LocationViewModel viewModel)
            {
                viewModel.ErrorOccurred -= ViewModel_ErrorOccurred;

                // Ensure location tracking is stopped when leaving the page
                if (viewModel.IsLocationTracking)
                {
                    viewModel.StopLocationTrackingCommand.Execute(null);
                }
            }

            // Cancel any pending operations
            if (!_cts.IsCancellationRequested)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = new CancellationTokenSource();
            }
        }

        #endregion
    }
}