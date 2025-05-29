using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Services;
using Location.Photography.Application.Services;
using Location.Photography.ViewModels.Events;
using Location.Photography.ViewModels.Interfaces;
using MediatR;
using System;
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
        #endregion

        #region Commands
        public IRelayCommand CalculateCommand { get; }
        public IRelayCommand ResetCommand { get; }
        #endregion

        #region Constructors
        public ExposureCalculatorViewModel() : base(null, null)
        {
            // Design-time constructor
            _shutterSpeedsForPicker = ShutterSpeeds.Full;
            _apeaturesForPicker = Apetures.Full;
            _isosForPicker = ISOs.Full;

            CalculateCommand = new RelayCommand(Calculate);
            ResetCommand = new RelayCommand(Reset);
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

            // Default values
            _fullHalfThirds = ExposureIncrements.Full;
            _toCalculate = FixedValue.ShutterSpeeds;
            _evValue = 0;

            // Load initial picker values
            LoadPickerValuesAsync().ConfigureAwait(false);
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
            try
            {
                IsBusy = true;
                ClearErrors();

                // Create base exposure triangle from old values
                var baseExposure = new ExposureTriangleDto
                {
                    ShutterSpeed = OldShutterSpeed,
                    Aperture = OldFstop,
                    Iso = OldISO
                };

                Result<ExposureSettingsDto> isoResult = new Result<ExposureSettingsDto>(true, null, null, null);
                Result<ExposureSettingsDto> shutterResult = new Result<ExposureSettingsDto>(true, null, null, null);
                Result<ExposureSettingsDto> apertureResult = new Result<ExposureSettingsDto>(true, null, null, null);

                if (SkipCalculation != FixedValue.ShutterSpeeds)
                {
                    // 1. Calculate shutter speed
                    shutterResult = _exposureCalculatorService.CalculateShutterSpeedAsync(
                       baseExposure, FStopSelected, ISOSelected, FullHalfThirds, default, EVValue)
                       .GetAwaiter().GetResult();
                    if (shutterResult.IsSuccess && shutterResult.Data != null)
                    {
                        ShutterSpeedResult = shutterResult.Data.ShutterSpeed;
                    }
                    else
                    {
                        ShutterSpeedResult = OldShutterSpeed;
                    }
                }
                else
                {
                    ShutterSpeedResult = ShutterSpeedSelected;
                }

                if (SkipCalculation != FixedValue.Aperture)
                {
                    // 2. Calculate aperture
                    apertureResult = _exposureCalculatorService.CalculateApertureAsync(
                       baseExposure, ShutterSpeedSelected, ISOSelected, FullHalfThirds, default, EVValue)
                       .GetAwaiter().GetResult();
                    if (apertureResult.IsSuccess && apertureResult.Data != null)
                    {
                        FStopResult = apertureResult.Data.Aperture;
                    }
                    else
                    {
                        FStopResult = OldFstop;
                    }
                }
                else
                {
                    FStopResult = FStopSelected;
                }

                if (SkipCalculation != FixedValue.ISO)
                {
                    // 3. Calculate ISO
                    isoResult = _exposureCalculatorService.CalculateIsoAsync(
                                baseExposure, ShutterSpeedSelected, FStopSelected, FullHalfThirds, default, EVValue)
                                .GetAwaiter().GetResult();
                    if (isoResult.IsSuccess && isoResult.Data != null)
                    {
                        ISOResult = isoResult.Data.Iso;
                    }
                    else
                    {
                        ISOResult = OldISO;
                    }
                }
                else
                {
                    ISOResult = ISOSelected;
                }

                // Check if any calculation failed
                if (!shutterResult.IsSuccess || !apertureResult.IsSuccess || !isoResult.IsSuccess)
                {
                    // Show the first error message we find
                    var errorMessage = shutterResult.ErrorMessage ?? apertureResult.ErrorMessage ??
                                   isoResult.ErrorMessage ?? "Calculation failed";
                    SetValidationError(errorMessage);
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

                // Clear results
                ShutterSpeedResult = string.Empty;
                FStopResult = string.Empty;
                ISOResult = string.Empty;

                // Clear error message
                ShowError = false;
                ClearErrors();
            }
            catch (Exception ex)
            {
                OnSystemError($"Error resetting exposure calculator: {ex.Message}");
            }
        }

        protected override void OnErrorOccurred(string message)
        {
            ErrorOccurred?.Invoke(this, new OperationErrorEventArgs(OperationErrorSource.Unknown, message));
        }

        public void OnNavigatedToAsync()
        {
             LoadPickerValuesAsync().ConfigureAwait(false);
        }

        public void OnNavigatedFromAsync()
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}