using Location.Core.Application.Services;
using Location.Core.Domain.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Core.Application.Common.Interfaces
{
    /// <summary>
    /// Interface for publishing and subscribing to events
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

        /// <summary>
        /// Publishes an event
        /// </summary>
        /// <typeparam name="TEvent">The type of the event</typeparam>
        /// <param name="event">The event to publish</param>
        Task PublishAsync<TEvent>(TEvent @event) where TEvent : class;

        /// <summary>
        /// Subscribes a handler to an event
        /// </summary>
        /// <typeparam name="TEvent">The type of the event</typeparam>
        /// <param name="handler">The handler to subscribe</param>
        Task SubscribeAsync<TEvent>(IEventHandler<TEvent> handler) where TEvent : class;

        /// <summary>
        /// Unsubscribes a handler from an event
        /// </summary>
        /// <typeparam name="TEvent">The type of the event</typeparam>
        /// <param name="handler">The handler to unsubscribe</param>
        Task UnsubscribeAsync<TEvent>(IEventHandler<TEvent> handler) where TEvent : class;
    }
}