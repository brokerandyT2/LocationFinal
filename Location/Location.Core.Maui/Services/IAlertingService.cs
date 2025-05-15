// Location.Core.Application/Services/IAlertingService.cs
using Location.Core.Application.Alerts;
using System.Threading.Tasks;

namespace Location.Core.Application.Services
{
    public interface IAlertingService
    {
        Task ShowInfoAlertAsync(string message, string title = "Information");
        Task ShowSuccessAlertAsync(string message, string title = "Success");
        Task ShowWarningAlertAsync(string message, string title = "Warning");
        Task ShowErrorAlertAsync(string message, string title = "Error");
    }
}