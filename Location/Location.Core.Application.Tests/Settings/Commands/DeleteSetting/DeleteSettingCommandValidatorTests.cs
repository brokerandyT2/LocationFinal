using FluentValidation.TestHelper;
using Location.Core.Application.Settings.Commands.DeleteSetting;
using NUnit.Framework;

namespace Location.Core.Application.Tests.Settings.Commands.DeleteSetting
{
    [TestFixture]
    public class DeleteSettingCommandValidatorTests
    {
        private DeleteSettingCommandValidator _validator;

        [SetUp]
        public void SetUp()
        {
            _validator = new DeleteSettingCommandValidator();
        }

        [Test]
        public void Validate_WithValidKey_ShouldNotHaveErrors()
        {
            // Arrange
            var command = new DeleteSettingCommand { Key = "WeatherApiKey" };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Test]
        public void Validate_WithEmptyKey_ShouldHaveError()
        {
            // Arrange
            var command = new DeleteSettingCommand { Key = string.Empty };

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
            var command = new DeleteSettingCommand { Key = null };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Key)
                .WithErrorMessage("Key is required");
        }

        [Test]
        public void Validate_WithWhitespaceKey_ShouldHaveError()
        {
            // Arrange
            var command = new DeleteSettingCommand { Key = "   " };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Key)
                .WithErrorMessage("Key is required");
        }

        [Test]
        public void Validate_WithSpecialCharacterKey_ShouldNotHaveErrors()
        {
            // Arrange
            var command = new DeleteSettingCommand { Key = "Key@With#Special$Chars%" };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Test]
        public void Validate_WithNumericKey_ShouldNotHaveErrors()
        {
            // Arrange
            var command = new DeleteSettingCommand { Key = "12345" };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Test]
        public void Validate_WithUnderscoreKey_ShouldNotHaveErrors()
        {
            // Arrange
            var command = new DeleteSettingCommand { Key = "Weather_Api_Key" };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Test]
        public void Validate_WithHyphenKey_ShouldNotHaveErrors()
        {
            // Arrange
            var command = new DeleteSettingCommand { Key = "weather-api-key" };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Test]
        public void Validate_WithDotKey_ShouldNotHaveErrors()
        {
            // Arrange
            var command = new DeleteSettingCommand { Key = "app.weather.apikey" };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }
    }
}