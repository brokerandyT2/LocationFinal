// Location.Photography.ViewModels/ExposureCalculatorViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Location.Core.Application.Common.Models;
using Location.Photography.Application.Services;
using Location.Photography.ViewModels.Events;
using MediatR;

namespace Location.Photography.ViewModels
{
    public class ExposureCalculatorViewModel : ObservableObject
    {
        #region Fields
        private readonly IMediator _mediator;
        private readonly IExposureCalculatorService _exposureCalculatorService;

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
        private bool _vmIsBusy;
        private bool _showError;
        private string _errorMessage;
        #endregion

        #region Events
        public event EventHandler<OperationErrorEventArgs> ErrorOccurred;
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

        public bool VmIsBusy
        {
            get => _vmIsBusy;
            set => SetProperty(ref _vmIsBusy, value);
        }

        public bool ShowError
        {
            get => _showError;
            set => SetProperty(ref _showError, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                if (SetProperty(ref _errorMessage, value))
                {
                    ShowError = !string.IsNullOrEmpty(value);
                    if (ShowError)
                    {
                        OnErrorOccurred(new OperationErrorEventArgs(
                            OperationErrorSource.Unknown,
                            value));
                    }
                }
            }
        }
        #endregion

        #region Commands
        public IRelayCommand CalculateCommand { get; }
        public IRelayCommand ResetCommand { get; }
        #endregion

        #region Constructors
        public ExposureCalculatorViewModel()
        {
            // Design-time constructor
            _shutterSpeedsForPicker = ShutterSpeeds.Full;
            _apeaturesForPicker = Apetures.Full;
            _isosForPicker = ISOs.Full;

            CalculateCommand = new RelayCommand(Calculate);
            ResetCommand = new RelayCommand(Reset);
        }

        public ExposureCalculatorViewModel(IMediator mediator, IExposureCalculatorService exposureCalculatorService)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _exposureCalculatorService = exposureCalculatorService ?? throw new ArgumentNullException(nameof(exposureCalculatorService));

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
                VmIsBusy = true;
                ShowError = false;

                string incrementString = GetIncrementString();

                // Use the shared utility classes to get the appropriate values
                ShutterSpeedsForPicker = ShutterSpeeds.GetScale(incrementString);
                ApeaturesForPicker = Apetures.GetScale(incrementString);
                ISOsForPicker = ISOs.GetScale(incrementString);
            }
            catch (Exception ex)
            {
                HandleError(ex, "Error loading exposure values");
            }
            finally
            {
                VmIsBusy = false;
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
                VmIsBusy = true;
                ShowError = false;

                // Store the old values for comparison
                StoreOldValues();

                // Create base exposure triangle
                var baseExposure = new ExposureTriangleDto
                {
                    ShutterSpeed = OldShutterSpeed,
                    Aperture = OldFstop,
                    Iso = OldISO
                };

                // Apply EV compensation if needed
                ApplyEVCompensation(ref baseExposure);

                // Perform calculations based on what's fixed
                Task<Result<ExposureSettingsDto>> resultTask = null;

                switch (ToCalculate)
                {
                    case FixedValue.ShutterSpeeds:
                        resultTask = _exposureCalculatorService.CalculateShutterSpeedAsync(
                            baseExposure, FStopSelected, ISOSelected, FullHalfThirds);
                        break;

                    case FixedValue.Aperture:
                        resultTask = _exposureCalculatorService.CalculateApertureAsync(
                            baseExposure, ShutterSpeedSelected, ISOSelected, FullHalfThirds);
                        break;

                    case FixedValue.ISO:
                        resultTask = _exposureCalculatorService.CalculateIsoAsync(
                            baseExposure, ShutterSpeedSelected, FStopSelected, FullHalfThirds);
                        break;
                }

                if (resultTask != null)
                {
                    var result = resultTask.GetAwaiter().GetResult();

                    if (result.IsSuccess && result.Data != null)
                    {
                        // Update the results
                        ShutterSpeedResult = result.Data.ShutterSpeed;
                        FStopResult = result.Data.Aperture;
                        ISOResult = result.Data.Iso;
                    }
                    else
                    {
                        // Show error
                        ErrorMessage = result.ErrorMessage ?? "Calculation failed";
                    }
                }
            }
            catch (Exception ex)
            {
                HandleError(ex, "Error calculating exposure");
            }
            finally
            {
                VmIsBusy = false;
            }
        }

        private void StoreOldValues()
        {
            OldShutterSpeed = ShutterSpeedSelected;
            OldFstop = FStopSelected;
            OldISO = ISOSelected;
        }

        private void ApplyEVCompensation(ref ExposureTriangleDto exposure)
        {
            // This would adjust the base exposure based on EV compensation
            // For now, we're just using the base exposure as is
            // In a real implementation, you would modify the exposure values based on EVValue
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
                ErrorMessage = string.Empty;
            }
            catch (Exception ex)
            {
                HandleError(ex, "Error resetting exposure calculator");
            }
        }

        protected virtual void OnErrorOccurred(OperationErrorEventArgs e)
        {
            ErrorOccurred?.Invoke(this, e);
        }

        private void HandleError(Exception ex, string message)
        {
            ErrorMessage = $"{message}: {ex.Message}";
            OnErrorOccurred(new OperationErrorEventArgs(OperationErrorSource.Unknown, ErrorMessage, ex));
        }
        #endregion
    }
}