using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Services;
using Location.Core.Application.Weather.DTOs;
using Location.Core.Domain.Entities;
using Location.Core.Domain.ValueObjects;
using Location.Core.Infrastructure.External;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Location.Core.Infrastructure.Test.External
{
    [TestFixture]
    public class WeatherServiceTests
    {
        private Mock<IUnitOfWork> _unitOfWorkMock;
        private Mock<Location.Core.Application.Common.Interfaces.IWeatherRepository> _weatherRepositoryMock;
        private Mock<Location.Core.Application.Common.Interfaces.ILocationRepository> _locationRepositoryMock;
        private Mock<Location.Core.Application.Common.Interfaces.ISettingRepository> _settingRepositoryMock;
        private Mock<IHttpClientFactory> _httpClientFactoryMock;
        private Mock<ILogger<WeatherService>> _loggerMock;
        private WeatherService _weatherService;

        [SetUp]
        public void Setup()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _weatherRepositoryMock = new Mock<Location.Core.Application.Common.Interfaces.IWeatherRepository>();
            _locationRepositoryMock = new Mock<Location.Core.Application.Common.Interfaces.ILocationRepository>();
            _settingRepositoryMock = new Mock<Location.Core.Application.Common.Interfaces.ISettingRepository>();
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _loggerMock = new Mock<ILogger<WeatherService>>();

            _unitOfWorkMock.Setup(u => u.Weather).Returns(_weatherRepositoryMock.Object);
            _unitOfWorkMock.Setup(u => u.Locations).Returns(_locationRepositoryMock.Object);
            _unitOfWorkMock.Setup(u => u.Settings).Returns(_settingRepositoryMock.Object);

            _weatherService = new WeatherService(
                _httpClientFactoryMock.Object,
                _unitOfWorkMock.Object,
                _loggerMock.Object);
        }

        [Test]
        public async Task GetWeatherAsync_WithValidApiKey_ReturnsWeatherData()
        {
            // Arrange
            var latitude = 40.7128;
            var longitude = -74.0060;
            var apiKey = "test-api-key";

            var setting = new Setting("WeatherApiKey", apiKey);
            _settingRepositoryMock
                .Setup(x => x.GetByKeyAsync("WeatherApiKey", It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Setting>.Success(setting));

            var mockHttpClient = new TestHttpClient(@"
            {
                ""lat"": 40.7128,
                ""lon"": -74.0060,
                ""timezone"": ""America/New_York"",
                ""timezone_offset"": -14400,
                ""current"": {
                    ""dt"": 1646870400,
                    ""sunrise"": 1646831040,
                    ""sunset"": 1646872620,
                    ""temp"": 20.5,
                    ""feels_like"": 19.8,
                    ""pressure"": 1015,
                    ""humidity"": 65,
                    ""dew_point"": 13.4,
                    ""uvi"": 5.2,
                    ""clouds"": 40,
                    ""visibility"": 10000,
                    ""wind_speed"": 12.5,
                    ""wind_deg"": 180,
                    ""wind_gust"": 15.8,
                    ""weather"": [{
                        ""id"": 802,
                        ""main"": ""Clouds"",
                        ""description"": ""scattered clouds"",
                        ""icon"": ""03d""
                    }]
                },
                ""daily"": []
            }");

            _httpClientFactoryMock
                .Setup(x => x.CreateClient(It.IsAny<string>()))
                .Returns(mockHttpClient);

            // Act
            var result = await _weatherService.GetWeatherAsync(latitude, longitude);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Temperature.Should().Be(20.5);
            result.Data.Description.Should().Be("scattered clouds");
            result.Data.Icon.Should().Be("03d");
            result.Data.WindSpeed.Should().Be(12.5);
            result.Data.WindDirection.Should().Be(180);
            result.Data.Humidity.Should().Be(65);
        }

        [Test]
        public async Task UpdateWeatherForLocationAsync_WithExistingWeather_UpdatesWeather()
        {
            // Arrange
            var locationId = 1;
            var location = new Location.Core.Domain.Entities.Location(
                "Test Location",
                "Test Description",
                new Coordinate(40.7128, -74.0060),
                new Address("New York", "NY"));

            _locationRepositoryMock
                .Setup(x => x.GetByIdAsync(locationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Location.Core.Domain.Entities.Location>.Success(location));

            var existingWeather = new Weather(
                locationId,
                new Coordinate(40.7128, -74.0060),
                "America/New_York",
                -14400);

            // Return Weather directly, not Result<Weather>
            _weatherRepositoryMock
                .Setup(x => x.GetByLocationIdAsync(locationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingWeather);

            var setting = new Setting("WeatherApiKey", "test-api-key");
            _settingRepositoryMock
                .Setup(x => x.GetByKeyAsync("WeatherApiKey", It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Setting>.Success(setting));

            var mockHttpClient = new TestHttpClient(@"
            {
                ""lat"": 40.7128,
                ""lon"": -74.0060,
                ""timezone"": ""America/New_York"",
                ""timezone_offset"": -14400,
                ""current"": {
                    ""dt"": 1646870400,
                    ""sunrise"": 1646831040,
                    ""sunset"": 1646872620,
                    ""temp"": 20.5,
                    ""feels_like"": 19.8,
                    ""pressure"": 1015,
                    ""humidity"": 65,
                    ""dew_point"": 13.4,
                    ""uvi"": 5.2,
                    ""clouds"": 40,
                    ""visibility"": 10000,
                    ""wind_speed"": 12.5,
                    ""wind_deg"": 180,
                    ""wind_gust"": 15.8,
                    ""weather"": [{
                        ""id"": 802,
                        ""main"": ""Clouds"",
                        ""description"": ""scattered clouds"",
                        ""icon"": ""03d""
                    }]
                },
                ""daily"": []
            }");

            _httpClientFactoryMock
                .Setup(x => x.CreateClient(It.IsAny<string>()))
                .Returns(mockHttpClient);

            // Act
            var result = await _weatherService.UpdateWeatherForLocationAsync(locationId);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            _weatherRepositoryMock.Verify(x => x.Update(It.IsAny<Weather>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }


        [Test]
        public async Task UpdateWeatherForLocationAsync_WithNewWeather_AddsWeather()
        {
            // Arrange
            var locationId = 1;
            var location = new Location.Core.Domain.Entities.Location(
                "Test Location",
                "Test Description",
                new Coordinate(40.7128, -74.0060),
                new Address("New York", "NY"));

            _locationRepositoryMock
                .Setup(x => x.GetByIdAsync(locationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Location.Core.Domain.Entities.Location>.Success(location));

            // Return null for "not found" scenario
            _weatherRepositoryMock
                .Setup(x => x.GetByLocationIdAsync(locationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Weather)null);

            var setting = new Setting("WeatherApiKey", "test-api-key");
            _settingRepositoryMock
                .Setup(x => x.GetByKeyAsync("WeatherApiKey", It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Setting>.Success(setting));

            var mockHttpClient = new TestHttpClient(@"
            {
                ""lat"": 40.7128,
                ""lon"": -74.0060,
                ""timezone"": ""America/New_York"",
                ""timezone_offset"": -14400,
                ""current"": {
                    ""dt"": 1646870400,
                    ""sunrise"": 1646831040,
                    ""sunset"": 1646872620,
                    ""temp"": 20.5,
                    ""feels_like"": 19.8,
                    ""pressure"": 1015,
                    ""humidity"": 65,
                    ""dew_point"": 13.4,
                    ""uvi"": 5.2,
                    ""clouds"": 40,
                    ""visibility"": 10000,
                    ""wind_speed"": 12.5,
                    ""wind_deg"": 180,
                    ""wind_gust"": 15.8,
                    ""weather"": [{
                        ""id"": 802,
                        ""main"": ""Clouds"",
                        ""description"": ""scattered clouds"",
                        ""icon"": ""03d""
                    }]
                },
                ""daily"": []
            }");

            _httpClientFactoryMock
                .Setup(x => x.CreateClient(It.IsAny<string>()))
                .Returns(mockHttpClient);

            // Act
            var result = await _weatherService.UpdateWeatherForLocationAsync(locationId);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            _weatherRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Weather>(), It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task GetWeatherAsync_WithNoApiKey_ReturnsFailure()
        {
            // Arrange
            var latitude = 40.7128;
            var longitude = -74.0060;

            _settingRepositoryMock
                .Setup(x => x.GetByKeyAsync("WeatherApiKey", It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Setting>.Failure("Setting not found"));

            // Act
            var result = await _weatherService.GetWeatherAsync(latitude, longitude);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Weather API key not configured");
        }

        [Test]
        public async Task UpdateAllWeatherAsync_ProcessesAllActiveLocations()
        {
            // Arrange
            var locations = new List<Location.Core.Domain.Entities.Location>
            {
                new Location.Core.Domain.Entities.Location("Location 1", "Desc 1", new Coordinate(40.7128, -74.0060), new Address("New York", "NY")),
                new Location.Core.Domain.Entities.Location("Location 2", "Desc 2", new Coordinate(34.0522, -118.2437), new Address("Los Angeles", "CA"))
            };

            _locationRepositoryMock
                .Setup(x => x.GetActiveAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<List<Location.Core.Domain.Entities.Location>>.Success(locations));

            var setting = new Setting("WeatherApiKey", "test-api-key");
            _settingRepositoryMock
                .Setup(x => x.GetByKeyAsync("WeatherApiKey", It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Setting>.Success(setting));

            var mockHttpClient = new TestHttpClient(@"
            {
                ""lat"": 40.7128,
                ""lon"": -74.0060,
                ""timezone"": ""America/New_York"",
                ""timezone_offset"": -14400,
                ""current"": {
                    ""dt"": 1646870400,
                    ""sunrise"": 1646831040,
                    ""sunset"": 1646872620,
                    ""temp"": 20.5,
                    ""feels_like"": 19.8,
                    ""pressure"": 1015,
                    ""humidity"": 65,
                    ""dew_point"": 13.4,
                    ""uvi"": 5.2,
                    ""clouds"": 40,
                    ""visibility"": 10000,
                    ""wind_speed"": 12.5,
                    ""wind_deg"": 180,
                    ""weather"": [{
                        ""id"": 802,
                        ""main"": ""Clouds"",
                        ""description"": ""scattered clouds"",
                        ""icon"": ""03d""
                    }]
                },
                ""daily"": []
            }");

            _httpClientFactoryMock
                .Setup(x => x.CreateClient(It.IsAny<string>()))
                .Returns(mockHttpClient);

            // Act
            var result = await _weatherService.UpdateAllWeatherAsync();

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be(2); // Both locations processed successfully
        }
    }

    // Test helper class
    internal class TestHttpClient : HttpClient
    {
        private readonly string _response;

        public TestHttpClient(string response)
        {
            _response = response;
        }

        public override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(_response)
            };
            return Task.FromResult(response);
        }
    }
}