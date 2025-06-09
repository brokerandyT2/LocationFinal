using FluentAssertions;
using Location.Core.Application.Common.Models;
using Location.Photography.Application.Commands.ExposureCalculator;
using Location.Photography.Application.Services;
using Moq;
using NUnit.Framework;

namespace Location.Photography.Application.Tests.Commands.ExposureCalculator
{
    [TestFixture]
    public class CalculateExposureCommandHandlerTests
    {
        private CalculateExposureCommandHandler _handler;
        private Mock<IExposureCalculatorService> _exposureCalculatorServiceMock;

        [SetUp]
        public void SetUp()
        {
            _exposureCalculatorServiceMock = new Mock<IExposureCalculatorService>();
            _handler = new CalculateExposureCommandHandler(_exposureCalculatorServiceMock.Object);
        }

        [Test]
        public void Constructor_WithNullExposureCalculatorService_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            FluentActions.Invoking(() => new CalculateExposureCommandHandler(null))
                .Should().Throw<ArgumentNullException>()
                .WithParameterName("exposureCalculatorService");
        }

        [Test]
        public async Task Handle_WithFixedShutterSpeed_ShouldCalculateShutterSpeed()
        {
            // Arrange
            var command = new CalculateExposureCommand
            {
                BaseExposure = new ExposureTriangleDto
                {
                    ShutterSpeed = "1/125",
                    Aperture = "f/8",
                    Iso = "100"
                },
                TargetAperture = "f/11",
                TargetIso = "200",
                Increments = ExposureIncrements.Full,
                ToCalculate = FixedValue.ShutterSpeeds,
                EvCompensation = 0.0
            };

            var expectedResult = new ExposureSettingsDto
            {
                ShutterSpeed = "1/60",
                Aperture = "f/11",
                Iso = "200"
            };

            _exposureCalculatorServiceMock
                .Setup(x => x.CalculateShutterSpeedAsync(
                    It.IsAny<ExposureTriangleDto>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<ExposureIncrements>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<double>()))
                .ReturnsAsync(Result<ExposureSettingsDto>.Success(expectedResult));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be(expectedResult);

            _exposureCalculatorServiceMock.Verify(x => x.CalculateShutterSpeedAsync(
                command.BaseExposure,
                command.TargetAperture,
                command.TargetIso,
                command.Increments,
                It.IsAny<CancellationToken>(),
                command.EvCompensation), Times.Once);
        }

        [Test]
        public async Task Handle_WithFixedAperture_ShouldCalculateAperture()
        {
            // Arrange
            var command = new CalculateExposureCommand
            {
                BaseExposure = new ExposureTriangleDto
                {
                    ShutterSpeed = "1/125",
                    Aperture = "f/8",
                    Iso = "100"
                },
                TargetShutterSpeed = "1/60",
                TargetIso = "200",
                Increments = ExposureIncrements.Full,
                ToCalculate = FixedValue.Aperture,
                EvCompensation = 0.0
            };

            var expectedResult = new ExposureSettingsDto
            {
                ShutterSpeed = "1/60",
                Aperture = "f/11",
                Iso = "200"
            };

            _exposureCalculatorServiceMock
                .Setup(x => x.CalculateApertureAsync(
                    It.IsAny<ExposureTriangleDto>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<ExposureIncrements>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<double>()))
                .ReturnsAsync(Result<ExposureSettingsDto>.Success(expectedResult));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be(expectedResult);

            _exposureCalculatorServiceMock.Verify(x => x.CalculateApertureAsync(
                command.BaseExposure,
                command.TargetShutterSpeed,
                command.TargetIso,
                command.Increments,
                It.IsAny<CancellationToken>(),
                command.EvCompensation), Times.Once);
        }

        [Test]
        public async Task Handle_WithFixedISO_ShouldCalculateISO()
        {
            // Arrange
            var command = new CalculateExposureCommand
            {
                BaseExposure = new ExposureTriangleDto
                {
                    ShutterSpeed = "1/125",
                    Aperture = "f/8",
                    Iso = "100"
                },
                TargetShutterSpeed = "1/60",
                TargetAperture = "f/11",
                Increments = ExposureIncrements.Full,
                ToCalculate = FixedValue.ISO,
                EvCompensation = 0.0
            };

            var expectedResult = new ExposureSettingsDto
            {
                ShutterSpeed = "1/60",
                Aperture = "f/11",
                Iso = "400"
            };

            _exposureCalculatorServiceMock
                .Setup(x => x.CalculateIsoAsync(
                    It.IsAny<ExposureTriangleDto>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<ExposureIncrements>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<double>()))
                .ReturnsAsync(Result<ExposureSettingsDto>.Success(expectedResult));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be(expectedResult);

            _exposureCalculatorServiceMock.Verify(x => x.CalculateIsoAsync(
                command.BaseExposure,
                command.TargetShutterSpeed,
                command.TargetAperture,
                command.Increments,
                It.IsAny<CancellationToken>(),
                command.EvCompensation), Times.Once);
        }

        [Test]
        public async Task Handle_WithInvalidCalculationType_ShouldReturnFailure()
        {
            // Arrange
            var command = new CalculateExposureCommand
            {
                BaseExposure = new ExposureTriangleDto
                {
                    ShutterSpeed = "1/125",
                    Aperture = "f/8",
                    Iso = "100"
                },
                TargetShutterSpeed = "1/60",
                TargetAperture = "f/11",
                Increments = ExposureIncrements.Full,
                ToCalculate = FixedValue.Empty, // Invalid calculation type
                EvCompensation = 0.0
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Invalid calculation type");
        }

        [Test]
        public async Task Handle_WhenServiceThrowsException_ShouldReturnFailure()
        {
            // Arrange
            var command = new CalculateExposureCommand
            {
                BaseExposure = new ExposureTriangleDto
                {
                    ShutterSpeed = "1/125",
                    Aperture = "f/8",
                    Iso = "100"
                },
                TargetAperture = "f/11",
                TargetIso = "200",
                Increments = ExposureIncrements.Full,
                ToCalculate = FixedValue.ShutterSpeeds,
                EvCompensation = 0.0
            };

            _exposureCalculatorServiceMock
                .Setup(x => x.CalculateShutterSpeedAsync(
                    It.IsAny<ExposureTriangleDto>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<ExposureIncrements>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<double>()))
                .ThrowsAsync(new Exception("Service error"));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Error calculating exposure");
            result.ErrorMessage.Should().Contain("Service error");
        }

        [Test]
        public async Task Handle_WhenServiceReturnsFailure_ShouldPassThroughFailure()
        {
            // Arrange
            var command = new CalculateExposureCommand
            {
                BaseExposure = new ExposureTriangleDto
                {
                    ShutterSpeed = "1/125",
                    Aperture = "f/8",
                    Iso = "100"
                },
                TargetAperture = "f/11",
                TargetIso = "200",
                Increments = ExposureIncrements.Full,
                ToCalculate = FixedValue.ShutterSpeeds,
                EvCompensation = 0.0
            };

            var serviceFailure = Result<ExposureSettingsDto>.Failure("Exposure error");

            _exposureCalculatorServiceMock
                .Setup(x => x.CalculateShutterSpeedAsync(
                    It.IsAny<ExposureTriangleDto>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<ExposureIncrements>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<double>()))
                .ReturnsAsync(serviceFailure);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Exposure error");
        }

        [Test]
        public async Task Handle_WithPositiveEvCompensation_ShouldPassItToService()
        {
            // Arrange
            var command = new CalculateExposureCommand
            {
                BaseExposure = new ExposureTriangleDto
                {
                    ShutterSpeed = "1/125",
                    Aperture = "f/8",
                    Iso = "100"
                },
                TargetAperture = "f/11",
                TargetIso = "200",
                Increments = ExposureIncrements.Full,
                ToCalculate = FixedValue.ShutterSpeeds,
                EvCompensation = 1.0 // +1 EV compensation
            };

            var expectedResult = new ExposureSettingsDto
            {
                ShutterSpeed = "1/30", // Slower shutter for +1 EV
                Aperture = "f/11",
                Iso = "200"
            };

            _exposureCalculatorServiceMock
                .Setup(x => x.CalculateShutterSpeedAsync(
                    It.IsAny<ExposureTriangleDto>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<ExposureIncrements>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<double>()))
                .ReturnsAsync(Result<ExposureSettingsDto>.Success(expectedResult));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();

            // Verify the EV compensation was passed to the service
            _exposureCalculatorServiceMock.Verify(x => x.CalculateShutterSpeedAsync(
                It.IsAny<ExposureTriangleDto>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<ExposureIncrements>(),
                It.IsAny<CancellationToken>(),
                1.0), Times.Once);
        }

        [Test]
        public async Task Handle_WithNegativeEvCompensation_ShouldPassItToService()
        {
            // Arrange
            var command = new CalculateExposureCommand
            {
                BaseExposure = new ExposureTriangleDto
                {
                    ShutterSpeed = "1/125",
                    Aperture = "f/8",
                    Iso = "100"
                },
                TargetAperture = "f/11",
                TargetIso = "200",
                Increments = ExposureIncrements.Full,
                ToCalculate = FixedValue.ShutterSpeeds,
                EvCompensation = -1.0 // -1 EV compensation
            };

            var expectedResult = new ExposureSettingsDto
            {
                ShutterSpeed = "1/250", // Faster shutter for -1 EV
                Aperture = "f/11",
                Iso = "200"
            };

            _exposureCalculatorServiceMock
                .Setup(x => x.CalculateShutterSpeedAsync(
                    It.IsAny<ExposureTriangleDto>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<ExposureIncrements>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<double>()))
                .ReturnsAsync(Result<ExposureSettingsDto>.Success(expectedResult));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();

            // Verify the EV compensation was passed to the service
            _exposureCalculatorServiceMock.Verify(x => x.CalculateShutterSpeedAsync(
                It.IsAny<ExposureTriangleDto>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<ExposureIncrements>(),
                It.IsAny<CancellationToken>(),
                -1.0), Times.Once);
        }
    }
}