// Location.Core.ViewModels/BaseViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using Location.Core.Application.Services;
using Location.Core.Application.Events;
using System;
using System.Threading.Tasks;
using Location.Core.Application.Common.Interfaces;


namespace Location.Photography.ViewModels
{
    public abstract class ViewModelBase : ObservableObject, IDisposable
    {
        private readonly IAlertService? _alertService;
        private readonly IEventBus? _eventBus;

        private bool _isBusy;
        private bool _isError;
        private string _errorMessage = string.Empty;

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

        protected ViewModelBase(IAlertService? alertService = null, IEventBus? eventBus = null)
        {
            _alertService = alertService;
            _eventBus = eventBus;
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

        public virtual void Dispose()
        {
            // Base implementation is empty, derived classes can override
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