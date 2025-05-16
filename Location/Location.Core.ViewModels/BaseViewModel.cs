// Location.Core.ViewModels/BaseViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using Location.Core.Application.Services;
using System;
using System.Threading.Tasks;

namespace Location.Core.ViewModels
{
    public abstract class BaseViewModel : ObservableObject, IDisposable
    {
        private readonly IAlertService? _alertService;

        private bool _isBusy;
        private bool _isError;
        private string _errorMessage = string.Empty;

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
                }
            }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        protected BaseViewModel(IAlertService? alertService = null)
        {
            _alertService = alertService;
        }

        protected virtual async Task PublishErrorAsync(string message)
        {
            // Only publish if we have an alerting service
            if (_alertService != null)
            {
                await _alertService.ShowErrorAlertAsync(message, "Error");
            }
        }

        public virtual void Dispose()
        {
            // Base implementation is empty, derived classes can override
            GC.SuppressFinalize(this);
        }
    }
}