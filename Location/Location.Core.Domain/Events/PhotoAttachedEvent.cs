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
        /// <summary>
        /// Initializes a new instance of the <see cref="PhotoAttachedEvent"/> class, representing an event where a
        /// photo is attached to a specific location.
        /// </summary>
        /// <param name="locationId">The unique identifier of the location to which the photo is attached.</param>
        /// <param name="photoPath">The file path of the attached photo. Cannot be <see langword="null"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="photoPath"/> is <see langword="null"/>.</exception>
        public PhotoAttachedEvent(int locationId, string photoPath)
        {
            LocationId = locationId;
            PhotoPath = photoPath ?? throw new ArgumentNullException(nameof(photoPath));
        }
    }
}