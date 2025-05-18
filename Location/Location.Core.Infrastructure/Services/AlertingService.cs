using Location.Core.Application.Alerts;
using Location.Core.Application.Services;
using MediatR;
using System.Threading.Tasks;

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