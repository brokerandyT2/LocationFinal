// Location.Photography.Infrastructure/Services/SubscriptionService.cs
using Location.Core.Application.Common.Models;
using Location.Photography.Application.Commands.Subscription;
using Location.Photography.Application.Common.Interfaces;
using Location.Photography.Application.Queries.Subscription;
using Location.Photography.Application.Services;
using Location.Photography.Domain.Entities;
using Location.Photography.Infrastructure.Resources;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Storage;
using Plugin.InAppBilling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Photography.Infrastructure.Services
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly ILogger<SubscriptionService> _logger;
        private readonly ISubscriptionRepository _subscriptionRepository;

        // Cache for billing connection state to reduce overhead
        private bool _isConnected = false;
        private DateTime _lastConnectionCheck = DateTime.MinValue;
        private readonly TimeSpan _connectionCheckInterval = TimeSpan.FromMinutes(5);

        public SubscriptionService(
            ILogger<SubscriptionService> logger,
            ISubscriptionRepository subscriptionRepository)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _subscriptionRepository = subscriptionRepository ?? throw new ArgumentNullException(nameof(subscriptionRepository));
        }

        public async Task<Result<bool>> InitializeAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Check if we recently verified connection to avoid excessive calls
                if (_isConnected && DateTime.UtcNow - _lastConnectionCheck < _connectionCheckInterval)
                {
                    return Result<bool>.Success(true);
                }

                // Move billing service connection to background thread to prevent UI blocking
                var connected = await Task.Run(async () =>
                {
                    return await CrossInAppBilling.Current.ConnectAsync().ConfigureAwait(false);
                }, cancellationToken).ConfigureAwait(false);

                _isConnected = connected;
                _lastConnectionCheck = DateTime.UtcNow;

                return Result<bool>.Success(connected);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize billing service");
                _isConnected = false;
                return Result<bool>.Failure(AppResources.Subscription_Error_NetworkConnectivity);
            }
        }

        public async Task<Result<ProcessSubscriptionResultDto>> PurchaseSubscriptionAsync(string productId, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Ensure we're connected before attempting purchase
                var initResult = await InitializeAsync(cancellationToken).ConfigureAwait(false);
                if (!initResult.IsSuccess || !initResult.Data)
                {
                    return Result<ProcessSubscriptionResultDto>.Failure(AppResources.Subscription_Error_BillingServiceNotAvailable);
                }

                // Move purchase operation to background thread to prevent UI blocking
                var result = await Task.Run(async () =>
                {
                    var purchase = await CrossInAppBilling.Current.PurchaseAsync(productId, ItemType.Subscription).ConfigureAwait(false);

                    if (purchase == null)
                    {
                        return Result<ProcessSubscriptionResultDto>.Failure(AppResources.Subscription_Error_ProcessingRequest);
                    }

                    var subscriptionResult = new ProcessSubscriptionResultDto
                    {
                        IsSuccessful = purchase.State == PurchaseState.Purchased,
                        TransactionId = purchase.Id,
                        PurchaseToken = purchase.PurchaseToken,
                        PurchaseDate = purchase.TransactionDateUtc,
                        ProductId = purchase.ProductId,
                        Status = purchase.State == PurchaseState.Purchased ? SubscriptionStatus.Active : SubscriptionStatus.Failed,
                        ExpirationDate = CalculateExpirationDate(productId, purchase.TransactionDateUtc)
                    };

                    return Result<ProcessSubscriptionResultDto>.Success(subscriptionResult);

                }, cancellationToken).ConfigureAwait(false);

                return result;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (InAppBillingPurchaseException ex)
            {
                _logger.LogWarning(ex, "Purchase failed: {Message}", ex.Message);
                return Result<ProcessSubscriptionResultDto>.Failure(AppResources.Subscription_Error_ProcessingRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to purchase subscription");
                return Result<ProcessSubscriptionResultDto>.Failure(AppResources.Subscription_Error_NetworkConnectivity);
            }
        }

        public async Task<Result<bool>> StoreSubscriptionAsync(ProcessSubscriptionResultDto subscriptionData, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var userId = await GetCurrentUserIdAsync().ConfigureAwait(false);

                // Move subscription creation to background thread to prevent UI blocking
                var storeResult = await Task.Run(async () =>
                {
                    var subscription = new Subscription(
                        subscriptionData.ProductId,
                        subscriptionData.TransactionId,
                        subscriptionData.PurchaseToken,
                        subscriptionData.PurchaseDate,
                        subscriptionData.ExpirationDate,
                        subscriptionData.Status,
                        subscriptionData.ProductId.Contains("monthly") ? Domain.Entities.SubscriptionPeriod.Monthly : Domain.Entities.SubscriptionPeriod.Yearly,
                        userId
                    );

                    return await _subscriptionRepository.CreateAsync(subscription, cancellationToken).ConfigureAwait(false);
                }, cancellationToken).ConfigureAwait(false);

                return Result<bool>.Success(storeResult.IsSuccess);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to store subscription");
                return Result<bool>.Failure(AppResources.Subscription_Error_FailedToStoreSubscriptionData);
            }
        }

        public async Task<Result<SubscriptionStatusDto>> GetCurrentSubscriptionStatusAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var userId = await GetCurrentUserIdAsync().ConfigureAwait(false);

                // Move subscription retrieval to background thread to prevent UI blocking
                var statusResult = await Task.Run(async () =>
                {
                    var subscriptionResult = await _subscriptionRepository.GetActiveSubscriptionAsync(userId, cancellationToken).ConfigureAwait(false);

                    if (!subscriptionResult.IsSuccess || subscriptionResult.Data == null)
                    {
                        return new SubscriptionStatusDto
                        {
                            HasActiveSubscription = false,
                            Status = SubscriptionStatus.Expired
                        };
                    }

                    var subscription = subscriptionResult.Data;
                    return new SubscriptionStatusDto
                    {
                        HasActiveSubscription = subscription.IsActive,
                        ProductId = subscription.ProductId,
                        Status = subscription.Status,
                        ExpirationDate = subscription.ExpirationDate,
                        PurchaseDate = subscription.PurchaseDate,
                        Period = subscription.Period,
                        IsExpiringSoon = subscription.IsExpiringSoon(),
                        DaysUntilExpiration = subscription.DaysUntilExpiration()
                    };

                }, cancellationToken).ConfigureAwait(false);

                return Result<SubscriptionStatusDto>.Success(statusResult);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get subscription status");
                return Result<SubscriptionStatusDto>.Failure(AppResources.Subscription_Error_FailedToRetrieveSubscriptionStatus);
            }
        }

        public async Task<Result<bool>> ValidateAndUpdateSubscriptionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Ensure we're connected before validation
                var initResult = await InitializeAsync(cancellationToken).ConfigureAwait(false);
                if (!initResult.IsSuccess || !initResult.Data)
                {
                    return Result<bool>.Failure(AppResources.Subscription_Error_BillingServiceNotAvailableForValidation);
                }

                // Move validation to background thread to prevent UI blocking
                var validationResult = await Task.Run(async () =>
                {
                    var purchases = await CrossInAppBilling.Current.GetPurchasesAsync(ItemType.Subscription).ConfigureAwait(false);

                    if (purchases?.Any() == true)
                    {
                        // Process all purchases in parallel for better performance
                        var updateTasks = purchases.Select(async purchase =>
                        {
                            var existingSubscription = await _subscriptionRepository.GetByPurchaseTokenAsync(purchase.PurchaseToken, cancellationToken).ConfigureAwait(false);
                            if (existingSubscription.IsSuccess && existingSubscription.Data != null)
                            {
                                var subscription = existingSubscription.Data;
                                subscription.UpdateStatus(purchase.State == PurchaseState.Purchased ? SubscriptionStatus.Active : SubscriptionStatus.Expired);
                                await _subscriptionRepository.UpdateAsync(subscription, cancellationToken).ConfigureAwait(false);
                            }
                        });

                        await Task.WhenAll(updateTasks).ConfigureAwait(false);
                    }

                    return true;

                }, cancellationToken).ConfigureAwait(false);

                return Result<bool>.Success(validationResult);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate subscription");
                return Result<bool>.Failure(AppResources.Subscription_Error_NetworkConnectivity);
            }
        }

        public async Task<Result<List<SubscriptionProductDto>>> GetAvailableProductsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Ensure we're connected before attempting to get products
                var initResult = await InitializeAsync(cancellationToken).ConfigureAwait(false);
                if (!initResult.IsSuccess || !initResult.Data)
                {
                    return Result<List<SubscriptionProductDto>>.Failure(AppResources.Subscription_Error_BillingServiceNotAvailable);
                }

                // Move product retrieval to background thread to prevent UI blocking
                var subscriptionProducts = await Task.Run(async () =>
                {
                    var productIds = new List<string> { "monthly_subscription", "yearly_subscription" };
                    var products = await CrossInAppBilling.Current.GetProductInfoAsync(ItemType.Subscription, productIds.ToArray()).ConfigureAwait(false);

                    if (products == null || !products.Any())
                    {
                        return null;
                    }

                    return products.Select(p => new SubscriptionProductDto
                    {
                        ProductId = p.ProductId,
                        Title = p.Name,
                        Description = p.Description,
                        Price = p.LocalizedPrice,
                        PriceAmountMicros = p.MicrosPrice.ToString(),
                        CurrencyCode = p.CurrencyCode,
                        Period = p.ProductId.Contains("monthly") ? Domain.Entities.SubscriptionPeriod.Monthly : Domain.Entities.SubscriptionPeriod.Yearly
                    }).ToList();

                }, cancellationToken).ConfigureAwait(false);

                if (subscriptionProducts == null)
                {
                    return Result<List<SubscriptionProductDto>>.Failure(AppResources.Subscription_Error_NoSubscriptionProductsAvailable);
                }

                return Result<List<SubscriptionProductDto>>.Success(subscriptionProducts);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get available products");
                return Result<List<SubscriptionProductDto>>.Failure(AppResources.Subscription_Error_NetworkConnectivity);
            }
        }

        public async Task DisconnectAsync()
        {
            try
            {
                // Move disconnection to background thread to prevent UI blocking
                await Task.Run(async () =>
                {
                    await CrossInAppBilling.Current.DisconnectAsync().ConfigureAwait(false);
                }).ConfigureAwait(false);

                _isConnected = false;
                _lastConnectionCheck = DateTime.MinValue;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to disconnect from billing service");
                _isConnected = false;
            }
        }

        public async Task<Result<bool>> StoreSubscriptionInSettingsAsync(ProcessSubscriptionResultDto subscriptionData, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // This method can be implemented here or delegated to the command
                // For now, we'll return success as the ProcessSubscriptionCommand handles this
                return await Task.FromResult(Result<bool>.Success(true)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to store subscription in settings");
                return Result<bool>.Failure(AppResources.Subscription_Error_FailedToStoreSubscriptionInSettings);
            }
        }

        private DateTime CalculateExpirationDate(string productId, DateTime purchaseDate)
        {
            return productId.Contains("monthly")
                ? purchaseDate.AddMonths(1)
                : purchaseDate.AddYears(1);
        }

        private async Task<string> GetCurrentUserIdAsync()
        {
            try
            {
                var userId = await SecureStorage.GetAsync("UniqueID").ConfigureAwait(false);
                return userId ?? Guid.NewGuid().ToString();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get user ID from secure storage, generating new one");
                return Guid.NewGuid().ToString();
            }
        }
    }
}