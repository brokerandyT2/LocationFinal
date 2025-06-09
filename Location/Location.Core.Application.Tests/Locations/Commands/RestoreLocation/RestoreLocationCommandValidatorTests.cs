using FluentValidation.TestHelper;
using Location.Core.Application.Commands.Locations;
using NUnit.Framework;

namespace Location.Core.Application.Tests.Locations.Commands.RestoreLocation
{
    [Category("Locations")]
    [Category("Restore Location")]
    [TestFixture]
    public class RestoreLocationCommandValidatorTests
    {
        private RestoreLocationCommandValidator _validator;

        [SetUp]
        public void SetUp()
        {
            _validator = new RestoreLocationCommandValidator();
        }

        [Test]
        public void Validate_WithValidLocationId_ShouldNotHaveErrors()
        {
            // Arrange
            var command = new RestoreLocationCommand { LocationId = 1 };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Test]
        public void Validate_WithZeroLocationId_ShouldHaveError()
        {
            // Arrange
            var command = new RestoreLocationCommand { LocationId = 0 };

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
            var command = new RestoreLocationCommand { LocationId = -1 };

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
            var command = new RestoreLocationCommand { LocationId = int.MaxValue };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }
    }
}