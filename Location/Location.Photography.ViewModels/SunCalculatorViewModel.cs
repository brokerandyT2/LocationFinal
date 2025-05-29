// Location.Photography.ViewModels.Premium/SunCalculatorViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Locations.DTOs;
using Location.Core.Application.Locations.Queries.GetLocations;
using Location.Core.Application.Services;
using Location.Core.ViewModels;
using Location.Photography.Application.Queries.SunLocation;
using Location.Photography.Application.Services;
using Location.Photography.Domain.Models;
using Location.Photography.ViewModels.Events;
using Location.Photography.ViewModels.Interfaces;
using MediatR;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;
using OperationErrorEventArgs = Location.Photography.ViewModels.Events.OperationErrorEventArgs;
using OperationErrorSource = Location.Photography.ViewModels.Events.OperationErrorSource;

namespace Location.Photography.ViewModels
{
    public partial class SunCalculatorViewModel : ViewModelBase, Interfaces.INavigationAware
    {
        private readonly IMediator _mediator;
        private readonly IErrorDisplayService _errorDisplayService;

        [ObservableProperty]
        private ObservableCollection<LocationListItemViewModel> _locations = new();

        [ObservableProperty]
        private LocationListItemViewModel _selectedLocation;

        [ObservableProperty]
        private DateTime _dates = DateTime.Today;

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

        public new event EventHandler<OperationErrorEventArgs> ErrorOccurred;

        public SunCalculatorViewModel(IMediator mediator, IErrorDisplayService errorDisplayService)
            : base(null, errorDisplayService)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _errorDisplayService = errorDisplayService ?? throw new ArgumentNullException(nameof(errorDisplayService));
        }

        [RelayCommand]
        public async Task LoadLocationsAsync()
        {
            var command = new AsyncRelayCommand(async () =>
            {
                try
                {
                    ClearErrors();

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
                        OnSystemError(result.ErrorMessage ?? "Failed to load locations");
                    }
                }
                catch (Exception ex)
                {
                    OnSystemError($"Error loading locations: {ex.Message}");
                }
            });

            await ExecuteAndTrackAsync(command);
        }

        partial void OnSelectedLocationChanged(LocationListItemViewModel value)
        {
            if (value != null)
            {
                LocationPhoto = value.Photo;
                CalculateSun();
            }
        }

        public void OnDateChanged(DateTime value)
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
            var command = new AsyncRelayCommand(async () =>
            {
                await Task.Run(() => CalculateSun());
            });

            await ExecuteAndTrackAsync(command);
        }

        public void CalculateSun()
        {
            if (SelectedLocation == null)
                return;

            try
            {
                IsBusy = true;
                ClearErrors();

                // Use MediatR to get sun times
                var query = new GetSunTimesQuery
                {
                    Latitude = SelectedLocation.Latitude,
                    Longitude = SelectedLocation.Longitude,
                    Date = Dates
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
                    OnSystemError(result.ErrorMessage ?? "Failed to calculate sun times");
                }
            }
            catch (Exception ex)
            {
                OnSystemError($"Error calculating sun times: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        protected override void OnErrorOccurred(string message)
        {
            ErrorOccurred?.Invoke(this, new OperationErrorEventArgs(OperationErrorSource.Unknown, message));
        }

        public void OnNavigatedToAsync()
        {
            _ = LoadLocationsAsync();
        }

        public void OnNavigatedFromAsync()
        {
           // throw new NotImplementedException();
        }
    }
}