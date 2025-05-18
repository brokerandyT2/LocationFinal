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
        // Add a type check to ensure we're using MauiAlertService
        private readonly bool _isSafeAlertService;

        public AlertEventHandler(IAlertService alertService)
        {
            _alertService = alertService;
            // Check if the implementation is safe (not AlertingService)
            _isSafeAlertService = alertService.GetType().Name != "AlertingService";
        }

        public async Task Handle(AlertEvent notification, CancellationToken cancellationToken)
        {
            // Skip handling if we detect we're using AlertingService to prevent circular references
            if (!_isSafeAlertService)
            {
                // Log or handle the situation (optional)
                return;
            }

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