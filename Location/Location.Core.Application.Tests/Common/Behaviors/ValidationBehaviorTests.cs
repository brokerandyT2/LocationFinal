using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Location.Core.Application.Common.Behaviors;
using Location.Core.Application.Common.Models;
using MediatR;
using Moq;
using NUnit.Framework;

namespace Location.Core.Application.Tests.Common.Behaviors
{
    [Category("Validations")]
    [TestFixture]
    public class ValidationBehaviorTests
    {
        private ValidationBehavior<TestRequest, Result<string>> _behavior;
        private Mock<IValidator<TestRequest>> _validatorMock;
        private Mock<IMediator> _mediatorMock;

        [SetUp]
        public void SetUp()
        {
            _validatorMock = new Mock<IValidator<TestRequest>>();
            _mediatorMock = new Mock<IMediator>();
            _behavior = new ValidationBehavior<TestRequest, Result<string>>(new[] { _validatorMock.Object }, _mediatorMock.Object);
        }

        [Test]
        public async Task Handle_WithNoValidators_ShouldContinuePipeline()
        {
            // Arrange
            var behavior = new ValidationBehavior<TestRequest, Result<string>>(Enumerable.Empty<IValidator<TestRequest>>(), _mediatorMock.Object);
            var request = new TestRequest();
            var expectedResponse = Result<string>.Success("Success");
            RequestHandlerDelegate<Result<string>> next = (_ctx) => Task.FromResult(expectedResponse);

            // Act
            var result = await behavior.Handle(request, next, CancellationToken.None);

            // Assert
            result.Should().Be(expectedResponse);
        }

        [Test]
        public async Task Handle_WithValidationErrors_ShouldReturnFailure()
        {
            // Arrange
            var request = new TestRequest();
            var validationFailures = new List<ValidationFailure>
            {
                new ValidationFailure("Name", "Name is required"),
                new ValidationFailure("Age", "Age must be positive")
            };

            _validatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(validationFailures));

            RequestHandlerDelegate<Result<string>> next = (_ctx) => Task.FromResult(Result<string>.Success("Success"));

            // Act
            var result = await _behavior.Handle(request, next, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().HaveCount(2);
            result.Errors.Should().Contain(e => e.Message == "Name is required");
            result.Errors.Should().Contain(e => e.Message == "Age must be positive");
        }

        // Make test classes public
        public class TestRequest : IRequest<Result<string>>
        {
            public string Name { get; set; }
            public int Age { get; set; }
        }
    }
}