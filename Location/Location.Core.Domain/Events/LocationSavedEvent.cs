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
        /// <summary>
        /// Initializes a new instance of the <see cref="LocationSavedEvent"/> class with the specified location.
        /// </summary>
        /// <param name="location">The location associated with the event. Cannot be <see langword="null"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="location"/> is <see langword="null"/>.</exception>
        public LocationSavedEvent(Entities.Location location)
        {
            Location = location ?? throw new ArgumentNullException(nameof(location));
        }
    }
}