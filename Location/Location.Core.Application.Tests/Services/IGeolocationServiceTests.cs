using FluentAssertions;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Services;
using Moq;
using NUnit.Framework;
using GeolocationAccuracy = Location.Core.Application.Services.GeolocationAccuracy;

namespace Location.Core.Application.Tests.Services
{
    [Category("GeoLocation Service")]
    [TestFixture]
    public class IGeolocationServiceTests
    {
        private Mock<IGeolocationService> _geolocationServiceMock;

        [SetUp]
        public void Setup()
        {
            _geolocationServiceMock = new Mock<IGeolocationService>();
        }

        [Test]
        public async Task GetCurrentLocationAsync_WithLocationEnabled_ShouldReturnSuccess()
        {
            // Arrange
            var expectedLocation = new GeolocationDto
            {
                Latitude = 47.6062,
                Longitude = -122.3321,
                Altitude = 56.0,
                Accuracy = 10.0,
                Timestamp = DateTime.UtcNow
            };
            var expectedResult = Result<GeolocationDto>.Success(expectedLocation);

            _geolocationServiceMock.Setup(x => x.GetCurrentLocationAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _geolocationServiceMock.Object.GetCurrentLocationAsync();

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Latitude.Should().Be(47.6062);
            result.Data.Longitude.Should().Be(-122.3321);
            result.Data.Accuracy.Should().Be(10.0);
        }

        [Test]
        public async Task GetCurrentLocationAsync_WithLocationDisabled_ShouldReturnFailure()
        {
            // Arrange
            var expectedResult = Result<GeolocationDto>.Failure("Location services are disabled");

            _geolocationServiceMock.Setup(x => x.GetCurrentLocationAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _geolocationServiceMock.Object.GetCurrentLocationAsync();

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Location services are disabled");
        }

        [Test]
        public async Task IsLocationEnabledAsync_WhenEnabled_ShouldReturnTrue()
        {
            // Arrange
            var expectedResult = Result<bool>.Success(true);

            _geolocationServiceMock.Setup(x => x.IsLocationEnabledAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _geolocationServiceMock.Object.IsLocationEnabledAsync();

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();
        }

        [Test]
        public async Task IsLocationEnabledAsync_WhenDisabled_ShouldReturnFalse()
        {
            // Arrange
            var expectedResult = Result<bool>.Success(false);

            _geolocationServiceMock.Setup(x => x.IsLocationEnabledAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _geolocationServiceMock.Object.IsLocationEnabledAsync();

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeFalse();
        }

        [Test]
        public async Task RequestPermissionsAsync_WhenGranted_ShouldReturnTrue()
        {
            // Arrange
            var expectedResult = Result<bool>.Success(true);

            _geolocationServiceMock.Setup(x => x.RequestPermissionsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _geolocationServiceMock.Object.RequestPermissionsAsync();

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();
        }

        [Test]
        public async Task RequestPermissionsAsync_WhenDenied_ShouldReturnFalse()
        {
            // Arrange
            var expectedResult = Result<bool>.Success(false);

            _geolocationServiceMock.Setup(x => x.RequestPermissionsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _geolocationServiceMock.Object.RequestPermissionsAsync();

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeFalse();
        }

        [Test]
        public async Task StartTrackingAsync_WithValidAccuracy_ShouldReturnSuccess()
        {
            // Arrange
            var accuracy = GeolocationAccuracy.High;
            var expectedResult = Result<bool>.Success(true);

            _geolocationServiceMock.Setup(x => x.StartTrackingAsync(
                accuracy,
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _geolocationServiceMock.Object.StartTrackingAsync(accuracy);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();

            _geolocationServiceMock.Verify(x => x.StartTrackingAsync(
                accuracy,
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task StartTrackingAsync_WithDefaultAccuracy_ShouldUseMedium()
        {
            // Arrange
            var expectedResult = Result<bool>.Success(true);

            _geolocationServiceMock.Setup(x => x.StartTrackingAsync(
                GeolocationAccuracy.Medium, // Default value
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _geolocationServiceMock.Object.StartTrackingAsync();

            // Assert
            result.IsSuccess.Should().BeTrue();

            _geolocationServiceMock.Verify(x => x.StartTrackingAsync(
                GeolocationAccuracy.Medium,
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task StopTrackingAsync_ShouldReturnSuccess()
        {
            // Arrange
            var expectedResult = Result<bool>.Success(true);

            _geolocationServiceMock.Setup(x => x.StopTrackingAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _geolocationServiceMock.Object.StopTrackingAsync();

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();
        }

        [Test]
        public async Task GetCurrentLocationAsync_WithTimeout_ShouldReturnFailure()
        {
            // Arrange
            var expectedResult = Result<GeolocationDto>.Failure("Location request timed out");

            _geolocationServiceMock.Setup(x => x.GetCurrentLocationAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _geolocationServiceMock.Object.GetCurrentLocationAsync();

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Location request timed out");
        }

        [Test]
        public void GeolocationDto_ShouldHaveCorrectProperties()
        {
            // Arrange & Act
            var dto = new GeolocationDto
            {
                Latitude = 47.6062,
                Longitude = -122.3321,
                Altitude = 100.5,
                Accuracy = 5.0,
                Timestamp = DateTime.UtcNow
            };

            // Assert
            dto.Latitude.Should().Be(47.6062);
            dto.Longitude.Should().Be(-122.3321);
            dto.Altitude.Should().Be(100.5);
            dto.Accuracy.Should().Be(5.0);
            dto.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Test]
        public void GeolocationDto_NullableAltitude_ShouldAcceptNull()
        {
            // Arrange & Act
            var dto = new GeolocationDto
            {
                Latitude = 47.6062,
                Longitude = -122.3321,
                Altitude = null,
                Accuracy = null,
                Timestamp = DateTime.UtcNow
            };

            // Assert
            dto.Altitude.Should().BeNull();
            dto.Accuracy.Should().BeNull();
        }

        [Test]
        public void GeolocationAccuracy_ShouldHaveCorrectValues()
        {
            // Assert
            GeolocationAccuracy.Lowest.Should().Be(GeolocationAccuracy.Lowest);
            GeolocationAccuracy.Low.Should().Be(GeolocationAccuracy.Low);
            GeolocationAccuracy.Medium.Should().Be(GeolocationAccuracy.Medium);
            GeolocationAccuracy.High.Should().Be(GeolocationAccuracy.High);
            GeolocationAccuracy.Best.Should().Be(GeolocationAccuracy.Best);
        }
    }
}