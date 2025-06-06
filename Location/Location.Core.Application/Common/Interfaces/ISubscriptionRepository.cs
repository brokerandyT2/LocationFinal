// Location.Core.Application/Common/Interfaces/ISubscriptionRepository.cs
using Location.Core.Application.Common.Models;
using Location.Photography.Domain.Entities;

namespace Location.Core.Application.Common.Interfaces
{
    public interface ISubscriptionRepository
    {
        /// <summary>
        /// Creates a new subscription record
        /// </summary>
        Task<Result<Subscription>> CreateAsync(Subscription subscription, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the current active subscription for a user
        /// </summary>
        Task<Result<Subscription>> GetActiveSubscriptionAsync(string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets subscription by transaction ID
        /// </summary>
        Task<Result<Subscription>> GetByTransactionIdAsync(string transactionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing subscription
        /// </summary>
        Task<Result<Subscription>> UpdateAsync(Subscription subscription, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets subscription by purchase token
        /// </summary>
        Task<Result<Subscription>> GetByPurchaseTokenAsync(string purchaseToken, CancellationToken cancellationToken = default);
    }
}