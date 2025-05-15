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
                    await _alertService.DisplayAlert(notification.Title, notification.Message, "OK");
                    break;
                case AlertType.Warning:
                    await _alertService.DisplayAlert("Warning: " + notification.Title, notification.Message, "OK");
                    break;
                case AlertType.Error:
                    await _alertService.DisplayAlert("Error: " + notification.Title, notification.Message, "OK");
                    break;
                case AlertType.Info:
                default:
                    await _alertService.DisplayAlert(notification.Title, notification.Message, "OK");
                    break;
            }
        }
    }
}