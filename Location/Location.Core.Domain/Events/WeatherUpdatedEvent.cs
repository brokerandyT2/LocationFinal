using System;
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

        public WeatherUpdatedEvent(int locationId, DateTime updateTime)
        {
            LocationId = locationId;
            UpdateTime = updateTime;
        }
    }
}