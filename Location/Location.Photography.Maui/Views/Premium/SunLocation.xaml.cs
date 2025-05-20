using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Interfaces.Persistence;
using Location.Core.Application.Services;
using Location.Core.Application.Settings.Queries.GetSettingByKey;
using Location.Photography.Application.Queries.SunLocation;
using Location.Photography.Domain.Services;
using Location.Photography.Infrastructure;
using Location.Photography.ViewModels;
using MediatR;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using ILocationRepository = Location.Core.Application.Common.Interfaces.ILocationRepository;
using ISettingRepository = Location.Core.Application.Common.Interfaces.Persistence.ISettingRepository;
using OperationErrorEventArgs = Location.Photography.ViewModels.Events.OperationErrorEventArgs;
using OperationErrorSource = Location.Photography.ViewModels.Events.OperationErrorSource;
namespace Location.Photography.Maui.Views.Premium
{
    public partial class SunLocation : ContentPage
    {
        #region Services

        private readonly IMediator _mediator;
        private readonly IAlertService _alertService;
        private readonly ILocationRepository _locationRepository;
        private readonly ISunCalculatorService _sunCalculatorService;

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor for design-time and XAML preview
        /// </summary>
        public SunLocation()
        {
            InitializeComponent();

            // Create a design-time view model
            BindingContext = new SunLocationViewModel();
        }

        /// <summary>
        /// Main constructor with DI
        /// </summary>
        public SunLocation(
            IMediator mediator,
            IAlertService alertService,
            ILocationRepository locationRepository,
            ISunCalculatorService sunCalculatorService, ISettingRepository setting)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
            _locationRepository = locationRepository ?? throw new ArgumentNullException(nameof(locationRepository));
            _sunCalculatorService = sunCalculatorService ?? throw new ArgumentNullException(nameof(sunCalculatorService));
            _settingRepository = setting ?? throw new ArgumentNullException(nameof(setting));
            InitializeComponent();
            InitializeViewModel();
        }
        private ISettingRepository _settingRepository;
        #endregion

        protected override async void OnNavigatedTo(NavigatedToEventArgs args)
        {
            base.OnNavigatedTo(args);
            date.Format = (await _settingRepository.GetByKeyAsync(MagicStrings.DateFormat)).Value;
            time.Format = (await _settingRepository.GetByKeyAsync(MagicStrings.TimeFormat)).Value;

        }


        #region Initialization

        /// <summary>
        /// Sets up the ViewModel with the required services
        /// </summary>
        private async Task InitializeViewModel()
        {

           

            try
            {
                // Create the view model
                var viewModel = new SunLocationViewModel(_mediator, _sunCalculatorService);

                // Subscribe to error events
                viewModel.ErrorOccurred += ViewModel_ErrorOccurred;

                // Set the binding context
                BindingContext = viewModel;

                // Load initial data
                LoadLocationsAsync();
            }
            catch (Exception ex)
            {
                HandleError(ex, "Error initializing view model");
            }
        }

        /// <summary>
        /// Load locations from the location repository
        /// </summary>
        private async void LoadLocationsAsync()
        {
            try
            {
                if (BindingContext is SunLocationViewModel viewModel)
                {
                    viewModel.IsBusy = true;
                    //viewModel.Locations =
                    // Get locations from the repository
                    var result = await _locationRepository.GetAllAsync();

                    if (result.IsSuccess && result.Data != null)
                    {
                        // Map the locations to view models
                        var locationViewModels = result.Data.Select(l =>
                            new LocationViewModel() { Name = l.Title, Description = l.Description, Lattitude = l.Coordinate.Latitude, Longitude = l.Coordinate.Longitude, Photo = l.PhotoPath });

                        // Create an observable collection of locations
                        viewModel.Locations = new ObservableCollection<LocationViewModel>(locationViewModels);

                        // If there are any locations, select the first one
                        if (viewModel.Locations.Count > 0)
                        {
                            locationPicker.SelectedIndex = 0;
                            var selectedLocation = viewModel.Locations[0];

                            // Set the coordinates from the selected location
                            viewModel.Latitude = selectedLocation.Lattitude;
                            viewModel.Longitude = selectedLocation.Longitude;

                            // Update the sun position after the location is set
                            await UpdateSunPositionAsync(viewModel);
                        }

                    }
                    else
                    {
                        // Handle error getting locations
                        viewModel.ErrorMessage = result.ErrorMessage ?? "Failed to load locations";
                    }
                }
            }
            catch (Exception ex)
            {
                HandleError(ex, "Error loading locations");
            }
            finally
            {
                if (BindingContext is SunLocationViewModel viewModel)
                {
                    viewModel.IsBusy = false;
                }
            }
        }

        /// <summary>
        /// Update the sun position using the mediator pattern
        /// </summary>
        private async Task UpdateSunPositionAsync(SunLocationViewModel viewModel)
        {
            try
            {
                if (viewModel.Latitude == 0 && viewModel.Longitude == 0)
                    return;

                var query = new GetCurrentSunPositionQuery
                {
                    Latitude = viewModel.Latitude,
                    Longitude = viewModel.Longitude,
                    DateTime = viewModel.SelectedDateTime
                };

                var result = await _mediator.Send(query);

                if (result.IsSuccess && result.Data != null)
                {
                    viewModel.SunDirection = result.Data.Azimuth;
                    viewModel.SunElevation = result.Data.Elevation;
                }
                else
                {
                    viewModel.ErrorMessage = result.ErrorMessage ?? "Failed to calculate sun position";
                }
            }
            catch (Exception ex)
            {
                HandleError(ex, "Error updating sun position");
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handle location picker selection change
        /// </summary>
        private async void locationPicker_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (locationPicker.SelectedItem is LocationViewModel selectedLocation &&
                BindingContext is SunLocationViewModel viewModel)
            {
                viewModel.Latitude = selectedLocation.Lattitude;
                viewModel.Longitude = selectedLocation.Longitude;

                // Update sun position when location changes
                await UpdateSunPositionAsync(viewModel);
            }
        }

        /// <summary>
        /// Handle date selection change
        /// </summary>
        private async void date_DateSelected(object sender, DateChangedEventArgs e)
        {
            if (BindingContext is SunLocationViewModel viewModel)
            {
                viewModel.SelectedDate = e.NewDate;

                // Update sun position when date changes
                await UpdateSunPositionAsync(viewModel);
            }
        }

        /// <summary>
        /// Handle time selection change
        /// </summary>
        private async void time_TimeSelected(object sender, TimeChangedEventArgs e)
        {
            if (BindingContext is SunLocationViewModel viewModel)
            {
                viewModel.SelectedTime = e.NewTime;

                // Update sun position when time changes
                await UpdateSunPositionAsync(viewModel);
            }
        }

        /// <summary>
        /// Handle errors from the view model
        /// </summary>
        private void ViewModel_ErrorOccurred(object sender, OperationErrorEventArgs e)
        {
            // Display error to user if it's not already displayed in the UI
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await _alertService.ShowErrorAlertAsync(
                    e.Message,
                    "Error");
            });
        }

        #endregion

        #region Lifecycle Methods

        /// <summary>
        /// Called when the page appears
        /// </summary>
        protected override void OnAppearing()
        {
            base.OnAppearing();

            try
            {
                // Get the view model
                if (BindingContext is SunLocationViewModel viewModel)
                {
                    // Re-subscribe to events
                    viewModel.ErrorOccurred -= ViewModel_ErrorOccurred;
                    viewModel.ErrorOccurred += ViewModel_ErrorOccurred;

                    // Start monitoring compass and sensors
                    viewModel.BeginMonitoring = true;

                    // If there are no locations yet, load them
                    if (viewModel.Locations == null || viewModel.Locations.Count == 0)
                    {
                        LoadLocationsAsync();
                    }
                    else
                    {
                        // Update sun position with current values
                        _ = UpdateSunPositionAsync(viewModel);
                    }
                }
            }
            catch (Exception ex)
            {
                HandleError(ex, "Error during page appearing");
            }
        }

        /// <summary>
        /// Called when the page disappears
        /// </summary>
        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            try
            {
                // Stop monitoring sensors when the page disappears
                if (BindingContext is SunLocationViewModel viewModel)
                {
                    // Unsubscribe from events
                    viewModel.ErrorOccurred -= ViewModel_ErrorOccurred;

                    // Stop monitoring
                    viewModel.BeginMonitoring = false;
                }
            }
            catch (Exception ex)
            {
                // Just log the error since we're leaving the page
                System.Diagnostics.Debug.WriteLine($"Error during page disappearing: {ex.Message}");
            }
        }

        #endregion

        #region Error Handling

        /// <summary>
        /// Handle errors during view operations
        /// </summary>
        private void HandleError(Exception ex, string message)
        {
            // Log the error
            System.Diagnostics.Debug.WriteLine($"Error: {message}. {ex.Message}");

            // Display alert to user
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await _alertService.ShowErrorAlertAsync(message, "Error");
            });

            // Pass the error to the ViewModel if available
            if (BindingContext is SunLocationViewModel viewModel)
            {
                viewModel.ErrorMessage = $"{message}: {ex.Message}";
                viewModel.IsBusy = false;
            }
        }

        #endregion
    }
}