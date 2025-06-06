using Location.Core.Domain.Interfaces;

namespace Location.Core.Domain.Common
{
    /// <summary>
    /// Base class for all domain events
    /// </summary>
    public abstract class DomainEvent : IDomainEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DomainEvent"/> class.
        /// </summary>
        /// <remarks>The <see cref="DomainEvent"/> constructor sets the <see cref="DateOccurred"/>
        /// property          to the current UTC date and time, indicating when the event occurred.</remarks>
        protected DomainEvent()
        {
            DateOccurred = DateTimeOffset.UtcNow;
        }

        public DateTimeOffset DateOccurred { get; protected set; }
    }
}