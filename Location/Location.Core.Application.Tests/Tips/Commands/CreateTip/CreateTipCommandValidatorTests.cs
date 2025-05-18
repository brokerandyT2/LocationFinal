using FluentValidation.TestHelper;
using Location.Core.Application.Tips.Commands.CreateTip;
using NUnit.Framework;

namespace Location.Core.Application.Tests.Tips.Commands.CreateTip
{
    [Category("Tips")]
    [Category("Create")]
    [TestFixture]
    public class CreateTipCommandValidatorTests
    {
        private CreateTipCommandValidator _validator;

        [SetUp]
        public void SetUp()
        {
            _validator = new CreateTipCommandValidator();
        }

        [Test]
        public void Validate_WithValidData_ShouldNotHaveErrors()
        {
            // Arrange
            var command = new CreateTipCommand
            {
                TipTypeId = 1,
                Title = "Rule of Thirds",
                Content = "Divide your frame into thirds for better composition",
                Fstop = "f/8",
                ShutterSpeed = "1/125",
                Iso = "ISO 100",
                I8n = "en-US"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Test]
        public void Validate_WithZeroTipTypeId_ShouldHaveError()
        {
            // Arrange
            var command = new CreateTipCommand
            {
                TipTypeId = 0,
                Title = "Test Tip",
                Content = "Test content",
                I8n = "en-US"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.TipTypeId)
                .WithErrorMessage("TipTypeId must be greater than 0");
        }

        [Test]
        public void Validate_WithEmptyTitle_ShouldHaveError()
        {
            // Arrange
            var command = new CreateTipCommand
            {
                TipTypeId = 1,
                Title = string.Empty,
                Content = "Test content",
                I8n = "en-US"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Title)
                .WithErrorMessage("Title is required");
        }

        [Test]
        public void Validate_WithTitleExceedingMaxLength_ShouldHaveError()
        {
            // Arrange
            var command = new CreateTipCommand
            {
                TipTypeId = 1,
                Title = new string('a', 101),
                Content = "Test content",
                I8n = "en-US"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Title)
                .WithErrorMessage("Title must not exceed 100 characters");
        }

        [Test]
        public void Validate_WithEmptyContent_ShouldHaveError()
        {
            // Arrange
            var command = new CreateTipCommand
            {
                TipTypeId = 1,
                Title = "Test Tip",
                Content = string.Empty,
                I8n = "en-US"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Content)
                .WithErrorMessage("Content is required");
        }

        [Test]
        public void Validate_WithContentExceedingMaxLength_ShouldHaveError()
        {
            // Arrange
            var command = new CreateTipCommand
            {
                TipTypeId = 1,
                Title = "Test Tip",
                Content = new string('a', 1001),
                I8n = "en-US"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Content)
                .WithErrorMessage("Content must not exceed 1000 characters");
        }

        [Test]
        public void Validate_WithFstopExceedingMaxLength_ShouldHaveError()
        {
            // Arrange
            var command = new CreateTipCommand
            {
                TipTypeId = 1,
                Title = "Test Tip",
                Content = "Test content",
                Fstop = new string('a', 21),
                I8n = "en-US"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Fstop)
                .WithErrorMessage("F-stop must not exceed 20 characters");
        }

        [Test]
        public void Validate_WithShutterSpeedExceedingMaxLength_ShouldHaveError()
        {
            // Arrange
            var command = new CreateTipCommand
            {
                TipTypeId = 1,
                Title = "Test Tip",
                Content = "Test content",
                ShutterSpeed = new string('a', 21),
                I8n = "en-US"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.ShutterSpeed)
                .WithErrorMessage("Shutter speed must not exceed 20 characters");
        }

        [Test]
        public void Validate_WithIsoExceedingMaxLength_ShouldHaveError()
        {
            // Arrange
            var command = new CreateTipCommand
            {
                TipTypeId = 1,
                Title = "Test Tip",
                Content = "Test content",
                Iso = new string('a', 21),
                I8n = "en-US"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Iso)
                .WithErrorMessage("ISO must not exceed 20 characters");
        }

        [Test]
        public void Validate_WithEmptyI8n_ShouldHaveError()
        {
            // Arrange
            var command = new CreateTipCommand
            {
                TipTypeId = 1,
                Title = "Test Tip",
                Content = "Test content",
                I8n = string.Empty
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
            var command = new CreateTipCommand
            {
                TipTypeId = 1,
                Title = "Test Tip",
                Content = "Test content",
                I8n = new string('a', 11)
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.I8n)
                .WithErrorMessage("Localization must not exceed 10 characters");
        }
    }
}