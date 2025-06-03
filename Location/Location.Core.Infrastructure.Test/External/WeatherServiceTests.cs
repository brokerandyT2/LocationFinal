using FluentAssertions;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Services;
using Location.Core.Application.Weather.DTOs;
using Location.Core.Infrastructure.External;
using Location.Core.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using NUnit.Framework;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Core.Infrastructure.Test.External
{
    [TestFixture]
    public class WeatherServiceTests
    {
        private WeatherService _weatherService;
        private Mock<IHttpClientFactory> _mockHttpClientFactory;
        private Mock<IUnitOfWork> _mockUnitOfWork;
        private Mock<ILogger<WeatherService>> _mockLogger;
        private Mock<IInfrastructureExceptionMappingService> _mockExceptionMapper;

        [SetUp]
        public void Setup()
        {
            _mockHttpClientFactory = new Mock<IHttpClientFactory>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockLogger = new Mock<ILogger<WeatherService>>();
            _mockExceptionMapper = new Mock<IInfrastructureExceptionMappingService>();

            _weatherService = new WeatherService(
                _mockHttpClientFactory.Object,
                _mockUnitOfWork.Object,
                _mockLogger.Object,
                _mockExceptionMapper.Object);
        }

        [Test]
        public async Task GetHourlyForecastAsync_WithValidCoordinates_ShouldReturnSuccess()
        {
            // Arrange
            var latitude = 47.6062;
            var longitude = -122.3321;

            // Setup mocks for API key and HTTP response
            SetupApiKeyMock();
            SetupHttpClientMock();

            // Act
            var result = await _weatherService.GetHourlyForecastAsync(latitude, longitude);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.HourlyForecasts.Should().NotBeEmpty();
        }

        [Test]
        public async Task GetForecastAsync_WithValidCoordinates_ShouldReturnSuccess()
        {
            // Arrange
            var latitude = 47.6062;
            var longitude = -122.3321;

            // Setup mocks
            SetupApiKeyMock();
            SetupHttpClientMock();

            // Act
            var result = await _weatherService.GetForecastAsync(latitude, longitude);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
        }

        [Test]
        public async Task UpdateWeatherForLocationAsync_WithValidLocation_ShouldReturnSuccess()
        {
            // Arrange
            var locationId = 1;
            SetupLocationMock();
            SetupApiKeyMock();
            SetupHttpClientMock();

            // Act
            var result = await _weatherService.UpdateWeatherForLocationAsync(locationId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
        }

        private void SetupApiKeyMock()
        {
            var mockSettings = new Mock<Location.Core.Application.Common.Interfaces.ISettingRepository>();
            mockSettings.Setup(x => x.GetByKeyAsync("WeatherApiKey", It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Location.Core.Domain.Entities.Setting>.Success(
                    new Location.Core.Domain.Entities.Setting("WeatherApiKey", "test-api-key", "Test API Key")));

            _mockUnitOfWork.Setup(x => x.Settings).Returns(mockSettings.Object);
        }

        private void SetupLocationMock()
        {
            var mockLocations = new Mock<Location.Core.Application.Common.Interfaces.ILocationRepository>();
            var testLocation = new Location.Core.Domain.Entities.Location(
                "Test Location",
                "Test Description",
                new Location.Core.Domain.ValueObjects.Coordinate(47.6062, -122.3321),
                new Location.Core.Domain.ValueObjects.Address("Seattle", "WA"));

            mockLocations.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Location.Core.Domain.Entities.Location>.Success(testLocation));

            _mockUnitOfWork.Setup(x => x.Locations).Returns(mockLocations.Object);
        }

        private void SetupHttpClientMock()
        {
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            var mockHttpClient = new HttpClient(mockHttpMessageHandler.Object);

            var weatherResponse = CreateMockWeatherResponse();

            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(weatherResponse)
                });

            _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
                .Returns(mockHttpClient);
        }

        private string CreateMockWeatherResponse()
        {
            return @"{
                ""lat"": 47.6062,
                ""lon"": -122.3321,
                ""timezone"": ""America/Los_Angeles"",
                ""timezone_offset"": -28800,
                ""current"": {
                    ""dt"": 1640000000,
                    ""temp"": 283.15,
                    ""weather"": [{
                        ""main"": ""Clear"",
                        ""description"": ""clear sky"",
                        ""icon"": ""01d""
                    }]
                },
                ""hourly"": [{
                    ""dt"": 1640000000,
                    ""temp"": 283.15,
                    ""feels_like"": 281.15,
                    ""weather"": [{
                        ""main"": ""Clear"",
                        ""description"": ""clear sky"",
                        ""icon"": ""01d""
                    }],
                    ""wind_speed"": 5.14,
                    ""wind_deg"": 180,
                    ""humidity"": 65,
                    ""pressure"": 1013,
                    ""clouds"": 10,
                    ""uvi"": 5.0,
                    ""pop"": 0.0,
                    ""visibility"": 10000,
                    ""dew_point"": 276.15
                }],
                ""daily"": [{
                    ""dt"": 1640000000,
                    ""temp"": {
                        ""day"": 285.15,
                        ""min"": 280.15,
                        ""max"": 290.15
                    },
                    ""weather"": [{
                        ""main"": ""Clear"",
                        ""description"": ""clear sky"",
                        ""icon"": ""01d""
                    }],
                    ""wind_speed"": 6.2,
                    ""wind_deg"": 225,
                    ""humidity"": 70,
                    ""pressure"": 1015,
                    ""clouds"": 45,
                    ""pop"": 0.0,
                    ""uvi"": 7.2,
                    ""sunrise"": 1639991234,
                    ""sunset"": 1640025678,
                    ""moonrise"": 1640012345,
                    ""moonset"": 1640056789,
                    ""moon_phase"": 0.5
                }]
            }";
        }
    }
}