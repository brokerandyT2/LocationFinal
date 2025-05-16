using Location.Core.Application.Services;
using Location.Photography.Application.Services;
using Location.Photography.Domain.Services;
using MediatR;
using OperationErrorEventArgs = Location.Photography.ViewModels.Events.OperationErrorEventArgs;

namespace Location.Photography.Maui.Views.Premium;

public partial class ExposureCalculator : ContentPage
{
    #region Fields

    private readonly IMediator _mediator;
    private readonly ISunCalculatorService _sunCalculatorService;
    private readonly IExposureCalculatorService _exposureCalculatorService;
    private readonly IAlertService _alertService;
    private bool _skipCalculations = true;
    private const double _fullStopEV = 1.0;
    private const double _halfStopEV = 0.5;
    private const double _thirdStopEV = 0.33;
    private double _currentEVStep = _fullStopEV;

    #endregion

    #region Constructors

    /// <summary>
    /// Default constructor for design-time and XAML preview
    /// </summary>
    public ExposureCalculator()
    {
        InitializeComponent();

        // Create a design-time view model for the preview
        var viewModel = new ExposureCalculatorViewModel();
        BindingContext = viewModel;

        CloseButton.IsVisible = false;
    }

    /// <summary>
    /// Main constructor with DI
    /// </summary>
    public ExposureCalculator(
        IMediator mediator,
        ISunCalculatorService sunCalculatorService,
        IExposureCalculatorService exposureCalculatorService,
        IAlertService alertService)
    {
        _mediator = mediator;
        _sunCalculatorService = sunCalculatorService;
        _exposureCalculatorService = exposureCalculatorService;
        _alertService = alertService;

        InitializeComponent();
        InitializeViewModel();
    }

    /// <summary>
    /// Constructor for use when coming from Tips
    /// </summary>
    public ExposureCalculator(
        IMediator mediator,
        ISunCalculatorService sunCalculatorService,
        IExposureCalculatorService exposureCalculatorService,
        IAlertService alertService,
        int tipID,
        bool isFromTips = false)
    {
        _mediator = mediator;
        _sunCalculatorService = sunCalculatorService;
        _exposureCalculatorService = exposureCalculatorService;
        _alertService = alertService;

        InitializeComponent();

        // Show the close button if opened from tips
        CloseButton.IsVisible = isFromTips;

        // Load tip data and initialize
        InitializeViewModelFromTip(tipID);
    }

    #endregion

    #region Initialization

    /// <summary>
    /// Initialize the view model with default values
    /// </summary>
    private void InitializeViewModel()
    {
        try
        {
            // Create and configure the view model
            var viewModel = new ExposureCalculatorViewModel(_mediator, _exposureCalculatorService);
            viewModel.ErrorOccurred += ViewModel_ErrorOccurred;
            BindingContext = viewModel;

            // Set initial values
            _skipCalculations = true;

            // Default to Full stop increments
            viewModel.FullHalfThirds = ExposureIncrements.Full;
            SetupEVSlider();

            // Select defaults from the available options
            if (viewModel.ShutterSpeedsForPicker?.Length > 0)
                ShutterSpeed_Picker.SelectedIndex = 0;

            if (viewModel.ApeaturesForPicker?.Length > 0)
                fstop_Picker.SelectedIndex = 0;

            if (viewModel.ISOsForPicker?.Length > 0)
                ISO_Picker.SelectedIndex = 0;

            // Store the initial values
            viewModel.ShutterSpeedSelected = ShutterSpeed_Picker.SelectedItem?.ToString();
            viewModel.FStopSelected = fstop_Picker.SelectedItem?.ToString();
            viewModel.ISOSelected = ISO_Picker.SelectedItem?.ToString();

            viewModel.OldShutterSpeed = viewModel.ShutterSpeedSelected;
            viewModel.OldFstop = viewModel.FStopSelected;
            viewModel.OldISO = viewModel.ISOSelected;

            // Default to calculating shutter speed
            viewModel.ToCalculate = FixedValue.ShutterSpeeds;
            ShutterSpeed_Picker.IsEnabled = false;
            fstop_Picker.IsEnabled = true;
            ISO_Picker.IsEnabled = true;

            // Set initial EV value
            viewModel.EVValue = 0;

            // Now enable calculations
            _skipCalculations = false;

            // Perform the initial calculation
            viewModel.Calculate();
        }
        catch (Exception ex)
        {
            HandleError(ex, "Error initializing exposure calculator");
        }
    }

    /// <summary>
    /// Initialize the view model from a tip
    /// </summary>
    private void InitializeViewModelFromTip(int tipID)
    {
        try
        {
            // Create the view model with tip data
            var viewModel = new ExposureCalculatorViewModel(_mediator, _exposureCalculatorService);
            viewModel.ErrorOccurred += ViewModel_ErrorOccurred;
            BindingContext = viewModel;

            // Default to Full stop increments
            _skipCalculations = true;
            viewModel.FullHalfThirds = ExposureIncrements.Full;
            exposurefull.IsChecked = true;
            SetupEVSlider();

            // Load the tip data - in a real implementation, this would use the mediator to fetch the tip
            // TODO: Implement the tip loading with CQRS pattern

            // Select defaults from the available options
            if (viewModel.ShutterSpeedsForPicker?.Length > 0)
                ShutterSpeed_Picker.SelectedIndex = 0;

            if (viewModel.ApeaturesForPicker?.Length > 0)
                fstop_Picker.SelectedIndex = 0;

            if (viewModel.ISOsForPicker?.Length > 0)
                ISO_Picker.SelectedIndex = 0;

            // Store the initial values
            viewModel.ShutterSpeedSelected = ShutterSpeed_Picker.SelectedItem?.ToString();
            viewModel.FStopSelected = fstop_Picker.SelectedItem?.ToString();
            viewModel.ISOSelected = ISO_Picker.SelectedItem?.ToString();

            viewModel.OldShutterSpeed = viewModel.ShutterSpeedSelected;
            viewModel.OldFstop = viewModel.FStopSelected;
            viewModel.OldISO = viewModel.ISOSelected;

            // Default to calculating shutter speed
            viewModel.ToCalculate = FixedValue.ShutterSpeeds;
            shutter.IsChecked = true;
            ShutterSpeed_Picker.IsEnabled = false;
            fstop_Picker.IsEnabled = true;
            ISO_Picker.IsEnabled = true;

            // Set initial EV value
            viewModel.EVValue = 0;

            // Now enable calculations
            _skipCalculations = false;

            // Perform the initial calculation
            viewModel.Calculate();
        }
        catch (Exception ex)
        {
            HandleError(ex, "Error initializing exposure calculator from tip");
        }
    }

    /// <summary>
    /// Set up the EV slider based on the current exposure increment
    /// </summary>
    private void SetupEVSlider()
    {
        if (_skipCalculations)
            return;

        try
        {
            // Get the view model
            var viewModel = (ExposureCalculatorViewModel)BindingContext;

            // Determine the step size based on the selected exposure increment
            switch (viewModel.FullHalfThirds)
            {
                case ExposureIncrements.Full:
                    _currentEVStep = _fullStopEV;
                    break;
                case ExposureIncrements.Half:
                    _currentEVStep = _halfStopEV;
                    break;
                case ExposureIncrements.Third:
                    _currentEVStep = _thirdStopEV;
                    break;
                default:
                    _currentEVStep = _fullStopEV;
                    break;
            }

            // Store the current value before adjusting the slider
            double currentValue = EvSlider.Value;

            // Round the current value to the nearest step
            double roundedValue = Math.Round(currentValue / _currentEVStep) * _currentEVStep;

            // Update the slider value if it's different from the rounded value
            if (Math.Abs(roundedValue - currentValue) > 0.001)
            {
                EvSlider.Value = roundedValue;
            }

            // Update the ViewModel with the EV value
            viewModel.EVValue = roundedValue;
        }
        catch (Exception ex)
        {
            HandleError(ex, "Error setting up EV slider");
        }
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Handle changes in the exposure steps (full, half, third)
    /// </summary>
    private void exposuresteps_CheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        if (_skipCalculations || !e.Value)
            return;

        try
        {
            // Get the sender as RadioButton
            RadioButton radioButton = (RadioButton)sender;

            // Get the view model
            var viewModel = (ExposureCalculatorViewModel)BindingContext;

            // Set the divisions based on the selected radio button
            if (radioButton == exposurefull)
                viewModel.FullHalfThirds = ExposureIncrements.Full;
            else if (radioButton == exposurehalfstop)
                viewModel.FullHalfThirds = ExposureIncrements.Half;
            else if (radioButton == exposurethirdstop)
                viewModel.FullHalfThirds = ExposureIncrements.Third;

            // Update the EV slider's step size
            SetupEVSlider();

            // Update the values from the pickers
            PopulateViewModel();

            // Recalculate
            viewModel.Calculate();
        }
        catch (Exception ex)
        {
            HandleError(ex, "Error changing exposure steps");
        }
    }

    /// <summary>
    /// Handle changes in what to calculate (shutter, aperture, ISO)
    /// </summary>
    private void calculate_CheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        if (_skipCalculations || !e.Value)
            return;

        try
        {
            // Get the sender as RadioButton
            RadioButton radioButton = (RadioButton)sender;

            // Get the view model
            var viewModel = (ExposureCalculatorViewModel)BindingContext;

            // Parse the selected value to determine what to calculate
            if (int.TryParse(radioButton.Value?.ToString(), out int value))
            {
                viewModel.ToCalculate = (FixedValue)value;

                // Enable/disable the appropriate pickers
                ShutterSpeed_Picker.IsEnabled = value != 0; // Enable if not calculating shutter
                fstop_Picker.IsEnabled = value != 3;        // Enable if not calculating aperture
                ISO_Picker.IsEnabled = value != 1;          // Enable if not calculating ISO

                // Update the values from the pickers
                PopulateViewModel();

                // Recalculate
                viewModel.Calculate();
            }
        }
        catch (Exception ex)
        {
            HandleError(ex, "Error changing calculation type");
        }
    }

    /// <summary>
    /// Handle changes in shutter speed selection
    /// </summary>
    private void ShutterSpeed_Picker_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (_skipCalculations)
            return;

        try
        {
            // Get the view model
            var viewModel = (ExposureCalculatorViewModel)BindingContext;

            // Store the old value
            viewModel.OldShutterSpeed = viewModel.ShutterSpeedSelected;

            // Set the new value
            viewModel.ShutterSpeedSelected = ShutterSpeed_Picker.SelectedItem?.ToString();

            // Recalculate
            viewModel.Calculate();
        }
        catch (Exception ex)
        {
            HandleError(ex, "Error selecting shutter speed");
        }
    }

    /// <summary>
    /// Handle changes in aperture selection
    /// </summary>
    private void fstop_Picker_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (_skipCalculations)
            return;

        try
        {
            // Get the view model
            var viewModel = (ExposureCalculatorViewModel)BindingContext;

            // Store the old value
            viewModel.OldFstop = viewModel.FStopSelected;

            // Set the new value
            viewModel.FStopSelected = fstop_Picker.SelectedItem?.ToString();

            // Recalculate
            viewModel.Calculate();
        }
        catch (Exception ex)
        {
            HandleError(ex, "Error selecting aperture");
        }
    }

    /// <summary>
    /// Handle changes in ISO selection
    /// </summary>
    private void ISO_Picker_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (_skipCalculations)
            return;

        try
        {
            // Get the view model
            var viewModel = (ExposureCalculatorViewModel)BindingContext;

            // Store the old value
            viewModel.OldISO = viewModel.ISOSelected;

            // Set the new value
            viewModel.ISOSelected = ISO_Picker.SelectedItem?.ToString();

            // Recalculate
            viewModel.Calculate();
        }
        catch (Exception ex)
        {
            HandleError(ex, "Error selecting ISO");
        }
    }

    /// <summary>
    /// Handle changes in the EV slider value
    /// </summary>
    private void EvSlider_ValueChanged(object sender, ValueChangedEventArgs e)
    {
        if (_skipCalculations)
            return;

        try
        {
            // Get the view model
            var viewModel = (ExposureCalculatorViewModel)BindingContext;

            // Calculate the nearest step value
            double newValue = Math.Round(e.NewValue / _currentEVStep) * _currentEVStep;

            // Only update if the value has actually changed by a step
            if (Math.Abs(newValue - viewModel.EVValue) >= _currentEVStep * 0.5)
            {
                // Round to avoid floating point precision issues
                newValue = Math.Round(newValue, 2);

                // Update the slider value if needed
                if (Math.Abs(EvSlider.Value - newValue) > 0.001)
                {
                    EvSlider.Value = newValue;
                }

                // Update the view model
                viewModel.EVValue = newValue;

                // Recalculate
                viewModel.Calculate();
            }
        }
        catch (Exception ex)
        {
            HandleError(ex, "Error changing EV value");
        }
    }

    /// <summary>
    /// Handle close button press when opened from Tips
    /// </summary>
    private void CloseButton_Pressed(object sender, EventArgs e)
    {
        Navigation.PopModalAsync();
    }

    /// <summary>
    /// Handle errors from the ViewModel
    /// </summary>
    private void ViewModel_ErrorOccurred(object sender, OperationErrorEventArgs e)
    {
        // Display error to user if it's not already displayed in the UI
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await _alertService.ShowErrorAlertAsync(e.Message, "Error");
        });
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Updates the view model with the current picker values
    /// </summary>
    private void PopulateViewModel()
    {
        if (BindingContext is ExposureCalculatorViewModel viewModel)
        {
            // Store the current values for comparison
            string oldShutter = viewModel.ShutterSpeedSelected;
            string oldFStop = viewModel.FStopSelected;
            string oldISO = viewModel.ISOSelected;

            // Update the view model with the current picker values
            if (ShutterSpeed_Picker.SelectedItem != null)
                viewModel.ShutterSpeedSelected = ShutterSpeed_Picker.SelectedItem.ToString();

            if (fstop_Picker.SelectedItem != null)
                viewModel.FStopSelected = fstop_Picker.SelectedItem.ToString();

            if (ISO_Picker.SelectedItem != null)
                viewModel.ISOSelected = ISO_Picker.SelectedItem.ToString();

            // Update the old values
            viewModel.OldShutterSpeed = oldShutter;
            viewModel.OldFstop = oldFStop;
            viewModel.OldISO = oldISO;
        }
    }

    #endregion

    #region Lifecycle Methods

    /// <summary>
    /// Called when the page appears
    /// </summary>
    protected override void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            // If the view model isn't initialized yet, initialize it
            if (BindingContext == null)
            {
                InitializeViewModel();
            }

            // Resubscribe to events
            if (BindingContext is ExposureCalculatorViewModel viewModel)
            {
                viewModel.ErrorOccurred -= ViewModel_ErrorOccurred;
                viewModel.ErrorOccurred += ViewModel_ErrorOccurred;
            }
        }
        catch (Exception ex)
        {
            HandleError(ex, "Error during page appearing");
        }
    }

    /// <summary>
    /// Called when the page disappears
    /// </summary>
    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        // Unsubscribe from events
        if (BindingContext is ExposureCalculatorViewModel viewModel)
        {
            viewModel.ErrorOccurred -= ViewModel_ErrorOccurred;
        }
    }

    #endregion

    #region Error Handling

    /// <summary>
    /// Handle errors during view operations
    /// </summary>
    private void HandleError(Exception ex, string message)
    {
        // Log the error
        System.Diagnostics.Debug.WriteLine($"Error: {message}. {ex.Message}");

        // Display alert to user
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await _alertService.ShowErrorAlertAsync(message, "Error");
        });

        // Pass the error to the ViewModel if available
        if (BindingContext is ExposureCalculatorViewModel viewModel)
        {
            viewModel.ShowError = true;
            viewModel.ErrorMessage = $"{message}: {ex.Message}";

            // Since we're using the error label in the XAML, make sure it's updated
            if (errorLabel != null)
            {
                errorLabel.Text = $"{message}: {ex.Message}";
            }
        }
    }

    #endregion
}

// These enums are needed for the UI code-behind to work with the ViewModel
// They should be in the ViewModel but are included here for completeness
public enum ExposureIncrements
{
    Full,
    Half,
    Third
}

public enum FixedValue
{
    ShutterSpeeds = 0,
    ISO = 1,
    Empty = 2,
    Aperture = 3
}