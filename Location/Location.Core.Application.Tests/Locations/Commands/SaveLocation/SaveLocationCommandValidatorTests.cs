using NUnit.Framework;
using FluentAssertions;
using FluentValidation.TestHelper;
using Location.Core.Application.Locations.Commands.SaveLocation;
using Location.Core.Application.Tests.Helpers;
using FluentValidation;

namespace Location.Core.Application.Tests.Locations.Commands.SaveLocation
{
    [TestFixture]
    public class SaveLocationCommandValidatorTests
    {
        private SaveLocationCommandValidator _validator;

        [SetUp]
        public void Setup()
        {
            _validator = new SaveLocationCommandValidator();
        }

        [Test]
        public void Validate_WithValidCommand_ShouldNotHaveErrors()
        {
            // Arrange
            var command = TestDataBuilder.CreateValidSaveLocationCommand();

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Test]
        public void Validate_WithEmptyTitle_ShouldHaveError()
        {
            // Arrange
            var command = TestDataBuilder.CreateValidSaveLocationCommand(title: "");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Title)
                .WithErrorMessage("Title is required");
        }

        [Test]
        public void Validate_WithNullTitle_ShouldHaveError()
        {
            // Arrange
            var command = TestDataBuilder.CreateValidSaveLocationCommand();
            command.Title = null!;

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Title)
                .WithErrorMessage("Title is required");
        }

        [Test]
        public void Validate_WithTooLongTitle_ShouldHaveError()
        {
            // Arrange
            var command = TestDataBuilder.CreateValidSaveLocationCommand(title: new string('a', 101));

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Title)
                .WithErrorMessage("Title cannot exceed 100 characters");
        }

        [Test]
        public void Validate_WithTooLongDescription_ShouldHaveError()
        {
            // Arrange
            var command = TestDataBuilder.CreateValidSaveLocationCommand(description: new string('a', 501));

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Description)
                .WithErrorMessage("Description cannot exceed 500 characters");
        }

        [Test]
        public void Validate_WithInvalidLatitude_ShouldHaveError()
        {
            // Arrange
            var command = TestDataBuilder.CreateValidSaveLocationCommand(latitude: 91);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Latitude)
                .WithErrorMessage("Latitude must be between -90 and 90");
        }

        [Test]
        public void Validate_WithInvalidLongitude_ShouldHaveError()
        {
            // Arrange
            var command = TestDataBuilder.CreateValidSaveLocationCommand(longitude: 181);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Longitude)
                .WithErrorMessage("Longitude must be between -180 and 180");
        }

        [Test]
        public void Validate_WithEmptyCity_ShouldHaveError()
        {
            // Arrange
            var command = TestDataBuilder.CreateValidSaveLocationCommand(city: "");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.City)
                .WithErrorMessage("City is required");
        }

        [Test]
        public void Validate_WithEmptyState_ShouldHaveError()
        {
            // Arrange
            var command = TestDataBuilder.CreateValidSaveLocationCommand(state: "");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.State)
                .WithErrorMessage("State is required");
        }

        [Test]
        public void Validate_WithNullIsland_ShouldHaveError()
        {
            // Arrange
            var command = TestDataBuilder.CreateValidSaveLocationCommand(latitude: 0, longitude: 0);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x)
                .WithErrorMessage("Null Island (0,0) is not a valid location");
        }

        [Test]
        public void Validate_WithValidPhoto_ShouldNotHaveErrors()
        {
            // Arrange
            var command = TestDataBuilder.CreateValidSaveLocationCommand(photoPath: "/photos/test.jpg");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Test]
        public void Validate_WithNullPhoto_ShouldNotHaveErrors()
        {
            // Arrange
            var command = TestDataBuilder.CreateValidSaveLocationCommand(photoPath: null);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Test]
        public void Validate_WithExistingLocationId_ShouldNotHaveErrors()
        {
            // Arrange
            var command = TestDataBuilder.CreateValidSaveLocationCommand(id: 123);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }
    }

    // Placeholder for the actual implementation
    public class SaveLocationCommandValidator : FluentValidation.AbstractValidator<SaveLocationCommand>
    {
        public SaveLocationCommandValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title is required")
                .MaximumLength(100).WithMessage("Title cannot exceed 100 characters");

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Description cannot exceed 500 characters");

            RuleFor(x => x.Latitude)
                .InclusiveBetween(-90, 90).WithMessage("Latitude must be between -90 and 90");

            RuleFor(x => x.Longitude)
                .InclusiveBetween(-180, 180).WithMessage("Longitude must be between -180 and 180");

            RuleFor(x => x.City)
                .NotEmpty().WithMessage("City is required");

            RuleFor(x => x.State)
                .NotEmpty().WithMessage("State is required");

            RuleFor(x => x)
                .Must(cmd => !(cmd.Latitude == 0 && cmd.Longitude == 0))
                .WithMessage("Null Island (0,0) is not a valid location");
        }
    }
}