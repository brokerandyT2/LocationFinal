using Location.Core.Application.Locations.Queries.GetLocationById;
using Location.Core.Application.Services;
using Location.Core.Maui.Services;
using Location.Core.ViewModels;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Core.Maui.Views
{
    public partial class EditLocation : ContentPage
    {
        private readonly IMediator _mediator;
        private readonly IMediaService _mediaService;
        private readonly IGeolocationService _geolocationService;
        private readonly IAlertService _alertService;
        private readonly INavigationService _navigationService;
        private readonly int _locationId;
        private readonly bool _isModalMode;
        private CancellationTokenSource _cts = new CancellationTokenSource();

        /// <summary>
        /// Default constructor for design-time and XAML preview
        /// </summary>
        public EditLocation()
        {
            InitializeComponent();
            _locationId = 0;
            _isModalMode = false;
            //CloseModal.IsVisible = _isModalMode;

            // Set a default view model for design time
            BindingContext = new LocationViewModel();
        }

        /// <summary>
        /// Main constructor with DI
        /// </summary>
        public EditLocation(
            IMediator mediator,
            IMediaService mediaService,
            IGeolocationService geolocationService,
            IAlertService alertService,
            INavigationService navigationService,
            int locationId = 0,
            bool isModalMode = false)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _mediaService = mediaService ?? throw new ArgumentNullException(nameof(mediaService));
            _geolocationService = geolocationService ?? throw new ArgumentNullException(nameof(geolocationService));
            _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _locationId = locationId;
            _isModalMode = isModalMode;

            InitializeComponent();

            // Configure UI based on mode
           // CloseModal.IsVisible = _isModalMode;

            // Initialize ViewModel and load data
            InitializeViewModel();
        }

        private void InitializeViewModel()
        {
            // Create a view model instance with required services
            var viewModel = new LocationViewModel(_mediator, _mediaService, _geolocationService, _alertService);

            // Subscribe to error events
            viewModel.ErrorOccurred += ViewModel_ErrorOccurred;

            // Set as binding context
            BindingContext = viewModel;

            // Load location data if we have an ID
            if (_locationId > 0)
            {
                LoadLocationData(_locationId);
            }
        }

        private async void LoadLocationData(int id)
        {
            if (BindingContext is LocationViewModel viewModel)
            {
                try
                {
                    viewModel.IsBusy = true;
                    await viewModel.LoadLocationCommand.ExecuteAsync(id);
                }
                catch (Exception ex)
                {
                    await _alertService.ShowErrorAlertAsync($"Error loading location: {ex.Message}", "Error");
                    viewModel.ErrorMessage = $"Error loading location: {ex.Message}";
                    viewModel.IsError = true;
                }
                finally
                {
                    viewModel.IsBusy = false;
                }
            }
        }

        private void ViewModel_ErrorOccurred(object sender, OperationErrorEventArgs e)
        {
            // Display error if not already shown in the UI
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await _alertService.ShowErrorAlertAsync(e.Message, "Error");
            });
        }

        private async void WeatherButton_Pressed(object sender, EventArgs e)
        {
            if (BindingContext is LocationViewModel viewModel)
            {
                try
                {
                    // Navigate to WeatherDisplay for this location
                    var weatherPage = new WeatherDisplay(_mediator, _alertService, viewModel.Id);
                    await Navigation.PushModalAsync(new NavigationPage(weatherPage));
                }
                catch (Exception ex)
                {
                    await _alertService.ShowErrorAlertAsync($"Error opening weather: {ex.Message}", "Error");
                }
            }
        }

        private async void SunEvents_Pressed(object sender, EventArgs e)
        {
            if (BindingContext is LocationViewModel viewModel)
            {
                try
                {
                    // We would navigate to Sun Calculations here
                    // This will be implemented when we migrate that view
                    await _alertService.ShowInfoAlertAsync("Sun calculations feature will be available soon.", "Coming Soon");
                }
                catch (Exception ex)
                {
                    await _alertService.ShowErrorAlertAsync($"Error: {ex.Message}", "Error");
                }
            }
        }

        private void ImageButton_Pressed(object sender, EventArgs e)
        {
            // Handle close button press
            if (_isModalMode)
            {
                Navigation.PopModalAsync();
            }
            else
            {
                Navigation.PopAsync();
            }
        }

        private void CloseModal_Pressed(object sender, EventArgs e)
        {
            // Close the modal
            Navigation.PopModalAsync();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // Re-subscribe to ViewModel events in case BindingContext changed
            if (BindingContext is LocationViewModel viewModel)
            {
                viewModel.ErrorOccurred -= ViewModel_ErrorOccurred;
                viewModel.ErrorOccurred += ViewModel_ErrorOccurred;
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            // Unsubscribe from ViewModel events
            if (BindingContext is LocationViewModel viewModel)
            {
                viewModel.ErrorOccurred -= ViewModel_ErrorOccurred;
            }

            // Cancel any pending operations
            if (!_cts.IsCancellationRequested)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = new CancellationTokenSource();
            }
        }
    }
}