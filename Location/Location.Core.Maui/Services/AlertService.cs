using Location.Core.Application.Services;
using Microsoft.Maui.Controls;

namespace Location.Core.Maui.Services
{
    public class AlertService : IAlertService
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
    }
}