// Location.Photography.Infrastructure/Repositories/SubscriptionRepository.cs
using Location.Core.Application.Common.Models;
using Location.Core.Infrastructure.Data;
using Location.Photography.Application.Common.Interfaces;
using Location.Photography.Domain.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Photography.Infrastructure.Repositories
{
    public class SubscriptionRepository : ISubscriptionRepository
    {
        private readonly IDatabaseContext _context;
        private readonly ILogger<SubscriptionRepository> _logger;

        public SubscriptionRepository(IDatabaseContext context, ILogger<SubscriptionRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<Subscription>> CreateAsync(Subscription subscription, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (subscription == null)
                {
                    return Result<Subscription>.Failure("Subscription cannot be null");
                }

                await _context.InsertAsync(subscription);

                _logger.LogInformation("Created subscription with ID: {SubscriptionId}", subscription.Id);
                return Result<Subscription>.Success(subscription);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating subscription");
                return Result<Subscription>.Failure($"Error creating subscription: {ex.Message}");
            }
        }

        public async Task<Result<Subscription>> GetActiveSubscriptionAsync(string userId, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Result<Subscription>.Failure("User ID cannot be null or empty");
                }

                var subscriptions = await _context.Table<Subscription>()
                    .Where(s => s.UserId == userId &&
                               s.Status == SubscriptionStatus.Active &&
                               s.ExpirationDate > DateTime.UtcNow)
                    .OrderByDescending(s => s.ExpirationDate)
                    .ToListAsync();

                var subscription = subscriptions.FirstOrDefault();

                if (subscription == null)
                {
                    return Result<Subscription>.Failure("No active subscription found");
                }

                return Result<Subscription>.Success(subscription);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active subscription for user: {UserId}", userId);
                return Result<Subscription>.Failure($"Error retrieving active subscription: {ex.Message}");
            }
        }

        public async Task<Result<Subscription>> GetByTransactionIdAsync(string transactionId, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (string.IsNullOrWhiteSpace(transactionId))
                {
                    return Result<Subscription>.Failure("Transaction ID cannot be null or empty");
                }

                var subscriptions = await _context.Table<Subscription>()
                    .Where(s => s.TransactionId == transactionId)
                    .ToListAsync();

                var subscription = subscriptions.FirstOrDefault();

                if (subscription == null)
                {
                    return Result<Subscription>.Failure("Subscription not found");
                }

                return Result<Subscription>.Success(subscription);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting subscription by transaction ID: {TransactionId}", transactionId);
                return Result<Subscription>.Failure($"Error retrieving subscription: {ex.Message}");
            }
        }

        public async Task<Result<Subscription>> UpdateAsync(Subscription subscription, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (subscription == null)
                {
                    return Result<Subscription>.Failure("Subscription cannot be null");
                }

                await _context.UpdateAsync(subscription);

                _logger.LogInformation("Updated subscription with ID: {SubscriptionId}", subscription.Id);
                return Result<Subscription>.Success(subscription);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating subscription with ID: {SubscriptionId}", subscription?.Id);
                return Result<Subscription>.Failure($"Error updating subscription: {ex.Message}");
            }
        }

        public async Task<Result<Subscription>> GetByPurchaseTokenAsync(string purchaseToken, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (string.IsNullOrWhiteSpace(purchaseToken))
                {
                    return Result<Subscription>.Failure("Purchase token cannot be null or empty");
                }

                var subscriptions = await _context.Table<Subscription>()
                    .Where(s => s.PurchaseToken == purchaseToken)
                    .ToListAsync();

                var subscription = subscriptions.FirstOrDefault();

                if (subscription == null)
                {
                    return Result<Subscription>.Failure("Subscription not found");
                }

                return Result<Subscription>.Success(subscription);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting subscription by purchase token: {PurchaseToken}", purchaseToken);
                return Result<Subscription>.Failure($"Error retrieving subscription: {ex.Message}");
            }
        }
    }
}