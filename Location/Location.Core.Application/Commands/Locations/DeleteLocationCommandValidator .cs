
using FluentValidation;

namespace Location.Core.Application.Commands.Locations
{
    public class DeleteLocationCommandValidator : AbstractValidator<DeleteLocationCommand>
    {
        public DeleteLocationCommandValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("Location ID must be greater than 0");
        }
    }
}