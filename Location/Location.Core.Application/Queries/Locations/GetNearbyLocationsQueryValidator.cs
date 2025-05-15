using FluentValidation;

namespace Location.Core.Application.Queries.Locations
{
    public class GetNearbyLocationsQueryValidator : AbstractValidator<GetNearbyLocationsQuery>
    {
        public GetNearbyLocationsQueryValidator()
        {
            RuleFor(x => x.Latitude)
                .InclusiveBetween(-90, 90).WithMessage("Latitude must be between -90 and 90 degrees");

            RuleFor(x => x.Longitude)
                .InclusiveBetween(-180, 180).WithMessage("Longitude must be between -180 and 180 degrees");

            RuleFor(x => x.DistanceKm)
                .GreaterThan(0).WithMessage("Distance must be greater than 0")
                .LessThanOrEqualTo(100).WithMessage("Distance must not exceed 100km");
        }
    }
}