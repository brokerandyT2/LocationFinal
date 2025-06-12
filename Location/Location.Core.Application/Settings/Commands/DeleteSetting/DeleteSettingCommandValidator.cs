using FluentValidation;
using Location.Core.Application.Resources;

namespace Location.Core.Application.Settings.Commands.DeleteSetting
{
    /// <summary>
    /// Provides validation logic for the <see cref="DeleteSettingCommand"/>.
    /// </summary>
    /// <remarks>This validator ensures that the <see cref="DeleteSettingCommand.Key"/> property is not
    /// empty.</remarks>
    public class DeleteSettingCommandValidator : AbstractValidator<DeleteSettingCommand>
    {
        /// <summary>
        /// Validates the <see cref="DeleteSettingCommand"/> to ensure it meets the required criteria.
        /// </summary>
        /// <remarks>This validator enforces that the <c>Key</c> property of the <see
        /// cref="DeleteSettingCommand"/>  is not empty. If the validation fails, an appropriate error message is
        /// provided.</remarks>
        public DeleteSettingCommandValidator()
        {
            RuleFor(x => x.Key)
                .NotEmpty().WithMessage(AppResources.Setting_ValidationError_KeyRequired);
        }
    }
}