// Location.Photography.Shared/ViewModels/Interfaces/ISunCalculations.cs
using Location.Core.ViewModels;
//using Locations.Core.Shared.ViewModels;
using System;
using System.Collections.Generic;
using System.Windows.Input;

namespace Location.Photography.ViewModels.Interfaces
{
    public interface ISunCalculations
    {
        // Properties for location selection
        List<LocationViewModel> LocationsS { get; set; }

        // Properties for date and location
        DateTime Date { get; set; }
        double Latitude { get; set; }
        double Longitude { get; set; }
        string DateFormat { get; set; }
        string TimeFormat { get; set; }

        // Sun time properties
        DateTime Sunrise { get; set; }
        DateTime Sunset { get; set; }
        DateTime SolarNoon { get; set; }
        DateTime AstronomicalDawn { get; set; }
        DateTime AstronomicalDusk { get; set; }
        DateTime NauticalDawn { get; set; }
        DateTime NauticalDusk { get; set; }
        DateTime Civildawn { get; set; }
        DateTime Civildusk { get; set; }
        DateTime GoldenHourMorning { get; }
        DateTime GoldenHourEvening { get; }

        // Formatted time strings
        string SunRiseFormatted { get; }
        string SunSetFormatted { get; }
        string SolarNoonFormatted { get; }
        string GoldenHourMorningFormatted { get; }
        string GoldenHourEveningFormatted { get; }
        string AstronomicalDawnFormatted { get; }
        string AstronomicalDuskFormatted { get; }
        string NauticalDawnFormatted { get; }
        string NauticalDuskFormatted { get; }
        string CivilDawnFormatted { get; }
        string CivilDuskFormatted { get; }

        // Methods
        void CalculateSun();

        // Commands
        ICommand LoadLocationsCommand { get; }
        ICommand CalculateSunTimesCommand { get; }

        // Events
        event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        event EventHandler<OperationErrorEventArgs> ErrorOccurred;
    }
}