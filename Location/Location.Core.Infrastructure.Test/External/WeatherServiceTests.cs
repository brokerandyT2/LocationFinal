using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
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
using Location.Core.Infrastructure.External.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using NUnit.Framework;

namespace Location.Core.Infrastructure.Test.External
{
    [TestFixture]
    public class WeatherServiceTests
    {
        private Mock<IHttpClientFactory> _httpClientFactoryMock;
        private Mock<IUnitOfWork> _unitOfWorkMock;
        private Mock<Location.Core.Application.Common.Interfaces.ILocationRepository> _locationRepositoryMock;
        private Mock<Location.Core.Application.Common.Interfaces.IWeatherRepository> _weatherRepositoryMock;
        private Mock<Location.Core.Application.Common.Interfaces.ISettingRepository> _settingRepositoryMock;
        private Mock<ILogger<WeatherService>> _loggerMock;
        private WeatherService _weatherService;
        private readonly string _apiKey = "test-api-key";

        [SetUp]
        public void SetUp()
        {
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _locationRepositoryMock = new Mock<Location.Core.Application.Common.Interfaces.ILocationRepository>();
            _weatherRepositoryMock = new Mock<Location.Core.Application.Common.Interfaces.IWeatherRepository>();
            _settingRepositoryMock = new Mock<Location.Core.Application.Common.Interfaces.ISettingRepository>();
            _loggerMock = new Mock<ILogger<WeatherService>>();

            _unitOfWorkMock.Setup(u => u.Locations).Returns(_locationRepositoryMock.Object);
            _unitOfWorkMock.Setup(u => u.Weather).Returns(_weatherRepositoryMock.Object);
            _unitOfWorkMock.Setup(u => u.Settings).Returns(_settingRepositoryMock.Object);

            _weatherService = new WeatherService(
                _httpClientFactoryMock.Object,
                _unitOfWorkMock.Object,
                _loggerMock.Object);
        }

        [Test]
        public async Task GetWeatherAsync_WithValidCoordinates_ShouldReturnSuccess()
        {
            // Arrange
            var latitude = 40.7128;
            var longitude = -74.0060;
            var apiKeySetting = new Setting("WeatherApiKey", _apiKey, "Weather API Key");

            _settingRepositoryMock
                .Setup(x => x.GetByKeyAsync("WeatherApiKey", It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Setting>.Success(apiKeySetting));

            var mockWeatherResponse = CreateMockWeatherResponse();
            var mockHttpMessageHandler = CreateMockHttpMessageHandler(mockWeatherResponse);
            var mockHttpClient = new HttpClient(mockHttpMessageHandler.Object);

            _httpClientFactoryMock
                .Setup(x => x.CreateClient(It.IsAny<string>()))
                .Returns(mockHttpClient);

            // Act
            var result = await _weatherService.GetWeatherAsync(latitude, longitude);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Temperature.Should().Be(22.5);
        }

        [Test]
        public async Task GetWeatherAsync_WithoutApiKey_ShouldReturnFailure()
        {
            // Arrange
            var latitude = 40.7128;
            var longitude = -74.0060;

            _settingRepositoryMock
                .Setup(x => x.GetByKeyAsync("WeatherApiKey", It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Setting>.Failure("API key not found"));

            // Act
            var result = await _weatherService.GetWeatherAsync(latitude, longitude);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Weather API key not configured");
        }

        [Test]
        public async Task UpdateWeatherForLocationAsync_WithValidLocation_ShouldReturnSuccess()
        {
            // Arrange
            var locationId = 1;
            var coordinate = new Coordinate(40.7128, -74.0060);
            var address = new Address("New York", "NY");
            var location = new Domain.Entities.Location("Test Location", "Description", coordinate, address);
            SetPrivateProperty(location, "Id", locationId);

            _locationRepositoryMock
                .Setup(x => x.GetByIdAsync(locationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Location>.Success(location));

            var apiKeySetting = new Setting("WeatherApiKey", _apiKey, "Weather API Key");
            _settingRepositoryMock
                .Setup(x => x.GetByKeyAsync("WeatherApiKey", It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Setting>.Success(apiKeySetting));

            var mockWeatherResponse = CreateMockWeatherResponse();
            var mockHttpMessageHandler = CreateMockHttpMessageHandler(mockWeatherResponse);
            var mockHttpClient = new HttpClient(mockHttpMessageHandler.Object);

            _httpClientFactoryMock
                .Setup(x => x.CreateClient(It.IsAny<string>()))
                .Returns(mockHttpClient);

            _weatherRepositoryMock
                .Setup(x => x.GetByLocationIdAsync(locationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Weather>.Failure("Not found"));

            _weatherRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<Weather>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Weather w, CancellationToken ct) => Result<Weather>.Success(w));

            _unitOfWorkMock
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            var result = await _weatherService.UpdateWeatherForLocationAsync(locationId);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
        }

        [Test]
        public async Task UpdateWeatherForLocationAsync_WithNonExistentLocation_ShouldReturnFailure()
        {
            // Arrange
            var locationId = 999;

            _locationRepositoryMock
                .Setup(x => x.GetByIdAsync(locationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Location>.Failure("Location not found"));

            // Act
            var result = await _weatherService.UpdateWeatherForLocationAsync(locationId);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Location not found");
        }

        [Test]
        public async Task GetForecastAsync_WithValidCoordinates_ShouldReturnSuccess()
        {
            // Arrange
            var latitude = 40.7128;
            var longitude = -74.0060;
            var days = 7;
            var apiKeySetting = new Setting("WeatherApiKey", _apiKey, "Weather API Key");

            _settingRepositoryMock
                .Setup(x => x.GetByKeyAsync("WeatherApiKey", It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Setting>.Success(apiKeySetting));

            var mockWeatherResponse = CreateMockWeatherResponse();
            var mockHttpMessageHandler = CreateMockHttpMessageHandler(mockWeatherResponse);
            var mockHttpClient = new HttpClient(mockHttpMessageHandler.Object);

            _httpClientFactoryMock
                .Setup(x => x.CreateClient(It.IsAny<string>()))
                .Returns(mockHttpClient);

            // Act
            var result = await _weatherService.GetForecastAsync(latitude, longitude, days);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.DailyForecasts.Should().NotBeEmpty();
        }

        [Test]
        public async Task UpdateAllWeatherAsync_WithActiveLocations_ShouldReturnSuccessCount()
        {
            // Arrange
            var location1 = new Domain.Entities.Location("Location 1", "Desc 1", new Coordinate(40.7128, -74.0060), new Address("New York", "NY"));
            SetPrivateProperty(location1, "Id", 1);

            var location2 = new Domain.Entities.Location("Location 2", "Desc 2", new Coordinate(34.0522, -118.2437), new Address("Los Angeles", "CA"));
            SetPrivateProperty(location2, "Id", 2);

            var locations = new List<Domain.Entities.Location> { location1, location2 };

            _locationRepositoryMock
                .Setup(x => x.GetActiveAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<List<Domain.Entities.Location>>.Success(locations));

            var apiKeySetting = new Setting("WeatherApiKey", _apiKey, "Weather API Key");
            _settingRepositoryMock
                .Setup(x => x.GetByKeyAsync("WeatherApiKey", It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Setting>.Success(apiKeySetting));

            var mockWeatherResponse = CreateMockWeatherResponse();
            var mockHttpMessageHandler = CreateMockHttpMessageHandler(mockWeatherResponse);
            var mockHttpClient = new HttpClient(mockHttpMessageHandler.Object);

            _httpClientFactoryMock
                .Setup(x => x.CreateClient(It.IsAny<string>()))
                .Returns(mockHttpClient);

            _weatherRepositoryMock
                .Setup(x => x.GetByLocationIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Weather>.Failure("Not found"));

            _weatherRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<Weather>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Weather w, CancellationToken ct) => Result<Weather>.Success(w));

            _unitOfWorkMock
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            var result = await _weatherService.UpdateAllWeatherAsync();

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be(2);
        }

        [Test]
        public async Task UpdateAllWeatherAsync_WithNoActiveLocations_ShouldReturnZero()
        {
            // Arrange
            _locationRepositoryMock
                .Setup(x => x.GetActiveAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<List<Domain.Entities.Location>>.Success(new List<Domain.Entities.Location>()));

            // Act
            var result = await _weatherService.UpdateAllWeatherAsync();

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be(0);
        }

        [Test]
        public async Task GetWeatherAsync_WithHttpError_ShouldReturnFailure()
        {
            // Arrange
            var latitude = 40.7128;
            var longitude = -74.0060;
            var apiKeySetting = new Setting("WeatherApiKey", _apiKey, "Weather API Key");

            _settingRepositoryMock
                .Setup(x => x.GetByKeyAsync("WeatherApiKey", It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Setting>.Success(apiKeySetting));

            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError));

            var mockHttpClient = new HttpClient(mockHttpMessageHandler.Object);

            _httpClientFactoryMock
                .Setup(x => x.CreateClient(It.IsAny<string>()))
                .Returns(mockHttpClient);

            // Act
            var result = await _weatherService.GetWeatherAsync(latitude, longitude);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Weather API request failed");
        }

        private Mock<HttpMessageHandler> CreateMockHttpMessageHandler(string responseContent)
        {
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
                });

            return mockHttpMessageHandler;
        }

        private string CreateMockWeatherResponse()
        {
            var response = new OpenWeatherResponse
            {
                Lat = 40.7128,
                Lon = -74.0060,
                Timezone = "America/New_York",
                TimezoneOffset = -18000,
                Current = new CurrentWeather
                {
                    Dt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    Sunrise = DateTimeOffset.UtcNow.AddHours(-6).ToUnixTimeSeconds(),
                    Sunset = DateTimeOffset.UtcNow.AddHours(6).ToUnixTimeSeconds(),
                    Temp = 22.5,
                    FeelsLike = 21.0,
                    Pressure = 1013,
                    Humidity = 65,
                    DewPoint = 15.2,
                    Uvi = 6.5,
                    Clouds = 25,
                    Visibility = 10000,
                    WindSpeed = 12.5,
                    WindDeg = 180,
                    WindGust = 15.0,
                    Weather = new List<WeatherDescription>
                    {
                        new WeatherDescription
                        {
                            Id = 800,
                            Main = "Clear",
                            Description = "clear sky",
                            Icon = "01d"
                        }
                    }
                },
                Daily = new List<DailyForecast>
                {
                    new DailyForecast
                    {
                        Dt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                        Sunrise = DateTimeOffset.UtcNow.AddHours(-6).ToUnixTimeSeconds(),
                        Sunset = DateTimeOffset.UtcNow.AddHours(6).ToUnixTimeSeconds(),
                        MoonRise = DateTimeOffset.UtcNow.AddHours(12).ToUnixTimeSeconds(),
                        MoonSet = DateTimeOffset.UtcNow.AddDays(1).AddHours(6).ToUnixTimeSeconds(),
                        MoonPhase = 0.25,
                        Temp = new DailyTemperature
                        {
                            Day = 25.0,
                            Min = 18.0,
                            Max = 28.0,
                            Night = 20.0,
                            Eve = 23.0,
                            Morn = 19.0
                        },
                        FeelsLike = new DailyFeelsLike
                        {
                            Day = 24.0,
                            Night = 19.0,
                            Eve = 22.0,
                            Morn = 18.0
                        },
                        Pressure = 1015,
                        Humidity = 60,
                        DewPoint = 15.0,
                        WindSpeed = 10.0,
                        WindDeg = 200,
                        WindGust = 12.0,
                        Weather = new List<WeatherDescription>
                        {
                            new WeatherDescription
                            {
                                Id = 801,
                                Main = "Clouds",
                                Description = "few clouds",
                                Icon = "02d"
                            }
                        },
                        Clouds = 20,
                        Pop = 0.1,
                        Rain = 0.0,
                        Uvi = 7.0
                    }
                }
            };

            return JsonSerializer.Serialize(response);
        }

        private void SetPrivateProperty(object obj, string propertyName, object value)
        {
            var property = obj.GetType().GetProperty(propertyName);
            property?.SetValue(obj, value);
        }
    }
}