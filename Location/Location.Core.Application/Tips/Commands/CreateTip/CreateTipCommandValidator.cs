using FluentValidation;
using Location.Core.Application.Resources;

namespace Location.Core.Application.Tips.Commands.CreateTip
{
    /// <summary>
    /// Provides validation rules for the <see cref="CreateTipCommand"/> object.
    /// </summary>
    /// <remarks>This validator ensures that all required properties of the <see cref="CreateTipCommand"/>
    /// object meet the specified constraints. It validates the following: <list type="bullet">
    /// <item><description><c>TipTypeId</c> must be greater than 0.</description></item> <item><description><c>Title</c>
    /// is required and must not exceed 100 characters.</description></item> <item><description><c>Content</c> is
    /// required and must not exceed 1000 characters.</description></item> <item><description><c>Fstop</c>,
    /// <c>ShutterSpeed</c>, and <c>Iso</c> must not exceed 20 characters each.</description></item>
    /// <item><description><c>I8n</c> (localization) is required and must not exceed 10 characters.</description></item>
    /// </list></remarks>
    public class CreateTipCommandValidator : AbstractValidator<CreateTipCommand>
    {
        /// <summary>
        /// Provides validation rules for the <c>CreateTipCommand</c> object.
        /// </summary>
        /// <remarks>This validator ensures that all required fields in the <c>CreateTipCommand</c> object
        /// are properly populated and meet the specified constraints. Validation rules include checks for non-empty
        /// fields, maximum lengths, and specific value ranges where applicable.</remarks>
        public CreateTipCommandValidator()
        {
            RuleFor(x => x.TipTypeId)
                .GreaterThan(0).WithMessage(AppResources.TipType_ValidationError_IdRequired);

            RuleFor(x => x.Title)
                .NotEmpty().WithMessage(AppResources.Tip_ValidationError_TitleRequired)
                .MaximumLength(100).WithMessage(AppResources.Tip_ValidationError_TitleMaxLength);

            RuleFor(x => x.Content)
                .NotEmpty().WithMessage(AppResources.Tip_ValidationError_ContentRequired)
                .MaximumLength(1000).WithMessage(AppResources.Tip_ValidationError_ContentMaxLength);

            RuleFor(x => x.Fstop)
                .MaximumLength(20).WithMessage(AppResources.Tip_ValidationError_FStopMaxLength);

            RuleFor(x => x.ShutterSpeed)
                .MaximumLength(20).WithMessage(AppResources.Tip_ValidationError_ShutterSpeedMaxLength);

            RuleFor(x => x.Iso)
                .MaximumLength(20).WithMessage(AppResources.Tip_ValidationError_IsoMaxLength);

            RuleFor(x => x.I8n)
                .NotEmpty().WithMessage(AppResources.Tip_ValidationError_LocalizationRequired)
                .MaximumLength(10).WithMessage(AppResources.Tip_ValidationError_LocalizationMaxLength);
        }
    }
}