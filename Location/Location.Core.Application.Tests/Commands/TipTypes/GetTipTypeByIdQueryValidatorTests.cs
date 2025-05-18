using FluentValidation.TestHelper;
using Location.Core.Application.Queries.TipTypes;
using NUnit.Framework;

namespace Location.Core.Application.Tests.Commands.TipTypes
{
    [Category("Tip Types")]

    [TestFixture]
    public class GetTipTypeByIdQueryValidatorTests
    {
        private GetTipTypeByIdQueryValidator _validator;

        [SetUp]
        public void SetUp()
        {
            _validator = new GetTipTypeByIdQueryValidator();
        }

        [Test]
        public void Validate_WithValidId_ShouldNotHaveErrors()
        {
            // Arrange
            var query = new GetTipTypeByIdQuery { Id = 1 };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Test]
        public void Validate_WithZeroId_ShouldHaveError()
        {
            // Arrange
            var query = new GetTipTypeByIdQuery { Id = 0 };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Id)
                .WithErrorMessage("Id must be greater than 0");
        }

        [Test]
        public void Validate_WithNegativeId_ShouldHaveError()
        {
            // Arrange
            var query = new GetTipTypeByIdQuery { Id = -1 };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Id)
                .WithErrorMessage("Id must be greater than 0");
        }

        [Test]
        public void Validate_WithLargePositiveId_ShouldNotHaveErrors()
        {
            // Arrange
            var query = new GetTipTypeByIdQuery { Id = int.MaxValue };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Test]
        public void Validate_WithRandomPositiveId_ShouldNotHaveErrors()
        {
            // Arrange
            var query = new GetTipTypeByIdQuery { Id = 42 };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Test]
        public void Validate_WithMinValueId_ShouldHaveError()
        {
            // Arrange
            var query = new GetTipTypeByIdQuery { Id = int.MinValue };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Id)
                .WithErrorMessage("Id must be greater than 0");
        }

        [Test]
        public void Validate_WithBoundaryValue_ShouldHaveError()
        {
            // Arrange
            var query = new GetTipTypeByIdQuery { Id = 0 };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Id)
                .WithErrorMessage("Id must be greater than 0");
        }

        [Test]
        public void Validate_WithBoundaryValuePlusOne_ShouldNotHaveErrors()
        {
            // Arrange
            var query = new GetTipTypeByIdQuery { Id = 1 };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }
    }
}