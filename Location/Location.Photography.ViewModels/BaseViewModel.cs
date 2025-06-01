// Location.Photography.ViewModels/BaseViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Events;
using Location.Core.Application.Services;
using Location.Photography.ViewModels.Events;

namespace Location.Photography.ViewModels
{
    public abstract class ViewModelBase : ObservableObject, IDisposable
    {
        private readonly IEventBus? _eventBus;
        private readonly IErrorDisplayService? _errorDisplayService;

        private bool _isBusy;
        private bool _isError;
        private string _errorMessage = string.Empty;
        private bool _hasActiveErrors;

        // Command tracking for retry functionality
        private IAsyncRelayCommand? _lastCommand;
        private object? _lastCommandParameter;

        // PERFORMANCE OPTIMIZATION: Property change batching
        private readonly Dictionary<string, object> _pendingPropertyChanges = new();
        private readonly object _batchLock = new object();
        private bool _isBatchingPropertyChanges = false;

        // Add the ErrorOccurred event for system errors (MediatR failures)
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

        // Retry tracking properties
        public IAsyncRelayCommand? LastCommand => _lastCommand;
        public object? LastCommandParameter => _lastCommandParameter;

        protected ViewModelBase(IEventBus? eventBus = null, IErrorDisplayService? errorDisplayService = null)
        {
            _eventBus = eventBus;
            _errorDisplayService = errorDisplayService;

            // Subscribe to error display service if available
            if (_errorDisplayService != null)
            {
                _errorDisplayService.ErrorsReady += OnErrorsReady;
            }
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Batch property change notifications to reduce UI updates
        /// </summary>
        protected void BeginPropertyChangeBatch()
        {
            lock (_batchLock)
            {
                _isBatchingPropertyChanges = true;
                _pendingPropertyChanges.Clear();
            }
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: End batching and fire all pending notifications at once
        /// </summary>
        protected async Task EndPropertyChangeBatchAsync()
        {
            Dictionary<string, object> pendingChanges;

            lock (_batchLock)
            {
                if (!_isBatchingPropertyChanges) return;

                pendingChanges = new Dictionary<string, object>(_pendingPropertyChanges);
                _pendingPropertyChanges.Clear();
                _isBatchingPropertyChanges = false;
            }

            // Fire all notifications on UI thread
            if (pendingChanges.Count > 0)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    foreach (var propertyName in pendingChanges.Keys)
                    {
                        OnPropertyChanged(propertyName);
                    }
                });
            }
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Override SetProperty to support batching
        /// </summary>
        protected new bool SetProperty<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;

            lock (_batchLock)
            {
                if (_isBatchingPropertyChanges && !string.IsNullOrEmpty(propertyName))
                {
                    _pendingPropertyChanges[propertyName] = value!;
                    return true;
                }
            }

            // Normal immediate notification if not batching
            OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// Tracks the last executed command for retry functionality
        /// </summary>
        protected void TrackCommand(IAsyncRelayCommand command, object? parameter = null)
        {
            _lastCommand = command;
            _lastCommandParameter = parameter;
        }

        /// <summary>
        /// Executes a command and tracks it for retry capability
        /// </summary>
        public async Task ExecuteAndTrackAsync(IAsyncRelayCommand command, object? parameter = null)
        {
            IsBusy = true;
            TrackCommand(command, parameter);
            await command.ExecuteAsync(parameter);
            IsBusy = false;
        }

        /// <summary>
        /// Retries the last executed command
        /// </summary>
        public async Task RetryLastCommandAsync()
        {
            IsBusy = true;
            if (_lastCommand?.CanExecute(_lastCommandParameter) == true)
            {
                await _lastCommand.ExecuteAsync(_lastCommandParameter);
            }
            IsBusy = false;
        }

        /// <summary>
        /// Triggers system error event (for MediatR failures)
        /// </summary>
        public virtual void OnSystemError(string message)
        {
            ErrorOccurred?.Invoke(this, new OperationErrorEventArgs(OperationErrorSource.Unknown, message));
        }

        /// <summary>
        /// Sets validation error (displays in UI)
        /// </summary>
        protected virtual void SetValidationError(string message)
        {
            ErrorMessage = message;
            IsError = true;
        }

        /// <summary>
        /// Clears all error states
        /// </summary>
        protected virtual void ClearErrors()
        {
            IsError = false;
            ErrorMessage = string.Empty;
            HasActiveErrors = false;
        }

        protected virtual async Task PublishErrorAsync(string message)
        {
            // Also publish to event bus for system-wide handling
            if (_eventBus != null)
            {
                await _eventBus.PublishAsync(new ErrorOccurredEvent(message, GetType().Name));
            }
        }

        protected virtual void OnErrorOccurred(string message)
        {
            ErrorOccurred?.Invoke(this, new OperationErrorEventArgs(OperationErrorSource.Unknown, message));
        }

        private async void OnErrorsReady(object? sender, ErrorDisplayEventArgs e)
        {
            HasActiveErrors = true;

            try
            {
                // System errors from ErrorDisplayService trigger system error event
                OnSystemError(e.DisplayMessage);
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
}