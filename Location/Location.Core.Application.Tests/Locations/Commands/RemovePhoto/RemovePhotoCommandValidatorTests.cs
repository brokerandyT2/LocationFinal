using FluentValidation.TestHelper;
using Location.Core.Application.Commands.Locations;
using NUnit.Framework;

namespace Location.Core.Application.Tests.Locations.Commands.RemovePhoto
{
    [Category("Locations")]
    [Category("PHOTO Management")]
    [TestFixture]
    public class RemovePhotoCommandValidatorTests
    {
        private RemovePhotoCommandValidator _validator;

        [SetUp]
        public void SetUp()
        {
            _validator = new RemovePhotoCommandValidator();
        }

        [Test]
        public void Validate_WithValidLocationId_ShouldNotHaveErrors()
        {
            // Arrange
            var command = new RemovePhotoCommand { LocationId = 1 };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Test]
        public void Validate_WithZeroLocationId_ShouldHaveError()
        {
            // Arrange
            var command = new RemovePhotoCommand { LocationId = 0 };

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
            var command = new RemovePhotoCommand { LocationId = -1 };

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
            var command = new RemovePhotoCommand { LocationId = int.MaxValue };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }
    }
}