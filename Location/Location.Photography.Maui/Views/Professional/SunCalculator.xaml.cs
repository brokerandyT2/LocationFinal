
using Location.Core.Application.Services;
using Location.Photography.ViewModels;
using Location.Photography.ViewModels.Events;
using Location.Photography.Application.Services;
using MediatR;
using Microsoft.Maui.Graphics;
namespace Location.Photography.Maui.Views.Professional
{
    public partial class SunCalculator : ContentPage
    {
        private readonly EnhancedSunCalculatorViewModel _viewModel;
        private readonly IAlertService _alertService;
        private readonly IMediator _mediator;
        private readonly IExposureCalculatorService _exposureCalculatorService;
        public SunCalculator()
        {
            InitializeComponent();
            _viewModel = new EnhancedSunCalculatorViewModel();
            BindingContext = _viewModel;
            InitializeSunPathCanvas();
        }

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
            _viewModel.LoadLocationsAsync();
            LocationPicker.SelectedIndex = 0;
            BindingContext = _viewModel;
            InitializeSunPathCanvas();
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

                    await _viewModel.LoadLocationsAsync();

                    if (_viewModel.SelectedLocation != null)
                    {
                        await _viewModel.CalculateEnhancedSunDataAsync();
                        UpdateSunPathCanvas();
                    }

                    // Subscribe to property changes for sun path updates
                    _viewModel.PropertyChanged += OnViewModelPropertyChanged;
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
            if (e.PropertyName == nameof(_viewModel.SunPathPoints) ||
                e.PropertyName == nameof(_viewModel.CurrentAzimuth) ||
                e.PropertyName == nameof(_viewModel.CurrentElevation))
            {
                MainThread.BeginInvokeOnMainThread(() => UpdateSunPathCanvas());
            }
        }

        private void InitializeSunPathCanvas()
        {
            SunPathCanvas.Drawable = new SunPathDrawable(_viewModel);
        }

        private void UpdateSunPathCanvas()
        {
            if (SunPathCanvas?.Drawable is SunPathDrawable drawable)
            {
                drawable.UpdateSunPath(_viewModel);
                SunPathCanvas.Invalidate();
            }
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
                var lightMeterPage = new Professional.LightMeter();// _mediator, _alertService, null, null, _exposureCalculatorService);

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
    }

    // Custom drawable for sun path canvas
    public class SunPathDrawable : IDrawable
    {
        private EnhancedSunCalculatorViewModel _viewModel;
        private readonly Color _skyColor = Color.FromRgb(135, 206, 235); // Light blue
        private readonly Color _arcColor = Color.FromRgb(255, 215, 0); // Gold
        private readonly Color _sunColor = Color.FromRgb(255, 165, 0); // Orange
        private readonly Color _markerColor = Color.FromRgb(139, 69, 19); // Brown
        private readonly Color _nightArcColor = Color.FromRgb(72, 61, 139); // Dark slate blue

        public SunPathDrawable(EnhancedSunCalculatorViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        public void UpdateSunPath(EnhancedSunCalculatorViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            if (_viewModel == null) return;

            var width = dirtyRect.Width;
            var height = dirtyRect.Height;
            var centerX = width / 2;
            var centerY = height - 20; // Ground level
            var radius = Math.Min(width, height) * 0.4f;

            // Clear background
            canvas.FillColor = _skyColor;
            canvas.FillRectangle(dirtyRect);

            // Draw ground line
            canvas.StrokeColor = Colors.Green;
            canvas.StrokeSize = 2;
            canvas.DrawLine(0, centerY, width, centerY);

            // Draw 24-hour arc (semicircle from east to west)
            DrawSunArc(canvas, centerX, centerY, radius, true); // Day arc
            DrawSunArc(canvas, centerX, centerY, radius, false); // Night arc

            // Draw hour markers
            DrawHourMarkers(canvas, centerX, centerY, radius);

            // Draw major sun event markers
            DrawSunEventMarkers(canvas, centerX, centerY, radius);

            // Draw current sun position
            DrawCurrentSunPosition(canvas, centerX, centerY, radius);

            // Draw labels
            DrawLabels(canvas, centerX, centerY, width, height);
        }

        private void DrawSunArc(ICanvas canvas, float centerX, float centerY, float radius, bool isDayArc)
        {
            canvas.StrokeColor = isDayArc ? _arcColor : _nightArcColor;
            canvas.StrokeSize = 3;

            if (isDayArc)
            {
                // Day arc (above ground)
                canvas.DrawArc(centerX - radius, centerY - radius, radius * 2, radius * 2, 0, 180, false, false);
            }
            else
            {
                // Night arc (below ground, dashed)
                canvas.StrokeDashPattern = new float[] { 5, 5 };
                canvas.DrawArc(centerX - radius, centerY, radius * 2, radius * 2, 0, 180, false, false);
                canvas.StrokeDashPattern = null;
            }
        }

        private void DrawHourMarkers(ICanvas canvas, float centerX, float centerY, float radius)
        {
            canvas.StrokeColor = _markerColor;
            canvas.StrokeSize = 1;

            // Draw hour markers (every 2 hours for clarity)
            for (int hour = 0; hour <= 24; hour += 2)
            {
                var angle = (hour / 24.0) * Math.PI; // 0 to π for 24 hours
                var x = centerX + (float)(radius * Math.Cos(Math.PI - angle));
                var y = centerY - (float)(radius * Math.Sin(Math.PI - angle));

                // Draw tick mark
                var tickLength = (hour % 6 == 0) ? 8 : 4; // Longer ticks for 6AM, 12PM, 6PM, 12AM
                var outerX = centerX + (float)((radius + tickLength) * Math.Cos(Math.PI - angle));
                var outerY = centerY - (float)((radius + tickLength) * Math.Sin(Math.PI - angle));

                canvas.DrawLine(x, y, outerX, outerY);

                // Draw hour labels for major times
                if (hour % 6 == 0)
                {
                    var labelX = centerX + (float)((radius + 15) * Math.Cos(Math.PI - angle));
                    var labelY = centerY - (float)((radius + 15) * Math.Sin(Math.PI - angle));

                    var timeLabel = hour switch
                    {
                        0 => "12 AM",
                        6 => "6 AM",
                        12 => "12 PM",
                        18 => "6 PM",
                        24 => "12 AM",
                        _ => $"{hour}:00"
                    };

                    canvas.FontColor = Colors.Black;
                    canvas.FontSize = 10;
                    canvas.DrawString(timeLabel, labelX - 15, labelY - 5, 30, 10, HorizontalAlignment.Center, VerticalAlignment.Center);
                }
            }
        }

        private void DrawSunEventMarkers(ICanvas canvas, float centerX, float centerY, float radius)
        {
            if (_viewModel.SunPathPoints == null || !_viewModel.SunPathPoints.Any()) return;

            canvas.StrokeColor = Colors.Red;
            canvas.FillColor = Colors.Red;
            canvas.StrokeSize = 2;

            // Find key sun events from sun path points
            var sunrisePoint = _viewModel.SunPathPoints.FirstOrDefault(p => p.Elevation > 0);
            var sunsetPoint = _viewModel.SunPathPoints.LastOrDefault(p => p.Elevation > 0);
            var noonPoint = _viewModel.SunPathPoints.OrderByDescending(p => p.Elevation).FirstOrDefault();

            // Draw sunrise marker
            if (sunrisePoint != null)
            {
                DrawEventMarker(canvas, centerX, centerY, radius, sunrisePoint, "SR");
            }

            // Draw solar noon marker
            if (noonPoint != null)
            {
                DrawEventMarker(canvas, centerX, centerY, radius, noonPoint, "N");
            }

            // Draw sunset marker
            if (sunsetPoint != null)
            {
                DrawEventMarker(canvas, centerX, centerY, radius, sunsetPoint, "SS");
            }
        }

        private void DrawEventMarker(ICanvas canvas, float centerX, float centerY, float radius, SunPathPoint point, string label)
        {
            var hour = point.Time.Hour + (point.Time.Minute / 60.0);
            var angle = (hour / 24.0) * Math.PI;
            var elevationFactor = Math.Max(0, point.Elevation / 90.0); // Normalize elevation
            var markerRadius = radius * elevationFactor;

            var x = centerX + (float)(markerRadius * Math.Cos(Math.PI - angle));
            var y = centerY - (float)(markerRadius * Math.Sin(Math.PI - angle));

            // Draw marker circle
            canvas.FillCircle(x, y, 4);

            // Draw label
            canvas.FontColor = Colors.Red;
            canvas.FontSize = 8;
            canvas.DrawString(label, x - 10, y - 15, 20, 10, HorizontalAlignment.Center, VerticalAlignment.Center);
        }

        private void DrawCurrentSunPosition(ICanvas canvas, float centerX, float centerY, float radius)
        {
            if (_viewModel.CurrentElevation <= 0) return; // Sun is below horizon

            var currentTime = DateTime.Now;
            var hour = currentTime.Hour + (currentTime.Minute / 60.0);
            var angle = (hour / 24.0) * Math.PI;
            var elevationFactor = Math.Max(0, _viewModel.CurrentElevation / 90.0);
            var sunRadius = radius * elevationFactor;

            var x = centerX + (float)(sunRadius * Math.Cos(Math.PI - angle));
            var y = centerY - (float)(sunRadius * Math.Sin(Math.PI - angle));

            // Draw sun
            canvas.FillColor = _sunColor;
            canvas.FillCircle(x, y, 8);

            // Draw sun rays
            canvas.StrokeColor = _sunColor;
            canvas.StrokeSize = 2;
            for (int i = 0; i < 8; i++)
            {
                var rayAngle = (i * Math.PI * 2) / 8;
                var rayStartX = x + (float)(10 * Math.Cos(rayAngle));
                var rayStartY = y + (float)(10 * Math.Sin(rayAngle));
                var rayEndX = x + (float)(15 * Math.Cos(rayAngle));
                var rayEndY = y + (float)(15 * Math.Sin(rayAngle));
                canvas.DrawLine(rayStartX, rayStartY, rayEndX, rayEndY);
            }
        }

        private void DrawLabels(ICanvas canvas, float centerX, float centerY, float width, float height)
        {
            canvas.FontColor = Colors.Black;
            canvas.FontSize = 12;

            // Draw compass directions
            canvas.DrawString("E", 10, centerY - 10, 20, 20, HorizontalAlignment.Center, VerticalAlignment.Center);
            canvas.DrawString("W", width - 30, centerY - 10, 20, 20, HorizontalAlignment.Center, VerticalAlignment.Center);
            canvas.DrawString("S", centerX - 10, centerY + 10, 20, 20, HorizontalAlignment.Center, VerticalAlignment.Center);

            // Draw title
            canvas.FontSize = 14;
            canvas.DrawString("Sun Path - 24 Hour View", centerX - 60, 10, 120, 20, HorizontalAlignment.Center, VerticalAlignment.Center);
        }
    }
}