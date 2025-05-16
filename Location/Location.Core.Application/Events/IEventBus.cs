// Location.Core.Application/Services/IEventBus.cs
using System;
using System.Threading.Tasks;

namespace Location.Core.Application.Services
{
    public interface IEventBus
    {
        Task PublishAsync<TEvent>(TEvent @event) where TEvent : class;
        Task SubscribeAsync<TEvent>(IEventHandler<TEvent> handler) where TEvent : class;
        Task UnsubscribeAsync<TEvent>(IEventHandler<TEvent> handler) where TEvent : class;
    }
}