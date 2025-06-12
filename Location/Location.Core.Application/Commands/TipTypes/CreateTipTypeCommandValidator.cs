using FluentValidation;
using Location.Core.Application.Resources;

namespace Location.Core.Application.Commands.TipTypes
{
    /// <summary>
    /// Provides validation rules for the <see cref="CreateTipTypeCommand"/>.
    /// </summary>
    /// <remarks>This validator ensures that the <see cref="CreateTipTypeCommand.Name"/> and  <see
    /// cref="CreateTipTypeCommand.I8n"/> properties meet the required constraints.</remarks>
    public class CreateTipTypeCommandValidator : AbstractValidator<CreateTipTypeCommand>
    {
        /// <summary>
        /// Validates the properties of a <see cref="CreateTipTypeCommand"/> object to ensure they meet the required
        /// criteria.
        /// </summary>
        /// <remarks>This validator enforces the following rules: <list type="bullet">
        /// <item><description>The <c>Name</c> property must not be empty and must not exceed 100
        /// characters.</description></item> <item><description>The <c>I8n</c> property must not be empty and must not
        /// exceed 10 characters.</description></item> </list></remarks>
        public CreateTipTypeCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage(AppResources.TipType_ValidationError_NameRequired)
                .MaximumLength(100)
                .WithMessage(AppResources.TipType_ValidationError_NameMaxLength);

            RuleFor(x => x.I8n)
                .NotEmpty()
                .WithMessage(AppResources.TipType_ValidationError_LocalizationRequired)
                .MaximumLength(10)
                .WithMessage(AppResources.TipType_ValidationError_LocalizationMaxLength);
        }
    }
}