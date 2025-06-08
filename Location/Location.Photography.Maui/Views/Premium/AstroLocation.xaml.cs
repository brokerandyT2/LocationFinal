// Location.Photography.Maui/Views/Premium/AstroLocation.xaml.cs

using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Services;
using Location.Photography.Application.Services;
using Location.Photography.Domain.Services;
using Location.Photography.Infrastructure;
using Location.Photography.ViewModels;
using Location.Photography.ViewModels.Events;
using MediatR;
using System.Collections.ObjectModel;
using ISettingRepository = Location.Core.Application.Common.Interfaces.Persistence.ISettingRepository;

namespace Location.Photography.Maui.Views.Premium
{
    public partial class AstroLocation : ContentPage
    {
        private readonly IMediator _mediator;
        private readonly IAlertService _alertService;
        private readonly ILocationRepository _locationRepository;
        private readonly ISunCalculatorService _sunCalculatorService;
        private readonly ISettingRepository _settingRepository;
        private readonly ITimezoneService _timezoneService;
        private AstroLocationViewModel _viewModel;
        private string _timeFormat;
        private string _dateFormat;
        public AstroLocation(
            IMediator mediator,
            IAlertService alertService,
            ILocationRepository locationRepository,
            ISunCalculatorService sunCalculatorService,
            ISettingRepository settingRepository,
            IErrorDisplayService errorDisplayService,
            ITimezoneService timezoneService)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
            _locationRepository = locationRepository ?? throw new ArgumentNullException(nameof(locationRepository));
            _sunCalculatorService = sunCalculatorService ?? throw new ArgumentNullException(nameof(sunCalculatorService));
            _settingRepository = settingRepository ?? throw new ArgumentNullException(nameof(settingRepository));
            _timezoneService = timezoneService ?? throw new ArgumentNullException(nameof(timezoneService));

            InitializeComponent();
            InitializeViewModel(errorDisplayService);
        }

        // Simplified constructor for dependency injection scenarios
        public AstroLocation(
            IMediator mediator,
            ISunCalculatorService sunCalculatorService,
            IErrorDisplayService errorDisplayService,
            ITimezoneService timezoneService, ISettingRepository setting)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _sunCalculatorService = sunCalculatorService ?? throw new ArgumentNullException(nameof(sunCalculatorService));
            _timezoneService = timezoneService ?? throw new ArgumentNullException(nameof(timezoneService));

            InitializeComponent();

            var timeFormat = setting.GetByKeyAsync(MagicStrings.TimeFormat).Result.Value;
            var dateFormat = setting.GetByKeyAsync(MagicStrings.DateFormat).Result.Value;
            _viewModel = new AstroLocationViewModel(mediator, sunCalculatorService, errorDisplayService, timezoneService,dateFormat, timeFormat );
            BindingContext = _viewModel;
        }

        protected override async void OnNavigatedTo(NavigatedToEventArgs args)
        {
            base.OnNavigatedTo(args);

            try
            {
                // Load date/time format settings
                var dateFormatResult = await _settingRepository.GetByKeyAsync(MagicStrings.DateFormat);
                var timeFormatResult = await _settingRepository.GetByKeyAsync(MagicStrings.TimeFormat);

                if (!string.IsNullOrEmpty(dateFormatResult.Value))
                    datePicker.Format = dateFormatResult.Value;

                // Note: Event picker doesn't need time format as it shows event names
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting date/time formats: {ex.Message}");
            }
        }

        private void InitializeViewModel(IErrorDisplayService errorDisplayService)
        {
            try
            {
                _dateFormat =  _settingRepository.GetByKeyAsync(MagicStrings.DateFormat).Result.Value;
                _timeFormat = _settingRepository.GetByKeyAsync(MagicStrings.TimeFormat).Result.Value;
                _viewModel = new AstroLocationViewModel(_mediator, _sunCalculatorService, errorDisplayService, _timezoneService, _dateFormat, _timeFormat);
                _viewModel.BeginMonitoring = true;
                _viewModel.StartSensors();
                BindingContext = _viewModel;
                LoadLocationsAsync();
            }
            catch (Exception ex)
            {
                HandleError(ex, "Error initializing view model");
            }
        }

        private async void LoadLocationsAsync()
        {
            try
            {
                if (_viewModel == null) return;

                _viewModel.IsBusy = true;

                var result = await _locationRepository.GetAllAsync();

                if (result.IsSuccess && result.Data != null)
                {
                    var locationViewModels = result.Data.Select(l =>
                        new LocationViewModel()
                        {
                            Id = l.Id,
                            Name = l.Title,
                            Description = l.Description,
                            Lattitude = l.Coordinate.Latitude,
                            Longitude = l.Coordinate.Longitude,
                            Photo = l.PhotoPath
                        });

                    _viewModel.Locations = new ObservableCollection<LocationViewModel>(locationViewModels);

                    if (_viewModel.Locations.Count > 0)
                    {
                        locationPicker.SelectedIndex = 0;
                        var selectedLocation = _viewModel.Locations[0];
                        _viewModel.SelectedLocation = selectedLocation;
                    }
                }
                else
                {
                    _viewModel.ErrorMessage = result.ErrorMessage ?? "Failed to load locations";
                }
            }
            catch (Exception ex)
            {
                HandleError(ex, "Error loading locations");
            }
            finally
            {
                if (_viewModel != null)
                {
                    _viewModel.IsBusy = false;
                }
            }
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

                    _viewModel.BeginMonitoring = true;

                    if (_viewModel.Locations == null || _viewModel.Locations.Count == 0)
                    {
                        LoadLocationsAsync();
                    }
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

            try
            {
                if (_viewModel != null)
                {
                    _viewModel.ErrorOccurred -= OnSystemError;
                    _viewModel.BeginMonitoring = false;
                    _viewModel.StopSensors();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during page disappearing: {ex.Message}");
            }
        }

        private async void OnSystemError(object sender, OperationErrorEventArgs e)
        {
            var retry = await DisplayAlert("Error", $"{e.Message}. Try again?", "OK", "Cancel");
            if (retry && sender is AstroLocationViewModel viewModel)
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
                _viewModel.ErrorMessage = $"{message}: {ex.Message}";
                _viewModel.IsBusy = false;
            }
        }

        // Enhanced arrow positioning with time label
        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);

            // Update time label position relative to arrow
            UpdateTimeLabelPosition();
        }

        private void UpdateTimeLabelPosition()
        {
            try
            {
                if (_viewModel?.SelectedEvent != null)
                {
                    // Calculate position 10px from arrow point
                    // Arrow points up, so label should be above it
                    var arrowCenterX = arrow.Width / 2;
                    var arrowCenterY = arrow.Height / 2;

                    // Position label 10px above the arrow point (which points up)
                    var labelOffsetY = -85; // 75 (half arrow height) + 10 (offset)

                    timeLabel.Margin = new Thickness(0, labelOffsetY, 0, 0);

                    // Update target icon based on event type
                    UpdateTargetIcon();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating time label position: {ex.Message}");
            }
        }

        private void UpdateTargetIcon()
        {
            if (_viewModel?.SelectedEvent == null) return;

            var icon = _viewModel.SelectedEvent.Target switch
            {
                Photography.Domain.Models.AstroTarget.Moon => "🌙",
                Photography.Domain.Models.AstroTarget.Planets => "🪐",
                Photography.Domain.Models.AstroTarget.MilkyWayCore => "🌌",
                Photography.Domain.Models.AstroTarget.DeepSkyObjects => "⭐",
                Photography.Domain.Models.AstroTarget.MeteorShowers => "☄️",
                Photography.Domain.Models.AstroTarget.Comets => "☄️",
                Photography.Domain.Models.AstroTarget.Constellations => "✨",
                Photography.Domain.Models.AstroTarget.NorthernLights => "🌌",
                _ => "⭐"
            };

            //targetIcon.Text = icon;
        }

        // Event handlers for UI interactions
        private void OnLocationPickerSelectedIndexChanged(object sender, EventArgs e)
        {
            // ViewModel binding handles this automatically
            UpdateTimeLabelPosition();
        }

        private void OnDatePickerDateSelected(object sender, DateChangedEventArgs e)
        {
            // ViewModel binding handles this automatically
            UpdateTimeLabelPosition();
        }

        private void OnEventPickerSelectedIndexChanged(object sender, EventArgs e)
        {
            // ViewModel binding handles this automatically
            UpdateTimeLabelPosition();
        }

        // Handle property changes to update UI elements
        private void OnViewModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AstroLocationViewModel.SelectedEvent) ||
                e.PropertyName == nameof(AstroLocationViewModel.EventTimeLabel))
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    UpdateTimeLabelPosition();
                });
            }
        }
    }
}