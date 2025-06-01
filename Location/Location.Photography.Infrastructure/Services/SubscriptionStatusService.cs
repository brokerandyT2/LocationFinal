// Location.Photography.Infrastructure/Services/SubscriptionStatusService.cs
using Location.Core.Application.Common.Models;
using Location.Core.Application.Settings.Queries.GetSettingByKey;
using Location.Photography.Application.Common.Constants;
using Location.Photography.Application.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Networking;
using Polly;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Photography.Infrastructure.Services
{
    public class SubscriptionStatusService : ISubscriptionStatusService
    {
        private readonly ILogger<SubscriptionStatusService> _logger;
        private readonly IMediator _mediator;
        private readonly ISubscriptionService _subscriptionService;
        private readonly IAsyncPolicy _retryPolicy;

        // Cache for subscription info to reduce database calls
        private LocalSubscriptionInfo? _cachedSubscriptionInfo;
        private DateTime _cacheExpiry = DateTime.MinValue;
        private readonly TimeSpan _cacheTimeout = TimeSpan.FromMinutes(5);

        public SubscriptionStatusService(
            ILogger<SubscriptionStatusService> logger,
            IMediator mediator,
            ISubscriptionService subscriptionService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _subscriptionService = subscriptionService ?? throw new ArgumentNullException(nameof(subscriptionService));

            _retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromMinutes(5),
                    onRetry: (outcome, timespan, retryCount, context) =>
                    {
                        _logger.LogWarning("Subscription validation retry {RetryCount} in {Delay} minutes", retryCount, timespan.TotalMinutes);
                    });
        }

        public async Task<Result<SubscriptionStatusResult>> CheckSubscriptionStatusAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var localInfo = await GetLocalSubscriptionInfoAsync(cancellationToken).ConfigureAwait(false);
                if (!localInfo.IsSuccess)
                {
                    return Result<SubscriptionStatusResult>.Failure("Failed to retrieve local subscription info");
                }

                var hasNetwork = await HasNetworkConnectivityAsync().ConfigureAwait(false);

                if (!hasNetwork)
                {
                    return await ProcessOfflineStatusAsync(localInfo.Data, cancellationToken).ConfigureAwait(false);
                }

                return await ProcessOnlineStatusAsync(localInfo.Data, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking subscription status");
                return Result<SubscriptionStatusResult>.Failure($"Error checking subscription status: {ex.Message}");
            }
        }

        public async Task<Result<bool>> CanAccessPremiumFeaturesAsync(CancellationToken cancellationToken = default)
        {
            var statusResult = await CheckSubscriptionStatusAsync(cancellationToken).ConfigureAwait(false);
            if (!statusResult.IsSuccess)
            {
                return Result<bool>.Success(false);
            }

            var status = statusResult.Data;
            return Result<bool>.Success(
                status.HasActiveSubscription &&
                (status.SubscriptionType == SubscriptionConstants.Premium || status.SubscriptionType == SubscriptionConstants.Pro));
        }

        public async Task<Result<bool>> CanAccessProFeaturesAsync(CancellationToken cancellationToken = default)
        {
            var statusResult = await CheckSubscriptionStatusAsync(cancellationToken).ConfigureAwait(false);
            if (!statusResult.IsSuccess)
            {
                return Result<bool>.Success(false);
            }

            var status = statusResult.Data;
            return Result<bool>.Success(
                status.HasActiveSubscription &&
                status.SubscriptionType == SubscriptionConstants.Pro);
        }

        public async Task<Result<bool>> IsInGracePeriodAsync(CancellationToken cancellationToken = default)
        {
            var localInfo = await GetLocalSubscriptionInfoAsync(cancellationToken).ConfigureAwait(false);
            if (!localInfo.IsSuccess || !localInfo.Data.ExpirationDate.HasValue)
            {
                return Result<bool>.Success(false);
            }

            var gracePeriodEnd = localInfo.Data.ExpirationDate.Value.AddDays(3);
            var isInGracePeriod = DateTime.UtcNow <= gracePeriodEnd && DateTime.UtcNow > localInfo.Data.ExpirationDate.Value;

            return Result<bool>.Success(isInGracePeriod);
        }

        public async Task<Result<LocalSubscriptionInfo>> GetLocalSubscriptionInfoAsync(CancellationToken cancellationToken = default)
        {
            // Check cache first to reduce database calls
            if (_cachedSubscriptionInfo != null && DateTime.UtcNow < _cacheExpiry)
            {
                return Result<LocalSubscriptionInfo>.Success(_cachedSubscriptionInfo);
            }

            try
            {
                // Batch all subscription-related queries into a single operation to reduce database round trips
                var subscriptionQueries = new Dictionary<string, GetSettingByKeyQuery>
                {
                    [SubscriptionConstants.SubscriptionType] = new GetSettingByKeyQuery { Key = SubscriptionConstants.SubscriptionType },
                    [SubscriptionConstants.SubscriptionExpiration] = new GetSettingByKeyQuery { Key = SubscriptionConstants.SubscriptionExpiration },
                    [SubscriptionConstants.SubscriptionProductId] = new GetSettingByKeyQuery { Key = SubscriptionConstants.SubscriptionProductId },
                    [SubscriptionConstants.SubscriptionPurchaseDate] = new GetSettingByKeyQuery { Key = SubscriptionConstants.SubscriptionPurchaseDate },
                    [SubscriptionConstants.SubscriptionTransactionId] = new GetSettingByKeyQuery { Key = SubscriptionConstants.SubscriptionTransactionId }
                };

                // Execute all queries in parallel to minimize database access time
                var queryTasks = new List<Task<(string key, Result<GetSettingByKeyQueryResponse> result)>>();

                foreach (var kvp in subscriptionQueries)
                {
                    var key = kvp.Key;
                    var query = kvp.Value;
                    queryTasks.Add(Task.Run(async () =>
                    {
                        var result = await _mediator.Send(query, cancellationToken).ConfigureAwait(false);
                        return (key, result);
                    }, cancellationToken));
                }

                var results = await Task.WhenAll(queryTasks).ConfigureAwait(false);

                // Process results into a dictionary for efficient lookup
                var settingsDict = new Dictionary<string, string>();
                foreach (var (key, result) in results)
                {
                    if (result.IsSuccess && result.Data != null)
                    {
                        settingsDict[key] = result.Data.Value;
                    }
                }

                var info = new LocalSubscriptionInfo
                {
                    SubscriptionType = settingsDict.GetValueOrDefault(SubscriptionConstants.SubscriptionType, SubscriptionConstants.Free),
                    ProductId = settingsDict.GetValueOrDefault(SubscriptionConstants.SubscriptionProductId, string.Empty),
                    TransactionId = settingsDict.GetValueOrDefault(SubscriptionConstants.SubscriptionTransactionId, string.Empty),
                    HasValidData = settingsDict.ContainsKey(SubscriptionConstants.SubscriptionType)
                };

                // Parse dates with error handling
                if (settingsDict.TryGetValue(SubscriptionConstants.SubscriptionExpiration, out var expirationStr) &&
                    DateTime.TryParse(expirationStr, out var expiration))
                {
                    info.ExpirationDate = expiration;
                }

                if (settingsDict.TryGetValue(SubscriptionConstants.SubscriptionPurchaseDate, out var purchaseStr) &&
                    DateTime.TryParse(purchaseStr, out var purchase))
                {
                    info.PurchaseDate = purchase;
                }

                // Cache the result to avoid repeated database calls
                _cachedSubscriptionInfo = info;
                _cacheExpiry = DateTime.UtcNow.Add(_cacheTimeout);

                return Result<LocalSubscriptionInfo>.Success(info);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving local subscription info");
                return Result<LocalSubscriptionInfo>.Failure($"Error retrieving local subscription info: {ex.Message}");
            }
        }

        private async Task<Result<SubscriptionStatusResult>> ProcessOfflineStatusAsync(LocalSubscriptionInfo localInfo, CancellationToken cancellationToken)
        {
            var isInGracePeriod = await IsInGracePeriodAsync(cancellationToken).ConfigureAwait(false);
            var hasActiveSubscription = IsSubscriptionActive(localInfo) || (isInGracePeriod.IsSuccess && isInGracePeriod.Data);

            return Result<SubscriptionStatusResult>.Success(new SubscriptionStatusResult
            {
                HasActiveSubscription = hasActiveSubscription,
                SubscriptionType = localInfo.SubscriptionType,
                ExpirationDate = localInfo.ExpirationDate,
                IsInGracePeriod = isInGracePeriod.IsSuccess && isInGracePeriod.Data,
                NetworkCheckPerformed = false,
                IsError = false
            });
        }

        private async Task<Result<SubscriptionStatusResult>> ProcessOnlineStatusAsync(LocalSubscriptionInfo localInfo, CancellationToken cancellationToken)
        {
            try
            {
                var validationResult = await _retryPolicy.ExecuteAsync(async () =>
                {
                    return await _subscriptionService.ValidateAndUpdateSubscriptionAsync(cancellationToken).ConfigureAwait(false);
                }).ConfigureAwait(false);

                if (!validationResult.IsSuccess)
                {
                    return Result<SubscriptionStatusResult>.Success(new SubscriptionStatusResult
                    {
                        HasActiveSubscription = false,
                        SubscriptionType = SubscriptionConstants.Free,
                        IsError = true,
                        ErrorMessage = "Unable to validate subscription after multiple attempts",
                        NetworkCheckPerformed = true
                    });
                }

                // Clear cache to force refresh after validation
                _cachedSubscriptionInfo = null;
                _cacheExpiry = DateTime.MinValue;

                // After successful validation, get updated local info
                var updatedLocalInfo = await GetLocalSubscriptionInfoAsync(cancellationToken).ConfigureAwait(false);
                if (!updatedLocalInfo.IsSuccess)
                {
                    updatedLocalInfo = Result<LocalSubscriptionInfo>.Success(localInfo);
                }

                var isInGracePeriod = await IsInGracePeriodAsync(cancellationToken).ConfigureAwait(false);
                var hasActiveSubscription = IsSubscriptionActive(updatedLocalInfo.Data) || (isInGracePeriod.IsSuccess && isInGracePeriod.Data);

                return Result<SubscriptionStatusResult>.Success(new SubscriptionStatusResult
                {
                    HasActiveSubscription = hasActiveSubscription,
                    SubscriptionType = updatedLocalInfo.Data.SubscriptionType,
                    ExpirationDate = updatedLocalInfo.Data.ExpirationDate,
                    IsInGracePeriod = isInGracePeriod.IsSuccess && isInGracePeriod.Data,
                    NetworkCheckPerformed = true,
                    IsError = false
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during online subscription validation");

                return Result<SubscriptionStatusResult>.Success(new SubscriptionStatusResult
                {
                    HasActiveSubscription = false,
                    SubscriptionType = SubscriptionConstants.Free,
                    IsError = true,
                    ErrorMessage = "Network validation failed after multiple retries",
                    NetworkCheckPerformed = true
                });
            }
        }

        private bool IsSubscriptionActive(LocalSubscriptionInfo info)
        {
            if (!info.ExpirationDate.HasValue || info.SubscriptionType == SubscriptionConstants.Free)
            {
                return false;
            }

            return DateTime.UtcNow <= info.ExpirationDate.Value;
        }

        private async Task<bool> HasNetworkConnectivityAsync()
        {
            try
            {
                return await Task.Run(() => Connectivity.NetworkAccess == NetworkAccess.Internet).ConfigureAwait(false);
            }
            catch
            {
                return false;
            }
        }
    }
}