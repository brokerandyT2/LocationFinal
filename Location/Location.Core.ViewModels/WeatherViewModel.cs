// Location.Core.ViewModels/WeatherViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Location.Core.Application.Queries.Weather;
using Location.Core.Application.Services;
using Location.Core.Application.Weather.DTOs;
using Location.Core.Application.Weather.Queries.GetWeatherForecast;
using MediatR;
using System.Collections.ObjectModel;

namespace Location.Core.ViewModels
{
    public partial class WeatherViewModel : BaseViewModel, INavigationAware
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

                // Get weather data for the location (includes forecast completeness check)
                var query = new GetWeatherByLocationQuery { LocationId = locationId };
                var result = await _mediator.Send(query, cancellationToken);

                if (!result.IsSuccess || result.Data == null)
                {
                    // System error from MediatR
                    OnSystemError(result.ErrorMessage ?? "Failed to load weather data");
                    return;
                }

                var weatherData = result.Data;

                // Now get the forecast data using coordinates from weather data
                var forecastQuery = new GetWeatherForecastQuery
                {
                    Latitude = weatherData.Latitude,
                    Longitude = weatherData.Longitude,
                    Days = 5 // Only need today + next 4 days for display
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

                // Process first 5 days only (today + next 4 days)
                var today = DateTime.Today;
                foreach (var dailyForecast in forecast.DailyForecasts.Take(5))
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
            return $"a{iconCode}.png";
        }

        public void OnNavigatedToAsync()
        {
            throw new NotImplementedException();
        }

        public void OnNavigatedFromAsync()
        {
            throw new NotImplementedException();
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