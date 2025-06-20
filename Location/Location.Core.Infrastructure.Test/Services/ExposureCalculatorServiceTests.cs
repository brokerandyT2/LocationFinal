﻿// Location.Photography.Infrastructure.Tests/Services/ExposureCalculatorServiceTests.cs
using FluentAssertions;
using Location.Photography.Application.Errors;
using Location.Photography.Application.Services;
using Location.Photography.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Location.Photography.Infrastructure.Tests.Services
{
    [TestFixture]
    public class ExposureCalculatorServiceTests
    {
        private ExposureCalculatorService _exposureCalculatorService;
        private Mock<IExposureTriangleService> _exposureTriangleServiceMock;
        private Mock<ILogger<ExposureCalculatorService>> _loggerMock;

        [SetUp]
        public void SetUp()
        {
            _exposureTriangleServiceMock = new Mock<IExposureTriangleService>();
            _loggerMock = new Mock<ILogger<ExposureCalculatorService>>();
            _exposureCalculatorService = new ExposureCalculatorService(_loggerMock.Object, _exposureTriangleServiceMock.Object);
        }

        [Test]
        public async Task CalculateShutterSpeedAsync_ShouldReturnCorrectExposure_WhenAllParametersValid()
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

            _exposureTriangleServiceMock
                .Setup(x => x.CalculateShutterSpeed(
                    baseExposure.ShutterSpeed,
                    baseExposure.Aperture,
                    baseExposure.Iso,
                    targetAperture,
                    targetIso,
                    1, // Scale for Full increments
                    evCompensation))
                .Returns("1/60");

            // Act
            var result = await _exposureCalculatorService.CalculateShutterSpeedAsync(
                baseExposure, targetAperture, targetIso, increments, CancellationToken.None, evCompensation);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.ShutterSpeed.Should().Be("1/60");
            result.Data.Aperture.Should().Be(targetAperture);
            result.Data.Iso.Should().Be(targetIso);

            _exposureTriangleServiceMock.Verify(
                x => x.CalculateShutterSpeed(
                    baseExposure.ShutterSpeed,
                    baseExposure.Aperture,
                    baseExposure.Iso,
                    targetAperture,
                    targetIso,
                    1,
                    evCompensation),
                Times.Once);
        }

        [Test]
        public async Task CalculateApertureAsync_ShouldReturnCorrectExposure_WhenAllParametersValid()
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

            _exposureTriangleServiceMock
                .Setup(x => x.CalculateAperture(
                    baseExposure.ShutterSpeed,
                    baseExposure.Aperture,
                    baseExposure.Iso,
                    targetShutterSpeed,
                    targetIso,
                    1, // Scale for Full increments
                    evCompensation))
                .Returns("f/11");

            // Act
            var result = await _exposureCalculatorService.CalculateApertureAsync(
                baseExposure, targetShutterSpeed, targetIso, increments, CancellationToken.None, evCompensation);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.ShutterSpeed.Should().Be(targetShutterSpeed);
            result.Data.Aperture.Should().Be("f/11");
            result.Data.Iso.Should().Be(targetIso);

            _exposureTriangleServiceMock.Verify(
                x => x.CalculateAperture(
                    baseExposure.ShutterSpeed,
                    baseExposure.Aperture,
                    baseExposure.Iso,
                    targetShutterSpeed,
                    targetIso,
                    1,
                    evCompensation),
                Times.Once);
        }

        [Test]
        public async Task CalculateIsoAsync_ShouldReturnCorrectExposure_WhenAllParametersValid()
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

            _exposureTriangleServiceMock
                .Setup(x => x.CalculateIso(
                    baseExposure.ShutterSpeed,
                    baseExposure.Aperture,
                    baseExposure.Iso,
                    targetShutterSpeed,
                    targetAperture,
                    1, // Scale for Full increments
                    evCompensation))
                .Returns("400");

            // Act
            var result = await _exposureCalculatorService.CalculateIsoAsync(
                baseExposure, targetShutterSpeed, targetAperture, increments, CancellationToken.None, evCompensation);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.ShutterSpeed.Should().Be(targetShutterSpeed);
            result.Data.Aperture.Should().Be(targetAperture);
            result.Data.Iso.Should().Be("400");

            _exposureTriangleServiceMock.Verify(
                x => x.CalculateIso(
                    baseExposure.ShutterSpeed,
                    baseExposure.Aperture,
                    baseExposure.Iso,
                    targetShutterSpeed,
                    targetAperture,
                    1,
                    evCompensation),
                Times.Once);
        }

        [Test]
        public async Task CalculateShutterSpeedAsync_ShouldHandleServiceError_AndReturnFailure()
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
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<double>()))
                .Throws(new OverexposedError(2.0));

            // Act
            var result = await _exposureCalculatorService.CalculateShutterSpeedAsync(
                baseExposure, targetAperture, targetIso, increments, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("overexposed");
        }

        [Test]
        public async Task CalculateApertureAsync_ShouldHandleServiceError_AndReturnFailure()
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
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<double>()))
                .Throws(new UnderexposedError(1.5));

            // Act
            var result = await _exposureCalculatorService.CalculateApertureAsync(
                baseExposure, targetShutterSpeed, targetIso, increments, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("underexposed");
        }

        [Test]
        public async Task CalculateIsoAsync_ShouldHandleParameterLimitError_AndReturnFailure()
        {
            // Arrange
            var baseExposure = new ExposureTriangleDto
            {
                ShutterSpeed = "1/125",
                Aperture = "f/8",
                Iso = "100"
            };

            var targetShutterSpeed = "1/1000";
            var targetAperture = "f/16";
            var increments = ExposureIncrements.Full;

            _exposureTriangleServiceMock
                .Setup(x => x.CalculateIso(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<double>()))
                .Throws(new ExposureParameterLimitError("ISO", "51200", "25600"));

            // Act
            var result = await _exposureCalculatorService.CalculateIsoAsync(
                baseExposure, targetShutterSpeed, targetAperture, increments, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("exceeds available limits");
        }

        [Test]
        public async Task CalculateShutterSpeedAsync_ShouldApplyEvCompensation_WhenProvided()
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
            var evCompensation = 1.0; // +1 EV

            _exposureTriangleServiceMock
                .Setup(x => x.CalculateShutterSpeed(
                    baseExposure.ShutterSpeed,
                    baseExposure.Aperture,
                    baseExposure.Iso,
                    targetAperture,
                    targetIso,
                    1, // Scale for Full increments
                    evCompensation))
                .Returns("1/30"); // With +1 EV compensation, shutter speed should be slower

            // Act
            var result = await _exposureCalculatorService.CalculateShutterSpeedAsync(
                baseExposure, targetAperture, targetIso, increments, CancellationToken.None, evCompensation);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.ShutterSpeed.Should().Be("1/30");

            // Verify the ev compensation was passed to the domain service
            _exposureTriangleServiceMock.Verify(x => x.CalculateShutterSpeed(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), 1.0), Times.Once);
        }

        [Test]
        public async Task GetShutterSpeedsAsync_ShouldReturnCorrectShutterSpeeds_ForFullIncrements()
        {
            // Arrange
            var increments = ExposureIncrements.Full;

            // Act
            var result = await _exposureCalculatorService.GetShutterSpeedsAsync(increments, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().BeEquivalentTo(ShutterSpeeds.Full);
        }

        [Test]
        public async Task GetAperturesAsync_ShouldReturnCorrectApertures_ForFullIncrements()
        {
            // Arrange
            var increments = ExposureIncrements.Full;

            // Act
            var result = await _exposureCalculatorService.GetAperturesAsync(increments, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().BeEquivalentTo(Apetures.Full);
        }

        [Test]
        public async Task GetIsosAsync_ShouldReturnCorrectIsos_ForFullIncrements()
        {
            // Arrange
            var increments = ExposureIncrements.Full;

            // Act
            var result = await _exposureCalculatorService.GetIsosAsync(increments, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().BeEquivalentTo(ISOs.Full);
        }

        [Test]
        public async Task CalculateShutterSpeedAsync_ShouldHonorCancellationToken()
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

            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            // Act & Assert
            await FluentActions.Invoking(() => _exposureCalculatorService.CalculateShutterSpeedAsync(
                baseExposure, targetAperture, targetIso, increments, cancellationTokenSource.Token))
                .Should().ThrowAsync<OperationCanceledException>();
        }

        [Test]
        public async Task CalculateApertureAsync_ShouldHonorCancellationToken()
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

            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            // Act & Assert
            await FluentActions.Invoking(() => _exposureCalculatorService.CalculateApertureAsync(
                baseExposure, targetShutterSpeed, targetIso, increments, cancellationTokenSource.Token))
                .Should().ThrowAsync<OperationCanceledException>();
        }

        [Test]
        public async Task CalculateIsoAsync_ShouldHonorCancellationToken()
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

            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            // Act & Assert
            await FluentActions.Invoking(() => _exposureCalculatorService.CalculateIsoAsync(
                baseExposure, targetShutterSpeed, targetAperture, increments, cancellationTokenSource.Token))
                .Should().ThrowAsync<OperationCanceledException>();
        }
    }
}