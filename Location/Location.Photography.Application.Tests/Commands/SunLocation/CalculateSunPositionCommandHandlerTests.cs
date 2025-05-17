using FluentAssertions;
using Location.Core.Application.Common.Models;
using Location.Photography.Application.Commands.SunLocation;
using Location.Photography.Application.Services;
using Location.Photography.Domain.Models;
using Moq;
using NUnit.Framework;

namespace Location.Photography.Application.Tests.Commands.SunLocation
{
    [TestFixture]
    public class CalculateSunPositionCommandHandlerTests
    {
        private Mock<ISunService> _sunServiceMock;
        private CalculateSunPositionCommand.CalculateSunPositionCommandHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _sunServiceMock = new Mock<ISunService>();
            _handler = new CalculateSunPositionCommand.CalculateSunPositionCommandHandler(_sunServiceMock.Object);
        }

        [Test]
        public void Constructor_WithNullSunService_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            FluentActions.Invoking(() => new CalculateSunPositionCommand.CalculateSunPositionCommandHandler(null))
                .Should().Throw<ArgumentNullException>()
                .WithParameterName("sunService");
        }

        [Test]
        public async Task Handle_WithValidCoordinates_ShouldReturnSunPosition()
        {
            // Arrange
            var command = new CalculateSunPositionCommand
            {
                Latitude = 47.6062,
                Longitude = -122.3321,
                DateTime = new DateTime(2024, 6, 15, 12, 0, 0)
            };

            var sunPosition = new SunPositionDto
            {
                Azimuth = 180.5,
                Elevation = 65.3,
                DateTime = command.DateTime,
                Latitude = command.Latitude,
                Longitude = command.Longitude
            };

            _sunServiceMock
                .Setup(x => x.GetSunPositionAsync(
                    command.Latitude,
                    command.Longitude,
                    command.DateTime,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<SunPositionDto>.Success(sunPosition));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Azimuth.Should().Be(180.5);
            result.Data.Elevation.Should().Be(65.3);
            result.Data.DateTime.Should().Be(command.DateTime);
            result.Data.Latitude.Should().Be(command.Latitude);
            result.Data.Longitude.Should().Be(command.Longitude);

            _sunServiceMock.Verify(x => x.GetSunPositionAsync(
                command.Latitude,
                command.Longitude,
                command.DateTime,
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_WithExtremeLatitude_ShouldReturnSunPosition()
        {
            // Arrange
            var command = new CalculateSunPositionCommand
            {
                Latitude = 89.9, // Near North Pole
                Longitude = -122.3321,
                DateTime = new DateTime(2024, 6, 21, 12, 0, 0) // Summer solstice
            };

            var sunPosition = new SunPositionDto
            {
                Azimuth = 180.0,
                Elevation = 23.4, // Approximate maximum elevation at North Pole on solstice
                DateTime = command.DateTime,
                Latitude = command.Latitude,
                Longitude = command.Longitude
            };

            _sunServiceMock
                .Setup(x => x.GetSunPositionAsync(
                    command.Latitude,
                    command.Longitude,
                    command.DateTime,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<SunPositionDto>.Success(sunPosition));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Azimuth.Should().Be(180.0);
            result.Data.Elevation.Should().Be(23.4);

            _sunServiceMock.Verify(x => x.GetSunPositionAsync(
                command.Latitude,
                command.Longitude,
                command.DateTime,
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_WithExtremeDate_ShouldReturnSunPosition()
        {
            // Arrange
            var command = new CalculateSunPositionCommand
            {
                Latitude = 47.6062,
                Longitude = -122.3321,
                DateTime = new DateTime(2024, 12, 21, 12, 0, 0) // Winter solstice
            };

            var sunPosition = new SunPositionDto
            {
                Azimuth = 180.5,
                Elevation = 18.7, // Lower elevation in winter
                DateTime = command.DateTime,
                Latitude = command.Latitude,
                Longitude = command.Longitude
            };

            _sunServiceMock
                .Setup(x => x.GetSunPositionAsync(
                    command.Latitude,
                    command.Longitude,
                    command.DateTime,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<SunPositionDto>.Success(sunPosition));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Azimuth.Should().Be(180.5);
            result.Data.Elevation.Should().Be(18.7);

            _sunServiceMock.Verify(x => x.GetSunPositionAsync(
                command.Latitude,
                command.Longitude,
                command.DateTime,
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_WhenSunServiceFails_ShouldReturnFailure()
        {
            // Arrange
            var command = new CalculateSunPositionCommand
            {
                Latitude = 47.6062,
                Longitude = -122.3321,
                DateTime = new DateTime(2024, 6, 15, 12, 0, 0)
            };

            _sunServiceMock
                .Setup(x => x.GetSunPositionAsync(
                    command.Latitude,
                    command.Longitude,
                    command.DateTime,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<SunPositionDto>.Failure("Failed to calculate sun position"));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Failed to calculate sun position");
        }

        [Test]
        public async Task Handle_WithCancellationToken_ShouldPassItThrough()
        {
            // Arrange
            var command = new CalculateSunPositionCommand
            {
                Latitude = 47.6062,
                Longitude = -122.3321,
                DateTime = new DateTime(2024, 6, 15, 12, 0, 0)
            };
            var cancellationToken = new CancellationToken();

            var sunPosition = new SunPositionDto
            {
                Azimuth = 180.5,
                Elevation = 65.3,
                DateTime = command.DateTime,
                Latitude = command.Latitude,
                Longitude = command.Longitude
            };

            _sunServiceMock
                .Setup(x => x.GetSunPositionAsync(
                    command.Latitude,
                    command.Longitude,
                    command.DateTime,
                    cancellationToken))
                .ReturnsAsync(Result<SunPositionDto>.Success(sunPosition));

            // Act
            await _handler.Handle(command, cancellationToken);

            // Assert
            _sunServiceMock.Verify(x => x.GetSunPositionAsync(
                command.Latitude,
                command.Longitude,
                command.DateTime,
                cancellationToken), Times.Once);
        }

        [Test]
        public async Task Handle_WhenExceptionThrown_ShouldReturnFailure()
        {
            // Arrange
            var command = new CalculateSunPositionCommand
            {
                Latitude = 47.6062,
                Longitude = -122.3321,
                DateTime = new DateTime(2024, 6, 15, 12, 0, 0)
            };

            _sunServiceMock
                .Setup(x => x.GetSunPositionAsync(
                    command.Latitude,
                    command.Longitude,
                    command.DateTime,
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Unexpected error"));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Error calculating sun position");
            result.ErrorMessage.Should().Contain("Unexpected error");
        }

        [Test]
        public async Task Handle_ShouldReturnResultFromSunService()
        {
            // Arrange
            var command = new CalculateSunPositionCommand
            {
                Latitude = 47.6062,
                Longitude = -122.3321,
                DateTime = new DateTime(2024, 6, 15, 12, 0, 0)
            };

            var sunPosition = new SunPositionDto
            {
                Azimuth = 180.5,
                Elevation = 65.3,
                DateTime = command.DateTime,
                Latitude = command.Latitude,
                Longitude = command.Longitude
            };

            var serviceResult = Result<SunPositionDto>.Success(sunPosition);

            _sunServiceMock
                .Setup(x => x.GetSunPositionAsync(
                    command.Latitude,
                    command.Longitude,
                    command.DateTime,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceResult);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().BeSameAs(serviceResult);
        }
    }
}