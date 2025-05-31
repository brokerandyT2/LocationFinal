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
            AddHandlers();
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
            InitializeViewModel(exposureCalculatorService);
            AddHandlers();
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
            AddHandlers();
        }
        private void AddHandlers()
        {
            ShutterLockButton.ImageSource = "lockopen.png";
            IsoLockButton.ImageSource = "lockopen.png";
            ApertureLockButton.ImageSource = "lockopen.png";

            ShutterLockButton.Pressed += (s, e) => HandleClick("shutter");
            IsoLockButton.Pressed += (s, e) => HandleClick("iso");
            ApertureLockButton.Pressed += (s, e) => HandleClick("aperture");
            _viewModel.IsBusy = false;
        }

        private void HandleClick(string v)
        {
            if (v == "shutter")
            {

                ShutterSpeed_Picker.IsEnabled = !ShutterSpeed_Picker.IsEnabled;
                ShutterLockButton.ImageSource = ShutterSpeed_Picker.IsEnabled ? "lockopen.png" : "lock.png";

                IsoLockButton.ImageSource = "lockopen.png";
                ISO_Picker.IsEnabled = true;

                ApertureLockButton.ImageSource = "lockopen.png";
                fstop_Picker.IsEnabled = true;
            }
            else if (v == "iso")
            {
                ShutterLockButton.ImageSource = "lockopen.png";
                ShutterSpeed_Picker.IsEnabled = true;


                ISO_Picker.IsEnabled = !ISO_Picker.IsEnabled;
                IsoLockButton.ImageSource = ISO_Picker.IsEnabled ? "lockopen.png" : "lock.png";

                ApertureLockButton.ImageSource = "lockopen.png";
                fstop_Picker.IsEnabled = true;
            }
            else if (v == "aperture")
            {
                ShutterLockButton.ImageSource = "lockopen.png";
                ShutterSpeed_Picker.IsEnabled = true;

                IsoLockButton.ImageSource = "lockopen.png";
                ISO_Picker.IsEnabled = true;

                fstop_Picker.IsEnabled = !fstop_Picker.IsEnabled;
                ApertureLockButton.ImageSource = fstop_Picker.IsEnabled ? "lockopen.png" : "lock.png";

            }
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

                // Initialize target pickers to same values
                SyncTargetPickers();

                // Subscribe to lock state changes
                _viewModel.PropertyChanged += ViewModel_PropertyChanged;

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

                // Initialize target pickers to same values
                SyncTargetPickers();

                // Subscribe to lock state changes
                _viewModel.PropertyChanged += ViewModel_PropertyChanged;

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

        private void SyncTargetPickers()
        {
            if (_viewModel == null) return;

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
                HandleError(ex, "Error syncing target pickers");
            }
        }

        #region Event Handlers

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
                SyncTargetPickers();
                _viewModel.Calculate();
            }
            catch (Exception ex)
            {
                HandleError(ex, "Error changing exposure steps");
            }
        }

        private void PresetPicker_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_skipCalculations || _viewModel == null)
                return;

            try
            {
                // Preset application is handled in the ViewModel through binding
                // Just sync the target pickers after preset is applied
                SyncTargetPickers();
            }
            catch (Exception ex)
            {
                HandleError(ex, "Error selecting preset");
            }
        }

        #region Base Exposure Picker Events

        private void ShutterSpeed_Picker_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_skipCalculations || _viewModel == null)
                return;

            try
            {
                _viewModel.ShutterSpeedSelected = ShutterSpeed_Picker.SelectedItem?.ToString();

                // Update target picker if shutter is not locked
                if (!_viewModel.IsShutterLocked)
                {
                    TargetShutterSpeed_Picker.SelectedItem = _viewModel.ShutterSpeedSelected;
                }

                _viewModel.Calculate();
            }
            catch (Exception ex)
            {
                HandleError(ex, "Error selecting base shutter speed");
            }
        }

        private void fstop_Picker_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_skipCalculations || _viewModel == null)
                return;

            try
            {
                _viewModel.FStopSelected = fstop_Picker.SelectedItem?.ToString();

                // Update target picker if aperture is not locked
                if (!_viewModel.IsApertureLocked)
                {
                    TargetFstop_Picker.SelectedItem = _viewModel.FStopSelected;
                }

                _viewModel.Calculate();
            }
            catch (Exception ex)
            {
                HandleError(ex, "Error selecting base aperture");
            }
        }

        private void ISO_Picker_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_skipCalculations || _viewModel == null)
                return;

            try
            {
                _viewModel.ISOSelected = ISO_Picker.SelectedItem?.ToString();

                // Update target picker if ISO is not locked
                if (!_viewModel.IsIsoLocked)
                {
                    TargetISO_Picker.SelectedItem = _viewModel.ISOSelected;
                }

                _viewModel.Calculate();
            }
            catch (Exception ex)
            {
                HandleError(ex, "Error selecting base ISO");
            }
        }

        #endregion

        #region Target Picker Events

        private void TargetShutterSpeed_Picker_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_skipCalculations || _viewModel == null)
                return;

            try
            {
                string selectedValue = TargetShutterSpeed_Picker.SelectedItem?.ToString();
                if (!string.IsNullOrEmpty(selectedValue))
                {
                    _viewModel.ShutterSpeedSelected = selectedValue;

                    // Update base picker to match
                    ShutterSpeed_Picker.SelectedItem = selectedValue;

                    _viewModel.Calculate();
                }
            }
            catch (Exception ex)
            {
                HandleError(ex, "Error selecting target shutter speed");
            }
        }

        private void TargetFstop_Picker_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_skipCalculations || _viewModel == null)
                return;

            try
            {
                string selectedValue = TargetFstop_Picker.SelectedItem?.ToString();
                if (!string.IsNullOrEmpty(selectedValue))
                {
                    _viewModel.FStopSelected = selectedValue;

                    // Update base picker to match
                    fstop_Picker.SelectedItem = selectedValue;

                    _viewModel.Calculate();
                }
            }
            catch (Exception ex)
            {
                HandleError(ex, "Error selecting target aperture");
            }
        }

        private void TargetISO_Picker_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_skipCalculations || _viewModel == null)
                return;

            try
            {
                string selectedValue = TargetISO_Picker.SelectedItem?.ToString();
                if (!string.IsNullOrEmpty(selectedValue))
                {
                    _viewModel.ISOSelected = selectedValue;

                    // Update base picker to match
                    ISO_Picker.SelectedItem = selectedValue;

                    _viewModel.Calculate();
                }
            }
            catch (Exception ex)
            {
                HandleError(ex, "Error selecting target ISO");
            }
        }

        #endregion

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

        #endregion

        #region Helper Methods

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // Update lock button text when lock states change
            switch (e.PropertyName)
            {
                case nameof(ExposureCalculatorViewModel.IsShutterLocked):
                    if (ShutterLockButton != null)
                        ShutterLockButton.Text = _viewModel.IsShutterLocked ? "LOCK" : "OPEN";
                    break;
                case nameof(ExposureCalculatorViewModel.IsApertureLocked):
                    if (ApertureLockButton != null)
                        ApertureLockButton.Text = _viewModel.IsApertureLocked ? "LOCK" : "OPEN";
                    break;
                case nameof(ExposureCalculatorViewModel.IsIsoLocked):
                    if (IsoLockButton != null)
                        IsoLockButton.Text = _viewModel.IsIsoLocked ? "LOCK" : "OPEN";
                    break;
            }
        }

        private void PopulateViewModel()
        {
            if (_viewModel == null) return;

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

                // Sync target pickers
                SyncTargetPickers();
            }
            catch (Exception ex)
            {
                HandleError(ex, "Error populating view model");
            }
        }

        #endregion

        #region Lifecycle Events

        protected override void OnAppearing()
        {
            base.OnAppearing();

            try
            {
                if (_viewModel != null)
                {
                    _viewModel.ErrorOccurred -= OnSystemError;
                    _viewModel.ErrorOccurred += OnSystemError;

                    // Load presets when page appears
                    _viewModel.LoadPresetsCommand.Execute(null);
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
                _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
            }
        }

        #endregion

        #region Error Handling

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

        #endregion

        private void ShutterLockButton_Pressed(object sender, EventArgs e)
        {

        }
    }
}