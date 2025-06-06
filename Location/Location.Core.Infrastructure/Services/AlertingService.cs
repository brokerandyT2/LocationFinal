using Location.Core.Application.Alerts;
using Location.Core.Application.Services;
using MediatR;

namespace Location.Core.Infrastructure.Services
{
    /*Additional Recommendation
Consider refactoring your alert system in the future to avoid the need for circular dependencies. For example:

Use a dedicated event bus for alerts instead of MediatR.
Use a command pattern instead of services directly calling each other.
Consider using a mediator pattern with explicit routing of alert messages.

These architectural patterns can make your code more maintainable and less prone to circular dependencies.*/
    public class AlertingService : IAlertService
    {
        private readonly IMediator _mediator;
        private bool _isHandlingAlert = false;

        public AlertingService(IMediator mediator)
        {
            _mediator = mediator;
        }
        /// <summary>
        /// Displays an informational alert with the specified message and title.
        /// </summary>
        /// <remarks>This method ensures that only one alert is handled at a time. If an alert is already
        /// being handled,  subsequent calls to this method will return immediately without displaying a new
        /// alert.</remarks>
        /// <param name="message">The message to display in the alert. This parameter cannot be null or empty.</param>
        /// <param name="title">The title of the alert. Defaults to "Information" if not specified.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task ShowInfoAlertAsync(string message, string title = "Information")
        {
            if (_isHandlingAlert)
                return;

            try
            {
                _isHandlingAlert = true;
                await _mediator.Publish(new AlertEvent(message, title, AlertType.Info));
            }
            finally
            {
                _isHandlingAlert = false;
            }
        }
        /// <summary>
        /// Displays a success alert with the specified message and title.
        /// </summary>
        /// <remarks>This method ensures that only one alert is handled at a time. If an alert is already
        /// being handled, the method will return without displaying a new alert.</remarks>
        /// <param name="message">The message to display in the alert. This parameter cannot be null or empty.</param>
        /// <param name="title">The title of the alert. Defaults to <see langword="Success"/> if not specified.</param>
        /// <returns>A task that represents the asynchronous operation of displaying the alert.</returns>
        public async Task ShowSuccessAlertAsync(string message, string title = "Success")
        {
            if (_isHandlingAlert)
                return;

            try
            {
                _isHandlingAlert = true;
                await _mediator.Publish(new AlertEvent(message, title, AlertType.Success));
            }
            finally
            {
                _isHandlingAlert = false;
            }
        }
        /// <summary>
        /// Displays a warning alert with the specified message and title.
        /// </summary>
        /// <remarks>This method ensures that only one alert is handled at a time. If an alert is already
        /// being handled,  subsequent calls to this method will be ignored until the current alert is
        /// completed.</remarks>
        /// <param name="message">The message to display in the warning alert. Cannot be null or empty.</param>
        /// <param name="title">The title of the warning alert. Defaults to "Warning" if not specified.</param>
        /// <returns></returns>
        public async Task ShowWarningAlertAsync(string message, string title = "Warning")
        {
            if (_isHandlingAlert)
                return;

            try
            {
                _isHandlingAlert = true;
                await _mediator.Publish(new AlertEvent(message, title, AlertType.Warning));
            }
            finally
            {
                _isHandlingAlert = false;
            }
        }
        /// <summary>
        /// Displays an error alert with the specified message and title.
        /// </summary>
        /// <remarks>This method ensures that only one alert is displayed at a time. If an alert is
        /// already being handled,  subsequent calls to this method will be ignored until the current alert is
        /// completed.</remarks>
        /// <param name="message">The error message to display in the alert. This parameter cannot be null or empty.</param>
        /// <param name="title">The title of the alert. Defaults to <see langword="Error"/> if not specified.</param>
        /// <returns>A task that represents the asynchronous operation of displaying the alert.</returns>
        public async Task ShowErrorAlertAsync(string message, string title = "Error")
        {
            if (_isHandlingAlert)
                return;

            try
            {
                _isHandlingAlert = true;
                await _mediator.Publish(new AlertEvent(message, title, AlertType.Error));
            }
            finally
            {
                _isHandlingAlert = false;
            }
        }
    }
}