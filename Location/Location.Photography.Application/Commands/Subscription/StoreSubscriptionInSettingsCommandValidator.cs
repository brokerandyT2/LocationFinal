using FluentValidation;
using Location.Photography.Application.Resources;

namespace Location.Photography.Application.Commands.Subscription
{
    public class StoreSubscriptionInSettingsCommandValidator : AbstractValidator<StoreSubscriptionInSettingsCommand>
    {
        public StoreSubscriptionInSettingsCommandValidator()
        {
            RuleFor(x => x.ProductId)
                .NotEmpty()
                .WithMessage(AppResources.Subscription_ValidationError_ProductIdRequired)
                .Must(BeValidProductId)
                .WithMessage(AppResources.Subscription_ValidationError_InvalidProductId);

            RuleFor(x => x.ExpirationDate)
                .NotEmpty()
                .WithMessage(AppResources.Subscription_ValidationError_ExpirationDateRequired)
                .Must(BeFutureDate)
                .WithMessage(AppResources.Subscription_ValidationError_ExpirationDateFuture);

            RuleFor(x => x.PurchaseDate)
                .NotEmpty()
                .WithMessage(AppResources.Subscription_ValidationError_PurchaseDateRequired)
                .Must(BeValidPurchaseDate)
                .WithMessage(AppResources.Subscription_ValidationError_InvalidPurchaseDate);

            RuleFor(x => x.TransactionId)
                .NotEmpty()
                .WithMessage(AppResources.Subscription_ValidationError_TransactionIdRequired)
                .MinimumLength(5)
                .WithMessage(AppResources.Subscription_ValidationError_TransactionIdMinLength);

            RuleFor(x => x)
                .Must(HaveValidDateRange)
                .WithMessage(AppResources.Subscription_ValidationError_InvalidDateRange);
        }

        private bool BeValidProductId(string productId)
        {
            if (string.IsNullOrWhiteSpace(productId))
                return false;

            // Basic validation - product ID should contain expected subscription identifiers
            var validPrefixes = new[] { "monthly", "yearly", "premium", "pro", "subscription" };

            var lowerProductId = productId.ToLowerInvariant();

            foreach (var prefix in validPrefixes)
            {
                if (lowerProductId.Contains(prefix))
                    return true;
            }

            return false;
        }

        private bool BeFutureDate(DateTime date)
        {
            return date > DateTime.UtcNow;
        }

        private bool BeValidPurchaseDate(DateTime date)
        {
            var minDate = new DateTime(2020, 1, 1); // Reasonable minimum date
            var maxDate = DateTime.UtcNow.AddDays(1); // Allow slight future tolerance for time zones

            return date >= minDate && date <= maxDate;
        }

        private bool HaveValidDateRange(StoreSubscriptionInSettingsCommand command)
        {
            return command.PurchaseDate < command.ExpirationDate;
        }
    }
}