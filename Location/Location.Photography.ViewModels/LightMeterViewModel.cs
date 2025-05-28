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
        private readonly IErrorDisplayService _errorDisplayService;

        private string _selectedIso = "100";
        private string _selectedAperture = "f/5.6";
        private string _selectedShutterSpeed = "1/60";
        private string _exposureStep = "Third";
        private float _currentLux;
        private double _currentEV;
        private bool _isUserInteracting;
        private bool _isSensorActive;
        private string[] _shutterSpeedsForPicker;
        private string[] _aperturesForPicker;
        private string[] _isosForPicker;
        private ExposureIncrements _fullHalfThirds = ExposureIncrements.Third;
        private double _needleAngle;
        private string _lightConditionText = "Unknown";
        #endregion

        #region Events
        public new event EventHandler<OperationErrorEventArgs> ErrorOccurred;
        public event EventHandler<LightMeterUpdateEventArgs> LightMeterUpdated;
        #endregion

        #region Properties
        public string SelectedIso
        {
            get => _selectedIso;
            set
            {
                if (SetProperty(ref _selectedIso, value))
                {
                    CalculateEV();
                    OnPropertyChanged(nameof(ExposureInfo));
                }
            }
        }

        public string SelectedAperture
        {
            get => _selectedAperture;
            set
            {
                if (SetProperty(ref _selectedAperture, value))
                {
                    CalculateEV();
                    OnPropertyChanged(nameof(ExposureInfo));
                }
            }
        }

        public string SelectedShutterSpeed
        {
            get => _selectedShutterSpeed;
            set
            {
                if (SetProperty(ref _selectedShutterSpeed, value))
                {
                    CalculateEV();
                    OnPropertyChanged(nameof(ExposureInfo));
                }
            }
        }

        public string ExposureStep
        {
            get => _exposureStep;
            set
            {
                if (SetProperty(ref _exposureStep, value))
                {
                    LoadPickerValuesAsync().ConfigureAwait(false);
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
                    CalculateEV();
                    UpdateLightCondition();
                    OnPropertyChanged(nameof(LightConditionText));
                }
            }
        }

        public double CurrentEV
        {
            get => _currentEV;
            set
            {
                if (SetProperty(ref _currentEV, value))
                {
                    CalculateNeedleAngle();
                    OnPropertyChanged(nameof(NeedleAngle));
                }
            }
        }

        public bool IsUserInteracting
        {
            get => _isUserInteracting;
            set => SetProperty(ref _isUserInteracting, value);
        }

        public bool IsSensorActive
        {
            get => _isSensorActive;
            set => SetProperty(ref _isSensorActive, value);
        }

        public string[] ShutterSpeedsForPicker
        {
            get => _shutterSpeedsForPicker;
            set => SetProperty(ref _shutterSpeedsForPicker, value);
        }

        public string[] AperturesForPicker
        {
            get => _aperturesForPicker;
            set => SetProperty(ref _aperturesForPicker, value);
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
                    LoadPickerValuesAsync().ConfigureAwait(false);
                }
            }
        }

        public double NeedleAngle
        {
            get => _needleAngle;
            set => SetProperty(ref _needleAngle, value);
        }

        public string LightConditionText
        {
            get => _lightConditionText;
            set => SetProperty(ref _lightConditionText, value);
        }

        public string ExposureInfo => $"ISO {SelectedIso} • {SelectedAperture} • {SelectedShutterSpeed}";
        #endregion

        #region Commands
        public IRelayCommand ResetCommand { get; private set; }
        #endregion

        #region Constructors
        public LightMeterViewModel() : base(null, null)
        {
            // Design-time constructor
            LoadDefaultPickerValues();
            InitializeCommands();
        }

        public LightMeterViewModel(
            IMediator mediator,
            ISettingRepository settingRepository,
            IErrorDisplayService errorDisplayService)
            : base(null, errorDisplayService)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _settingRepository = settingRepository ?? throw new ArgumentNullException(nameof(settingRepository));
            _errorDisplayService = errorDisplayService ?? throw new ArgumentNullException(nameof(errorDisplayService));

            InitializeCommands();
            LoadPickerValuesAsync().ConfigureAwait(false);
        }
        #endregion

        #region Methods
        private void InitializeCommands()
        {
            ResetCommand = new RelayCommand(Reset);
        }

        private void LoadDefaultPickerValues()
        {
            // Use the existing utility classes with default Third increments
            _shutterSpeedsForPicker = ShutterSpeeds.Thirds;
            _aperturesForPicker = Apetures.Thirds;
            _isosForPicker = ISOs.Thirds;
        }

        private async Task LoadPickerValuesAsync()
        {
            try
            {
                IsBusy = true;
                ClearErrors();

                string incrementString = GetIncrementString();

                // Use the existing utility classes from ShutterSpeeds.cs
                ShutterSpeedsForPicker = ShutterSpeeds.GetScale(incrementString);
                AperturesForPicker = Apetures.GetScale(incrementString);
                ISOsForPicker = ISOs.GetScale(incrementString);

                // Set default values if current selections are not in the new arrays
                EnsureValidSelections();
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
                ExposureIncrements.Half => "Half",
                ExposureIncrements.Third => "Third",
                _ => "Third"
            };
        }

        private void EnsureValidSelections()
        {
            // Ensure current selections are valid for the new scale
            if (ShutterSpeedsForPicker != null && !Array.Exists(ShutterSpeedsForPicker, s => s == SelectedShutterSpeed))
            {
                SelectedShutterSpeed = ShutterSpeedsForPicker.Length > 0 ? ShutterSpeedsForPicker[ShutterSpeedsForPicker.Length / 2] : "1/60";
            }

            if (AperturesForPicker != null && !Array.Exists(AperturesForPicker, a => a == SelectedAperture))
            {
                SelectedAperture = AperturesForPicker.Length > 0 ? AperturesForPicker[AperturesForPicker.Length / 2] : "f/5.6";
            }

            if (ISOsForPicker != null && !Array.Exists(ISOsForPicker, i => i == SelectedIso))
            {
                SelectedIso = ISOsForPicker.Length > 0 ? ISOsForPicker[ISOsForPicker.Length / 2] : "100";
            }
        }

        public void UpdateExposureSettings(string iso, string aperture, string shutterSpeed)
        {
            try
            {
                // Validate that the values exist in our picker arrays
                if (ISOsForPicker != null && Array.Exists(ISOsForPicker, i => i == iso))
                {
                    SelectedIso = iso;
                }

                if (AperturesForPicker != null && Array.Exists(AperturesForPicker, a => a == aperture))
                {
                    SelectedAperture = aperture;
                }

                if (ShutterSpeedsForPicker != null && Array.Exists(ShutterSpeedsForPicker, s => s == shutterSpeed))
                {
                    SelectedShutterSpeed = shutterSpeed;
                }

                // Notify light meter updated
                LightMeterUpdated?.Invoke(this, new LightMeterUpdateEventArgs(SelectedIso, SelectedAperture, SelectedShutterSpeed, CurrentEV));
            }
            catch (Exception ex)
            {
                OnSystemError($"Error updating exposure settings: {ex.Message}");
            }
        }

        // Method to be called from code-behind with light sensor readings
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

        // Method to be called from code-behind to indicate sensor status
        public void SetSensorStatus(bool isActive)
        {
            IsSensorActive = isActive;
        }

        private void CalculateEV()
        {
            try
            {
                if (CurrentLux <= 0)
                {
                    CurrentEV = 0;
                    return;
                }

                // Parse aperture (remove 'f/' prefix and handle comma decimal separator)
                string apertureStr = SelectedAperture.Replace("f/", "").Replace(",", ".");
                if (!double.TryParse(apertureStr, out double aperture) || aperture <= 0)
                {
                    aperture = 5.6; // Default fallback
                }

                // Parse ISO
                if (!int.TryParse(SelectedIso, out int iso) || iso <= 0)
                {
                    iso = 100; // Default fallback
                }

                // Calculate EV using standard formula
                // EV = log2((Lux * Aperture²) / (Calibration_Constant * ISO))
                const double CalibrationConstant = 12.5;
                double ev = Math.Log2((CurrentLux * aperture * aperture) / (CalibrationConstant * iso));

                CurrentEV = Math.Round(ev, 1);
            }
            catch (Exception ex)
            {
                OnSystemError($"Error calculating EV: {ex.Message}");
                CurrentEV = 0;
            }
        }

        private void CalculateNeedleAngle()
        {
            // Convert EV (-5 to +5) to needle angle for meter display  
            // Clamp EV to display range
            double clampedEV = Math.Max(-5, Math.Min(5, CurrentEV));

            // Convert to angle (assuming 150° to 30° range like in the drawable)
            double startAngle = 150; // degrees
            double endAngle = 30;    // degrees
            double range = startAngle - endAngle; // 120 degrees total

            // Map EV range (-5 to +5) to angle range
            double normalizedEV = (clampedEV + 5) / 10.0; // 0 to 1
            NeedleAngle = startAngle - (normalizedEV * range);
        }

        private void UpdateLightCondition()
        {
            LightConditionText = CurrentLux switch
            {
                < 1 => "Very Dark",
                < 10 => "Dark",
                < 100 => "Dim",
                < 1000 => "Normal Indoor",
                < 10000 => "Bright Indoor",
                < 50000 => "Daylight",
                _ => "Bright Daylight"
            };
        }

        private void Reset()
        {
            try
            {
                // Reset to default values from the middle of each array
                if (ShutterSpeedsForPicker?.Length > 0)
                    SelectedShutterSpeed = ShutterSpeedsForPicker[ShutterSpeedsForPicker.Length / 2];

                if (AperturesForPicker?.Length > 0)
                    SelectedAperture = AperturesForPicker[AperturesForPicker.Length / 2];

                if (ISOsForPicker?.Length > 0)
                    SelectedIso = ISOsForPicker[ISOsForPicker.Length / 2];

                CurrentLux = 0;
                CurrentEV = 0;
                FullHalfThirds = ExposureIncrements.Third;

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

    // Event args class for light meter updates
    public class LightMeterUpdateEventArgs : EventArgs
    {
        public string Iso { get; }
        public string Aperture { get; }
        public string ShutterSpeed { get; }
        public double EV { get; }

        public LightMeterUpdateEventArgs(string iso, string aperture, string shutterSpeed, double ev)
        {
            Iso = iso;
            Aperture = aperture;
            ShutterSpeed = shutterSpeed;
            EV = ev;
        }
    }
}