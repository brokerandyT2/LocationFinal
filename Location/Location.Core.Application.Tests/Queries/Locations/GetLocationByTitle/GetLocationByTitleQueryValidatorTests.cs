using FluentValidation.TestHelper;
using Location.Core.Application.Queries.Locations;
using NUnit.Framework;

namespace Location.Core.Application.Tests.Queries.Locations.GetLocationByTitle
{
    [Category("Locations")]
    [Category("Query")]

    [TestFixture]
    public class GetLocationByTitleQueryValidatorTests
    {
        private GetLocationByTitleQueryValidator _validator;

        [SetUp]
        public void SetUp()
        {
            _validator = new GetLocationByTitleQueryValidator();
        }

        [Test]
        public void Validate_WithValidTitle_ShouldNotHaveErrors()
        {
            // Arrange
            var query = new GetLocationByTitleQuery { Title = "Space Needle" };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Test]
        public void Validate_WithEmptyTitle_ShouldHaveError()
        {
            // Arrange
            var query = new GetLocationByTitleQuery { Title = string.Empty };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Title)
                .WithErrorMessage("Title is required");
        }

        [Test]
        public void Validate_WithNullTitle_ShouldHaveError()
        {
            // Arrange
            var query = new GetLocationByTitleQuery { Title = null };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Title)
                .WithErrorMessage("Title is required");
        }

        [Test]
        public void Validate_WithTitleExceedingMaxLength_ShouldHaveError()
        {
            // Arrange
            var query = new GetLocationByTitleQuery { Title = new string('a', 101) };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Title)
                .WithErrorMessage("Title must not exceed 100 characters");
        }

        [Test]
        public void Validate_WithWhitespaceTitle_ShouldHaveError()
        {
            // Arrange
            var query = new GetLocationByTitleQuery { Title = "   " };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Title)
                .WithErrorMessage("Title is required");
        }

        [Test]
        public void Validate_WithMaxLengthTitle_ShouldNotHaveErrors()
        {
            // Arrange
            var query = new GetLocationByTitleQuery { Title = new string('a', 100) };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }
    }
}