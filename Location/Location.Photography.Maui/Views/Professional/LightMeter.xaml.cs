#if ANDROID
using Location.Core.Application.Common.Interfaces.Persistence;
using Location.Core.Application.Services;
using Location.Photography.Infrastructure;
using Location.Photography.ViewModels;
using Location.Photography.ViewModels.Events;
using Location.Photography.Maui.Platforms.Android;
using Location.Photography.Application.Services;
using Location.Core.Application.Common.Models;
using MediatR;
using System.Threading.Tasks;
using System.Timers;

namespace Location.Photography.Maui.Views.Professional
{
    public partial class LightMeter : ContentPage
    {
        private readonly IMediator _mediator;
        private readonly IAlertService _alertService;
        private readonly ISettingRepository _settingRepository;
        private readonly ILightSensorService _lightSensorService;
        private readonly IExposureCalculatorService _exposureCalculatorService;
        private LightMeterViewModel _viewModel;
        private readonly ISceneEvaluationService _imageAnalysisService;
        private System.Timers.Timer _sensorTimer;
        private int _refreshInterval = 2000; // Default 2 seconds
        private bool _isMonitoring = false;

        public LightMeter()
        {
            InitializeComponent();
            _viewModel = new LightMeterViewModel();
            _viewModel.LoadExposureArraysAsync();

            BindingContext = _viewModel;
        }

        public LightMeter(
            IMediator mediator,
            IAlertService alertService,
            ISettingRepository settingRepository,
            ILightSensorService lightSensorService,
            IExposureCalculatorService exposureCalculatorService, ISceneEvaluationService sceneEvaluationService)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
            _settingRepository = settingRepository ?? throw new ArgumentNullException(nameof(settingRepository));
            _lightSensorService = lightSensorService ?? throw new ArgumentNullException(nameof(lightSensorService));
            _exposureCalculatorService = exposureCalculatorService ?? throw new ArgumentNullException(nameof(exposureCalculatorService));
            _imageAnalysisService = sceneEvaluationService ?? throw new ArgumentNullException(nameof(sceneEvaluationService));
            InitializeComponent();
            ApertureSlider.ValueChanged += OnApertureChanged;
            IsoSlider.ValueChanged += OnIsoChanged;
            ShutterSpeedSlider.ValueChanged += OnShutterSpeedChanged;
            EvSlider.ValueChanged += OnEvChanged;
            FullStepRadio.CheckedChanged += OnStepChanged;
            HalfStepRadio.CheckedChanged += OnStepChanged;
            ThirdStepRadio.CheckedChanged += OnStepChanged;
            MeasureButton.Clicked += OnMeasurePressed;


            InitializeViewModel();
            LoadRefreshInterval();
            // Start monitoring when the page appears
            //StartLightSensorMonitoring();


        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // DON'T start monitoring automatically
        }

        private bool _disposed = false;

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            try
            {
                _disposed = true;
                StopLightSensorMonitoring();
                _isMonitoring = false;

                if (_viewModel != null)
                {
                    _viewModel.ErrorOccurred -= OnSystemError;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during page cleanup: {ex.Message}");
            }
        }


        private async void InitializeViewModel()
        {
            if (_mediator != null && _settingRepository != null && _exposureCalculatorService != null)
            {
                _viewModel = new LightMeterViewModel(_mediator, _settingRepository, _exposureCalculatorService, null);
            }
            else
            {
                _viewModel = new LightMeterViewModel();
            }

            BindingContext = _viewModel;
            _viewModel.ErrorOccurred -= OnSystemError;
            _viewModel.ErrorOccurred += OnSystemError;

            try
            {
                _viewModel.IsBusy = true;

                // Initialize the arrays for sliders
                await _viewModel.LoadExposureArraysAsync();

            }
            catch (Exception ex)
            {
                _viewModel.ErrorMessage = $"Error initializing light meter: {ex.Message}";
                _viewModel.IsError = true;
                System.Diagnostics.Debug.WriteLine($"Light meter initialization error: {ex}");
            }
            finally
            {
                _viewModel.IsBusy = false;
            }
        }

        private async void LoadRefreshInterval()
        {
            try
            {
                if (_settingRepository != null)
                {
                    // Get refresh interval from settings using MagicStrings.CameraRefresh
                    var setting = await _settingRepository.GetByKeyAsync(MagicStrings.CameraRefresh);
                    if (int.TryParse(setting.Value, out int interval))
                    {
                        _refreshInterval = interval; // Convert to milliseconds
                        System.Diagnostics.Debug.WriteLine($"Loaded refresh interval: {_refreshInterval}ms");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Using default refresh interval: {_refreshInterval}ms");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading refresh interval: {ex.Message}");
            }
        }
        private void StartLightSensorMonitoring()
        {
            try
            {
                if (_lightSensorService != null)
                {
                    _lightSensorService.StartListening();

                    // Dispose existing timer if it exists
                    _sensorTimer?.Stop();
                    _sensorTimer?.Dispose();

                    // Initialize sensor update timer with interval from settings
                    _sensorTimer = new System.Timers.Timer(_refreshInterval);
                    _sensorTimer.Elapsed += OnSensorTimerElapsed;
                    _sensorTimer.AutoReset = true;
                    _sensorTimer.Start();

                    System.Diagnostics.Debug.WriteLine($"Light sensor monitoring started with {_refreshInterval}ms interval");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Light sensor service not available");
                    // Set a default lux value for testing/fallback
                    _viewModel?.UpdateLightReading(100f);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error starting light sensor monitoring: {ex.Message}");

                // Show user-friendly message about sensor not available
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    if (_alertService != null)
                    {
                        await _alertService.ShowErrorAlertAsync(
                            "Light sensor is not available on this device. Using default values.",
                            "Sensor Unavailable");
                    }
                });
            }
        }
        private async void OnSensorTimerElapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                // Early exit if disposed or inactive
                if (_sensorTimer == null || _lightSensorService == null || _viewModel == null)
                    return;

                float currentLux = _lightSensorService.GetCurrentLux();

                // Do calculations on background thread to avoid main thread blocking
                await Task.Run(() =>
                {
                    try
                    {
                        // Perform heavy calculations off UI thread
                        var calculatedEV = PerformEVCalculation(currentLux);

                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            try
                            {
                                // Check if still active before updating UI
                                if (_viewModel != null && !_disposed)
                                {
                                    // Only update UI properties on main thread
                                    _viewModel.UpdateLightReading(currentLux);
                                    _viewModel.SetCalculatedEV(calculatedEV);

                                    System.Diagnostics.Debug.WriteLine($"Timer reading: {currentLux} lux, Calculated EV: {calculatedEV}");
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Error updating UI from sensor: {ex.Message}");
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error in background sensor calculation: {ex.Message}");
                    }
                }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error reading light sensor: {ex.Message}");
            }
        }
        private double PerformEVCalculation(float lux)
        {
            try
            {
                if (lux <= 0)
                {
                    return 0;
                }

                // Parse current values (heavy string parsing moved to background)
                double aperture = ParseAperture(_viewModel.SelectedAperture);
                int iso = ParseIso(_viewModel.SelectedIso);

                if (aperture <= 0 || iso <= 0)
                {
                    return 0;
                }

                // Calculate EV using standard formula
                const double CalibrationConstant = 12.5;
                double ev = Math.Log2((lux * aperture * aperture) / (CalibrationConstant * iso));

                // Round to step precision
                return RoundToStep(ev);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error calculating EV: {ex.Message}");
                return 0;
            }
        }
        private double RoundToStep(double ev)
        {
            // Round EV to the appropriate step increment
            double stepSize = _viewModel.CurrentStep switch
            {
                ExposureIncrements.Full => 1.0,
                ExposureIncrements.Half => 0.5,
                ExposureIncrements.Third => 1.0 / 3.0,
                _ => 1.0 / 3.0
            };

            return Math.Round(ev / stepSize) * stepSize;
        }
        private int ParseIso(string iso)
        {
            if (string.IsNullOrWhiteSpace(iso))
                return 100;

            if (int.TryParse(iso, out int value))
            {
                return value;
            }

            return 100; // Default fallback
        }
        private double ParseAperture(string aperture)
        {
            if (string.IsNullOrWhiteSpace(aperture))
                return 5.6;

            // Handle f-stop format like "f/2.8"
            if (aperture.StartsWith("f/"))
            {
                string value = aperture.Substring(2).Replace(",", ".");
                if (double.TryParse(value, out double fNumber))
                {
                    return fNumber;
                }
            }

            return 5.6; // Default fallback
        }
        private void StopLightSensorMonitoring()
        {
            try
            {
                // Proper timer cleanup with cancellation
                if (_sensorTimer != null)
                {
                    _sensorTimer.Stop();
                    _sensorTimer.Elapsed -= OnSensorTimerElapsed;
                    _sensorTimer.Dispose();
                    _sensorTimer = null;
                }

                // Stop sensor service
                _lightSensorService?.StopListening();

                System.Diagnostics.Debug.WriteLine("Light sensor monitoring stopped");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error stopping light sensor monitoring: {ex.Message}");
            }
        }
       

        // Event handler for MEASURE button
        private async void OnMeasurePressed(object sender, EventArgs e)
        {
            try
            {
                if (_disposed) return; // Don't process if disposed

                if (!_isMonitoring)
                {
                    // START monitoring
                    _isMonitoring = true;

                    // Take immediate reading when button is pressed
                    if (_lightSensorService != null && _viewModel != null)
                    {
                        _viewModel.IsBusy = true;

                        // Perform immediate reading on background thread
                        await Task.Run(async () =>
                        {
                            try
                            {
                                float currentLux = _lightSensorService.GetCurrentLux();
                                var calculatedEV = PerformEVCalculation(currentLux);

                                await MainThread.InvokeOnMainThreadAsync(() =>
                                {
                                    if (!_disposed && _viewModel != null)
                                    {
                                        _viewModel.UpdateLightReading(currentLux);
                                        _viewModel.SetCalculatedEV(calculatedEV);

                                        System.Diagnostics.Debug.WriteLine($"Immediate reading: {currentLux} lux, Calculated EV: {calculatedEV}");
                                    }
                                });
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Error in immediate reading: {ex.Message}");
                            }
                        }).ConfigureAwait(false);

                        // Provide haptic feedback on UI thread
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            try
                            {
                                HapticFeedback.Perform(HapticFeedbackType.Click);
                            }
                            catch { } // Ignore haptic errors

                            _viewModel.IsBusy = false;
                        });
                    }

                    // Start continuous monitoring
                    StartLightSensorMonitoring();

                    // Update button text or state
                    if (sender is Button button)
                    {
                        button.Text = "STOP";
                        button.BackgroundColor = Color.FromRgb(180, 50, 50); // Red color for stop
                    }
                }
                else
                {
                    // STOP monitoring
                    _isMonitoring = false;
                    StopLightSensorMonitoring();

                    // Update button text or state
                    if (sender is Button button)
                    {
                        button.Text = "MEASURE";
                        button.BackgroundColor = Color.FromRgb(51, 51, 51); // Original dark color
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error toggling monitoring: {ex.Message}");

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (_viewModel != null)
                        _viewModel.IsBusy = false;
                });
            }
        }

        // Event handlers for sliders
        private void OnApertureChanged(object sender, ValueChangedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.SelectedApertureIndex = (int)Math.Round(e.NewValue);
                _viewModel.CalculateEV();
            }
        }
        private void ApertureSlider_ValueChanged(object sender, ValueChangedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.SelectedApertureIndex = (int)Math.Round(e.NewValue);
                _viewModel.CalculateEV();
            }
        }

        private void OnIsoChanged(object sender, ValueChangedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.SelectedIsoIndex = (int)Math.Round(e.NewValue);
                _viewModel.CalculateEV();
            }
        }

        private void OnShutterSpeedChanged(object sender, ValueChangedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.SelectedShutterSpeedIndex = (int)Math.Round(e.NewValue);
                _viewModel.CalculateEV();
            }
        }

        private void OnEvChanged(object sender, ValueChangedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.SelectedEV = e.NewValue;
                // EV changes might affect other calculations
                _viewModel.CalculateFromEV();
            }
        }

        // Event handler for step selection
        private async void OnStepChanged(object sender, CheckedChangedEventArgs e)
        {
            if (_viewModel != null && e.Value) // Only process when checked (not unchecked)
            {
                try
                {
                    if (sender == FullStepRadio)
                    {
                        _viewModel.CurrentStep = ExposureIncrements.Full;
                    }
                    else if (sender == HalfStepRadio)
                    {
                        _viewModel.CurrentStep = ExposureIncrements.Half;
                    }
                    else if (sender == ThirdStepRadio)
                    {
                        _viewModel.CurrentStep = ExposureIncrements.Third;
                    }

                    // Reload arrays with new step size
                    await _viewModel.LoadExposureArraysAsync();

                    // Recalculate EV with new step precision
                    _viewModel.CalculateEV();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error changing step size: {ex.Message}");
                }
            }
        }

        // Event handler for Close button
        private async void ImageButton_Pressed(object sender, EventArgs e)
        {
            try
            {
                // Stop sensor monitoring before closing
                StopLightSensorMonitoring();
                _isMonitoring = false;

                // Close the modal
                await Navigation.PopModalAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error closing light meter: {ex.Message}");
            }
        }

        private async void OnSystemError(object sender, OperationErrorEventArgs e)
        {
            var retry = await DisplayAlert("Error", $"{e.Message}. Try again?", "OK", "Cancel");
            if (retry && sender is LightMeterViewModel viewModel)
            {
                await viewModel.RetryLastCommandAsync();
            }
        }

        private void ApertureSlider_ValueChanged_1(object sender, ValueChangedEventArgs e)
        {

        }
    }
}
#endif