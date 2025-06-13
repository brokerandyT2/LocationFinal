using FluentValidation;
using Location.Photography.Application.Resources;

namespace Location.Photography.Application.Commands.Subscription
{
    public class ProcessSubscriptionCommandValidator : AbstractValidator<ProcessSubscriptionCommand>
    {
        public ProcessSubscriptionCommandValidator()
        {
            RuleFor(x => x.ProductId)
                .NotEmpty()
                .WithMessage(AppResources.Subscription_ValidationError_ProductIdRequired)
                .Must(BeValidProductId)
                .WithMessage(AppResources.Subscription_ValidationError_InvalidProductId);

            RuleFor(x => x.Period)
                .IsInEnum()
                .WithMessage(AppResources.Subscription_ValidationError_PeriodRequired);
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
    }
}