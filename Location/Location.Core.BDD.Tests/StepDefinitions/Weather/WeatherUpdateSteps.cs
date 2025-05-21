using BoDi;
using FluentAssertions;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Services;
using Location.Core.Application.Weather.DTOs;
using Location.Core.BDD.Tests.Drivers;
using Location.Core.BDD.Tests.Models;
using Location.Core.BDD.Tests.Support;
using MediatR;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Assist;

namespace Location.Core.BDD.Tests.StepDefinitions.Weather
{
    [Binding]
    public class WeatherUpdateSteps
    {
        private readonly ApiContext _apiContext;
        private readonly IMediator _mediator;
        private readonly Mock<IWeatherService> _weatherServiceMock;
        private readonly Mock<ILocationRepository> _locationRepositoryMock;
        private readonly Mock<IWeatherRepository> _weatherRepositoryMock;
        private readonly WeatherDriver _weatherDriver;
        private readonly IObjectContainer _objectContainer;

        public WeatherUpdateSteps(ApiContext apiContext, IObjectContainer objectContainer)
        {
            _apiContext = apiContext ?? throw new ArgumentNullException(nameof(apiContext));
            _objectContainer = objectContainer ?? throw new ArgumentNullException(nameof(objectContainer));
            _mediator = _apiContext.GetService<IMediator>();
            _weatherServiceMock = _apiContext.GetService<Mock<IWeatherService>>();
            _locationRepositoryMock = _apiContext.GetService<Mock<ILocationRepository>>();
            _weatherRepositoryMock = _apiContext.GetService<Mock<IWeatherRepository>>();
            _weatherDriver = new WeatherDriver(_apiContext);
        }

        [AfterScenario(Order = 10000)]
        public void CleanupAfterScenario()
        {
            try
            {
                // Safe cleanup logic here
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in WeatherUpdateSteps cleanup: {ex.Message}");
            }
        }

        [Given(@"I have multiple locations stored in the system for weather:")]
        public void GivenIHaveMultipleLocationsStoredInTheSystemForWeather(Table table)
        {
            var locations = table.CreateSet<LocationTestModel>().ToList();

            // Setup mock repository once at the beginning
            var locationRepoMock = _apiContext.GetService<Mock<Application.Common.Interfaces.ILocationRepository>>();

            foreach (var location in locations)
            {
                // Ensure the location has an ID
                if (!location.Id.HasValue)
                {
                    location.Id = locations.IndexOf(location) + 1;
                }

                var domainEntity = location.ToDomainEntity();
                _apiContext.StoreModel(location, $"Location_{location.Id}");

                // Setup GetByIdAsync for each location
                locationRepoMock.Setup(repo => repo.GetByIdAsync(
                    It.Is<int>(id => id == location.Id.Value),
                    It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<Domain.Entities.Location>.Success(domainEntity));

                // Setup GetByTitleAsync
                locationRepoMock.Setup(repo => repo.GetByTitleAsync(
                    It.Is<string>(title => title == location.Title),
                    It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<Domain.Entities.Location>.Success(domainEntity));
            }

            // Setup GetAllAsync
            locationRepoMock.Setup(repo => repo.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<List<Domain.Entities.Location>>.Success(
                    locations.Select(l => l.ToDomainEntity()).ToList()));

            // Setup GetActiveAsync (non-deleted locations)
            var activeLocations = locations.Where(l => !l.IsDeleted).ToList();
            locationRepoMock.Setup(repo => repo.GetActiveAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<List<Domain.Entities.Location>>.Success(
                    activeLocations.Select(l => l.ToDomainEntity()).ToList()));

            _apiContext.StoreModel(locations, "AllLocations");
        }

        [Given(@"I have a location with existing weather data from (.*) hours ago")]
        public void GivenIHaveALocationWithExistingWeatherDataFromHoursAgo(int hours)
        {
            // Get first location or create a new one if none exists
            var location = _apiContext.GetModel<List<LocationTestModel>>("AllLocations")?.FirstOrDefault()
                ?? new LocationTestModel { Id = 1, Title = "Test Location", Latitude = 40.7128, Longitude = -74.0060 };

            // Setup existing weather data with a timestamp from specified hours ago
            _weatherDriver.SetupExistingWeather(location.Id.Value, DateTime.UtcNow.AddHours(-hours));
        }

        [Given(@"the ""(.*)"" location has recent weather data")]
        public void GivenTheLocationHasRecentWeatherData(string locationTitle)
        {
            // Find location by title
            var locations = _apiContext.GetModel<List<LocationTestModel>>("AllLocations");
            var location = locations?.FirstOrDefault(l => l.Title == locationTitle);
            location.Should().NotBeNull($"Location with title '{locationTitle}' should exist");

            // Setup recent weather data (1 hour ago)
            _weatherDriver.SetupExistingWeather(location.Id.Value, DateTime.UtcNow.AddHours(-1));
        }

        [Given(@"the weather API is unavailable")]
        public void GivenTheWeatherAPIIsUnavailable()
        {
            _weatherDriver.SetupApiUnavailable();
        }

        [Given(@"the weather API returns an error for the location")]
        public void GivenTheWeatherAPIReturnsAnErrorForTheLocation()
        {
            // Get first location
            var location = _apiContext.GetModel<List<LocationTestModel>>("AllLocations")?.FirstOrDefault();
            location.Should().NotBeNull("At least one location should be available");

            // Setup connectivity issue for this location
            _weatherDriver.SetupExistingWeather(location.Id.Value, DateTime.UtcNow.AddHours(-24), true);
        }

        [When(@"I update the weather data for location ""(.*)""")]
        public async Task WhenIUpdateTheWeatherDataForLocation(string locationTitle)
        {
            // Find location by title
            var locations = _apiContext.GetModel<List<LocationTestModel>>("AllLocations");
            var location = locations?.FirstOrDefault(l => l.Title == locationTitle);
            location.Should().NotBeNull($"Location with title '{locationTitle}' should exist");

            // Update weather for this location
            await _weatherDriver.UpdateWeatherForLocationAsync(location.Id.Value, false);
        }

        [When(@"I force update the weather data for ""(.*)""")]
        public async Task WhenIForceUpdateTheWeatherDataFor(string locationTitle)
        {
            // Find location by title
            var locations = _apiContext.GetModel<List<LocationTestModel>>("AllLocations");
            var location = locations?.FirstOrDefault(l => l.Title == locationTitle);
            location.Should().NotBeNull($"Location with title '{locationTitle}' should exist");

            // Force update weather for this location
            await _weatherDriver.UpdateWeatherForLocationAsync(location.Id.Value, true);
        }

        [When(@"I update the weather data for the location")]
        public async Task WhenIUpdateTheWeatherDataForTheLocation()
        {
            // Get first location
            var location = _apiContext.GetModel<List<LocationTestModel>>("AllLocations")?.FirstOrDefault();
            location.Should().NotBeNull("At least one location should be available");

            // Update weather for this location
            await _weatherDriver.UpdateWeatherForLocationAsync(location.Id.Value, false);
        }

        [When(@"I update weather data for all locations")]
        public async Task WhenIUpdateWeatherDataForAllLocations()
        {
            await _weatherDriver.UpdateAllWeatherAsync();
        }

        [Then(@"I should receive updated weather data")]
        public void ThenIShouldReceiveUpdatedWeatherData()
        {
            var weatherResult = _apiContext.GetLastResult<WeatherDto>();
            weatherResult.Should().NotBeNull("Weather result should be available");
            weatherResult.IsSuccess.Should().BeTrue("Weather update operation should be successful");
            weatherResult.Data.Should().NotBeNull("Weather data should be available");

            // Check if the last update time is recent (within the last 5 minutes)
            var now = DateTime.UtcNow;
            var timeDifference = now - weatherResult.Data.LastUpdate;
            timeDifference.TotalMinutes.Should().BeLessThan(5, "Weather data should be recently updated");
        }

        [Then(@"I should receive a successful result")]
        public void ThenIShouldReceiveASuccessfulResult()
        {
            var weatherResult = _apiContext.GetLastResult<WeatherDto>();
            weatherResult.Should().NotBeNull("Weather result should be available");
            weatherResult.IsSuccess.Should().BeTrue("Weather update operation should be successful");
        }

        [Then(@"the weather data should be updated")]
        public void ThenTheWeatherDataShouldBeUpdated()
        {
            var weatherResult = _apiContext.GetLastResult<WeatherDto>();
            weatherResult.Should().NotBeNull("Weather result should be available");
            weatherResult.IsSuccess.Should().BeTrue("Weather update operation should be successful");
            weatherResult.Data.Should().NotBeNull("Weather data should be available");
        }

        [Then(@"the last update timestamp should be recent")]
        public void ThenTheLastUpdateTimestampShouldBeRecent()
        {
            var weatherResult = _apiContext.GetLastResult<WeatherDto>();
            weatherResult.Should().NotBeNull("Weather result should be available");
            weatherResult.Data.Should().NotBeNull("Weather data should be available");

            // Check if the last update time is recent (within the last 5 minutes)
            var now = DateTime.UtcNow;
            var timeDifference = now - weatherResult.Data.LastUpdate;
            timeDifference.TotalMinutes.Should().BeLessThan(5, "Weather data should be recently updated");
        }

        [Then(@"I should not receive updated weather data")]
        public void ThenIShouldNotReceiveUpdatedWeatherData()
        {
            var weatherResult = _apiContext.GetLastResult<WeatherDto>();
            weatherResult.Should().NotBeNull("Weather result should be available");

            // Either the operation failed or the data is old
            if (weatherResult.IsSuccess)
            {
                // If successful, check that the last update time is not recent
                var now = DateTime.UtcNow;
                var timeDifference = now - weatherResult.Data.LastUpdate;
                timeDifference.TotalMinutes.Should().BeGreaterThan(10, "Weather data should not be recently updated");
            }
            else
            {
                // If failed, that's expected
                weatherResult.IsSuccess.Should().BeFalse("Weather update operation should fail");
            }
        }

        [Then(@"I should receive an error related to API unavailability")]
        public void ThenIShouldReceiveAnErrorRelatedToAPIUnavailability()
        {
            var weatherResult = _apiContext.GetLastResult<WeatherDto>();
            weatherResult.Should().NotBeNull("Weather result should be available");
            weatherResult.IsSuccess.Should().BeFalse("Weather update operation should fail");
            weatherResult.ErrorMessage.Should().NotBeNullOrEmpty("Error message should be provided");
            weatherResult.ErrorMessage.Should().Contain("API", "Error should mention API");
        }

        [Then(@"the weather data should include the current temperature")]
        public void ThenTheWeatherDataShouldIncludeTheCurrentTemperature()
        {
            var weatherResult = _apiContext.GetLastResult<WeatherDto>();
            weatherResult.Should().NotBeNull("Weather result should be available");
            weatherResult.IsSuccess.Should().BeTrue("Weather update operation should be successful");
            weatherResult.Data.Should().NotBeNull("Weather data should be available");

            // Temperature should be available and in a reasonable range
            weatherResult.Data.Temperature.Should().NotBe(0, "Temperature should be set to a non-zero value");
        }

        [Then(@"the weather data should include the current wind information")]
        public void ThenTheWeatherDataShouldIncludeTheCurrentWindInformation()
        {
            var weatherResult = _apiContext.GetLastResult<WeatherDto>();
            weatherResult.Should().NotBeNull("Weather result should be available");
            weatherResult.IsSuccess.Should().BeTrue("Weather update operation should be successful");
            weatherResult.Data.Should().NotBeNull("Weather data should be available");

            // Wind information should be available
            weatherResult.Data.WindSpeed.Should().BeGreaterOrEqualTo(0, "Wind speed should be available");
            weatherResult.Data.WindDirection.Should().BeInRange(0, 360, "Wind direction should be in a valid range (0-360)");
        }

        [Then(@"the weather data should include a description")]
        public void ThenTheWeatherDataShouldIncludeADescription()
        {
            var weatherResult = _apiContext.GetLastResult<WeatherDto>();
            weatherResult.Should().NotBeNull("Weather result should be available");
            weatherResult.IsSuccess.Should().BeTrue("Weather update operation should be successful");
            weatherResult.Data.Should().NotBeNull("Weather data should be available");

            // Description should be non-empty
            weatherResult.Data.Description.Should().NotBeNullOrEmpty("Weather description should be available");
        }

        [Then(@"the update operation should report (.*) updated locations")]
        public void ThenTheUpdateOperationShouldReportUpdatedLocations(int expectedCount)
        {
            var updateResult = _apiContext.GetLastResult<int>();
            updateResult.Should().NotBeNull("Update result should be available");
            updateResult.IsSuccess.Should().BeTrue("Update operation should be successful");
            updateResult.Data.Should().Be(expectedCount, $"Operation should report {expectedCount} updated locations");
        }

        [Then(@"the operation should fail with an error message")]
        public void ThenTheOperationShouldFailWithAnErrorMessage()
        {
            var result = _apiContext.GetLastResult<object>();
            result.Should().NotBeNull("Result should be available");
            result.IsSuccess.Should().BeFalse("Operation should fail");
            result.ErrorMessage.Should().NotBeNullOrEmpty("Error message should be provided");
        }

        [Then(@"the weather data should include sunrise and sunset times")]
        public void ThenTheWeatherDataShouldIncludeSunriseAndSunsetTimes()
        {
            var weatherResult = _apiContext.GetLastResult<WeatherDto>();
            weatherResult.Should().NotBeNull("Weather result should be available");
            weatherResult.IsSuccess.Should().BeTrue("Weather update operation should be successful");
            weatherResult.Data.Should().NotBeNull("Weather data should be available");

            // Sunrise and sunset times should be present and sensible
            weatherResult.Data.Sunrise.Should().NotBe(default(DateTime), "Sunrise time should be set");
            weatherResult.Data.Sunset.Should().NotBe(default(DateTime), "Sunset time should be set");
            weatherResult.Data.Sunrise.Should().BeBefore(weatherResult.Data.Sunset, "Sunrise should be before sunset");
        }

        [Then(@"the weather data should indicate the timezone ""(.*)""")]
        public void ThenTheWeatherDataShouldIndicateTheTimezone(string expectedTimezone)
        {
            var weatherResult = _apiContext.GetLastResult<WeatherDto>();
            weatherResult.Should().NotBeNull("Weather result should be available");
            weatherResult.IsSuccess.Should().BeTrue("Weather update operation should be successful");
            weatherResult.Data.Should().NotBeNull("Weather data should be available");

            // Timezone information should match expected value
            weatherResult.Data.Timezone.Should().Be(expectedTimezone, $"Timezone should be '{expectedTimezone}'");
        }
    }
}