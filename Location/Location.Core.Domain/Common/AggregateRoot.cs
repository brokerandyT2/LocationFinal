using Location.Core.Domain.Interfaces;
using System.Collections.Generic;

namespace Location.Core.Domain.Common
{
/// <summary>
/// Represents the base class for aggregate roots in a domain-driven design (DDD) context.
/// </summary>
/// <remarks>An aggregate root is the entry point to an aggregate, which is a cluster of domain objects that are
/// treated as a single unit. This class provides functionality for managing domain events associated with the aggregate
/// root.</remarks>
    public abstract class AggregateRoot : Entity, IAggregateRoot
    {
        private readonly List<IDomainEvent> _domainEvents = new();
        /// <summary>
        /// Gets the collection of domain events associated with the current entity.
        /// </summary>
        /// <remarks>Domain events represent significant occurrences within the entity that may trigger
        /// side effects or be handled by external systems. This property provides a read-only view of the events for
        /// external consumers.</remarks>
        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
        /// <summary>
        /// Adds a domain event to the collection of events associated with the entity.
        /// </summary>
        /// <param name="eventItem">The domain event to add. Cannot be <see langword="null"/>.</param>
        public void AddDomainEvent(IDomainEvent eventItem)
        {
            _domainEvents.Add(eventItem);
        }
        /// <summary>
        /// Removes a specified domain event from the collection of domain events.
        /// </summary>
        /// <param name="eventItem">The domain event to remove. Must not be <see langword="null"/>.</param>
        public void RemoveDomainEvent(IDomainEvent eventItem)
        {
            _domainEvents.Remove(eventItem);
        }
        /// <summary>
        /// Clears all domain events associated with the current entity.
        /// </summary>
        /// <remarks>This method removes all events from the internal collection of domain events.  It is
        /// typically used after the events have been processed or dispatched.</remarks>
        public void ClearDomainEvents()
        {
            _domainEvents.Clear();
        }
    }
}