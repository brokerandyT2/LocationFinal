// Location.Core.Application/Services/IAlertingService.cs
using Location.Core.Application.Resources;

namespace Location.Core.Application.Services
{
    public interface IAlertService
    {
        Task ShowInfoAlertAsync(string message, string title = null);
        Task ShowSuccessAlertAsync(string message, string title = null);
        Task ShowWarningAlertAsync(string message, string title = null);
        Task ShowErrorAlertAsync(string message, string title = null);
    }

    public static class AlertServiceExtensions
    {
        public static Task ShowInfoAlertAsync(this IAlertService service, string message, string title = null)
        {
            return service.ShowInfoAlertAsync(message, title ?? AppResources.Alert_Information);
        }

        public static Task ShowSuccessAlertAsync(this IAlertService service, string message, string title = null)
        {
            return service.ShowSuccessAlertAsync(message, title ?? AppResources.Alert_Success);
        }

        public static Task ShowWarningAlertAsync(this IAlertService service, string message, string title = null)
        {
            return service.ShowWarningAlertAsync(message, title ?? AppResources.Alert_Warning);
        }

        public static Task ShowErrorAlertAsync(this IAlertService service, string message, string title = null)
        {
            return service.ShowErrorAlertAsync(message, title ?? AppResources.Alert_Error);
        }
    }
}