using FluentValidation;

namespace Location.Core.Application.Queries.Locations
{
    public class GetLocationByTitleQueryValidator : AbstractValidator<GetLocationByTitleQuery>
    {
        public GetLocationByTitleQueryValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title is required")
                .MaximumLength(100).WithMessage("Title must not exceed 100 characters");
        }
    }
}