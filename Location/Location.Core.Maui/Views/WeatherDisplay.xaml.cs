using Location.Core.Application.Services;
using Location.Core.Maui.Resources;
using Location.Core.ViewModels;
using MediatR;

namespace Location.Core.Maui.Views
{
    public partial class WeatherDisplay : ContentPage
    {
        private readonly IMediator _mediator;
        private readonly IErrorDisplayService _errorDisplayService;
        private readonly int _locationId;
        private CancellationTokenSource _cts = new CancellationTokenSource();

        /// <summary>
        /// Default constructor for design-time and XAML preview
        /// </summary>
        public WeatherDisplay()
        {
            InitializeComponent();

            // Set a default view model for design time
            BindingContext = new WeatherViewModel();
        }

        /// <summary>
        /// Main constructor with DI
        /// </summary>
        public WeatherDisplay(
            IMediator mediator,
            IErrorDisplayService errorDisplayService,
            int locationId)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _errorDisplayService = errorDisplayService ?? throw new ArgumentNullException(nameof(errorDisplayService));
            _locationId = locationId;

            InitializeComponent();

            // Initialize view model and load data
            InitializeViewModel();
        }

        private void InitializeViewModel()
        {
            // Create view model instance
            var viewModel = new WeatherViewModel(_mediator, _errorDisplayService);

            // Subscribe to system error events
            viewModel.ErrorOccurred += OnSystemError;

            // Set as binding context
            BindingContext = viewModel;

            // Load weather data
            if (_locationId > 0)
            {
                LoadWeatherData(_locationId);
            }
        }

        private async void LoadWeatherData(int locationId)
        {
            if (BindingContext is WeatherViewModel viewModel)
            {
                try
                {
                    viewModel.IsBusy = true;
                    await viewModel.ExecuteAndTrackAsync(viewModel.LoadWeatherCommand, locationId);
                }
                catch (Exception ex)
                {
                    viewModel.OnSystemError($"Error loading weather data: {ex.Message}");
                }
                finally
                {
                    viewModel.IsBusy = false;
                }
            }
        }

        private async void OnSystemError(object sender, OperationErrorEventArgs e)
        {
            var retry = await DisplayAlert(
                AppResources.Error,
                $"{e.Message}. Click OK to try again.",
                AppResources.OK,
                AppResources.Cancel);

            if (retry && sender is WeatherViewModel viewModel)
            {
                await viewModel.RetryLastCommandAsync();
            }
        }

        private void ImageButton_Pressed(object sender, EventArgs e)
        {
            // Close the modal
            Navigation.PopModalAsync();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // Re-subscribe to ViewModel events in case BindingContext changed
            if (BindingContext is WeatherViewModel viewModel)
            {
                viewModel.ErrorOccurred -= OnSystemError;
                viewModel.ErrorOccurred += OnSystemError;
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            // Unsubscribe from ViewModel events
            if (BindingContext is WeatherViewModel viewModel)
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