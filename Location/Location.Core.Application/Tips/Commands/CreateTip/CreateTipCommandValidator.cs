using FluentValidation;

namespace Location.Core.Application.Tips.Commands.CreateTip
{
    public class CreateTipCommandValidator : AbstractValidator<CreateTipCommand>
    {
        public CreateTipCommandValidator()
        {
            RuleFor(x => x.TipTypeId)
                .GreaterThan(0).WithMessage("TipTypeId must be greater than 0");

            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title is required")
                .MaximumLength(100).WithMessage("Title must not exceed 100 characters");

            RuleFor(x => x.Content)
                .NotEmpty().WithMessage("Content is required")
                .MaximumLength(1000).WithMessage("Content must not exceed 1000 characters");

            RuleFor(x => x.Fstop)
                .MaximumLength(20).WithMessage("F-stop must not exceed 20 characters");

            RuleFor(x => x.ShutterSpeed)
                .MaximumLength(20).WithMessage("Shutter speed must not exceed 20 characters");

            RuleFor(x => x.Iso)
                .MaximumLength(20).WithMessage("ISO must not exceed 20 characters");

            RuleFor(x => x.I8n)
                .NotEmpty().WithMessage("Localization is required")
                .MaximumLength(10).WithMessage("Localization must not exceed 10 characters");
        }
    }
}