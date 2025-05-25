using Location.Core.Application.Locations.Queries.GetLocationById;
using Location.Core.Application.Services;
using Location.Core.Maui.Services;
using Location.Core.Maui.Resources;
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
        private readonly INavigationService _navigationService;
        private readonly IErrorDisplayService _errorDisplayService;
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
            INavigationService navigationService,
            IErrorDisplayService errorDisplayService,
            int locationId = 0,
            bool isModalMode = false)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _mediaService = mediaService ?? throw new ArgumentNullException(nameof(mediaService));
            _geolocationService = geolocationService ?? throw new ArgumentNullException(nameof(geolocationService));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _errorDisplayService = errorDisplayService ?? throw new ArgumentNullException(nameof(errorDisplayService));
            _locationId = locationId;
            _isModalMode = isModalMode;

            InitializeComponent();

            // Initialize ViewModel and load data
            InitializeViewModel();
        }

        private void InitializeViewModel()
        {
            // Create a view model instance with required services
            var viewModel = new LocationViewModel(_mediator, _mediaService, _geolocationService, _errorDisplayService);

            // Subscribe to system error events
            viewModel.ErrorOccurred += OnSystemError;

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
                    await viewModel.ExecuteAndTrackAsync(viewModel.LoadLocationCommand, id);
                }
                catch (Exception ex)
                {
                    viewModel.OnSystemError($"Error loading location: {ex.Message}");
                }
                finally
                {
                    viewModel.IsBusy = false;
                }
            }
        }

        private async void WeatherButton_Pressed(object sender, EventArgs e)
        {
            if (BindingContext is LocationViewModel viewModel)
            {
                try
                {
                    // Navigate to WeatherDisplay for this location
                    var weatherPage = new WeatherDisplay(_mediator, _errorDisplayService, viewModel.Id);
                    await Navigation.PushModalAsync(new NavigationPage(weatherPage));
                }
                catch (Exception ex)
                {
                    await DisplayAlert(AppResources.Error, $"Error opening weather: {ex.Message}", AppResources.OK);
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
                    await DisplayAlert("Coming Soon", "Sun calculations feature will be available soon.", AppResources.OK);
                }
                catch (Exception ex)
                {
                    await DisplayAlert(AppResources.Error, $"Error: {ex.Message}", AppResources.OK);
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

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // Re-subscribe to ViewModel events in case BindingContext changed
            if (BindingContext is LocationViewModel viewModel)
            {
                viewModel.ErrorOccurred -= OnSystemError;
                viewModel.ErrorOccurred += OnSystemError;
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            // Unsubscribe from ViewModel events
            if (BindingContext is LocationViewModel viewModel)
            {
                viewModel.ErrorOccurred -= OnSystemError;
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