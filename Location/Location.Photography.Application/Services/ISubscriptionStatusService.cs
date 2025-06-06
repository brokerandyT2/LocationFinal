// Location.Photography.Application/Services/ISubscriptionStatusService.cs
using Location.Core.Application.Common.Models;

namespace Location.Photography.Application.Services
{
    public interface ISubscriptionStatusService
    {
        /// <summary>
        /// Checks current subscription status with network-aware logic
        /// </summary>
        Task<Result<SubscriptionStatusResult>> CheckSubscriptionStatusAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Determines if user can access premium features (with grace period)
        /// </summary>
        Task<Result<bool>> CanAccessPremiumFeaturesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Determines if user can access professional features (with grace period)
        /// </summary>
        Task<Result<bool>> CanAccessProFeaturesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if subscription is in grace period
        /// </summary>
        Task<Result<bool>> IsInGracePeriodAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets subscription details from local settings
        /// </summary>
        Task<Result<LocalSubscriptionInfo>> GetLocalSubscriptionInfoAsync(CancellationToken cancellationToken = default);
    }

    public class SubscriptionStatusResult
    {
        public bool HasActiveSubscription { get; set; }
        public string SubscriptionType { get; set; } = string.Empty;
        public DateTime? ExpirationDate { get; set; }
        public bool IsInGracePeriod { get; set; }
        public bool NetworkCheckPerformed { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public bool IsError { get; set; }
    }

    public class LocalSubscriptionInfo
    {
        public string SubscriptionType { get; set; } = string.Empty;
        public DateTime? ExpirationDate { get; set; }
        public DateTime? PurchaseDate { get; set; }
        public string ProductId { get; set; } = string.Empty;
        public string TransactionId { get; set; } = string.Empty;
        public bool HasValidData { get; set; }
    }
}