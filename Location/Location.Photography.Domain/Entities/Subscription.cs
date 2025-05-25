// Location.Photography.Domain/Entities/Subscription.cs
using SQLite;
using System;

namespace Location.Photography.Domain.Entities
{
    public class Subscription
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string ProductId { get; set; } = string.Empty;
        public string TransactionId { get; set; } = string.Empty;
        public string PurchaseToken { get; set; } = string.Empty;
        public DateTime PurchaseDate { get; set; }
        public DateTime ExpirationDate { get; set; }
        public SubscriptionStatus Status { get; set; }
        public SubscriptionPeriod Period { get; set; }
        public string UserId { get; set; } = string.Empty;
        public bool IsActive => Status == SubscriptionStatus.Active && ExpirationDate > DateTime.UtcNow;

        public Subscription() { } // EF Constructor

        public Subscription(
            string productId,
            string transactionId,
            string purchaseToken,
            DateTime purchaseDate,
            DateTime expirationDate,
            SubscriptionStatus status,
            SubscriptionPeriod period,
            string userId)
        {
            ProductId = productId ?? throw new ArgumentNullException(nameof(productId));
            TransactionId = transactionId ?? throw new ArgumentNullException(nameof(transactionId));
            PurchaseToken = purchaseToken ?? throw new ArgumentNullException(nameof(purchaseToken));
            PurchaseDate = purchaseDate;
            ExpirationDate = expirationDate;
            Status = status;
            Period = period;
            UserId = userId ?? throw new ArgumentNullException(nameof(userId));
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateStatus(SubscriptionStatus status)
        {
            Status = status;
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateExpiration(DateTime expirationDate)
        {
            ExpirationDate = expirationDate;
            UpdatedAt = DateTime.UtcNow;
        }

        public bool IsExpiringSoon(int daysThreshold = 7)
        {
            return IsActive && ExpirationDate.Subtract(DateTime.UtcNow).TotalDays <= daysThreshold;
        }

        public int DaysUntilExpiration()
        {
            if (!IsActive) return 0;
            return Math.Max(0, (int)ExpirationDate.Subtract(DateTime.UtcNow).TotalDays);
        }
    }

    public enum SubscriptionStatus
    {
        Active = 1,
        Expired = 2,
        Cancelled = 3,
        Pending = 4,
        Failed = 5
    }

    public enum SubscriptionPeriod
    {
        Monthly = 1,
        Yearly = 2
    }
}