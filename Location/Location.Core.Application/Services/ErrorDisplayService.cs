using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Location.Core.Application.Events.Errors;
using MediatR;

namespace Location.Core.Application.Services
{
    /// <summary>
    /// Service that aggregates domain error events and provides them to the UI layer for display
    /// </summary>
    public class ErrorDisplayService : IErrorDisplayService,
        INotificationHandler<LocationSaveErrorEvent>,
        INotificationHandler<WeatherUpdateErrorEvent>,
        INotificationHandler<ValidationErrorEvent>,
        INotificationHandler<TipValidationErrorEvent>,
        INotificationHandler<SettingErrorEvent>,
        INotificationHandler<TipTypeErrorEvent>
    {
        private readonly System.Timers.Timer _errorAggregationTimer;
        private readonly List<DomainErrorEvent> _pendingErrors = new();
        private readonly object _errorLock = new();

        /// <summary>
        /// Event raised when aggregated errors are ready to be displayed
        /// </summary>
        public event EventHandler<ErrorDisplayEventArgs>? ErrorsReady;

        public ErrorDisplayService()
        {
            // Initialize error aggregation timer (500ms window)
            _errorAggregationTimer = new System.Timers.Timer(500);
            _errorAggregationTimer.Elapsed += OnErrorAggregationTimerElapsed;
            _errorAggregationTimer.AutoReset = false;
        }

        // Domain Error Event Handlers
        public Task Handle(LocationSaveErrorEvent notification, CancellationToken cancellationToken)
        {
            AddPendingError(notification);
            return Task.CompletedTask;
        }

        public Task Handle(WeatherUpdateErrorEvent notification, CancellationToken cancellationToken)
        {
            AddPendingError(notification);
            return Task.CompletedTask;
        }

        public Task Handle(ValidationErrorEvent notification, CancellationToken cancellationToken)
        {
            AddPendingError(notification);
            return Task.CompletedTask;
        }

        public Task Handle(TipValidationErrorEvent notification, CancellationToken cancellationToken)
        {
            AddPendingError(notification);
            return Task.CompletedTask;
        }

        public Task Handle(SettingErrorEvent notification, CancellationToken cancellationToken)
        {
            AddPendingError(notification);
            return Task.CompletedTask;
        }

        public Task Handle(TipTypeErrorEvent notification, CancellationToken cancellationToken)
        {
            AddPendingError(notification);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Manually trigger error display for testing purposes
        /// </summary>
        public async Task TriggerErrorDisplayAsync(List<DomainErrorEvent> errors)
        {
            if (errors == null || errors.Count == 0)
                return;

            var displayMessage = GenerateDisplayMessage(errors);
            var eventArgs = new ErrorDisplayEventArgs(errors, displayMessage);

            ErrorsReady?.Invoke(this, eventArgs);
            await Task.CompletedTask;
        }

        private void AddPendingError(DomainErrorEvent errorEvent)
        {
            lock (_errorLock)
            {
                _pendingErrors.Add(errorEvent);

                // Reset/start the aggregation timer
                _errorAggregationTimer.Stop();
                _errorAggregationTimer.Start();
            }
        }

        private async void OnErrorAggregationTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            List<DomainErrorEvent> errorsToProcess;

            lock (_errorLock)
            {
                errorsToProcess = new List<DomainErrorEvent>(_pendingErrors);
                _pendingErrors.Clear();
            }

            if (errorsToProcess.Count == 0)
                return;

            await ProcessAggregatedErrors(errorsToProcess);
        }

        private async Task ProcessAggregatedErrors(List<DomainErrorEvent> errors)
        {
            var displayMessage = GenerateDisplayMessage(errors);
            var eventArgs = new ErrorDisplayEventArgs(errors, displayMessage);

            ErrorsReady?.Invoke(this, eventArgs);
            await Task.CompletedTask;
        }

        private string GenerateDisplayMessage(List<DomainErrorEvent> errors)
        {
            if (errors.Count == 1)
            {
                // Single error - show specific localized message
                var error = errors[0];
                return GetLocalizedErrorMessage(error);
            }
            else
            {
                // Multiple errors - show generic message
                return "Multiple errors occurred, please retry";
            }
        }

        private string GetLocalizedErrorMessage(DomainErrorEvent errorEvent)
        {
            // This would normally use a resource manager/localization service
            // For now, return a simple message based on resource key
            var resourceKey = errorEvent.GetResourceKey();
            var parameters = errorEvent.GetParameters();

            return resourceKey switch
            {
                "Location_Error_DuplicateTitle" => $"Location '{parameters.GetValueOrDefault("LocationTitle", "")}' already exists",
                "Location_Error_InvalidCoordinates" => "Invalid coordinates provided",
                "Location_Error_NetworkError" => "Network error occurred",
                "Location_Error_DatabaseError" => "Database error occurred",
                "Weather_Error_ApiUnavailable" => "Weather service is unavailable",
                "Weather_Error_NetworkTimeout" => "Weather service timeout",
                "Weather_Error_InvalidApiKey" => "Weather service authentication failed",
                "Validation_Error_Single" => $"Validation error: {parameters.GetValueOrDefault("ErrorMessage", "Invalid input")}",
                "Validation_Error_Multiple" => $"Multiple validation errors occurred ({parameters.GetValueOrDefault("ErrorCount", 0)} errors)",
                "Tip_Validation_Error_Single" => $"Tip validation error: {parameters.GetValueOrDefault("ErrorMessage", "Invalid tip data")}",
                "Setting_Error_DuplicateKey" => $"Setting '{parameters.GetValueOrDefault("SettingKey", "")}' already exists",
                "TipType_Error_DuplicateName" => $"Tip type '{parameters.GetValueOrDefault("TipTypeName", "")}' already exists",
                _ => "An error occurred, please try again"
            };
        }
    }
}