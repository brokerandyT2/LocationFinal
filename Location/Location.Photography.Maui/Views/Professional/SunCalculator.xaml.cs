using Location.Core.Application.Services;
using Location.Photography.Application.Services;
using Location.Photography.Maui.Controls;
using Location.Photography.ViewModels;
using Location.Photography.ViewModels.Events;
using MediatR;

namespace Location.Photography.Maui.Views.Professional
{
    public partial class SunCalculator : ContentPage
    {
        private readonly EnhancedSunCalculatorViewModel _viewModel;
        private readonly IAlertService _alertService;
        private readonly IMediator _mediator;
        private readonly IExposureCalculatorService _exposureCalculatorService;
        private SunPathDrawable _sunPathDrawable;
        private bool _isPopupVisible = false;

    /*    public SunCalculator()
        {
            InitializeComponent();
            _viewModel = new EnhancedSunCalculatorViewModel();
            BindingContext = _viewModel;
            InitializeSunPathCanvas();
        } */

        public SunCalculator(
            EnhancedSunCalculatorViewModel viewModel,
            IAlertService alertService,
            IMediator mediator,
            IExposureCalculatorService exposureCalculatorService)
        {
            InitializeComponent();

            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _exposureCalculatorService = exposureCalculatorService ?? throw new ArgumentNullException(nameof(exposureCalculatorService));
             LoadLocations();
            _viewModel.ErrorOccurred -= OnSystemError;
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;

            InitializeSunPathCanvas();

            if (_viewModel.SelectedLocation != null)
            {
                _viewModel.CalculateEnhancedSunDataAsync();
                UpdateSunPathCanvas();
            }
            BindingContext = _viewModel;
        }
        private async void LoadLocations()
        {
            try
            {
                if (_viewModel != null)
                {
                    await _viewModel.LoadLocationsAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading locations: {ex.Message}");
                await HandleErrorAsync(ex, "Error loading locations for sun calculator");
            }
        }
        protected override async void OnAppearing()
        {
            base.OnAppearing();

            try
            {
                if (_viewModel != null)
                {
                    _viewModel.ErrorOccurred -= OnSystemError;
                    _viewModel.ErrorOccurred += OnSystemError;

          

                    if (_viewModel.SelectedLocation != null)
                    {
                        await _viewModel.CalculateEnhancedSunDataAsync();
                        UpdateSunPathCanvas();
                    }

                    // Subscribe to property changes for sun path updates
                    
                }
            }
            catch (Exception ex)
            {
                await HandleErrorAsync(ex, "Error initializing enhanced sun calculator");
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            if (_viewModel != null)
            {
                _viewModel.ErrorOccurred -= OnSystemError;
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
                _viewModel?.Dispose();
            }
        }

        private void OnViewModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // Update sun path when relevant properties change
            if (e.PropertyName == nameof(_viewModel.SunPathPoints) ||
                e.PropertyName == nameof(_viewModel.CurrentAzimuth) ||
                e.PropertyName == nameof(_viewModel.CurrentElevation) ||
                e.PropertyName == nameof(_viewModel.SelectedLocation) ||
                e.PropertyName == nameof(_viewModel.SelectedDate))
            {
                MainThread.BeginInvokeOnMainThread(() => UpdateSunPathCanvas());
            }
        }

        private void InitializeSunPathCanvas()
        {
            try
            {
                _sunPathDrawable = new SunPathDrawable(_viewModel);
                SunPathCanvas.Drawable = _sunPathDrawable;

                // Add tap gesture recognizer for sun event interaction
                var tapGesture = new TapGestureRecognizer();
                tapGesture.Tapped += OnSunPathTapped;
                SunPathCanvas.GestureRecognizers.Add(tapGesture);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing sun path canvas: {ex.Message}");
            }
        }

        private void UpdateSunPathCanvas()
        {
            try
            {
                if (_sunPathDrawable != null && _viewModel != null)
                {
                    _sunPathDrawable.UpdateViewModel(_viewModel);
                    SunPathCanvas.Invalidate();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating sun path canvas: {ex.Message}");
            }
        }

        private async void OnSunPathTapped(object sender, TappedEventArgs e)
        {
            try
            {
                if (_isPopupVisible || _sunPathDrawable == null || _viewModel == null)
                    return;

                // Get tap position relative to the canvas
                var tapPosition = e.GetPosition((View)sender);
                if (!tapPosition.HasValue)
                    return;

                var canvasFrame = SunPathCanvas.Frame;
                var centerX = (float)canvasFrame.Width / 2;
                var groundY = (float)canvasFrame.Height * 0.75f;
                var radius = (float)canvasFrame.Width * 0.4f;

                // Check if tap hit any sun event
                var touchedEvent = _sunPathDrawable.GetTouchedEvent(
                    new PointF((float)tapPosition.Value.X, (float)tapPosition.Value.Y),
                    centerX, groundY, radius);

                if (touchedEvent != null)
                {
                    await ShowSunEventPopup(touchedEvent);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error handling sun path tap: {ex.Message}");
            }
        }

        private async Task ShowSunEventPopup(SunEventPoint sunEvent)
        {
            try
            {
                _isPopupVisible = true;

                var eventName = GetEventDisplayName(sunEvent.EventType);
                var message = $"{eventName}\n" +
                             $"Time: {sunEvent.Time}\n" +
                             $"Azimuth: {sunEvent.Azimuth:F1}°\n" +
                             $"Elevation: {sunEvent.Elevation:F1}°";

                await DisplayAlert("Sun Event", message, "OK");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing sun event popup: {ex.Message}");
            }
            finally
            {
                _isPopupVisible = false;
            }
        }

        private string GetEventDisplayName(SunEventType eventType)
        {
            return eventType switch
            {
                SunEventType.Sunrise => "🌅 Sunrise",
                SunEventType.Sunset => "🌅 Sunset",
                SunEventType.SolarNoon => "☀️ Solar Noon",
                SunEventType.CivilDawn => "🌄 Civil Dawn",
                SunEventType.CivilDusk => "🌆 Civil Dusk",
                SunEventType.GoldenHourStart => "✨ Golden Hour Start",
                SunEventType.GoldenHourEnd => "✨ Golden Hour End",
                SunEventType.Current => "📍 Current Position",
                _ => "Sun Event"
            };
        }

        private async void OnSystemError(object sender, OperationErrorEventArgs e)
        {
            try
            {
                var errorMessage = e.Source switch
                {
                    OperationErrorSource.Network => "Network connection issue. Please check your internet connection and try again.",
                    OperationErrorSource.Database => "Database error occurred. Please restart the app if the problem persists.",
                    OperationErrorSource.Sensor => "Sensor access error. Please check app permissions.",
                    OperationErrorSource.Permission => "Permission required. Please grant necessary permissions in settings.",
                    OperationErrorSource.Validation => e.Message,
                    _ => $"An error occurred: {e.Message}"
                };

                var retry = await DisplayAlert(
                    "Enhanced Sun Calculator Error",
                    errorMessage,
                    "Retry",
                    "Cancel");

                if (retry && sender is EnhancedSunCalculatorViewModel viewModel)
                {
                    await viewModel.RetryLastCommandAsync();
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Multiple errors occurred: {ex.Message}", "OK");
            }
        }

        private async Task HandleErrorAsync(Exception ex, string context)
        {
            var errorMessage = $"{context}: {ex.Message}";
            System.Diagnostics.Debug.WriteLine(errorMessage);

            try
            {
                if (_alertService != null)
                {
                    await _alertService.ShowErrorAlertAsync(errorMessage, "Enhanced Sun Calculator Error");
                }
                else
                {
                    await DisplayAlert("Enhanced Sun Calculator Error", errorMessage, "OK");
                }
            }
            catch
            {
                await DisplayAlert("Error", "Multiple errors occurred. Please restart the app.", "OK");
            }
        }

        private async void OnCalibrateButtonClicked(object sender, EventArgs e)
        {
            try
            {
                if (sender is Button button && button.CommandParameter is string evText)
                {
                    if (double.TryParse(evText, out double actualEV))
                    {
                        if (actualEV >= -10 && actualEV <= 20)
                        {
                            await _viewModel.CalibrateWithLightMeterAsync(actualEV);

                            if (ActualEVEntry != null)
                            {
                                ActualEVEntry.Text = string.Empty;
                            }

                            await DisplayAlert("Calibration Complete",
                                $"Light meter calibrated with EV {actualEV:F1}. Future predictions will be more accurate.",
                                "OK");
                        }
                        else
                        {
                            await DisplayAlert("Invalid EV Value",
                                "Please enter an EV value between -10 and +20.",
                                "OK");
                        }
                    }
                    else
                    {
                        await DisplayAlert("Invalid Input",
                            "Please enter a valid numeric EV value (e.g., 12.5).",
                            "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                await HandleErrorAsync(ex, "Error during light meter calibration");
            }
        }

        private async void OnLocationSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (_viewModel != null && _viewModel.SelectedLocation != null)
                {
                    await _viewModel.CalculateEnhancedSunDataAsync();
                    UpdateSunPathCanvas();
                }
            }
            catch (Exception ex)
            {
                await HandleErrorAsync(ex, "Error updating location selection");
            }
        }

        private async void OnDateSelectionChanged(object sender, DateChangedEventArgs e)
        {
            try
            {
                if (_viewModel != null && _viewModel.SelectedLocation != null)
                {
                    await _viewModel.CalculateEnhancedSunDataAsync();
                    UpdateSunPathCanvas();
                }
            }
            catch (Exception ex)
            {
                await HandleErrorAsync(ex, "Error updating date selection");
            }
        }

        // Command handlers for modal navigation
        public async Task OpenLightMeterModal(HourlyPredictionDisplayModel prediction)
        {
            try
            {
                if (_exposureCalculatorService == null)
                {
                    await DisplayAlert("Service Unavailable", "Light meter service is not available.", "OK");
                    return;
                }

                // Create light meter page with pre-populated values
                var lightMeterPage = new Professional.LightMeter();

                // Pre-populate the light meter with prediction values
                if (lightMeterPage.BindingContext is LightMeterViewModel lightMeterViewModel)
                {
                    // Set predicted values
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        try
                        {
                            lightMeterViewModel.SelectedEV = prediction.PredictedEV;
                            // Additional pre-population logic could go here
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error pre-populating light meter: {ex.Message}");
                        }
                    });
                }

                await Navigation.PushModalAsync(lightMeterPage);
            }
            catch (Exception ex)
            {
                await HandleErrorAsync(ex, "Error opening light meter");
            }
        }

        public async Task OpenCameraTipsModal(HourlyPredictionDisplayModel prediction)
        {
            try
            {
                // Check if prediction settings match any tips
                // This would query the tips database to find matching f-stop, shutter speed, ISO combinations

                // For now, show a placeholder modal
                await DisplayAlert("Camera Tips",
                    $"Tips for f/{prediction.SuggestedAperture} @ {prediction.SuggestedShutterSpeed} ISO {prediction.SuggestedISO}\n\n" +
                    "Camera tips feature coming soon!",
                    "OK");

                // Future implementation would open actual tips modal:
                // var tipsPage = new TipsPage(tipId);
                // await Navigation.PushModalAsync(tipsPage);
            }
            catch (Exception ex)
            {
                await HandleErrorAsync(ex, "Error opening camera tips");
            }
        }

        // Manual refresh method for sun path when data changes
        public void RefreshSunPath()
        {
            try
            {
                UpdateSunPathCanvas();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error refreshing sun path: {ex.Message}");
            }
        }

        // Helper method to get current sun path drawable for external access
        public SunPathDrawable GetSunPathDrawable()
        {
            return _sunPathDrawable;
        }
    }
}