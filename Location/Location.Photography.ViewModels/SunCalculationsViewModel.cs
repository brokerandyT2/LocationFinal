// Location.Photography.Shared.ViewModels/SunCalculationsViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Innovative.SolarCalculator;

using Location.Core.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Location.Photography.ViewModels
{
    public partial class SunCalculationsViewModel : ViewModelBase, ISunCalculations
    {
        private readonly ISunCalculator _sunCalculatorService;

        #region Fields
        private List<LocationViewModel> _locations = new List<LocationViewModel>();
        private LocationViewModel _selectedLocation;
        private double _latitude;
        private double _longitude;
        private DateTime _date = DateTime.Today;
        private string _dateFormat = "MM/dd/yyyy";
        private string _timeFormat = "hh:mm tt";

        private DateTime _sunrise = DateTime.Now;
        private DateTime _sunset = DateTime.Now;
        private DateTime _solarnoon = DateTime.Now;
        private DateTime _astronomicalDawn = DateTime.Now;
        private DateTime _nauticaldawn = DateTime.Now;
        private DateTime _nauticaldusk = DateTime.Now;
        private DateTime _astronomicalDusk = DateTime.Now;
        private DateTime _civildawn = DateTime.Now;
        private DateTime _civildusk = DateTime.Now;

        private string _locationPhoto = string.Empty;
        #endregion

        #region Properties
        public List<LocationViewModel> LocationsS
        {
            get => _locations;
            set
            {
                _locations = value;
                OnPropertyChanged();
            }
        }

        public LocationViewModel SelectedLocation
        {
            get => _selectedLocation;
            set
            {
                if (_selectedLocation != value)
                {
                    _selectedLocation = value;
                    if (_selectedLocation != null)
                    {
                        Latitude = _selectedLocation.Lattitude;
                        Longitude = _selectedLocation.Longitude;
                        LocationPhoto = _selectedLocation.Photo;
                        CalculateSun();
                    }
                    OnPropertyChanged();
                }
            }
        }

        public double Latitude
        {
            get => _latitude;
            set
            {
                _latitude = value;
                OnPropertyChanged();
            }
        }

        public double Longitude
        {
            get => _longitude;
            set
            {
                _longitude = value;
                OnPropertyChanged();
            }
        }

        public DateTime Date
        {
            get => _date;
            set
            {
                _date = value;
                OnPropertyChanged();
                CalculateSun();
            }
        }

        public string DateFormat
        {
            get => _dateFormat;
            set
            {
                _dateFormat = value;
                OnPropertyChanged();
            }
        }

        public string TimeFormat
        {
            get => _timeFormat;
            set
            {
                _timeFormat = value;
                OnPropertyChanged();
                // Update all formatted time strings
                OnPropertyChanged(nameof(SunRiseFormatted));
                OnPropertyChanged(nameof(SunSetFormatted));
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

        public DateTime Sunrise
        {
            get => _sunrise;
            set
            {
                _sunrise = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SunRiseFormatted));
                OnPropertyChanged(nameof(GoldenHourMorning));
                OnPropertyChanged(nameof(GoldenHourMorningFormatted));
            }
        }

        public DateTime Sunset
        {
            get => _sunset;
            set
            {
                _sunset = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SunSetFormatted));
                OnPropertyChanged(nameof(GoldenHourEvening));
                OnPropertyChanged(nameof(GoldenHourEveningFormatted));
            }
        }

        public DateTime SolarNoon
        {
            get => _solarnoon;
            set
            {
                _solarnoon = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SolarNoonFormatted));
            }
        }

        public DateTime AstronomicalDawn
        {
            get => _astronomicalDawn;
            set
            {
                _astronomicalDawn = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(AstronomicalDawnFormatted));
            }
        }

        public DateTime AstronomicalDusk
        {
            get => _astronomicalDusk;
            set
            {
                _astronomicalDusk = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(AstronomicalDuskFormatted));
            }
        }

        public DateTime NauticalDawn
        {
            get => _nauticaldawn;
            set
            {
                _nauticaldawn = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(NauticalDawnFormatted));
            }
        }

        public DateTime NauticalDusk
        {
            get => _nauticaldusk;
            set
            {
                _nauticaldusk = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(NauticalDuskFormatted));
            }
        }

        public DateTime Civildawn
        {
            get => _civildawn;
            set
            {
                _civildawn = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CivilDawnFormatted));
            }
        }

        public DateTime Civildusk
        {
            get => _civildusk;
            set
            {
                _civildusk = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CivilDuskFormatted));
            }
        }

        public DateTime GoldenHourMorning
        {
            get => _sunrise.AddHours(1);
        }

        public DateTime GoldenHourEvening
        {
            get => _sunset.AddHours(-1);
        }

        public string SunRiseFormatted
        {
            get => _sunrise.ToString(TimeFormat);
        }

        public string SunSetFormatted
        {
            get => _sunset.ToString(TimeFormat);
        }

        public string SolarNoonFormatted
        {
            get => _solarnoon.ToString(TimeFormat);
        }

        public string GoldenHourMorningFormatted
        {
            get => GoldenHourMorning.ToString(TimeFormat);
        }

        public string GoldenHourEveningFormatted
        {
            get => GoldenHourEvening.ToString(TimeFormat);
        }

        public string AstronomicalDawnFormatted
        {
            get => _astronomicalDawn.ToString(TimeFormat);
        }

        public string AstronomicalDuskFormatted
        {
            get => _astronomicalDusk.ToString(TimeFormat);
        }

        public string NauticalDawnFormatted
        {
            get => _nauticaldawn.ToString(TimeFormat);
        }

        public string NauticalDuskFormatted
        {
            get => _nauticaldusk.ToString(TimeFormat);
        }

        public string CivilDawnFormatted
        {
            get => _civildawn.ToString(TimeFormat);
        }

        public string CivilDuskFormatted
        {
            get => _civildusk.ToString(TimeFormat);
        }

        public string LocationPhoto
        {
            get => _locationPhoto;
            set
            {
                _locationPhoto = value;
                OnPropertyChanged();
            }
        }
        #endregion

        #region Commands
        public ICommand LoadLocationsCommand { get; }
        public ICommand CalculateSunTimesCommand { get; }
        #endregion

        #region Events
        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler<OperationErrorEventArgs>? ErrorOccurred;
        #endregion

        #region Constructor
        public SunCalculations(ISunCalculator sunCalculatorService)
        {
            _sunCalculatorService = sunCalculatorService ?? throw new ArgumentNullException(nameof(sunCalculatorService));

            LoadLocationsCommand = new AsyncRelayCommand(LoadLocationsAsync);
            CalculateSunTimesCommand = new RelayCommand(CalculateSun);
        }
        #endregion

        #region Methods
        public void CalculateSun()
        {
            try
            {
                IsBusy = true;
                ErrorMessage = string.Empty;

                if (Latitude == 0 && Longitude == 0)
                {
                    return; // Do not calculate for default coordinates
                }

                TimeZoneInfo localTimeZone = TimeZoneInfo.FindSystemTimeZoneById(TimeZoneInfo.Local.Id);
                SolarTimes solarTimes = new SolarTimes(Date, Latitude, Longitude);

                Sunrise = TimeZoneInfo.ConvertTimeFromUtc(solarTimes.Sunrise.ToUniversalTime(), localTimeZone);
                Sunset = TimeZoneInfo.ConvertTimeFromUtc(solarTimes.Sunset.ToUniversalTime(), localTimeZone);
                SolarNoon = TimeZoneInfo.ConvertTimeFromUtc(solarTimes.SolarNoon.ToUniversalTime(), localTimeZone);
                AstronomicalDawn = TimeZoneInfo.ConvertTimeFromUtc(solarTimes.DawnAstronomical.ToUniversalTime(), localTimeZone);
                NauticalDawn = TimeZoneInfo.ConvertTimeFromUtc(solarTimes.DawnNautical.ToUniversalTime(), localTimeZone);
                NauticalDusk = TimeZoneInfo.ConvertTimeFromUtc(solarTimes.DuskNautical.ToUniversalTime(), localTimeZone);
                AstronomicalDusk = TimeZoneInfo.ConvertTimeFromUtc(solarTimes.DuskAstronomical.ToUniversalTime(), localTimeZone);
                Civildawn = TimeZoneInfo.ConvertTimeFromUtc(solarTimes.DawnCivil.ToUniversalTime(), localTimeZone);
                Civildusk = TimeZoneInfo.ConvertTimeFromUtc(solarTimes.DuskCivil.ToUniversalTime(), localTimeZone);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error calculating sun times: {ex.Message}";
                OnErrorOccurred(new OperationErrorEventArgs(
                    Locations.Core.Shared.ViewModels.OperationErrorSource.Unknown,
                    ErrorMessage,
                    ex));
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

                // Note: In a real implementation, this would call a service to get locations
                // For now, we'll assume this method would be implemented to load data
                await Task.Delay(100); // Placeholder
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error loading locations: {ex.Message}";
                OnErrorOccurred(new OperationErrorEventArgs(
                    Locations.Core.Shared.ViewModels.OperationErrorSource.Unknown,
                    ErrorMessage,
                    ex));
            }
            finally
            {
                IsBusy = false;
            }
        }

        protected virtual void OnErrorOccurred(OperationErrorEventArgs e)
        {
            ErrorOccurred?.Invoke(this, e);
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}