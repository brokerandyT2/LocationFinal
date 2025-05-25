using System;
using System.Collections.Generic;
using Location.Core.Application.Events.Errors;

namespace Location.Core.Application.Services
{
    /// <summary>
    /// Service interface for displaying aggregated error messages to the user
    /// </summary>
    public interface IErrorDisplayService
    {
        /// <summary>
        /// Event raised when aggregated errors are ready to be displayed
        /// </summary>
        event EventHandler<ErrorDisplayEventArgs> ErrorsReady;

        /// <summary>
        /// Manually trigger error display (for testing)
        /// </summary>
        Task TriggerErrorDisplayAsync(List<DomainErrorEvent> errors);
    }

    /// <summary>
    /// Event arguments for error display events
    /// </summary>
    public class ErrorDisplayEventArgs : EventArgs
    {
        /// <summary>
        /// The aggregated errors to display
        /// </summary>
        public List<DomainErrorEvent> Errors { get; }

        /// <summary>
        /// Whether this is a single error or multiple errors
        /// </summary>
        public bool IsSingleError => Errors.Count == 1;

        /// <summary>
        /// The localized message to display to the user
        /// </summary>
        public string DisplayMessage { get; }

        public ErrorDisplayEventArgs(List<DomainErrorEvent> errors, string displayMessage)
        {
            Errors = errors ?? throw new ArgumentNullException(nameof(errors));
            DisplayMessage = displayMessage ?? throw new ArgumentNullException(nameof(displayMessage));
        }
    }
}