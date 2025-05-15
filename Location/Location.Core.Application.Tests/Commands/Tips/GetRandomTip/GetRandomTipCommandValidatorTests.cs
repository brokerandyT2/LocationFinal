using FluentValidation.TestHelper;
using Location.Core.Application.Commands.Tips;
using NUnit.Framework;

namespace Location.Core.Application.Tests.Commands.Tips.GetRandomTip
{
    [TestFixture]
    public class GetRandomTipCommandValidatorTests
    {
        private GetRandomTipCommandValidator _validator;

        [SetUp]
        public void SetUp()
        {
            _validator = new GetRandomTipCommandValidator();
        }

        [Test]
        public void Validate_WithValidTipTypeId_ShouldNotHaveErrors()
        {
            // Arrange
            var command = new GetRandomTipCommand { TipTypeId = 1 };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Test]
        public void Validate_WithZeroTipTypeId_ShouldHaveError()
        {
            // Arrange
            var command = new GetRandomTipCommand { TipTypeId = 0 };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.TipTypeId)
                .WithErrorMessage("TipTypeId must be greater than 0");
        }

        [Test]
        public void Validate_WithNegativeTipTypeId_ShouldHaveError()
        {
            // Arrange
            var command = new GetRandomTipCommand { TipTypeId = -1 };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.TipTypeId)
                .WithErrorMessage("TipTypeId must be greater than 0");
        }

        [Test]
        public void Validate_WithLargePositiveTipTypeId_ShouldNotHaveErrors()
        {
            // Arrange
            var command = new GetRandomTipCommand { TipTypeId = int.MaxValue };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Test]
        public void Validate_WithRandomPositiveTipTypeId_ShouldNotHaveErrors()
        {
            // Arrange
            var command = new GetRandomTipCommand { TipTypeId = 999 };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Test]
        public void Validate_WithNegativeLargeTipTypeId_ShouldHaveError()
        {
            // Arrange
            var command = new GetRandomTipCommand { TipTypeId = int.MinValue };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.TipTypeId)
                .WithErrorMessage("TipTypeId must be greater than 0");
        }
    }
}