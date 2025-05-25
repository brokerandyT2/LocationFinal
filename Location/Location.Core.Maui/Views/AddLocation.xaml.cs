using Location.Core.Application.Locations.Queries.GetLocationById;
using Location.Core.Application.Services;
using Location.Core.ViewModels;
using Location.Core.Maui.Resources;
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
        private readonly IErrorDisplayService _errorDisplayService;
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
            IErrorDisplayService errorDisplayService,
            int locationId = 0,
            bool isEditMode = false)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _mediaService = mediaService ?? throw new ArgumentNullException(nameof(mediaService));
            _geolocationService = geolocationService ?? throw new ArgumentNullException(nameof(geolocationService));
            _errorDisplayService = errorDisplayService ?? throw new ArgumentNullException(nameof(errorDisplayService));
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
                var viewModel = new LocationViewModel(_mediator, _mediaService, _geolocationService, _errorDisplayService);
                viewModel.Photo = string.IsNullOrEmpty(viewModel.Photo) ? "landscape.png" : viewModel.Photo;

                // Subscribe to system error events
                viewModel.ErrorOccurred += OnSystemError;

                // Set as binding context
                BindingContext = viewModel;
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
                var loadingViewModel = new LocationViewModel(_mediator, _mediaService, _geolocationService, _errorDisplayService)
                {
                    IsBusy = true
                };
                BindingContext = loadingViewModel;

                // Create a fully initialized view model with services
                var viewModel = new LocationViewModel(_mediator, _mediaService, _geolocationService, _errorDisplayService);

                // Subscribe to system error events
                viewModel.ErrorOccurred += OnSystemError;

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
                    // System error from MediatR - handled by LoadLocationCommand
                }
            }
            catch (Exception ex)
            {
                // Handle error loading location via LoadLocationCommand
                var errorViewModel = new LocationViewModel(_mediator, _mediaService, _geolocationService, _errorDisplayService);
                errorViewModel.ErrorOccurred += OnSystemError;
                await errorViewModel.ExecuteAndTrackAsync(errorViewModel.LoadLocationCommand, id);
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
                // Execute and track the save command for retry capability
                await viewModel.ExecuteAndTrackAsync(viewModel.SaveCommand, null);

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
                        var newViewModel = new LocationViewModel(_mediator, _mediaService, _geolocationService, _errorDisplayService);

                        // Subscribe to system error events
                        newViewModel.ErrorOccurred += OnSystemError;

                        // Set as binding context
                        BindingContext = newViewModel;

                        // Start getting location for the new entry
                        await GetCurrentLocationAsync();
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
                await viewModel.ExecuteAndTrackAsync(viewModel.TakePhotoCommand, null);
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
        /// Handle system errors from MediatR operations
        /// </summary>
        private async void OnSystemError(object sender, OperationErrorEventArgs e)
        {
            var retry = await DisplayAlert(
                AppResources.Error,
                $"{e.Message}. Click OK to try again.",
                AppResources.OK,
                AppResources.Cancel);

            if (retry && sender is LocationViewModel viewModel)
            {
                await viewModel.RetryLastCommandAsync();
            }
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
                    await viewModel.ExecuteAndTrackAsync(viewModel.StartLocationTrackingCommand, null);
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
                viewModel.ErrorOccurred -= OnSystemError;
                viewModel.ErrorOccurred += OnSystemError;

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
                viewModel.ErrorOccurred -= OnSystemError;

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