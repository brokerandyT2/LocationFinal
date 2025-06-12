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
using System.Text.Json;

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

        [Test]
        public async Task GetHourlyForecastAsync_WithMissingApiKey_ShouldReturnFailure()
        {
            // Arrange
            var latitude = 47.6062;
            var longitude = -122.3321;

            _mockSettingRepository.Setup(x => x.GetByKeyAsync("WeatherApiKey", It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Setting>.Failure("API key not found"));

            // Act
            var result = await _weatherService.GetHourlyForecastAsync(latitude, longitude);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Weather API key not configured");
        }

        [Test]
        public async Task GetForecastAsync_WithHttpError_ShouldReturnFailure()
        {
            // Arrange
            var latitude = 47.6062;
            var longitude = -122.3321;

            SetupApiKeyMock();
            SetupHttpClientMockWithError(HttpStatusCode.BadRequest);

            // Act
            var result = await _weatherService.GetForecastAsync(latitude, longitude);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Weather API request failed");
        }

        [Test]
        public async Task UpdateWeatherForLocationAsync_WithNonExistentLocation_ShouldReturnFailure()
        {
            // Arrange
            var locationId = 999;

            _mockLocationRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Location.Core.Domain.Entities.Location>.Failure("Location not found"));

            // Act
            var result = await _weatherService.UpdateWeatherForLocationAsync(locationId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Location not found");
        }

        [Test]
        public async Task GetHourlyForecastAsync_WithInvalidJson_ShouldThrowDomainException()
        {
            // Arrange
            var latitude = 47.6062;
            var longitude = -122.3321;

            SetupApiKeyMock();
            SetupHttpClientMockWithInvalidJson();

            // Setup exception mapper to return a domain exception
            _mockExceptionMapper.Setup(x => x.MapToWeatherDomainException(It.IsAny<Exception>(), It.IsAny<string>()))
                .Returns(new Location.Core.Domain.Exceptions.WeatherDomainException("PARSE_ERROR", "Failed to parse weather data", new JsonException()));

            // Act & Assert
            await FluentActions.Invoking(async () => await _weatherService.GetHourlyForecastAsync(latitude, longitude))
                .Should().ThrowAsync<Location.Core.Domain.Exceptions.WeatherDomainException>()
                .WithMessage("Failed to parse weather data");
        }

        [Test]
        public void Constructor_WithNullHttpClientFactory_ShouldThrowException()
        {
            // Act & Assert
            Action act = () => new WeatherService(
                null!,
                _mockUnitOfWork.Object,
                _mockLogger.Object,
                _mockExceptionMapper.Object);

            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("httpClientFactory");
        }

        [Test]
        public void Constructor_WithNullUnitOfWork_ShouldThrowException()
        {
            // Act & Assert
            Action act = () => new WeatherService(
                _mockHttpClientFactory.Object,
                null!,
                _mockLogger.Object,
                _mockExceptionMapper.Object);

            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("unitOfWork");
        }

        [Test]
        public void Constructor_WithNullLogger_ShouldThrowException()
        {
            // Act & Assert
            Action act = () => new WeatherService(
                _mockHttpClientFactory.Object,
                _mockUnitOfWork.Object,
                null!,
                _mockExceptionMapper.Object);

            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("logger");
        }

        [Test]
        public void Constructor_WithNullExceptionMapper_ShouldThrowException()
        {
            // Act & Assert
            Action act = () => new WeatherService(
                _mockHttpClientFactory.Object,
                _mockUnitOfWork.Object,
                _mockLogger.Object,
                null!);

            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("exceptionMapper");
        }

        [Test]
        public async Task GetForecastAsync_WithCancellationToken_ShouldPassThrough()
        {
            // Arrange
            var latitude = 47.6062;
            var longitude = -122.3321;
            var cancellationToken = new CancellationToken();

            SetupApiKeyMock();
            SetupHttpClientMock();

            // Act
            var result = await _weatherService.GetForecastAsync(latitude, longitude, 7);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            _mockSettingRepository.Verify(x => x.GetByKeyAsync("WeatherApiKey", cancellationToken), Times.Once);
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
                .ReturnsAsync((Weather?)null);

            // Setup AddAsync 
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

        private void SetupHttpClientMockWithError(HttpStatusCode statusCode)
        {
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();

            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    Content = new StringContent("Error response")
                });

            var mockHttpClient = new HttpClient(mockHttpMessageHandler.Object);
            _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
                .Returns(mockHttpClient);
        }

        private void SetupHttpClientMockWithInvalidJson()
        {
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();

            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{ invalid json")
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
                    ""weather"": [{
                        ""id"": 800,
                        ""main"": ""Clear"",
                        ""description"": ""clear sky"",
                        ""icon"": ""01d""
                    }]
                }],
                ""daily"": [{
                    ""dt"": 1640000000,
                    ""sunrise"": 1639999200,
                    ""sunset"": 1640034000,
                    ""temp"": {
                        ""day"": 283.15,
                        ""min"": 280.15,
                        ""max"": 285.15,
                        ""night"": 281.15,
                        ""eve"": 282.15,
                        ""morn"": 280.15
                    },
                    ""feels_like"": {
                        ""day"": 281.15,
                        ""night"": 279.15,
                        ""eve"": 280.15,
                        ""morn"": 278.15
                    },
                    ""pressure"": 1013,
                    ""humidity"": 65,
                    ""wind_speed"": 5.14,
                    ""wind_deg"": 180,
                    ""weather"": [{
                        ""id"": 800,
                        ""main"": ""Clear"",
                        ""description"": ""clear sky"",
                        ""icon"": ""01d""
                    }],
                    ""clouds"": 10,
                    ""uvi"": 5.0
                }]
            }";
        }
    }
}