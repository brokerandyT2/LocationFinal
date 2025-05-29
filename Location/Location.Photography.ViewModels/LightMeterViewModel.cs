using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Location.Core.Application.Common.Interfaces.Persistence;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Services;
using Location.Photography.Application.Services;
using Location.Photography.ViewModels.Events;
using MediatR;
using System;
using System.Threading.Tasks;

namespace Location.Photography.ViewModels
{
    public class LightMeterViewModel : ViewModelBase
    {
        #region Fields
        private readonly IMediator _mediator;
        private readonly ISettingRepository _settingRepository;
        private readonly IExposureCalculatorService _exposureCalculatorService;
        private readonly IErrorDisplayService _errorDisplayService;

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
            set => SetProperty(ref _selectedEV, value);
        }

        public float CurrentLux
        {
            get => _currentLux;
            set
            {
                if (SetProperty(ref _currentLux, value))
                {
                    CalculateEV();
                }
            }
        }

        // Step Selection Properties
        public bool IsFullStep
        {
            get => _isFullStep;
            set => SetProperty(ref _isFullStep, value);
        }

        public bool IsHalfStep
        {
            get => _isHalfStep;
            set => SetProperty(ref _isHalfStep, value);
        }

        public bool IsThirdStep
        {
            get => _isThirdStep;
            set => SetProperty(ref _isThirdStep, value);
        }

        public ExposureIncrements CurrentStep
        {
            get => _currentStep;
            set
            {
                if (SetProperty(ref _currentStep, value))
                {
                    UpdateStepFlags();
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
                }
            }
        }

        public int MaxApertureIndex => ApertureArray?.Length - 1 ?? 0;

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
                }
            }
        }

        public int MaxIsoIndex => IsoArray?.Length - 1 ?? 0;

        public string SelectedIso => IsoArray?[Math.Min(SelectedIsoIndex, MaxIsoIndex)] ?? "100";
        public string MinIso => IsoArray?[MaxIsoIndex] ?? "50"; // ISO array is in reverse order (high to low)
        public string MaxIso => IsoArray?[0] ?? "25600";

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
            LoadDefaultArrays();
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
            LoadDefaultArrays();
        }
        #endregion

        #region Methods

        private void InitializeCommands()
        {
            ResetCommand = new RelayCommand(Reset);
        }

        private void LoadDefaultArrays()
        {
            // Load default arrays (Third step)
            ApertureArray = Apetures.Thirds;
            IsoArray = ISOs.Thirds;
            ShutterSpeedArray = ShutterSpeeds.Thirds;

            // Set default selections to middle values
            SelectedApertureIndex = ApertureArray.Length / 2;
            SelectedIsoIndex = IsoArray.Length / 2;
            SelectedShutterSpeedIndex = ShutterSpeedArray.Length / 2;
        }

        public async Task LoadExposureArraysAsync()
        {
            try
            {
                IsBusy = true;
                ClearErrors();

                if (_exposureCalculatorService != null)
                {
                    // Load arrays from service based on current step
                    var apertureResult = await _exposureCalculatorService.GetAperturesAsync(CurrentStep);
                    var isoResult = await _exposureCalculatorService.GetIsosAsync(CurrentStep);
                    var shutterResult = await _exposureCalculatorService.GetShutterSpeedsAsync(CurrentStep);

                    if (apertureResult.IsSuccess)
                        ApertureArray = apertureResult.Data;

                    if (isoResult.IsSuccess)
                        IsoArray = isoResult.Data;

                    if (shutterResult.IsSuccess)
                        ShutterSpeedArray = shutterResult.Data;
                }
                else
                {
                    // Fallback to utility classes
                    string stepString = GetStepString();
                    ApertureArray = Apetures.GetScale(stepString);
                    IsoArray = ISOs.GetScale(stepString);
                    ShutterSpeedArray = ShutterSpeeds.GetScale(stepString);
                }

                // Ensure valid indices after array change
                EnsureValidIndices();

                // Notify UI of array changes
                OnPropertyChanged(nameof(MaxApertureIndex));
                OnPropertyChanged(nameof(MaxIsoIndex));
                OnPropertyChanged(nameof(MaxShutterSpeedIndex));
                OnPropertyChanged(nameof(MinAperture));
                OnPropertyChanged(nameof(MaxAperture));
                OnPropertyChanged(nameof(MinIso));
                OnPropertyChanged(nameof(MaxIso));
                OnPropertyChanged(nameof(MinShutterSpeed));
                OnPropertyChanged(nameof(MaxShutterSpeed));
            }
            catch (Exception ex)
            {
                OnSystemError($"Error loading exposure arrays: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private string GetStepString()
        {
            return CurrentStep switch
            {
                ExposureIncrements.Full => "Full",
                ExposureIncrements.Half => "Half",
                ExposureIncrements.Third => "Third",
                _ => "Third"
            };
        }

        private void EnsureValidIndices()
        {
            // Ensure indices are within valid ranges
            if (ApertureArray != null && SelectedApertureIndex >= ApertureArray.Length)
                SelectedApertureIndex = ApertureArray.Length / 2;

            if (IsoArray != null && SelectedIsoIndex >= IsoArray.Length)
                SelectedIsoIndex = IsoArray.Length / 2;

            if (ShutterSpeedArray != null && SelectedShutterSpeedIndex >= ShutterSpeedArray.Length)
                SelectedShutterSpeedIndex = ShutterSpeedArray.Length / 2;
        }

        private void UpdateStepFlags()
        {
            IsFullStep = (CurrentStep == ExposureIncrements.Full);
            IsHalfStep = (CurrentStep == ExposureIncrements.Half);
            IsThirdStep = (CurrentStep == ExposureIncrements.Third);
        }

        public void UpdateLightReading(float lux)
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

        public void CalculateEV()
        {
            try
            {
                if (CurrentLux <= 0)
                {
                    CalculatedEV = 0;
                    return;
                }

                // Parse current values
                double aperture = ParseAperture(SelectedAperture);
                int iso = ParseIso(SelectedIso);

                if (aperture <= 0 || iso <= 0)
                {
                    CalculatedEV = 0;
                    return;
                }

                // Calculate EV using standard formula
                // EV = log2((Lux * Aperture²) / (Calibration_Constant * ISO))
                const double CalibrationConstant = 12.5;
                double ev = Math.Log2((CurrentLux * aperture * aperture) / (CalibrationConstant * iso));

                // Round to step precision
                CalculatedEV = RoundToStep(ev);
            }
            catch (Exception ex)
            {
                OnSystemError($"Error calculating EV: {ex.Message}");
                CalculatedEV = 0;
            }
        }

        public void CalculateFromEV()
        {
            try
            {
                // This would adjust other settings based on EV change
                // For now, just update the calculated EV
                CalculatedEV = RoundToStep(SelectedEV);
            }
            catch (Exception ex)
            {
                OnSystemError($"Error calculating from EV: {ex.Message}");
            }
        }

        private double RoundToStep(double ev)
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

        private void Reset()
        {
            try
            {
                // Reset to middle values
                SelectedApertureIndex = ApertureArray?.Length / 2 ?? 0;
                SelectedIsoIndex = IsoArray?.Length / 2 ?? 0;
                SelectedShutterSpeedIndex = ShutterSpeedArray?.Length / 2 ?? 0;

                CurrentLux = 0;
                CalculatedEV = 0;
                SelectedEV = 0;
                CurrentStep = ExposureIncrements.Third;

                ClearErrors();
            }
            catch (Exception ex)
            {
                OnSystemError($"Error resetting light meter: {ex.Message}");
            }
        }

        protected override void OnErrorOccurred(string message)
        {
            ErrorOccurred?.Invoke(this, new OperationErrorEventArgs(OperationErrorSource.Unknown, message));
        }

        #endregion
    }
}