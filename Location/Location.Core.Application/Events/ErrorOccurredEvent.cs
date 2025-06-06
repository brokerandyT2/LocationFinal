// Location.Core.Application/Events/ErrorOccurredEvent.cs
namespace Location.Core.Application.Events
{
    /// <summary>
    /// Represents an event that occurs when an error is encountered, providing details about the error.
    /// </summary>
    /// <remarks>This class encapsulates information about an error, including a descriptive message, the
    /// source of the error,  and the timestamp when the error occurred. It is typically used to log or propagate error
    /// details in an application.</remarks>
    public class ErrorOccurredEvent
    {
        public string Message { get; }
        public string Source { get; }
        public DateTime Timestamp { get; }
        /// <summary>
        /// Represents an event that occurs when an error is encountered, providing details about the error.
        /// </summary>
        /// <param name="message">The error message describing the nature of the error. Cannot be <see langword="null"/>.</param>
        /// <param name="source">The source or origin of the error. Cannot be <see langword="null"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="message"/> or <paramref name="source"/> is <see langword="null"/>.</exception>
        public ErrorOccurredEvent(string message, string source)
        {
            Message = message ?? throw new ArgumentNullException(nameof(message));
            Source = source ?? throw new ArgumentNullException(nameof(source));
            Timestamp = DateTime.UtcNow;
        }
    }
}