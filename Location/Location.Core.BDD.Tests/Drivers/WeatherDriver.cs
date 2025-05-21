using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Services;
using Location.Core.Application.Weather.DTOs;
using Location.Core.Application.Commands.Weather;
using Location.Core.Application.Weather.Queries.GetWeatherForecast;
using Location.Core.Application.Weather.Queries.UpdateAllWeather;
using Location.Core.BDD.Tests.Models;
using Location.Core.BDD.Tests.Support;
using MediatR;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Core.BDD.Tests.Drivers
{
    /// <summary>
    /// Driver class for weather-related operations in BDD tests
    /// </summary>
    public class WeatherDriver
    {
        private readonly ApiContext _context;
        private readonly IMediator _mediator;
        private readonly Mock<IWeatherService> _weatherServiceMock;
        private readonly Mock<ILocationRepository> _locationRepositoryMock;
        private readonly Mock<IWeatherRepository> _weatherRepositoryMock;

        /// <summary>
        /// Initializes a new instance of the WeatherDriver class
        /// </summary>
        /// <param name="context">The API context for the test</param>
        public WeatherDriver(ApiContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _mediator = _context.GetService<IMediator>() ?? throw new InvalidOperationException("Failed to resolve IMediator from context");
            _weatherServiceMock = _context.GetService<Mock<IWeatherService>>() ?? throw new InvalidOperationException("Failed to resolve Mock<IWeatherService> from context");
            _locationRepositoryMock = _context.GetService<Mock<ILocationRepository>>() ?? throw new InvalidOperationException("Failed to resolve Mock<ILocationRepository> from context");
            _weatherRepositoryMock = _context.GetService<Mock<IWeatherRepository>>() ?? throw new InvalidOperationException("Failed to resolve Mock<IWeatherRepository> from context");
        }

        /// <summary>
        /// Updates weather for a specific location
        /// </summary>
        /// <param name="locationId">The ID of the location to update weather for</param>
        /// <param name="forceUpdate">Whether to force the update even if recent data exists</param>
        /// <returns>A result containing the updated weather data</returns>
        public async Task<Result<WeatherDto>> UpdateWeatherForLocationAsync(int locationId, bool forceUpdate = false)
        {
            try
            {
                // Set up the mock repositories
                var locationModel = _context.GetModel<LocationTestModel>($"Location_{locationId}");
                if (locationModel == null)
                {
                    return Result<WeatherDto>.Failure($"Location with ID {locationId} not found in context");
                }

                var locationEntity = locationModel.ToDomainEntity();
                _locationRepositoryMock
                    .Setup(repo => repo.GetByIdAsync(
                        It.Is<int>(id => id == locationId),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<Domain.Entities.Location>.Success(locationEntity));

                // Create a weather response
                var weatherDto = new WeatherDto
                {
                    Id = 1,
                    LocationId = locationId,
                    Latitude = locationModel.Latitude,
                    Longitude = locationModel.Longitude,
                    Timezone = "America/New_York",
                    TimezoneOffset = -18000,
                    LastUpdate = DateTime.UtcNow,
                    Temperature = 22.5,
                    Description = "Partly cloudy",
                    Icon = "03d",
                    WindSpeed = 12.5,
                    WindDirection = 180,
                    WindGust = 15.0,
                    Humidity = 65,
                    Pressure = 1012,
                    Clouds = 40,
                    UvIndex = 6.2,
                    Precipitation = 0.1,
                    Sunrise = DateTime.Today.AddHours(6).AddMinutes(30),
                    Sunset = DateTime.Today.AddHours(19).AddMinutes(45),
                    MoonRise = DateTime.Today.AddHours(20).AddMinutes(15),
                    MoonSet = DateTime.Today.AddHours(8).AddMinutes(20),
                    MoonPhase = 0.25
                };

                // Set up the weather service mock
                _weatherServiceMock
                    .Setup(service => service.UpdateWeatherForLocationAsync(
                        It.Is<int>(id => id == locationId),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<WeatherDto>.Success(weatherDto));

                // Create the command
                var command = new UpdateWeatherCommand
                {
                    LocationId = locationId,
                    ForceUpdate = forceUpdate
                };

                // Send the command
                var result = await _mediator.Send(command);

                // Store the result
                _context.StoreResult(result);

                if (result.IsSuccess && result.Data != null)
                {
                    var weatherModel = WeatherTestModel.FromDto(result.Data);
                    _context.StoreWeatherData(weatherModel);
                }

                return result;
            }
            catch (Exception ex)
            {
                return Result<WeatherDto>.Failure($"Failed to update weather: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets a weather forecast for specified coordinates
        /// </summary>
        /// <param name="latitude">Latitude coordinate</param>
        /// <param name="longitude">Longitude coordinate</param>
        /// <param name="days">Number of days to include in the forecast</param>
        /// <returns>A result containing the weather forecast</returns>
        public async Task<Result<WeatherForecastDto>> GetWeatherForecastAsync(double latitude, double longitude, int days = 7)
        {
            try
            {
                // Create a forecast response
                var forecastDto = new WeatherForecastDto
                {
                    WeatherId = 1,
                    LastUpdate = DateTime.UtcNow,
                    Timezone = "America/New_York",
                    TimezoneOffset = -18000,
                    DailyForecasts = new List<DailyForecastDto>()
                };

                // Add forecasts for requested days
                for (int i = 0; i < days; i++)
                {
                    forecastDto.DailyForecasts.Add(new DailyForecastDto
                    {
                        Date = DateTime.Today.AddDays(i),
                        Temperature = 22.5 + i,
                        MinTemperature = 18.2 + i,
                        MaxTemperature = 25.8 + i,
                        Description = i == 0 ? "Partly cloudy" : (i % 2 == 0 ? "Sunny" : "Cloudy"),
                        Icon = i == 0 ? "03d" : (i % 2 == 0 ? "01d" : "04d"),
                        WindSpeed = 12.5 - (i * 0.5),
                        WindDirection = 180 + (i * 10),
                        Humidity = 65 - (i * 2),
                        Pressure = 1012 + (i * 1),
                        Clouds = 40 - (i * 5),
                        UvIndex = 6.2 - (i * 0.3),
                        Precipitation = i % 2 == 0 ? 0 : 0.2,
                        Sunrise = DateTime.Today.AddDays(i).AddHours(6).AddMinutes(30 - i),
                        Sunset = DateTime.Today.AddDays(i).AddHours(19).AddMinutes(45 + i),
                        MoonRise = DateTime.Today.AddDays(i).AddHours(20).AddMinutes(15 - i * 2),
                        MoonSet = DateTime.Today.AddDays(i).AddHours(8).AddMinutes(20 + i * 2),
                        MoonPhase = 0.25 + (i * 0.05)
                    });
                }

                // Set up the weather service mock
                _weatherServiceMock
                    .Setup(service => service.GetForecastAsync(
                        It.Is<double>(lat => lat == latitude),
                        It.Is<double>(lon => lon == longitude),
                        It.Is<int>(d => d == days),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<WeatherForecastDto>.Success(forecastDto));

                // Create the query
                var query = new GetWeatherForecastQuery
                {
                    Latitude = latitude,
                    Longitude = longitude,
                    Days = days
                };

                // Send the query
                var result = await _mediator.Send(query);

                // Store the result
                _context.StoreResult(result);

                return result;
            }
            catch (Exception ex)
            {
                return Result<WeatherForecastDto>.Failure($"Failed to get weather forecast: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates weather data for all locations
        /// </summary>
        /// <returns>A result indicating the number of locations updated</returns>
        public async Task<Result<int>> UpdateAllWeatherAsync()
        {
            try
            {
                // Set up the weather service mock
                _weatherServiceMock
                    .Setup(service => service.UpdateAllWeatherAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<int>.Success(3)); // Assuming 3 locations were updated

                // Create the query
                var query = new UpdateAllWeatherQuery();

                // Send the query
                var result = await _mediator.Send(query);

                // Store the result
                _context.StoreResult(result);

                return result;
            }
            catch (Exception ex)
            {
                return Result<int>.Failure($"Failed to update all weather: {ex.Message}");
            }
        }

        /// <summary>
        /// Sets up existing weather data for a location
        /// </summary>
        /// <param name="locationId">The ID of the location</param>
        /// <param name="lastUpdate">The timestamp of the last update</param>
        /// <param name="isConnectivityIssue">Whether to simulate a connectivity issue</param>
        public void SetupExistingWeather(int locationId, DateTime lastUpdate, bool isConnectivityIssue = false)
        {
            try
            {
                var locationModel = _context.GetModel<LocationTestModel>($"Location_{locationId}");
                if (locationModel == null)
                {
                    locationModel = new LocationTestModel
                    {
                        Id = locationId,
                        Title = $"Location_{locationId}",
                        Latitude = 40.7128,
                        Longitude = -74.0060
                    };
                    _context.StoreModel(locationModel, $"Location_{locationId}");
                }

                // Create a weather model
                var weatherModel = new WeatherTestModel
                {
                    Id = 1,
                    LocationId = locationId,
                    Latitude = locationModel.Latitude,
                    Longitude = locationModel.Longitude,
                    Timezone = "America/New_York",
                    TimezoneOffset = -18000,
                    LastUpdate = lastUpdate,
                    Temperature = 22.5,
                    Description = "Partly cloudy",
                    Icon = "03d",
                    WindSpeed = 12.5,
                    WindDirection = 180,
                    WindGust = 15.0,
                    Humidity = 65,
                    Pressure = 1012,
                    Clouds = 40,
                    UvIndex = 6.2,
                    Precipitation = 0.1,
                    Sunrise = DateTime.Today.AddHours(6).AddMinutes(30),
                    Sunset = DateTime.Today.AddHours(19).AddMinutes(45),
                    MoonRise = DateTime.Today.AddHours(20).AddMinutes(15),
                    MoonSet = DateTime.Today.AddHours(8).AddMinutes(20),
                    MoonPhase = 0.25
                };

                // Set up the repositories
                var coordinate = new Domain.ValueObjects.Coordinate(locationModel.Latitude, locationModel.Longitude);
                var weatherEntity = new Domain.Entities.Weather(locationId, coordinate, "America/New_York", -18000);

                // Set ID and LastUpdate
                var type = typeof(Domain.Entities.Weather);
                var idProperty = type.GetProperty("Id");
                if (idProperty != null)
                {
                    idProperty.SetValue(weatherEntity, 1);
                }

                var lastUpdateProperty = type.GetProperty("LastUpdate");
                if (lastUpdateProperty != null)
                {
                    lastUpdateProperty.SetValue(weatherEntity, lastUpdate);
                }

                // Mock the weather repository
                _weatherRepositoryMock
                    .Setup(repo => repo.GetByLocationIdAsync(
                        It.Is<int>(id => id == locationId),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(weatherEntity);

                // If connectivity issue, make the weather service fail
                if (isConnectivityIssue)
                {
                    _weatherServiceMock
                        .Setup(service => service.UpdateWeatherForLocationAsync(
                            It.Is<int>(id => id == locationId),
                            It.IsAny<CancellationToken>()))
                        .ReturnsAsync(Result<WeatherDto>.Failure("API unavailable or connectivity issue"));
                }

                // Store the weather model
                _context.StoreModel(weatherModel, $"Weather_{locationId}");
                _context.StoreWeatherData(weatherModel);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting up existing weather: {ex.Message}");
            }
        }

        /// <summary>
        /// Sets up the weather API to be unavailable
        /// </summary>
        public void SetupApiUnavailable()
        {
            try
            {
                // Set up the weather service to fail with an API error
                _weatherServiceMock
                    .Setup(service => service.UpdateWeatherForLocationAsync(
                        It.IsAny<int>(),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<WeatherDto>.Failure("Weather API is temporarily unavailable"));

                _weatherServiceMock
                    .Setup(service => service.GetForecastAsync(
                        It.IsAny<double>(),
                        It.IsAny<double>(),
                        It.IsAny<int>(),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<WeatherForecastDto>.Failure("Weather API is temporarily unavailable"));

                _weatherServiceMock
                    .Setup(service => service.UpdateAllWeatherAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<int>.Failure("Weather API is temporarily unavailable"));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting up API unavailability: {ex.Message}");
            }
        }
    }
}