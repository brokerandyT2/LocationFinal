// Location.Photography.Application/Commands/Subscription/ProcessSubscriptionCommandValidator.cs
using FluentValidation;

namespace Location.Photography.Application.Commands.Subscription
{
    public class ProcessSubscriptionCommandValidator : AbstractValidator<ProcessSubscriptionCommand>
    {
        public ProcessSubscriptionCommandValidator()
        {
            RuleFor(x => x.ProductId)
                .NotEmpty()
                .WithMessage("Product ID is required")
                .Must(BeValidProductId)
                .WithMessage("Product ID must be a valid subscription product identifier");

            RuleFor(x => x.Period)
                .IsInEnum()
                .WithMessage("Subscription period must be Monthly or Yearly");
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