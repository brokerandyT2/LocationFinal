// Location.Photography.Infrastructure/Repositories/SubscriptionRepository.cs
using Location.Core.Application.Common.Models;
using Location.Core.Infrastructure.Data;
using Location.Photography.Application.Common.Interfaces;
using Location.Photography.Domain.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Photography.Infrastructure.Repositories
{
    public class SubscriptionRepository : ISubscriptionRepository
    {
        private readonly IDatabaseContext _context;
        private readonly ILogger<SubscriptionRepository> _logger;

        // Cache for frequently accessed subscriptions to reduce database calls
        private readonly ConcurrentDictionary<string, (Subscription subscription, DateTime expiry)> _subscriptionCache = new();
        private readonly TimeSpan _cacheTimeout = TimeSpan.FromMinutes(5);

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

                // Execute database operation on background thread to prevent UI blocking
                await Task.Run(async () =>
                {
                    await _context.InsertAsync(subscription).ConfigureAwait(false);
                }, cancellationToken).ConfigureAwait(false);

                // Clear cache for this user since we've added a new subscription
                InvalidateCacheForUser(subscription.UserId);

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

                // Check cache first to reduce database calls
                var cacheKey = $"active_{userId}";
                if (_subscriptionCache.TryGetValue(cacheKey, out var cached) && DateTime.UtcNow < cached.expiry)
                {
                    return Result<Subscription>.Success(cached.subscription);
                }

                // Execute database query on background thread to prevent UI blocking
                var subscription = await Task.Run(async () =>
                {
                    var subscriptions = await _context.Table<Subscription>()
                        .Where(s => s.UserId == userId &&
                                   s.Status == SubscriptionStatus.Active &&
                                   s.ExpirationDate > DateTime.UtcNow)
                        .OrderByDescending(s => s.ExpirationDate)
                        .ToListAsync().ConfigureAwait(false);

                    return subscriptions.FirstOrDefault();
                }, cancellationToken).ConfigureAwait(false);

                if (subscription == null)
                {
                    return Result<Subscription>.Failure("No active subscription found");
                }

                // Cache the result for future calls
                _subscriptionCache[cacheKey] = (subscription, DateTime.UtcNow.Add(_cacheTimeout));

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

                // Check cache first
                var cacheKey = $"transaction_{transactionId}";
                if (_subscriptionCache.TryGetValue(cacheKey, out var cached) && DateTime.UtcNow < cached.expiry)
                {
                    return Result<Subscription>.Success(cached.subscription);
                }

                // Execute database query on background thread to prevent UI blocking
                var subscription = await Task.Run(async () =>
                {
                    var subscriptions = await _context.Table<Subscription>()
                        .Where(s => s.TransactionId == transactionId)
                        .ToListAsync().ConfigureAwait(false);

                    return subscriptions.FirstOrDefault();
                }, cancellationToken).ConfigureAwait(false);

                if (subscription == null)
                {
                    return Result<Subscription>.Failure("Subscription not found");
                }

                // Cache the result
                _subscriptionCache[cacheKey] = (subscription, DateTime.UtcNow.Add(_cacheTimeout));

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

                // Execute database operation on background thread to prevent UI blocking
                await Task.Run(async () =>
                {
                    await _context.UpdateAsync(subscription).ConfigureAwait(false);
                }, cancellationToken).ConfigureAwait(false);

                // Clear cache for this user since subscription data has changed
                InvalidateCacheForUser(subscription.UserId);

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

                // Check cache first
                var cacheKey = $"token_{purchaseToken}";
                if (_subscriptionCache.TryGetValue(cacheKey, out var cached) && DateTime.UtcNow < cached.expiry)
                {
                    return Result<Subscription>.Success(cached.subscription);
                }

                // Execute database query on background thread to prevent UI blocking
                var subscription = await Task.Run(async () =>
                {
                    var subscriptions = await _context.Table<Subscription>()
                        .Where(s => s.PurchaseToken == purchaseToken)
                        .ToListAsync().ConfigureAwait(false);

                    return subscriptions.FirstOrDefault();
                }, cancellationToken).ConfigureAwait(false);

                if (subscription == null)
                {
                    return Result<Subscription>.Failure("Subscription not found");
                }

                // Cache the result
                _subscriptionCache[cacheKey] = (subscription, DateTime.UtcNow.Add(_cacheTimeout));

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

        /// <summary>
        /// Bulk operation to retrieve multiple subscriptions efficiently
        /// </summary>
        public async Task<Result<List<Subscription>>> GetSubscriptionsByUserIdsAsync(
            IEnumerable<string> userIds,
            CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (userIds == null || !userIds.Any())
                {
                    return Result<List<Subscription>>.Success(new List<Subscription>());
                }

                var userIdList = userIds.ToList();

                // Execute bulk query on background thread
                var subscriptions = await Task.Run(async () =>
                {
                    var allSubscriptions = new List<Subscription>();

                    // Process in batches to avoid potential query size limits
                    const int batchSize = 100;
                    for (int i = 0; i < userIdList.Count; i += batchSize)
                    {
                        var batch = userIdList.Skip(i).Take(batchSize);
                        var batchSubscriptions = await _context.Table<Subscription>()
                            .Where(s => batch.Contains(s.UserId))
                            .ToListAsync().ConfigureAwait(false);

                        allSubscriptions.AddRange(batchSubscriptions);
                    }

                    return allSubscriptions;
                }, cancellationToken).ConfigureAwait(false);

                return Result<List<Subscription>>.Success(subscriptions);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving subscriptions for multiple users");
                return Result<List<Subscription>>.Failure($"Error retrieving subscriptions: {ex.Message}");
            }
        }

        /// <summary>
        /// Efficiently check if user has any active subscription without loading full entity
        /// </summary>
        public async Task<Result<bool>> HasActiveSubscriptionAsync(string userId, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Result<bool>.Success(false);
                }

                // Check cache first for performance
                var cacheKey = $"hasactive_{userId}";
                if (_subscriptionCache.TryGetValue(cacheKey, out var cached) && DateTime.UtcNow < cached.expiry)
                {
                    return Result<bool>.Success(cached.subscription != null);
                }

                // Execute efficient count query on background thread
                var hasActiveSubscription = await Task.Run(async () =>
                {
                    var count = await _context.Table<Subscription>()
                        .CountAsync(s => s.UserId == userId &&
                                   s.Status == SubscriptionStatus.Active &&
                                   s.ExpirationDate > DateTime.UtcNow).ConfigureAwait(false);

                    return count > 0;
                }, cancellationToken).ConfigureAwait(false);

                return Result<bool>.Success(hasActiveSubscription);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if user has active subscription: {UserId}", userId);
                return Result<bool>.Failure($"Error checking subscription status: {ex.Message}");
            }
        }

        /// <summary>
        /// Clear cache entries for a specific user when their subscription data changes
        /// </summary>
        private void InvalidateCacheForUser(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return;

            var keysToRemove = _subscriptionCache.Keys
                .Where(key => key.Contains(userId))
                .ToList();

            foreach (var key in keysToRemove)
            {
                _subscriptionCache.TryRemove(key, out _);
            }
        }

        /// <summary>
        /// Periodic cleanup of expired cache entries to prevent memory leaks
        /// </summary>
        public void CleanupExpiredCache()
        {
            var expiredKeys = _subscriptionCache
                .Where(kvp => DateTime.UtcNow >= kvp.Value.expiry)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                _subscriptionCache.TryRemove(key, out _);
            }

            if (expiredKeys.Any())
            {
                _logger.LogDebug("Cleaned up {Count} expired cache entries", expiredKeys.Count);
            }
        }
    }
}