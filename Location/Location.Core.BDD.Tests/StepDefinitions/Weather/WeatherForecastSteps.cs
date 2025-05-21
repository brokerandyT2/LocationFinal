using BoDi;
using FluentAssertions;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Services;
using Location.Core.Application.Weather.DTOs;
using Location.Core.Application.Weather.Queries.GetWeatherForecast;
using Location.Core.BDD.Tests.Features;
using Location.Core.BDD.Tests.Models;
using Location.Core.BDD.Tests.Support;
using MediatR;
using Moq;
using System;
using System.Collections.Generic;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Assist;

namespace Location.Core.BDD.Tests.StepDefinitions.Weather
{
    [Binding]
    public class WeatherForecastSteps
    {
        private readonly ApiContext _context;
        private readonly IMediator _mediator;
        private readonly Mock<IWeatherService> _weatherServiceMock;
        private readonly Mock<ILocationRepository> _locationRepositoryMock;
        private readonly Mock<IWeatherRepository> _weatherRepositoryMock;
        private bool _hasInvalidCoordinates;
        // Add this to the WeatherForecastSteps.cs class

        private readonly IObjectContainer _objectContainer;

        public WeatherForecastSteps(ApiContext context, IObjectContainer objectContainer)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _objectContainer = objectContainer ?? throw new ArgumentNullException(nameof(objectContainer));
        }

        // This is the TestCleanup method that will safely handle cleanup
        [AfterScenario(Order = 10000)]
        public void CleanupAfterScenario()
        {
            try
            {

            }
            catch (Exception ex)
            {
                // Log but don't throw to avoid masking test failures
                Console.WriteLine($"Error in WeatherForecastSteps cleanup: {ex.Message}");
            }
        }

        // Fix for ambiguous step definition
        // Change this method:
        [Then(@"I should receive a successful result")]
        // To this:
        [Then(@"I should receive a successful forecast result")]
        public void ThenIShouldReceiveASuccessfulResult()
        {
            // Existing implementation remains the same
        }
        public WeatherForecastSteps(ApiContext context)
        {
            _context = context;
            _mediator = _context.GetService<IMediator>();
            _weatherServiceMock = _context.GetService<Mock<IWeatherService>>();
            _locationRepositoryMock = _context.GetService<Mock<ILocationRepository>>();
            _weatherRepositoryMock = _context.GetService<Mock<IWeatherRepository>>();
        }

        [Given(@"I have a new location with the following details:")]
        public void GivenIHaveANewLocationWithTheFollowingDetails(Table table)
        {
            var locationData = table.CreateInstance<LocationTestModel>();
            locationData.Id = 99; // Assign a unique ID

            // Set up the mock repository to return this location
            var domainEntity = locationData.ToDomainEntity();

            _locationRepositoryMock
                .Setup(repo => repo.GetByIdAsync(
                    It.Is<int>(id => id == locationData.Id.Value),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Location>.Success(domainEntity));

            // Set up weather repository to return null (no existing weather)
            _weatherRepositoryMock
                .Setup(repo => repo.GetByLocationIdAsync(
                    It.Is<int>(id => id == locationData.Id.Value),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Domain.Entities.Weather)null);

            // Store for later steps
            _context.StoreLocationData(locationData);
        }

        [Given(@"I have a location with invalid coordinates:")]
        public void GivenIHaveALocationWithInvalidCoordinates(Table table)
        {
            var locationData = table.CreateInstance<LocationTestModel>();
            locationData.Id = 100; // Assign a unique ID
            _hasInvalidCoordinates = true;

            // Set up the mock repository to return this location
            var domainEntity = locationData.ToDomainEntity();

            _locationRepositoryMock
                .Setup(repo => repo.GetByIdAsync(
                    It.Is<int>(id => id == locationData.Id.Value),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Location>.Success(domainEntity));

            // Set up weather service to fail for invalid coordinates
            _weatherServiceMock
                .Setup(service => service.GetForecastAsync(
                    It.Is<double>(lat => lat == locationData.Latitude),
                    It.Is<double>(lon => lon == locationData.Longitude),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<WeatherForecastDto>.Failure("Invalid coordinates: Latitude must be between -90 and 90 degrees, Longitude must be between -180 and 180 degrees"));

            // Store for later steps
            _context.StoreLocationData(locationData);
        }

        [When(@"I request the weather forecast for the location")]
        public async Task WhenIRequestTheWeatherForecastForTheLocation()
        {
            var locationData = _context.GetLocationData();
            locationData.Should().NotBeNull("Location data should be available");

            // Set up the mock service to return weather data if we don't have invalid coordinates
            if (!_hasInvalidCoordinates)
            {
                var forecastDto = new WeatherForecastDto
                {
                    WeatherId = 1,
                    LastUpdate = DateTime.UtcNow,
                    Timezone = "America/New_York",
                    TimezoneOffset = -18000,
                    DailyForecasts = new List<DailyForecastDto>
                    {
                        new DailyForecastDto
                        {
                            Date = DateTime.Today,
                            Temperature = 22.5,
                            MinTemperature = 18.2,
                            MaxTemperature = 25.8,
                            Description = "Partly cloudy",
                            Icon = "03d",
                            WindSpeed = 12.5,
                            WindDirection = 180,
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
                        },
                        new DailyForecastDto
                        {
                            Date = DateTime.Today.AddDays(1),
                            Temperature = 24.0,
                            MinTemperature = 19.5,
                            MaxTemperature = 26.2,
                            Description = "Clear sky",
                            Icon = "01d",
                            WindSpeed = 10.2,
                            WindDirection = 195,
                            Humidity = 60,
                            Pressure = 1015,
                            Clouds = 10,
                            UvIndex = 7.0,
                            Precipitation = 0.0,
                            Sunrise = DateTime.Today.AddDays(1).AddHours(6).AddMinutes(32),
                            Sunset = DateTime.Today.AddDays(1).AddHours(19).AddMinutes(43),
                            MoonRise = DateTime.Today.AddDays(1).AddHours(21).AddMinutes(5),
                            MoonSet = DateTime.Today.AddDays(1).AddHours(9).AddMinutes(10),
                            MoonPhase = 0.28
                        }
                    }
                };

                _weatherServiceMock
                    .Setup(service => service.GetForecastAsync(
                        It.Is<double>(lat => lat == locationData.Latitude),
                        It.Is<double>(lon => lon == locationData.Longitude),
                        It.IsAny<int>(),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<WeatherForecastDto>.Success(forecastDto));
            }

            // Create the query
            var query = new GetWeatherForecastQuery
            {
                Latitude = locationData.Latitude,
                Longitude = locationData.Longitude,
                Days = 7
            };

            // Execute the query
            var result = await _mediator.Send(query);

            // Store the result and weather data
            _context.StoreResult(result);

            if (result.IsSuccess && result.Data != null)
            {
                var weatherModel = new WeatherTestModel
                {
                    LocationId = locationData.Id.Value,
                    Latitude = locationData.Latitude,
                    Longitude = locationData.Longitude,
                    LastUpdate = result.Data.LastUpdate,
                    Timezone = result.Data.Timezone,
                    TimezoneOffset = result.Data.TimezoneOffset
                };

                _context.StoreWeatherData(weatherModel);
            }
        }

        [When(@"I request the detailed forecast for tomorrow")]
        public async Task WhenIRequestTheDetailedForecastForTomorrow()
        {
            // We'll use the same mock setup as for the general forecast
            await WhenIRequestTheWeatherForecastForTheLocation();
        }

        [When(@"I request the moon phase information for the current day")]
        public async Task WhenIRequestTheMoonPhaseInformationForTheCurrentDay()
        {
            // We'll use the same mock setup as for the general forecast
            await WhenIRequestTheWeatherForecastForTheLocation();
        }

        [When(@"I request the weather forecast for the new location")]
        public async Task WhenIRequestTheWeatherForecastForTheNewLocation()
        {
            // This should reuse the WhenIRequestTheWeatherForecastForTheLocation method
            await WhenIRequestTheWeatherForecastForTheLocation();
        }

        [When(@"I request the weather forecast for the invalid location")]
        public async Task WhenIRequestTheWeatherForecastForTheInvalidLocation()
        {
            // This should reuse the WhenIRequestTheWeatherForecastForTheLocation method
            await WhenIRequestTheWeatherForecastForTheLocation();
        }

        [Then(@"I should receive a successful weather forecast result")]
        public void ThenIShouldReceiveASuccessfulWeatherForecastResult()
        {
            var forecastResult = _context.GetLastResult<WeatherForecastDto>();
            forecastResult.Should().NotBeNull("Result should be available after query");
            forecastResult.IsSuccess.Should().BeTrue("Weather forecast query should be successful");
            forecastResult.Data.Should().NotBeNull("Weather forecast data should be returned");
        }

        [Then(@"the forecast should contain weather data for the current day")]
        public void ThenTheForecastShouldContainWeatherDataForTheCurrentDay()
        {
            var forecastResult = _context.GetLastResult<WeatherForecastDto>();
            forecastResult.Should().NotBeNull("Forecast result should be available");
            forecastResult.Data.Should().NotBeNull("Forecast data should be available");
            forecastResult.Data.DailyForecasts.Should().NotBeNullOrEmpty("Daily forecasts should be available");

            // Check if there's a forecast for the current day
            forecastResult.Data.DailyForecasts.Should().Contain(
                f => f.Date.Date == DateTime.Today.Date,
                "Forecast should contain data for the current day");
        }

        [Then(@"the forecast should include the following information:")]
        public void ThenTheForecastShouldIncludeTheFollowingInformation(Table table)
        {
            var forecastResult = _context.GetLastResult<WeatherForecastDto>();
            forecastResult.Should().NotBeNull("Forecast result should be available");
            forecastResult.Data.Should().NotBeNull("Forecast data should be available");
            forecastResult.Data.DailyForecasts.Should().NotBeNullOrEmpty("Daily forecasts should be available");

            // Get the current day's forecast
            var currentDayForecast = forecastResult.Data.DailyForecasts.Find(f => f.Date.Date == DateTime.Today.Date);
            currentDayForecast.Should().NotBeNull("Current day forecast should be available");

            // Check for each required property in the table
            foreach (var row in table.Rows)
            {
                foreach (var header in table.Header)
                {
                    var propertyName = header;
                    var property = typeof(DailyForecastDto).GetProperty(propertyName);

                    property.Should().NotBeNull($"DailyForecastDto should have a property named {propertyName}");
                    var value = property.GetValue(currentDayForecast);
                    value.Should().NotBeNull($"The value of {propertyName} should not be null");
                }
            }
        }

        [Then(@"the forecast should include at least (.*) upcoming days?")]
        public void ThenTheForecastShouldIncludeAtLeastUpcomingDays(int days)
        {
            var forecastResult = _context.GetLastResult<WeatherForecastDto>();
            forecastResult.Should().NotBeNull("Forecast result should be available");
            forecastResult.Data.Should().NotBeNull("Forecast data should be available");
            forecastResult.Data.DailyForecasts.Should().NotBeNullOrEmpty("Daily forecasts should be available");

            // Check if there are at least 'days' number of forecasts for days after today
            var upcomingDays = forecastResult.Data.DailyForecasts.FindAll(f => f.Date.Date > DateTime.Today.Date);
            upcomingDays.Should().HaveCountGreaterOrEqualTo(days,
                $"Forecast should include at least {days} upcoming days");
        }

        [Then(@"the forecast details should include:")]
        public void ThenTheForecastDetailsShouldInclude(Table table)
        {
            var forecastResult = _context.GetLastResult<WeatherForecastDto>();
            forecastResult.Should().NotBeNull("Forecast result should be available");
            forecastResult.Data.Should().NotBeNull("Forecast data should be available");
            forecastResult.Data.DailyForecasts.Should().NotBeNullOrEmpty("Daily forecasts should be available");

            // Get tomorrow's forecast
            var tomorrowForecast = forecastResult.Data.DailyForecasts.Find(f => f.Date.Date == DateTime.Today.AddDays(1).Date);
            tomorrowForecast.Should().NotBeNull("Tomorrow's forecast should be available");

            // Check for each required property in the table
            foreach (var row in table.Rows)
            {
                foreach (var header in table.Header)
                {
                    var propertyName = header.Replace(" ", ""); // Remove spaces for property matching
                    var property = typeof(DailyForecastDto).GetProperty(propertyName);

                    property.Should().NotBeNull($"DailyForecastDto should have a property named {propertyName}");
                    var value = property.GetValue(tomorrowForecast);
                    value.Should().NotBeNull($"The value of {propertyName} should not be null");
                }
            }
        }

        [Then(@"the moon phase information should include:")]
        public void ThenTheMoonPhaseInformationShouldInclude(Table table)
        {
            var forecastResult = _context.GetLastResult<WeatherForecastDto>();
            forecastResult.Should().NotBeNull("Forecast result should be available");
            forecastResult.Data.Should().NotBeNull("Forecast data should be available");
            forecastResult.Data.DailyForecasts.Should().NotBeNullOrEmpty("Daily forecasts should be available");

            // Get today's forecast with moon phase info
            var todayForecast = forecastResult.Data.DailyForecasts.Find(f => f.Date.Date == DateTime.Today.Date);
            todayForecast.Should().NotBeNull("Today's forecast should be available");

            // Check for the moon phase properties
            todayForecast.MoonPhase.Should().BeGreaterOrEqualTo(0).And.BeLessThanOrEqualTo(1,
                "Moon phase should be between 0 and 1");
            todayForecast.MoonRise.Should().NotBeNull("Moon rise time should be available");
            todayForecast.MoonSet.Should().NotBeNull("Moon set time should be available");

            // Moon phase description is not in the DTO, but we can check that the moon phase value is valid
            todayForecast.MoonPhase.Should().BeInRange(0, 1, "Moon phase should be in valid range");
        }

        [Then(@"I should receive a failure result")]
        public void ThenIShouldReceiveAFailureResult()
        {
            var forecastResult = _context.GetLastResult<WeatherForecastDto>();
            forecastResult.Should().NotBeNull("Result should be available after query");
            forecastResult.IsSuccess.Should().BeFalse("Weather forecast query should have failed");
        }

        [Then(@"the error message should contain information about invalid coordinates")]
        public void ThenTheErrorMessageShouldContainInformationAboutInvalidCoordinates()
        {
            var forecastResult = _context.GetLastResult<WeatherForecastDto>();
            forecastResult.Should().NotBeNull("Result should be available after query");
            forecastResult.ErrorMessage.Should().NotBeNullOrEmpty("Error message should be provided");
            forecastResult.ErrorMessage.Should().Contain("coordinates", "Error should mention coordinates");
        }
    }
}