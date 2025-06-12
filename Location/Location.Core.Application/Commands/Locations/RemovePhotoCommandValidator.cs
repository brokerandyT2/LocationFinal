using FluentValidation;
using Location.Core.Application.Resources;

namespace Location.Core.Application.Commands.Locations
{
    /// <summary>
    /// Provides validation rules for the <see cref="RemovePhotoCommand"/>.
    /// </summary>
    /// <remarks>This validator ensures that the <see cref="RemovePhotoCommand.LocationId"/> property meets
    /// the required constraints.</remarks>
    public class RemovePhotoCommandValidator : AbstractValidator<RemovePhotoCommand>
    {
        /// <summary>
        /// Validates the <see cref="RemovePhotoCommand"/> to ensure its properties meet the required conditions.
        /// </summary>
        /// <remarks>This validator enforces that the <c>LocationId</c> property of the command must be
        /// greater than 0.</remarks>
        public RemovePhotoCommandValidator()
        {
            RuleFor(x => x.LocationId)
                .GreaterThan(0)
                .WithMessage(AppResources.Location_ValidationError_LocationIdRequired);
        }
    }
}