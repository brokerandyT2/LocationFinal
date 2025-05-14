using Location.Core.Application.Common.Interfaces;
using Location.Core.Domain.Common;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Core.Infrastructure.Events
{
    public class InMemoryEventBus : IEventBus
    {
        private readonly ILogger<InMemoryEventBus> _logger;
        private readonly Dictionary<Type, List<object>> _eventHandlers = new();
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        public InMemoryEventBus(ILogger<InMemoryEventBus> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default)
            where TEvent : DomainEvent
        {
            if (domainEvent == null)
                throw new ArgumentNullException(nameof(domainEvent));

            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                var eventType = domainEvent.GetType();
                _logger.LogInformation("Publishing event {EventType}", eventType.Name);

                if (_eventHandlers.TryGetValue(eventType, out var handlers))
                {
                    foreach (var handler in handlers.ToList())
                    {
                        try
                        {
                            if (handler is IEventHandler<TEvent> typedHandler)
                            {
                                await typedHandler.HandleAsync(domainEvent, cancellationToken);
                                _logger.LogDebug("Event {EventType} handled by {HandlerType}",
                                    eventType.Name, handler.GetType().Name);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error handling event {EventType} with handler {HandlerType}",
                                eventType.Name, handler.GetType().Name);
                            // Continue processing other handlers even if one fails
                        }
                    }
                }
                else
                {
                    _logger.LogDebug("No handlers registered for event {EventType}", eventType.Name);
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public void Subscribe<TEvent>(IEventHandler<TEvent> handler) where TEvent : DomainEvent
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            _semaphore.Wait();
            try
            {
                var eventType = typeof(TEvent);

                if (!_eventHandlers.ContainsKey(eventType))
                {
                    _eventHandlers[eventType] = new List<object>();
                }

                _eventHandlers[eventType].Add(handler);
                _logger.LogInformation("Subscribed handler {HandlerType} to event {EventType}",
                    handler.GetType().Name, eventType.Name);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public void Unsubscribe<TEvent>(IEventHandler<TEvent> handler) where TEvent : DomainEvent
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            _semaphore.Wait();
            try
            {
                var eventType = typeof(TEvent);

                if (_eventHandlers.TryGetValue(eventType, out var handlers))
                {
                    handlers.Remove(handler);

                    if (handlers.Count == 0)
                    {
                        _eventHandlers.Remove(eventType);
                    }

                    _logger.LogInformation("Unsubscribed handler {HandlerType} from event {EventType}",
                        handler.GetType().Name, eventType.Name);
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task PublishAllAsync(IEnumerable<DomainEvent> domainEvents, CancellationToken cancellationToken = default)
        {
            if (domainEvents == null)
                throw new ArgumentNullException(nameof(domainEvents));

            foreach (var domainEvent in domainEvents)
            {
                await PublishAsync((dynamic)domainEvent, cancellationToken);
            }
        }

        public void Dispose()
        {
            _semaphore?.Dispose();
        }
    }
}