using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Services;
using Location.Photography.Application.Services;
using Location.Photography.ViewModels.Events;
using Location.Photography.ViewModels.Interfaces;
using Location.Core.Application.Tips.DTOs;
using Location.Core.Application.Tips.Queries.GetTipsByType;
using MediatR;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Location.Photography.ViewModels
{
    public class ExposureCalculatorViewModel : ViewModelBase, INavigationAware
    {
        #region Fields
        private readonly IMediator _mediator;
        private readonly IExposureCalculatorService _exposureCalculatorService;
        private readonly IErrorDisplayService _errorDisplayService;

        private string _shutterSpeedSelected;
        private string _fStopSelected;
        private string _isoSelected;
        private string _oldShutterSpeed;
        private string _oldFstop;
        private string _oldISO;
        private string _shutterSpeedResult;
        private string _fStopResult;
        private string _isoResult;
        private string[] _shutterSpeedsForPicker;
        private string[] _apeaturesForPicker;
        private string[] _isosForPicker;
        private ExposureIncrements _fullHalfThirds;
        private FixedValue _toCalculate;
        private double _evValue;
        private bool _showError;

        // Lock state properties
        private bool _isShutterLocked;
        private bool _isApertureLocked;
        private bool _isIsoLocked;

        // Preset properties
        private TipTypeDto _selectedPreset;
        private Lazy<ObservableCollection<TipTypeDto>> _availablePresets;

        // Performance optimization
        private DateTime _lastCalculationTime = DateTime.MinValue;
        private const int CalculationThrottleMs = 500; // Throttle calculations to max once per 500ms
        #endregion

        #region Events
        public new event EventHandler<OperationErrorEventArgs> ErrorOccurred;
        #endregion

        #region Properties
        public string ShutterSpeedSelected
        {
            get => _shutterSpeedSelected;
            set => SetProperty(ref _shutterSpeedSelected, value);
        }

        public string FStopSelected
        {
            get => _fStopSelected;
            set => SetProperty(ref _fStopSelected, value);
        }

        public string ISOSelected
        {
            get => _isoSelected;
            set => SetProperty(ref _isoSelected, value);
        }

        public string OldShutterSpeed
        {
            get => _oldShutterSpeed;
            set => SetProperty(ref _oldShutterSpeed, value);
        }

        public string OldFstop
        {
            get => _oldFstop;
            set => SetProperty(ref _oldFstop, value);
        }

        public string OldISO
        {
            get => _oldISO;
            set => SetProperty(ref _oldISO, value);
        }

        public string ShutterSpeedResult
        {
            get => _shutterSpeedResult;
            set => SetProperty(ref _shutterSpeedResult, value);
        }

        public string FStopResult
        {
            get => _fStopResult;
            set => SetProperty(ref _fStopResult, value);
        }

        public string ISOResult
        {
            get => _isoResult;
            set => SetProperty(ref _isoResult, value);
        }

        public string[] ShutterSpeedsForPicker
        {
            get => _shutterSpeedsForPicker;
            set => SetProperty(ref _shutterSpeedsForPicker, value);
        }

        public string[] ApeaturesForPicker
        {
            get => _apeaturesForPicker;
            set => SetProperty(ref _apeaturesForPicker, value);
        }

        public string[] ISOsForPicker
        {
            get => _isosForPicker;
            set => SetProperty(ref _isosForPicker, value);
        }

        public ExposureIncrements FullHalfThirds
        {
            get => _fullHalfThirds;
            set
            {
                if (SetProperty(ref _fullHalfThirds, value))
                {
                    // Update picker values when exposure increment changes
                    LoadPickerValuesAsync().ConfigureAwait(false);
                }
            }
        }

        public FixedValue ToCalculate
        {
            get => _toCalculate;
            set => SetProperty(ref _toCalculate, value);
        }

        public double EVValue
        {
            get => _evValue;
            set => SetProperty(ref _evValue, value);
        }

        public bool ShowError
        {
            get => _showError;
            set => SetProperty(ref _showError, value);
        }

        public FixedValue SkipCalculation
        {
            get => ToCalculate;
            set => ToCalculate = value;
        }

        // Lock state properties
        public bool IsShutterLocked
        {
            get => _isShutterLocked;
            set
            {
                if (SetProperty(ref _isShutterLocked, value))
                {
                    UpdateCalculationMode();
                }
            }
        }

        public bool IsApertureLocked
        {
            get => _isApertureLocked;
            set
            {
                if (SetProperty(ref _isApertureLocked, value))
                {
                    UpdateCalculationMode();
                }
            }
        }

        public bool IsIsoLocked
        {
            get => _isIsoLocked;
            set
            {
                if (SetProperty(ref _isIsoLocked, value))
                {
                    UpdateCalculationMode();
                }
            }
        }

        // Preset properties
        public TipTypeDto SelectedPreset
        {
            get => _selectedPreset;
            set
            {
                if (SetProperty(ref _selectedPreset, value) && value != null)
                {
                    ApplyPreset(value);
                }
            }
        }

        public ObservableCollection<TipTypeDto> AvailablePresets
        {
            get => _availablePresets.Value;
        }
        #endregion

        #region Commands
        public IRelayCommand CalculateCommand { get; }
        public IRelayCommand ResetCommand { get; }
        public IRelayCommand ToggleShutterLockCommand { get; }
        public IRelayCommand ToggleApertureLockCommand { get; }
        public IRelayCommand ToggleIsoLockCommand { get; }
        public IAsyncRelayCommand LoadPresetsCommand { get; }
        #endregion

        #region Constructors
        public ExposureCalculatorViewModel() : base(null, null)
        {
            // Design-time constructor
            _shutterSpeedsForPicker = ShutterSpeeds.Full;
            _apeaturesForPicker = Apetures.Full;
            _isosForPicker = ISOs.Full;
            _availablePresets = new Lazy<ObservableCollection<TipTypeDto>>(() => new ObservableCollection<TipTypeDto>());

            CalculateCommand = new RelayCommand(Calculate);
            ResetCommand = new RelayCommand(Reset);
            ToggleShutterLockCommand = new RelayCommand(ToggleShutterLock);
            ToggleApertureLockCommand = new RelayCommand(ToggleApertureLock);
            ToggleIsoLockCommand = new RelayCommand(ToggleIsoLock);
            LoadPresetsCommand = new AsyncRelayCommand(LoadPresetsAsync);
        }

        public ExposureCalculatorViewModel(IMediator mediator, IExposureCalculatorService exposureCalculatorService, IErrorDisplayService errorDisplayService)
            : base(null, errorDisplayService)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _exposureCalculatorService = exposureCalculatorService ?? throw new ArgumentNullException(nameof(exposureCalculatorService));
            _errorDisplayService = errorDisplayService ?? throw new ArgumentNullException(nameof(errorDisplayService));

            // Initialize commands
            CalculateCommand = new RelayCommand(Calculate);
            ResetCommand = new RelayCommand(Reset);
            ToggleShutterLockCommand = new RelayCommand(ToggleShutterLock);
            ToggleApertureLockCommand = new RelayCommand(ToggleApertureLock);
            ToggleIsoLockCommand = new RelayCommand(ToggleIsoLock);
            LoadPresetsCommand = new AsyncRelayCommand(LoadPresetsAsync);

            // Default values
            _fullHalfThirds = ExposureIncrements.Full;
            _toCalculate = FixedValue.ShutterSpeeds;
            _evValue = 0;
            _availablePresets = new Lazy<ObservableCollection<TipTypeDto>>(() => new ObservableCollection<TipTypeDto>());

            // Load initial picker values
            LoadPickerValuesAsync().ConfigureAwait(false);

            // Load presets on initialization
            LoadPresetsCommand.Execute(null);
        }
        #endregion

        #region Methods
        private async Task LoadPickerValuesAsync()
        {
            try
            {
                IsBusy = true;
                ShowError = false;

                string incrementString = GetIncrementString();

                // Use the shared utility classes to get the appropriate values
                ShutterSpeedsForPicker = ShutterSpeeds.GetScale(incrementString);
                ApeaturesForPicker = Apetures.GetScale(incrementString);
                ISOsForPicker = ISOs.GetScale(incrementString);
            }
            catch (Exception ex)
            {
                OnSystemError($"Error loading exposure values: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private string GetIncrementString()
        {
            return FullHalfThirds switch
            {
                ExposureIncrements.Full => "Full",
                ExposureIncrements.Half => "Halves",
                ExposureIncrements.Third => "Thirds",
                _ => "Full"
            };
        }

        public void Calculate()
        {
            // Throttle calculations to improve performance
            var now = DateTime.Now;
            if ((now - _lastCalculationTime).TotalMilliseconds < CalculationThrottleMs)
            {
                return; // Skip calculation if called too frequently
            }
            _lastCalculationTime = now;

            try
            {
                IsBusy = true;
                ClearErrors();

                // Validate inputs before calculation
                if (string.IsNullOrEmpty(OldShutterSpeed) || string.IsNullOrEmpty(OldFstop) || string.IsNullOrEmpty(OldISO))
                {
                    // Initialize with current values if old values are missing
                    StoreOldValues();
                    IsBusy = false;
                    return;
                }

                // Create base exposure triangle from old values
                var baseExposure = new ExposureTriangleDto
                {
                    ShutterSpeed = OldShutterSpeed,
                    Aperture = OldFstop,
                    Iso = OldISO
                };

                // Calculate based on what's NOT locked
                if (!IsShutterLocked)
                {
                    var shutterResult = _exposureCalculatorService.CalculateShutterSpeedAsync(
                       baseExposure, FStopSelected, ISOSelected, FullHalfThirds, default, EVValue)
                       .GetAwaiter().GetResult();

                    if (shutterResult.IsSuccess && shutterResult.Data != null)
                    {
                        ShutterSpeedResult = shutterResult.Data.ShutterSpeed;
                    }
                    else
                    {
                        ShutterSpeedResult = OldShutterSpeed;
                        if (!shutterResult.IsSuccess)
                        {
                            SetValidationError(shutterResult.ErrorMessage);
                            return;
                        }
                    }
                }
                else
                {
                    ShutterSpeedResult = ShutterSpeedSelected;
                }

                if (!IsApertureLocked)
                {
                    var apertureResult = _exposureCalculatorService.CalculateApertureAsync(
                       baseExposure, ShutterSpeedSelected, ISOSelected, FullHalfThirds, default, EVValue)
                       .GetAwaiter().GetResult();

                    if (apertureResult.IsSuccess && apertureResult.Data != null)
                    {
                        FStopResult = apertureResult.Data.Aperture;
                    }
                    else
                    {
                        FStopResult = OldFstop;
                        if (!apertureResult.IsSuccess)
                        {
                            SetValidationError(apertureResult.ErrorMessage);
                            return;
                        }
                    }
                }
                else
                {
                    FStopResult = FStopSelected;
                }

                if (!IsIsoLocked)
                {
                    var isoResult = _exposureCalculatorService.CalculateIsoAsync(
                                baseExposure, ShutterSpeedSelected, FStopSelected, FullHalfThirds, default, EVValue)
                                .GetAwaiter().GetResult();

                    if (isoResult.IsSuccess && isoResult.Data != null)
                    {
                        ISOResult = isoResult.Data.Iso;
                    }
                    else
                    {
                        ISOResult = OldISO;
                        if (!isoResult.IsSuccess)
                        {
                            SetValidationError(isoResult.ErrorMessage);
                            return;
                        }
                    }
                }
                else
                {
                    ISOResult = ISOSelected;
                }

                // Store the current values for the next calculation
                StoreOldValues();
            }
            catch (Exception ex)
            {
                OnSystemError($"Error calculating exposure: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void UpdateCalculationMode()
        {
            // Determine which parameter to calculate based on locks
            if (IsShutterLocked)
            {
                ToCalculate = FixedValue.ShutterSpeeds;
            }
            else if (IsApertureLocked)
            {
                ToCalculate = FixedValue.Aperture;
            }
            else if (IsIsoLocked)
            {
                ToCalculate = FixedValue.ISO;
            }
            else
            {
                // Default to calculating shutter speed if nothing is locked
                ToCalculate = FixedValue.ShutterSpeeds;
            }

            // Trigger recalculation
            Calculate();
        }

        private void StoreOldValues()
        {
            OldShutterSpeed = ShutterSpeedSelected;
            OldFstop = FStopSelected;
            OldISO = ISOSelected;
        }

        public void Reset()
        {
            try
            {
                // Reset to default values
                if (ShutterSpeedsForPicker?.Length > 0)
                    ShutterSpeedSelected = ShutterSpeedsForPicker[0];

                if (ApeaturesForPicker?.Length > 0)
                    FStopSelected = ApeaturesForPicker[0];

                if (ISOsForPicker?.Length > 0)
                    ISOSelected = ISOsForPicker[0];

                EVValue = 0;
                ToCalculate = FixedValue.ShutterSpeeds;
                FullHalfThirds = ExposureIncrements.Full;

                // Reset lock states
                IsShutterLocked = false;
                IsApertureLocked = false;
                IsIsoLocked = false;

                // Clear results
                ShutterSpeedResult = string.Empty;
                FStopResult = string.Empty;
                ISOResult = string.Empty;

                // Clear preset selection
                SelectedPreset = null;

                // Clear error message
                ShowError = false;
                ClearErrors();
            }
            catch (Exception ex)
            {
                OnSystemError($"Error resetting exposure calculator: {ex.Message}");
            }
        }

        #region Lock Methods
        private void ToggleShutterLock()
        {
            if (IsShutterLocked)
            {
                // Unlock shutter
                IsShutterLocked = false;
            }
            else
            {
                // Lock shutter, unlock others
                IsShutterLocked = true;
                IsApertureLocked = false;
                IsIsoLocked = false;
            }
        }

        private void ToggleApertureLock()
        {
            if (IsApertureLocked)
            {
                // Unlock aperture
                IsApertureLocked = false;
            }
            else
            {
                // Lock aperture, unlock others
                IsApertureLocked = true;
                IsShutterLocked = false;
                IsIsoLocked = false;
            }
        }

        private void ToggleIsoLock()
        {
            if (IsIsoLocked)
            {
                // Unlock ISO
                IsIsoLocked = false;
            }
            else
            {
                // Lock ISO, unlock others
                IsIsoLocked = true;
                IsShutterLocked = false;
                IsApertureLocked = false;
            }
        }
        #endregion

        #region Preset Methods
        private async Task LoadPresetsAsync()
        {
            try
            {
                IsBusy = true;
                ShowError = false;

                // Load all tip types for the preset dropdown
                var query = new Location.Core.Application.Queries.TipTypes.GetAllTipTypesQuery();
                var result = await _mediator.Send(query);

                if (result.IsSuccess && result.Data != null)
                {
                    AvailablePresets.Clear();

                    foreach (var tipType in result.Data)
                    {
                        AvailablePresets.Add(tipType);
                    }

                    // Notify that the collection has changed
                    OnPropertyChanged(nameof(AvailablePresets));
                }
                else
                {
                    OnSystemError(result.ErrorMessage ?? "Failed to load presets");
                }
            }
            catch (Exception ex)
            {
                OnSystemError($"Error loading presets: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async void ApplyPreset(TipTypeDto preset)
        {
            try
            {
                // Get a random tip from the selected tip type
                var randomTipQuery = new Location.Core.Application.Commands.Tips.GetRandomTipCommand { TipTypeId = preset.Id };
                var result = await _mediator.Send(randomTipQuery);

                if (result.IsSuccess && result.Data != null)
                {
                    var tip = result.Data;

                    // Apply preset values if they exist
                    if (!string.IsNullOrEmpty(tip.ShutterSpeed))
                    {
                        ShutterSpeedSelected = tip.ShutterSpeed;
                    }

                    if (!string.IsNullOrEmpty(tip.Fstop))
                    {
                        FStopSelected = tip.Fstop;
                    }

                    if (!string.IsNullOrEmpty(tip.Iso))
                    {
                        ISOSelected = tip.Iso;
                    }

                    // Store as old values and recalculate
                    StoreOldValues();
                    Calculate();
                }
                else
                {
                    OnSystemError("No tips found for the selected preset category");
                }
            }
            catch (Exception ex)
            {
                OnSystemError($"Error applying preset: {ex.Message}");
            }
        }
        #endregion

        protected override void OnErrorOccurred(string message)
        {
            ErrorOccurred?.Invoke(this, new OperationErrorEventArgs(OperationErrorSource.Unknown, message));
        }

        public void OnNavigatedToAsync()
        {
            LoadPickerValuesAsync().ConfigureAwait(false);
            LoadPresetsCommand.Execute(null);
        }

        public void OnNavigatedFromAsync()
        {
            // Implementation not required for this use case
        }
        #endregion
    }
}