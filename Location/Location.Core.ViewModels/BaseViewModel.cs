// Location.Core.ViewModels/BaseViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using Location.Core.Application.Services;
using Location.Core.Application.Events;
using System;
using System.Threading.Tasks;
using Location.Core.Application.Common.Interfaces;

namespace Location.Core.ViewModels
{
    public abstract class BaseViewModel : ObservableObject, IDisposable
    {
        private readonly IAlertService? _alertService;
        private readonly IEventBus? _eventBus;
        private readonly IErrorDisplayService? _errorDisplayService;

        private bool _isBusy;
        private bool _isError;
        private string _errorMessage = string.Empty;
        private bool _hasActiveErrors;

        // Add the ErrorOccurred event to BaseViewModel
        public event EventHandler<OperationErrorEventArgs>? ErrorOccurred;

        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        public bool IsError
        {
            get => _isError;
            set
            {
                if (SetProperty(ref _isError, value) && value && !string.IsNullOrEmpty(ErrorMessage))
                {
                    // When IsError is set to true, publish the error
                    PublishErrorAsync(ErrorMessage).ConfigureAwait(false);

                    // Also trigger the ErrorOccurred event
                    OnErrorOccurred(ErrorMessage);
                }
            }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public bool HasActiveErrors
        {
            get => _hasActiveErrors;
            set => SetProperty(ref _hasActiveErrors, value);
        }

        protected BaseViewModel(IAlertService? alertService = null, IEventBus? eventBus = null, IErrorDisplayService? errorDisplayService = null)
        {
            _alertService = alertService;
            _eventBus = eventBus;
            _errorDisplayService = errorDisplayService;

            // Subscribe to error display service if available
            if (_errorDisplayService != null)
            {
                _errorDisplayService.ErrorsReady += OnErrorsReady;
            }
        }

        protected virtual async Task PublishErrorAsync(string message)
        {
            // Only publish if we have an alerting service
            if (_alertService != null)
            {
                await _alertService.ShowErrorAlertAsync(message, "Error");
            }

            // Also publish to event bus for system-wide handling
            if (_eventBus != null)
            {
                await _eventBus.PublishAsync(new ErrorOccurredEvent(message, GetType().Name));
            }
        }

        // Add the OnErrorOccurred method to BaseViewModel
        protected virtual void OnErrorOccurred(string message)
        {
            ErrorOccurred?.Invoke(this, new OperationErrorEventArgs(message));
        }

        private async void OnErrorsReady(object? sender, ErrorDisplayEventArgs e)
        {
            HasActiveErrors = true;

            try
            {
                // Display the aggregated error message
                if (_alertService != null)
                {
                    await _alertService.ShowErrorAlertAsync(e.DisplayMessage, "Error");
                }

                // Update ViewModel error state
                ErrorMessage = e.DisplayMessage;
                IsError = true;
                OnErrorOccurred(e.DisplayMessage);
            }
            finally
            {
                HasActiveErrors = false;
            }
        }

        public virtual void Dispose()
        {
            // Unsubscribe from error display service
            if (_errorDisplayService != null)
            {
                _errorDisplayService.ErrorsReady -= OnErrorsReady;
            }

            GC.SuppressFinalize(this);
        }
    }

    // Move the OperationErrorEventArgs class to BaseViewModel file
    public class OperationErrorEventArgs : EventArgs
    {
        public string Message { get; }

        public OperationErrorEventArgs(string message)
        {
            Message = message;
        }
    }
}