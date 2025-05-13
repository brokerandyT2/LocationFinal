using System;
using Location.Core.Domain.Common;

namespace Location.Core.Domain.Events
{
    /// <summary>
    /// Domain event raised when a photo is attached to a location
    /// </summary>
    public class PhotoAttachedEvent : DomainEvent
    {
        public int LocationId { get; }
        public string PhotoPath { get; }

        public PhotoAttachedEvent(int locationId, string photoPath)
        {
            LocationId = locationId;
            PhotoPath = photoPath ?? throw new ArgumentNullException(nameof(photoPath));
        }
    }
}