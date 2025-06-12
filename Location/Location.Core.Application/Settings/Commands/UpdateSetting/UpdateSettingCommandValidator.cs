using FluentValidation;
using Location.Core.Application.Resources;

namespace Location.Core.Application.Settings.Commands.UpdateSetting
{
    /// <summary>
    /// Provides validation rules for the <see cref="UpdateSettingCommand"/>.
    /// </summary>
    /// <remarks>This validator ensures that the <see cref="UpdateSettingCommand.Key"/> is not empty and does
    /// not exceed 50 characters, and that the <see cref="UpdateSettingCommand.Value"/> is not null and does not exceed
    /// 500 characters. Validation errors will include descriptive messages for any violations.</remarks>
    public class UpdateSettingCommandValidator : AbstractValidator<UpdateSettingCommand>
    {
        /// <summary>
        /// Validates the properties of an <see cref="UpdateSettingCommand"/> to ensure they meet the required
        /// constraints.
        /// </summary>
        /// <remarks>This validator enforces the following rules: <list type="bullet">
        /// <item><description>The <c>Key</c> property must not be empty and must not exceed 50
        /// characters.</description></item> <item><description>The <c>Value</c> property must not be null and must not
        /// exceed 500 characters.</description></item> </list></remarks>
        public UpdateSettingCommandValidator()
        {
            RuleFor(x => x.Key)
                .NotEmpty().WithMessage(AppResources.Setting_ValidationError_KeyRequired)
                .MaximumLength(50).WithMessage(AppResources.Setting_ValidationError_KeyMaxLength);

            RuleFor(x => x.Value)
                .NotNull().WithMessage(AppResources.Setting_ValidationError_ValueRequired)
                .MaximumLength(500).WithMessage(AppResources.Setting_ValidationError_ValueMaxLength);
        }
    }
}