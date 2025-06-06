using Location.Core.Application.Services;
using Microsoft.Extensions.Logging;

namespace Location.Core.Infrastructure.Services
{
    /// <summary>
    /// A simple implementation of IAlertService for infrastructure components
    /// that only logs alerts without publishing events to avoid circular references
    /// </summary>
    public class DirectAlertingService : IAlertService
    {
        private readonly ILogger<DirectAlertingService> _logger;

        public DirectAlertingService(ILogger<DirectAlertingService> logger)
        {
            _logger = logger;
        }

        public Task ShowInfoAlertAsync(string message, string title = "Information")
        {
            _logger.LogInformation("Info Alert: {Title} - {Message}", title, message);
            return Task.CompletedTask;
        }

        public Task ShowSuccessAlertAsync(string message, string title = "Success")
        {
            _logger.LogInformation("Success Alert: {Title} - {Message}", title, message);
            return Task.CompletedTask;
        }

        public Task ShowWarningAlertAsync(string message, string title = "Warning")
        {
            _logger.LogWarning("Warning Alert: {Title} - {Message}", title, message);
            return Task.CompletedTask;
        }

        public Task ShowErrorAlertAsync(string message, string title = "Error")
        {
            _logger.LogError("Error Alert: {Title} - {Message}", title, message);
            return Task.CompletedTask;
        }
    }
}