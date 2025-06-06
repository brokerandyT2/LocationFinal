// Location.Core.Application/Services/IAlertingService.cs
namespace Location.Core.Application.Services
{
    public interface IAlertService
    {
        Task ShowInfoAlertAsync(string message, string title = "Information");
        Task ShowSuccessAlertAsync(string message, string title = "Success");
        Task ShowWarningAlertAsync(string message, string title = "Warning");
        Task ShowErrorAlertAsync(string message, string title = "Error");
    }
}