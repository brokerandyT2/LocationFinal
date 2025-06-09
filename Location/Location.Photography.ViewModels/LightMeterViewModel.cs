using CommunityToolkit.Mvvm.Input;
using Location.Core.Application.Common.Interfaces.Persistence;
using Location.Core.Application.Services;
using Location.Photography.Application.Services;
using Location.Photography.ViewModels.Events;
using MediatR;

namespace Location.Photography.ViewModels
{
    public class LightMeterViewModel : ViewModelBase
    {
        #region Fields
        private readonly IMediator _mediator;
        private readonly ISettingRepository _settingRepository;
        private readonly IExposureCalculatorService _exposureCalculatorService;
        private readonly IErrorDisplayService _errorDisplayService;

        // PERFORMANCE: Threading and caching
        private readonly SemaphoreSlim _calculationLock = new(1, 1);
        private readonly Dictionary<ExposureIncrements, ExposureArrays> _arrayCache = new();
        private DateTime _lastCalculationTime = DateTime.MinValue;
        private const int CALCULATION_THROTTLE_MS = 100;

        // Current exposure values
        private float _currentLux;
        private double _calculatedEV;
        private double _selectedEV;
        private ExposureIncrements _currentStep = ExposureIncrements.Third;

        // Slider arrays and indices
        private string[] _apertureArray;
        private string[] _isoArray;
        private string[] _shutterSpeedArray;
        private int _selectedApertureIndex = 0;
        private int _selectedIsoIndex = 0;
        private int _selectedShutterSpeedIndex = 0;

        // Step selection flags
        private bool _isFullStep;
        private bool _isHalfStep = false;
        private bool _isThirdStep = true; // Default to thirds
        #endregion

        #region Events
        public new event EventHandler<OperationErrorEventArgs> ErrorOccurred;
        #endregion

        #region Properties

        // EV Properties
        public double CalculatedEV
        {
            get => _calculatedEV;
            set => SetProperty(ref _calculatedEV, value);
        }

        public double SelectedEV
        {
            get => _selectedEV;
            set
            {
                if (SetProperty(ref _selectedEV, value))
                {
                    _ = CalculateFromEVOptimizedAsync();
                }
            }
        }

        public float CurrentLux
        {
            get => _currentLux;
            set
            {
                if (SetProperty(ref _currentLux, value))
                {
                    _ = CalculateEVOptimizedAsync();
                }
            }
        }

        // Step Selection Properties
        public bool IsFullStep
        {
            get => _isFullStep;
            set
            {
                if (SetProperty(ref _isFullStep, value) && value)
                {
                    CurrentStep = ExposureIncrements.Full;
                }
            }
        }

        public bool IsHalfStep
        {
            get => _isHalfStep;
            set
            {
                if (SetProperty(ref _isHalfStep, value) && value)
                {
                    CurrentStep = ExposureIncrements.Half;
                }
            }
        }

        public bool IsThirdStep
        {
            get => _isThirdStep;
            set
            {
                if (SetProperty(ref _isThirdStep, value) && value)
                {
                    CurrentStep = ExposureIncrements.Third;
                }
            }
        }

        public ExposureIncrements CurrentStep
        {
            get => _currentStep;
            set
            {
                if (SetProperty(ref _currentStep, value))
                {
                    UpdateStepFlagsOptimized();
                    _ = LoadExposureArraysOptimizedAsync();
                }
            }
        }

        // Aperture Properties
        public string[] ApertureArray
        {
            get => _apertureArray;
            set => SetProperty(ref _apertureArray, value);
        }

        public int SelectedApertureIndex
        {
            get => _selectedApertureIndex;
            set
            {
                if (SetProperty(ref _selectedApertureIndex, value))
                {
                    OnPropertyChanged(nameof(SelectedAperture));
                    _ = CalculateEVOptimizedAsync();
                }
            }
        }

        public int MaxApertureIndex => ApertureArray?.Length - 1 ?? 0;

        public void SetCalculatedEV(double ev)
        {
            CalculatedEV = ev;
        }

        public string SelectedAperture => ApertureArray?[Math.Min(SelectedApertureIndex, MaxApertureIndex)] ?? "f/5.6";
        public string MinAperture => ApertureArray?[0] ?? "f/1";
        public string MaxAperture => ApertureArray?[MaxApertureIndex] ?? "f/64";

        // ISO Properties
        public string[] IsoArray
        {
            get => _isoArray;
            set => SetProperty(ref _isoArray, value);
        }

        public int SelectedIsoIndex
        {
            get => _selectedIsoIndex;
            set
            {
                if (SetProperty(ref _selectedIsoIndex, value))
                {
                    OnPropertyChanged(nameof(SelectedIso));
                    _ = CalculateEVOptimizedAsync();
                }
            }
        }

        public int MaxIsoIndex => IsoArray?.Length - 1 ?? 0;

        public string SelectedIso => IsoArray?[Math.Min(SelectedIsoIndex, MaxIsoIndex)] ?? "100";
        public string MinIso => IsoArray?[0] ?? "50";
        public string MaxIso => IsoArray?[MaxIsoIndex] ?? "25600";

        // Shutter Speed Properties
        public string[] ShutterSpeedArray
        {
            get => _shutterSpeedArray;
            set => SetProperty(ref _shutterSpeedArray, value);
        }

        public int SelectedShutterSpeedIndex
        {
            get => _selectedShutterSpeedIndex;
            set
            {
                if (SetProperty(ref _selectedShutterSpeedIndex, value))
                {
                    OnPropertyChanged(nameof(SelectedShutterSpeed));
                    _ = CalculateEVOptimizedAsync();
                }
            }
        }

        public int MaxShutterSpeedIndex => ShutterSpeedArray?.Length - 1 ?? 0;

        public string SelectedShutterSpeed => ShutterSpeedArray?[Math.Min(SelectedShutterSpeedIndex, MaxShutterSpeedIndex)] ?? "1/60";
        public string MinShutterSpeed => ShutterSpeedArray?[0] ?? "30\"";
        public string MaxShutterSpeed => ShutterSpeedArray?[MaxShutterSpeedIndex] ?? "1/8000";

        #endregion

        #region Commands
        public IRelayCommand ResetCommand { get; private set; }
        #endregion

        #region Constructors
        public LightMeterViewModel() : base(null, null)
        {
            // Design-time constructor
            LoadDefaultArraysOptimized();
            InitializeCommands();
        }

        public LightMeterViewModel(
            IMediator mediator,
            ISettingRepository settingRepository,
            IExposureCalculatorService exposureCalculatorService,
            IErrorDisplayService errorDisplayService)
            : base(null, errorDisplayService)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _settingRepository = settingRepository ?? throw new ArgumentNullException(nameof(settingRepository));
            _exposureCalculatorService = exposureCalculatorService ?? throw new ArgumentNullException(nameof(exposureCalculatorService));
            _errorDisplayService = errorDisplayService;

            InitializeCommands();
            LoadDefaultArraysOptimized();
        }
        #endregion

        #region PERFORMANCE OPTIMIZED METHODS

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Throttled EV calculation
        /// </summary>
        private async Task CalculateEVOptimizedAsync()
        {
            // Throttle rapid updates
            var now = DateTime.Now;
            if ((now - _lastCalculationTime).TotalMilliseconds < CALCULATION_THROTTLE_MS)
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
                await Task.Run(() => CalculateEVCore());
            }
            finally
            {
                _calculationLock.Release();
            }
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Core EV calculation on background thread
        /// </summary>
        private async Task CalculateEVCore()
        {
            try
            {
                if (CurrentLux <= 0)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        CalculatedEV = 0;
                    });
                    return;
                }

                // Parse current values on background thread
                double aperture = ParseApertureOptimized(SelectedAperture);
                int iso = ParseIsoOptimized(SelectedIso);

                if (aperture <= 0 || iso <= 0)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        CalculatedEV = 0;
                    });
                    return;
                }

                // Calculate EV using standard formula
                const double CalibrationConstant = 12.5;
                double ev = Math.Log2((CurrentLux * aperture * aperture) / (CalibrationConstant * iso));

                // Round to step precision
                double roundedEV = RoundToStepOptimized(ev);

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    CalculatedEV = roundedEV;
                });
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    OnSystemError($"Error calculating EV: {ex.Message}");
                    CalculatedEV = 0;
                });
            }
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Optimized EV-based calculation
        /// </summary>
        private async Task CalculateFromEVOptimizedAsync()
        {
            try
            {
                // This would adjust other settings based on EV change
                double roundedEV = RoundToStepOptimized(SelectedEV);
                CalculatedEV = roundedEV;
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    OnSystemError($"Error calculating from EV: {ex.Message}");
                });
            }
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Optimized array loading with caching
        /// </summary>
        public async Task LoadExposureArraysOptimizedAsync()
        {
            try
            {
                IsBusy = true;
                ClearErrors();

                // Check cache first
                if (_arrayCache.TryGetValue(CurrentStep, out var cachedArrays))
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        ApplyCachedArraysOptimized(cachedArrays);
                    });
                    return;
                }

                // Load arrays on background thread
                var arrays = await Task.Run(async () =>
                {
                    try
                    {
                        ExposureArrays result;

                        if (_exposureCalculatorService != null)
                        {
                            // Load arrays from service based on current step
                            var apertureTask = _exposureCalculatorService.GetAperturesAsync(CurrentStep);
                            var isoTask = _exposureCalculatorService.GetIsosAsync(CurrentStep);
                            var shutterTask = _exposureCalculatorService.GetShutterSpeedsAsync(CurrentStep);

                            var results = await Task.WhenAll(apertureTask, isoTask, shutterTask);

                            result = new ExposureArrays
                            {
                                Apertures = results[0].IsSuccess ? results[0].Data : GetFallbackApertures(),
                                ISOs = results[1].IsSuccess ? results[1].Data : GetFallbackISOs(),
                                ShutterSpeeds = results[2].IsSuccess ? results[2].Data : GetFallbackShutterSpeeds()
                            };
                        }
                        else
                        {
                            // Fallback to utility classes
                            string stepString = GetStepStringOptimized();
                            result = new ExposureArrays
                            {
                                Apertures = Interfaces.Apetures.GetScale(stepString),
                                ISOs = Interfaces.ISOs.GetScale(stepString),
                                ShutterSpeeds = Interfaces.ShutterSpeeds.GetScale(stepString)
                            };
                        }

                        return result;
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException($"Error loading exposure arrays: {ex.Message}", ex);
                    }
                });

                // Cache the results
                _arrayCache[CurrentStep] = arrays;

                // Update UI on main thread
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    ApplyCachedArraysOptimized(arrays);
                    EnsureValidIndicesOptimized();
                    UpdatePropertyNotificationsOptimized();
                });
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    OnSystemError($"Error loading exposure arrays: {ex.Message}");
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
        /// PERFORMANCE OPTIMIZATION: Batch apply cached arrays
        /// </summary>
        private void ApplyCachedArraysOptimized(ExposureArrays arrays)
        {
            BeginPropertyChangeBatch();

            ApertureArray = arrays.Apertures;
            IsoArray = arrays.ISOs;
            ShutterSpeedArray = arrays.ShutterSpeeds;

            _ = EndPropertyChangeBatchAsync();
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Optimized light reading update
        /// </summary>
        public void UpdateLightReadingOptimized(float lux)
        {
            try
            {
                CurrentLux = lux;
            }
            catch (Exception ex)
            {
                OnSystemError($"Error updating light reading: {ex.Message}");
            }
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Optimized reset with batch updates
        /// </summary>
        private void ResetOptimized()
        {
            try
            {
                BeginPropertyChangeBatch();

                // Reset to middle values
                SelectedApertureIndex = ApertureArray?.Length / 2 ?? 0;
                SelectedIsoIndex = IsoArray?.Length / 2 ?? 0;
                SelectedShutterSpeedIndex = ShutterSpeedArray?.Length / 2 ?? 0;

                CurrentLux = 0;
                CalculatedEV = 0;
                SelectedEV = 0;
                CurrentStep = ExposureIncrements.Third;

                ClearErrors();

                _ = EndPropertyChangeBatchAsync();
            }
            catch (Exception ex)
            {
                OnSystemError($"Error resetting light meter: {ex.Message}");
                _ = EndPropertyChangeBatchAsync();
            }
        }

        #endregion

        #region Helper Methods

        private void InitializeCommands()
        {
            ResetCommand = new RelayCommand(ResetOptimized);
        }

        private void LoadDefaultArraysOptimized()
        {
            // Load default arrays (Third step) with caching
            var defaultArrays = new ExposureArrays
            {
                Apertures = Apetures.Thirds,
                ISOs = ISOs.Thirds,
                ShutterSpeeds = ShutterSpeeds.Thirds
            };

            _arrayCache[ExposureIncrements.Third] = defaultArrays;
            ApplyCachedArraysOptimized(defaultArrays);

            // Set default selections to middle values
            SelectedApertureIndex = ApertureArray?.Length / 2 ?? 0;
            SelectedIsoIndex = IsoArray?.Length / 2 ?? 0;
            SelectedShutterSpeedIndex = ShutterSpeedArray?.Length / 2 ?? 0;
        }

        private string GetStepStringOptimized()
        {
            return CurrentStep switch
            {
                ExposureIncrements.Full => "Full",
                ExposureIncrements.Half => "Half",
                ExposureIncrements.Third => "Third",
                _ => "Third"
            };
        }

        private void EnsureValidIndicesOptimized()
        {
            // Ensure indices are within valid ranges
            if (ApertureArray != null && SelectedApertureIndex >= ApertureArray.Length)
                SelectedApertureIndex = ApertureArray.Length / 2;

            if (IsoArray != null && SelectedIsoIndex >= IsoArray.Length)
                SelectedIsoIndex = IsoArray.Length / 2;

            if (ShutterSpeedArray != null && SelectedShutterSpeedIndex >= ShutterSpeedArray.Length)
                SelectedShutterSpeedIndex = ShutterSpeedArray.Length / 2;
        }

        private void UpdateStepFlagsOptimized()
        {
            BeginPropertyChangeBatch();

            _isFullStep = (CurrentStep == ExposureIncrements.Full);
            _isHalfStep = (CurrentStep == ExposureIncrements.Half);
            _isThirdStep = (CurrentStep == ExposureIncrements.Third);

            _ = EndPropertyChangeBatchAsync();
        }

        private void UpdatePropertyNotificationsOptimized()
        {
            // Batch notify UI of property changes
            BeginPropertyChangeBatch();

            OnPropertyChanged(nameof(MaxApertureIndex));
            OnPropertyChanged(nameof(MaxIsoIndex));
            OnPropertyChanged(nameof(MaxShutterSpeedIndex));
            OnPropertyChanged(nameof(MinAperture));
            OnPropertyChanged(nameof(MaxAperture));
            OnPropertyChanged(nameof(MinIso));
            OnPropertyChanged(nameof(MaxIso));
            OnPropertyChanged(nameof(MinShutterSpeed));
            OnPropertyChanged(nameof(MaxShutterSpeed));

            _ = EndPropertyChangeBatchAsync();
        }

        private double RoundToStepOptimized(double ev)
        {
            // Round EV to the appropriate step increment
            double stepSize = CurrentStep switch
            {
                ExposureIncrements.Full => 1.0,
                ExposureIncrements.Half => 0.5,
                ExposureIncrements.Third => 1.0 / 3.0,
                _ => 1.0 / 3.0
            };

            return Math.Round(ev / stepSize) * stepSize;
        }

        private double ParseApertureOptimized(string aperture)
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

        private int ParseIsoOptimized(string iso)
        {
            if (string.IsNullOrWhiteSpace(iso))
                return 100;

            if (int.TryParse(iso, out int value))
            {
                return value;
            }

            return 100; // Default fallback
        }

        private string[] GetFallbackApertures()
        {
            return Apetures.GetScale(GetStepStringOptimized());
        }

        private string[] GetFallbackISOs()
        {
            return ISOs.GetScale(GetStepStringOptimized());
        }

        private string[] GetFallbackShutterSpeeds()
        {
            return ShutterSpeeds.GetScale(GetStepStringOptimized());
        }

        public void UpdateLightReading(float lux)
        {
            UpdateLightReadingOptimized(lux);
        }

        public void CalculateEV()
        {
            _ = CalculateEVOptimizedAsync();
        }

        public void CalculateFromEV()
        {
            _ = CalculateFromEVOptimizedAsync();
        }

        protected override void OnErrorOccurred(string message)
        {
            ErrorOccurred?.Invoke(this, new OperationErrorEventArgs(OperationErrorSource.Unknown, message));
        }

        public void OnNavigatedToAsync()
        {
            InitializeCommands();
            LoadDefaultArraysOptimized();
        }

        public void OnNavigatedFromAsync()
        {
            // Cleanup not required for this implementation
        }

        public override void Dispose()
        {
            _calculationLock?.Dispose();
            _arrayCache.Clear();
            base.Dispose();
        }

        #endregion

        #region Helper Classes

        private class ExposureArrays
        {
            public string[] Apertures { get; set; } = Array.Empty<string>();
            public string[] ISOs { get; set; } = Array.Empty<string>();
            public string[] ShutterSpeeds { get; set; } = Array.Empty<string>();
        }

        #endregion
    }
}