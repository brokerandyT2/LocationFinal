// Location.Photography.Maui/Views/Premium/ExposureCalculator.xaml.cs
using Location.Core.Application.Services;
using Location.Photography.ViewModels;
using Location.Photography.Application.Services;
using Location.Photography.ViewModels.Events;
using Microsoft.Maui.Controls;
using MediatR;

namespace Location.Photography.Maui.Views.Premium
{
    public partial class ExposureCalculator : ContentPage
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
        public ExposureCalculator()
        {
            InitializeComponent();
            _viewModel = new ExposureCalculatorViewModel();
            _viewModel.CalculateCommand.Execute(_viewModel);
            BindingContext = _viewModel;
            CloseButton.IsVisible = false;
        }

        public ExposureCalculator(
            IExposureCalculatorService exposureCalculatorService,
            IAlertService alertService,
            IErrorDisplayService errorDisplayService, IMediator mediator)
        {
            _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
            _errorDisplayService = errorDisplayService ?? throw new ArgumentNullException(nameof(errorDisplayService));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            InitializeComponent();
            InitializeViewModel(exposureCalculatorService);
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
            InitializeViewModelFromTip(exposureCalculatorService, tipID);
        }

        private void InitializeViewModel(IExposureCalculatorService exposureCalculatorService)
        {
            try
            {
                _viewModel = new ExposureCalculatorViewModel(_mediator, exposureCalculatorService, _errorDisplayService);
                BindingContext = _viewModel;

                _skipCalculations = true;
                _viewModel.FullHalfThirds = Application.Services.ExposureIncrements.Full;
                SetupEVSlider();

                if (_viewModel.ShutterSpeedsForPicker?.Length > 0)
                    ShutterSpeed_Picker.SelectedIndex = 0;

                if (_viewModel.ApeaturesForPicker?.Length > 0)
                    fstop_Picker.SelectedIndex = 0;

                if (_viewModel.ISOsForPicker?.Length > 0)
                    ISO_Picker.SelectedIndex = 0;

                _viewModel.ShutterSpeedSelected = ShutterSpeed_Picker.SelectedItem?.ToString();
                _viewModel.FStopSelected = fstop_Picker.SelectedItem?.ToString();
                _viewModel.ISOSelected = ISO_Picker.SelectedItem?.ToString();

                _viewModel.OldShutterSpeed = _viewModel.ShutterSpeedSelected;
                _viewModel.OldFstop = _viewModel.FStopSelected;
                _viewModel.OldISO = _viewModel.ISOSelected;

                _viewModel.ToCalculate = Application.Services.FixedValue.ShutterSpeeds;
                _viewModel.EVValue = 0;

                _skipCalculations = false;
                _viewModel.Calculate();
            }
            catch (Exception ex)
            {
                HandleError(ex, "Error initializing exposure calculator");
            }
        }

        private void InitializeViewModelFromTip(IExposureCalculatorService exposureCalculatorService, int tipID)
        {
            try
            {
                _viewModel = new ExposureCalculatorViewModel(null, exposureCalculatorService, _errorDisplayService);
                BindingContext = _viewModel;

                _skipCalculations = true;
                _viewModel.FullHalfThirds = Application.Services.ExposureIncrements.Full;
                exposurefull.IsChecked = true;
                SetupEVSlider();

                if (_viewModel.ShutterSpeedsForPicker?.Length > 0)
                    ShutterSpeed_Picker.SelectedIndex = 0;

                if (_viewModel.ApeaturesForPicker?.Length > 0)
                    fstop_Picker.SelectedIndex = 0;

                if (_viewModel.ISOsForPicker?.Length > 0)
                    ISO_Picker.SelectedIndex = 0;

                _viewModel.ShutterSpeedSelected = ShutterSpeed_Picker.SelectedItem?.ToString();
                _viewModel.FStopSelected = fstop_Picker.SelectedItem?.ToString();
                _viewModel.ISOSelected = ISO_Picker.SelectedItem?.ToString();

                _viewModel.OldShutterSpeed = _viewModel.ShutterSpeedSelected;
                _viewModel.OldFstop = _viewModel.FStopSelected;
                _viewModel.OldISO = _viewModel.ISOSelected;

                _viewModel.ToCalculate = Application.Services.FixedValue.ShutterSpeeds;
                _viewModel.EVValue = 0;

                _skipCalculations = false;
                _viewModel.Calculate();
            }
            catch (Exception ex)
            {
                HandleError(ex, "Error initializing exposure calculator from tip");
            }
        }

        private void SetupEVSlider()
        {
            if (_skipCalculations || _viewModel == null)
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
                HandleError(ex, "Error setting up EV slider");
            }
        }

        private void exposuresteps_CheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            if (_skipCalculations || !e.Value || _viewModel == null)
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
                _viewModel.Calculate();
            }
            catch (Exception ex)
            {
                HandleError(ex, "Error changing exposure steps");
            }
        }

        private void ShutterSpeed_Picker_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_skipCalculations || _viewModel == null)
                return;

            try
            {
                _viewModel.SkipCalculation = Application.Services.FixedValue.ShutterSpeeds;
                _viewModel.ShutterSpeedSelected = ShutterSpeed_Picker.SelectedItem?.ToString();
                _viewModel.Calculate();
            }
            catch (Exception ex)
            {
                HandleError(ex, "Error selecting shutter speed");
            }
        }

        private void fstop_Picker_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_skipCalculations || _viewModel == null)
                return;

            try
            {
                _viewModel.SkipCalculation = Application.Services.FixedValue.Aperture;
                _viewModel.FStopSelected = fstop_Picker.SelectedItem?.ToString();
                _viewModel.Calculate();
            }
            catch (Exception ex)
            {
                HandleError(ex, "Error selecting aperture");
            }
        }

        private void ISO_Picker_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_skipCalculations || _viewModel == null)
                return;

            try
            {
                _viewModel.SkipCalculation = Application.Services.FixedValue.ISO;
                _viewModel.ISOSelected = ISO_Picker.SelectedItem?.ToString();
                _viewModel.Calculate();
            }
            catch (Exception ex)
            {
                HandleError(ex, "Error selecting ISO");
            }
        }

        private void EvSlider_ValueChanged(object sender, ValueChangedEventArgs e)
        {
            if (_skipCalculations || _viewModel == null)
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
                    _viewModel.Calculate();
                }
            }
            catch (Exception ex)
            {
                HandleError(ex, "Error changing EV value");
            }
        }

        private void CloseButton_Pressed(object sender, EventArgs e)
        {
            Navigation.PopModalAsync();
        }

        private void PopulateViewModel()
        {
            if (_viewModel == null) return;

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
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            try
            {
                if (_viewModel != null)
                {
                    _viewModel.ErrorOccurred -= OnSystemError;
                    _viewModel.ErrorOccurred += OnSystemError;
                }
            }
            catch (Exception ex)
            {
                HandleError(ex, "Error during page appearing");
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            if (_viewModel != null)
            {
                _viewModel.ErrorOccurred -= OnSystemError;
            }
        }

        private async void OnSystemError(object sender, OperationErrorEventArgs e)
        {
            var retry = await DisplayAlert("Error", $"{e.Message}. Try again?", "OK", "Cancel");
            if (retry && sender is ExposureCalculatorViewModel viewModel)
            {
                await viewModel.RetryLastCommandAsync();
            }
        }

        private void HandleError(Exception ex, string message)
        {
            System.Diagnostics.Debug.WriteLine($"Error: {message}. {ex.Message}");

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                if (_alertService != null)
                {
                    await _alertService.ShowErrorAlertAsync(message, "Error");
                }
                else
                {
                    await DisplayAlert("Error", message, "OK");
                }
            });

            if (_viewModel != null)
            {
                _viewModel.ShowError = true;
                _viewModel.ErrorMessage = $"{message}: {ex.Message}";
            }
        }
    }
}