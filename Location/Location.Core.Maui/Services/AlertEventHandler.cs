// Location.Core.Maui/Services/AlertEventHandler.cs
using Location.Core.Application.Alerts;
using Location.Core.Application.Services;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Core.Maui.Services
{
    public class AlertEventHandler : INotificationHandler<AlertEvent>
    {
        private readonly IAlertService _alertService;

        public AlertEventHandler(IAlertService alertService)
        {
            _alertService = alertService;
        }

        public async Task Handle(AlertEvent notification, CancellationToken cancellationToken)
        {
            switch (notification.Type)
            {
                case AlertType.Success:
                    await _alertService.ShowSuccessAlertAsync(notification.Message, notification.Title);
                    break;
                case AlertType.Warning:
                    await _alertService.ShowWarningAlertAsync(notification.Message, notification.Title);
                    break;
                case AlertType.Error:
                    await _alertService.ShowErrorAlertAsync(notification.Message, notification.Title);
                    break;
                case AlertType.Info:
                default:
                    await _alertService.ShowInfoAlertAsync(notification.Message, notification.Title);
                    break;
            }
        }
    }
}