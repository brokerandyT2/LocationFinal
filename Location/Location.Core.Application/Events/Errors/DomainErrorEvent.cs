using MediatR;
using System;
using System.Collections.Generic;

namespace Location.Core.Application.Events.Errors
{
    /// <summary>
    /// Base class for all domain-specific error events that can be published through MediatR
    /// </summary>
    /// <remarks>
    /// This abstract class provides the foundation for type-safe error handling across the application.
    /// Each concrete implementation represents a specific error scenario with rich domain context
    /// and localization support through resource keys.
    /// </remarks>
    public abstract class DomainErrorEvent : INotification
    {
        /// <summary>
        /// Unique identifier for this error occurrence
        /// </summary>
        public Guid ErrorId { get; }

        /// <summary>
        /// When this error occurred
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// The source operation or handler that generated this error
        /// </summary>
        public string Source { get; }

        /// <summary>
        /// Initializes a new domain error event
        /// </summary>
        /// <param name="source">The source operation or handler generating this error</param>
        protected DomainErrorEvent(string source)
        {
            ErrorId = Guid.NewGuid();
            Timestamp = DateTime.UtcNow;
            Source = source ?? throw new ArgumentNullException(nameof(source));
        }

        /// <summary>
        /// Gets the resource key for localized error message
        /// </summary>
        /// <returns>Resource key that maps to AppResources entry</returns>
        public abstract string GetResourceKey();

        /// <summary>
        /// Gets parameters for message formatting
        /// </summary>
        /// <returns>Dictionary of parameter names and values for message substitution</returns>
        public virtual Dictionary<string, object> GetParameters()
        {
            return new Dictionary<string, object>();
        }

        /// <summary>
        /// Gets the error severity level
        /// </summary>
        public virtual ErrorSeverity Severity => ErrorSeverity.Error;
    }

    /// <summary>
    /// Defines the severity levels for error events
    /// </summary>
    public enum ErrorSeverity
    {
        Info,
        Warning,
        Error,
        Critical
    }
}