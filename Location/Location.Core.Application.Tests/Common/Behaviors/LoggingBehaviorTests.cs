using NUnit.Framework;
using FluentAssertions;
using Moq;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Common.Behaviors;

namespace Location.Core.Application.Tests.Common.Behaviors
{
    [TestFixture]
    public class LoggingBehaviorTests
    {
        private LoggingBehavior<TestRequest, Result<string>> _behavior;
        private Mock<ILogger<LoggingBehavior<TestRequest, Result<string>>>> _loggerMock;
        private Mock<RequestHandlerDelegate<Result<string>>> _nextMock;
        private CancellationToken _ctx;

        [SetUp]
        public void Setup()
        {
            _ctx = new CancellationToken();
            _loggerMock = new Mock<ILogger<LoggingBehavior<TestRequest, Result<string>>>>();
            _nextMock = new Mock<RequestHandlerDelegate<Result<string>>>();
            _behavior = new LoggingBehavior<TestRequest, Result<string>>(_loggerMock.Object);
        }

        [Test]
        public async Task Handle_WithSuccessfulRequest_ShouldLogStartAndEnd()
        {
            var command = new TestRequest { Value = "test" };
            var result = Result<string>.Success("Success");

            _nextMock
                .Setup(x => x(_ctx))
                .ReturnsAsync(result);

            var actualResult = await _behavior.Handle(command, _nextMock.Object, CancellationToken.None);

            actualResult.Should().Be(result);

            _loggerMock.Verify(
                x => x.Log(
                    It.Is<LogLevel>(level => level == LogLevel.Information),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Starting request")),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
                Times.Once);

            _loggerMock.Verify(
                x => x.Log(
                    It.Is<LogLevel>(level => level == LogLevel.Information),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Completed request")),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
                Times.Once);
        }

        [Test]
        public async Task Handle_WithException_ShouldLogError()
        {
            var command = new TestRequest { Value = "test" };
            var exception = new InvalidOperationException("Test exception");

            _nextMock
                .Setup(x => x(_ctx))
                .ThrowsAsync(exception);

            Func<Task> act = async () => await _behavior.Handle(command, _nextMock.Object, CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>();

            _loggerMock.Verify(
                x => x.Log(
                    It.Is<LogLevel>(level => level == LogLevel.Error),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Request failed")),
                    exception,
                    It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
                Times.Once);
        }

        [Test]
        public async Task Handle_WithLongRunningRequest_ShouldLogWarning()
        {
            var command = new TestRequest { Value = "test" };
            var result = Result<string>.Success("Success");

            _nextMock
                .Setup(x => x(_ctx))
                .Returns(async () =>
                {
                    await Task.Delay(1000);
                    return result;
                });

            var actualResult = await _behavior.Handle(command, _nextMock.Object, CancellationToken.None);

            actualResult.Should().Be(result);

            _loggerMock.Verify(
                x => x.Log(
                    It.Is<LogLevel>(level => level == LogLevel.Warning),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Long running request")),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
                Times.Once);
        }

        [Test]
        public async Task Handle_ShouldLogRequestDetails()
        {
            var command = new TestRequest { Value = "test-value" };
            var result = Result<string>.Success("Success");

            _nextMock
                .Setup(x => x(_ctx))
                .ReturnsAsync(result);

            await _behavior.Handle(command, _nextMock.Object, CancellationToken.None);

            _loggerMock.Verify(
                x => x.Log(
                    It.Is<LogLevel>(level => level == LogLevel.Information),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("TestRequest")),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
                Times.AtLeastOnce);
        }

        [Test]
        public async Task Handle_WithFailureResult_ShouldLogWarning()
        {
            var command = new TestRequest { Value = "test" };
            var result = Result<string>.Failure("Operation failed");

            _nextMock
                .Setup(x => x(_ctx))
                .ReturnsAsync(result);

            var actualResult = await _behavior.Handle(command, _nextMock.Object, CancellationToken.None);

            actualResult.Should().Be(result);

            _loggerMock.Verify(
                x => x.Log(
                    It.Is<LogLevel>(level => level == LogLevel.Warning),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Request completed with failure")),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
                Times.Once);
        }

        [Test]
        public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
        {
            Action act = () => new LoggingBehavior<TestRequest, Result<string>>(null!);

            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("logger");
        }

        // Use TestRequest to avoid NUnit naming conflicts
        private class TestRequest : IRequest<Result<string>>
        {
            public string Value { get; set; } = string.Empty;
        }
    }
}