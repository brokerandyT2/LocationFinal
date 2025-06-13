// Location.Photography.Application/Queries/SunLocation/EnhancedSunQueryValidators.cs
using FluentValidation;
using Location.Photography.Application.Resources;

namespace Location.Photography.Application.Queries.SunLocation
{
    public class GetEnhancedSunTimesQueryValidator : AbstractValidator<GetEnhancedSunTimesQuery>
    {
        public GetEnhancedSunTimesQueryValidator()
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
                .WithMessage(AppResources.SunLocation_ValidationError_LatitudeRange);

            RuleFor(x => x.Longitude)
                .InclusiveBetween(-180.0, 180.0)
                .WithMessage(AppResources.SunLocation_ValidationError_LongitudeRange);

            RuleFor(x => x.Date)
                .NotEmpty()
                .WithMessage(AppResources.SunLocation_ValidationError_DateRequired)
                .Must(BeValidDate)
                .WithMessage(AppResources.SunLocation_ValidationError_InvalidDateRange);

            RuleFor(x => x.IntervalMinutes)
                .InclusiveBetween(1, 60)
                .WithMessage(AppResources.SunLocation_ValidationError_IntervalMinutes);
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
                .WithMessage(AppResources.SunLocation_ValidationError_LatitudeRange);

            RuleFor(x => x.Longitude)
                .InclusiveBetween(-180.0, 180.0)
                .WithMessage(AppResources.SunLocation_ValidationError_LongitudeRange);

            RuleFor(x => x.DateTime)
                .NotEmpty()
                .WithMessage(AppResources.SunLocation_ValidationError_DateTimeRequired)
                .Must(BeValidDateTime)
                .WithMessage(AppResources.SunLocation_ValidationError_InvalidDateTimeRange);

            RuleFor(x => x.ObjectHeight)
                .GreaterThan(0)
                .WithMessage(AppResources.SunLocation_ValidationError_ObjectHeight)
                .LessThanOrEqualTo(1000)
                .WithMessage(AppResources.SunLocation_ValidationError_ObjectHeightMaximum);

            RuleFor(x => x.TerrainType)
                .IsInEnum()
                .WithMessage(AppResources.SunLocation_ValidationError_TerrainType);
        }

        private bool BeValidDateTime(DateTime dateTime)
        {
            // Ensure datetime is not default and within reasonable range for shadow calculations
            var minDate = new DateTime(1900, 1, 1);
            var maxDate = new DateTime(2100, 12, 31);

            return dateTime != default && dateTime >= minDate && dateTime <= maxDate;
        }
    }
}