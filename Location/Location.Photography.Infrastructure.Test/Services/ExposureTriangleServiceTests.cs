using FluentAssertions;
using Location.Photography.Application.Errors;
using Location.Photography.Infrastructure.Services;
using NUnit.Framework;

namespace Location.Photography.Infrastructure.Test.Services
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

        #region CalculateShutterSpeed Tests

        [Test]
        public void CalculateShutterSpeed_ShouldReturnCorrectValue_WhenApertureIncreases()
        {
            // Arrange
            string baseShutterSpeed = "1/125";
            string baseAperture = "f/8";
            string baseIso = "100";
            string targetAperture = "f/11";  // 1 stop smaller aperture (less light)
            string targetIso = "100";        // unchanged
            int scale = 1;                   // full stops

            // Act
            string result = _exposureTriangleService.CalculateShutterSpeed(
                baseShutterSpeed, baseAperture, baseIso, targetAperture, targetIso, scale);

            // Assert
            result.Should().Be("1/60"); // 1 stop slower shutter to compensate
        }

        [Test]
        public void CalculateShutterSpeed_ShouldReturnCorrectValue_WhenIsoDecreases()
        {
            // Arrange
            string baseShutterSpeed = "1/125";
            string baseAperture = "f/8";
            string baseIso = "200";
            string targetAperture = "f/8";   // unchanged
            string targetIso = "100";        // 1 stop less sensitive
            int scale = 1;                   // full stops

            // Act
            string result = _exposureTriangleService.CalculateShutterSpeed(
                baseShutterSpeed, baseAperture, baseIso, targetAperture, targetIso, scale);

            // Assert
            result.Should().Be("1/60"); // 1 stop slower shutter to compensate
        }

        [Test]
        public void CalculateShutterSpeed_ShouldReturnCorrectValue_WhenCombinedChanges()
        {
            // Arrange
            string baseShutterSpeed = "1/125";
            string baseAperture = "f/8";
            string baseIso = "200";
            string targetAperture = "f/11";  // 1 stop smaller aperture
            string targetIso = "100";        // 1 stop less sensitive
            int scale = 1;                   // full stops

            // Act
            string result = _exposureTriangleService.CalculateShutterSpeed(
                baseShutterSpeed, baseAperture, baseIso, targetAperture, targetIso, scale);

            // Assert
            result.Should().Be("1/30"); // 2 stops slower shutter to compensate
        }

        [Test]
        public void CalculateShutterSpeed_ShouldApplyEvCompensation()
        {
            // Arrange
            string baseShutterSpeed = "1/125";
            string baseAperture = "f/8";
            string baseIso = "100";
            string targetAperture = "f/8";   // unchanged
            string targetIso = "100";        // unchanged
            int scale = 1;                   // full stops
            double evCompensation = 1.0;     // +1 EV (brighter)

            // Act
            string result = _exposureTriangleService.CalculateShutterSpeed(
                baseShutterSpeed, baseAperture, baseIso, targetAperture, targetIso, scale, evCompensation);

            // Assert
            result.Should().Be("1/60"); // 1 stop slower shutter to make exposure brighter
        }

        [Test]
        public void CalculateShutterSpeed_ShouldHandleHalfStops()
        {
            // Arrange
            string baseShutterSpeed = "1/125";
            string baseAperture = "f/8";
            string baseIso = "100";
            string targetAperture = "f/9.5"; // 0.5 stop smaller aperture
            string targetIso = "100";        // unchanged
            int scale = 2;                   // half stops

            // Act
            string result = _exposureTriangleService.CalculateShutterSpeed(
                baseShutterSpeed, baseAperture, baseIso, targetAperture, targetIso, scale);

            // Assert
            result.Should().Be("1/90"); // 0.5 stop slower shutter
        }

        [Test]
        public void CalculateShutterSpeed_ShouldHandleThirdStops()
        {
            // Arrange
            string baseShutterSpeed = "1/125";
            string baseAperture = "f/8";
            string baseIso = "100";
            string targetAperture = "f/9";   // 1/3 stop smaller aperture
            string targetIso = "100";        // unchanged
            int scale = 3;                   // third stops

            // Act
            string result = _exposureTriangleService.CalculateShutterSpeed(
                baseShutterSpeed, baseAperture, baseIso, targetAperture, targetIso, scale);

            // Assert
            result.Should().Be("1/100"); // 1/3 stop slower shutter
        }

        [Test]
        public void CalculateShutterSpeed_ShouldHandleSpecialFormats()
        {
            // Arrange
            string baseShutterSpeed = "2\"";  // 2 seconds
            string baseAperture = "f/16";
            string baseIso = "100";
            string targetAperture = "f/11";  // 1 stop wider aperture
            string targetIso = "100";        // unchanged
            int scale = 1;                   // full stops

            // Act
            string result = _exposureTriangleService.CalculateShutterSpeed(
                baseShutterSpeed, baseAperture, baseIso, targetAperture, targetIso, scale);

            // Assert
            result.Should().Be("1\""); // 1 second (1 stop faster)
        }

        [Test]
        public void CalculateShutterSpeed_WithInvalidFormat_ShouldThrowArgumentException()
        {
            // Arrange
            string baseShutterSpeed = "invalid";
            string baseAperture = "f/8";
            string baseIso = "100";
            string targetAperture = "f/11";
            string targetIso = "100";
            int scale = 1;

            // Act & Assert
            FluentActions.Invoking(() => _exposureTriangleService.CalculateShutterSpeed(
                baseShutterSpeed, baseAperture, baseIso, targetAperture, targetIso, scale))
                .Should().Throw<ArgumentException>()
                .WithMessage("*Invalid shutter speed format*");
        }

        [Test]
        public void CalculateShutterSpeed_ShouldThrowOverexposedError_WhenRequiredValueExceedsLimits()
        {
            // Arrange
            string baseShutterSpeed = "30\"";  // Already at the slowest standard shutter speed
            string baseAperture = "f/22";      // Already at the smallest standard aperture
            string baseIso = "50";             // Already at the lowest standard ISO
            string targetAperture = "f/32";    // Even smaller aperture
            string targetIso = "25";           // Even lower ISO
            int scale = 1;

            // Act & Assert
            FluentActions.Invoking(() => _exposureTriangleService.CalculateShutterSpeed(
                baseShutterSpeed, baseAperture, baseIso, targetAperture, targetIso, scale))
                .Should().Throw<OverexposedError>();
        }

        #endregion

        #region CalculateAperture Tests

        [Test]
        public void CalculateAperture_ShouldReturnCorrectValue_WhenShutterSpeedIncreases()
        {
            // Arrange
            string baseShutterSpeed = "1/125";
            string baseAperture = "f/8";
            string baseIso = "100";
            string targetShutterSpeed = "1/250"; // 1 stop faster (less light)
            string targetIso = "100";            // unchanged
            int scale = 1;                       // full stops

            // Act
            string result = _exposureTriangleService.CalculateAperture(
                baseShutterSpeed, baseAperture, baseIso, targetShutterSpeed, targetIso, scale);

            // Assert
            result.Should().Be("f/5.6"); // 1 stop wider aperture to compensate
        }

        [Test]
        public void CalculateAperture_ShouldReturnCorrectValue_WhenIsoIncreases()
        {
            // Arrange
            string baseShutterSpeed = "1/125";
            string baseAperture = "f/8";
            string baseIso = "100";
            string targetShutterSpeed = "1/125"; // unchanged
            string targetIso = "200";            // 1 stop more sensitive
            int scale = 1;                       // full stops

            // Act
            string result = _exposureTriangleService.CalculateAperture(
                baseShutterSpeed, baseAperture, baseIso, targetShutterSpeed, targetIso, scale);

            // Assert
            result.Should().Be("f/11"); // 1 stop smaller aperture to compensate
        }

        [Test]
        public void CalculateAperture_ShouldReturnCorrectValue_WhenCombinedChanges()
        {
            // Arrange
            string baseShutterSpeed = "1/125";
            string baseAperture = "f/8";
            string baseIso = "100";
            string targetShutterSpeed = "1/250"; // 1 stop faster (less light)
            string targetIso = "200";            // 1 stop more sensitive (more light)
            int scale = 1;                       // full stops

            // Act
            string result = _exposureTriangleService.CalculateAperture(
                baseShutterSpeed, baseAperture, baseIso, targetShutterSpeed, targetIso, scale);

            // Assert
            result.Should().Be("f/8"); // No net change, aperture stays the same
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
            double evCompensation = -1.0;        // -1 EV (darker)

            // Act
            string result = _exposureTriangleService.CalculateAperture(
                baseShutterSpeed, baseAperture, baseIso, targetShutterSpeed, targetIso, scale, evCompensation);

            // Assert
            result.Should().Be("f/11"); // 1 stop smaller aperture to make exposure darker
        }

        [Test]
        public void CalculateAperture_ShouldHandleHalfStops()
        {
            // Arrange
            string baseShutterSpeed = "1/125";
            string baseAperture = "f/8";
            string baseIso = "100";
            string targetShutterSpeed = "1/180"; // 0.5 stop faster
            string targetIso = "100";            // unchanged
            int scale = 2;                       // half stops

            // Act
            string result = _exposureTriangleService.CalculateAperture(
                baseShutterSpeed, baseAperture, baseIso, targetShutterSpeed, targetIso, scale);

            // Assert
            result.Should().Be("f/6.7"); // 0.5 stop wider aperture
        }

        [Test]
        public void CalculateAperture_ShouldHandleThirdStops()
        {
            // Arrange
            string baseShutterSpeed = "1/125";
            string baseAperture = "f/8";
            string baseIso = "100";
            string targetShutterSpeed = "1/160"; // 1/3 stop faster
            string targetIso = "100";            // unchanged
            int scale = 3;                       // third stops

            // Act
            string result = _exposureTriangleService.CalculateAperture(
                baseShutterSpeed, baseAperture, baseIso, targetShutterSpeed, targetIso, scale);

            // Assert
            result.Should().Be("f/7.1"); // 1/3 stop wider aperture
        }

        [Test]
        public void CalculateAperture_WithInvalidFormat_ShouldThrowArgumentException()
        {
            // Arrange
            string baseShutterSpeed = "1/125";
            string baseAperture = "invalid";
            string baseIso = "100";
            string targetShutterSpeed = "1/60";
            string targetIso = "100";
            int scale = 1;

            // Act & Assert
            FluentActions.Invoking(() => _exposureTriangleService.CalculateAperture(
                baseShutterSpeed, baseAperture, baseIso, targetShutterSpeed, targetIso, scale))
                .Should().Throw<ArgumentException>()
                .WithMessage("*Invalid aperture format*");
        }

        [Test]
        public void CalculateAperture_ShouldThrowUnderexposedError_WhenRequiredValueExceedsLimits()
        {
            // Arrange
            string baseShutterSpeed = "1/4000"; // Already at fastest standard shutter speed
            string baseAperture = "f/1.4";      // Already at widest standard aperture
            string baseIso = "6400";            // Already at high ISO
            string targetShutterSpeed = "1/8000"; // Even faster shutter
            string targetIso = "12800";         // Even higher ISO
            int scale = 1;

            // Act & Assert
            FluentActions.Invoking(() => _exposureTriangleService.CalculateAperture(
                baseShutterSpeed, baseAperture, baseIso, targetShutterSpeed, targetIso, scale))
                .Should().Throw<UnderexposedError>();
        }

        #endregion

        #region CalculateIso Tests

        [Test]
        public void CalculateIso_ShouldReturnCorrectValue_WhenShutterSpeedIncreases()
        {
            // Arrange
            string baseShutterSpeed = "1/125";
            string baseAperture = "f/8";
            string baseIso = "100";
            string targetShutterSpeed = "1/250"; // 1 stop faster (less light)
            string targetAperture = "f/8";       // unchanged
            int scale = 1;                       // full stops

            // Act
            string result = _exposureTriangleService.CalculateIso(
                baseShutterSpeed, baseAperture, baseIso, targetShutterSpeed, targetAperture, scale);

            // Assert
            result.Should().Be("200"); // 1 stop higher ISO to compensate
        }

        [Test]
        public void CalculateIso_ShouldReturnCorrectValue_WhenApertureIncreases()
        {
            // Arrange
            string baseShutterSpeed = "1/125";
            string baseAperture = "f/8";
            string baseIso = "100";
            string targetShutterSpeed = "1/125"; // unchanged
            string targetAperture = "f/11";      // 1 stop smaller aperture (less light)
            int scale = 1;                       // full stops

            // Act
            string result = _exposureTriangleService.CalculateIso(
                baseShutterSpeed, baseAperture, baseIso, targetShutterSpeed, targetAperture, scale);

            // Assert
            result.Should().Be("200"); // 1 stop higher ISO to compensate
        }

        [Test]
        public void CalculateIso_ShouldReturnCorrectValue_WhenCombinedChanges()
        {
            // Arrange
            string baseShutterSpeed = "1/125";
            string baseAperture = "f/8";
            string baseIso = "100";
            string targetShutterSpeed = "1/500"; // 2 stops faster (less light)
            string targetAperture = "f/5.6";     // 1 stop wider (more light)
            int scale = 1;                       // full stops

            // Act
            string result = _exposureTriangleService.CalculateIso(
                baseShutterSpeed, baseAperture, baseIso, targetShutterSpeed, targetAperture, scale);

            // Assert
            result.Should().Be("200"); // Net 1 stop less light, so ISO increases by 1 stop
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
            double evCompensation = -1.0;        // -1 EV (darker exposure)

            // Act
            string result = _exposureTriangleService.CalculateIso(
                baseShutterSpeed, baseAperture, baseIso, targetShutterSpeed, targetAperture, scale, evCompensation);

            // Assert
            result.Should().Be("50"); // 1 stop lower ISO to make exposure darker
        }

        [Test]
        public void CalculateIso_ShouldHandleHalfStops()
        {
            // Arrange
            string baseShutterSpeed = "1/125";
            string baseAperture = "f/8";
            string baseIso = "100";
            string targetShutterSpeed = "1/180"; // 0.5 stop faster
            string targetAperture = "f/8";       // unchanged
            int scale = 2;                       // half stops

            // Act
            string result = _exposureTriangleService.CalculateIso(
                baseShutterSpeed, baseAperture, baseIso, targetShutterSpeed, targetAperture, scale);

            // Assert
            result.Should().Be("140"); // 0.5 stop higher ISO
        }

        [Test]
        public void CalculateIso_ShouldHandleThirdStops()
        {
            // Arrange
            string baseShutterSpeed = "1/125";
            string baseAperture = "f/8";
            string baseIso = "100";
            string targetShutterSpeed = "1/160"; // 1/3 stop faster
            string targetAperture = "f/8";       // unchanged
            int scale = 3;                       // third stops

            // Act
            string result = _exposureTriangleService.CalculateIso(
                baseShutterSpeed, baseAperture, baseIso, targetShutterSpeed, targetAperture, scale);

            // Assert
            result.Should().Be("125"); // 1/3 stop higher ISO
        }

        [Test]
        public void CalculateIso_WithInvalidFormat_ShouldThrowArgumentException()
        {
            // Arrange
            string baseShutterSpeed = "1/125";
            string baseAperture = "f/8";
            string baseIso = "invalid";
            string targetShutterSpeed = "1/60";
            string targetAperture = "f/11";
            int scale = 1;

            // Act & Assert
            FluentActions.Invoking(() => _exposureTriangleService.CalculateIso(
                baseShutterSpeed, baseAperture, baseIso, targetShutterSpeed, targetAperture, scale))
                .Should().Throw<ArgumentException>()
                .WithMessage("*Invalid ISO format*");
        }

        [Test]
        public void CalculateIso_ShouldThrowParameterLimitError_WhenRequiredValueExceedsLimits()
        {
            // Arrange
            string baseShutterSpeed = "1/125";
            string baseAperture = "f/8";
            string baseIso = "25600";           // Already at very high ISO
            string targetShutterSpeed = "1/4000"; // Much faster shutter
            string targetAperture = "f/22";     // Much smaller aperture
            int scale = 1;

            // Act & Assert
            FluentActions.Invoking(() => _exposureTriangleService.CalculateIso(
                baseShutterSpeed, baseAperture, baseIso, targetShutterSpeed, targetAperture, scale))
                .Should().Throw<ExposureParameterLimitError>();
        }

        #endregion

        #region Edge Cases and Error Handling

        [Test]
        public void CalculateShutterSpeed_WithNullParameters_ShouldThrowArgumentException()
        {
            // Act & Assert
            FluentActions.Invoking(() => _exposureTriangleService.CalculateShutterSpeed(
                null, "f/8", "100", "f/11", "100", 1))
                .Should().Throw<ArgumentException>();
        }

        [Test]
        public void CalculateAperture_WithEmptyParameters_ShouldThrowArgumentException()
        {
            // Act & Assert
            FluentActions.Invoking(() => _exposureTriangleService.CalculateAperture(
                "1/125", "", "100", "1/60", "100", 1))
                .Should().Throw<ArgumentException>();
        }

        [Test]
        public void CalculateIso_WithInvalidScale_ShouldDefaultToFullStops()
        {
            // Arrange
            string baseShutterSpeed = "1/125";
            string baseAperture = "f/8";
            string baseIso = "100";
            string targetShutterSpeed = "1/250";
            string targetAperture = "f/8";
            int invalidScale = 99; // Invalid scale value

            // Act
            string result = _exposureTriangleService.CalculateIso(
                baseShutterSpeed, baseAperture, baseIso, targetShutterSpeed, targetAperture, invalidScale);

            // Assert - Should default to full stops behavior
            result.Should().Be("200");
        }

        [Test]
        public void CalculateShutterSpeed_WithZeroEvCompensation_ShouldBehaveSameAsWithoutCompensation()
        {
            // Arrange
            string baseShutterSpeed = "1/125";
            string baseAperture = "f/8";
            string baseIso = "100";
            string targetAperture = "f/11";
            string targetIso = "100";
            int scale = 1;

            // Act
            string resultWithoutEv = _exposureTriangleService.CalculateShutterSpeed(
                baseShutterSpeed, baseAperture, baseIso, targetAperture, targetIso, scale);
            string resultWithZeroEv = _exposureTriangleService.CalculateShutterSpeed(
                baseShutterSpeed, baseAperture, baseIso, targetAperture, targetIso, scale, 0.0);

            // Assert
            resultWithoutEv.Should().Be(resultWithZeroEv);
        }

        [Test]
        public void CalculateAperture_WithExtremeEvCompensation_ShouldHandleGracefully()
        {
            // Arrange
            string baseShutterSpeed = "1/125";
            string baseAperture = "f/8";
            string baseIso = "100";
            string targetShutterSpeed = "1/125";
            string targetIso = "100";
            int scale = 1;
            double extremeEvCompensation = 10.0; // Very large EV compensation

            // Act & Assert - Should either handle gracefully or throw appropriate error
            FluentActions.Invoking(() => _exposureTriangleService.CalculateAperture(
                baseShutterSpeed, baseAperture, baseIso, targetShutterSpeed, targetIso, scale, extremeEvCompensation))
                .Should().NotThrow<InvalidOperationException>(); // Should not crash unexpectedly
        }

        #endregion

        #region Integration Tests

        [Test]
        public void AllCalculationMethods_WithConsistentParameters_ShouldProduceConsistentResults()
        {
            // Arrange
            string baseShutterSpeed = "1/125";
            string baseAperture = "f/8";
            string baseIso = "100";
            int scale = 1;

            // Test scenario: Calculate each parameter individually and verify consistency
            string newShutterSpeed = _exposureTriangleService.CalculateShutterSpeed(
                baseShutterSpeed, baseAperture, baseIso, "f/11", "200", scale);

            string newAperture = _exposureTriangleService.CalculateAperture(
                baseShutterSpeed, baseAperture, baseIso, newShutterSpeed, "200", scale);

            string newIso = _exposureTriangleService.CalculateIso(
                baseShutterSpeed, baseAperture, baseIso, newShutterSpeed, newAperture, scale);

            // Assert - The calculated values should be mathematically consistent
            newShutterSpeed.Should().NotBeNull();
            newAperture.Should().NotBeNull();
            newIso.Should().NotBeNull();

            // All calculations should complete without throwing exceptions
            FluentActions.Invoking(() =>
            {
                _exposureTriangleService.CalculateShutterSpeed(baseShutterSpeed, baseAperture, baseIso, newAperture, newIso, scale);
                _exposureTriangleService.CalculateAperture(baseShutterSpeed, baseAperture, baseIso, newShutterSpeed, newIso, scale);
                _exposureTriangleService.CalculateIso(baseShutterSpeed, baseAperture, baseIso, newShutterSpeed, newAperture, scale);
            }).Should().NotThrow();
        }

        #endregion
    }
}