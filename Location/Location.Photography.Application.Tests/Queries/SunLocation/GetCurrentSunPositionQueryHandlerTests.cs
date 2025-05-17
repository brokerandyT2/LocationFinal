using FluentAssertions;
using Location.Core.Application.Common.Models;
using Location.Photography.Application.Queries.SunLocation;
using Location.Photography.Domain.Models;
using Location.Photography.Domain.Services;
using MediatR;
using Moq;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Photography.Application.Tests.Queries.SunLocation
{
    [TestFixture]
    public class GetCurrentSunPositionQueryHandlerTests
    {
        private GetCurrentSunPositionQuery.GetCurrentSunPositionQueryHandler _handler;
        private Mock<ISunCalculatorService> _sunCalculatorServiceMock;

        [SetUp]
        public void SetUp()
        {
            _sunCalculatorServiceMock = new Mock<ISunCalculatorService>();
            _handler = new GetCurrentSunPositionQuery.GetCurrentSunPositionQueryHandler(_sunCalculatorServiceMock.Object);
        }

        [Test]
        public void Constructor_WithNullSunCalculatorService_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            FluentActions.Invoking(() => new GetCurrentSunPositionQuery.GetCurrentSunPositionQueryHandler(null))
                .Should().Throw<ArgumentNullException>()
                .WithParameterName("sunCalculatorService");
        }

        [Test]
        public async Task Handle_WithValidQuery_ShouldReturnCurrentSunPosition()
        {
            // Arrange
            var query = new GetCurrentSunPositionQuery
            {
                Latitude = 47.6062,
                Longitude = -122.3321,
                DateTime = new DateTime(2024, 5, 15, 12, 0, 0)
            };

            var expectedAzimuth = 180.0; // Due south at noon
            var expectedElevation = 60.0; // High in the sky

            _sunCalculatorServiceMock
                .Setup(x => x.GetSolarAzimuth(query.DateTime, query.Latitude, query.Longitude))
                .Returns(expectedAzimuth);

            _sunCalculatorServiceMock
                .Setup(x => x.GetSolarElevation(query.DateTime, query.Latitude, query.Longitude))
                .Returns(expectedElevation);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Azimuth.Should().Be(expectedAzimuth);
            result.Data.Elevation.Should().Be(expectedElevation);
            result.Data.DateTime.Should().Be(query.DateTime);
            result.Data.Latitude.Should().Be(query.Latitude);
            result.Data.Longitude.Should().Be(query.Longitude);

            _sunCalculatorServiceMock.Verify(x => x.GetSolarAzimuth(query.DateTime, query.Latitude, query.Longitude), Times.Once);
            _sunCalculatorServiceMock.Verify(x => x.GetSolarElevation(query.DateTime, query.Latitude, query.Longitude), Times.Once);
        }

        [Test]
        public async Task Handle_WhenCalculatorThrowsException_ShouldReturnFailureResult()
        {
            // Arrange
            var query = new GetCurrentSunPositionQuery
            {
                Latitude = 47.6062,
                Longitude = -122.3321,
                DateTime = new DateTime(2024, 5, 15, 12, 0, 0)
            };

            _sunCalculatorServiceMock
                .Setup(x => x.GetSolarAzimuth(query.DateTime, query.Latitude, query.Longitude))
                .Throws(new ArgumentException("Invalid coordinates"));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Error calculating sun position");
            result.ErrorMessage.Should().Contain("Invalid coordinates");
        }

        [Test]
        public async Task Handle_WithExtremeLatitude_ShouldCalculateCorrectly()
        {
            // Arrange - Arctic Circle in summer
            var query = new GetCurrentSunPositionQuery
            {
                Latitude = 78.0, // Far north
                Longitude = 15.0,
                DateTime = new DateTime(2024, 6, 21, 12, 0, 0) // Summer solstice
            };

            var expectedAzimuth = 180.0; // Due south at noon
            var expectedElevation = 35.5; // Low elevation due to high latitude

            _sunCalculatorServiceMock
                .Setup(x => x.GetSolarAzimuth(query.DateTime, query.Latitude, query.Longitude))
                .Returns(expectedAzimuth);

            _sunCalculatorServiceMock
                .Setup(x => x.GetSolarElevation(query.DateTime, query.Latitude, query.Longitude))
                .Returns(expectedElevation);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Azimuth.Should().Be(expectedAzimuth);
            result.Data.Elevation.Should().Be(expectedElevation);

            // Verify the extreme latitude was passed to the calculator
            _sunCalculatorServiceMock.Verify(x => x.GetSolarAzimuth(
                It.IsAny<DateTime>(),
                78.0,
                It.IsAny<double>()), Times.Once);

            _sunCalculatorServiceMock.Verify(x => x.GetSolarElevation(
                It.IsAny<DateTime>(),
                78.0,
                It.IsAny<double>()), Times.Once);
        }

        [Test]
        public async Task Handle_WithExtremeSouthLatitude_ShouldCalculateCorrectly()
        {
            // Arrange - Antarctic Circle in winter
            var query = new GetCurrentSunPositionQuery
            {
                Latitude = -78.0, // Far south
                Longitude = 0.0,
                DateTime = new DateTime(2024, 6, 21, 12, 0, 0) // Winter in southern hemisphere
            };

            var expectedAzimuth = 0.0; // Due north at noon in southern hemisphere
            var expectedElevation = -35.5; // Negative elevation (below horizon)

            _sunCalculatorServiceMock
                .Setup(x => x.GetSolarAzimuth(query.DateTime, query.Latitude, query.Longitude))
                .Returns(expectedAzimuth);

            _sunCalculatorServiceMock
                .Setup(x => x.GetSolarElevation(query.DateTime, query.Latitude, query.Longitude))
                .Returns(expectedElevation);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Azimuth.Should().Be(expectedAzimuth);
            result.Data.Elevation.Should().Be(expectedElevation);

            // Verify the extreme latitude was passed to the calculator
            _sunCalculatorServiceMock.Verify(x => x.GetSolarAzimuth(
                It.IsAny<DateTime>(),
                -78.0,
                It.IsAny<double>()), Times.Once);

            _sunCalculatorServiceMock.Verify(x => x.GetSolarElevation(
                It.IsAny<DateTime>(),
                -78.0,
                It.IsAny<double>()), Times.Once);
        }

        [Test]
        public async Task Handle_WithExtremeLongitude_ShouldCalculateCorrectly()
        {
            // Arrange
            var query = new GetCurrentSunPositionQuery
            {
                Latitude = 47.6062,
                Longitude = 179.9, // Near International Date Line
                DateTime = new DateTime(2024, 5, 15, 12, 0, 0)
            };

            var expectedAzimuth = 180.0; // Due south at noon
            var expectedElevation = 60.0; // High in the sky

            _sunCalculatorServiceMock
                .Setup(x => x.GetSolarAzimuth(query.DateTime, query.Latitude, query.Longitude))
                .Returns(expectedAzimuth);

            _sunCalculatorServiceMock
                .Setup(x => x.GetSolarElevation(query.DateTime, query.Latitude, query.Longitude))
                .Returns(expectedElevation);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();

            // Verify the extreme longitude was passed to the calculator
            _sunCalculatorServiceMock.Verify(x => x.GetSolarAzimuth(
                It.IsAny<DateTime>(),
                It.IsAny<double>(),
                179.9), Times.Once);

            _sunCalculatorServiceMock.Verify(x => x.GetSolarElevation(
                It.IsAny<DateTime>(),
                It.IsAny<double>(),
                179.9), Times.Once);
        }

        [Test]
        public async Task Handle_AtSunrise_ShouldShowLowElevation()
        {
            // Arrange
            var query = new GetCurrentSunPositionQuery
            {
                Latitude = 47.6062,
                Longitude = -122.3321,
                DateTime = new DateTime(2024, 5, 15, 5, 30, 0) // Around sunrise time
            };

            var expectedAzimuth = 90.0; // Due east at sunrise
            var expectedElevation = 0.5; // Just above the horizon

            _sunCalculatorServiceMock
                .Setup(x => x.GetSolarAzimuth(query.DateTime, query.Latitude, query.Longitude))
                .Returns(expectedAzimuth);

            _sunCalculatorServiceMock
                .Setup(x => x.GetSolarElevation(query.DateTime, query.Latitude, query.Longitude))
                .Returns(expectedElevation);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Azimuth.Should().Be(expectedAzimuth);
            result.Data.Elevation.Should().Be(expectedElevation);
        }

        [Test]
        public async Task Handle_AtSunset_ShouldShowLowElevation()
        {
            // Arrange
            var query = new GetCurrentSunPositionQuery
            {
                Latitude = 47.6062,
                Longitude = -122.3321,
                DateTime = new DateTime(2024, 5, 15, 20, 45, 0) // Around sunset time
            };

            var expectedAzimuth = 270.0; // Due west at sunset
            var expectedElevation = 0.5; // Just above the horizon

            _sunCalculatorServiceMock
                .Setup(x => x.GetSolarAzimuth(query.DateTime, query.Latitude, query.Longitude))
                .Returns(expectedAzimuth);

            _sunCalculatorServiceMock
                .Setup(x => x.GetSolarElevation(query.DateTime, query.Latitude, query.Longitude))
                .Returns(expectedElevation);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Azimuth.Should().Be(expectedAzimuth);
            result.Data.Elevation.Should().Be(expectedElevation);
        }

        [Test]
        public async Task Handle_AtMidnight_ShouldShowNegativeElevation()
        {
            // Arrange
            var query = new GetCurrentSunPositionQuery
            {
                Latitude = 47.6062,
                Longitude = -122.3321,
                DateTime = new DateTime(2024, 5, 15, 0, 0, 0) // Midnight
            };

            var expectedAzimuth = 0.0; // Due north at midnight
            var expectedElevation = -30.0; // Below the horizon

            _sunCalculatorServiceMock
                .Setup(x => x.GetSolarAzimuth(query.DateTime, query.Latitude, query.Longitude))
                .Returns(expectedAzimuth);

            _sunCalculatorServiceMock
                .Setup(x => x.GetSolarElevation(query.DateTime, query.Latitude, query.Longitude))
                .Returns(expectedElevation);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Azimuth.Should().Be(expectedAzimuth);
            result.Data.Elevation.Should().Be(expectedElevation);
        }

        [Test]
        public async Task Handle_WithCancellationToken_ShouldObeyToken()
        {
            // Arrange
            var query = new GetCurrentSunPositionQuery
            {
                Latitude = 47.6062,
                Longitude = -122.3321,
                DateTime = DateTime.UtcNow
            };

            var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel token immediately

            // Act & Assert
            // Since our handler doesn't have async operations that check cancellation,
            // we need to verify the token is passed to the Task.FromResult
            await FluentActions.Invoking(async () =>
                await _handler.Handle(query, cts.Token))
                .Should().ThrowAsync<OperationCanceledException>();
        }
    }
}