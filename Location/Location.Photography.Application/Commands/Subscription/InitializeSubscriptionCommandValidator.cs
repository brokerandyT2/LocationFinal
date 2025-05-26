// Location.Photography.Application/Commands/Subscription/InitializeSubscriptionCommandValidator.cs
using FluentValidation;

namespace Location.Photography.Application.Commands.Subscription
{
    public class InitializeSubscriptionCommandValidator : AbstractValidator<InitializeSubscriptionCommand>
    {
        public InitializeSubscriptionCommandValidator()
        {
            // No validation rules needed for initialization command
            // The command has no parameters to validate
        }
    }
}