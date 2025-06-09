using FluentAssertions;
using Location.Core.Domain.Common;
using Location.Core.Domain.Interfaces;
using NUnit.Framework;

namespace Location.Core.Domain.Tests.Common
{
    [TestFixture]
    public class AggregateRootTests
    {
        private class TestAggregate : AggregateRoot
        {
            public TestAggregate(int id)
            {
                Id = id;
            }
        }

        private class TestDomainEvent : DomainEvent
        {
            public string TestProperty { get; }

            public TestDomainEvent(string testProperty)
            {
                TestProperty = testProperty;
            }
        }

        [Test]
        public void AddDomainEvent_ShouldAddEventToCollection()
        {
            // Arrange
            var aggregate = new TestAggregate(1);
            var domainEvent = new TestDomainEvent("test");

            // Act
            aggregate.AddDomainEvent(domainEvent);

            // Assert
            aggregate.DomainEvents.Should().ContainSingle();
            aggregate.DomainEvents.Should().Contain(domainEvent);
        }

        [Test]
        public void RemoveDomainEvent_ShouldRemoveEventFromCollection()
        {
            // Arrange
            var aggregate = new TestAggregate(1);
            var domainEvent = new TestDomainEvent("test");
            aggregate.AddDomainEvent(domainEvent);

            // Act
            aggregate.RemoveDomainEvent(domainEvent);

            // Assert
            aggregate.DomainEvents.Should().BeEmpty();
        }

        [Test]
        public void ClearDomainEvents_ShouldRemoveAllEvents()
        {
            // Arrange
            var aggregate = new TestAggregate(1);
            aggregate.AddDomainEvent(new TestDomainEvent("test1"));
            aggregate.AddDomainEvent(new TestDomainEvent("test2"));
            aggregate.AddDomainEvent(new TestDomainEvent("test3"));

            // Act
            aggregate.ClearDomainEvents();

            // Assert
            aggregate.DomainEvents.Should().BeEmpty();
        }

        [Test]
        public void DomainEvents_ShouldReturnReadOnlyCollection()
        {
            // Arrange
            var aggregate = new TestAggregate(1);
            var domainEvent = new TestDomainEvent("test");
            aggregate.AddDomainEvent(domainEvent);

            // Act
            var events = aggregate.DomainEvents;

            // Assert
            events.Should().BeAssignableTo<IReadOnlyCollection<IDomainEvent>>();
            events.Should().ContainSingle();
        }
    }
}