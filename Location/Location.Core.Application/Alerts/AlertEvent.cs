// Location.Core.Application/Alerts/AlertEvent.cs
using MediatR;

namespace Location.Core.Application.Alerts
{
    public class AlertEvent : INotification
    {
        public string Title { get; }
        public string Message { get; }
        public AlertType Type { get; }

        public AlertEvent(string message, string title = "Alert", AlertType type = AlertType.Info)
        {
            Message = message;
            Title = title;
            Type = type;
        }
    }

    public enum AlertType
    {
        Info,
        Success,
        Warning,
        Error
    }
}