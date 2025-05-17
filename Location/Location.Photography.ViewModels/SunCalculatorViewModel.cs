using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Location.Core.Application.Locations.DTOs;
using Location.Core.ViewModels;
using Location.Photography.Application.Services;
using Location.Photography.ViewModels.Events;
using Microsoft.VisualBasic;
using System.Diagnostics;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Location.Photography.ViewModels.Premium
{
    public partial class SunCalculatorViewModel : BaseViewModel
    {
        private readonly ILocationService _locationService;
        private readonly ISunService _sunService;
        private readonly ISettingService _settingService;

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private string _errorMessage;

        [ObservableProperty]
        private List<LocationDto> _locations;

        [ObservableProperty]
        private LocationDto _selectedLocation;

        [ObservableProperty]
        private DateTime _date;

        [ObservableProperty]
        private string _locationPhoto;

        [ObservableProperty]
        private string _dateFormat;

        [ObservableProperty]
        private string _timeFormat;

        [ObservableProperty]
        private string _astronomicalDawnFormatted;

        [ObservableProperty]
        private string _nauticalDawnFormatted;

        [ObservableProperty]
        private string _civilDawnFormatted;

        [ObservableProperty]
        private string _sunRiseFormatted;

        [ObservableProperty]
        private string _goldenHourMorningFormatted;

        [ObservableProperty]
        private string _solarNoonFormatted;

        [ObservableProperty]
        private string _goldenHourEveningFormatted;

        [ObservableProperty]
        private string _sunSetFormatted;

        [ObservableProperty]
        private string _civilDuskFormatted;

        [ObservableProperty]
        private string _nauticalDuskFormatted;

        [ObservableProperty]
        private string _astronomicalDuskFormatted;

        public bool HasErrorMessage => !string.IsNullOrEmpty(ErrorMessage);

        public event EventHandler<OperationErrorEventArgs> ErrorOccurred;

        public SunCalculatorViewModel(
            ILocationService locationService,
            ISunService sunService,
            ISettingService settingService)
        {
            _locationService = locationService ?? throw new ArgumentNullException(nameof(locationService));
            _sunService = sunService ?? throw new ArgumentNullException(nameof(sunService));
            _settingService = settingService ?? throw new ArgumentNullException(nameof(settingService));

            Date = DateTime.Today;
            Locations = new List<LocationDto>();
        }

        public async Task InitializeAsync()
        {
            try
            {
                IsBusy = true;
                ErrorMessage = string.Empty;

                // Load format settings
                await LoadSettingsAsync();

                // Load locations
                await LoadLocationsAsync();

                // Calculate sun times
                if (SelectedLocation != null)
                {
                    await CalculateSunTimesAsync();
                }
            }
            catch (Exception ex)
            {
                HandleError(ex, "Failed to initialize");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadSettingsAsync()
        {
            try
            {
                var dateFormatSetting = await _settingService.GetSettingByNameAsync("DateFormat");
                var timeFormatSetting = await _settingService.GetSettingByNameAsync("TimeFormat");

                DateFormat = dateFormatSetting?.Value ?? "MM/dd/yyyy";
                TimeFormat = timeFormatSetting?.Value ?? "hh:mm tt";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading settings: {ex.Message}");
                // Use default formats
                DateFormat = "MM/dd/yyyy";
                TimeFormat = "hh:mm tt";
            }
        }

        private async Task LoadLocationsAsync()
        {
            try
            {
                var locationsResult = await _locationService.GetLocationsAsync();

                if (locationsResult.IsSuccess && locationsResult.Data != null)
                {
                    Locations = locationsResult.Data;

                    if (Locations.Count > 0)
                    {
                        SelectedLocation = Locations[0];
                        LocationPhoto = SelectedLocation.PhotoPath;
                    }
                    else
                    {
                        ErrorMessage = "No locations found. Please add locations first.";
                    }
                }
                else
                {
                    ErrorMessage = locationsResult.ErrorMessage ?? "Failed to load locations";
                }
            }
            catch (Exception ex)
            {
                HandleError(ex, "Error loading locations");
            }
        }

        [RelayCommand]
        public async Task UpdateCalculations()
        {
            if (SelectedLocation == null)
                return;

            await CalculateSunTimesAsync();
        }

        private async Task CalculateSunTimesAsync()
        {
            if (SelectedLocation == null)
                return;

            try
            {
                IsBusy = true;
                ErrorMessage = string.Empty;

                var result = await _sunService.GetSunTimesAsync(
                    SelectedLocation.Latitude,
                    SelectedLocation.Longitude,
                    Date);

                if (result.IsSuccess && result.Data != null)
                {
                    var sunTimes = result.Data;

                    // Format and display all sun times
                    AstronomicalDawnFormatted = FormatDateTime(sunTimes.AstronomicalDawn);
                    NauticalDawnFormatted = FormatDateTime(sunTimes.NauticalDawn);
                    CivilDawnFormatted = FormatDateTime(sunTimes.CivilDawn);
                    SunRiseFormatted = FormatDateTime(sunTimes.Sunrise);
                    GoldenHourMorningFormatted = FormatDateTime(sunTimes.GoldenHourMorningEnd);
                    SolarNoonFormatted = FormatDateTime(sunTimes.SolarNoon);
                    GoldenHourEveningFormatted = FormatDateTime(sunTimes.GoldenHourEveningStart);
                    SunSetFormatted = FormatDateTime(sunTimes.Sunset);
                    CivilDuskFormatted = FormatDateTime(sunTimes.CivilDusk);
                    NauticalDuskFormatted = FormatDateTime(sunTimes.NauticalDusk);
                    AstronomicalDuskFormatted = FormatDateTime(sunTimes.AstronomicalDusk);

                    // Update location photo if available
                    LocationPhoto = SelectedLocation.PhotoPath;
                }
                else
                {
                    ErrorMessage = result.ErrorMessage ?? "Failed to calculate sun times";
                }
            }
            catch (Exception ex)
            {
                HandleError(ex, "Error calculating sun times");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private string FormatDateTime(DateTime dateTime)
        {
            return dateTime.ToString(TimeFormat);
        }

        private void HandleError(Exception ex, string message)
        {
            Debug.WriteLine($"{message}: {ex}");
            ErrorMessage = $"{message}: {ex.Message}";
            ErrorOccurred?.Invoke(this, new OperationErrorEventArgs(ex.Message));
        }
    }
}