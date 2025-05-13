using Location.Core.Domain.Common;
using System.Collections.Generic;

namespace Location.Core.Domain.Interfaces
{
    /// <summary>
    /// Marker interface for aggregate roots
    /// </summary>
    public interface IAggregateRoot : IEntity
    {
        IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
        void AddDomainEvent(IDomainEvent eventItem);
        void RemoveDomainEvent(IDomainEvent eventItem);
        void ClearDomainEvents();
    }
}