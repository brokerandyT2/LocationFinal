using FluentAssertions;
using Location.Core.Application.Common.Models;
using Location.Photography.Application.Queries.SunLocation;
using Location.Photography.Application.Services;
using Location.Photography.Domain.Models;
using Moq;
using NUnit.Framework;

namespace Location.Photography.Application.Tests.Queries.SunLocation
{
    [TestFixture]
    public class GetSunPositionQueryHandlerTests
    {
        private GetSunPositionQuery.GetSunPositionQueryHandler _handler;
        private Mock<ISunService> _sunServiceMock;

        [SetUp]
        public void SetUp()
        {
            _sunServiceMock = new Mock<ISunService>();
            _handler = new GetSunPositionQuery.GetSunPositionQueryHandler(_sunServiceMock.Object);
        }

        [Test]
        public void Constructor_WithNullSunService_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            FluentActions.Invoking(() => new GetSunPositionQuery.GetSunPositionQueryHandler(null))
                .Should().Throw<ArgumentNullException>()
                .WithParameterName("sunService");
        }

        [Test]
        public async Task Handle_WithValidQuery_ShouldReturnSunPosition()
        {
            // Arrange
            var query = new GetSunPositionQuery
            {
                Latitude = 47.6062,
                Longitude = -122.3321,
                DateTime = new DateTime(2024, 5, 15, 12, 0, 0)
            };

            var sunPosition = new SunPositionDto
            {
                Azimuth = 180.0, // Due south at noon
                Elevation = 60.0, // High in the sky
                Latitude = query.Latitude,
                Longitude = query.Longitude,
                DateTime = query.DateTime
            };

            _sunServiceMock
                .Setup(x => x.GetSunPositionAsync(
                    query.Latitude,
                    query.Longitude,
                    query.DateTime,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<SunPositionDto>.Success(sunPosition));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().Be(sunPosition);

            _sunServiceMock.Verify(x => x.GetSunPositionAsync(
                query.Latitude,
                query.Longitude,
                query.DateTime,
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_WhenServiceReturnsFailure_ShouldReturnFailure()
        {
            // Arrange
            var query = new GetSunPositionQuery
            {
                Latitude = 47.6062,
                Longitude = -122.3321,
                DateTime = new DateTime(2024, 5, 15, 12, 0, 0)
            };

            var errorMessage = "Error calculating sun position";

            _sunServiceMock
                .Setup(x => x.GetSunPositionAsync(
                    query.Latitude,
                    query.Longitude,
                    query.DateTime,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<SunPositionDto>.Failure(errorMessage));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be(errorMessage);
        }

        [Test]
        public async Task Handle_WithExtremeLatitude_ShouldPassToService()
        {
            // Arrange
            var query = new GetSunPositionQuery
            {
                Latitude = 89.9, // Near North Pole
                Longitude = -122.3321,
                DateTime = new DateTime(2024, 5, 15, 12, 0, 0)
            };

            var sunPosition = new SunPositionDto
            {
                Azimuth = 180.0,
                Elevation = 23.5, // Low on the horizon
                Latitude = query.Latitude,
                Longitude = query.Longitude,
                DateTime = query.DateTime
            };

            _sunServiceMock
                .Setup(x => x.GetSunPositionAsync(
                    query.Latitude,
                    query.Longitude,
                    query.DateTime,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<SunPositionDto>.Success(sunPosition));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();

            // Verify the exact latitude was passed to the service
            _sunServiceMock.Verify(x => x.GetSunPositionAsync(
                89.9,
                It.IsAny<double>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_WithExtremeLongitude_ShouldPassToService()
        {
            // Arrange
            var query = new GetSunPositionQuery
            {
                Latitude = 47.6062,
                Longitude = 179.9, // Near International Date Line
                DateTime = new DateTime(2024, 5, 15, 12, 0, 0)
            };

            var sunPosition = new SunPositionDto
            {
                Azimuth = 180.0,
                Elevation = 60.0,
                Latitude = query.Latitude,
                Longitude = query.Longitude,
                DateTime = query.DateTime
            };

            _sunServiceMock
                .Setup(x => x.GetSunPositionAsync(
                    query.Latitude,
                    query.Longitude,
                    query.DateTime,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<SunPositionDto>.Success(sunPosition));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();

            // Verify the exact longitude was passed to the service
            _sunServiceMock.Verify(x => x.GetSunPositionAsync(
                It.IsAny<double>(),
                179.9,
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_WithHistoricalDate_ShouldPassToService()
        {
            // Arrange
            var query = new GetSunPositionQuery
            {
                Latitude = 47.6062,
                Longitude = -122.3321,
                DateTime = new DateTime(1900, 1, 1, 12, 0, 0) // Historical date
            };

            var sunPosition = new SunPositionDto
            {
                Azimuth = 180.0,
                Elevation = 20.0,
                Latitude = query.Latitude,
                Longitude = query.Longitude,
                DateTime = query.DateTime
            };

            _sunServiceMock
                .Setup(x => x.GetSunPositionAsync(
                    query.Latitude,
                    query.Longitude,
                    query.DateTime,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<SunPositionDto>.Success(sunPosition));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();

            // Verify the historical date was passed to the service
            _sunServiceMock.Verify(x => x.GetSunPositionAsync(
                It.IsAny<double>(),
                It.IsAny<double>(),
                new DateTime(1900, 1, 1, 12, 0, 0),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_WithFutureDate_ShouldPassToService()
        {
            // Arrange
            var query = new GetSunPositionQuery
            {
                Latitude = 47.6062,
                Longitude = -122.3321,
                DateTime = new DateTime(2050, 1, 1, 12, 0, 0) // Future date
            };

            var sunPosition = new SunPositionDto
            {
                Azimuth = 180.0,
                Elevation = 20.0,
                Latitude = query.Latitude,
                Longitude = query.Longitude,
                DateTime = query.DateTime
            };

            _sunServiceMock
                .Setup(x => x.GetSunPositionAsync(
                    query.Latitude,
                    query.Longitude,
                    query.DateTime,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<SunPositionDto>.Success(sunPosition));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();

            // Verify the future date was passed to the service
            _sunServiceMock.Verify(x => x.GetSunPositionAsync(
                It.IsAny<double>(),
                It.IsAny<double>(),
                new DateTime(2050, 1, 1, 12, 0, 0),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_WithCancellationToken_ShouldPassItToService()
        {
            // Arrange
            var query = new GetSunPositionQuery
            {
                Latitude = 47.6062,
                Longitude = -122.3321,
                DateTime = new DateTime(2024, 5, 15, 12, 0, 0)
            };

            var cancellationToken = new CancellationToken();

            _sunServiceMock
                .Setup(x => x.GetSunPositionAsync(
                    query.Latitude,
                    query.Longitude,
                    query.DateTime,
                    cancellationToken))
                .ReturnsAsync(Result<SunPositionDto>.Success(new SunPositionDto()));

            // Act
            await _handler.Handle(query, cancellationToken);

            // Assert
            _sunServiceMock.Verify(x => x.GetSunPositionAsync(
                query.Latitude,
                query.Longitude,
                query.DateTime,
                cancellationToken), Times.Once);
        }
    }
}