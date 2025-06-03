using FluentAssertions;
using Location.Photography.Application.Services;
using Location.Photography.Domain.Services;
using Moq;
using NUnit.Framework;

namespace Location.Photography.Application.Tests.Services
{
    [TestFixture]
    public class SunServiceTests
    {
        private SunService _sunService;
        private Mock<ISunCalculatorService> _sunCalculatorServiceMock;

        [SetUp]
        public void SetUp()
        {
            _sunCalculatorServiceMock = new Mock<ISunCalculatorService>();
            _sunService = new SunService(_sunCalculatorServiceMock.Object);
        }

        [Test]
        public void Constructor_WithNullSunCalculatorService_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            FluentActions.Invoking(() => new SunService(null))
                .Should().Throw<ArgumentNullException>()
                .WithParameterName("sunCalculatorService");
        }

        [Test]
        public async Task GetSunPositionAsync_ShouldReturnCorrectPosition()
        {
            // Arrange
            double latitude = 47.6062;
            double longitude = -122.3321;
            DateTime dateTime = new DateTime(2024, 5, 15, 12, 0, 0);

            double expectedAzimuth = 180.0; // Due south at noon
            double expectedElevation = 60.0; // High in the sky

            _sunCalculatorServiceMock
                .Setup(x => x.GetSolarAzimuth(dateTime, latitude, longitude, It.IsAny<string>()))
                .Returns(expectedAzimuth);

            _sunCalculatorServiceMock
                .Setup(x => x.GetSolarElevation(dateTime, latitude, longitude, It.IsAny<string>()))
                .Returns(expectedElevation);

            // Act
            var result = await _sunService.GetSunPositionAsync(latitude, longitude, dateTime, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Azimuth.Should().Be(expectedAzimuth);
            result.Data.Elevation.Should().Be(expectedElevation);
            result.Data.DateTime.Should().Be(dateTime);
            result.Data.Latitude.Should().Be(latitude);
            result.Data.Longitude.Should().Be(longitude);
        }

        [Test]
        public async Task GetSunPositionAsync_WhenCalculatorThrowsException_ShouldReturnFailureResult()
        {
            // Arrange
            double latitude = 47.6062;
            double longitude = -122.3321;
            DateTime dateTime = new DateTime(2024, 5, 15, 12, 0, 0);

            _sunCalculatorServiceMock
                .Setup(x => x.GetSolarAzimuth(dateTime, latitude, longitude, It.IsAny<string>()))
                .Throws(new ArgumentException("Invalid coordinates"));

            // Act
            var result = await _sunService.GetSunPositionAsync(latitude, longitude, dateTime, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Error calculating sun position");
            result.ErrorMessage.Should().Contain("Invalid coordinates");
        }

        [Test]
        public async Task GetSunTimesAsync_ShouldReturnAllSunTimes()
        {
            // Arrange
            double latitude = 47.6062;
            double longitude = -122.3321;
            DateTime date = new DateTime(2024, 5, 15);

            var sunrise = new DateTime(2024, 5, 15, 5, 30, 0);
            var sunset = new DateTime(2024, 5, 15, 20, 45, 0);
            var solarNoon = new DateTime(2024, 5, 15, 13, 7, 30);
            var civilDawn = new DateTime(2024, 5, 15, 5, 0, 0);
            var civilDusk = new DateTime(2024, 5, 15, 21, 15, 0);
            var nauticalDawn = new DateTime(2024, 5, 15, 4, 30, 0);
            var nauticalDusk = new DateTime(2024, 5, 15, 21, 45, 0);
            var astronomicalDawn = new DateTime(2024, 5, 15, 4, 0, 0);
            var astronomicalDusk = new DateTime(2024, 5, 15, 22, 15, 0);

            // Set up all the required calculations
            _sunCalculatorServiceMock.Setup(x => x.GetSunrise(date, latitude, longitude, It.IsAny<string>())).Returns(sunrise);
            _sunCalculatorServiceMock.Setup(x => x.GetSunset(date, latitude, longitude, It.IsAny<string>())).Returns(sunset);
            _sunCalculatorServiceMock.Setup(x => x.GetSolarNoon(date, latitude, longitude, It.IsAny<string>())).Returns(solarNoon);
            _sunCalculatorServiceMock.Setup(x => x.GetCivilDawn(date, latitude, longitude, It.IsAny<string>())).Returns(civilDawn);
            _sunCalculatorServiceMock.Setup(x => x.GetCivilDusk(date, latitude, longitude, It.IsAny<string>())).Returns(civilDusk);
            _sunCalculatorServiceMock.Setup(x => x.GetNauticalDawn(date, latitude, longitude, It.IsAny<string>())).Returns(nauticalDawn);
            _sunCalculatorServiceMock.Setup(x => x.GetNauticalDusk(date, latitude, longitude, It.IsAny<string>())).Returns(nauticalDusk);
            _sunCalculatorServiceMock.Setup(x => x.GetAstronomicalDawn(date, latitude, longitude, It.IsAny<string>())).Returns(astronomicalDawn);
            _sunCalculatorServiceMock.Setup(x => x.GetAstronomicalDusk(date, latitude, longitude, It.IsAny<string>())).Returns(astronomicalDusk);

            // Act
            var result = await _sunService.GetSunTimesAsync(latitude, longitude, date, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Date.Should().Be(date);
            result.Data.Latitude.Should().Be(latitude);
            result.Data.Longitude.Should().Be(longitude);
            result.Data.Sunrise.Should().Be(sunrise);
            result.Data.Sunset.Should().Be(sunset);
            result.Data.SolarNoon.Should().Be(solarNoon);
            result.Data.CivilDawn.Should().Be(civilDawn);
            result.Data.CivilDusk.Should().Be(civilDusk);
            result.Data.NauticalDawn.Should().Be(nauticalDawn);
            result.Data.NauticalDusk.Should().Be(nauticalDusk);
            result.Data.AstronomicalDawn.Should().Be(astronomicalDawn);
            result.Data.AstronomicalDusk.Should().Be(astronomicalDusk);

            // Golden hour calculations
            result.Data.GoldenHourMorningStart.Should().Be(sunrise);
            result.Data.GoldenHourMorningEnd.Should().Be(sunrise.AddHours(1));
            result.Data.GoldenHourEveningStart.Should().Be(sunset.AddHours(-1));
            result.Data.GoldenHourEveningEnd.Should().Be(sunset);
        }

        [Test]
        public async Task GetSunTimesAsync_WhenCalculatorThrowsException_ShouldReturnFailureResult()
        {
            // Arrange
            double latitude = 47.6062;
            double longitude = -122.3321;
            DateTime date = new DateTime(2024, 5, 15);

            _sunCalculatorServiceMock
                .Setup(x => x.GetSunrise(date, latitude, longitude, It.IsAny<string>()))
                .Throws(new ArgumentException("Invalid date"));

            // Act
            var result = await _sunService.GetSunTimesAsync(latitude, longitude, date, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Error calculating sun times");
            result.ErrorMessage.Should().Contain("Invalid date");
        }

        [Test]
        public async Task GetSunTimesAsync_WithExtremeLocation_ShouldHandlePolarDay()
        {
            // Arrange - Arctic Circle in summer
            double latitude = 78.0;
            double longitude = 15.0;
            DateTime date = new DateTime(2024, 6, 21); // Summer solstice

            // In polar day, there's no sunrise/sunset
            var mockTime = new DateTime(2024, 6, 21, 12, 0, 0);

            // When there's no sunrise/sunset, the calculator returns the same time for both
            _sunCalculatorServiceMock.Setup(x => x.GetSunrise(date, latitude, longitude, It.IsAny<string>())).Returns(mockTime);
            _sunCalculatorServiceMock.Setup(x => x.GetSunset(date, latitude, longitude, It.IsAny<string>())).Returns(mockTime);
            _sunCalculatorServiceMock.Setup(x => x.GetSolarNoon(date, latitude, longitude, It.IsAny<string>())).Returns(mockTime);
            _sunCalculatorServiceMock.Setup(x => x.GetCivilDawn(date, latitude, longitude, It.IsAny<string>())).Returns(mockTime);
            _sunCalculatorServiceMock.Setup(x => x.GetCivilDusk(date, latitude, longitude, It.IsAny<string>())).Returns(mockTime);
            _sunCalculatorServiceMock.Setup(x => x.GetNauticalDawn(date, latitude, longitude, It.IsAny<string>())).Returns(mockTime);
            _sunCalculatorServiceMock.Setup(x => x.GetNauticalDusk(date, latitude, longitude, It.IsAny<string>())).Returns(mockTime);
            _sunCalculatorServiceMock.Setup(x => x.GetAstronomicalDawn(date, latitude, longitude, It.IsAny<string>())).Returns(mockTime);
            _sunCalculatorServiceMock.Setup(x => x.GetAstronomicalDusk(date, latitude, longitude, It.IsAny<string>())).Returns(mockTime);

            // Act
            var result = await _sunService.GetSunTimesAsync(latitude, longitude, date, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Sunrise.Should().Be(mockTime);
            result.Data.Sunset.Should().Be(mockTime);

            // Even for polar day, the golden hour calculations should still work
            result.Data.GoldenHourMorningStart.Should().Be(mockTime);
            result.Data.GoldenHourMorningEnd.Should().Be(mockTime.AddHours(1));
            result.Data.GoldenHourEveningStart.Should().Be(mockTime.AddHours(-1));
            result.Data.GoldenHourEveningEnd.Should().Be(mockTime);
        }

        [Test]
        public async Task GetSunPositionAsync_WithCancellationToken_ShouldHonorToken()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            var token = cts.Token;
            double latitude = 47.6062;
            double longitude = -122.3321;
            DateTime dateTime = new DateTime(2024, 5, 15, 12, 0, 0);

            // Cancel the token before calling the method
            cts.Cancel();

            // Act & Assert
            await FluentActions.Invoking(async () =>
                await _sunService.GetSunPositionAsync(latitude, longitude, dateTime, token))
                .Should().ThrowAsync<OperationCanceledException>();
        }

        [Test]
        public async Task GetSunTimesAsync_WithCancellationToken_ShouldHonorToken()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            var token = cts.Token;
            double latitude = 47.6062;
            double longitude = -122.3321;
            DateTime date = new DateTime(2024, 5, 15);

            // Cancel the token before calling the method
            cts.Cancel();

            // Act & Assert
            await FluentActions.Invoking(async () =>
                await _sunService.GetSunTimesAsync(latitude, longitude, date, token))
                .Should().ThrowAsync<OperationCanceledException>();
        }
    }
}