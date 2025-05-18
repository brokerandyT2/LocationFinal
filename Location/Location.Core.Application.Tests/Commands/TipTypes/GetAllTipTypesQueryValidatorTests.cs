using FluentValidation.TestHelper;
using Location.Core.Application.Queries.TipTypes;
using NUnit.Framework;

namespace Location.Core.Application.Tests.Commands.TipTypes
{
    [Category("Tip Types")]

    [TestFixture]
    public class GetAllTipTypesQueryValidatorTests
    {
        private GetAllTipTypesQueryValidator _validator;

        [SetUp]
        public void SetUp()
        {
            _validator = new GetAllTipTypesQueryValidator();
        }

        [Test]
        public void Validate_WithEmptyQuery_ShouldNotHaveErrors()
        {
            // Arrange
            var query = new GetAllTipTypesQuery();

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Test]
        public void Validate_ShouldAlwaysSucceed()
        {
            // Arrange
            // GetAllTipTypesQuery has no properties to validate
            var query = new GetAllTipTypesQuery();

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }
    }
}