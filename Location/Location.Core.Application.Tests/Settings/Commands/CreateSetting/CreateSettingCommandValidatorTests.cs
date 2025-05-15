using FluentValidation.TestHelper;
using Location.Core.Application.Settings.Commands.CreateSetting;
using NUnit.Framework;

namespace Location.Core.Application.Tests.Settings.Commands.CreateSetting
{
    [TestFixture]
    public class CreateSettingCommandValidatorTests
    {
        private CreateSettingCommandValidator _validator;

        [SetUp]
        public void SetUp()
        {
            _validator = new CreateSettingCommandValidator();
        }

        [Test]
        public void Validate_WithValidData_ShouldNotHaveErrors()
        {
            // Arrange
            var command = new CreateSettingCommand
            {
                Key = "WeatherApiKey",
                Value = "test-api-key-123",
                Description = "API key for weather service"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Test]
        public void Validate_WithEmptyKey_ShouldHaveError()
        {
            // Arrange
            var command = new CreateSettingCommand
            {
                Key = string.Empty,
                Value = "test-value",
                Description = "Test description"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Key)
                .WithErrorMessage("Key is required");
        }

        [Test]
        public void Validate_WithNullKey_ShouldHaveError()
        {
            // Arrange
            var command = new CreateSettingCommand
            {
                Key = null,
                Value = "test-value",
                Description = "Test description"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Key)
                .WithErrorMessage("Key is required");
        }

        [Test]
        public void Validate_WithKeyExceedingMaxLength_ShouldHaveError()
        {
            // Arrange
            var command = new CreateSettingCommand
            {
                Key = new string('a', 51),
                Value = "test-value",
                Description = "Test description"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Key)
                .WithErrorMessage("Key must not exceed 50 characters");
        }

        [Test]
        public void Validate_WithNullValue_ShouldHaveError()
        {
            // Arrange
            var command = new CreateSettingCommand
            {
                Key = "TestKey",
                Value = null,
                Description = "Test description"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Value)
                .WithErrorMessage("Value cannot be null");
        }

        [Test]
        public void Validate_WithValueExceedingMaxLength_ShouldHaveError()
        {
            // Arrange
            var command = new CreateSettingCommand
            {
                Key = "TestKey",
                Value = new string('a', 501),
                Description = "Test description"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Value)
                .WithErrorMessage("Value must not exceed 500 characters");
        }

        [Test]
        public void Validate_WithDescriptionExceedingMaxLength_ShouldHaveError()
        {
            // Arrange
            var command = new CreateSettingCommand
            {
                Key = "TestKey",
                Value = "test-value",
                Description = new string('a', 201)
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Description)
                .WithErrorMessage("Description must not exceed 200 characters");
        }

        [Test]
        public void Validate_WithEmptyValue_ShouldNotHaveErrors()
        {
            // Arrange
            var command = new CreateSettingCommand
            {
                Key = "TestKey",
                Value = string.Empty,
                Description = "Test description"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Value);
        }
    }
}