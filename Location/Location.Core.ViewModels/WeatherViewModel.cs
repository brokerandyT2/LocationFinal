// Location.Core.ViewModels/WeatherViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Location.Core.Application.Commands.Weather;
using Location.Core.Application.Queries.Weather;
using Location.Core.Application.Services;
using Location.Core.Application.Weather.DTOs;
using Location.Core.Application.Weather.Queries.GetWeatherForecast;
using MediatR;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Linq;
using System.Threading.Tasks;

namespace Location.Core.ViewModels
{
    public partial class WeatherViewModel : BaseViewModel
    {
        private readonly IMediator _mediator;

        [ObservableProperty]
        private int _locationId;

        public ObservableCollection<DailyWeatherViewModel> DailyForecasts { get; } = new();

        [ObservableProperty]
        private WeatherForecastDto _weatherForecast;

        // Default constructor for design-time
        public WeatherViewModel() : base(null, null)
        {
        }

        // Main constructor with dependencies
        public WeatherViewModel(
            IMediator mediator,
            IErrorDisplayService errorDisplayService)
            : base(null, errorDisplayService)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        [RelayCommand]
        private async Task LoadWeatherAsync(int locationId, CancellationToken cancellationToken = default)
        {
            try
            {
                IsBusy = true;
                ClearErrors();
                LocationId = locationId;

                // First, get the weather data for the location
                var query = new GetWeatherByLocationQuery { LocationId = locationId };
                var result = await _mediator.Send(query, cancellationToken);

                if (!result.IsSuccess || result.Data == null)
                {
                    // Try updating the weather instead
                    var updateCommand = new UpdateWeatherCommand { LocationId = locationId, ForceUpdate = true };
                    result = await _mediator.Send(updateCommand, cancellationToken);

                    if (!result.IsSuccess || result.Data == null)
                    {
                        // System error from MediatR
                        OnSystemError(result.ErrorMessage ?? "Failed to load weather data");
                        return;
                    }
                }

                // Now get the forecast data
                var forecastQuery = new GetWeatherForecastQuery
                {
                    Latitude = result.Data.Latitude,
                    Longitude = result.Data.Longitude,
                    Days = 7
                };

                var forecastResult = await _mediator.Send(forecastQuery, cancellationToken);

                if (!forecastResult.IsSuccess || forecastResult.Data == null)
                {
                    // System error from MediatR
                    OnSystemError(forecastResult.ErrorMessage ?? "Failed to load forecast data");
                    return;
                }

                // Store the forecast data
                WeatherForecast = forecastResult.Data;

                // Process the forecast data for display
                ProcessForecastData(forecastResult.Data);
            }
            catch (Exception ex)
            {
                // System error
                OnSystemError($"Error loading weather data: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ProcessForecastData(WeatherForecastDto forecast)
        {
            try
            {
                if (forecast?.DailyForecasts == null || forecast.DailyForecasts.Count == 0)
                {
                    // Validation error - show in UI
                    SetValidationError("No forecast data available");
                    return;
                }

                // Clear existing forecasts
                DailyForecasts.Clear();

                // Process all available days (up to 7)
                var today = DateTime.Today;
                foreach (var dailyForecast in forecast.DailyForecasts.Take(7))
                {
                    var isToday = dailyForecast.Date.Date == today;

                    DailyForecasts.Add(new DailyWeatherViewModel
                    {
                        Date = dailyForecast.Date,
                        DayName = dailyForecast.Date.ToString("dddd, MMMM d"),
                        Description = dailyForecast.Description,
                        MinTemperature = $"{dailyForecast.MinTemperature:F1}°",
                        MaxTemperature = $"{dailyForecast.MaxTemperature:F1}°",
                        WeatherIcon = GetWeatherIconUrl(dailyForecast.Icon),
                        SunriseTime = dailyForecast.Sunrise.ToString("t"),
                        SunsetTime = dailyForecast.Sunset.ToString("t"),
                        WindDirection = dailyForecast.WindDirection,
                        WindSpeed = $"{dailyForecast.WindSpeed:F1} mph",
                        WindGust = dailyForecast.WindGust.HasValue ? $"{dailyForecast.WindGust.Value:F1} mph" : "N/A",
                        IsToday = isToday
                    });
                }
            }
            catch (Exception ex)
            {
                // System error
                OnSystemError($"Error processing forecast data: {ex.Message}");
            }
        }

        private string GetWeatherIconUrl(string iconCode)
        {
            if (string.IsNullOrEmpty(iconCode))
            {
                return "weather_unknown.png";
            }

            // Map icon code to local image or return URL for web images
            // This is a simplified implementation - you may need to adjust it
            return $"weather_{iconCode}.png";
        }
    }

    public class DailyWeatherViewModel : ObservableObject
    {
        private DateTime _date;
        private string _dayName = string.Empty;
        private string _description = string.Empty;
        private string _minTemperature = string.Empty;
        private string _maxTemperature = string.Empty;
        private string _weatherIcon = string.Empty;
        private string _sunriseTime = string.Empty;
        private string _sunsetTime = string.Empty;
        private double _windDirection;
        private string _windSpeed = string.Empty;
        private string _windGust = string.Empty;

        public DateTime Date
        {
            get => _date;
            set => SetProperty(ref _date, value);
        }

        public string DayName
        {
            get => _dayName;
            set => SetProperty(ref _dayName, value);
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public string MinTemperature
        {
            get => _minTemperature;
            set => SetProperty(ref _minTemperature, value);
        }

        public string MaxTemperature
        {
            get => _maxTemperature;
            set => SetProperty(ref _maxTemperature, value);
        }

        public string WeatherIcon
        {
            get => _weatherIcon;
            set => SetProperty(ref _weatherIcon, value);
        }

        public string SunriseTime
        {
            get => _sunriseTime;
            set => SetProperty(ref _sunriseTime, value);
        }

        public string SunsetTime
        {
            get => _sunsetTime;
            set => SetProperty(ref _sunsetTime, value);
        }

        public double WindDirection
        {
            get => _windDirection;
            set => SetProperty(ref _windDirection, value);
        }

        public string WindSpeed
        {
            get => _windSpeed;
            set => SetProperty(ref _windSpeed, value);
        }

        public string WindGust
        {
            get => _windGust;
            set => SetProperty(ref _windGust, value);
        }

        public bool IsToday { get; set; }
    }
}
    }
}