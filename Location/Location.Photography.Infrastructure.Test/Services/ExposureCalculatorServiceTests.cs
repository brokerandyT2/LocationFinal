using FluentAssertions;
using Location.Photography.Application.Services;
using Location.Photography.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Photography.Infrastructure.Test.Services
{
    [TestFixture]
    public class ExposureCalculatorServiceTests
    {
        private ExposureCalculatorService _exposureCalculatorService;
        private Mock<ILogger<ExposureCalculatorService>> _loggerMock;
        private Mock<IExposureTriangleService> _exposureTriangleServiceMock;

        [SetUp]
        public void SetUp()
        {
            _loggerMock = new Mock<ILogger<ExposureCalculatorService>>();
            _exposureTriangleServiceMock = new Mock<IExposureTriangleService>();
            _exposureCalculatorService = new ExposureCalculatorService(
                _loggerMock.Object,
                _exposureTriangleServiceMock.Object);
        }

        [Test]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            FluentActions.Invoking(() => new ExposureCalculatorService(null, _exposureTriangleServiceMock.Object))
                .Should().Throw<ArgumentNullException>()
                .WithParameterName("logger");
        }

        [Test]
        public void Constructor_WithNullExposureTriangleService_ThrowsArgumentNullException()
        {
            // Act & Assert
            FluentActions.Invoking(() => new ExposureCalculatorService(_loggerMock.Object, null))
                .Should().Throw<ArgumentNullException>()
                .WithParameterName("exposureTriangleService");
        }

        [Test]
        public async Task CalculateShutterSpeedAsync_WithValidParameters_ReturnsSuccess()
        {
            // Arrange
            var baseExposure = new ExposureTriangleDto
            {
                ShutterSpeed = "1/125",
                Aperture = "f/8",
                Iso = "100"
            };
            var targetAperture = "f/11";
            var targetIso = "200";
            var increments = ExposureIncrements.Full;
            var evCompensation = 0.0;
            var expectedShutterSpeed = "1/60";

            _exposureTriangleServiceMock
                .Setup(x => x.CalculateShutterSpeed(
                    baseExposure.ShutterSpeed,
                    baseExposure.Aperture,
                    baseExposure.Iso,
                    targetAperture,
                    targetIso,
                    1, // Full stop increments
                    evCompensation))
                .Returns(expectedShutterSpeed);

            // Act
            var result = await _exposureCalculatorService.CalculateShutterSpeedAsync(
                baseExposure,
                targetAperture,
                targetIso,
                increments,
                CancellationToken.None,
                evCompensation);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.ShutterSpeed.Should().Be(expectedShutterSpeed);
            result.Data.Aperture.Should().Be(targetAperture);
            result.Data.Iso.Should().Be(targetIso);
        }

        [Test]
        public async Task CalculateShutterSpeedAsync_WhenExposureTriangleServiceThrows_ReturnsFailure()
        {
            // Arrange
            var baseExposure = new ExposureTriangleDto
            {
                ShutterSpeed = "1/125",
                Aperture = "f/8",
                Iso = "100"
            };
            var targetAperture = "f/11";
            var targetIso = "200";
            var increments = ExposureIncrements.Full;

            _exposureTriangleServiceMock
                .Setup(x => x.CalculateShutterSpeed(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<double>()))
                .Throws(new Exception("Test exception"));

            // Act
            var result = await _exposureCalculatorService.CalculateShutterSpeedAsync(
                baseExposure,
                targetAperture,
                targetIso,
                increments);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Error calculating shutter speed");
        }

        [Test]
        public async Task CalculateApertureAsync_WithValidParameters_ReturnsSuccess()
        {
            // Arrange
            var baseExposure = new ExposureTriangleDto
            {
                ShutterSpeed = "1/125",
                Aperture = "f/8",
                Iso = "100"
            };
            var targetShutterSpeed = "1/60";
            var targetIso = "200";
            var increments = ExposureIncrements.Full;
            var evCompensation = 0.0;
            var expectedAperture = "f/11";

            _exposureTriangleServiceMock
                .Setup(x => x.CalculateAperture(
                    baseExposure.ShutterSpeed,
                    baseExposure.Aperture,
                    baseExposure.Iso,
                    targetShutterSpeed,
                    targetIso,
                    1, // Full stop increments
                    evCompensation))
                .Returns(expectedAperture);

            // Act
            var result = await _exposureCalculatorService.CalculateApertureAsync(
                baseExposure,
                targetShutterSpeed,
                targetIso,
                increments,
                CancellationToken.None,
                evCompensation);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.ShutterSpeed.Should().Be(targetShutterSpeed);
            result.Data.Aperture.Should().Be(expectedAperture);
            result.Data.Iso.Should().Be(targetIso);
        }

        [Test]
        public async Task CalculateApertureAsync_WhenExposureTriangleServiceThrows_ReturnsFailure()
        {
            // Arrange
            var baseExposure = new ExposureTriangleDto
            {
                ShutterSpeed = "1/125",
                Aperture = "f/8",
                Iso = "100"
            };
            var targetShutterSpeed = "1/60";
            var targetIso = "200";
            var increments = ExposureIncrements.Full;

            _exposureTriangleServiceMock
                .Setup(x => x.CalculateAperture(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<double>()))
                .Throws(new Exception("Test exception"));

            // Act
            var result = await _exposureCalculatorService.CalculateApertureAsync(
                baseExposure,
                targetShutterSpeed,
                targetIso,
                increments);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Error calculating aperture");
        }

        [Test]
        public async Task CalculateIsoAsync_WithValidParameters_ReturnsSuccess()
        {
            // Arrange
            var baseExposure = new ExposureTriangleDto
            {
                ShutterSpeed = "1/125",
                Aperture = "f/8",
                Iso = "100"
            };
            var targetShutterSpeed = "1/60";
            var targetAperture = "f/11";
            var increments = ExposureIncrements.Full;
            var evCompensation = 0.0;
            var expectedIso = "400";

            _exposureTriangleServiceMock
                .Setup(x => x.CalculateIso(
                    baseExposure.ShutterSpeed,
                    baseExposure.Aperture,
                    baseExposure.Iso,
                    targetShutterSpeed,
                    targetAperture,
                    1, // Full stop increments
                    evCompensation))
                .Returns(expectedIso);

            // Act
            var result = await _exposureCalculatorService.CalculateIsoAsync(
                baseExposure,
                targetShutterSpeed,
                targetAperture,
                increments,
                CancellationToken.None,
                evCompensation);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.ShutterSpeed.Should().Be(targetShutterSpeed);
            result.Data.Aperture.Should().Be(targetAperture);
            result.Data.Iso.Should().Be(expectedIso);
        }

        [Test]
        public async Task CalculateIsoAsync_WhenExposureTriangleServiceThrows_ReturnsFailure()
        {
            // Arrange
            var baseExposure = new ExposureTriangleDto
            {
                ShutterSpeed = "1/125",
                Aperture = "f/8",
                Iso = "100"
            };
            var targetShutterSpeed = "1/60";
            var targetAperture = "f/11";
            var increments = ExposureIncrements.Full;

            _exposureTriangleServiceMock
                .Setup(x => x.CalculateIso(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<double>()))
                .Throws(new Exception("Test exception"));

            // Act
            var result = await _exposureCalculatorService.CalculateIsoAsync(
                baseExposure,
                targetShutterSpeed,
                targetAperture,
                increments);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Error calculating ISO");
        }

        [Test]
        public async Task GetShutterSpeedsAsync_WithFullIncrements_ReturnsCorrectValues()
        {
            // Arrange
            var increments = ExposureIncrements.Full;
            var expectedShutterSpeeds = new[] { "30\"", "15\"", "8\"", "4\"", "2\"", "1\"", "0.5", "1/4", "1/8", "1/15", "1/30", "1/60", "1/125", "1/250", "1/500", "1/1000", "1/2000", "1/4000", "1/8000" };

            // Act
            var result = await _exposureCalculatorService.GetShutterSpeedsAsync(increments);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeEquivalentTo(expectedShutterSpeeds);
        }

        [Test]
        public async Task GetAperturesAsync_WithFullIncrements_ReturnsCorrectValues()
        {
            // Arrange
            var increments = ExposureIncrements.Full;
            var expectedApertures = new[] { "f/1", "f/1.4", "f/2", "f/2.8", "f/4", "f/5.6", "f/8", "f/11", "f/16", "f/22", "f/32", "f/45", "f/64" };

            // Act
            var result = await _exposureCalculatorService.GetAperturesAsync(increments);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeEquivalentTo(expectedApertures);
        }

        [Test]
        public async Task GetIsosAsync_WithFullIncrements_ReturnsCorrectValues()
        {
            // Arrange
            var increments = ExposureIncrements.Full;
            var expectedIsos = new[] { "25600", "12800", "6400", "3200", "1600", "800", "400", "200", "100", "50" };

            // Act
            var result = await _exposureCalculatorService.GetIsosAsync(increments);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeEquivalentTo(expectedIsos);
        }

        [Test]
        public async Task CalculateShutterSpeedAsync_WithCancellationToken_ThrowsWhenCancelled()
        {
            // Arrange
            var baseExposure = new ExposureTriangleDto
            {
                ShutterSpeed = "1/125",
                Aperture = "f/8",
                Iso = "100"
            };
            var targetAperture = "f/11";
            var targetIso = "200";
            var increments = ExposureIncrements.Full;
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await FluentActions.Invoking(() => _exposureCalculatorService.CalculateShutterSpeedAsync(
                baseExposure,
                targetAperture,
                targetIso,
                increments,
                cts.Token))
                .Should().ThrowAsync<OperationCanceledException>();
        }

        [Test]
        public async Task CalculateApertureAsync_WithCancellationToken_ThrowsWhenCancelled()
        {
            // Arrange
            var baseExposure = new ExposureTriangleDto
            {
                ShutterSpeed = "1/125",
                Aperture = "f/8",
                Iso = "100"
            };
            var targetShutterSpeed = "1/60";
            var targetIso = "200";
            var increments = ExposureIncrements.Full;
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await FluentActions.Invoking(() => _exposureCalculatorService.CalculateApertureAsync(
                baseExposure,
                targetShutterSpeed,
                targetIso,
                increments,
                cts.Token))
                .Should().ThrowAsync<OperationCanceledException>();
        }

        [Test]
        public async Task CalculateIsoAsync_WithCancellationToken_ThrowsWhenCancelled()
        {
            // Arrange
            var baseExposure = new ExposureTriangleDto
            {
                ShutterSpeed = "1/125",
                Aperture = "f/8",
                Iso = "100"
            };
            var targetShutterSpeed = "1/60";
            var targetAperture = "f/11";
            var increments = ExposureIncrements.Full;
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await FluentActions.Invoking(() => _exposureCalculatorService.CalculateIsoAsync(
                baseExposure,
                targetShutterSpeed,
                targetAperture,
                increments,
                cts.Token))
                .Should().ThrowAsync<OperationCanceledException>();
        }
    }
}