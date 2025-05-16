// Location.Core.Application/Events/ErrorOccurredEvent.cs
using System;

namespace Location.Core.Application.Events
{
    public class ErrorOccurredEvent
    {
        public string Message { get; }
        public string Source { get; }
        public DateTime Timestamp { get; }

        public ErrorOccurredEvent(string message, string source)
        {
            Message = message ?? throw new ArgumentNullException(nameof(message));
            Source = source ?? throw new ArgumentNullException(nameof(source));
            Timestamp = DateTime.UtcNow;
        }
    }
}