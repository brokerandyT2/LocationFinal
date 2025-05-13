using NUnit.Framework;
using FluentAssertions;
using Location.Core.Domain.Common;
using System;

namespace Location.Core.Domain.Tests.Common
{
    [TestFixture]
    public class DomainEventTests
    {
        private class TestDomainEvent : DomainEvent
        {
            public string TestProperty { get; }

            public TestDomainEvent(string testProperty)
            {
                TestProperty = testProperty;
            }
        }

        [Test]
        public void DomainEvent_WhenCreated_ShouldSetDateOccurred()
        {
            // Arrange
            var beforeCreation = DateTimeOffset.UtcNow;

            // Act
            var domainEvent = new TestDomainEvent("test");

            // Assert
            var afterCreation = DateTimeOffset.UtcNow;
            domainEvent.DateOccurred.Should().BeOnOrAfter(beforeCreation);
            domainEvent.DateOccurred.Should().BeOnOrBefore(afterCreation);
        }

        [Test]
        public void DomainEvent_DateOccurred_ShouldBeUtc()
        {
            // Arrange & Act
            var domainEvent = new TestDomainEvent("test");

            // Assert
            domainEvent.DateOccurred.Offset.Should().Be(TimeSpan.Zero);
        }
    }
}