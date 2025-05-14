using NUnit.Framework;
using FluentAssertions;
using Moq;
using System.Threading;
using System.Threading.Tasks;
using Location.Core.Application.Services;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Weather.DTOs;
using Location.Core.Application.Tests.Utilities;

namespace Location.Core.Application.Tests.Services
{
    [TestFixture]
    public class IWeatherServiceTests
    {
        private Mock<IWeatherService> _weatherServiceMock;

        [SetUp]
        public void Setup()
        {
            _weatherServiceMock = new Mock<IWeatherService>();
        }

        [Test]
        public async Task GetWeatherAsync_WithValidCoordinates_ShouldReturnSuccess()
        {
            // Arrange
            var latitude = 47.6062;
            var longitude = -122.3321;
            var expectedWeather = TestDataBuilder.CreateValidWeatherDto();
            var expectedResult = Result<WeatherDto>.Success(expectedWeather);

            _weatherServiceMock.Setup(x => x.GetWeatherAsync(
                latitude,
                longitude,
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _weatherServiceMock.Object.GetWeatherAsync(latitude, longitude);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().BeEquivalentTo(expectedWeather);

            _weatherServiceMock.Verify(x => x.GetWeatherAsync(
                latitude,
                longitude,
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task GetWeatherAsync_WithInvalidCoordinates_ShouldReturnFailure()
        {
            // Arrange
            var latitude = 91.0; // Invalid latitude
            var longitude = -122.3321;
            var expectedResult = Result<WeatherDto>.Failure("Invalid coordinates");

            _weatherServiceMock.Setup(x => x.GetWeatherAsync(
                latitude,
                longitude,
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _weatherServiceMock.Object.GetWeatherAsync(latitude, longitude);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Invalid coordinates");
        }

        [Test]
        public async Task UpdateWeatherForLocationAsync_WithValidLocationId_ShouldReturnSuccess()
        {
            // Arrange
            var locationId = 42;
            var expectedWeather = TestDataBuilder.CreateValidWeatherDto();
            var expectedResult = Result<WeatherDto>.Success(expectedWeather);

            _weatherServiceMock.Setup(x => x.UpdateWeatherForLocationAsync(
                locationId,
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _weatherServiceMock.Object.UpdateWeatherForLocationAsync(locationId);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
        }

        [Test]
        public async Task GetForecastAsync_WithValidCoordinates_ShouldReturnForecast()
        {
            // Arrange
            var latitude = 47.6062;
            var longitude = -122.3321;
            var days = 7;
            var expectedForecast = TestDataBuilder.CreateValidWeatherForecastDto();
            var expectedResult = Result<WeatherForecastDto>.Success(expectedForecast);

            _weatherServiceMock.Setup(x => x.GetForecastAsync(
                latitude,
                longitude,
                days,
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _weatherServiceMock.Object.GetForecastAsync(latitude, longitude, days);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.DailyForecasts.Should().HaveCount(7);
        }

        [Test]
        public async Task GetForecastAsync_WithDefaultDays_ShouldReturn7DayForecast()
        {
            // Arrange
            var latitude = 47.6062;
            var longitude = -122.3321;
            var expectedForecast = TestDataBuilder.CreateValidWeatherForecastDto();
            var expectedResult = Result<WeatherForecastDto>.Success(expectedForecast);

            _weatherServiceMock.Setup(x => x.GetForecastAsync(
                latitude,
                longitude,
                7, // Default value
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _weatherServiceMock.Object.GetForecastAsync(latitude, longitude);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _weatherServiceMock.Verify(x => x.GetForecastAsync(
                latitude,
                longitude,
                7,
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task UpdateAllWeatherAsync_ShouldReturnCountOfUpdated()
        {
            // Arrange
            var expectedCount = 5;
            var expectedResult = Result<int>.Success(expectedCount);

            _weatherServiceMock.Setup(x => x.UpdateAllWeatherAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _weatherServiceMock.Object.UpdateAllWeatherAsync();

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be(5);
        }

        [Test]
        public async Task UpdateAllWeatherAsync_WithNoLocations_ShouldReturnZero()
        {
            // Arrange
            var expectedResult = Result<int>.Success(0);

            _weatherServiceMock.Setup(x => x.UpdateAllWeatherAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _weatherServiceMock.Object.UpdateAllWeatherAsync();

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be(0);
        }

        [Test]
        public async Task GetWeatherAsync_WithCancellationToken_ShouldPassThrough()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            var token = cts.Token;
            var expectedResult = Result<WeatherDto>.Success(TestDataBuilder.CreateValidWeatherDto());

            _weatherServiceMock.Setup(x => x.GetWeatherAsync(
                It.IsAny<double>(),
                It.IsAny<double>(),
                token))
                .ReturnsAsync(expectedResult);

            // Act
            await _weatherServiceMock.Object.GetWeatherAsync(0, 0, token);

            // Assert
            _weatherServiceMock.Verify(x => x.GetWeatherAsync(
                It.IsAny<double>(),
                It.IsAny<double>(),
                token), Times.Once);
        }
    }
}