using System.Collections.Generic;
using Location.Core.Domain.ValueObjects;

namespace Location.Core.Domain.Rules
{
    /// <summary>
    /// Business rules for coordinate validation
    /// </summary>
    public static class CoordinateValidationRules
    {
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

        public static bool IsValidDistance(Coordinate from, Coordinate to, double maxDistanceKm)
        {
            if (from == null || to == null)
                return false;

            var distance = from.DistanceTo(to);
            return distance <= maxDistanceKm;
        }
    }
}