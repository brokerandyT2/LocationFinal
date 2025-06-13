using FluentValidation;
using Location.Core.Application.Resources;

namespace Location.Core.Application.Tips.Commands.DeleteTip
{
    /// <summary>
    /// Provides validation logic for the <see cref="DeleteTipCommand"/>.
    /// </summary>
    /// <remarks>This validator ensures that the <see cref="DeleteTipCommand.Id"/> property meets the required
    /// constraints. Specifically, it verifies that the <c>Id</c> is greater than 0.</remarks>
    public class DeleteTipCommandValidator : AbstractValidator<DeleteTipCommand>
    {
        /// <summary>
        /// Validates the properties of a command to delete a tip.
        /// </summary>
        /// <remarks>This validator ensures that the command contains a valid identifier for the tip to be
        /// deleted.</remarks>
        public DeleteTipCommandValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage(AppResources.Tip_ValidationError_IdRequired);
        }
    }
}