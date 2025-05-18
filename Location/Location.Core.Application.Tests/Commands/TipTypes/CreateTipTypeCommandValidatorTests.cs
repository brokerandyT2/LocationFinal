using FluentValidation.TestHelper;
using Location.Core.Application.Commands.TipTypes;
using NUnit.Framework;

namespace Location.Core.Application.Tests.Commands.TipTypes
{
    [Category("Tip Types")]

    [TestFixture]
    public class CreateTipTypeCommandValidatorTests
    {
        private CreateTipTypeCommandValidator _validator;

        [SetUp]
        public void SetUp()
        {
            _validator = new CreateTipTypeCommandValidator();
        }

        [Test]
        public void Validate_WithValidData_ShouldNotHaveErrors()
        {
            // Arrange
            var command = new CreateTipTypeCommand
            {
                Name = "Landscape Photography",
                I8n = "en-US"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Test]
        public void Validate_WithEmptyName_ShouldHaveError()
        {
            // Arrange
            var command = new CreateTipTypeCommand
            {
                Name = string.Empty,
                I8n = "en-US"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Name)
                .WithErrorMessage("Name is required");
        }

        [Test]
        public void Validate_WithNullName_ShouldHaveError()
        {
            // Arrange
            var command = new CreateTipTypeCommand
            {
                Name = null,
                I8n = "en-US"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Name)
                .WithErrorMessage("Name is required");
        }

        [Test]
        public void Validate_WithNameExceedingMaxLength_ShouldHaveError()
        {
            // Arrange
            var command = new CreateTipTypeCommand
            {
                Name = new string('a', 101),
                I8n = "en-US"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Name)
                .WithErrorMessage("Name must not exceed 100 characters");
        }

        [Test]
        public void Validate_WithWhitespaceName_ShouldHaveError()
        {
            // Arrange
            var command = new CreateTipTypeCommand
            {
                Name = "   ",
                I8n = "en-US"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Name)
                .WithErrorMessage("Name is required");
        }

        [Test]
        public void Validate_WithEmptyI8n_ShouldHaveError()
        {
            // Arrange
            var command = new CreateTipTypeCommand
            {
                Name = "Portrait Photography",
                I8n = string.Empty
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.I8n)
                .WithErrorMessage("Localization is required");
        }

        [Test]
        public void Validate_WithNullI8n_ShouldHaveError()
        {
            // Arrange
            var command = new CreateTipTypeCommand
            {
                Name = "Portrait Photography",
                I8n = null
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.I8n)
                .WithErrorMessage("Localization is required");
        }

        [Test]
        public void Validate_WithI8nExceedingMaxLength_ShouldHaveError()
        {
            // Arrange
            var command = new CreateTipTypeCommand
            {
                Name = "Portrait Photography",
                I8n = new string('a', 11)
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.I8n)
                .WithErrorMessage("Localization must not exceed 10 characters");
        }

        [Test]
        public void Validate_WithMaxLengthName_ShouldNotHaveErrors()
        {
            // Arrange
            var command = new CreateTipTypeCommand
            {
                Name = new string('a', 100),
                I8n = "en-US"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Name);
        }

        [Test]
        public void Validate_WithMaxLengthI8n_ShouldNotHaveErrors()
        {
            // Arrange
            var command = new CreateTipTypeCommand
            {
                Name = "Portrait Photography",
                I8n = new string('a', 10)
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.I8n);
        }
    }
}