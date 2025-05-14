using NUnit.Framework;
using FluentAssertions;
using FluentValidation.TestHelper;
using Location.Core.Application.Tests.Helpers;
using FluentValidation;
using Location.Core.Application.Commands.Locations;

namespace Location.Core.Application.Tests.Locations.Commands.DeleteLocation
{
    [TestFixture]
    public class DeleteLocationCommandValidatorTests
    {
        private DeleteLocationCommandValidator _validator;

        [SetUp]
        public void Setup()
        {
            _validator = new DeleteLocationCommandValidator();
        }

        [Test]
        public void Validate_WithValidId_ShouldNotHaveErrors()
        {
            // Arrange
            var command = new DeleteLocationCommand { Id = 1 };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Test]
        public void Validate_WithZeroId_ShouldHaveError()
        {
            // Arrange
            var command = new DeleteLocationCommand { Id = 0 };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Id)
                .WithErrorMessage("Location ID must be greater than 0");
        }

        [Test]
        public void Validate_WithNegativeId_ShouldHaveError()
        {
            // Arrange
            var command = new DeleteLocationCommand { Id = -1 };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Id)
                .WithErrorMessage("Location ID must be greater than 0");
        }

        [Test]
        public void Validate_WithLargePositiveId_ShouldNotHaveErrors()
        {
            // Arrange
            var command = new DeleteLocationCommand { Id = int.MaxValue };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }
    }

    // Placeholder for the actual implementation
    public class DeleteLocationCommandValidator : FluentValidation.AbstractValidator<DeleteLocationCommand>
    {
        public DeleteLocationCommandValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0)
                .WithMessage("Location ID must be greater than 0");
        }
    }
}