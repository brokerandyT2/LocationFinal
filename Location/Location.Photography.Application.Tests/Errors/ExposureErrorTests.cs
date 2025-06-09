using FluentAssertions;
using Location.Photography.Application.Errors;
using NUnit.Framework;

namespace Location.Photography.Application.Tests.Errors
{
    [TestFixture]
    public class ExposureErrorTests
    {
        [Test]
        public void OverexposedError_Constructor_ShouldSetStopsOverexposedProperty()
        {
            // Arrange
            double stopsOverexposed = 2.5;

            // Act
            var error = new OverexposedError(stopsOverexposed);

            // Assert
            error.StopsOverexposed.Should().Be(stopsOverexposed);
            error.Message.Should().Be("Image will be overexposed by approximately 2.5 stops");
        }

        [Test]
        public void OverexposedError_Constructor_ShouldFormatMessageCorrectly()
        {
            // Arrange
            double stopsOverexposed = 1.3;

            // Act
            var error = new OverexposedError(stopsOverexposed);

            // Assert
            error.Message.Should().Be("Image will be overexposed by approximately 1.3 stops");
        }

        [Test]
        public void OverexposedError_ShouldInheritFromExposureError()
        {
            // Arrange
            var error = new OverexposedError(1.0);

            // Act & Assert
            error.Should().BeAssignableTo<ExposureError>();
            error.Should().BeAssignableTo<Exception>();
        }

        [Test]
        public void UnderexposedError_Constructor_ShouldSetStopsUnderexposedProperty()
        {
            // Arrange
            double stopsUnderexposed = 3.2;

            // Act
            var error = new UnderexposedError(stopsUnderexposed);

            // Assert
            error.StopsUnderexposed.Should().Be(stopsUnderexposed);
            error.Message.Should().Be("Image will be underexposed by approximately 3.2 stops");
        }

        [Test]
        public void UnderexposedError_Constructor_ShouldFormatMessageCorrectly()
        {
            // Arrange
            double stopsUnderexposed = 0.7;

            // Act
            var error = new UnderexposedError(stopsUnderexposed);

            // Assert
            error.Message.Should().Be("Image will be underexposed by approximately 0.7 stops");
        }

        [Test]
        public void UnderexposedError_ShouldInheritFromExposureError()
        {
            // Arrange
            var error = new UnderexposedError(1.0);

            // Act & Assert
            error.Should().BeAssignableTo<ExposureError>();
            error.Should().BeAssignableTo<Exception>();
        }

        [Test]
        public void ExposureParameterLimitError_Constructor_ShouldSetAllProperties()
        {
            // Arrange
            string parameterName = "ShutterSpeed";
            string requestedValue = "1/16000";
            string availableLimit = "1/8000";

            // Act
            var error = new ExposureParameterLimitError(parameterName, requestedValue, availableLimit);

            // Assert
            error.ParameterName.Should().Be(parameterName);
            error.RequestedValue.Should().Be(requestedValue);
            error.AvailableLimit.Should().Be(availableLimit);
        }

        [Test]
        public void ExposureParameterLimitError_Constructor_ShouldFormatMessageCorrectly()
        {
            // Arrange
            string parameterName = "Aperture";
            string requestedValue = "f/0.5";
            string availableLimit = "f/1.0";

            // Act
            var error = new ExposureParameterLimitError(parameterName, requestedValue, availableLimit);

            // Assert
            error.Message.Should().Be("The requested Aperture (f/0.5) exceeds available limits. The closest available value is f/1.0.");
        }

        [Test]
        public void ExposureParameterLimitError_ShouldInheritFromExposureError()
        {
            // Arrange
            var error = new ExposureParameterLimitError("ISO", "102400", "25600");

            // Act & Assert
            error.Should().BeAssignableTo<ExposureError>();
            error.Should().BeAssignableTo<Exception>();
        }
    }
}