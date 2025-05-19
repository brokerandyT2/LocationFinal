using FluentValidation;

namespace Location.Core.Application.Queries.Locations
{
    /// <summary>
    /// Validates the <see cref="GetLocationByTitleQuery"/> to ensure its properties meet the required criteria.
    /// </summary>
    /// <remarks>This validator enforces the following rules for the <c>Title</c> property: <list
    /// type="bullet"> <item><description>The <c>Title</c> must not be empty.</description></item>
    /// <item><description>The <c>Title</c> must not exceed 100 characters in length.</description></item>
    /// </list></remarks>
    public class GetLocationByTitleQueryValidator : AbstractValidator<GetLocationByTitleQuery>
    {
        /// <summary>
        /// Validates the properties of a <see cref="GetLocationByTitleQuery"/> instance.
        /// </summary>
        /// <remarks>This validator ensures that the <c>Title</c> property of the query is not empty and
        /// does not exceed 100 characters.</remarks>
        public GetLocationByTitleQueryValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title is required")
                .MaximumLength(100).WithMessage("Title must not exceed 100 characters");
        }
    }
}