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

        public LocationDeletedEvent(int locationId)
        {
            LocationId = locationId;
        }
    }
}