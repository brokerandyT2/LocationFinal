// Location.Photography.Infrastructure/Services/SubscriptionFeatureGuardService.cs
using Location.Core.Application.Common.Models;
using Location.Photography.Application.Common.Constants;
using Location.Photography.Application.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Photography.Infrastructure.Services
{
    public class SubscriptionFeatureGuardService : ISubscriptionFeatureGuard
    {
        private readonly ISubscriptionStatusService _subscriptionStatusService;
        private readonly ILogger<SubscriptionFeatureGuardService> _logger;

        // Cache for feature access results to reduce repeated subscription checks
        private readonly ConcurrentDictionary<string, (FeatureAccessResult result, DateTime expiry)> _accessCache = new();
        private readonly TimeSpan _cacheTimeout = TimeSpan.FromMinutes(2); // Short cache for subscription checks

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

                // Check cache first to reduce database/service calls
                const string cacheKey = "premium_access";
                if (_accessCache.TryGetValue(cacheKey, out var cached) && DateTime.UtcNow < cached.expiry)
                {
                    return Result<FeatureAccessResult>.Success(cached.result);
                }

                // Move subscription check to background thread to prevent UI blocking
                var accessResult = await Task.Run(async () =>
                {
                    var canAccessResult = await _subscriptionStatusService.CanAccessPremiumFeaturesAsync(cancellationToken).ConfigureAwait(false);

                    if (!canAccessResult.IsSuccess)
                    {
                        _logger.LogWarning("Failed to check premium feature access: {Error}", canAccessResult.ErrorMessage);
                        return new FeatureAccessResult
                        {
                            HasAccess = false,
                            RequiredSubscription = SubscriptionConstants.Premium,
                            Action = FeatureAccessAction.ShowUpgradePrompt,
                            Message = "Premium subscription required to access this feature"
                        };
                    }

                    if (canAccessResult.Data)
                    {
                        return new FeatureAccessResult
                        {
                            HasAccess = true,
                            Action = FeatureAccessAction.Allow
                        };
                    }

                    var statusResult = await _subscriptionStatusService.CheckSubscriptionStatusAsync(cancellationToken).ConfigureAwait(false);
                    var message = "Premium subscription required to access this feature";

                    if (statusResult.IsSuccess && statusResult.Data.IsInGracePeriod)
                    {
                        message = "Your premium subscription has expired. You have limited time remaining to renew.";
                    }

                    return new FeatureAccessResult
                    {
                        HasAccess = false,
                        RequiredSubscription = SubscriptionConstants.Premium,
                        Action = FeatureAccessAction.ShowUpgradePrompt,
                        Message = message
                    };

                }, cancellationToken).ConfigureAwait(false);

                // Cache the result to reduce future subscription checks
                _accessCache[cacheKey] = (accessResult, DateTime.UtcNow.Add(_cacheTimeout));

                return Result<FeatureAccessResult>.Success(accessResult);
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

                // Check cache first to reduce database/service calls
                const string cacheKey = "pro_access";
                if (_accessCache.TryGetValue(cacheKey, out var cached) && DateTime.UtcNow < cached.expiry)
                {
                    return Result<FeatureAccessResult>.Success(cached.result);
                }

                // Move subscription check to background thread to prevent UI blocking
                var accessResult = await Task.Run(async () =>
                {
                    var canAccessResult = await _subscriptionStatusService.CanAccessProFeaturesAsync(cancellationToken).ConfigureAwait(false);

                    if (!canAccessResult.IsSuccess)
                    {
                        _logger.LogWarning("Failed to check pro feature access: {Error}", canAccessResult.ErrorMessage);
                        return new FeatureAccessResult
                        {
                            HasAccess = false,
                            RequiredSubscription = SubscriptionConstants.Pro,
                            Action = FeatureAccessAction.ShowUpgradePrompt,
                            Message = "Professional subscription required to access this feature"
                        };
                    }

                    if (canAccessResult.Data)
                    {
                        return new FeatureAccessResult
                        {
                            HasAccess = true,
                            Action = FeatureAccessAction.Allow
                        };
                    }

                    var statusResult = await _subscriptionStatusService.CheckSubscriptionStatusAsync(cancellationToken).ConfigureAwait(false);
                    var message = "Professional subscription required to access this feature";

                    if (statusResult.IsSuccess && statusResult.Data.IsInGracePeriod)
                    {
                        message = "Your professional subscription has expired. You have limited time remaining to renew.";
                    }

                    return new FeatureAccessResult
                    {
                        HasAccess = false,
                        RequiredSubscription = SubscriptionConstants.Pro,
                        Action = FeatureAccessAction.ShowUpgradePrompt,
                        Message = message
                    };

                }, cancellationToken).ConfigureAwait(false);

                // Cache the result to reduce future subscription checks
                _accessCache[cacheKey] = (accessResult, DateTime.UtcNow.Add(_cacheTimeout));

                return Result<FeatureAccessResult>.Success(accessResult);
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

                // Check cache first to reduce database/service calls
                const string cacheKey = "paid_access";
                if (_accessCache.TryGetValue(cacheKey, out var cached) && DateTime.UtcNow < cached.expiry)
                {
                    return Result<FeatureAccessResult>.Success(cached.result);
                }

                // Move subscription checks to background thread to prevent UI blocking
                var accessResult = await Task.Run(async () =>
                {
                    // Check premium access first (higher tier)
                    var premiumResult = await CheckPremiumFeatureAccessAsync(cancellationToken).ConfigureAwait(false);

                    if (premiumResult.IsSuccess && premiumResult.Data.HasAccess)
                    {
                        return premiumResult.Data;
                    }

                    // Check pro access as fallback
                    var proResult = await CheckProFeatureAccessAsync(cancellationToken).ConfigureAwait(false);

                    if (proResult.IsSuccess && proResult.Data.HasAccess)
                    {
                        return proResult.Data;
                    }

                    // No access to either tier
                    return new FeatureAccessResult
                    {
                        HasAccess = false,
                        RequiredSubscription = SubscriptionConstants.Premium,
                        Action = FeatureAccessAction.ShowUpgradePrompt,
                        Message = "Premium or Professional subscription required to access this feature"
                    };

                }, cancellationToken).ConfigureAwait(false);

                // Cache the result to reduce future subscription checks
                _accessCache[cacheKey] = (accessResult, DateTime.UtcNow.Add(_cacheTimeout));

                return Result<FeatureAccessResult>.Success(accessResult);
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

        /// <summary>
        /// Clear all cached access results when subscription status changes
        /// </summary>
        public void InvalidateAccessCache()
        {
            _accessCache.Clear();
            _logger.LogDebug("Feature access cache invalidated");
        }

        /// <summary>
        /// Bulk check for multiple feature types to optimize performance when checking multiple features
        /// </summary>
        public async Task<Result<Dictionary<string, FeatureAccessResult>>> CheckMultipleFeatureAccessAsync(
            string[] featureTypes,
            CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var results = new Dictionary<string, FeatureAccessResult>();

                // Process feature checks in parallel for better performance
                var checkTasks = featureTypes.Select(async featureType =>
                {
                    FeatureAccessResult result = featureType.ToLower() switch
                    {
                        "premium" => (await CheckPremiumFeatureAccessAsync(cancellationToken).ConfigureAwait(false)).Data,
                        "pro" => (await CheckProFeatureAccessAsync(cancellationToken).ConfigureAwait(false)).Data,
                        "paid" => (await CheckPaidFeatureAccessAsync(cancellationToken).ConfigureAwait(false)).Data,
                        _ => new FeatureAccessResult
                        {
                            HasAccess = false,
                            Action = FeatureAccessAction.ShowError,
                            Message = $"Unknown feature type: {featureType}"
                        }
                    };

                    return (featureType, result);
                });

                var checkResults = await Task.WhenAll(checkTasks).ConfigureAwait(false);

                foreach (var (featureType, result) in checkResults)
                {
                    results[featureType] = result;
                }

                return Result<Dictionary<string, FeatureAccessResult>>.Success(results);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking multiple feature access");
                return Result<Dictionary<string, FeatureAccessResult>>.Failure($"Error checking feature access: {ex.Message}");
            }
        }

        /// <summary>
        /// Cleanup expired cache entries to prevent memory leaks
        /// </summary>
        public void CleanupExpiredCache()
        {
            var expiredKeys = new List<string>();

            foreach (var kvp in _accessCache)
            {
                if (DateTime.UtcNow >= kvp.Value.expiry)
                {
                    expiredKeys.Add(kvp.Key);
                }
            }

            foreach (var key in expiredKeys)
            {
                _accessCache.TryRemove(key, out _);
            }

            if (expiredKeys.Count > 0)
            {
                _logger.LogDebug("Cleaned up {Count} expired feature access cache entries", expiredKeys.Count);
            }
        }
    }
}