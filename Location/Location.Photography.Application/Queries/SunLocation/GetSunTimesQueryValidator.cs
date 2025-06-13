using FluentValidation;
using Location.Photography.Application.Resources;

namespace Location.Photography.Application.Queries.SunLocation
{
    public class GetSunTimesQueryValidator : AbstractValidator<GetSunTimesQuery>
    {
        public GetSunTimesQueryValidator()
        {
            RuleFor(x => x.Latitude)
                .InclusiveBetween(-90.0, 90.0)
                .WithMessage(AppResources.SunLocation_ValidationError_LatitudeRange);

            RuleFor(x => x.Longitude)
                .InclusiveBetween(-180.0, 180.0)
                .WithMessage(AppResources.SunLocation_ValidationError_LongitudeRange);

            RuleFor(x => x.Date)
                .NotEmpty()
                .WithMessage(AppResources.SunLocation_ValidationError_DateRequired)
                .Must(BeValidDate)
                .WithMessage(AppResources.SunLocation_ValidationError_InvalidDateRange);
        }

        private bool BeValidDate(DateTime date)
        {
            // Ensure date is not default and within reasonable range for sun calculations
            var minDate = new DateTime(1900, 1, 1);
            var maxDate = new DateTime(2100, 12, 31);

            return date != default && date >= minDate && date <= maxDate;
        }
    }
}