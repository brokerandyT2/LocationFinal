// Location.Photography.Application/Queries/SunLocation/EnhancedSunQueryValidators.cs
using FluentValidation;
using System;

namespace Location.Photography.Application.Queries.SunLocation
{
    public class GetEnhancedSunTimesQueryValidator : AbstractValidator<GetEnhancedSunTimesQuery>
    {
        public GetEnhancedSunTimesQueryValidator()
        {
            RuleFor(x => x.Latitude)
                .InclusiveBetween(-90.0, 90.0)
                .WithMessage("Latitude must be between -90 and 90 degrees");

            RuleFor(x => x.Longitude)
                .InclusiveBetween(-180.0, 180.0)
                .WithMessage("Longitude must be between -180 and 180 degrees");

            RuleFor(x => x.Date)
                .NotEmpty()
                .WithMessage("Date is required")
                .Must(BeValidDate)
                .WithMessage("Date must be a valid date within reasonable range");
        }

        private bool BeValidDate(DateTime date)
        {
            var minDate = new DateTime(1900, 1, 1);
            var maxDate = new DateTime(2100, 12, 31);
            return date != default && date >= minDate && date <= maxDate;
        }
    }

    public class GetMoonDataQueryValidator : AbstractValidator<GetMoonDataQuery>
    {
        public GetMoonDataQueryValidator()
        {
            RuleFor(x => x.Latitude)
                .InclusiveBetween(-90.0, 90.0)
                .WithMessage("Latitude must be between -90 and 90 degrees");

            RuleFor(x => x.Longitude)
                .InclusiveBetween(-180.0, 180.0)
                .WithMessage("Longitude must be between -180 and 180 degrees");

            RuleFor(x => x.Date)
                .NotEmpty()
                .WithMessage("Date is required")
                .Must(BeValidDate)
                .WithMessage("Date must be a valid date within reasonable range");
        }

        private bool BeValidDate(DateTime date)
        {
            var minDate = new DateTime(1900, 1, 1);
            var maxDate = new DateTime(2100, 12, 31);
            return date != default && date >= minDate && date <= maxDate;
        }
    }

    public class GetSunPathDataQueryValidator : AbstractValidator<GetSunPathDataQuery>
    {
        public GetSunPathDataQueryValidator()
        {
            RuleFor(x => x.Latitude)
                .InclusiveBetween(-90.0, 90.0)
                .WithMessage("Latitude must be between -90 and 90 degrees");

            RuleFor(x => x.Longitude)
                .InclusiveBetween(-180.0, 180.0)
                .WithMessage("Longitude must be between -180 and 180 degrees");

            RuleFor(x => x.Date)
                .NotEmpty()
                .WithMessage("Date is required")
                .Must(BeValidDate)
                .WithMessage("Date must be a valid date within reasonable range");

            RuleFor(x => x.IntervalMinutes)
                .InclusiveBetween(1, 60)
                .WithMessage("Interval minutes must be between 1 and 60");
        }

        private bool BeValidDate(DateTime date)
        {
            var minDate = new DateTime(1900, 1, 1);
            var maxDate = new DateTime(2100, 12, 31);
            return date != default && date >= minDate && date <= maxDate;
        }
    }

    public class GetOptimalShootingTimesQueryValidator : AbstractValidator<GetOptimalShootingTimesQuery>
    {
        public GetOptimalShootingTimesQueryValidator()
        {
            RuleFor(x => x.Latitude)
                .InclusiveBetween(-90.0, 90.0)
                .WithMessage("Latitude must be between -90 and 90 degrees");

            RuleFor(x => x.Longitude)
                .InclusiveBetween(-180.0, 180.0)
                .WithMessage("Longitude must be between -180 and 180 degrees");

            RuleFor(x => x.Date)
                .NotEmpty()
                .WithMessage("Date is required")
                .Must(BeValidDate)
                .WithMessage("Date must be a valid date within reasonable range");
        }

        private bool BeValidDate(DateTime date)
        {
            var minDate = new DateTime(1900, 1, 1);
            var maxDate = new DateTime(2100, 12, 31);
            return date != default && date >= minDate && date <= maxDate;
        }
    }

    public class GetShadowCalculationQueryValidator : AbstractValidator<GetShadowCalculationQuery>
    {
        public GetShadowCalculationQueryValidator()
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

            RuleFor(x => x.ObjectHeight)
                .GreaterThan(0)
                .WithMessage("Object height must be greater than 0")
                .LessThanOrEqualTo(1000)
                .WithMessage("Object height must be less than or equal to 1000 meters");

            RuleFor(x => x.TerrainType)
                .IsInEnum()
                .WithMessage("Invalid terrain type");
        }

        private bool BeValidDateTime(DateTime dateTime)
        {
            var minDate = new DateTime(1900, 1, 1);
            var maxDate = new DateTime(2100, 12, 31);
            return dateTime != default && dateTime >= minDate && dateTime <= maxDate;
        }
    }
}