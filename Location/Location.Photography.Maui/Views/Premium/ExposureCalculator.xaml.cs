// Location.Photography.Maui/Views/Premium/ExposureCalculator.xaml.cs
using Location.Core.Application.Services;
using Location.Photography.ViewModels;
using Location.Photography.Application.Services;
using Location.Photography.ViewModels.Events;
using Microsoft.Maui.Controls;
using MediatR;

namespace Location.Photography.Maui.Views.Premium
{
    public partial class ExposureCalculator : ContentPage, IDisposable
    {
        private readonly IAlertService _alertService;
        private readonly IErrorDisplayService _errorDisplayService;
        private ExposureCalculatorViewModel _viewModel;
        private bool _skipCalculations = true;
        private const double _fullStopEV = 1.0;
        private const double _halfStopEV = 0.5;
        private const double _thirdStopEV = 0.33;
        private double _currentEVStep = _fullStopEV;
        private IMediator _mediator;

        // PERFORMANCE: UI state management
        private readonly SemaphoreSlim _uiUpdateLock = new(1, 1);
        private bool _isInitialized = false;
        private bool _isDisposed = false;
        private CancellationTokenSource _uiCancellationSource = new();

        // PERFORMANCE: Prevent redundant operations
        private DateTime _lastPickerUpdate = DateTime.MinValue;
        private const int PICKER_UPDATE_THROTTLE_MS = 100;
        private readonly Dictionary<string, DateTime> _lastValueChanges = new();

        public ExposureCalculator()
        {
            InitializeComponent();
            _viewModel = new ExposureCalculatorViewModel();
            _viewModel.CalculateCommand.Execute(_viewModel);
            BindingContext = _viewModel;
            CloseButton.IsVisible = false;
            _ = InitializeUIAsync();
        }

        public ExposureCalculator(
            IExposureCalculatorService exposureCalculatorService,
            IAlertService alertService,
            IErrorDisplayService errorDisplayService,
            IMediator mediator)
        {
            _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
            _errorDisplayService = errorDisplayService ?? throw new ArgumentNullException(nameof(errorDisplayService));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

            InitializeComponent();
            CloseButton.IsVisible = false;
            _ = InitializeUIAsync(exposureCalculatorService);
        }

        public ExposureCalculator(
            IExposureCalculatorService exposureCalculatorService,
            IAlertService alertService,
            IErrorDisplayService errorDisplayService,
            int tipID,
            bool isFromTips = false)
        {
            _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
            _errorDisplayService = errorDisplayService ?? throw new ArgumentNullException(nameof(errorDisplayService));

            InitializeComponent();
            CloseButton.IsVisible = isFromTips;
            _ = InitializeUIAsync(exposureCalculatorService, tipID);
        }

        #region PERFORMANCE OPTIMIZED INITIALIZATION

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Async UI initialization with proper loading states
        /// </summary>
        private async Task InitializeUIAsync(IExposureCalculatorService exposureCalculatorService = null, int? tipID = null)
        {
            try
            {
                if (_isDisposed) return;

                // Show loading state immediately
                await ShowLoadingStateAsync("Initializing exposure calculator...");

                // Initialize ViewModel on background thread
                await Task.Run(async () =>
                {
                    try
                    {
                        await InitializeViewModelAsync(exposureCalculatorService, tipID);
                    }
                    catch (Exception ex)
                    {
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            HandleErrorSafely(ex, "Error initializing view model");
                        });
                        return;
                    }
                }, _uiCancellationSource.Token);

                // Setup UI components on main thread
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    try
                    {
                        if (_isDisposed) return;

                        SetupUIComponents();
                        SubscribeToViewModelEvents();
                        _isInitialized = true;
                    }
                    catch (Exception ex)
                    {
                        HandleErrorSafely(ex, "Error setting up UI components");
                    }
                    finally
                    {
                        HideLoadingState();
                    }
                });
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    HandleErrorSafely(ex, "Critical error during initialization");
                    HideLoadingState();
                });
            }
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Background ViewModel initialization
        /// </summary>
        private async Task InitializeViewModelAsync(IExposureCalculatorService exposureCalculatorService, int? tipID)
        {
            try
            {
                if (tipID.HasValue)
                {
                    _viewModel = new ExposureCalculatorViewModel(null, exposureCalculatorService, _errorDisplayService);
                }
                else if (exposureCalculatorService != null)
                {
                    _viewModel = new ExposureCalculatorViewModel(_mediator, exposureCalculatorService, _errorDisplayService);
                }
                else
                {
                    _viewModel = new ExposureCalculatorViewModel();
                }

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    BindingContext = _viewModel;
                });

                // Load initial data
                await _viewModel.LoadPresetsCommand.ExecuteAsync(null);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"ViewModel initialization failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: UI component setup with loading states
        /// </summary>
        private void SetupUIComponents()
        {
            try
            {
                _skipCalculations = true;

                // Setup lock buttons with loading indicators
                SetupLockButtons();

                // Setup EV slider
                SetupEVSlider();

                // Initialize picker values with loading states
                InitializePickersWithLoadingStates();

                // Setup event handlers
                SetupEventHandlers();

                _skipCalculations = false;

                // Perform initial calculation
                _viewModel?.CalculateCommand.Execute(_viewModel);
            }
            catch (Exception ex)
            {
                HandleErrorSafely(ex, "Error setting up UI components");
            }
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Setup lock buttons with proper visual states
        /// </summary>
        private void SetupLockButtons()
        {
            try
            {
                ShutterLockButton.ImageSource = "lockopen.png";
                IsoLockButton.ImageSource = "lockopen.png";
                ApertureLockButton.ImageSource = "lockopen.png";

                ShutterLockButton.Pressed += (s, e) => HandleLockButtonClick("shutter");
                IsoLockButton.Pressed += (s, e) => HandleLockButtonClick("iso");
                ApertureLockButton.Pressed += (s, e) => HandleLockButtonClick("aperture");
            }
            catch (Exception ex)
            {
                HandleErrorSafely(ex, "Error setting up lock buttons");
            }
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Setup event handlers with throttling
        /// </summary>
        private void SetupEventHandlers()
        {
            try
            {
                // Throttled picker events
                ShutterSpeed_Picker.SelectedIndexChanged += (s, e) => HandlePickerChangeThrottled("shutter", () =>
                {
                    if (_skipCalculations || _viewModel == null) return;
                    _viewModel.ShutterSpeedSelected = ShutterSpeed_Picker.SelectedItem?.ToString();
                    _viewModel?.CalculateCommand.Execute(_viewModel);
                });

                fstop_Picker.SelectedIndexChanged += (s, e) => HandlePickerChangeThrottled("aperture", () =>
                {
                    if (_skipCalculations || _viewModel == null) return;
                    _viewModel.FStopSelected = fstop_Picker.SelectedItem?.ToString();
                    _viewModel?.CalculateCommand.Execute(_viewModel);
                });

                ISO_Picker.SelectedIndexChanged += (s, e) => HandlePickerChangeThrottled("iso", () =>
                {
                    if (_skipCalculations || _viewModel == null) return;
                    _viewModel.ISOSelected = ISO_Picker.SelectedItem?.ToString();
                    _viewModel?.CalculateCommand.Execute(_viewModel);
                });

                // Target picker events
                SetupTargetPickerEvents();

                // Other UI events
                EvSlider.ValueChanged += EvSlider_ValueChanged;
                exposurefull.CheckedChanged += exposuresteps_CheckedChanged;
                exposurehalfstop.CheckedChanged += exposuresteps_CheckedChanged;
                exposurethirdstop.CheckedChanged += exposuresteps_CheckedChanged;
                PresetPicker.SelectedIndexChanged += PresetPicker_SelectedIndexChanged;
                CloseButton.Pressed += CloseButton_Pressed;
            }
            catch (Exception ex)
            {
                HandleErrorSafely(ex, "Error setting up event handlers");
            }
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Initialize pickers with loading states
        /// </summary>
        private void InitializePickersWithLoadingStates()
        {
            try
            {
                // Show loading for pickers
                SetPickerLoadingState(ShutterSpeed_Picker, true);
                SetPickerLoadingState(fstop_Picker, true);
                SetPickerLoadingState(ISO_Picker, true);

                // Initialize picker selections
                if (_viewModel?.ShutterSpeedsForPicker?.Length > 0)
                {
                    ShutterSpeed_Picker.SelectedIndex = 0;
                    SetPickerLoadingState(ShutterSpeed_Picker, false);
                }

                if (_viewModel?.ApeaturesForPicker?.Length > 0)
                {
                    fstop_Picker.SelectedIndex = 0;
                    SetPickerLoadingState(fstop_Picker, false);
                }

                if (_viewModel?.ISOsForPicker?.Length > 0)
                {
                    ISO_Picker.SelectedIndex = 0;
                    SetPickerLoadingState(ISO_Picker, false);
                }

                // Initialize target pickers
                SyncTargetPickers();
            }
            catch (Exception ex)
            {
                HandleErrorSafely(ex, "Error initializing pickers");
            }
        }

        #endregion

        #region PERFORMANCE OPTIMIZED EVENT HANDLING

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Throttled picker change handling
        /// </summary>
        private void HandlePickerChangeThrottled(string pickerType, Action updateAction)
        {
            try
            {
                var now = DateTime.Now;
                var key = $"picker_{pickerType}";

                if (_lastValueChanges.ContainsKey(key))
                {
                    var timeSinceLastChange = (now - _lastValueChanges[key]).TotalMilliseconds;
                    if (timeSinceLastChange < PICKER_UPDATE_THROTTLE_MS)
                    {
                        return; // Skip this update
                    }
                }

                _lastValueChanges[key] = now;
                updateAction?.Invoke();
            }
            catch (Exception ex)
            {
                HandleErrorSafely(ex, $"Error handling {pickerType} picker change");
            }
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Lock button handling with visual feedback
        /// </summary>
        private void HandleLockButtonClick(string lockType)
        {
            try
            {
                if (_viewModel == null) return;

                // Provide immediate visual feedback
                ProvideLockButtonFeedback(lockType);

                // Update lock states
                switch (lockType)
                {
                    case "shutter":
                        UpdateShutterLock();
                        break;
                    case "iso":
                        UpdateIsoLock();
                        break;
                    case "aperture":
                        UpdateApertureLock();
                        break;
                }

                // Update visual states
                UpdateLockButtonVisualStates();
            }
            catch (Exception ex)
            {
                HandleErrorSafely(ex, $"Error handling {lockType} lock button");
            }
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Immediate visual feedback for lock buttons
        /// </summary>
        private void ProvideLockButtonFeedback(string lockType)
        {
            try
            {
                // Provide haptic feedback if available
                Task.Run(() =>
                {
                    try
                    {
                        HapticFeedback.Perform(HapticFeedbackType.Click);
                    }
                    catch { } // Ignore haptic errors
                });

                // Visual feedback - briefly change button opacity
                var button = lockType switch
                {
                    "shutter" => ShutterLockButton,
                    "iso" => IsoLockButton,
                    "aperture" => ApertureLockButton,
                    _ => null
                };

                if (button != null)
                {
                    button.Opacity = 0.5;
                    Task.Delay(100).ContinueWith(_ =>
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            if (!_isDisposed)
                                button.Opacity = 1.0;
                        });
                    });
                }
            }
            catch (Exception ex)
            {
                // Log but don't throw for visual feedback errors
                System.Diagnostics.Debug.WriteLine($"Error providing lock button feedback: {ex.Message}");
            }
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Update lock states with proper validation
        /// </summary>
        private void UpdateShutterLock()
        {
            if (_viewModel == null) return;

            ShutterSpeed_Picker.IsEnabled = !ShutterSpeed_Picker.IsEnabled;
            ShutterLockButton.ImageSource = ShutterSpeed_Picker.IsEnabled ? "lockopen.png" : "lock.png";

            if (!ShutterSpeed_Picker.IsEnabled)
            {
                // Lock shutter, unlock others
                IsoLockButton.ImageSource = "lockopen.png";
                ISO_Picker.IsEnabled = true;
                ApertureLockButton.ImageSource = "lockopen.png";
                fstop_Picker.IsEnabled = true;

                _viewModel.IsShutterLocked = true;
                _viewModel.IsIsoLocked = false;
                _viewModel.IsApertureLocked = false;
            }
            else
            {
                _viewModel.IsShutterLocked = false;
            }
        }

        private void UpdateIsoLock()
        {
            if (_viewModel == null) return;

            ISO_Picker.IsEnabled = !ISO_Picker.IsEnabled;
            IsoLockButton.ImageSource = ISO_Picker.IsEnabled ? "lockopen.png" : "lock.png";

            if (!ISO_Picker.IsEnabled)
            {
                // Lock ISO, unlock others
                ShutterLockButton.ImageSource = "lockopen.png";
                ShutterSpeed_Picker.IsEnabled = true;
                ApertureLockButton.ImageSource = "lockopen.png";
                fstop_Picker.IsEnabled = true;

                _viewModel.IsIsoLocked = true;
                _viewModel.IsShutterLocked = false;
                _viewModel.IsApertureLocked = false;
            }
            else
            {
                _viewModel.IsIsoLocked = false;
            }
        }

        private void UpdateApertureLock()
        {
            if (_viewModel == null) return;

            fstop_Picker.IsEnabled = !fstop_Picker.IsEnabled;
            ApertureLockButton.ImageSource = fstop_Picker.IsEnabled ? "lockopen.png" : "lock.png";

            if (!fstop_Picker.IsEnabled)
            {
                // Lock aperture, unlock others
                ShutterLockButton.ImageSource = "lockopen.png";
                ShutterSpeed_Picker.IsEnabled = true;
                IsoLockButton.ImageSource = "lockopen.png";
                ISO_Picker.IsEnabled = true;

                _viewModel.IsApertureLocked = true;
                _viewModel.IsShutterLocked = false;
                _viewModel.IsIsoLocked = false;
            }
            else
            {
                _viewModel.IsApertureLocked = false;
            }
        }

        #endregion

        #region PERFORMANCE OPTIMIZED UI STATE MANAGEMENT

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Subscribe to ViewModel events with proper error handling
        /// </summary>
        private void SubscribeToViewModelEvents()
        {
            if (_viewModel == null) return;

            try
            {
                _viewModel.ErrorOccurred -= OnSystemError;
                _viewModel.ErrorOccurred += OnSystemError;
                _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            }
            catch (Exception ex)
            {
                HandleErrorSafely(ex, "Error subscribing to ViewModel events");
            }
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Show loading state with proper UI updates
        /// </summary>
        private async Task ShowLoadingStateAsync(string message)
        {
            try
            {
                if (_isDisposed) return;

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (_viewModel != null)
                    {
                        _viewModel.IsBusy = true;
                    }

                    // Show loading overlay or indicator
                    // You could add a loading overlay here if needed
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing loading state: {ex.Message}");
            }
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Hide loading state safely
        /// </summary>
        private void HideLoadingState()
        {
            try
            {
                if (_isDisposed || _viewModel == null) return;

                _viewModel.IsBusy = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error hiding loading state: {ex.Message}");
            }
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Set picker loading state with visual feedback
        /// </summary>
        private void SetPickerLoadingState(Picker picker, bool isLoading)
        {
            try
            {
                if (_isDisposed || picker == null) return;

                picker.IsEnabled = !isLoading;
                picker.Opacity = isLoading ? 0.5 : 1.0;

                // You could add a loading spinner next to the picker here
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting picker loading state: {ex.Message}");
            }
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Update lock button visual states
        /// </summary>
        private void UpdateLockButtonVisualStates()
        {
            try
            {
                if (_isDisposed || _viewModel == null) return;

                // Update button text/images based on lock states
                ShutterLockButton.ImageSource = _viewModel.IsShutterLocked ? "lock.png" : "lockopen.png";
                IsoLockButton.ImageSource = _viewModel.IsIsoLocked ? "lock.png" : "lockopen.png";
                ApertureLockButton.ImageSource = _viewModel.IsApertureLocked ? "lock.png" : "lockopen.png";

                // Update picker states
                ShutterSpeed_Picker.IsEnabled = !_viewModel.IsShutterLocked;
                ISO_Picker.IsEnabled = !_viewModel.IsIsoLocked;
                fstop_Picker.IsEnabled = !_viewModel.IsApertureLocked;
            }
            catch (Exception ex)
            {
                HandleErrorSafely(ex, "Error updating lock button visual states");
            }
        }

        #endregion

        #region EXISTING EVENT HANDLERS (OPTIMIZED)

        private void exposuresteps_CheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            if (_skipCalculations || !e.Value || _viewModel == null || _isDisposed)
                return;

            try
            {
                RadioButton radioButton = (RadioButton)sender;

                if (radioButton == exposurefull)
                    _viewModel.FullHalfThirds = Application.Services.ExposureIncrements.Full;
                else if (radioButton == exposurehalfstop)
                    _viewModel.FullHalfThirds = Application.Services.ExposureIncrements.Half;
                else if (radioButton == exposurethirdstop)
                    _viewModel.FullHalfThirds = Application.Services.ExposureIncrements.Third;

                SetupEVSlider();
                PopulateViewModel();
                SyncTargetPickers();
                _viewModel?.CalculateCommand.Execute(_viewModel);
            }
            catch (Exception ex)
            {
                HandleErrorSafely(ex, "Error changing exposure steps");
            }
        }

        private void PresetPicker_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_skipCalculations || _viewModel == null || _isDisposed)
                return;

            try
            {
                // Preset application is handled in the ViewModel through binding
                SyncTargetPickers();
            }
            catch (Exception ex)
            {
                HandleErrorSafely(ex, "Error selecting preset");
            }
        }

        private void SetupTargetPickerEvents()
        {
            try
            {
                TargetShutterSpeed_Picker.SelectedIndexChanged += (s, e) => HandlePickerChangeThrottled("target_shutter", () =>
                {
                    if (_skipCalculations || _viewModel == null) return;
                    string selectedValue = TargetShutterSpeed_Picker.SelectedItem?.ToString();
                    if (!string.IsNullOrEmpty(selectedValue))
                    {
                        _viewModel.ShutterSpeedSelected = selectedValue;
                        ShutterSpeed_Picker.SelectedItem = selectedValue;
                        _viewModel?.CalculateCommand.Execute(_viewModel);
                    }
                });

                TargetFstop_Picker.SelectedIndexChanged += (s, e) => HandlePickerChangeThrottled("target_aperture", () =>
                {
                    if (_skipCalculations || _viewModel == null) return;
                    string selectedValue = TargetFstop_Picker.SelectedItem?.ToString();
                    if (!string.IsNullOrEmpty(selectedValue))
                    {
                        _viewModel.FStopSelected = selectedValue;
                        fstop_Picker.SelectedItem = selectedValue;
                        _viewModel?.CalculateCommand.Execute(_viewModel);
                    }
                });

                TargetISO_Picker.SelectedIndexChanged += (s, e) => HandlePickerChangeThrottled("target_iso", () =>
                {
                    if (_skipCalculations || _viewModel == null) return;
                    string selectedValue = TargetISO_Picker.SelectedItem?.ToString();
                    if (!string.IsNullOrEmpty(selectedValue))
                    {
                        _viewModel.ISOSelected = selectedValue;
                        ISO_Picker.SelectedItem = selectedValue;
                        _viewModel?.CalculateCommand.Execute(_viewModel);
                    }
                });
            }
            catch (Exception ex)
            {
                HandleErrorSafely(ex, "Error setting up target picker events");
            }
        }

        private void EvSlider_ValueChanged(object sender, ValueChangedEventArgs e)
        {
            if (_skipCalculations || _viewModel == null || _isDisposed)
                return;

            try
            {
                double newValue = Math.Round(e.NewValue / _currentEVStep) * _currentEVStep;

                if (Math.Abs(newValue - _viewModel.EVValue) >= _currentEVStep * 0.5)
                {
                    newValue = Math.Round(newValue, 2);

                    if (Math.Abs(EvSlider.Value - newValue) > 0.001)
                    {
                        EvSlider.Value = newValue;
                    }

                    _viewModel.EVValue = newValue;
                    _viewModel?.CalculateCommand.Execute(_viewModel);
                }
            }
            catch (Exception ex)
            {
                HandleErrorSafely(ex, "Error changing EV value");
            }
        }

        private void CloseButton_Pressed(object sender, EventArgs e)
        {
            try
            {
                Navigation.PopModalAsync();
            }
            catch (Exception ex)
            {
                HandleErrorSafely(ex, "Error closing page");
            }
        }

        #endregion

        #region HELPER METHODS (OPTIMIZED)

        private void SetupEVSlider()
        {
            if (_skipCalculations || _viewModel == null || _isDisposed)
                return;

            try
            {
                switch (_viewModel.FullHalfThirds)
                {
                    case Application.Services.ExposureIncrements.Full:
                        _currentEVStep = _fullStopEV;
                        break;
                    case Application.Services.ExposureIncrements.Half:
                        _currentEVStep = _halfStopEV;
                        break;
                    case Application.Services.ExposureIncrements.Third:
                        _currentEVStep = _thirdStopEV;
                        break;
                    default:
                        _currentEVStep = _fullStopEV;
                        break;
                }

                double currentValue = EvSlider.Value;
                double roundedValue = Math.Round(currentValue / _currentEVStep) * _currentEVStep;

                if (Math.Abs(roundedValue - currentValue) > 0.001)
                {
                    EvSlider.Value = roundedValue;
                }

                _viewModel.EVValue = roundedValue;
            }
            catch (Exception ex)
            {
                HandleErrorSafely(ex, "Error setting up EV slider");
            }
        }

        private void SyncTargetPickers()
        {
            if (_viewModel == null || _isDisposed) return;

            try
            {
                // Set target pickers to match base exposure values
                if (TargetShutterSpeed_Picker.ItemsSource != null && !string.IsNullOrEmpty(_viewModel.ShutterSpeedSelected))
                {
                    TargetShutterSpeed_Picker.SelectedItem = _viewModel.ShutterSpeedSelected;
                }

                if (TargetFstop_Picker.ItemsSource != null && !string.IsNullOrEmpty(_viewModel.FStopSelected))
                {
                    TargetFstop_Picker.SelectedItem = _viewModel.FStopSelected;
                }

                if (TargetISO_Picker.ItemsSource != null && !string.IsNullOrEmpty(_viewModel.ISOSelected))
                {
                    TargetISO_Picker.SelectedItem = _viewModel.ISOSelected;
                }
            }
            catch (Exception ex)
            {
                HandleErrorSafely(ex, "Error syncing target pickers");
            }
        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (_isDisposed) return;

            try
            {
                // Update lock button text when lock states change
                switch (e.PropertyName)
                {
                    case nameof(ExposureCalculatorViewModel.IsShutterLocked):
                    case nameof(ExposureCalculatorViewModel.IsApertureLocked):
                    case nameof(ExposureCalculatorViewModel.IsIsoLocked):
                        UpdateLockButtonVisualStates();
                        break;
                }
            }
            catch (Exception ex)
            {
                HandleErrorSafely(ex, "Error handling property change");
            }
        }

        private void PopulateViewModel()
        {
            if (_viewModel == null || _isDisposed) return;

            try
            {
                string oldShutter = _viewModel.ShutterSpeedSelected;
                string oldFStop = _viewModel.FStopSelected;
                string oldISO = _viewModel.ISOSelected;

                if (ShutterSpeed_Picker.SelectedItem != null)
                    _viewModel.ShutterSpeedSelected = ShutterSpeed_Picker.SelectedItem.ToString();

                if (fstop_Picker.SelectedItem != null)
                    _viewModel.FStopSelected = fstop_Picker.SelectedItem.ToString();

                if (ISO_Picker.SelectedItem != null)
                    _viewModel.ISOSelected = ISO_Picker.SelectedItem.ToString();

                _viewModel.OldShutterSpeed = oldShutter;
                _viewModel.OldFstop = oldFStop;
                _viewModel.OldISO = oldISO;

                SyncTargetPickers();
            }
            catch (Exception ex)
            {
                HandleErrorSafely(ex, "Error populating view model");
            }
        }

        #endregion

        #region LIFECYCLE EVENTS (OPTIMIZED)

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            try
            {
                if (_viewModel != null && !_isDisposed)
                {
                    _viewModel.ErrorOccurred -= OnSystemError;
                    _viewModel.ErrorOccurred += OnSystemError;

                    // Load presets when page appears
                    if (_isInitialized)
                    {
                        await _viewModel.LoadPresetsCommand.ExecuteAsync(null);
                    }
                }
            }
            catch (Exception ex)
            {
                HandleErrorSafely(ex, "Error during page appearing");
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            try
            {
                if (_viewModel != null)
                {
                    _viewModel.ErrorOccurred -= OnSystemError;
                    _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during page disappearing: {ex.Message}");
            }
        }

        protected void OnDestroy()
        {
            try
            {
                _isDisposed = true;
                _uiCancellationSource?.Cancel();
                _uiCancellationSource?.Dispose();
                _uiUpdateLock?.Dispose();
                _lastValueChanges?.Clear();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during page destruction: {ex.Message}");
            }
            finally
            {
                
            }
        }

        #endregion

        #region ERROR HANDLING (OPTIMIZED)

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Safe error handling that won't crash UI
        /// </summary>
        private void HandleErrorSafely(Exception ex, string context)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Error: {context}. {ex.Message}");

                if (_isDisposed) return;

                MainThread.BeginInvokeOnMainThread(async () =>
                {
                try
                {
                    if (_alertService != null)
                    {
                        await _alertService.ShowErrorAlertAsync(context, "Error");
                    }
                        else
                        {
                            await DisplayAlert("Error", context, "OK");
                        }
                    }
                    catch
                    {
                        // Last resort - don't let error handling crash the app
                        System.Diagnostics.Debug.WriteLine($"Critical: Failed to show error dialog for: {context}");
                    }

                    if (_viewModel != null)
                    {
                        _viewModel.ShowError = true;
                        _viewModel.ErrorMessage = $"{context}: {ex.Message}";
                        _viewModel.IsBusy = false;
                    }
                });
            }
            catch
            {
                // Absolutely cannot let error handling itself crash
                System.Diagnostics.Debug.WriteLine($"Critical error in error handler: {context}");
            }
        }

        private async void OnSystemError(object sender, OperationErrorEventArgs e)
        {
            try
            {
                if (_isDisposed) return;

                var retry = await DisplayAlert("Error", $"{e.Message}. Try again?", "OK", "Cancel");
                if (retry && sender is ExposureCalculatorViewModel viewModel)
                {
                    await viewModel.RetryLastCommandAsync();
                }
            }
            catch (Exception ex)
            {
                HandleErrorSafely(ex, "Error in system error handler");
            }
        }

        #endregion

        #region UNUSED EVENTS (KEPT FOR COMPATIBILITY)

        private void ShutterLockButton_Pressed(object sender, EventArgs e)
        {
            // This method is kept for compatibility but functionality moved to HandleLockButtonClick
        }

        public void Dispose()
        {
            OnDestroy();
            OnDisappearing(); // Ensure cleanup on dispose
        }



        #endregion
    }
}