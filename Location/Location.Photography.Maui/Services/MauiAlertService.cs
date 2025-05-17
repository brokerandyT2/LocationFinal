// Location.Core.Maui/Services/MauiAlertService.cs
using Location.Core.Application.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Location.Core.Maui.Services
{
    /// <summary>
    /// MAUI implementation of the IAlertService interface
    /// </summary>
    public class MauiAlertService : IAlertService
    {
        private readonly ILogger<MauiAlertService> _logger;

        public MauiAlertService(ILogger<MauiAlertService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Shows an informational alert to the user
        /// </summary>
        public async Task ShowInfoAlertAsync(string message, string title = "Information")
        {
            _logger.LogInformation("Info Alert: {Title} - {Message}", title, message);
            await ShowAlertAsync(title, message, "OK");
        }

        /// <summary>
        /// Shows a success alert to the user
        /// </summary>
        public async Task ShowSuccessAlertAsync(string message, string title = "Success")
        {
            _logger.LogInformation("Success Alert: {Title} - {Message}", title, message);
            await ShowAlertAsync(title, message, "OK");
        }

        /// <summary>
        /// Shows a warning alert to the user
        /// </summary>
        public async Task ShowWarningAlertAsync(string message, string title = "Warning")
        {
            _logger.LogWarning("Warning Alert: {Title} - {Message}", title, message);
            await ShowAlertAsync(title, message, "OK");
        }

        /// <summary>
        /// Shows an error alert to the user
        /// </summary>
        public async Task ShowErrorAlertAsync(string message, string title = "Error")
        {
            _logger.LogError("Error Alert: {Title} - {Message}", title, message);
            await ShowAlertAsync(title, message, "OK");
        }

        /// <summary>
        /// Helper method to show alerts on the main thread
        /// </summary>
        private async Task ShowAlertAsync(string title, string message, string buttonText)
        {
            // Ensure we're on the main thread for UI operations
            await Microsoft.Maui.ApplicationModel.MainThread.InvokeOnMainThreadAsync(async () =>
            {
                try
                {
                    // Get the current page to display the alert
                    var currentPage = GetCurrentPage();
                    if (currentPage != null)
                    {
                        await currentPage.DisplayAlert(title, message, buttonText);
                    }
                    else
                    {
                        // If no page is available, use the Application's main page
                        if (Microsoft.Maui.Controls.Application.Current?.MainPage != null)
                        {
                            await Microsoft.Maui.Controls.Application.Current.MainPage.DisplayAlert(title, message, buttonText);
                        }
                        else
                        {
                            // Log if we can't display the alert
                            _logger.LogWarning("Could not display alert: No active page found");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error displaying alert");
                }
            });
        }

        /// <summary>
        /// Gets the current active page to display alerts on
        /// </summary>
        private Microsoft.Maui.Controls.Page GetCurrentPage()
        {
            // Navigate to the current page
            if (Microsoft.Maui.Controls.Application.Current?.MainPage == null)
                return null;

            var mainPage =Microsoft.Maui.Controls.Application.Current.MainPage;

            // Handle different navigation scenarios
            if (mainPage is Microsoft.Maui.Controls.Shell shell)
            {
                return shell.CurrentPage;
            }
            else if (mainPage is Microsoft.Maui.Controls.NavigationPage navPage)
            {
                return navPage.CurrentPage;
            }
            else if (mainPage is Microsoft.Maui.Controls.TabbedPage tabbedPage)
            {
                return tabbedPage.CurrentPage;
            }
            else if (mainPage is Microsoft.Maui.Controls.FlyoutPage flyoutPage)
            {
                return flyoutPage.Detail;
            }

            return mainPage;
        }
    }
}