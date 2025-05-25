// Location.Photography.ViewModels/Base/SubscriptionAwareViewModelBase.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Location.Core.Application.Services;
using Location.Photography.Application.Services;
using Location.Photography.ViewModels.Events;
using System;
using System.Threading.Tasks;

namespace Location.Photography.ViewModels
{
    public abstract partial class SubscriptionAwareViewModelBase : ViewModelBase
    {
        protected readonly ISubscriptionFeatureGuard _featureGuard;
        private readonly IErrorDisplayService _errorDisplayService;

        [ObservableProperty]
        private bool _hasSubscriptionError;

        [ObservableProperty]
        private string _subscriptionErrorMessage = string.Empty;

        public event EventHandler<SubscriptionUpgradeRequestedEventArgs> SubscriptionUpgradeRequested;

        protected SubscriptionAwareViewModelBase(
            ISubscriptionFeatureGuard featureGuard,
            IErrorDisplayService errorDisplayService)
            : base(null, errorDisplayService)
        {
            _featureGuard = featureGuard ?? throw new ArgumentNullException(nameof(featureGuard));
            _errorDisplayService = errorDisplayService ?? throw new ArgumentNullException(nameof(errorDisplayService));
        }

        /// <summary>
        /// Checks premium feature access and handles UI accordingly
        /// </summary>
        protected async Task<bool> CheckPremiumFeatureAccessAsync()
        {
            try
            {
                var result = await _featureGuard.CheckPremiumFeatureAccessAsync();

                if (!result.IsSuccess)
                {
                    HasSubscriptionError = true;
                    SubscriptionErrorMessage = "Unable to verify subscription status";
                    return false;
                }

                var accessResult = result.Data;

                if (accessResult.HasAccess)
                {
                    HasSubscriptionError = false;
                    SubscriptionErrorMessage = string.Empty;
                    return true;
                }

                await HandleFeatureAccessDenied(accessResult);
                return false;
            }
            catch (Exception ex)
            {
                HasSubscriptionError = true;
                SubscriptionErrorMessage = "Error checking subscription status";
                OnSystemError($"There was an error processing your request, please try again: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Checks professional feature access and handles UI accordingly
        /// </summary>
        protected async Task<bool> CheckProFeatureAccessAsync()
        {
            try
            {
                var result = await _featureGuard.CheckProFeatureAccessAsync();

                if (!result.IsSuccess)
                {
                    HasSubscriptionError = true;
                    SubscriptionErrorMessage = "Unable to verify subscription status";
                    return false;
                }

                var accessResult = result.Data;

                if (accessResult.HasAccess)
                {
                    HasSubscriptionError = false;
                    SubscriptionErrorMessage = string.Empty;
                    return true;
                }

                await HandleFeatureAccessDenied(accessResult);
                return false;
            }
            catch (Exception ex)
            {
                HasSubscriptionError = true;
                SubscriptionErrorMessage = "Error checking subscription status";
                OnSystemError($"There was an error processing your request, please try again: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Checks any paid feature access and handles UI accordingly
        /// </summary>
        protected async Task<bool> CheckPaidFeatureAccessAsync()
        {
            try
            {
                var result = await _featureGuard.CheckPaidFeatureAccessAsync();

                if (!result.IsSuccess)
                {
                    HasSubscriptionError = true;
                    SubscriptionErrorMessage = "Unable to verify subscription status";
                    return false;
                }

                var accessResult = result.Data;

                if (accessResult.HasAccess)
                {
                    HasSubscriptionError = false;
                    SubscriptionErrorMessage = string.Empty;
                    return true;
                }

                await HandleFeatureAccessDenied(accessResult);
                return false;
            }
            catch (Exception ex)
            {
                HasSubscriptionError = true;
                SubscriptionErrorMessage = "Error checking subscription status";
                OnSystemError($"There was an error processing your request, please try again: {ex.Message}");
                return false;
            }
        }

        private async Task HandleFeatureAccessDenied(FeatureAccessResult accessResult)
        {
            switch (accessResult.Action)
            {
                case FeatureAccessAction.ShowUpgradePrompt:
                    OnSubscriptionUpgradeRequested(new SubscriptionUpgradeRequestedEventArgs
                    {
                        RequiredSubscription = accessResult.RequiredSubscription,
                        Message = accessResult.Message
                    });
                    break;

                case FeatureAccessAction.ShowError:
                    HasSubscriptionError = true;
                    SubscriptionErrorMessage = accessResult.Message;
                    SetValidationError(accessResult.Message);
                    break;
            }
            await Task.CompletedTask;
        }

        protected virtual void OnSubscriptionUpgradeRequested(SubscriptionUpgradeRequestedEventArgs e)
        {
            SubscriptionUpgradeRequested?.Invoke(this, e);
        }

        [RelayCommand]
        protected virtual async Task RequestUpgradeAsync()
        {
            OnSubscriptionUpgradeRequested(new SubscriptionUpgradeRequestedEventArgs
            {
                RequiredSubscription = "Premium",
                Message = "Upgrade to Premium to access this feature"
            });
            await Task.CompletedTask;
        }
    }
}