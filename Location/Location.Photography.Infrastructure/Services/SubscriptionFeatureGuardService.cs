// Location.Photography.Infrastructure/Services/SubscriptionFeatureGuardService.cs
using Location.Core.Application.Common.Models;
using Location.Photography.Application.Common.Constants;
using Location.Photography.Application.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Photography.Infrastructure.Services
{
    public class SubscriptionFeatureGuardService : ISubscriptionFeatureGuard
    {
        private readonly ISubscriptionStatusService _subscriptionStatusService;
        private readonly ILogger<SubscriptionFeatureGuardService> _logger;

        public SubscriptionFeatureGuardService(
            ISubscriptionStatusService subscriptionStatusService,
            ILogger<SubscriptionFeatureGuardService> logger)
        {
            _subscriptionStatusService = subscriptionStatusService ?? throw new ArgumentNullException(nameof(subscriptionStatusService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<FeatureAccessResult>> CheckPremiumFeatureAccessAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var canAccessResult = await _subscriptionStatusService.CanAccessPremiumFeaturesAsync(cancellationToken);

                if (!canAccessResult.IsSuccess)
                {
                    _logger.LogWarning("Failed to check premium feature access: {Error}", canAccessResult.ErrorMessage);
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
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking premium feature access");
                return Result<FeatureAccessResult>.Failure($"Error checking premium feature access: {ex.Message}");
            }
        }

        public async Task<Result<FeatureAccessResult>> CheckProFeatureAccessAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var canAccessResult = await _subscriptionStatusService.CanAccessProFeaturesAsync(cancellationToken);

                if (!canAccessResult.IsSuccess)
                {
                    _logger.LogWarning("Failed to check pro feature access: {Error}", canAccessResult.ErrorMessage);
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
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking pro feature access");
                return Result<FeatureAccessResult>.Failure($"Error checking pro feature access: {ex.Message}");
            }
        }

        public async Task<Result<FeatureAccessResult>> CheckPaidFeatureAccessAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

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
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking paid feature access");
                return Result<FeatureAccessResult>.Failure($"Error checking paid feature access: {ex.Message}");
            }
        }
    }
}