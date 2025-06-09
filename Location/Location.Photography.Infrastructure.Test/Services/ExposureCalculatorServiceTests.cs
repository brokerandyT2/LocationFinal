using FluentAssertions;
using Location.Core.Application.Common.Models;
using Location.Photography.Application.DTOs;
using Location.Photography.Application.Errors;
using Location.Photography.Application.Services;
using Location.Photography.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

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

        #region Constructor Tests

        [Test]
        public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            FluentActions.Invoking(() => new ExposureCalculatorService(null, _exposureTriangleServiceMock.Object))
                .Should().Throw<ArgumentNullException>()
                .WithParameterName("logger");
        }

        [Test]
        public void Constructor_WithNullExposureTriangleService_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            FluentActions.Invoking(() => new ExposureCalculatorService(_loggerMock.Object, null))
                .Should().Throw<ArgumentNullException>()
                .WithParameterName("exposureTriangleService");
        }

        #endregion

        #region CalculateShutterSpeedAsync Tests

        [Test]
        public async Task CalculateShutterSpeedAsync_WithValidParameters_ShouldReturnSuccess()
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
        public async Task CalculateShutterSpeedAsync_WithHalfStopIncrements_ShouldUseCorrectScale()
        {
            // Arrange
            var baseExposure = new ExposureTriangleDto
            {
                ShutterSpeed = "1/125",
                Aperture = "f/8",
                Iso = "100"
            };
            var targetAperture = "f/9.5";
            var targetIso = "140";
            var increments = ExposureIncrements.Half;

            _exposureTriangleServiceMock
                .Setup(x => x.CalculateShutterSpeed(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), 2, It.IsAny<double>()))
                .Returns("1/90");

            // Act
            var result = await _exposureCalculatorService.CalculateShutterSpeedAsync(
                baseExposure, targetAperture, targetIso, increments);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _exposureTriangleServiceMock.Verify(
                x => x.CalculateShutterSpeed(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), 2, It.IsAny<double>()),
                Times.Once);
        }

        [Test]
        public async Task CalculateShutterSpeedAsync_WithThirdStopIncrements_ShouldUseCorrectScale()
        {
            // Arrange
            var baseExposure = new ExposureTriangleDto
            {
                ShutterSpeed = "1/125",
                Aperture = "f/8",
                Iso = "100"
            };
            var targetAperture = "f/9";
            var targetIso = "125";
            var increments = ExposureIncrements.Third;

            _exposureTriangleServiceMock
                .Setup(x => x.CalculateShutterSpeed(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), 3, It.IsAny<double>()))
                .Returns("1/100");

            // Act
            var result = await _exposureCalculatorService.CalculateShutterSpeedAsync(
                baseExposure, targetAperture, targetIso, increments);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _exposureTriangleServiceMock.Verify(
                x => x.CalculateShutterSpeed(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), 3, It.IsAny<double>()),
                Times.Once);
        }

        [Test]
        public async Task CalculateShutterSpeedAsync_WithEvCompensation_ShouldPassEvValueCorrectly()
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
            var evCompensation = 1.0;

            _exposureTriangleServiceMock
                .Setup(x => x.CalculateShutterSpeed(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), evCompensation))
                .Returns("1/30");

            // Act
            var result = await _exposureCalculatorService.CalculateShutterSpeedAsync(
                baseExposure, targetAperture, targetIso, increments, CancellationToken.None, evCompensation);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.ShutterSpeed.Should().Be("1/30");

            _exposureTriangleServiceMock.Verify(
                x => x.CalculateShutterSpeed(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), evCompensation),
                Times.Once);
        }

        [Test]
        public async Task CalculateShutterSpeedAsync_WhenExposureTriangleServiceThrows_ShouldReturnFailure()
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
                .Throws(new Exception("Test exception"));

            // Act
            var result = await _exposureCalculatorService.CalculateShutterSpeedAsync(
                baseExposure, targetAperture, targetIso, increments);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Error calculating shutter speed");
        }

        [Test]
        public async Task CalculateShutterSpeedAsync_WhenOverexposedError_ShouldReturnFailureWithErrorMessage()
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
        public async Task CalculateShutterSpeedAsync_WithCancellationToken_ShouldThrowWhenCancelled()
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

        #endregion

        #region CalculateApertureAsync Tests

        [Test]
        public async Task CalculateApertureAsync_WithValidParameters_ShouldReturnSuccess()
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
        public async Task CalculateApertureAsync_WhenExposureTriangleServiceThrows_ShouldReturnFailure()
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
                .Throws(new Exception("Test exception"));

            // Act
            var result = await _exposureCalculatorService.CalculateApertureAsync(
                baseExposure, targetShutterSpeed, targetIso, increments);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Error calculating aperture");
        }

        [Test]
        public async Task CalculateApertureAsync_WhenUnderexposedError_ShouldReturnFailureWithErrorMessage()
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
        public async Task CalculateApertureAsync_WithCancellationToken_ShouldThrowWhenCancelled()
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

        #endregion

        #region CalculateIsoAsync Tests

        [Test]
        public async Task CalculateIsoAsync_WithValidParameters_ShouldReturnSuccess()
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
        public async Task CalculateIsoAsync_WhenExposureTriangleServiceThrows_ShouldReturnFailure()
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
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<double>()))
                .Throws(new Exception("Test exception"));

            // Act
            var result = await _exposureCalculatorService.CalculateIsoAsync(
                baseExposure, targetShutterSpeed, targetAperture, increments);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Error calculating ISO");
        }

        [Test]
        public async Task CalculateIsoAsync_WhenParameterLimitError_ShouldReturnFailureWithErrorMessage()
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
        public async Task CalculateIsoAsync_WithCancellationToken_ShouldThrowWhenCancelled()
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

        #endregion

        #region GetShutterSpeedsAsync Tests

        [Test]
        public async Task GetShutterSpeedsAsync_WithFullIncrements_ShouldReturnCorrectValues()
        {
            // Arrange
            var increments = ExposureIncrements.Full;

            // Act
            var result = await _exposureCalculatorService.GetShutterSpeedsAsync(increments, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().NotBeEmpty();
            result.Data.Should().Contain("1/125");
            result.Data.Should().Contain("1/250");
            result.Data.Should().Contain("1/60");
        }

        [Test]
        public async Task GetShutterSpeedsAsync_WithHalfIncrements_ShouldReturnCorrectValues()
        {
            // Arrange
            var increments = ExposureIncrements.Half;

            // Act
            var result = await _exposureCalculatorService.GetShutterSpeedsAsync(increments, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().NotBeEmpty();
            result.Data.Length.Should().BeGreaterThan(19); // More values than full stops
        }

        [Test]
        public async Task GetShutterSpeedsAsync_WithThirdIncrements_ShouldReturnCorrectValues()
        {
            // Arrange
            var increments = ExposureIncrements.Third;

            // Act
            var result = await _exposureCalculatorService.GetShutterSpeedsAsync(increments, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().NotBeEmpty();
            result.Data.Length.Should().BeGreaterThan(30); // More values than half stops
        }

        [Test]
        public async Task GetShutterSpeedsAsync_WithCancellationToken_ShouldThrowWhenCancelled()
        {
            // Arrange
            var increments = ExposureIncrements.Full;
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            // Act & Assert
            await FluentActions.Invoking(() => _exposureCalculatorService.GetShutterSpeedsAsync(
                increments, cancellationTokenSource.Token))
                .Should().ThrowAsync<OperationCanceledException>();
        }

        #endregion

        #region GetAperturesAsync Tests

        [Test]
        public async Task GetAperturesAsync_WithFullIncrements_ShouldReturnCorrectValues()
        {
            // Arrange
            var increments = ExposureIncrements.Full;

            // Act
            var result = await _exposureCalculatorService.GetAperturesAsync(increments, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().NotBeEmpty();
            result.Data.Should().Contain("f/8");
            result.Data.Should().Contain("f/11");
            result.Data.Should().Contain("f/5.6");
        }

        [Test]
        public async Task GetAperturesAsync_WithHalfIncrements_ShouldReturnCorrectValues()
        {
            // Arrange
            var increments = ExposureIncrements.Half;

            // Act
            var result = await _exposureCalculatorService.GetAperturesAsync(increments, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().NotBeEmpty();
            result.Data.Length.Should().BeGreaterThan(13); // More values than full stops
        }

        [Test]
        public async Task GetAperturesAsync_WithCancellationToken_ShouldThrowWhenCancelled()
        {
            // Arrange
            var increments = ExposureIncrements.Full;
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            // Act & Assert
            await FluentActions.Invoking(() => _exposureCalculatorService.GetAperturesAsync(
                increments, cancellationTokenSource.Token))
                .Should().ThrowAsync<OperationCanceledException>();
        }

        #endregion

        #region GetIsosAsync Tests

        [Test]
        public async Task GetIsosAsync_WithFullIncrements_ShouldReturnCorrectValues()
        {
            // Arrange
            var increments = ExposureIncrements.Full;

            // Act
            var result = await _exposureCalculatorService.GetIsosAsync(increments, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().NotBeEmpty();
            result.Data.Should().Contain("100");
            result.Data.Should().Contain("200");
            result.Data.Should().Contain("400");
        }

        [Test]
        public async Task GetIsosAsync_WithHalfIncrements_ShouldReturnCorrectValues()
        {
            // Arrange
            var increments = ExposureIncrements.Half;

            // Act
            var result = await _exposureCalculatorService.GetIsosAsync(increments, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().NotBeEmpty();
            result.Data.Length.Should().BeGreaterThan(10); // More values than full stops
        }

        [Test]
        public async Task GetIsosAsync_WithCancellationToken_ShouldThrowWhenCancelled()
        {
            // Arrange
            var increments = ExposureIncrements.Full;
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            // Act & Assert
            await FluentActions.Invoking(() => _exposureCalculatorService.GetIsosAsync(
                increments, cancellationTokenSource.Token))
                .Should().ThrowAsync<OperationCanceledException>();
        }

        #endregion

        #region Integration Tests

        [Test]
        public async Task AllCalculationMethods_WithConsistentParameters_ShouldProduceConsistentResults()
        {
            // Arrange
            var baseExposure = new ExposureTriangleDto
            {
                ShutterSpeed = "1/125",
                Aperture = "f/8",
                Iso = "100"
            };
            var increments = ExposureIncrements.Full;

            _exposureTriangleServiceMock
                .Setup(x => x.CalculateShutterSpeed(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<double>()))
                .Returns("1/60");

            _exposureTriangleServiceMock
                .Setup(x => x.CalculateAperture(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<double>()))
                .Returns("f/11");

            _exposureTriangleServiceMock
                .Setup(x => x.CalculateIso(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<double>()))
                .Returns("200");

            // Act - Call all calculation methods
            var shutterResult = await _exposureCalculatorService.CalculateShutterSpeedAsync(
                baseExposure, "f/11", "200", increments);
            var apertureResult = await _exposureCalculatorService.CalculateApertureAsync(
                baseExposure, "1/60", "200", increments);
            var isoResult = await _exposureCalculatorService.CalculateIsoAsync(
                baseExposure, "1/60", "f/11", increments);

            // Assert - All methods should succeed
            shutterResult.IsSuccess.Should().BeTrue();
            apertureResult.IsSuccess.Should().BeTrue();
            isoResult.IsSuccess.Should().BeTrue();

            // Verify all service methods were called correctly
            _exposureTriangleServiceMock.Verify(
                x => x.CalculateShutterSpeed(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<double>()),
                Times.Once);

            _exposureTriangleServiceMock.Verify(
                x => x.CalculateAperture(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<double>()),
                Times.Once);

            _exposureTriangleServiceMock.Verify(
                x => x.CalculateIso(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<double>()),
                Times.Once);
        }

        [Test]
        public async Task AllGetMethods_WithSameIncrements_ShouldReturnConsistentArraySizes()
        {
            // Arrange
            var increments = ExposureIncrements.Full;

            // Act
            var shutterSpeedsResult = await _exposureCalculatorService.GetShutterSpeedsAsync(increments);
            var aperturesResult = await _exposureCalculatorService.GetAperturesAsync(increments);
            var isosResult = await _exposureCalculatorService.GetIsosAsync(increments);

            // Assert
            shutterSpeedsResult.IsSuccess.Should().BeTrue();
            aperturesResult.IsSuccess.Should().BeTrue();
            isosResult.IsSuccess.Should().BeTrue();

            // All arrays should contain data
            shutterSpeedsResult.Data.Should().NotBeEmpty();
            aperturesResult.Data.Should().NotBeEmpty();
            isosResult.Data.Should().NotBeEmpty();

            // Verify arrays contain expected common values
            shutterSpeedsResult.Data.Should().Contain("1/125");
            aperturesResult.Data.Should().Contain("f/8");
            isosResult.Data.Should().Contain("100");
        }

        #endregion

        #region Edge Cases and Error Handling

        [Test]
        public async Task CalculateShutterSpeedAsync_WithInvalidIncrement_ShouldDefaultToFullStops()
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
            var invalidIncrement = (ExposureIncrements)999; // Invalid enum value

            _exposureTriangleServiceMock
                .Setup(x => x.CalculateShutterSpeed(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), 1, It.IsAny<double>())) // Should default to scale 1
                .Returns("1/60");

            // Act
            var result = await _exposureCalculatorService.CalculateShutterSpeedAsync(
                baseExposure, targetAperture, targetIso, invalidIncrement);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _exposureTriangleServiceMock.Verify(
                x => x.CalculateShutterSpeed(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), 1, It.IsAny<double>()),
                Times.Once);
        }

        [Test]
        public async Task GetShutterSpeedsAsync_WithInvalidIncrement_ShouldDefaultToFullStops()
        {
            // Arrange
            var invalidIncrement = (ExposureIncrements)999;

            // Act
            var result = await _exposureCalculatorService.GetShutterSpeedsAsync(invalidIncrement);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeEmpty();
            // Should return full stop values as default
        }

        #endregion
    }
}