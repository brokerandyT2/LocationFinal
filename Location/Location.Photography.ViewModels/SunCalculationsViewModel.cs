// Location.Photography.ViewModels/SunCalculationsViewModel.cs
using CommunityToolkit.Mvvm.Input;
using Location.Core.ViewModels;
using Location.Photography.Domain.Services;
using Location.Photography.ViewModels.Events;
using Location.Photography.ViewModels.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using OperationErrorEventArgs = Location.Photography.ViewModels.Events.OperationErrorEventArgs;

namespace Location.Photography.ViewModels
{
    public class SunCalculationsViewModel : ViewModelBase, ISunCalculations
    {
        #region Fields
        private readonly ISunCalculatorService _sunCalculatorService;
        private List<LocationViewModel> _locations = new List<LocationViewModel>();
        private LocationViewModel _selectedLocation = new LocationViewModel();
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

        public DateTime GoldenHourMorning => _sunrise.AddHours(1);

        public DateTime GoldenHourEvening => _sunset.AddHours(-1);

        public string SunRiseFormatted => _sunrise.ToString(TimeFormat);

        public string SunSetFormatted => _sunset.ToString(TimeFormat);

        public string SolarNoonFormatted => _solarnoon.ToString(TimeFormat);

        public string GoldenHourMorningFormatted => GoldenHourMorning.ToString(TimeFormat);

        public string GoldenHourEveningFormatted => GoldenHourEvening.ToString(TimeFormat);

        public string AstronomicalDawnFormatted => _astronomicalDawn.ToString(TimeFormat);

        public string AstronomicalDuskFormatted => _astronomicalDusk.ToString(TimeFormat);

        public string NauticalDawnFormatted => _nauticaldawn.ToString(TimeFormat);

        public string NauticalDuskFormatted => _nauticaldusk.ToString(TimeFormat);

        public string CivilDawnFormatted => _civildawn.ToString(TimeFormat);

        public string CivilDuskFormatted => _civildusk.ToString(TimeFormat);

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
        public new event EventHandler<OperationErrorEventArgs>? ErrorOccurred;
        #endregion

        #region Constructor
        public SunCalculationsViewModel(ISunCalculatorService sunCalculatorService)
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

                // Calculate sun times using our custom service
                Sunrise = _sunCalculatorService.GetSunrise(Date, Latitude, Longitude);
                Sunset = _sunCalculatorService.GetSunset(Date, Latitude, Longitude);
                SolarNoon = _sunCalculatorService.GetSolarNoon(Date, Latitude, Longitude);
                AstronomicalDawn = _sunCalculatorService.GetAstronomicalDawn(Date, Latitude, Longitude);
                AstronomicalDusk = _sunCalculatorService.GetAstronomicalDusk(Date, Latitude, Longitude);
                NauticalDawn = _sunCalculatorService.GetNauticalDawn(Date, Latitude, Longitude);
                NauticalDusk = _sunCalculatorService.GetNauticalDusk(Date, Latitude, Longitude);
                Civildawn = _sunCalculatorService.GetCivilDawn(Date, Latitude, Longitude);
                Civildusk = _sunCalculatorService.GetCivilDusk(Date, Latitude, Longitude);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error calculating sun times: {ex.Message}";
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

        public async Task LoadLocationsAsync()
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
                    OperationErrorSource.Unknown,
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
        #endregion
    }

    // Helper class needed for the ViewModel
    public class LocationViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double Lattitude { get; set; }
        public double Longitude { get; set; }
        public string Photo { get; set; } = string.Empty;
    }
}