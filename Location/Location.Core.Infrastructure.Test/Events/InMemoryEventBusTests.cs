
using NUnit.Framework;
using FluentAssertions;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Domain.Interfaces;
using Location.Core.Infrastructure.Events;
using Location.Core.Infrastructure.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Core.Infrastructure.Tests.Events
{
    [TestFixture]
    public class InMemoryEventBusTests
    {
        private InMemoryEventBus _eventBus;
        private Mock<ILogger<InMemoryEventBus>> _mockLogger;

        [SetUp]
        public void Setup()
        {
            _mockLogger = new Mock<ILogger<InMemoryEventBus>>();
            _eventBus = new InMemoryEventBus(_mockLogger.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _eventBus?.Dispose();
        }

        [Test]
        public async Task PublishAsync_WithValidEvent_ShouldLogInformation()
        {
            // Arrange
            var domainEvent = new TestDomainEvent { TestData = "test" };

            // Act
            await _eventBus.PublishAsync(domainEvent);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    It.Is<LogLevel>(level => level == LogLevel.Information),
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
                Times.AtLeastOnce
            );
        }

        [Test]
        public void PublishAsync_WithNullEvent_ShouldThrowException()
        {
            // Act
            Func<Task> act = async () => await _eventBus.PublishAsync(null!);

            // Assert
            act.Should().ThrowAsync<ArgumentNullException>()
                .WithParameterName("domainEvent");
        }

        [Test]
        public async Task PublishAsync_WithSubscribedHandler_ShouldInvokeHandler()
        {
            // Arrange
            var domainEvent = new TestDomainEvent { TestData = "test" };
            var handlerInvoked = false;
            var handler = new TestEventHandler(() => handlerInvoked = true);
            _eventBus.Subscribe(typeof(TestDomainEvent), handler);

            // Act
            await _eventBus.PublishAsync(domainEvent);

            // Assert
            handlerInvoked.Should().BeTrue();
        }

        [Test]
        public async Task PublishAsync_WithMultipleHandlers_ShouldInvokeAllHandlers()
        {
            // Arrange
            var domainEvent = new TestDomainEvent { TestData = "test" };
            var handler1Invoked = false;
            var handler2Invoked = false;
            var handler1 = new TestEventHandler(() => handler1Invoked = true);
            var handler2 = new TestEventHandler(() => handler2Invoked = true);

            _eventBus.Subscribe(typeof(TestDomainEvent), handler1);
            _eventBus.Subscribe(typeof(TestDomainEvent), handler2);

            // Act
            await _eventBus.PublishAsync(domainEvent);

            // Assert
            handler1Invoked.Should().BeTrue();
            handler2Invoked.Should().BeTrue();
        }

        [Test]
        public async Task PublishAsync_WithNoHandlers_ShouldComplete()
        {
            // Arrange
            var domainEvent = new TestDomainEvent { TestData = "test" };

            // Act
            Func<Task> act = async () => await _eventBus.PublishAsync(domainEvent);

            // Assert
            await act.Should().NotThrowAsync();
            _mockLogger.Verify(
                x => x.Log(
                    It.Is<LogLevel>(level => level == LogLevel.Debug),
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
                Times.AtLeastOnce
            );
        }

        [Test]
        public async Task PublishAsync_WithHandlerException_ShouldLogErrorAndContinue()
        {
            // Arrange
            var domainEvent = new TestDomainEvent { TestData = "test" };
            var handlerInvoked = false;
            var failingHandler = new TestEventHandler(() => throw new Exception("Handler error"));
            var successfulHandler = new TestEventHandler(() => handlerInvoked = true);

            _eventBus.Subscribe(typeof(TestDomainEvent), failingHandler);
            _eventBus.Subscribe(typeof(TestDomainEvent), successfulHandler);

            // Act
            await _eventBus.PublishAsync(domainEvent);

            // Assert
            handlerInvoked.Should().BeTrue();
            _mockLogger.Verify(
                x => x.Log(
                    It.Is<LogLevel>(level => level == LogLevel.Error),
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
                Times.AtLeastOnce
            );
        }

        [Test]
        public async Task PublishAllAsync_WithMultipleEvents_ShouldPublishAll()
        {
            // Arrange
            var events = new[]
            {
                new TestDomainEvent { TestData = "event1" },
                new TestDomainEvent { TestData = "event2" },
                new TestDomainEvent { TestData = "event3" }
            };
            var eventCount = 0;
            var handler = new TestEventHandler(() => eventCount++);
            _eventBus.Subscribe(typeof(TestDomainEvent), handler);

            // Act
            await _eventBus.PublishAllAsync(events);

            // Assert
            eventCount.Should().Be(3);
        }

        [Test]
        public void PublishAllAsync_WithNullArray_ShouldThrowException()
        {
            // Act
            Func<Task> act = async () => await _eventBus.PublishAllAsync(null!);

            // Assert
            act.Should().ThrowAsync<ArgumentNullException>()
                .WithParameterName("domainEvents");
        }

        [Test]
        public void Subscribe_WithNullHandler_ShouldThrowException()
        {
            // Act
            Action act = () => _eventBus.Subscribe(typeof(TestDomainEvent), null!);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("handler");
        }

        [Test]
        public void Unsubscribe_WithNullHandler_ShouldThrowException()
        {
            // Act
            Action act = () => _eventBus.Unsubscribe(typeof(TestDomainEvent), null!);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("handler");
        }

        [Test]
        public async Task Unsubscribe_WithSubscribedHandler_ShouldRemoveHandler()
        {
            // Arrange
            var domainEvent = new TestDomainEvent { TestData = "test" };
            var handlerInvoked = false;
            var handler = new TestEventHandler(() => handlerInvoked = true);

            _eventBus.Subscribe(typeof(TestDomainEvent), handler);
            _eventBus.Unsubscribe(typeof(TestDomainEvent), handler);

            // Act
            await _eventBus.PublishAsync(domainEvent);

            // Assert
            handlerInvoked.Should().BeFalse();
        }

        [Test]
        public void Constructor_WithNullLogger_ShouldThrowException()
        {
            // Act
            Action act = () => new InMemoryEventBus(null!);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("logger");
        }

        [Test]
        public async Task PublishAsync_WithCancellationToken_ShouldRespectCancellation()
        {
            // Arrange
            var domainEvent = new TestDomainEvent { TestData = "test" };
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act
            Func<Task> act = async () => await _eventBus.PublishAsync(domainEvent, cts.Token);

            // Assert
            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        [Test]
        public async Task Subscribe_UnsubscribeMultipleTimes_ShouldHandleCorrectly()
        {
            // Arrange
            var domainEvent = new TestDomainEvent { TestData = "test" };
            var invocationCount = 0;
            var handler = new TestEventHandler(() => invocationCount++);

            // Act - Subscribe, publish, unsubscribe, publish
            _eventBus.Subscribe(typeof(TestDomainEvent), handler);
            await _eventBus.PublishAsync(domainEvent);

            _eventBus.Unsubscribe(typeof(TestDomainEvent), handler);
            await _eventBus.PublishAsync(domainEvent);

            // Assert
            invocationCount.Should().Be(1); // Should only be invoked once
        }

        // Test helper classes
        private class TestDomainEvent : IDomainEvent
        {
            public DateTimeOffset DateOccurred { get; } = DateTimeOffset.UtcNow;
            public string TestData { get; set; } = string.Empty;
        }

        private class TestEventHandler
        {
            private readonly Action _onHandle;

            public TestEventHandler(Action onHandle)
            {
                _onHandle = onHandle;
            }

            public Task HandleAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
            {
                _onHandle();
                return Task.CompletedTask;
            }
        }
    }
}