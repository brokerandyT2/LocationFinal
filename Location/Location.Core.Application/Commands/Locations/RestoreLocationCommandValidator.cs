using FluentValidation;

namespace Location.Core.Application.Commands.Locations
{
    /// <summary>
    /// Provides validation logic for the <see cref="RestoreLocationCommand"/>.
    /// </summary>
    /// <remarks>This validator ensures that the <c>LocationId</c> property of the <see
    /// cref="RestoreLocationCommand"/>  meets the required constraints, such as being greater than 0.</remarks>
    public class RestoreLocationCommandValidator : AbstractValidator<RestoreLocationCommand>
    {
        /// <summary>
        /// Validates the properties of a command to restore a location.
        /// </summary>
        /// <remarks>This validator ensures that the <c>LocationId</c> property of the command meets the
        /// required conditions. Specifically, it checks that the <c>LocationId</c> is greater than 0.</remarks>
        public RestoreLocationCommandValidator()
        {
            RuleFor(x => x.LocationId)
                .GreaterThan(0).WithMessage("LocationId must be greater than 0");
        }
    }
}