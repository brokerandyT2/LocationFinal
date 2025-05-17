// Location.Photography.ViewModels.Premium/SunCalculatorViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Locations.DTOs;
using Location.Core.Application.Locations.Queries.GetLocations;
using Location.Core.Application.Services;
using Location.Core.ViewModels;
using Location.Photography.Application.Commands.SunLocation;
using Location.Photography.Application.Queries.SunLocation;
using Location.Photography.Application.Services;
using Location.Photography.Domain.Models;
using Location.Photography.ViewModels.Events;
using MediatR;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OperationErrorEventArgs = Location.Photography.ViewModels.Events.OperationErrorEventArgs;
using OperationErrorSource = Location.Photography.ViewModels.Events.OperationErrorSource;

namespace Location.Photography.ViewModels.Premium
{
    public partial class SunCalculatorViewModel : BaseViewModel
    {
        private readonly IMediator _mediator;
        private readonly IAlertService _alertService;

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private string _errorMessage;

        [ObservableProperty]
        private ObservableCollection<LocationListItemViewModel> _locations = new();

        [ObservableProperty]
        private LocationListItemViewModel _selectedLocation;

        [ObservableProperty]
        private DateTime _date = DateTime.Today;

        [ObservableProperty]
        private string _locationPhoto = string.Empty;

        [ObservableProperty]
        private string _dateFormat = "MM/dd/yyyy";

        [ObservableProperty]
        private string _timeFormat = "hh:mm tt";

        // Sun Times properties
        [ObservableProperty]
        private DateTime _sunrise = DateTime.Now;

        [ObservableProperty]
        private DateTime _sunset = DateTime.Now;

        [ObservableProperty]
        private DateTime _solarNoon = DateTime.Now;

        [ObservableProperty]
        private DateTime _astronomicalDawn = DateTime.Now;

        [ObservableProperty]
        private DateTime _nauticalDawn = DateTime.Now;

        [ObservableProperty]
        private DateTime _nauticalDusk = DateTime.Now;

        [ObservableProperty]
        private DateTime _astronomicalDusk = DateTime.Now;

        [ObservableProperty]
        private DateTime _civilDawn = DateTime.Now;

        [ObservableProperty]
        private DateTime _civilDusk = DateTime.Now;

        // Formatted string properties for display
        public string SunRiseFormatted => Sunrise.ToString(TimeFormat);
        public string SunSetFormatted => Sunset.ToString(TimeFormat);
        public string SolarNoonFormatted => SolarNoon.ToString(TimeFormat);
        public string GoldenHourMorningFormatted => Sunrise.AddHours(1).ToString(TimeFormat);
        public string GoldenHourEveningFormatted => Sunset.AddHours(-1).ToString(TimeFormat);
        public string AstronomicalDawnFormatted => AstronomicalDawn.ToString(TimeFormat);
        public string AstronomicalDuskFormatted => AstronomicalDusk.ToString(TimeFormat);
        public string NauticalDawnFormatted => NauticalDawn.ToString(TimeFormat);
        public string NauticalDuskFormatted => NauticalDusk.ToString(TimeFormat);
        public string CivilDawnFormatted => CivilDawn.ToString(TimeFormat);
        public string CivilDuskFormatted => CivilDusk.ToString(TimeFormat);

        public event EventHandler<OperationErrorEventArgs> ErrorOccurred;

        public SunCalculatorViewModel(IMediator mediator, IAlertService alertService)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
        }

        [RelayCommand]
        public async Task LoadLocationsAsync()
        {
            try
            {
                IsBusy = true;
                ErrorMessage = string.Empty;

                // Create query to get locations
                var query = new GetLocationsQuery
                {
                    PageNumber = 1,
                    PageSize = 100, // Get all locations
                    IncludeDeleted = false
                };

                // Send the query through MediatR
                var result = await _mediator.Send(query, CancellationToken.None);

                if (result.IsSuccess && result.Data != null)
                {
                    // Clear current collection
                    Locations.Clear();

                    // Add locations to collection
                    foreach (var locationDto in result.Data.Items)
                    {
                        Locations.Add(new LocationListItemViewModel
                        {
                            Id = locationDto.Id,
                            Title = locationDto.Title,
                            Latitude = locationDto.Latitude,
                            Longitude = locationDto.Longitude,
                            Photo = locationDto.PhotoPath,
                            IsDeleted = locationDto.IsDeleted
                        });
                    }

                    // Select the first location by default
                    if (Locations.Count > 0)
                    {
                        SelectedLocation = Locations[0];
                    }
                }
                else
                {
                    HandleError(new Exception(result.ErrorMessage ?? "No locations found"), "Failed to load locations");
                }
            }
            catch (Exception ex)
            {
                HandleError(ex, "Error loading locations");
            }
            finally
            {
                IsBusy = false;
            }
        }

        partial void OnSelectedLocationChanged(LocationListItemViewModel value)
        {
            if (value != null)
            {
                LocationPhoto = value.Photo;
                CalculateSun();
            }
        }

        partial void OnDateChanged(DateTime value)
        {
            CalculateSun();
        }

        partial void OnTimeFormatChanged(string value)
        {
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

        [RelayCommand]
        public async Task CalculateSunAsync()
        {
            CalculateSun();
        }

        public void CalculateSun()
        {
            if (SelectedLocation == null)
                return;

            try
            {
                IsBusy = true;
                ErrorMessage = string.Empty;

                // Use MediatR to get sun times
                var query = new GetSunTimesQuery
                {
                    Latitude = SelectedLocation.Latitude,
                    Longitude = SelectedLocation.Longitude,
                    Date = Date
                };

                var result = _mediator.Send(query).GetAwaiter().GetResult();

                if (result.IsSuccess && result.Data != null)
                {
                    var sunTimes = result.Data;

                    // Update all the time properties
                    Sunrise = sunTimes.Sunrise;
                    Sunset = sunTimes.Sunset;
                    SolarNoon = sunTimes.SolarNoon;
                    AstronomicalDawn = sunTimes.AstronomicalDawn;
                    AstronomicalDusk = sunTimes.AstronomicalDusk;
                    NauticalDawn = sunTimes.NauticalDawn;
                    NauticalDusk = sunTimes.NauticalDusk;
                    CivilDawn = sunTimes.CivilDawn;
                    CivilDusk = sunTimes.CivilDusk;

                    // Update the formatted string properties
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
                else
                {
                    HandleError(new Exception(result.ErrorMessage ?? "Failed to calculate sun times"), "Sun calculation error");
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

        private void HandleError(Exception ex, string message)
        {
            ErrorMessage = $"{message}: {ex.Message}";
            Debug.WriteLine($"{message}: {ex}");
            OnErrorOccurred(new OperationErrorEventArgs(OperationErrorSource.Unknown, ErrorMessage, ex));
        }

        protected virtual void OnErrorOccurred(OperationErrorEventArgs e)
        {
            ErrorOccurred?.Invoke(this, e);
        }
    }
}