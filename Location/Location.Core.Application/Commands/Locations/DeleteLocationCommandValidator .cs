using FluentValidation;
using Location.Core.Application.Resources;

namespace Location.Core.Application.Commands.Locations
{
    /// <summary>
    /// Provides validation logic for the <see cref="DeleteLocationCommand"/>.
    /// </summary>
    /// <remarks>This validator ensures that the <see cref="DeleteLocationCommand.Id"/> property meets the
    /// required conditions before the command is processed.</remarks>
    public class DeleteLocationCommandValidator : AbstractValidator<DeleteLocationCommand>
    {
        /// <summary>
        /// Validates the <see cref="DeleteLocationCommand"/> to ensure it meets the required criteria.
        /// </summary>
        /// <remarks>This validator enforces that the location ID is greater than 0.</remarks>
        public DeleteLocationCommandValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0)
                .WithMessage(AppResources.Location_ValidationError_LocationIdRequired);
        }
    }
}