using FluentValidation.TestHelper;
using Location.Core.Application.Tips.Commands.DeleteTip;
using NUnit.Framework;

namespace Location.Core.Application.Tests.Tips.Commands.DeleteTip
{
    [Category("Tips")]
    [Category("Delete")]
    [TestFixture]
    public class DeleteTipCommandValidatorTests
    {
        private DeleteTipCommandValidator _validator;

        [SetUp]
        public void SetUp()
        {
            _validator = new DeleteTipCommandValidator();
        }

        [Test]
        public void Validate_WithValidId_ShouldNotHaveErrors()
        {
            // Arrange
            var command = new DeleteTipCommand { Id = 1 };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Test]
        public void Validate_WithZeroId_ShouldHaveError()
        {
            // Arrange
            var command = new DeleteTipCommand { Id = 0 };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Id)
                .WithErrorMessage("Id must be greater than 0");
        }

        [Test]
        public void Validate_WithNegativeId_ShouldHaveError()
        {
            // Arrange
            var command = new DeleteTipCommand { Id = -1 };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Id)
                .WithErrorMessage("Id must be greater than 0");
        }

        [Test]
        public void Validate_WithLargePositiveId_ShouldNotHaveErrors()
        {
            // Arrange
            var command = new DeleteTipCommand { Id = int.MaxValue };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Test]
        public void Validate_WithRandomPositiveId_ShouldNotHaveErrors()
        {
            // Arrange
            var command = new DeleteTipCommand { Id = 42 };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Test]
        public void Validate_WithNegativeLargeId_ShouldHaveError()
        {
            // Arrange
            var command = new DeleteTipCommand { Id = int.MinValue };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Id)
                .WithErrorMessage("Id must be greater than 0");
        }
    }
}