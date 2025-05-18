using FluentValidation.TestHelper;
using Location.Core.Application.Commands.Weather;
using Location.Core.Application.Weather.Commands.UpdateWeather;
using NUnit.Framework;

namespace Location.Core.Application.Tests.Weather.Commands.UpdateWeather
{
    [Category("Weather")]
    [Category("Update")]

    [TestFixture]
    public class UpdateWeatherCommandValidatorTests
    {
        private UpdateWeatherCommandValidator _validator;

        [SetUp]
        public void SetUp()
        {
            _validator = new UpdateWeatherCommandValidator();
        }

        [Test]
        public void Validate_WithValidLocationId_ShouldNotHaveErrors()
        {
            // Arrange
            var command = new UpdateWeatherCommand { LocationId = 1 };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Test]
        public void Validate_WithZeroLocationId_ShouldHaveError()
        {
            // Arrange
            var command = new UpdateWeatherCommand { LocationId = 0 };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.LocationId)
                .WithErrorMessage("LocationId must be greater than 0");
        }

        [Test]
        public void Validate_WithNegativeLocationId_ShouldHaveError()
        {
            // Arrange
            var command = new UpdateWeatherCommand { LocationId = -1 };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.LocationId)
                .WithErrorMessage("LocationId must be greater than 0");
        }

        [Test]
        public void Validate_WithLargePositiveLocationId_ShouldNotHaveErrors()
        {
            // Arrange
            var command = new UpdateWeatherCommand { LocationId = int.MaxValue };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }
    }
}