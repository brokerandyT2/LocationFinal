// Location.Photography.Infrastructure/Services/SubscriptionService.cs
using Location.Core.Application.Common.Models;
using Location.Photography.Application.Commands.Subscription;
using Location.Photography.Application.Common.Interfaces;
using Location.Photography.Application.Queries.Subscription;
using Location.Photography.Application.Services;
using Location.Photography.Domain.Entities;
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

                var connected = await CrossInAppBilling.Current.ConnectAsync();
                return Result<bool>.Success(connected);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize billing service");
                return Result<bool>.Failure("Network connectivity issue. Please check your connection and try again.");
            }
        }

        public async Task<Result<List<SubscriptionProductDto>>> GetAvailableProductsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var productIds = new List<string> { "monthly_subscription", "yearly_subscription" };
                var products = await CrossInAppBilling.Current.GetProductInfoAsync(ItemType.Subscription, productIds.ToArray());

                if (products == null || !products.Any())
                {
                    return Result<List<SubscriptionProductDto>>.Failure("No subscription products available");
                }

                var subscriptionProducts = products.Select(p => new SubscriptionProductDto
                {
                    ProductId = p.ProductId,
                    Title = p.Name,
                    Description = p.Description,
                    Price = p.LocalizedPrice,
                    PriceAmountMicros = p.MicrosPrice.ToString(),
                    CurrencyCode = p.CurrencyCode,
                    Period = p.ProductId.Contains("monthly") ? Domain.Entities.SubscriptionPeriod.Monthly : Domain.Entities.SubscriptionPeriod.Yearly
                }).ToList();

                return Result<List<SubscriptionProductDto>>.Success(subscriptionProducts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get available products");
                return Result<List<SubscriptionProductDto>>.Failure("Network connectivity issue. Please check your connection and try again.");
            }
        }

        public async Task<Result<ProcessSubscriptionResultDto>> PurchaseSubscriptionAsync(string productId, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var purchase = await CrossInAppBilling.Current.PurchaseAsync(productId, ItemType.Subscription);

                if (purchase == null)
                {
                    return Result<ProcessSubscriptionResultDto>.Failure("There was an error processing your request, please try again");
                }

                var result = new ProcessSubscriptionResultDto
                {
                    IsSuccessful = purchase.State == PurchaseState.Purchased,
                    TransactionId = purchase.Id,
                    PurchaseToken = purchase.PurchaseToken,
                    PurchaseDate = purchase.TransactionDateUtc,
                    ProductId = purchase.ProductId,
                    Status = purchase.State == PurchaseState.Purchased ? SubscriptionStatus.Active : SubscriptionStatus.Failed,
                    ExpirationDate = CalculateExpirationDate(productId, purchase.TransactionDateUtc)
                };

                return Result<ProcessSubscriptionResultDto>.Success(result);
            }
            catch (InAppBillingPurchaseException ex)
            {
                _logger.LogWarning(ex, "Purchase failed: {Message}", ex.Message);
                return Result<ProcessSubscriptionResultDto>.Failure("There was an error processing your request, please try again");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to purchase subscription");
                return Result<ProcessSubscriptionResultDto>.Failure("Network connectivity issue. Please check your connection and try again.");
            }
        }

        public async Task<Result<bool>> StoreSubscriptionAsync(ProcessSubscriptionResultDto subscriptionData, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var userId = await GetCurrentUserIdAsync();
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

                var result = await _subscriptionRepository.CreateAsync(subscription, cancellationToken);
                return Result<bool>.Success(result.IsSuccess);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to store subscription");
                return Result<bool>.Failure("Failed to store subscription data");
            }
        }

        public async Task<Result<SubscriptionStatusDto>> GetCurrentSubscriptionStatusAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var userId = await GetCurrentUserIdAsync();
                var subscriptionResult = await _subscriptionRepository.GetActiveSubscriptionAsync(userId, cancellationToken);

                if (!subscriptionResult.IsSuccess || subscriptionResult.Data == null)
                {
                    return Result<SubscriptionStatusDto>.Success(new SubscriptionStatusDto
                    {
                        HasActiveSubscription = false,
                        Status = SubscriptionStatus.Expired
                    });
                }

                var subscription = subscriptionResult.Data;
                var status = new SubscriptionStatusDto
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

                return Result<SubscriptionStatusDto>.Success(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get subscription status");
                return Result<SubscriptionStatusDto>.Failure("Failed to retrieve subscription status");
            }
        }

        public async Task<Result<bool>> ValidateAndUpdateSubscriptionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var purchases = await CrossInAppBilling.Current.GetPurchasesAsync(ItemType.Subscription);

                if (purchases?.Any() == true)
                {
                    foreach (var purchase in purchases)
                    {
                        var existingSubscription = await _subscriptionRepository.GetByPurchaseTokenAsync(purchase.PurchaseToken, cancellationToken);
                        if (existingSubscription.IsSuccess && existingSubscription.Data != null)
                        {
                            var subscription = existingSubscription.Data;
                            subscription.UpdateStatus(purchase.State == PurchaseState.Purchased ? SubscriptionStatus.Active : SubscriptionStatus.Expired);
                            await _subscriptionRepository.UpdateAsync(subscription, cancellationToken);
                        }
                    }
                }

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate subscription");
                return Result<bool>.Failure("Network connectivity issue. Please check your connection and try again.");
            }
        }

        public async Task DisconnectAsync()
        {
            try
            {
                await CrossInAppBilling.Current.DisconnectAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to disconnect from billing service");
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
            return await SecureStorage.GetAsync("UniqueID") ?? Guid.NewGuid().ToString();
        }

        public async Task<Result<bool>> StoreSubscriptionInSettingsAsync(ProcessSubscriptionResultDto subscriptionData, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // This method can be implemented here or delegated to the command
                // For now, we'll return success as the ProcessSubscriptionCommand handles this
                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to store subscription in settings");
                return Result<bool>.Failure("Failed to store subscription in settings");
            }
        }
    }
}