using FluentValidation.TestHelper;
using Location.Core.Application.Settings.Commands.UpdateSetting;
using NUnit.Framework;

namespace Location.Core.Application.Tests.Settings.Commands.UpdateSetting
{
    [TestFixture]
    public class UpdateSettingCommandValidatorTests
    {
        private UpdateSettingCommandValidator _validator;

        [SetUp]
        public void SetUp()
        {
            _validator = new UpdateSettingCommandValidator();
        }

        [Test]
        public void Validate_WithValidData_ShouldNotHaveErrors()
        {
            // Arrange
            var command = new UpdateSettingCommand
            {
                Key = "WeatherApiKey",
                Value = "updated-api-key-123"
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
            var command = new UpdateSettingCommand
            {
                Key = string.Empty,
                Value = "test-value"
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
            var command = new UpdateSettingCommand
            {
                Key = null,
                Value = "test-value"
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
            var command = new UpdateSettingCommand
            {
                Key = new string('a', 51),
                Value = "test-value"
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
            var command = new UpdateSettingCommand
            {
                Key = "TestKey",
                Value = null
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
            var command = new UpdateSettingCommand
            {
                Key = "TestKey",
                Value = new string('a', 501)
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Value)
                .WithErrorMessage("Value must not exceed 500 characters");
        }

        [Test]
        public void Validate_WithEmptyValue_ShouldNotHaveErrors()
        {
            // Arrange
            var command = new UpdateSettingCommand
            {
                Key = "TestKey",
                Value = string.Empty
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Value);
        }

        [Test]
        public void Validate_WithMaxLengthKey_ShouldNotHaveErrors()
        {
            // Arrange
            var command = new UpdateSettingCommand
            {
                Key = new string('a', 50),
                Value = "test-value"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Key);
        }

        [Test]
        public void Validate_WithMaxLengthValue_ShouldNotHaveErrors()
        {
            // Arrange
            var command = new UpdateSettingCommand
            {
                Key = "TestKey",
                Value = new string('a', 500)
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Value);
        }
    }
}