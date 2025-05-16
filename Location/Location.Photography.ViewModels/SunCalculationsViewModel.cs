using CommunityToolkit.Mvvm.Input;
using Location.Photography.Domain.Services;
using System.Windows.Input;

namespace Location.Photography.ViewModels
{
    public partial class SunCalculationsViewModel : ViewModelBase
    {
        #region Fields
        private readonly ISunCalculatorService _sunCalculatorService;
        private List<LocationViewModel> _locations = new List<LocationViewModel>();
        private LocationViewModel _selectedLocation;
        private double _latitude;
        private double _longitude;
        private DateTime _date = DateTime.Today;
        private string _dateFormat = "MM/dd/yyyy";
        private string _timeFormat = "hh:mm tt";

        private DateTime _sunrise = DateTime.Now;
        private DateTime _sunset = DateTime.Now;
        private DateTime _solarNoon = DateTime.Now;
        private DateTime _astronomicalDawn = DateTime.Now;
        private DateTime _nauticalDawn = DateTime.Now;
        private DateTime _nauticalDusk = DateTime.Now;
        private DateTime _astronomicalDusk = DateTime.Now;
        private DateTime _civilDawn = DateTime.Now;
        private DateTime _civilDusk = DateTime.Now;

        private string _locationPhoto = string.Empty;
        #endregion

        #region Properties
        public List<LocationViewModel> Locations
        {
            get => _locations;
            set => SetProperty(ref _locations, value);
        }

        public LocationViewModel SelectedLocation
        {
            get => _selectedLocation;
            set
            {
                if (SetProperty(ref _selectedLocation, value) && _selectedLocation != null)
                {
                    Latitude = _selectedLocation.Latitude;
                    Longitude = _selectedLocation.Longitude;
                    LocationPhoto = _selectedLocation.Photo;
                    CalculateSunAsync().ConfigureAwait(false);
                }
            }
        }

        public double Latitude
        {
            get => _latitude;
            set => SetProperty(ref _latitude, value);
        }

        public double Longitude
        {
            get => _longitude;
            set => SetProperty(ref _longitude, value);
        }

        public DateTime Date
        {
            get => _date;
            set
            {
                if (SetProperty(ref _date, value))
                {
                    CalculateSunAsync().ConfigureAwait(false);
                }
            }
        }

        public string DateFormat
        {
            get => _dateFormat;
            set => SetProperty(ref _dateFormat, value);
        }

        public string TimeFormat
        {
            get => _timeFormat;
            set
            {
                if (SetProperty(ref _timeFormat, value))
                {
                    OnPropertyChanged(nameof(SunriseFormatted));
                    OnPropertyChanged(nameof(SunsetFormatted));
                    OnPropertyChanged(nameof(SolarNoonFormatted));
                    OnPropertyChanged(nameof(GoldenHourMorningFormatted));
                    OnPropertyChanged(nameof(GoldenHourEveningFormatted));
                    OnPropertyChanged(nameof(AstronomicalDawnFormatted));
                    OnPropertyChanged(nameof(AstronomicalDuskFormatted));
                    OnPropertyChanged(nameof(NauticalDawnFormatted));
                    OnPropertyChanged(nameof(NauticalDuskFormatted));
                    OnPropertyChanged(nameof(CivilDawnFormatted));
                    OnPropertyChanged(nameof(CivilDuskFormatted));
                }
            }
        }

        public DateTime Sunrise
        {
            get => _sunrise;
            set
            {
                if (SetProperty(ref _sunrise, value))
                {
                    OnPropertyChanged(nameof(SunriseFormatted));
                    OnPropertyChanged(nameof(GoldenHourMorning));
                    OnPropertyChanged(nameof(GoldenHourMorningFormatted));
                }
            }
        }

        public DateTime Sunset
        {
            get => _sunset;
            set
            {
                if (SetProperty(ref _sunset, value))
                {
                    OnPropertyChanged(nameof(SunsetFormatted));
                    OnPropertyChanged(nameof(GoldenHourEvening));
                    OnPropertyChanged(nameof(GoldenHourEveningFormatted));
                }
            }
        }

        public DateTime SolarNoon
        {
            get => _solarNoon;
            set
            {
                if (SetProperty(ref _solarNoon, value))
                {
                    OnPropertyChanged(nameof(SolarNoonFormatted));
                }
            }
        }

        public DateTime AstronomicalDawn
        {
            get => _astronomicalDawn;
            set
            {
                if (SetProperty(ref _astronomicalDawn, value))
                {
                    OnPropertyChanged(nameof(AstronomicalDawnFormatted));
                }
            }
        }

        public DateTime AstronomicalDusk
        {
            get => _astronomicalDusk;
            set
            {
                if (SetProperty(ref _astronomicalDusk, value))
                {
                    OnPropertyChanged(nameof(AstronomicalDuskFormatted));
                }
            }
        }

        public DateTime NauticalDawn
        {
            get => _nauticalDawn;
            set
            {
                if (SetProperty(ref _nauticalDawn, value))
                {
                    OnPropertyChanged(nameof(NauticalDawnFormatted));
                }
            }
        }

        public DateTime NauticalDusk
        {
            get => _nauticalDusk;
            set
            {
                if (SetProperty(ref _nauticalDusk, value))
                {
                    OnPropertyChanged(nameof(NauticalDuskFormatted));
                }
            }
        }

        public DateTime CivilDawn
        {
            get => _civilDawn;
            set
            {
                if (SetProperty(ref _civilDawn, value))
                {
                    OnPropertyChanged(nameof(CivilDawnFormatted));
                }
            }
        }

        public DateTime CivilDusk
        {
            get => _civilDusk;
            set
            {
                if (SetProperty(ref _civilDusk, value))
                {
                    OnPropertyChanged(nameof(CivilDuskFormatted));
                }
            }
        }

        public DateTime GoldenHourMorning => Sunrise.AddHours(1);

        public DateTime GoldenHourEvening => Sunset.AddHours(-1);

        public string SunriseFormatted => Sunrise.ToString(TimeFormat);

        public string SunsetFormatted => Sunset.ToString(TimeFormat);

        public string SolarNoonFormatted => SolarNoon.ToString(TimeFormat);

        public string GoldenHourMorningFormatted => GoldenHourMorning.ToString(TimeFormat);

        public string GoldenHourEveningFormatted => GoldenHourEvening.ToString(TimeFormat);

        public string AstronomicalDawnFormatted => AstronomicalDawn.ToString(TimeFormat);

        public string AstronomicalDuskFormatted => AstronomicalDusk.ToString(TimeFormat);

        public string NauticalDawnFormatted => NauticalDawn.ToString(TimeFormat);

        public string NauticalDuskFormatted => NauticalDusk.ToString(TimeFormat);

        public string CivilDawnFormatted => CivilDawn.ToString(TimeFormat);

        public string CivilDuskFormatted => CivilDusk.ToString(TimeFormat);

        public string LocationPhoto
        {
            get => _locationPhoto;
            set => SetProperty(ref _locationPhoto, value);
        }
        #endregion

        #region Commands
        public ICommand LoadLocationsCommand { get; }
        public ICommand CalculateSunTimesCommand { get; }
        #endregion

        #region Constructor
        public SunCalculationsViewModel(ISunCalculatorService sunCalculatorService)
        {
            _sunCalculatorService = sunCalculatorService ?? throw new ArgumentNullException(nameof(sunCalculatorService));

            LoadLocationsCommand = new AsyncRelayCommand(LoadLocationsAsync);
            CalculateSunTimesCommand = new AsyncRelayCommand(CalculateSunAsync);
        }
        #endregion

        #region Methods
        private async Task CalculateSunAsync()
        {
            try
            {
                if (Latitude == 0 && Longitude == 0)
                {
                    return; // Do not calculate for default coordinates
                }

                IsBusy = true;
                ErrorMessage = string.Empty;

                // Calculate sun times directly using the service
                Sunrise = _sunCalculatorService.GetSunrise(Date, Latitude, Longitude);
                Sunset = _sunCalculatorService.GetSunset(Date, Latitude, Longitude);
                SolarNoon = _sunCalculatorService.GetSolarNoon(Date, Latitude, Longitude);
                AstronomicalDawn = _sunCalculatorService.GetAstronomicalDawn(Date, Latitude, Longitude);
                AstronomicalDusk = _sunCalculatorService.GetAstronomicalDusk(Date, Latitude, Longitude);
                NauticalDawn = _sunCalculatorService.GetNauticalDawn(Date, Latitude, Longitude);
                NauticalDusk = _sunCalculatorService.GetNauticalDusk(Date, Latitude, Longitude);
                CivilDawn = _sunCalculatorService.GetCivilDawn(Date, Latitude, Longitude);
                CivilDusk = _sunCalculatorService.GetCivilDusk(Date, Latitude, Longitude);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error calculating sun times: {ex.Message}";
                IsError = true;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadLocationsAsync()
        {
            try
            {
                IsBusy = true;
                ErrorMessage = string.Empty;

                // This is a placeholder for loading locations from a repository
                // In a real implementation, you would use a service or repository to load locations
                await Task.Delay(100); // Simulating data loading

                // Populate with sample data for now
                var locationViewModels = new List<LocationViewModel>
                {
                    new LocationViewModel
                    {
                        Id = 1,
                        Name = "Indianapolis",
                        Description = "Capital of Indiana",
                        Latitude = 39.7684,
                        Longitude = -86.1581,
                        Photo = "Resources/Images/indy.jpg"
                    },
                    new LocationViewModel
                    {
                        Id = 2,
                        Name = "Chicago",
                        Description = "Windy City",
                        Latitude = 41.8781,
                        Longitude = -87.6298,
                        Photo = "Resources/Images/chicago.jpg"
                    }
                };

                Locations = locationViewModels;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error loading locations: {ex.Message}";
                IsError = true;
            }
            finally
            {
                IsBusy = false;
            }
        }
        #endregion
    }

    // Simple LocationViewModel for SunCalculationsViewModel to use
    public class LocationViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Photo { get; set; }
    }
}