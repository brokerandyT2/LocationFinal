﻿using FluentValidation;
using System;

namespace Location.Photography.Application.Queries.SunLocation
{
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
}