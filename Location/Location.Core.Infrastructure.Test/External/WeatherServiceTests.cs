using FluentAssertions;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Domain.Entities;
using Location.Core.Domain.ValueObjects;
using Location.Core.Infrastructure.External;
using Location.Core.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using NUnit.Framework;
using System.Net;

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
        private Mock<ISettingRepository> _mockSettingRepository;
        private Mock<ILocationRepository> _mockLocationRepository;
        private Mock<IWeatherRepository> _mockWeatherRepository;

        [SetUp]
        public void Setup()
        {
            _mockHttpClientFactory = new Mock<IHttpClientFactory>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockLogger = new Mock<ILogger<WeatherService>>();
            _mockExceptionMapper = new Mock<IInfrastructureExceptionMappingService>();
            _mockSettingRepository = new Mock<ISettingRepository>();
            _mockLocationRepository = new Mock<ILocationRepository>();
            _mockWeatherRepository = new Mock<IWeatherRepository>();

            // Setup UnitOfWork to return our mocked repositories
            _mockUnitOfWork.Setup(x => x.Settings).Returns(_mockSettingRepository.Object);
            _mockUnitOfWork.Setup(x => x.Locations).Returns(_mockLocationRepository.Object);
            _mockUnitOfWork.Setup(x => x.Weather).Returns(_mockWeatherRepository.Object);
            _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

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

            SetupApiKeyMock();
            SetupHttpClientMock();

            // Act
            var result = await _weatherService.GetForecastAsync(latitude, longitude);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.DailyForecasts.Should().NotBeEmpty();
        }

        [Test]
        public async Task UpdateWeatherForLocationAsync_WithValidLocation_ShouldReturnSuccess()
        {
            // Arrange
            var locationId = 1;

            SetupLocationMock();
            SetupApiKeyMock();
            SetupHttpClientMock();
            SetupWeatherRepositoryMock();

            // Act
            var result = await _weatherService.UpdateWeatherForLocationAsync(locationId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
        }

        private void SetupApiKeyMock()
        {
            var apiKeySetting = new Setting("WeatherApiKey", "test-api-key", "Test API Key");

            _mockSettingRepository.Setup(x => x.GetByKeyAsync("WeatherApiKey", It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Setting>.Success(apiKeySetting));

            // Setup temperature setting (optional)
            var tempSetting = new Setting("TemperatureType", "C", "Temperature Unit");
            _mockSettingRepository.Setup(x => x.GetByKeyAsync("TemperatureType", It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Setting>.Success(tempSetting));

            // Setup wind direction setting (optional)
            var windSetting = new Setting("WindDirection", "withWind", "Wind Direction Setting");
            _mockSettingRepository.Setup(x => x.GetByKeyAsync("WindDirection", It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Setting>.Success(windSetting));
        }

        private void SetupLocationMock()
        {
            var testLocation = new Location.Core.Domain.Entities.Location(
                "Test Location",
                "Test Description",
                new Coordinate(47.6062, -122.3321),
                new Address("Seattle", "WA"));

            _mockLocationRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Location.Core.Domain.Entities.Location>.Success(testLocation));
        }
        private void SetupWeatherRepositoryMock()
        {
            // Setup to return null for new weather (no existing weather)
            _mockWeatherRepository.Setup(x => x.GetByLocationIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Weather)null);

            // Setup AddAsync - FIX: Include both parameters in callback
            _mockWeatherRepository.Setup(x => x.AddAsync(It.IsAny<Weather>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Weather w, CancellationToken ct) => w);
        }

        private void SetupHttpClientMock()
        {
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
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

            var mockHttpClient = new HttpClient(mockHttpMessageHandler.Object);
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
                    ""feels_like"": 281.15,
                    ""pressure"": 1013,
                    ""humidity"": 65,
                    ""uvi"": 5.0,
                    ""clouds"": 10,
                    ""wind_speed"": 5.14,
                    ""wind_deg"": 180,
                    ""weather"": [{
                        ""id"": 800,
                        ""main"": ""Clear"",
                        ""description"": ""clear sky"",
                        ""icon"": ""01d""
                    }]
                },
                ""hourly"": [{
                    ""dt"": 1640000000,
                    ""temp"": 283.15,
                    ""feels_like"": 281.15,
                    ""pressure"": 1013,
                    ""humidity"": 65,
                    ""uvi"": 5.0,
                    ""clouds"": 10,
                    ""wind_speed"": 5.14,
                    ""wind_deg"": 180,
                    ""wind_gust"": 7.2,
                    ""weather"": [{
                        ""id"": 800,
                        ""main"": ""Clear"",
                        ""description"": ""clear sky"",
                        ""icon"": ""01d""
                    }],
                    ""pop"": 0.0,
                    ""visibility"": 10000,
                    ""dew_point"": 276.15
                }],
                ""daily"": [{
                    ""dt"": 1640000000,
                    ""sunrise"": 1639991234,
                    ""sunset"": 1640025678,
                    ""moonrise"": 1640012345,
                    ""moonset"": 1640056789,
                    ""moon_phase"": 0.5,
                    ""temp"": {
                        ""day"": 285.15,
                        ""min"": 280.15,
                        ""max"": 290.15,
                        ""night"": 282.15,
                        ""eve"": 284.15,
                        ""morn"": 281.15
                    },
                    ""feels_like"": {
                        ""day"": 283.15,
                        ""night"": 280.15,
                        ""eve"": 282.15,
                        ""morn"": 279.15
                    },
                    ""pressure"": 1015,
                    ""humidity"": 70,
                    ""wind_speed"": 6.2,
                    ""wind_deg"": 225,
                    ""wind_gust"": 10.5,
                    ""weather"": [{
                        ""id"": 500,
                        ""main"": ""Rain"",
                        ""description"": ""light rain"",
                        ""icon"": ""10d""
                    }],
                    ""clouds"": 45,
                    ""pop"": 0.8,
                    ""rain"": 2.5,
                    ""uvi"": 7.2
                }]
            }";
        }
    }
}