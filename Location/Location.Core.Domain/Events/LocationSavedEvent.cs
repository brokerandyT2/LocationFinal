using System;
using Location.Core.Domain.Common;

namespace Location.Core.Domain.Events
{
    /// <summary>
    /// Domain event raised when a location is saved
    /// </summary>
    public class LocationSavedEvent : DomainEvent
    {
        public Entities.Location Location { get; }

        public LocationSavedEvent(Entities.Location location)
        {
            Location = location ?? throw new ArgumentNullException(nameof(location));
        }
    }
}