
using NUnit.Framework;
using FluentAssertions;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Services;
using Location.Core.Application.Weather.DTOs;
using Location.Core.Domain.Entities;
using Location.Core.Domain.ValueObjects;
using Location.Core.Infrastructure.External;
using Location.Core.Infrastructure.External.Models;
using Location.Core.Infrastructure.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ILocationRepository = Location.Core.Application.Common.Interfaces.Persistence.ILocationRepository;
namespace Location.Core.Infrastructure.Tests.External
{
    [TestFixture]
    public class WeatherServiceTests
    {
        private WeatherService _weatherService;
        private Mock<IHttpClientFactory> _mockHttpClientFactory;
        private Mock<IUnitOfWork> _mockUnitOfWork;
        private Mock<ILogger<WeatherService>> _mockLogger;
        private Mock<ISettingRepository> _mockSettingRepository;
        private Mock<ILocationRepository> _mockLocationRepository;
        private Mock<IWeatherRepository> _mockWeatherRepository;

        [SetUp]
        public void Setup()
        {
            _mockHttpClientFactory = new Mock<IHttpClientFactory>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockLogger = new Mock<ILogger<WeatherService>>();
            _mockSettingRepository = new Mock<ISettingRepository>();
            _mockLocationRepository = new Mock<ILocationRepository>();
            _mockWeatherRepository = new Mock<IWeatherRepository>();

            _mockUnitOfWork.Setup(x => x.Settings).Returns(_mockSettingRepository.Object);
            _mockUnitOfWork.Setup(x => x.Locations).Returns(_mockLocationRepository.Object);
            _mockUnitOfWork.Setup(x => x.Weather).Returns(_mockWeatherRepository.Object);

            _weatherService = new WeatherService(
                _mockHttpClientFactory.Object,
                _mockUnitOfWork.Object,
                _mockLogger.Object);
        }

        [Test]
        public async Task GetWeatherAsync_WithValidApiResponse_ShouldReturnWeatherDto()
        {
            // Arrange
            var apiKey = "test_api_key";
            var latitude = 47.6062;
            var longitude = -122.3321;

            SetupApiKey(apiKey);
            var httpClient = SetupHttpClient(CreateValidWeatherResponse());
            _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
                .Returns(httpClient);

            // Act
            var result = await _weatherService.GetWeatherAsync(latitude, longitude);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Temperature.Should().Be(20);
            result.Data.Description.Should().Be("clear sky");
            result.Data.WindSpeed.Should().Be(10);
        }

        [Test]
        public async Task GetWeatherAsync_WithNoApiKey_ShouldReturnFailure()
        {
            // Arrange
            _mockSettingRepository.Setup(x => x.GetByKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Setting>.Failure("Not found"));

            // Act
            var result = await _weatherService.GetWeatherAsync(47.6062, -122.3321);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Weather API key not configured");
        }

        [Test]
        public async Task GetWeatherAsync_WithHttpError_ShouldReturnFailure()
        {
            // Arrange
            var apiKey = "test_api_key";
            SetupApiKey(apiKey);

            var httpClient = SetupHttpClient(HttpStatusCode.BadRequest);
            _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
                .Returns(httpClient);

            // Act
            var result = await _weatherService.GetWeatherAsync(47.6062, -122.3321);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Weather API request failed");
        }

        [Test]
        public async Task GetWeatherAsync_WithInvalidJson_ShouldReturnFailure()
        {
            // Arrange
            var apiKey = "test_api_key";
            SetupApiKey(apiKey);

            var httpClient = SetupHttpClient("invalid json");
            _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
                .Returns(httpClient);

            // Act
            var result = await _weatherService.GetWeatherAsync(47.6062, -122.3321);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Failed to fetch weather");
        }

        [Test]
        public async Task UpdateWeatherForLocationAsync_WithValidLocation_ShouldUpdateWeather()
        {
            // Arrange
            var locationId = 1;
            var location = TestDataBuilder.CreateValidLocation();
            var apiKey = "test_api_key";

            SetupApiKey(apiKey);
            _mockLocationRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(location);

            var httpClient = SetupHttpClient(CreateValidWeatherResponse());
            _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
                .Returns(httpClient);

            _mockWeatherRepository.Setup(x => x.GetByLocationIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Weather?)null);
            _mockWeatherRepository.Setup(x => x.AddAsync(It.IsAny<Weather>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Weather w, CancellationToken _) => w);

            // Act
            var result = await _weatherService.UpdateWeatherForLocationAsync(locationId);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _mockWeatherRepository.Verify(x => x.AddAsync(It.IsAny<Weather>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task UpdateWeatherForLocationAsync_WithNonExistingLocation_ShouldReturnFailure()
        {
            // Arrange
            _mockLocationRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Domain.Entities.Location?)null);

            // Act
            var result = await _weatherService.UpdateWeatherForLocationAsync(999);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Location not found");
        }

        [Test]
        public async Task GetForecastAsync_WithValidApiResponse_ShouldReturnForecast()
        {
            // Arrange
            var apiKey = "test_api_key";
            SetupApiKey(apiKey);

            var response = CreateForecastResponse();
            var httpClient = SetupHttpClient(response);
            _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
                .Returns(httpClient);

            // Act
            var result = await _weatherService.GetForecastAsync(47.6062, -122.3321, 7);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.DailyForecasts.Should().HaveCount(3);
            result.Data.DailyForecasts[0].Temperature.Should().Be(20);
        }

        [Test]
        public async Task UpdateAllWeatherAsync_WithMultipleLocations_ShouldUpdateAll()
        {
            // Arrange
            var locations = new[]
            {
                TestDataBuilder.CreateValidLocation(title: "Location 1"),
                TestDataBuilder.CreateValidLocation(title: "Location 2")
            };

            SetupApiKey("test_api_key");
            _mockLocationRepository.Setup(x => x.GetActiveAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(locations);

            var httpClient = SetupHttpClient(CreateValidWeatherResponse());
            _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
                .Returns(httpClient);

            _mockWeatherRepository.Setup(x => x.GetByLocationIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Weather?)null);
            _mockWeatherRepository.Setup(x => x.AddAsync(It.IsAny<Weather>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Weather w, CancellationToken _) => w);

            // Act
            var result = await _weatherService.UpdateAllWeatherAsync();

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be(2);
        }

        [Test]
        public async Task GetWeatherAsync_WithCancellation_ShouldReturnFailure()
        {
            // Arrange
            var apiKey = "test_api_key";
            SetupApiKey(apiKey);

            var cts = new CancellationTokenSource();
            cts.Cancel();

            var httpClient = SetupHttpClient(CreateValidWeatherResponse());
            _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
                .Returns(httpClient);

            // Act
            var result = await _weatherService.GetWeatherAsync(47.6062, -122.3321, cts.Token);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Request cancelled");
        }

        [Test]
        public void Constructor_WithNullHttpClientFactory_ShouldThrowException()
        {
            // Act
            Action act = () => new WeatherService(null!, _mockUnitOfWork.Object, _mockLogger.Object);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("httpClientFactory");
        }

        [Test]
        public void Constructor_WithNullUnitOfWork_ShouldThrowException()
        {
            // Act
            Action act = () => new WeatherService(_mockHttpClientFactory.Object, null!, _mockLogger.Object);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("unitOfWork");
        }

        [Test]
        public void Constructor_WithNullLogger_ShouldThrowException()
        {
            // Act
            Action act = () => new WeatherService(_mockHttpClientFactory.Object, _mockUnitOfWork.Object, null!);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("logger");
        }

        // Helper methods
        private void SetupApiKey(string apiKey)
        {
            var setting = TestDataBuilder.CreateValidSetting(key: "WeatherApiKey", value: apiKey);
            _mockSettingRepository.Setup(x => x.GetByKeyAsync("WeatherApiKey", It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Setting>.Success(setting));
        }

        private HttpClient SetupHttpClient(string responseContent)
        {
            var messageHandler = new Mock<HttpMessageHandler>();
            messageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(responseContent)
                });

            return new HttpClient(messageHandler.Object);
        }

        private HttpClient SetupHttpClient(HttpStatusCode statusCode)
        {
            var messageHandler = new Mock<HttpMessageHandler>();
            messageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    Content = new StringContent("Error")
                });

            return new HttpClient(messageHandler.Object);
        }

        private string CreateValidWeatherResponse()
        {
            var response = TestDataBuilder.CreateOpenWeatherResponse();
            return JsonSerializer.Serialize(response);
        }

        private string CreateForecastResponse()
        {
            var response = TestDataBuilder.CreateOpenWeatherResponse();
            response.Daily = new System.Collections.Generic.List<DailyForecast>
            {
                new DailyForecast
                {
                    Dt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    Temp = new DailyTemperature { Day = 20, Min = 15, Max = 25 },
                    Weather = new System.Collections.Generic.List<WeatherDescription>
                    {
                        new WeatherDescription { Description = "clear sky", Icon = "01d" }
                    }
                },
                new DailyForecast
                {
                    Dt = DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeSeconds(),
                    Temp = new DailyTemperature { Day = 22, Min = 17, Max = 27 },
                    Weather = new System.Collections.Generic.List<WeatherDescription>
                    {
                        new WeatherDescription { Description = "partly cloudy", Icon = "02d" }
                    }
                },
                new DailyForecast
                {
                    Dt = DateTimeOffset.UtcNow.AddDays(2).ToUnixTimeSeconds(),
                    Temp = new DailyTemperature { Day = 19, Min = 14, Max = 24 },
                    Weather = new System.Collections.Generic.List<WeatherDescription>
                    {
                        new WeatherDescription { Description = "rain", Icon = "10d" }
                    }
                }
            };
            return JsonSerializer.Serialize(response);
        }
    }
}