using NUnit.Framework;
using FluentAssertions;
using Location.Core.Application.Common.Behaviors;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Location.Core.Application.Common.Models;

namespace Location.Core.Application.Tests.Common.Behaviors
{
    [TestFixture]
    public class LoggingBehaviorTests
    {
        private Mock<ILogger<LoggingBehavior<TestRequest, Result<string>>>> _mockLogger;
        private LoggingBehavior<TestRequest, Result<string>> _behavior;

        [SetUp]
        public void Setup()
        {
            _mockLogger = new Mock<ILogger<LoggingBehavior<TestRequest, Result<string>>>>();
            _behavior = new LoggingBehavior<TestRequest, Result<string>>(_mockLogger.Object);
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
            var request = new TestRequest { Value = "test" };
            var response = Result<string>.Success("success");
            var cancellationToken = CancellationToken.None;

            RequestHandlerDelegate<Result<string>> next = () => Task.FromResult(response);

            // Act
            var result = await _behavior.Handle(request, next, cancellationToken);

            // Assert
            result.Should().Be(response);
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Starting request")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Test]
        public async Task Handle_WithSuccessfulRequest_ShouldLogStartAndEnd()
        {
            // Arrange
            var request = new TestRequest { Value = "test" };
            var response = Result<string>.Success("success");
            var cancellationToken = CancellationToken.None;

            RequestHandlerDelegate<Result<string>> next = () => Task.FromResult(response);

            // Act
            var result = await _behavior.Handle(request, next, cancellationToken);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Starting request")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Completed request")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Test]
        public async Task Handle_WithLongRunningRequest_ShouldLogWarning()
        {
            // Arrange
            var request = new TestRequest { Value = "test" };
            var response = Result<string>.Success("success");
            var cancellationToken = CancellationToken.None;

            RequestHandlerDelegate<Result<string>> next = async () =>
            {
                await Task.Delay(600);
                return response;
            };

            // Act
            var result = await _behavior.Handle(request, next, cancellationToken);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Long running request")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Test]
        public async Task Handle_WithFailureResult_ShouldLogWarning()
        {
            // Arrange
            var request = new TestRequest { Value = "test" };
            var response = Result<string>.Failure("Error occurred");
            var cancellationToken = CancellationToken.None;

            RequestHandlerDelegate<Result<string>> next = () => Task.FromResult(response);

            // Act
            var result = await _behavior.Handle(request, next, cancellationToken);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Request completed with failure")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Test]
        public async Task Handle_WithException_ShouldLogError()
        {
            // Arrange
            var request = new TestRequest { Value = "test" };
            var expectedException = new InvalidOperationException("Test exception");
            var cancellationToken = CancellationToken.None;

            RequestHandlerDelegate<Result<string>> next = () => throw expectedException;

            // Act
            Func<Task> act = async () => await _behavior.Handle(request, next, cancellationToken);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>();
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Request failed")),
                    expectedException,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
    }

    // Make the test classes public instead of private nested classes
    public class TestRequest : IRequest<Result<string>>
    {
        public string Value { get; set; } = string.Empty;
    }
}