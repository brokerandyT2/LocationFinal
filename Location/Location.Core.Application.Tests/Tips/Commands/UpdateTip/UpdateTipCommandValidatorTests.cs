using FluentValidation.TestHelper;
using Location.Core.Application.Tips.Commands.UpdateTip;
using NUnit.Framework;

namespace Location.Core.Application.Tests.Tips.Commands.UpdateTip
{
    [TestFixture]
    public class UpdateTipCommandValidatorTests
    {
        private UpdateTipCommandValidator _validator;

        [SetUp]
        public void SetUp()
        {
            _validator = new UpdateTipCommandValidator();
        }

        [Test]
        public void Validate_WithValidData_ShouldNotHaveErrors()
        {
            // Arrange
            var command = new UpdateTipCommand
            {
                Id = 1,
                TipTypeId = 2,
                Title = "Updated Rule of Thirds",
                Content = "Updated content about composition",
                Fstop = "f/11",
                ShutterSpeed = "1/250",
                Iso = "ISO 200",
                I8n = "en-US"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Test]
        public void Validate_WithZeroId_ShouldHaveError()
        {
            // Arrange
            var command = new UpdateTipCommand
            {
                Id = 0,
                TipTypeId = 1,
                Title = "Test Tip",
                Content = "Test content",
                I8n = "en-US"
            };

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
            var command = new UpdateTipCommand
            {
                Id = -1,
                TipTypeId = 1,
                Title = "Test Tip",
                Content = "Test content",
                I8n = "en-US"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Id)
                .WithErrorMessage("Id must be greater than 0");
        }

        [Test]
        public void Validate_WithZeroTipTypeId_ShouldHaveError()
        {
            // Arrange
            var command = new UpdateTipCommand
            {
                Id = 1,
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
            var command = new UpdateTipCommand
            {
                Id = 1,
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
            var command = new UpdateTipCommand
            {
                Id = 1,
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
            var command = new UpdateTipCommand
            {
                Id = 1,
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
            var command = new UpdateTipCommand
            {
                Id = 1,
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
        public void Validate_WithEmptyI8n_ShouldHaveError()
        {
            // Arrange
            var command = new UpdateTipCommand
            {
                Id = 1,
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
        public void Validate_WithEmptyOptionalFields_ShouldNotHaveErrors()
        {
            // Arrange
            var command = new UpdateTipCommand
            {
                Id = 1,
                TipTypeId = 1,
                Title = "Test Tip",
                Content = "Test content",
                Fstop = string.Empty,
                ShutterSpeed = string.Empty,
                Iso = string.Empty,
                I8n = "en-US"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Fstop);
            result.ShouldNotHaveValidationErrorFor(x => x.ShutterSpeed);
            result.ShouldNotHaveValidationErrorFor(x => x.Iso);
        }
    }
}