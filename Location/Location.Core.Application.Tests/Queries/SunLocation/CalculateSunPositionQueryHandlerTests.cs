// Location.Photography.Application.Tests/Queries/SunLocation/CalculateSunPositionQueryHandlerTests.cs
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
    public class CalculateSunPositionQueryHandlerTests
    {
        private Mock<ISunCalculatorService> _sunCalculatorServiceMock;
        private GetCurrentSunPositionQuery.GetCurrentSunPositionQueryHandler _handler;

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
        public async Task Handle_WithValidQuery_ShouldReturnSunPosition()
        {
            // Arrange
            var query = new GetCurrentSunPositionQuery
            {
                Latitude = 47.6062,
                Longitude = -122.3321,
                DateTime = new DateTime(2024, 6, 15, 12, 0, 0)
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
        public async Task Handle_WhenExceptionThrown_ShouldReturnFailure()
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
                .Throws(new Exception("Unexpected error"));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Error calculating sun position");
            result.ErrorMessage.Should().Contain("Unexpected error");
        }

        [Test]
        public async Task Handle_WithCancellationToken_ShouldObeyToken()
        {
            // Arrange
            var query = new GetCurrentSunPositionQuery
            {
                Latitude = 47.6062,
                Longitude = -122.3321,
                DateTime = DateTime.Now
            };

            var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel token immediately

            // Act & Assert
            await FluentActions.Invoking(async () =>
                await _handler.Handle(query, cts.Token))
                .Should().ThrowAsync<OperationCanceledException>();
        }
    }
}