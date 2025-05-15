using FluentValidation;

namespace Location.Core.Application.Settings.Commands.CreateSetting
{
    public class CreateSettingCommandValidator : AbstractValidator<CreateSettingCommand>
    {
        public CreateSettingCommandValidator()
        {
            RuleFor(x => x.Key)
                .NotEmpty().WithMessage("Key is required")
                .MaximumLength(50).WithMessage("Key must not exceed 50 characters");

            RuleFor(x => x.Value)
                .NotNull().WithMessage("Value cannot be null")
                .MaximumLength(500).WithMessage("Value must not exceed 500 characters");

            RuleFor(x => x.Description)
                .MaximumLength(200).WithMessage("Description must not exceed 200 characters");
        }
    }
}