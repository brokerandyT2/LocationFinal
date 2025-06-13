using FluentValidation;
using Location.Photography.Application.Resources;

namespace Location.Photography.Application.Queries.SunLocation
{
    public class GetSunPositionQueryValidator : AbstractValidator<GetSunPositionQuery>
    {
        public GetSunPositionQueryValidator()
        {
            RuleFor(x => x.Latitude)
                .InclusiveBetween(-90.0, 90.0)
                .WithMessage(AppResources.SunLocation_ValidationError_LatitudeRange);

            RuleFor(x => x.Longitude)
                .InclusiveBetween(-180.0, 180.0)
                .WithMessage(AppResources.SunLocation_ValidationError_LongitudeRange);

            RuleFor(x => x.DateTime)
                .NotEmpty()
                .WithMessage(AppResources.SunLocation_ValidationError_DateTimeRequired)
                .Must(BeValidDateTime)
                .WithMessage(AppResources.SunLocation_ValidationError_InvalidDateTimeRange);
        }

        private bool BeValidDateTime(DateTime dateTime)
        {
            // Ensure datetime is not default and within reasonable range for sun calculations
            var minDate = new DateTime(1900, 1, 1);
            var maxDate = new DateTime(2100, 12, 31);

            return dateTime != default && dateTime >= minDate && dateTime <= maxDate;
        }
    }
}