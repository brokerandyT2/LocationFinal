using Location.Core.Domain.Common;

namespace Location.Core.Domain.Events
{
    /// <summary>
    /// Domain event raised when weather data is updated
    /// </summary>
    public class WeatherUpdatedEvent : DomainEvent
    {
        public int LocationId { get; }
        public DateTime UpdateTime { get; }
        /// <summary>
        /// Initializes a new instance of the <see cref="WeatherUpdatedEvent"/> class with the specified location ID and
        /// update time.
        /// </summary>
        /// <param name="locationId">The unique identifier of the location where the weather update occurred.</param>
        /// <param name="updateTime">The date and time when the weather update was recorded.</param>
        public WeatherUpdatedEvent(int locationId, DateTime updateTime)
        {
            LocationId = locationId;
            UpdateTime = updateTime;
        }
    }
}