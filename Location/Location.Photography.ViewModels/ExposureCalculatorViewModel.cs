using Location.Photography.Application.Services;
using Location.Photography.ViewModels;
using Location.Photography.ViewModels.Events;
using MediatR;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OperationErrorEventArgs = Location.Photography.ViewModels.Events.OperationErrorEventArgs;

namespace Location.Photography.Maui.Views.Premium
{
    public class ExposureCalculatorViewModel : ViewModelBase
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
        private bool _showError;
        private string _errorMessage;
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

        public new string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                if (SetProperty(ref _errorMessage, value))
                {
                    ShowError = !string.IsNullOrEmpty(value);
                    base.ErrorMessage = value;
                }
            }
        }
        #endregion

        #region Commands
        public ICommand CalculateCommand { get; }
        public ICommand ResetCommand { get; }
        #endregion

        #region Constructor
        public ExposureCalculatorViewModel()
        {
            // Design-time constructor
            _shutterSpeedsForPicker = new string[] { "1/1000", "1/500", "1/250", "1/125", "1/60", "1/30", "1/15", "1/8", "1/4", "1/2", "1" };
            _apeaturesForPicker = new string[] { "f/1.4", "f/2", "f/2.8", "f/4", "f/5.6", "f/8", "f/11", "f/16", "f/22" };
            _isosForPicker = new string[] { "100", "200", "400", "800", "1600", "3200", "6400" };

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

            // Load initial picker values
            LoadPickerValuesAsync().ConfigureAwait(false);
        }
        #endregion

        #region Methods
        private async Task LoadPickerValuesAsync()
        {
            try
            {
                // Use the IExposureCalculatorService to get the values based on the current increment
                var shutterSpeedsResult = await _exposureCalculatorService.GetShutterSpeedsAsync(FullHalfThirds);
                if (shutterSpeedsResult.IsSuccess && shutterSpeedsResult.Data != null)
                {
                    ShutterSpeedsForPicker = shutterSpeedsResult.Data;
                }

                var aperturesResult = await _exposureCalculatorService.GetAperturesAsync(FullHalfThirds);
                if (aperturesResult.IsSuccess && aperturesResult.Data != null)
                {
                    ApeaturesForPicker = aperturesResult.Data;
                }

                var isosResult = await _exposureCalculatorService.GetIsosAsync(FullHalfThirds);
                if (isosResult.IsSuccess && isosResult.Data != null)
                {
                    ISOsForPicker = isosResult.Data;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error loading exposure values: {ex.Message}";
                OnErrorOccurred(new OperationErrorEventArgs(
                    OperationErrorSource.Unknown,
                    ErrorMessage,
                    ex));
            }
        }

        /// <summary>
        /// Performs exposure calculation based on the selected values and fixed parameter
        /// </summary>
        public void Calculate()
        {
            try
            {
                IsBusy = true;
                ShowError = false;
                ErrorMessage = string.Empty;

                // Prepare the base exposure triangle
                var baseExposure = new ExposureTriangleDto
                {
                    ShutterSpeed = ShutterSpeedSelected,
                    Aperture = FStopSelected,
                    Iso = ISOSelected
                };

                // Apply EV adjustment (if any)
                // TODO: Apply the EV value to the calculation

                // Perform calculation based on what's fixed
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
                    var result = resultTask.Result;

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
                ErrorMessage = $"Error calculating exposure: {ex.Message}";
                OnErrorOccurred(new OperationErrorEventArgs(
                    OperationErrorSource.Unknown,
                    ErrorMessage,
                    ex));
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Reset to default values
        /// </summary>
        public void Reset()
        {
            try
            {
                if (ShutterSpeedsForPicker?.Length > 0)
                    ShutterSpeedSelected = ShutterSpeedsForPicker[0];

                if (ApeaturesForPicker?.Length > 0)
                    FStopSelected = ApeaturesForPicker[0];

                if (ISOsForPicker?.Length > 0)
                    ISOSelected = ISOsForPicker[0];

                EVValue = 0;
                ToCalculate = FixedValue.ShutterSpeeds;
                FullHalfThirds = ExposureIncrements.Full;

                Calculate();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error resetting exposure calculator: {ex.Message}";
                OnErrorOccurred(new OperationErrorEventArgs(
                    OperationErrorSource.Unknown,
                    ErrorMessage,
                    ex));
            }
        }
        #endregion
    }
}