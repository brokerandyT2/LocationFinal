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
using ShutterSpeeds = Location.Photography.ViewModels.Interfaces.ShutterSpeeds;
using Apetures = Location.Photography.ViewModels.Interfaces.Apetures;
using ISOs = Location.Photography.ViewModels.Interfaces.ISOs;

namespace Location.Photography.ViewModels
{
    public class ExposureCalculatorViewModel : ViewModelBase
    {
        #region Fields
        private readonly IMediator _mediator;
        private readonly IExposureCalculatorService _exposureCalculatorService;

        // PERFORMANCE: Threading and caching
        private readonly SemaphoreSlim _calculationLock = new(1, 1);
        private DateTime _lastCalculationTime = DateTime.MinValue;
        private const int CalculationThrottleMs = 200; // Reduced from 500ms for better responsiveness
        private readonly Dictionary<string, string> _pickerValuesCache = new();

        // Core properties
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
        #endregion

        #region Events
        public new event EventHandler<OperationErrorEventArgs> ErrorOccurred;
        #endregion

        #region Properties
        public string ShutterSpeedSelected
        {
            get => _shutterSpeedSelected;
            set
            {
                if (SetProperty(ref _shutterSpeedSelected, value))
                {
                    _ = CalculateOptimizedAsync();
                }
            }
        }

        public string FStopSelected
        {
            get => _fStopSelected;
            set
            {
                if (SetProperty(ref _fStopSelected, value))
                {
                    _ = CalculateOptimizedAsync();
                }
            }
        }

        public string ISOSelected
        {
            get => _isoSelected;
            set
            {
                if (SetProperty(ref _isoSelected, value))
                {
                    _ = CalculateOptimizedAsync();
                }
            }
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
                    _ = LoadPickerValuesOptimizedAsync();
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
            set
            {
                if (SetProperty(ref _evValue, value))
                {
                    _ = CalculateOptimizedAsync();
                }
            }
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
                    UpdateCalculationModeOptimized();
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
                    UpdateCalculationModeOptimized();
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
                    UpdateCalculationModeOptimized();
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
                    _ = ApplyPresetOptimizedAsync(value);
                }
            }
        }

        public ObservableCollection<TipTypeDto> AvailablePresets
        {
            get => _availablePresets.Value;
        }
        #endregion

        #region Commands
        public IRelayCommand CalculateCommand { get; private set; }
        public IRelayCommand ResetCommand { get; private set; }
        public IRelayCommand ToggleShutterLockCommand { get; private set; }
        public IRelayCommand ToggleApertureLockCommand { get; private set; }
        public IRelayCommand ToggleIsoLockCommand { get; private set; }
        public IAsyncRelayCommand LoadPresetsCommand { get; private set; }
        #endregion

        #region Constructors
        public ExposureCalculatorViewModel() : base(null, null)
        {
            // Design-time constructor
            _shutterSpeedsForPicker = ShutterSpeeds.Full;
            _apeaturesForPicker = Apetures.Full;
            _isosForPicker = ISOs.Full;
            _availablePresets = new Lazy<ObservableCollection<TipTypeDto>>(() => new ObservableCollection<TipTypeDto>());

            InitializeCommands();
            InitializeDefaults();
        }

        public ExposureCalculatorViewModel(IMediator mediator, IExposureCalculatorService exposureCalculatorService, IErrorDisplayService errorDisplayService)
            : base(null, errorDisplayService)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _exposureCalculatorService = exposureCalculatorService ?? throw new ArgumentNullException(nameof(exposureCalculatorService));

            InitializeCommands();
            InitializeDefaults();

            // Load initial data
            _ = LoadPickerValuesOptimizedAsync();
            LoadPresetsCommand.Execute(null);
        }

        private void InitializeCommands()
        {
            CalculateCommand = new RelayCommand(() => _ = CalculateOptimizedAsync());
            ResetCommand = new RelayCommand(ResetOptimized);
            ToggleShutterLockCommand = new RelayCommand(ToggleShutterLockOptimized);
            ToggleApertureLockCommand = new RelayCommand(ToggleApertureLockOptimized);
            ToggleIsoLockCommand = new RelayCommand(ToggleIsoLockOptimized);
            LoadPresetsCommand = new AsyncRelayCommand(LoadPresetsOptimizedAsync);
        }

        private void InitializeDefaults()
        {
            _fullHalfThirds = ExposureIncrements.Full;
            _toCalculate = FixedValue.ShutterSpeeds;
            _evValue = 0;
            _availablePresets = new Lazy<ObservableCollection<TipTypeDto>>(() => new ObservableCollection<TipTypeDto>());
        }
        #endregion

        #region PERFORMANCE OPTIMIZED METHODS

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Throttled and optimized calculation
        /// </summary>
        private async Task CalculateOptimizedAsync()
        {
            // Throttle rapid updates
            var now = DateTime.Now;
            if ((now - _lastCalculationTime).TotalMilliseconds < CalculationThrottleMs)
            {
                return;
            }
            _lastCalculationTime = now;

            if (!await _calculationLock.WaitAsync(50))
            {
                return; // Skip if another calculation is in progress
            }

            try
            {
                await Task.Run(() => CalculateCore());
            }
            finally
            {
                _calculationLock.Release();
            }
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Core calculation logic moved to background thread
        /// </summary>
        private async Task CalculateCore()
        {
            try
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    IsBusy = true;
                    ClearErrors();
                });

                // Validate inputs before calculation
                if (string.IsNullOrEmpty(OldShutterSpeed) || string.IsNullOrEmpty(OldFstop) || string.IsNullOrEmpty(OldISO))
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        StoreOldValuesOptimized();
                        IsBusy = false;
                    });
                    return;
                }

                // Perform calculations on background thread
                var calculationResults = await PerformCalculationsAsync();

                // Update UI on main thread with batch updates
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    try
                    {
                        BeginPropertyChangeBatch();

                        ShutterSpeedResult = calculationResults.ShutterSpeed;
                        FStopResult = calculationResults.Aperture;
                        ISOResult = calculationResults.ISO;

                        StoreOldValuesOptimized();

                        _ = EndPropertyChangeBatchAsync();
                    }
                    catch (Exception ex)
                    {
                        OnSystemError($"Error updating calculation results: {ex.Message}");
                        _ = EndPropertyChangeBatchAsync();
                    }
                    finally
                    {
                        IsBusy = false;
                    }
                });
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    OnSystemError($"Error calculating exposure: {ex.Message}");
                    IsBusy = false;
                });
            }
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Background calculation execution
        /// </summary>
        private async Task<ExposureCalculationResult> PerformCalculationsAsync()
        {
            var result = new ExposureCalculationResult
            {
                ShutterSpeed = ShutterSpeedSelected,
                Aperture = FStopSelected,
                ISO = ISOSelected
            };

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
                var shutterResult = await _exposureCalculatorService.CalculateShutterSpeedAsync(
                   baseExposure, FStopSelected, ISOSelected, FullHalfThirds, default, EVValue);

                if (shutterResult.IsSuccess && shutterResult.Data != null)
                {
                    result.ShutterSpeed = shutterResult.Data.ShutterSpeed;
                }
                else
                {
                    result.ShutterSpeed = OldShutterSpeed;
                    if (!shutterResult.IsSuccess)
                    {
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            SetValidationError(shutterResult.ErrorMessage);
                        });
                        return result;
                    }
                }
            }

            if (!IsApertureLocked)
            {
                var apertureResult = await _exposureCalculatorService.CalculateApertureAsync(
                   baseExposure, ShutterSpeedSelected, ISOSelected, FullHalfThirds, default, EVValue);

                if (apertureResult.IsSuccess && apertureResult.Data != null)
                {
                    result.Aperture = apertureResult.Data.Aperture;
                }
                else
                {
                    result.Aperture = OldFstop;
                    if (!apertureResult.IsSuccess)
                    {
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            SetValidationError(apertureResult.ErrorMessage);
                        });
                        return result;
                    }
                }
            }

            if (!IsIsoLocked)
            {
                var isoResult = await _exposureCalculatorService.CalculateIsoAsync(
                            baseExposure, ShutterSpeedSelected, FStopSelected, FullHalfThirds, default, EVValue);

                if (isoResult.IsSuccess && isoResult.Data != null)
                {
                    result.ISO = isoResult.Data.Iso;
                }
                else
                {
                    result.ISO = OldISO;
                    if (!isoResult.IsSuccess)
                    {
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            SetValidationError(isoResult.ErrorMessage);
                        });
                        return result;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Optimized picker values loading with caching
        /// </summary>
        private async Task LoadPickerValuesOptimizedAsync()
        {
            try
            {
                IsBusy = true;
                ShowError = false;

                string incrementString = GetIncrementStringOptimized();

                // Check cache first
                var cacheKey = $"picker_values_{incrementString}";
                if (_pickerValuesCache.ContainsKey(cacheKey))
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        BeginPropertyChangeBatch();

                        string[] cachedValueParts = _pickerValuesCache[cacheKey].ToString().Split('|');
                        ShutterSpeedsForPicker = cachedValueParts[0].Split(',');
                        ApeaturesForPicker = cachedValueParts[1].Split(',');
                        ISOsForPicker = cachedValueParts[2].Split(',');

                        _ = EndPropertyChangeBatchAsync();
                    });
                    return;
                }

                // Load values on background thread
                var pickerValues = await Task.Run(() =>
                {
                    var shutterSpeeds = ShutterSpeeds.GetScale(incrementString);
                    var apertures = Apetures.GetScale(incrementString);
                    var isos = ISOs.GetScale(incrementString);

                    return new
                    {
                        ShutterSpeeds = shutterSpeeds,
                        Apertures = apertures,
                        ISOs = isos
                    };
                });

                // Cache the results
                var cacheValue = $"{string.Join(",", pickerValues.ShutterSpeeds)}|{string.Join(",", pickerValues.Apertures)}|{string.Join(",", pickerValues.ISOs)}";
                _pickerValuesCache[cacheKey] = cacheValue;

                // Update UI
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    BeginPropertyChangeBatch();

                    ShutterSpeedsForPicker = pickerValues.ShutterSpeeds;
                    ApeaturesForPicker = pickerValues.Apertures;
                    ISOsForPicker = pickerValues.ISOs;

                    // Set default selections if not already set
                    if (string.IsNullOrEmpty(ShutterSpeedSelected) && ShutterSpeedsForPicker?.Length > 0)
                        ShutterSpeedSelected = ShutterSpeedsForPicker[ShutterSpeedsForPicker.Length / 2];

                    if (string.IsNullOrEmpty(FStopSelected) && ApeaturesForPicker?.Length > 0)
                        FStopSelected = ApeaturesForPicker[ApeaturesForPicker.Length / 2];

                    if (string.IsNullOrEmpty(ISOSelected) && ISOsForPicker?.Length > 0)
                        ISOSelected = ISOsForPicker[ISOsForPicker.Length / 2];

                    _ = EndPropertyChangeBatchAsync();
                });
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    OnSystemError($"Error loading exposure values: {ex.Message}");
                });
            }
            finally
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    IsBusy = false;
                });
            }
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Optimized preset loading
        /// </summary>
        private async Task LoadPresetsOptimizedAsync()
        {
            try
            {
                IsBusy = true;
                ShowError = false;

                var query = new Location.Core.Application.Queries.TipTypes.GetAllTipTypesQuery();
                var result = await _mediator.Send(query);

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (result.IsSuccess && result.Data != null)
                    {
                        AvailablePresets.Clear();

                        foreach (var tipType in result.Data)
                        {
                            AvailablePresets.Add(tipType);
                        }

                        OnPropertyChanged(nameof(AvailablePresets));
                    }
                    else
                    {
                        OnSystemError(result.ErrorMessage ?? "Failed to load presets");
                    }
                });
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    OnSystemError($"Error loading presets: {ex.Message}");
                });
            }
            finally
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    IsBusy = false;
                });
            }
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Optimized preset application
        /// </summary>
        private async Task ApplyPresetOptimizedAsync(TipTypeDto preset)
        {
            try
            {
                var randomTipQuery = new Location.Core.Application.Commands.Tips.GetRandomTipCommand { TipTypeId = preset.Id };
                var result = await _mediator.Send(randomTipQuery);

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (result.IsSuccess && result.Data != null)
                    {
                        var tip = result.Data;

                        BeginPropertyChangeBatch();

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

                        StoreOldValuesOptimized();

                        _ = EndPropertyChangeBatchAsync();
                        _ = CalculateOptimizedAsync();
                    }
                    else
                    {
                        OnSystemError("No tips found for the selected preset category");
                    }
                });
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    OnSystemError($"Error applying preset: {ex.Message}");
                });
            }
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Optimized calculation mode update
        /// </summary>
        private void UpdateCalculationModeOptimized()
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
            _ = CalculateOptimizedAsync();
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Optimized value storage
        /// </summary>
        private void StoreOldValuesOptimized()
        {
            OldShutterSpeed = ShutterSpeedSelected;
            OldFstop = FStopSelected;
            OldISO = ISOSelected;
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Optimized reset with batch updates
        /// </summary>
        public void ResetOptimized()
        {
            try
            {
                BeginPropertyChangeBatch();

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

                _ = EndPropertyChangeBatchAsync();
            }
            catch (Exception ex)
            {
                OnSystemError($"Error resetting exposure calculator: {ex.Message}");
                _ = EndPropertyChangeBatchAsync();
            }
        }

        #endregion

        #region OPTIMIZED LOCK METHODS

        private void ToggleShutterLockOptimized()
        {
            if (IsShutterLocked)
            {
                IsShutterLocked = false;
            }
            else
            {
                IsShutterLocked = true;
                IsApertureLocked = false;
                IsIsoLocked = false;
            }
        }

        private void ToggleApertureLockOptimized()
        {
            if (IsApertureLocked)
            {
                IsApertureLocked = false;
            }
            else
            {
                IsApertureLocked = true;
                IsShutterLocked = false;
                IsIsoLocked = false;
            }
        }

        private void ToggleIsoLockOptimized()
        {
            if (IsIsoLocked)
            {
                IsIsoLocked = false;
            }
            else
            {
                IsIsoLocked = true;
                IsShutterLocked = false;
                IsApertureLocked = false;
            }
        }

        #endregion

        #region Helper Methods

        private string GetIncrementStringOptimized()
        {
            return FullHalfThirds switch
            {
                ExposureIncrements.Full => "Full",
                ExposureIncrements.Half => "Halves",
                ExposureIncrements.Third => "Thirds",
                _ => "Full"
            };
        }

        protected override void OnErrorOccurred(string message)
        {
            ErrorOccurred?.Invoke(this, new OperationErrorEventArgs(OperationErrorSource.Unknown, message));
        }

        public void OnNavigatedToAsync()
        {
            _ = LoadPickerValuesOptimizedAsync();
            LoadPresetsCommand.Execute(null);
        }

        public void OnNavigatedFromAsync()
        {
            // Implementation not required for this use case
        }

        public override void Dispose()
        {
            _calculationLock?.Dispose();
            _pickerValuesCache.Clear();
            base.Dispose();
        }

        #endregion

        #region Helper Classes

        private class ExposureCalculationResult
        {
            public string ShutterSpeed { get; set; } = string.Empty;
            public string Aperture { get; set; } = string.Empty;
            public string ISO { get; set; } = string.Empty;
        }

        #endregion
    }
}