// Location.Photography.Maui/Views/Premium/SunLocation.xaml.cs
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Services;
using Location.Photography.Application.Queries.SunLocation;
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
    public partial class SunLocation : ContentPage
    {
        private readonly IMediator _mediator;
        private readonly IAlertService _alertService;
        private readonly ILocationRepository _locationRepository;
        private readonly ISunCalculatorService _sunCalculatorService;
        private readonly ISettingRepository _settingRepository;
        private readonly ITimezoneService _timezoneService;
        private readonly IExposureCalculatorService _exposureCalculatorService;
        private SunLocationViewModel _viewModel;

        public SunLocation(IMediator mediator, ISunCalculatorService sunCalculatorService, IErrorDisplayService error, ITimezoneService time, IExposureCalculatorService exposureCalculatorService)
        {
            _exposureCalculatorService = exposureCalculatorService ?? throw new ArgumentNullException(nameof(exposureCalculatorService));
            InitializeComponent();
            _viewModel = new SunLocationViewModel(mediator, sunCalculatorService, error, time, exposureCalculatorService);
            BindingContext = _viewModel;
        }


        public SunLocation(
            IMediator mediator,
            IAlertService alertService,
            ILocationRepository locationRepository,
            ISunCalculatorService sunCalculatorService,
            ISettingRepository settingRepository,
            IErrorDisplayService errorDisplayService,
            ITimezoneService timezoneService, IExposureCalculatorService exposureCalculatorService)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
            _locationRepository = locationRepository ?? throw new ArgumentNullException(nameof(locationRepository));
            _sunCalculatorService = sunCalculatorService ?? throw new ArgumentNullException(nameof(sunCalculatorService));
            _settingRepository = settingRepository ?? throw new ArgumentNullException(nameof(settingRepository));
            _timezoneService = timezoneService ?? throw new ArgumentNullException(nameof(timezoneService));
            _exposureCalculatorService = exposureCalculatorService ?? throw new ArgumentNullException(nameof(exposureCalculatorService));
            InitializeComponent();
            InitializeViewModel(errorDisplayService);
        }

        private async void OnTimelineEventTapped(object sender, EventArgs e)
        {
            if (sender is StackLayout stackLayout && stackLayout.BindingContext is TimelineEventViewModel timelineEvent)
            {
                if (_viewModel != null)
                {
                    // Update the selected date and time to match the timeline event
                    _viewModel.SelectedDate = timelineEvent.EventTime.Date;
                    _viewModel.SelectedTime = timelineEvent.EventTime.TimeOfDay;
                }
            }
        }

        protected override async void OnNavigatedTo(NavigatedToEventArgs args)
        {
            base.OnNavigatedTo(args);

            try
            {
                var dateFormatResult = await _settingRepository.GetByKeyAsync(MagicStrings.DateFormat);
                var timeFormatResult = await _settingRepository.GetByKeyAsync(MagicStrings.TimeFormat);

                if (!string.IsNullOrEmpty(dateFormatResult.Value))
                    date.Format = dateFormatResult.Value;

                if (!string.IsNullOrEmpty(timeFormatResult.Value))
                    time.Format = timeFormatResult.Value;
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
                _viewModel = new SunLocationViewModel(_mediator, _sunCalculatorService, errorDisplayService, _timezoneService, _exposureCalculatorService);
                _viewModel.BeginMonitoring = true;
                _viewModel.StartSensors();
                _viewModel.UpdateSunPositionCommand.Execute(errorDisplayService);
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

                        await UpdateSunPositionAsync(_viewModel);
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

        private async Task UpdateSunPositionAsync(SunLocationViewModel viewModel)
        {
            try
            {
                if (viewModel.SelectedLocation == null)
                    return;

                var query = new GetCurrentSunPositionQuery
                {
                    Latitude = viewModel.SelectedLocation.Lattitude,
                    Longitude = viewModel.SelectedLocation.Longitude,
                    DateTime = viewModel.SelectedDate.Date.Add(viewModel.SelectedTime)
                };

                var result = await _mediator.Send(query);

                if (result.IsSuccess && result.Data != null)
                {
                    // The ViewModel will handle updating sun direction through compass integration
                    viewModel.SunElevation = result.Data.Elevation;
                }
                else
                {
                    viewModel.ErrorMessage = result.ErrorMessage ?? "Failed to calculate sun position";
                }
            }
            catch (Exception ex)
            {
                HandleError(ex, "Error updating sun position");
            }
        }

        private async void locationPicker_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (locationPicker.SelectedItem is LocationViewModel selectedLocation && _viewModel != null)
            {
                _viewModel.SelectedLocation = selectedLocation;

                await UpdateSunPositionAsync(_viewModel);
            }
        }

        private async void date_DateSelected(object sender, DateChangedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.SelectedDate = e.NewDate;
                await UpdateSunPositionAsync(_viewModel);
            }
        }

        private async void time_TimeSelected(object sender, TimeChangedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.SelectedTime = e.NewTime;
                await UpdateSunPositionAsync(_viewModel);
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
                    else
                    {
                        _ = UpdateSunPositionAsync(_viewModel);
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
            if (retry && sender is SunLocationViewModel viewModel)
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
    }
}