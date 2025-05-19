using FluentValidation;

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