using FluentValidation;

namespace Location.Core.Application.Commands.Locations
{
    public class RestoreLocationCommandValidator : AbstractValidator<RestoreLocationCommand>
    {
        public RestoreLocationCommandValidator()
        {
            RuleFor(x => x.LocationId)
                .GreaterThan(0).WithMessage("LocationId must be greater than 0");
        }
    }
}