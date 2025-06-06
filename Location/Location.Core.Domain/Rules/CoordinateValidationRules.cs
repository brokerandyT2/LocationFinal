using Location.Core.Domain.ValueObjects;

namespace Location.Core.Domain.Rules
{
    /// <summary>
    /// Business rules for coordinate validation
    /// </summary>
    public static class CoordinateValidationRules
    {
        /// <summary>
        /// Validates the specified latitude and longitude values and determines if they represent a valid geographic
        /// location.
        /// </summary>
        /// <remarks>A location is considered invalid if: <list type="bullet"> <item><description>The
        /// latitude is outside the range -90 to 90.</description></item> <item><description>The longitude is outside
        /// the range -180 to 180.</description></item> <item><description>The coordinates represent Null Island (0,0),
        /// which is not a valid location.</description></item> </list></remarks>
        /// <param name="latitude">The latitude value to validate, in degrees. Must be in the range -90 to 90.</param>
        /// <param name="longitude">The longitude value to validate, in degrees. Must be in the range -180 to 180.</param>
        /// <param name="errors">When this method returns, contains a list of error messages describing any validation failures. If the
        /// location is valid, the list will be empty.</param>
        /// <returns><see langword="true"/> if the latitude and longitude values are valid; otherwise, <see langword="false"/>.</returns>
        public static bool IsValid(double latitude, double longitude, out List<string> errors)
        {
            errors = new List<string>();

            if (latitude < -90 || latitude > 90)
            {
                errors.Add($"Latitude {latitude} is out of valid range (-90 to 90)");
            }

            if (longitude < -180 || longitude > 180)
            {
                errors.Add($"Longitude {longitude} is out of valid range (-180 to 180)");
            }

            // Check for specific invalid coordinates
            if (latitude == 0 && longitude == 0)
            {
                errors.Add("Null Island (0,0) is not a valid location");
            }

            return errors.Count == 0;
        }
        /// <summary>
        /// Determines whether the distance between two coordinates is within the specified maximum distance.
        /// </summary>
        /// <param name="from">The starting coordinate. Cannot be <see langword="null"/>.</param>
        /// <param name="to">The destination coordinate. Cannot be <see langword="null"/>.</param>
        /// <param name="maxDistanceKm">The maximum allowable distance, in kilometers.</param>
        /// <returns><see langword="true"/> if the distance between <paramref name="from"/> and <paramref name="to"/>  is less
        /// than or equal to <paramref name="maxDistanceKm"/>; otherwise, <see langword="false"/>.</returns>
        public static bool IsValidDistance(Coordinate from, Coordinate to, double maxDistanceKm)
        {
            if (from == null || to == null)
                return false;

            var distance = from.DistanceTo(to);
            return distance <= maxDistanceKm;
        }
    }
}