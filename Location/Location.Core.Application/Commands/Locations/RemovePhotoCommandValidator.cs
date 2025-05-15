using FluentValidation;

namespace Location.Core.Application.Commands.Locations
{
    public class RemovePhotoCommandValidator : AbstractValidator<RemovePhotoCommand>
    {
        public RemovePhotoCommandValidator()
        {
            RuleFor(x => x.LocationId)
                .GreaterThan(0).WithMessage("LocationId must be greater than 0");
        }
    }
}