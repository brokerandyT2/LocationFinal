using Location.Photography.Application.Resources;
namespace Location.Photography.Application.Common.Constants
{
    public static class SubscriptionConstants
    {
        // Subscription types
        public static readonly string Free = AppResources.SubscriptionType_Free;
        public static readonly string Pro = AppResources.SubscriptionType_Pro;
        public static readonly string Premium = AppResources.SubscriptionType_Premium;
        // Setting keys
        public static readonly string SubscriptionType = "SubscriptionType";
        public static readonly string SubscriptionExpiration = "SubscriptionExpiration";
        public static readonly string SubscriptionProductId = "SubscriptionProductId";
        public static readonly string SubscriptionPurchaseDate = "SubscriptionPurchaseDate";
        public static readonly string SubscriptionTransactionId = "SubscriptionTransactionId";
    }
}