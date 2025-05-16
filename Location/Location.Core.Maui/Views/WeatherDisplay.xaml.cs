using Location.Core.Application.Queries.Weather;
using Location.Core.Application.Services;
using Location.Core.ViewModels;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Core.Maui.Views
{
    public partial class WeatherDisplay : ContentPage
    {
        private readonly IMediator _mediator;
        private readonly IAlertService _alertService;
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
            IAlertService alertService,
            int locationId)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
            _locationId = locationId;

            InitializeComponent();

            // Initialize view model and load data
            InitializeViewModel();
        }

        private void InitializeViewModel()
        {
            // Create view model instance
            var viewModel = new WeatherViewModel(_mediator, _alertService);

            // Subscribe to error events
            viewModel.ErrorOccurred += ViewModel_ErrorOccurred;

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
                    await viewModel.LoadWeatherCommand.ExecuteAsync(locationId);
                }
                catch (Exception ex)
                {
                    await _alertService.ShowErrorAlertAsync($"Error loading weather data: {ex.Message}", "Error");
                    viewModel.ErrorMessage = $"Error loading weather data: {ex.Message}";
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
                viewModel.ErrorOccurred -= ViewModel_ErrorOccurred;
                viewModel.ErrorOccurred += ViewModel_ErrorOccurred;
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            // Unsubscribe from ViewModel events
            if (BindingContext is WeatherViewModel viewModel)
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