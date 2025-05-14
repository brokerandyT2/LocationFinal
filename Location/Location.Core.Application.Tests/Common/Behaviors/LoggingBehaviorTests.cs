using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Location.Core.Application.Common.Behaviors;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;

namespace Location.Core.Application.Tests.Common.Behaviors
{
    [TestFixture]
    public class LoggingBehaviorTests
    {
        private Mock<ILogger<LoggingBehavior<TestRequest, Result>>> _loggerMock;
        private LoggingBehavior<TestRequest, Result> _behavior;
        CancellationToken _ctx;
        [SetUp]
        public void SetUp()
        {
            _loggerMock = new Mock<ILogger<LoggingBehavior<TestRequest, Result>>>();
            _behavior = new LoggingBehavior<TestRequest, Result>(_loggerMock.Object);
            _ctx = new CancellationToken();
        }

        [Test]
        public async Task Handle_ShouldLogRequestDetails()
        {
            // Arrange
            var request = new TestRequest();
            var response = Result.Success();
            RequestHandlerDelegate<Result> next = (_ctx) => Task.FromResult(response);

            // Act
            await _behavior.Handle(request, next, CancellationToken.None);

            // Assert - Use the correct way to verify ILogger calls
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Starting request")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.AtLeastOnce);
        }

        [Test]
        public async Task Handle_WithException_ShouldLogError()
        {
            // Arrange
            var request = new TestRequest();
            var exception = new InvalidOperationException("Test exception");
            RequestHandlerDelegate<Result> next = (_ctx) => throw exception;

            // Act & Assert
            await _behavior.Invoking(b => b.Handle(request, next, CancellationToken.None))
                .Should().ThrowAsync<InvalidOperationException>();

            // Verify error logging
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Request failed")),
                    exception,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Test]
        public async Task Handle_WithLongRunningRequest_ShouldLogWarning()
        {
            // Arrange
            var request = new TestRequest();
            var response = Result.Success();
            RequestHandlerDelegate<Result> next = async (_ctx) =>
            {
                await Task.Delay(600); // Simulate long running request
                return response;
            };

            // Act
            await _behavior.Handle(request, next, CancellationToken.None);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Long running request")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        // Make test classes public for Moq
        public class TestRequest : IRequest<Result>
        {
            public string Name { get; set; } = "Test";
        }
    }
}