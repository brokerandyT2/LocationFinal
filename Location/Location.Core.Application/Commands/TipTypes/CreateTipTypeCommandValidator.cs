using FluentValidation;

namespace Location.Core.Application.Commands.TipTypes
{
    public class CreateTipTypeCommandValidator : AbstractValidator<CreateTipTypeCommand>
    {
        public CreateTipTypeCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required")
                .MaximumLength(100).WithMessage("Name must not exceed 100 characters");

            RuleFor(x => x.I8n)
                .NotEmpty().WithMessage("Localization is required")
                .MaximumLength(10).WithMessage("Localization must not exceed 10 characters");
        }
    }
}