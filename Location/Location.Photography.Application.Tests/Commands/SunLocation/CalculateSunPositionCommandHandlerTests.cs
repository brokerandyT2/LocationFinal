// Location.Photography.Application/Commands/ExposureCalculator/CalculateExposureCommandValidator.cs
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
        public async Task Handle_WithExtremeLatitude_ShouldPassToService()
        {
            // Arrange
            var command = new CalculateSunPositionCommand
            {
                Latitude = 89.5, // Near North Pole
                Longitude = 0.0,
                DateTime = new DateTime(2024, 6, 21, 12, 0, 0) // Summer solstice
            };

            var sunPosition = new SunPositionDto
            {
                Azimuth = 180.0,
                Elevation = 23.5, // Near the tropic
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
            result.Data.Latitude.Should().Be(89.5);
            result.Data.Longitude.Should().Be(0.0);
        }

        [Test]
        public async Task Handle_WithHistoricalDate_ShouldPassToService()
        {
            // Arrange
            var command = new CalculateSunPositionCommand
            {
                Latitude = 40.7128,
                Longitude = -74.0060,
                DateTime = new DateTime(1969, 7, 20, 12, 0, 0) // Apollo 11 moon landing
            };

            var sunPosition = new SunPositionDto
            {
                Azimuth = 190.0,
                Elevation = 70.0,
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
            result.Data.DateTime.Should().Be(new DateTime(1969, 7, 20, 12, 0, 0));

            _sunServiceMock.Verify(x => x.GetSunPositionAsync(
                40.7128,
                -74.0060,
                new DateTime(1969, 7, 20, 12, 0, 0),
                It.IsAny<CancellationToken>()), Times.Once);
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