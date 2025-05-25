// Location.Photography.Application/Services/ISubscriptionService.cs
using Location.Core.Application.Common.Models;
using Location.Photography.Application.Commands.Subscription;
using Location.Photography.Application.Queries.Subscription;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Photography.Application.Services
{
    public interface ISubscriptionService
    {
        /// <summary>
        /// Initializes the billing service connection
        /// </summary>
        Task<Result<bool>> InitializeAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets available subscription products from the store
        /// </summary>
        Task<Result<List<SubscriptionProductDto>>> GetAvailableProductsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Processes a subscription purchase
        /// </summary>
        Task<Result<ProcessSubscriptionResultDto>> PurchaseSubscriptionAsync(string productId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Stores subscription data in local SQLite database
        /// </summary>
        Task<Result<bool>> StoreSubscriptionAsync(ProcessSubscriptionResultDto subscriptionData, CancellationToken cancellationToken = default);

        /// <summary>
        /// Stores subscription data in settings table after successful purchase
        /// </summary>
        Task<Result<bool>> StoreSubscriptionInSettingsAsync(ProcessSubscriptionResultDto subscriptionData, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets current subscription status from local database
        /// </summary>
        Task<Result<SubscriptionStatusDto>> GetCurrentSubscriptionStatusAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates subscription with the store and updates local data
        /// </summary>
        Task<Result<bool>> ValidateAndUpdateSubscriptionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Disconnects from billing service
        /// </summary>
        Task DisconnectAsync();
    }
}