// Location.Core.Application/Services/IEventHandler.cs
using System.Threading.Tasks;

namespace Location.Core.Application.Services
{
    public interface IEventHandler<in TEvent> where TEvent : class
    {
        Task HandleAsync(TEvent @event);
    }
}