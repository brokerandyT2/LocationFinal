using FluentValidation;
using Location.Photography.Application.Resources;

namespace Location.Photography.Application.Queries.ExposureCalculator
{
    public class GetExposureValuesQueryValidator : AbstractValidator<GetExposureValuesQuery>
    {
        public GetExposureValuesQueryValidator()
        {
            RuleFor(x => x.Increments)
                .IsInEnum()
                .WithMessage(AppResources.ExposureCalculator_ValidationError_IncrementRequired);
        }
    }
}