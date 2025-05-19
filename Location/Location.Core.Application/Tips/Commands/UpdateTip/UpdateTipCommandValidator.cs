using FluentValidation;

namespace Location.Core.Application.Tips.Commands.UpdateTip
{
    /// <summary>
    /// Provides validation rules for the <see cref="UpdateTipCommand"/> class.
    /// </summary>
    /// <remarks>This validator ensures that all required properties of the <see cref="UpdateTipCommand"/> are
    /// provided and meet the specified constraints. Validation includes checks for non-empty fields, maximum lengths,
    /// and numeric constraints where applicable.</remarks>
    public class UpdateTipCommandValidator : AbstractValidator<UpdateTipCommand>
    {
        /// <summary>
        /// Validates the properties of an <see cref="UpdateTipCommand"/> to ensure they meet the required rules.
        /// </summary>
        /// <remarks>This validator enforces the following rules: <list type="bullet">
        /// <item><description><c>Id</c> must be greater than 0.</description></item>
        /// <item><description><c>TipTypeId</c> must be greater than 0.</description></item>
        /// <item><description><c>Title</c> is required and must not exceed 100 characters.</description></item>
        /// <item><description><c>Content</c> is required and must not exceed 1000 characters.</description></item>
        /// <item><description><c>Fstop</c> must not exceed 20 characters.</description></item>
        /// <item><description><c>ShutterSpeed</c> must not exceed 20 characters.</description></item>
        /// <item><description><c>Iso</c> must not exceed 20 characters.</description></item>
        /// <item><description><c>I8n</c> (localization) is required and must not exceed 10
        /// characters.</description></item> </list></remarks>
        public UpdateTipCommandValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("Id must be greater than 0");

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