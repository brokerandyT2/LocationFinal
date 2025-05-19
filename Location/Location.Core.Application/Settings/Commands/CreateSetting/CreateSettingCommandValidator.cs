using FluentValidation;

namespace Location.Core.Application.Settings.Commands.CreateSetting
{
    /// <summary>
    /// Provides validation rules for the <see cref="CreateSettingCommand"/> class.
    /// </summary>
    /// <remarks>This validator ensures that the <see cref="CreateSettingCommand.Key"/> property is not empty
    /// and does not exceed 50 characters, the <see cref="CreateSettingCommand.Value"/> property is not null and does
    /// not exceed 500 characters,  and the <see cref="CreateSettingCommand.Description"/> property does not exceed 200
    /// characters.</remarks>
    public class CreateSettingCommandValidator : AbstractValidator<CreateSettingCommand>
    {
        /// <summary>
        /// Validates the properties of a <c>CreateSettingCommand</c> object to ensure they meet the required
        /// constraints.
        /// </summary>
        /// <remarks>This validator enforces the following rules: <list type="bullet">
        /// <item><description>The <c>Key</c> property must not be empty and must not exceed 50
        /// characters.</description></item> <item><description>The <c>Value</c> property must not be null and must not
        /// exceed 500 characters.</description></item> <item><description>The <c>Description</c> property, if provided,
        /// must not exceed 200 characters.</description></item> </list></remarks>
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