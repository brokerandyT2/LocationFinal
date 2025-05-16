using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Location.Core.Application.Commands.Weather;
using Location.Core.Application.Queries.Weather;
using Location.Core.Application.Services;
using Location.Core.Application.Weather.DTOs;
using Location.Core.Application.Weather.Queries.GetWeatherForecast;
using Location.Core.Domain.Entities;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Core.ViewModels
{
    public partial class WeatherViewModel : BaseViewModel
    {
        private readonly IMediator _mediator;

        [ObservableProperty]
        private int _locationId;

        [ObservableProperty]
        private string _dayOne = string.Empty;

        [ObservableProperty]
        private string _dayTwo = string.Empty;

        [ObservableProperty]
        private string _forecast_Day_One = string.Empty;

        [ObservableProperty]
        private string _forecast_Day_Two = string.Empty;

        [ObservableProperty]
        private string _temperature_Day_One_Min = string.Empty;

        [ObservableProperty]
        private string _temperature_Day_One_Max = string.Empty;

        [ObservableProperty]
        private string _temperature_Day_Two_Min = string.Empty;

        [ObservableProperty]
        private string _temperature_Day_Two_Max = string.Empty;

        [ObservableProperty]
        private string _weather_Day_One_Icon = string.Empty;

        [ObservableProperty]
        private string _weather_Day_Two_Icon = string.Empty;

        [ObservableProperty]
        private string _sunrise_Day_One_String = string.Empty;

        [ObservableProperty]
        private string _sunset_Day_One_String = string.Empty;

        [ObservableProperty]
        private string _sunrise_Day_Two_String = string.Empty;

        [ObservableProperty]
        private string _sunset_Day_Two_String = string.Empty;

        [ObservableProperty]
        private double _windDirectionDay_One;

        [ObservableProperty]
        private double _windDirectionDay_Two;

        [ObservableProperty]
        private string _windSpeedDay_One = string.Empty;

        [ObservableProperty]
        private string _windSpeedDay_Two = string.Empty;

        [ObservableProperty]
        private string _windGustDay_One = string.Empty;

        [ObservableProperty]
        private string _windGustDay_Two = string.Empty;

        [ObservableProperty]
        private WeatherForecastDto _weatherForecast;

        // Event to notify about errors
        public event EventHandler<OperationErrorEventArgs>? ErrorOccurred;

        // Default constructor for design-time
        public WeatherViewModel() : base(null)
        {
        }

        // Main constructor with dependencies
        public WeatherViewModel(
            IMediator mediator,
            IAlertService alertingService)
            : base(alertingService)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        [RelayCommand]
        private async Task LoadWeatherAsync(int locationId, CancellationToken cancellationToken = default)
        {
            try
            {
                IsBusy = true;
                IsError = false;
                ErrorMessage = string.Empty;
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
                        ErrorMessage = result.ErrorMessage ?? "Failed to load weather data";
                        IsError = true;
                        OnErrorOccurred(ErrorMessage);
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
                    ErrorMessage = forecastResult.ErrorMessage ?? "Failed to load forecast data";
                    IsError = true;
                    OnErrorOccurred(ErrorMessage);
                    return;
                }

                // Store the forecast data
                WeatherForecast = forecastResult.Data;

                // Process the forecast data for display
                ProcessForecastData(forecastResult.Data);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error loading weather data: {ex.Message}";
                IsError = true;
                OnErrorOccurred(ErrorMessage);
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
                if (forecast?.DailyForecasts == null || forecast.DailyForecasts.Count < 2)
                {
                    return;
                }

                // Process day one
                var dayOne = forecast.DailyForecasts[0];
                DayOne = dayOne.Date.ToString("dddd, MMMM d");
                Forecast_Day_One = dayOne.Description;
                Temperature_Day_One_Min = $"{dayOne.MinTemperature:F1}°";
                Temperature_Day_One_Max = $"{dayOne.MaxTemperature:F1}°";
                Weather_Day_One_Icon = GetWeatherIconUrl(dayOne.Icon);
                Sunrise_Day_One_String = dayOne.Sunrise.ToString("t");
                Sunset_Day_One_String = dayOne.Sunset.ToString("t");
                WindDirectionDay_One = dayOne.WindDirection;
                WindSpeedDay_One = $"{dayOne.WindSpeed:F1} mph";
                WindGustDay_One = dayOne.WindGust.HasValue ? $"{dayOne.WindGust.Value:F1} mph" : "N/A";

                // Process day two
                var dayTwo = forecast.DailyForecasts[1];
                DayTwo = dayTwo.Date.ToString("dddd, MMMM d");
                Forecast_Day_Two = dayTwo.Description;
                Temperature_Day_Two_Min = $"{dayTwo.MinTemperature:F1}°";
                Temperature_Day_Two_Max = $"{dayTwo.MaxTemperature:F1}°";
                Weather_Day_Two_Icon = GetWeatherIconUrl(dayTwo.Icon);
                Sunrise_Day_Two_String = dayTwo.Sunrise.ToString("t");
                Sunset_Day_Two_String = dayTwo.Sunset.ToString("t");
                WindDirectionDay_Two = dayTwo.WindDirection;
                WindSpeedDay_Two = $"{dayTwo.WindSpeed:F1} mph";
                WindGustDay_Two = dayTwo.WindGust.HasValue ? $"{dayTwo.WindGust.Value:F1} mph" : "N/A";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error processing forecast data: {ex.Message}";
                IsError = true;
                OnErrorOccurred(ErrorMessage);
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

        // Helper method to raise error event
        protected virtual void OnErrorOccurred(string message)
        {
            ErrorOccurred?.Invoke(this, new OperationErrorEventArgs(message));
        }
    }
}