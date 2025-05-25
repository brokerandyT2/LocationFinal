// Location.Photography.Application/Services/SubscriptionFeatureGuard.cs
using Location.Core.Application.Common.Models;
using Location.Photography.Application.Common.Constants;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Photography.Application.Services
{
    public interface ISubscriptionFeatureGuard
    {
        /// <summary>
        /// Checks if premium features are accessible and returns appropriate action
        /// </summary>
        Task<Result<FeatureAccessResult>> CheckPremiumFeatureAccessAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if professional features are accessible and returns appropriate action
        /// </summary>
        Task<Result<FeatureAccessResult>> CheckProFeatureAccessAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if any paid features are accessible
        /// </summary>
        Task<Result<FeatureAccessResult>> CheckPaidFeatureAccessAsync(CancellationToken cancellationToken = default);
    }

    public class SubscriptionFeatureGuard : ISubscriptionFeatureGuard
    {
        private readonly ISubscriptionStatusService _subscriptionStatusService;

        public SubscriptionFeatureGuard(ISubscriptionStatusService subscriptionStatusService)
        {
            _subscriptionStatusService = subscriptionStatusService ?? throw new ArgumentNullException(nameof(subscriptionStatusService));
        }

        public async Task<Result<FeatureAccessResult>> CheckPremiumFeatureAccessAsync(CancellationToken cancellationToken = default)
        {
            var canAccessResult = await _subscriptionStatusService.CanAccessPremiumFeaturesAsync(cancellationToken);

            if (!canAccessResult.IsSuccess)
            {
                return Result<FeatureAccessResult>.Success(new FeatureAccessResult
                {
                    HasAccess = false,
                    RequiredSubscription = SubscriptionConstants.Premium,
                    Action = FeatureAccessAction.ShowUpgradePrompt,
                    Message = "Premium subscription required to access this feature"
                });
            }

            if (canAccessResult.Data)
            {
                return Result<FeatureAccessResult>.Success(new FeatureAccessResult
                {
                    HasAccess = true,
                    Action = FeatureAccessAction.Allow
                });
            }

            var statusResult = await _subscriptionStatusService.CheckSubscriptionStatusAsync(cancellationToken);
            var message = "Premium subscription required to access this feature";

            if (statusResult.IsSuccess && statusResult.Data.IsInGracePeriod)
            {
                message = "Your premium subscription has expired. You have limited time remaining to renew.";
            }

            return Result<FeatureAccessResult>.Success(new FeatureAccessResult
            {
                HasAccess = false,
                RequiredSubscription = SubscriptionConstants.Premium,
                Action = FeatureAccessAction.ShowUpgradePrompt,
                Message = message
            });
        }

        public async Task<Result<FeatureAccessResult>> CheckProFeatureAccessAsync(CancellationToken cancellationToken = default)
        {
            var canAccessResult = await _subscriptionStatusService.CanAccessProFeaturesAsync(cancellationToken);

            if (!canAccessResult.IsSuccess)
            {
                return Result<FeatureAccessResult>.Success(new FeatureAccessResult
                {
                    HasAccess = false,
                    RequiredSubscription = SubscriptionConstants.Pro,
                    Action = FeatureAccessAction.ShowUpgradePrompt,
                    Message = "Professional subscription required to access this feature"
                });
            }

            if (canAccessResult.Data)
            {
                return Result<FeatureAccessResult>.Success(new FeatureAccessResult
                {
                    HasAccess = true,
                    Action = FeatureAccessAction.Allow
                });
            }

            var statusResult = await _subscriptionStatusService.CheckSubscriptionStatusAsync(cancellationToken);
            var message = "Professional subscription required to access this feature";

            if (statusResult.IsSuccess && statusResult.Data.IsInGracePeriod)
            {
                message = "Your professional subscription has expired. You have limited time remaining to renew.";
            }

            return Result<FeatureAccessResult>.Success(new FeatureAccessResult
            {
                HasAccess = false,
                RequiredSubscription = SubscriptionConstants.Pro,
                Action = FeatureAccessAction.ShowUpgradePrompt,
                Message = message
            });
        }

        public async Task<Result<FeatureAccessResult>> CheckPaidFeatureAccessAsync(CancellationToken cancellationToken = default)
        {
            var premiumResult = await CheckPremiumFeatureAccessAsync(cancellationToken);

            if (premiumResult.IsSuccess && premiumResult.Data.HasAccess)
            {
                return premiumResult;
            }

            var proResult = await CheckProFeatureAccessAsync(cancellationToken);

            if (proResult.IsSuccess && proResult.Data.HasAccess)
            {
                return proResult;
            }

            return Result<FeatureAccessResult>.Success(new FeatureAccessResult
            {
                HasAccess = false,
                RequiredSubscription = SubscriptionConstants.Premium,
                Action = FeatureAccessAction.ShowUpgradePrompt,
                Message = "Premium or Professional subscription required to access this feature"
            });
        }
    }

    public class FeatureAccessResult
    {
        public bool HasAccess { get; set; }
        public string RequiredSubscription { get; set; } = string.Empty;
        public FeatureAccessAction Action { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public enum FeatureAccessAction
    {
        Allow,
        ShowUpgradePrompt,
        ShowError
    }
}