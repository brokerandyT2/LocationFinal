using System;
using Location.Core.Domain.Common;

namespace Location.Core.Domain.Events
{
    /// <summary>
    /// Domain event raised when a location is deleted
    /// </summary>
    public class LocationDeletedEvent : DomainEvent
    {
        public int LocationId { get; }
        /// <summary>
        /// Represents an event that is triggered when a location is deleted.
        /// </summary>
        /// <param name="locationId">The unique identifier of the deleted location.</param>
        public LocationDeletedEvent(int locationId)
        {
            LocationId = locationId;
        }
    }
}