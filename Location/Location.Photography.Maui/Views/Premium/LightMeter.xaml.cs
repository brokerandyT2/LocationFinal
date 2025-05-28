using Location.Core.Application.Common.Interfaces.Persistence;
using Location.Core.Application.Services;
using Location.Photography.Maui.Controls;

#if ANDROID
using Location.Photography.Maui.Platforms.Android;
#endif
using Location.Photography.ViewModels;
using Location.Photography.ViewModels.Events;
using MediatR;

namespace Location.Photography.Maui.Views.Premium
{
    public partial class LightMeter : ContentPage
    {
        private IServiceProvider _serviceProvider;
        private readonly IMediator _mediator;
        private readonly IAlertService _alertService;
        private readonly ISettingRepository _settingRepository;
#if ANDROID
        private readonly ILightSensorService _lightSensorService;
#endif
        private LightMeterViewModel _viewModel;
        private LunaProDrawable _lightMeterDrawable;

        public LightMeter()
        {

            ///FUCK.  Wrong Constructor is being called.
            ///
            InitializeComponent();
            _viewModel = new LightMeterViewModel();
            BindingContext = _viewModel;
            InitializeLightMeter();
        }
#if ANDROID

        
        public LightMeter(
            IMediator mediator,
            IAlertService alertService,
            ISettingRepository settingRepository,
            ILightSensorService lightSensorService, IServiceProvider serviceProvider)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
            _settingRepository = settingRepository ?? throw new ArgumentNullException(nameof(settingRepository));
            _lightSensorService = lightSensorService ?? throw new ArgumentNullException(nameof(lightSensorService));
            _serviceProvider = serviceProvider;
            InitializeComponent();
            InitializeLightMeter();
        }
#endif
        private void InitializeLightMeter()
        {
            // Create and configure the light meter drawable
            _lightMeterDrawable = new LunaProDrawable(LightMeterGraphicsView);

            // Set up event handlers for interactions
            _lightMeterDrawable.InteractionStarted += OnLightMeterInteractionStarted;
            _lightMeterDrawable.InteractionEnded += OnLightMeterInteractionEnded;

            // Assign the drawable to the GraphicsView
            LightMeterGraphicsView.Drawable = _lightMeterDrawable;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            InitializeViewModel();
            StartLightSensor();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            StopLightSensor();

            if (_viewModel != null)
            {
                _viewModel.ErrorOccurred -= OnSystemError;
            }

            if (_lightMeterDrawable != null)
            {
                _lightMeterDrawable.InteractionStarted -= OnLightMeterInteractionStarted;
                _lightMeterDrawable.InteractionEnded -= OnLightMeterInteractionEnded;
            }
        }

        private async void InitializeViewModel()
        {
            _viewModel = new LightMeterViewModel()
            {
                IsBusy = true
            };

            BindingContext = _viewModel;
            _viewModel.ErrorOccurred -= OnSystemError;
            _viewModel.ErrorOccurred += OnSystemError;

            try
            {
                // Initialize any settings or data needed for the light meter
                await Task.Delay(500); // Simulate initialization

                // TODO: Load user preferences for exposure calculation settings
                // TODO: Initialize with saved ISO, aperture, shutter speed settings

            }
            catch (Exception ex)
            {
                _viewModel.ErrorMessage = $"Error initializing light meter: {ex.Message}";
                _viewModel.IsError = true;
                System.Diagnostics.Debug.WriteLine($"Light meter initialization error: {ex}");
            }
            finally
            {
                _viewModel.IsBusy = false;
            }
        }

        private void StartLightSensor()
        {
            try
            {
#if ANDROID
                _lightSensorService?.StartListening();
#endif
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error starting light sensor: {ex.Message}");
                // TODO: Show user-friendly message about sensor not available
            }
        }

        private void StopLightSensor()
        {
            try
            {
#if ANDROID
                _lightSensorService?.StopListening();
#endif
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error stopping light sensor: {ex.Message}");
            }
        }

        private void OnLightMeterInteractionStarted(object sender, EventArgs e)
        {
            // Handle when user starts interacting with the light meter dials
            // This could be used to pause sensor updates or provide haptic feedback
            System.Diagnostics.Debug.WriteLine("Light meter interaction started");

            // TODO: Notify ViewModel that user is interacting with dials
            if (_viewModel != null)
            {
                // _viewModel.IsUserInteracting = true;
            }
        }

        private void OnLightMeterInteractionEnded(object sender, EventArgs e)
        {
            // Handle when user finishes interacting with the light meter dials
            System.Diagnostics.Debug.WriteLine("Light meter interaction ended");

            if (_lightMeterDrawable != null)
            {
                // Get the current selected values from the light meter
                var selectedValues = _lightMeterDrawable.SelectedValues;
                System.Diagnostics.Debug.WriteLine($"Selected: ASA={selectedValues.Asa}, Shutter={selectedValues.ShutterSpeed}, F-Stop={selectedValues.FStop}");

                // TODO: Update ViewModel with new exposure settings
                // TODO: Recalculate exposure based on current light sensor reading
                // TODO: Update any exposure calculations or recommendations
            }

            // TODO: Notify ViewModel that user interaction is complete
            if (_viewModel != null)
            {
                // _viewModel.IsUserInteracting = false;
                // _viewModel.UpdateExposureCalculation(selectedValues);
            }
        }

        private async void OnSystemError(object sender, OperationErrorEventArgs e)
        {
            var retry = await DisplayAlert("Error", $"{e.Message}. Try again?", "OK", "Cancel");
            if (retry && sender is LightMeterViewModel viewModel)
            {
                await viewModel.RetryLastCommandAsync();
            }
        }

        private async void ImageButton_Pressed(object sender, EventArgs e)
        {
            try
            {
                Navigation.PopModalAsync();
            
            }
            catch (Exception ex)
            {
                
                //_mediator.Send();
            }
        }
    }
}