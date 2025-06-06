// Location.Photography.Application/Queries/SunLocation/GetCurrentSunPositionQueryValidator.cs
using FluentValidation;

namespace Location.Photography.Application.Queries.SunLocation
{
    public class GetCurrentSunPositionQueryValidator : AbstractValidator<GetCurrentSunPositionQuery>
    {
        public GetCurrentSunPositionQueryValidator()
        {
            RuleFor(x => x.Latitude)
                .InclusiveBetween(-90.0, 90.0)
                .WithMessage("Latitude must be between -90 and 90 degrees");

            RuleFor(x => x.Longitude)
                .InclusiveBetween(-180.0, 180.0)
                .WithMessage("Longitude must be between -180 and 180 degrees");

            RuleFor(x => x.DateTime)
                .NotEmpty()
                .WithMessage("DateTime is required")
                .Must(BeValidDateTime)
                .WithMessage("DateTime must be a valid date and time within reasonable range");
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