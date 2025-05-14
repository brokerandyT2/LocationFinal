using NUnit.Framework;
using FluentAssertions;
using Moq;
using MediatR;
using FluentValidation;
using FluentValidation.Results;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Common.Behaviors;

namespace Location.Core.Application.Tests.Common.Behaviors
{
    [TestFixture]
    public class ValidationBehaviorTests
    {
        private ValidationBehavior<TestRequest, Result<string>> _behavior;
        private Mock<IValidator<TestRequest>> _validatorMock;
        private Mock<RequestHandlerDelegate<Result<string>>> _nextMock;

        [SetUp]
        public void Setup()
        {
            _validatorMock = new Mock<IValidator<TestRequest>>();
            _nextMock = new Mock<RequestHandlerDelegate<Result<string>>>();

            var validators = new List<IValidator<TestRequest>> { _validatorMock.Object };
            _behavior = new ValidationBehavior<TestRequest, Result<string>>(validators);
        }

        [Test]
        public async Task Handle_WithNoValidationErrors_ShouldContinuePipeline()
        {
            var command = new TestRequest { Value = "valid" };
            var expectedResult = Result<string>.Success("Success");

            _validatorMock
                .Setup(x => x.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _nextMock
                .Setup(x => x())
                .ReturnsAsync(expectedResult);

            var result = await _behavior.Handle(command, _nextMock.Object, CancellationToken.None);

            result.Should().Be(expectedResult);
            _nextMock.Verify(x => x(), Times.Once);
        }

        [Test]
        public async Task Handle_WithValidationErrors_ShouldReturnFailure()
        {
            var command = new TestRequest { Value = "" };
            var validationErrors = new List<ValidationFailure>
            {
                new ValidationFailure("Value", "Value is required"),
                new ValidationFailure("Value", "Value must not be empty")
            };

            _validatorMock
                .Setup(x => x.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(validationErrors));

            var result = await _behavior.Handle(command, _nextMock.Object, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().HaveCount(2);
            result.Errors.Should().Contain(x => x.Message == "Value is required");
            result.Errors.Should().Contain(x => x.Message == "Value must not be empty");
            _nextMock.Verify(x => x(), Times.Never);
        }

        [Test]
        public async Task Handle_WithMultipleValidators_ShouldValidateAll()
        {
            var command = new TestRequest { Value = "test" };
            var validator2Mock = new Mock<IValidator<TestRequest>>();

            var validators = new List<IValidator<TestRequest>>
            {
                _validatorMock.Object,
                validator2Mock.Object
            };

            var behavior = new ValidationBehavior<TestRequest, Result<string>>(validators);

            _validatorMock
                .Setup(x => x.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            validator2Mock
                .Setup(x => x.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _nextMock
                .Setup(x => x())
                .ReturnsAsync(Result<string>.Success("Success"));

            var result = await behavior.Handle(command, _nextMock.Object, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            _validatorMock.Verify(x => x.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()), Times.Once);
            validator2Mock.Verify(x => x.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_WithNoValidators_ShouldContinuePipeline()
        {
            var command = new TestRequest { Value = "test" };
            var behavior = new ValidationBehavior<TestRequest, Result<string>>(Enumerable.Empty<IValidator<TestRequest>>());
            var expectedResult = Result<string>.Success("Success");

            _nextMock
                .Setup(x => x())
                .ReturnsAsync(expectedResult);

            var result = await behavior.Handle(command, _nextMock.Object, CancellationToken.None);

            result.Should().Be(expectedResult);
            _nextMock.Verify(x => x(), Times.Once);
        }

        [Test]
        public async Task Handle_WithCancellationToken_ShouldPassThrough()
        {
            var command = new TestRequest { Value = "test" };
            var cts = new CancellationTokenSource();
            var token = cts.Token;

            _validatorMock
                .Setup(x => x.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), token))
                .ReturnsAsync(new ValidationResult());

            _nextMock
                .Setup(x => x())
                .ReturnsAsync(Result<string>.Success("Success"));

            await _behavior.Handle(command, _nextMock.Object, token);

            _validatorMock.Verify(x => x.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), token), Times.Once);
        }

        [Test]
        public async Task Handle_WithValidationException_ShouldConvertToFailure()
        {
            var command = new TestRequest { Value = "test" };

            _validatorMock
                .Setup(x => x.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ValidationException("Validation failed"));

            var result = await _behavior.Handle(command, _nextMock.Object, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().ContainSingle();
            result.Errors.First().Message.Should().Be("Validation failed");
            _nextMock.Verify(x => x(), Times.Never);
        }

        // Use TestRequest instead of TestCommand to avoid NUnit naming conflict
        private class TestRequest : IRequest<Result<string>>
        {
            public string Value { get; set; } = string.Empty;
        }
    }
}