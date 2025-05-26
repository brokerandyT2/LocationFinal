// Location.Photography.Application/Commands/Subscription/CheckSubscriptionStatusCommandValidator.cs
using FluentValidation;

namespace Location.Photography.Application.Commands.Subscription
{
    public class CheckSubscriptionStatusCommandValidator : AbstractValidator<CheckSubscriptionStatusCommand>
    {
        public CheckSubscriptionStatusCommandValidator()
        {
            // No validation rules needed for status check command
            // The command has no parameters to validate
        }
    }
}