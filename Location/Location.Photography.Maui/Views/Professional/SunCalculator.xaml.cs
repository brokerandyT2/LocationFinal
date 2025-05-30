// Location.Photography.Maui/Views/Professional/SunCalculator.xaml.cs
using Location.Core.Application.Services;
using Location.Photography.ViewModels;
using Location.Photography.ViewModels.Events;

namespace Location.Photography.Maui.Views.Professional
{
    public partial class SunCalculator : ContentPage
    {
        private readonly EnhancedSunCalculatorViewModel _viewModel;
        private readonly IAlertService _alertService;

        public SunCalculator()
        {
            InitializeComponent();
            // Design-time constructor - uses basic ViewModel for preview
            _viewModel = new EnhancedSunCalculatorViewModel(null, null, null);
            BindingContext = _viewModel;
        }

        public SunCalculator(EnhancedSunCalculatorViewModel viewModel, IAlertService alertService)
        {
            InitializeComponent();

            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));

            BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            try
            {
                if (_viewModel != null)
                {
                    // Subscribe to error events
                    _viewModel.ErrorOccurred -= OnSystemError;
                    _viewModel.ErrorOccurred += OnSystemError;

                    // Initialize the enhanced sun calculator
                    await _viewModel.LoadLocationsAsync();

                    // If we have a selected location, calculate enhanced data
                    if (_viewModel.SelectedLocation != null)
                    {
                        await _viewModel.CalculateEnhancedSunDataAsync();
                    }
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

                // Dispose of any resources (cancellation tokens, etc.)
                _viewModel?.Dispose();
            }
        }

        private async void OnSystemError(object sender, OperationErrorEventArgs e)
        {
            try
            {
                // Determine error message based on source
                var errorMessage = e.Source switch
                {
                    OperationErrorSource.Network => "Network connection issue. Please check your internet connection and try again.",
                    OperationErrorSource.Database => "Database error occurred. Please restart the app if the problem persists.",
                    OperationErrorSource.Sensor => "Sensor access error. Please check app permissions.",
                    OperationErrorSource.Permission => "Permission required. Please grant necessary permissions in settings.",
                    OperationErrorSource.Validation => e.Message,
                    _ => $"An error occurred: {e.Message}"
                };

                // Show error with retry option
                var retry = await DisplayAlert(
                    "Enhanced Sun Calculator Error",
                    errorMessage,
                    "Retry",
                    "Cancel");

                if (retry && sender is EnhancedSunCalculatorViewModel viewModel)
                {
                    // Retry the last command if available
                    await viewModel.RetryLastCommandAsync();
                }
            }
            catch (Exception ex)
            {
                // Fallback error handling
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
                // Last resort - simple display alert
                await DisplayAlert("Error", "Multiple errors occurred. Please restart the app.", "OK");
            }
        }

        // Handle calibration button with validation
        private async void OnCalibrateButtonClicked(object sender, EventArgs e)
        {
            try
            {
                if (sender is Button button && button.CommandParameter is string evText)
                {
                    if (double.TryParse(evText, out double actualEV))
                    {
                        // Validate EV range (-10 to +20 is reasonable for light meter)
                        if (actualEV >= -10 && actualEV <= 20)
                        {
                            await _viewModel.CalibrateWithLightMeterAsync(actualEV);

                            // Clear the entry after successful calibration
                            if (ActualEVEntry != null)
                            {
                                ActualEVEntry.Text = string.Empty;
                            }

                            // Show success message
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

        // Handle location selection changes
        private async void OnLocationSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (_viewModel != null && _viewModel.SelectedLocation != null)
                {
                    // Automatically recalculate when location changes
                    await _viewModel.CalculateEnhancedSunDataAsync();
                }
            }
            catch (Exception ex)
            {
                await HandleErrorAsync(ex, "Error updating location selection");
            }
        }

        // Handle date selection changes
        private async void OnDateSelectionChanged(object sender, DateChangedEventArgs e)
        {
            try
            {
                if (_viewModel != null && _viewModel.SelectedLocation != null)
                {
                    // Automatically recalculate when date changes
                    await _viewModel.CalculateEnhancedSunDataAsync();
                }
            }
            catch (Exception ex)
            {
                await HandleErrorAsync(ex, "Error updating date selection");
            }
        }

        // Handle refresh gesture (if CollectionView supports it)
        private async void OnRefreshRequested(object sender, EventArgs e)
        {
            try
            {
                if (_viewModel != null)
                {
                    await _viewModel.LoadLocationsAsync();

                    if (_viewModel.SelectedLocation != null)
                    {
                        await _viewModel.CalculateEnhancedSunDataAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                await HandleErrorAsync(ex, "Error refreshing data");
            }
        }

        // Handle optimal window tapped for more details
        private async void OnOptimalWindowTapped(object sender, TappedEventArgs e)
        {
            try
            {
                if (e.Parameter is OptimalWindowDisplayModel window)
                {
                    var details = $"Shooting Window Details:\n\n" +
                                 $"Type: {window.WindowType}\n" +
                                 $"Time: {window.GetFormattedTimeRange(_viewModel.TimeFormat)}\n" +
                                 $"Duration: {window.DurationDisplay}\n" +
                                 $"Light Quality: {window.LightQuality}\n" +
                                 $"Optimal For: {window.OptimalFor}\n" +
                                 $"Confidence: {window.ConfidenceDisplay}";

                    await DisplayAlert("Shooting Window Details", details, "OK");
                }
            }
            catch (Exception ex)
            {
                await HandleErrorAsync(ex, "Error displaying window details");
            }
        }

        // Handle hourly prediction tapped for detailed breakdown
        private async void OnHourlyPredictionTapped(object sender, TappedEventArgs e)
        {
            try
            {
                if (e.Parameter is HourlyPredictionDisplayModel prediction)
                {
                    var details = $"Light Prediction Details:\n\n" +
                                 $"Time: {prediction.LocationTimeDisplay}\n" +
                                 $"Predicted EV: {prediction.FormattedPrediction}\n" +
                                 $"Suggested Settings: {prediction.FormattedSettings}\n" +
                                 $"Light Quality: {prediction.LightQuality}\n" +
                                 $"Color Temperature: {prediction.ColorTemperature:F0}K\n" +
                                 $"Confidence: {prediction.ConfidenceDisplay}\n\n" +
                                 $"Recommendations:\n{prediction.Recommendations}";

                    await DisplayAlert("Light Prediction Details", details, "OK");
                }
            }
            catch (Exception ex)
            {
                await HandleErrorAsync(ex, "Error displaying prediction details");
            }
        }

        // Navigation awareness for proper cleanup
       
    }
}