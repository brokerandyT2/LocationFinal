using FluentValidation;

namespace Location.Core.Application.Settings.Commands.DeleteSetting
{
    public class DeleteSettingCommandValidator : AbstractValidator<DeleteSettingCommand>
    {
        public DeleteSettingCommandValidator()
        {
            RuleFor(x => x.Key)
                .NotEmpty().WithMessage("Key is required");
        }
    }
}