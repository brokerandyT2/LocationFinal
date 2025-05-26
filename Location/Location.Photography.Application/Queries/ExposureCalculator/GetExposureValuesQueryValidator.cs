// Location.Photography.Application/Queries/ExposureCalculator/GetExposureValuesQueryValidator.cs
using FluentValidation;
using Location.Photography.Application.Services;

namespace Location.Photography.Application.Queries.ExposureCalculator
{
    public class GetExposureValuesQueryValidator : AbstractValidator<GetExposureValuesQuery>
    {
        public GetExposureValuesQueryValidator()
        {
            RuleFor(x => x.Increments)
                .IsInEnum()
                .WithMessage("Invalid exposure increment value. Must be Full, Half, or Third stops.");
        }
    }
}