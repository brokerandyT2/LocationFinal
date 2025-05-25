// Location.Photography.ViewModels/Events/SubscriptionUpgradeRequestedEventArgs.cs
using System;

namespace Location.Photography.ViewModels.Events
{
    public class SubscriptionUpgradeRequestedEventArgs : EventArgs
    {
        public string RequiredSubscription { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string FeatureName { get; set; } = string.Empty;
    }
}