// Fixed ExposureTriangleServiceTests.cs
using FluentAssertions;
using Location.Photography.Application.Errors;
using Location.Photography.Infrastructure.Services;
using NUnit.Framework;
namespace Location.Photography.Infrastructure.Tests.Services
{
    [TestFixture]
    public class ExposureTriangleServiceTests
    {
        private ExposureTriangleService _exposureTriangleService;

        [SetUp]
        public void SetUp()
        {
            _exposureTriangleService = new ExposureTriangleService();
        }

        [Test]
        public void CalculateAperture_ShouldReturnCorrectValue_WhenShutterSpeedIncreases()
        {
            // Arrange
            string baseShutterSpeed = "1/125";
            string baseAperture = "f/8";
            string baseIso = "100";
            string targetShutterSpeed = "1/250"; // 1 stop darker
            string targetIso = "100";            // unchanged
            int scale = 1;                       // full stops

            // Act
            string result = _exposureTriangleService.CalculateAperture(
                baseShutterSpeed, baseAperture, baseIso, targetShutterSpeed, targetIso, scale);

            // Assert
            result.Should().Be("f/5.6"); // 1 stop wider to compensate for faster shutter
        }

        [Test]
        public void CalculateAperture_ShouldReturnCorrectValue_WhenIsoIncreases()
        {
            // Arrange
            string baseShutterSpeed = "1/125";
            string baseAperture = "f/8";
            string baseIso = "100";
            string targetShutterSpeed = "1/125"; // unchanged
            string targetIso = "200";            // 1 stop brighter
            int scale = 1;                       // full stops

            // Act
            string result = _exposureTriangleService.CalculateAperture(
                baseShutterSpeed, baseAperture, baseIso, targetShutterSpeed, targetIso, scale);

            // Assert
            result.Should().Be("f/11"); // 1 stop smaller to compensate for higher ISO
        }

        [Test]
        public void CalculateAperture_ShouldApplyEvCompensation()
        {
            // Arrange
            string baseShutterSpeed = "1/125";
            string baseAperture = "f/8";
            string baseIso = "100";
            string targetShutterSpeed = "1/125"; // unchanged
            string targetIso = "100";            // unchanged
            int scale = 1;                       // full stops
            double evCompensation = 1.0;         // +1 EV (brighter = wider aperture)

            // Act
            string result = _exposureTriangleService.CalculateAperture(
                baseShutterSpeed, baseAperture, baseIso, targetShutterSpeed, targetIso, scale, evCompensation);

            // Assert
            result.Should().Be("f/11"); // 1 stop wider to make exposure brighter
        }

        [Test]
        public void CalculateIso_ShouldReturnCorrectValue_WhenApertureIncreases()
        {
            // Arrange
            string baseShutterSpeed = "1/125";
            string baseAperture = "f/8";
            string baseIso = "100";
            string targetShutterSpeed = "1/125"; // unchanged
            string targetAperture = "f/11";      // 1 stop darker
            int scale = 1;                       // full stops

            // Act
            string result = _exposureTriangleService.CalculateIso(
                baseShutterSpeed, baseAperture, baseIso, targetShutterSpeed, targetAperture, scale);

            // Assert
            result.Should().Be("200"); // 1 stop higher to compensate for smaller aperture
        }

        [Test]
        public void CalculateIso_ShouldApplyEvCompensation()
        {
            // Arrange
            string baseShutterSpeed = "1/125";
            string baseAperture = "f/8";
            string baseIso = "100";
            string targetShutterSpeed = "1/125"; // unchanged
            string targetAperture = "f/8";       // unchanged
            int scale = 1;                       // full stops
            double evCompensation = 1.0;         // +1 EV (brighter = higher ISO)

            // Act
            string result = _exposureTriangleService.CalculateIso(
                baseShutterSpeed, baseAperture, baseIso, targetShutterSpeed, targetAperture, scale, evCompensation);

            // Assert
            result.Should().Be("200"); // 1 stop higher to make exposure brighter
        }

        [Test]
        public void CalculateIso_ShouldHandleCombinedChanges()
        {
            // Arrange
            string baseShutterSpeed = "1/125";
            string baseAperture = "f/8";
            string baseIso = "100";
            string targetShutterSpeed = "1/500"; // 2 stops darker
            string targetAperture = "f/5.6";     // 1 stop brighter
            int scale = 1;                       // full stops

            // Act
            string result = _exposureTriangleService.CalculateIso(
                baseShutterSpeed, baseAperture, baseIso, targetShutterSpeed, targetAperture, scale);

            // Assert
            result.Should().Be("200"); // Net: 1 stop darker, so ISO needs to be 1 stop higher
        }

        [Test]
        public void CalculateShutterSpeed_ShouldHandleHalfStops()
        {
            // Arrange
            string baseShutterSpeed = "1/125";
            string baseAperture = "f/8";
            string baseIso = "100";
            string targetAperture = "f/9.5";     // 0.5 stop darker (half stop)
            string targetIso = "100";            // unchanged
            int scale = 2;                       // half stops

            // Act
            string result = _exposureTriangleService.CalculateShutterSpeed(
                baseShutterSpeed, baseAperture, baseIso, targetAperture, targetIso, scale);

            // Assert
            result.Should().Be("1/90"); // 0.5 stop slower to compensate for smaller aperture
        }

        [Test]
        public void CalculateAperture_ShouldHandleThirdStops()
        {
            // Arrange
            string baseShutterSpeed = "1/125";
            string baseAperture = "f/8";
            string baseIso = "100";
            string targetShutterSpeed = "1/160"; // 1/3 stop darker
            string targetIso = "100";            // unchanged
            int scale = 3;                       // third stops

            // Act
            string result = _exposureTriangleService.CalculateAperture(
                baseShutterSpeed, baseAperture, baseIso, targetShutterSpeed, targetIso, scale);

            // Assert
            result.Should().Be("f/7.1"); // 1/3 stop wider to compensate for faster shutter
        }

        [Test]
        public void CalculateShutterSpeed_ShouldHandleSpecialFormats()
        {
            // Arrange
            string baseShutterSpeed = "2\"";     // 2 seconds
            string baseAperture = "f/16";
            string baseIso = "100";
            string targetAperture = "f/11";      // 1 stop brighter
            string targetIso = "100";            // unchanged
            int scale = 1;                       // full stops

            // Act
            string result = _exposureTriangleService.CalculateShutterSpeed(
                baseShutterSpeed, baseAperture, baseIso, targetAperture, targetIso, scale);

            // Assert
            result.Should().Be("1\""); // 1 stop faster (1 second) to compensate for wider aperture
        }

        [Test]
        public void CalculateShutterSpeed_ShouldApplyEvCompensation()
        {
            // Arrange
            string baseShutterSpeed = "1/125";
            string baseAperture = "f/8";
            string baseIso = "100";
            string targetAperture = "f/8";  // unchanged
            string targetIso = "100";       // unchanged
            int scale = 1;                  // full stops
            double evCompensation = 1.0;    // +1 EV (brighter)

            // Act
            string result = _exposureTriangleService.CalculateShutterSpeed(
                baseShutterSpeed, baseAperture, baseIso, targetAperture, targetIso, scale, evCompensation);

            // Assert
            result.Should().Be("1/60"); // 1 stop slower to make exposure brighter
        }

        [Test]
        public void CalculateShutterSpeed_ShouldThrowOverexposedError_WhenRequiredValueExceedsLimits()
        {
            // Arrange
            string baseShutterSpeed = "30\"";    // Already at the slowest standard shutter speed
            string baseAperture = "f/22";        // Already at the smallest standard aperture
            string baseIso = "50";               // Already at the lowest standard ISO
            string targetAperture = "f/32";      // 1 stop darker
            string targetIso = "25";             // 1 stop darker (not in standard list)
            int scale = 1;                       // full stops

            // Act & Assert
            FluentActions.Invoking(() => _exposureTriangleService.CalculateShutterSpeed(
                baseShutterSpeed, baseAperture, baseIso, targetAperture, targetIso, scale))
                .Should().Throw<OverexposedError>();
        }

        [Test]
        public void CalculateAperture_ShouldThrowUnderexposedError_WhenRequiredValueExceedsLimits()
        {
            // Arrange
            string baseShutterSpeed = "1/8000";  // Fastest standard shutter speed
            string baseAperture = "f/1.4";       // Already at the widest standard aperture
            string baseIso = "100";
            string targetShutterSpeed = "1/16000"; // 1 stop faster (not in standard list)
            string targetIso = "50";              // 1 stop lower sensitivity
            int scale = 1;                        // full stops

            // Act & Assert
            FluentActions.Invoking(() => _exposureTriangleService.CalculateAperture(
                baseShutterSpeed, baseAperture, baseIso, targetShutterSpeed, targetIso, scale))
                .Should().Throw<UnderexposedError>();
        }

        [Test]
        public void CalculateIso_ShouldThrowExposureParameterLimitError_WhenRequiredValueExceedsLimits()
        {
            // Arrange
            string baseShutterSpeed = "1/125";
            string baseAperture = "f/8";
            string baseIso = "25600";            // Already at the highest standard ISO
            string targetShutterSpeed = "1/500"; // 2 stops faster
            string targetAperture = "f/16";      // 2 stops smaller
            int scale = 1;                       // full stops

            // Act & Assert
            FluentActions.Invoking(() => _exposureTriangleService.CalculateIso(
                baseShutterSpeed, baseAperture, baseIso, targetShutterSpeed, targetAperture, scale))
                .Should().Throw<ExposureParameterLimitError>();
        }
    }
}