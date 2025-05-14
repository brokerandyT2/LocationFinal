using NUnit.Framework;
using FluentAssertions;
using Location.Core.Application.Common.Behaviors;
using Microsoft.Extensions.Logging;
using Moq;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Common.Interfaces;

namespace Location.Core.Application.Tests.Common.Behaviors
{
    [TestFixture]
    public class LoggingBehaviorTests
    {
        private Mock<ILogger<LoggingBehavior<TestRequest, Result<string>>>> _mockLogger;

        [SetUp]
        public void SetUp()
        {
            _mockLogger = new Mock<ILogger<LoggingBehavior<TestRequest, Result<string>>>>();
        }

        [Test]
        public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
        {
            // Act
            Action act = () => new LoggingBehavior<TestRequest, Result<string>>(null);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("logger");
        }

        [Test]
        public async Task Handle_ShouldLogRequestDetails()
        {
            // Arrange
            var behavior = new LoggingBehavior<TestRequest, Result<string>>(_mockLogger.Object);
            var request = new TestRequest { Value = "Test" };
            var response = Result<string>.Success("Success");
            RequestHandlerDelegate<Result<string>> next = (CancellationToken ct) => Task.FromResult(response);

            // Act
            var result = await behavior.Handle(request, next, CancellationToken.None);

            // Assert
            result.Should().Be(response);
            _mockLogger.Verify(
                x => x.LogInformation(
                    It.IsAny<string>(),
                    It.IsAny<object[]>()),
                Times.AtLeast(2)); // Start and end logging
        }

        [Test]
        public async Task Handle_WithException_ShouldLogError()
        {
            // Arrange
            var behavior = new LoggingBehavior<TestRequest, Result<string>>(_mockLogger.Object);
            var request = new TestRequest { Value = "Test" };
            var exception = new InvalidOperationException("Test exception");
            RequestHandlerDelegate<Result<string>> next = (CancellationToken ct) => throw exception;

            // Act
            Func<Task> act = async () => await behavior.Handle(request, next, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Test exception");
            _mockLogger.Verify(
                x => x.LogError(
                    It.IsAny<Exception>(),
                    It.IsAny<string>(),
                    It.IsAny<object[]>()),
                Times.Once);
        }

        [Test]
        public async Task Handle_WithFailureResult_ShouldLogWarning()
        {
            // Arrange
            var behavior = new LoggingBehavior<TestRequest, Result<string>>(_mockLogger.Object);
            var request = new TestRequest { Value = "Test" };
            var response = Result<string>.Failure("Test error");
            RequestHandlerDelegate<Result<string>> next = (CancellationToken ct) => Task.FromResult(response);

            // Act
            var result = await behavior.Handle(request, next, CancellationToken.None);

            // Assert
            result.Should().Be(response);
            _mockLogger.Verify(
                x => x.LogWarning(
                    It.IsAny<string>(),
                    It.IsAny<object[]>()),
                Times.Once);
        }

        [Test]
        public async Task Handle_WithLongRunningRequest_ShouldLogWarning()
        {
            // Arrange
            var behavior = new LoggingBehavior<TestRequest, Result<string>>(_mockLogger.Object);
            var request = new TestRequest { Value = "Test" };
            var response = Result<string>.Success("Success");
            RequestHandlerDelegate<Result<string>> next = async (CancellationToken ct) =>
            {
                await Task.Delay(600, ct);
                return response;
            };

            // Act
            var result = await behavior.Handle(request, next, CancellationToken.None);

            // Assert
            result.Should().Be(response);
            _mockLogger.Verify(
                x => x.LogWarning(
                    It.IsAny<string>(),
                    It.IsAny<object[]>()),
                Times.Once);
        }

        [Test]
        public async Task Handle_WithSuccessfulRequest_ShouldLogStartAndEnd()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<LoggingBehavior<NonResultRequest, NonResultResponse>>>();
            var behavior = new LoggingBehavior<NonResultRequest, NonResultResponse>(mockLogger.Object);
            var request = new NonResultRequest { Data = "Test" };
            var response = new NonResultResponse { Value = "Success" };
            RequestHandlerDelegate<NonResultResponse> next = (CancellationToken ct) => Task.FromResult(response);

            // Act
            var result = await behavior.Handle(request, next, CancellationToken.None);

            // Assert
            result.Should().Be(response);
            mockLogger.Verify(
                x => x.LogInformation(
                    It.IsAny<string>(),
                    It.IsAny<object[]>()),
                Times.AtLeast(2));
        }

        [Test]
        public async Task Handle_WithCancellationToken_ShouldPassThrough()
        {
            // Arrange
            var behavior = new LoggingBehavior<TestRequest, Result<string>>(_mockLogger.Object);
            var request = new TestRequest { Value = "Test" };
            var response = Result<string>.Success("Success");
            var cancellationToken = new CancellationToken();
            var receivedToken = default(CancellationToken);

            RequestHandlerDelegate<Result<string>> next = (CancellationToken ct) =>
            {
                receivedToken = ct;
                return Task.FromResult(response);
            };

            // Act
            var result = await behavior.Handle(request, next, cancellationToken);

            // Assert
            result.Should().Be(response);
            receivedToken.Should().Be(cancellationToken);
        }
    }

    // Test request/response classes
    public class TestRequest : IRequest<Result<string>>
    {
        public string Value { get; set; } = "";
    }

    public class NonResultRequest : IRequest<NonResultResponse>
    {
        public string Data { get; set; } = "";
    }

    public class NonResultResponse
    {
        public string Value { get; set; } = "";
    }
}