using Location.Core.Application.Services;
using Microsoft.Maui.Controls;

namespace Location.Core.Maui.Services
{
    public class AlertService : Location.Core.Application.Services.IAlertingService
    {
        public async Task DisplayAlert(string title, string message, string cancel)
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
                Microsoft.Maui.Controls.Application.Current?.MainPage?.DisplayAlert(title, message, cancel));
        }

        public async Task<bool> DisplayAlert(string title, string message, string accept, string cancel)
        {
            return await MainThread.InvokeOnMainThreadAsync(() =>
                Microsoft.Maui.Controls.Application.Current?.MainPage?.DisplayAlert(title, message, accept, cancel) ?? Task.FromResult(false));
        }

        public async Task<string> DisplayActionSheet(string title, string cancel, string destruction, params string[] buttons)
        {
            return await MainThread.InvokeOnMainThreadAsync(() =>
                Microsoft.Maui.Controls.Application.Current?.MainPage?.DisplayActionSheet(title, cancel, destruction, buttons) ?? Task.FromResult(string.Empty));
        }

        public Task ShowInfoAlertAsync(string message, string title = "Information")
        {
            throw new NotImplementedException();
        }

        public Task ShowSuccessAlertAsync(string message, string title = "Success")
        {
            throw new NotImplementedException();
        }

        public Task ShowWarningAlertAsync(string message, string title = "Warning")
        {
            throw new NotImplementedException();
        }

        public Task ShowErrorAlertAsync(string message, string title = "Error")
        {
            throw new NotImplementedException();
        }
    }
}