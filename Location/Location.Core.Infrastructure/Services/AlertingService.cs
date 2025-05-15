// Location.Core.Infrastructure/Services/AlertingService.cs
using Location.Core.Application.Alerts;
using Location.Core.Application.Services;
using MediatR;
using System.Threading.Tasks;

namespace Location.Core.Infrastructure.Services
{
    public class AlertingService : IAlertingService
    {
        private readonly IMediator _mediator;

        public AlertingService(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task ShowInfoAlertAsync(string message, string title = "Information")
        {
            await _mediator.Publish(new AlertEvent(message, title, AlertType.Info));
        }

        public async Task ShowSuccessAlertAsync(string message, string title = "Success")
        {
            await _mediator.Publish(new AlertEvent(message, title, AlertType.Success));
        }

        public async Task ShowWarningAlertAsync(string message, string title = "Warning")
        {
            await _mediator.Publish(new AlertEvent(message, title, AlertType.Warning));
        }

        public async Task ShowErrorAlertAsync(string message, string title = "Error")
        {
            await _mediator.Publish(new AlertEvent(message, title, AlertType.Error));
        }
    }
}