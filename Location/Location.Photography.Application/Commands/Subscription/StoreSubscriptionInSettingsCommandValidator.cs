// Location.Photography.Application/Commands/Subscription/StoreSubscriptionInSettingsCommandValidator.cs
using FluentValidation;
using System;

namespace Location.Photography.Application.Commands.Subscription
{
    public class StoreSubscriptionInSettingsCommandValidator : AbstractValidator<StoreSubscriptionInSettingsCommand>
    {
        public StoreSubscriptionInSettingsCommandValidator()
        {
            RuleFor(x => x.ProductId)
                .NotEmpty()
                .WithMessage("Product ID is required")
                .Must(BeValidProductId)
                .WithMessage("Product ID must be a valid subscription product identifier");

            RuleFor(x => x.ExpirationDate)
                .NotEmpty()
                .WithMessage("Expiration date is required")
                .Must(BeFutureDate)
                .WithMessage("Expiration date must be in the future");

            RuleFor(x => x.PurchaseDate)
                .NotEmpty()
                .WithMessage("Purchase date is required")
                .Must(BeValidPurchaseDate)
                .WithMessage("Purchase date must be a valid date not in the future");

            RuleFor(x => x.TransactionId)
                .NotEmpty()
                .WithMessage("Transaction ID is required")
                .MinimumLength(5)
                .WithMessage("Transaction ID must be at least 5 characters long");

            RuleFor(x => x)
                .Must(HaveValidDateRange)
                .WithMessage("Purchase date must be before expiration date");
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