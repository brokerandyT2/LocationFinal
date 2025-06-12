// Location.Core.Application/Alerts/AlertEvent.cs
using Location.Core.Application.Resources;
using MediatR;

namespace Location.Core.Application.Alerts
{
    public class AlertEvent : INotification
    {
        public string Title { get; }
        public string Message { get; }
        public AlertType Type { get; }
        /// <summary>
        /// // Initializes a new instance of the <see cref="AlertEvent"/> class with the specified message, title, and type.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="title"></param>
        /// <param name="type"></param>
        public AlertEvent(string message, string title = "Alert", AlertType type = AlertType.Info)
        {

          
            Message = message;
            Title = title ?? AppResources.Alert_DefaultTitle; 
            Type = type;
        }
    }
    /// <summary>
    /// Represents the type or severity of an alert message.
    /// </summary>
    /// <remarks>This enumeration is typically used to categorize alert messages in a user interface or
    /// logging system. Each value corresponds to a specific level of importance or context for the alert.</remarks>
    public enum AlertType
    {
        Info,
        Success,
        Warning,
        Error
    }
}