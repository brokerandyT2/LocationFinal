// Location.Core.Infrastructure/Services/AlertingService.cs
using Location.Core.Application.Alerts;
using Location.Core.Application.Services;
using MediatR;
using System.Threading.Tasks;

namespace Location.Core.Infrastructure.Services
{
    public class AlertingService : IAlertService
    {
        private readonly IMediator _mediator;

        public AlertingService(IMediator mediator)
        {
            _mediator = mediator;
        }
        private bool _isHandlingAlert = false; // Add this flag
        public async Task ShowInfoAlertAsync(string message, string title = "Information")
        {
            if (_isHandlingAlert)
            { return; }

            if (!_isHandlingAlert)
            {
                try
                {
                    await _mediator.Publish(new AlertEvent(message, title, AlertType.Info));
                    _isHandlingAlert = true;
                }
                finally
                { _isHandlingAlert = false; }
            }
        }

        public async Task ShowSuccessAlertAsync(string message, string title = "Success")
        {
            if (_isHandlingAlert)
            { return; }

            if (!_isHandlingAlert)
            {
                try
                {
                    await _mediator.Publish(new AlertEvent(message, title, AlertType.Success));
                    _isHandlingAlert = true;
                }
                finally
                { _isHandlingAlert = false; }
            }
        }

        public async Task ShowWarningAlertAsync(string message, string title = "Warning")
        {
            if (_isHandlingAlert)
            { return; }

            if (!_isHandlingAlert)
            {
                try
                {
                    await _mediator.Publish(new AlertEvent(message, title, AlertType.Warning));
                    _isHandlingAlert = true;
                }
                finally
                { _isHandlingAlert = false; }
            }
        }

        public async Task ShowErrorAlertAsync(string message, string title = "Error")
        {
            if(_isHandlingAlert)
            { return; }

            if (!_isHandlingAlert)
            {
                try
                {_isHandlingAlert = true;
                    await _mediator.Publish(new AlertEvent(message, title, AlertType.Error));
                    
                }
                finally
                { _isHandlingAlert = false; }
            }
        }
    }
}