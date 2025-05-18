using FluentValidation.TestHelper;
using Location.Core.Application.Commands.Locations;
using NUnit.Framework;

namespace Location.Core.Application.Tests.Locations.Commands.AttachPhoto
{
    [Category("Locations")]
    [Category("PHOTO Management")]
    [TestFixture]
    public class AttachPhotoCommandValidatorTests
    {
        private AttachPhotoCommandValidator _validator;

        [SetUp]
        public void SetUp()
        {
            _validator = new AttachPhotoCommandValidator();
        }

        [Test]
        public void Validate_WithValidData_ShouldNotHaveErrors()
        {
            // Arrange
            var command = new AttachPhotoCommand
            {
                LocationId = 1,
                PhotoPath = "/path/to/photo.jpg"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Test]
        public void Validate_WithZeroLocationId_ShouldHaveError()
        {
            // Arrange
            var command = new AttachPhotoCommand
            {
                LocationId = 0,
                PhotoPath = "/path/to/photo.jpg"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.LocationId)
                .WithErrorMessage("LocationId must be greater than 0");
        }

        [Test]
        public void Validate_WithEmptyPhotoPath_ShouldHaveError()
        {
            // Arrange
            var command = new AttachPhotoCommand
            {
                LocationId = 1,
                PhotoPath = string.Empty
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.PhotoPath)
                .WithErrorMessage("Photo path is required");
        }

        [Test]
        public void Validate_WithInvalidPhotoPath_ShouldHaveError()
        {
            // Arrange
            var command = new AttachPhotoCommand
            {
                LocationId = 1,
                PhotoPath = "invalid|path<>with:chars"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.PhotoPath)
                .WithErrorMessage("Photo path is not valid");
        }

        [Test]
        public void Validate_WithNullPhotoPath_ShouldHaveError()
        {
            // Arrange
            var command = new AttachPhotoCommand
            {
                LocationId = 1,
                PhotoPath = null
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.PhotoPath)
                .WithErrorMessage("Photo path is required");
        }

        [Test]
        public void Validate_WithValidFilePath_ShouldNotHaveErrors()
        {
            // Arrange
            var command = new AttachPhotoCommand
            {
                LocationId = 123,
                PhotoPath = @"C:\Users\Test\Pictures\photo.jpg"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.PhotoPath);
        }

        [Test]
        public void Validate_WithRelativePath_ShouldNotHaveErrors()
        {
            // Arrange
            var command = new AttachPhotoCommand
            {
                LocationId = 1,
                PhotoPath = "./photos/landscape.jpg"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.PhotoPath);
        }
    }
}