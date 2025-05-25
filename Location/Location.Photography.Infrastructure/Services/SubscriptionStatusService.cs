// Location.Photography.Infrastructure/Services/SubscriptionStatusService.cs
using Location.Core.Application.Common.Models;
using Location.Core.Application.Settings.Queries.GetSettingByKey;
using Location.Photography.Application.Commands.Subscription;
using Location.Photography.Application.Common.Constants;
using Location.Photography.Application.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Networking;
using Plugin.InAppBilling;
using Polly;
using System;
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

                var localInfo = await GetLocalSubscriptionInfoAsync(cancellationToken);
                if (!localInfo.IsSuccess)
                {
                    return Result<SubscriptionStatusResult>.Failure("Failed to retrieve local subscription info");
                }

                var hasNetwork = await HasNetworkConnectivityAsync();

                if (!hasNetwork)
                {
                    return await ProcessOfflineStatusAsync(localInfo.Data, cancellationToken);
                }

                return await ProcessOnlineStatusAsync(localInfo.Data, cancellationToken);
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
            var statusResult = await CheckSubscriptionStatusAsync(cancellationToken);
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
            var statusResult = await CheckSubscriptionStatusAsync(cancellationToken);
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
            var localInfo = await GetLocalSubscriptionInfoAsync(cancellationToken);
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
            try
            {
                var subscriptionTypeQuery = new GetSettingByKeyQuery { Key = SubscriptionConstants.SubscriptionType };
                var expirationQuery = new GetSettingByKeyQuery { Key = SubscriptionConstants.SubscriptionExpiration };
                var productIdQuery = new GetSettingByKeyQuery { Key = SubscriptionConstants.SubscriptionProductId };
                var purchaseDateQuery = new GetSettingByKeyQuery { Key = SubscriptionConstants.SubscriptionPurchaseDate };
                var transactionIdQuery = new GetSettingByKeyQuery { Key = SubscriptionConstants.SubscriptionTransactionId };

                var subscriptionTypeResult = await _mediator.Send(subscriptionTypeQuery, cancellationToken);
                var expirationResult = await _mediator.Send(expirationQuery, cancellationToken);
                var productIdResult = await _mediator.Send(productIdQuery, cancellationToken);
                var purchaseDateResult = await _mediator.Send(purchaseDateQuery, cancellationToken);
                var transactionIdResult = await _mediator.Send(transactionIdQuery, cancellationToken);

                var info = new LocalSubscriptionInfo
                {
                    SubscriptionType = subscriptionTypeResult.IsSuccess ? subscriptionTypeResult.Data.Value : SubscriptionConstants.Free,
                    ProductId = productIdResult.IsSuccess ? productIdResult.Data.Value : string.Empty,
                    TransactionId = transactionIdResult.IsSuccess ? transactionIdResult.Data.Value : string.Empty,
                    HasValidData = subscriptionTypeResult.IsSuccess
                };

                if (expirationResult.IsSuccess && DateTime.TryParse(expirationResult.Data.Value, out var expiration))
                {
                    info.ExpirationDate = expiration;
                }

                if (purchaseDateResult.IsSuccess && DateTime.TryParse(purchaseDateResult.Data.Value, out var purchase))
                {
                    info.PurchaseDate = purchase;
                }

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
            var isInGracePeriod = await IsInGracePeriodAsync(cancellationToken);
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
                    return await _subscriptionService.ValidateAndUpdateSubscriptionAsync(cancellationToken);
                });

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

                // After successful validation, get updated local info
                var updatedLocalInfo = await GetLocalSubscriptionInfoAsync(cancellationToken);
                if (!updatedLocalInfo.IsSuccess)
                {
                    updatedLocalInfo = Result<LocalSubscriptionInfo>.Success(localInfo);
                }

                var isInGracePeriod = await IsInGracePeriodAsync(cancellationToken);
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
                return Connectivity.NetworkAccess == NetworkAccess.Internet;
            }
            catch
            {
                return false;
            }
        }
    }
}