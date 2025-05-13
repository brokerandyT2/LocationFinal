using System.Threading;
using System.Threading.Tasks;
using Location.Core.Domain.Interfaces;

namespace Location.Core.Application.Common.Interfaces
{
    /// <summary>
    /// Interface for publishing domain events
    /// </summary>
    public interface IEventBus
    {
        /// <summary>
        /// Publishes a domain event
        /// </summary>
        /// <param name="domainEvent">The event to publish</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);

        /// <summary>
        /// Publishes multiple domain events
        /// </summary>
        /// <param name="domainEvents">The events to publish</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task PublishAllAsync(IDomainEvent[] domainEvents, CancellationToken cancellationToken = default);
    }
}