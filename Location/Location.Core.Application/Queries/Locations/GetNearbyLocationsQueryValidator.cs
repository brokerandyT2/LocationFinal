using FluentValidation;
using Location.Core.Application.Resources;

namespace Location.Core.Application.Queries.Locations
{
    /// <summary>
    /// Validates the parameters of a <see cref="GetNearbyLocationsQuery"/> to ensure they meet the required
    /// constraints.
    /// </summary>
    /// <remarks>This validator enforces the following rules: <list type="bullet"> <item><description>The
    /// <c>Latitude</c> must be between -90 and 90 degrees.</description></item> <item><description>The <c>Longitude</c>
    /// must be between -180 and 180 degrees.</description></item> <item><description>The <c>DistanceKm</c> must be
    /// greater than 0 and less than or equal to 100 kilometers.</description></item> </list> If any of these rules are
    /// violated, a validation error will be generated with an appropriate error message.</remarks>
    public class GetNearbyLocationsQueryValidator : AbstractValidator<GetNearbyLocationsQuery>
    {
        /// <summary>
        /// Validates the parameters of a query for retrieving nearby locations.
        /// </summary>
        /// <remarks>This validator ensures that the latitude, longitude, and distance parameters of the
        /// query meet the required constraints. Specifically: <list type="bullet"> <item><description>Latitude must be
        /// between -90 and 90 degrees.</description></item> <item><description>Longitude must be between -180 and 180
        /// degrees.</description></item> <item><description>Distance must be greater than 0 and not exceed 100
        /// kilometers.</description></item> </list></remarks>
        public GetNearbyLocationsQueryValidator()
        {
            RuleFor(x => x.Latitude)
                .InclusiveBetween(-90, 90)
                .WithMessage(AppResources.Location_ValidationError_LatitudeRange);

            RuleFor(x => x.Longitude)
                .InclusiveBetween(-180, 180)
                .WithMessage(AppResources.Location_ValidationError_LongitudeRange);

            RuleFor(x => x.DistanceKm)
                .GreaterThan(0)
                .WithMessage(AppResources.Location_ValidationError_DistanceRequired)
                .LessThanOrEqualTo(100)
                .WithMessage(AppResources.Location_ValidationError_DistanceMaximum);
        }
    }
}