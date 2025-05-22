using System;
using System.Collections.Generic;

namespace Location.Core.Domain.ValueObjects
{
    /// <summary>
    /// Value object representing geographic coordinates
    /// </summary>
    public class Coordinate : ValueObject
    {
        public double Latitude { get; private set; }
        public double Longitude { get; private set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="Coordinate"/> class with the specified latitude and longitude.
        /// </summary>
        /// <remarks>The latitude and longitude values are rounded to six decimal places for
        /// precision.</remarks>
        /// <param name="latitude">The latitude of the coordinate, in decimal degrees. Must be in the range -90 to 90.</param>
        /// <param name="longitude">The longitude of the coordinate, in decimal degrees. Must be in the range -180 to 180.</param>
        public Coordinate(double latitude, double longitude)
        {
            ValidateCoordinates(latitude, longitude);
            Latitude = Math.Round(latitude, 6);
            Longitude = Math.Round(longitude, 6);
        }
        public Coordinate(double latitude, double longitude, bool skipValidation)
        {
            if (!skipValidation)
            {
                ValidateCoordinates(latitude, longitude);
            }
            Latitude = Math.Round(latitude, 6);
            Longitude = Math.Round(longitude, 6);
        }
        /// <summary>
        /// Validates that the specified latitude and longitude values are within their respective valid ranges.
        /// </summary>
        /// <param name="latitude">The latitude value to validate. Must be between -90 and 90, inclusive.</param>
        /// <param name="longitude">The longitude value to validate. Must be between -180 and 180, inclusive.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="latitude"/> is outside the range of -90 to 90,  or if <paramref name="longitude"/>
        /// is outside the range of -180 to 180.</exception>
        private static void ValidateCoordinates(double latitude, double longitude)
        {
            if (latitude < -90 || latitude > 90)
            {
                throw new ArgumentOutOfRangeException(nameof(latitude), "Latitude must be between -90 and 90");
            }

            if (longitude < -180 || longitude > 180)
            {
                throw new ArgumentOutOfRangeException(nameof(longitude), "Longitude must be between -180 and 180");
            }
        }

        /// <summary>
        /// Calculates distance to another coordinate in kilometers
        /// </summary>
        public double DistanceTo(Coordinate other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));

            const double earthRadiusKm = 6371;
            var dLat = ToRadians(other.Latitude - Latitude);
            var dLon = ToRadians(other.Longitude - Longitude);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(Latitude)) * Math.Cos(ToRadians(other.Latitude)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return earthRadiusKm * c;
        }
        /// <summary>
        /// Converts an angle from degrees to radians.
        /// </summary>
        /// <param name="degrees">The angle in degrees to be converted.</param>
        /// <returns>The equivalent angle in radians.</returns>
        private static double ToRadians(double degrees)
        {
            return degrees * (Math.PI / 180);
        }
        /// <summary>
        /// Provides the components used to determine equality for the current object.
        /// </summary>
        /// <remarks>This method returns an enumerable of objects that represent the significant
        /// properties of the object for equality comparison. Override this method in derived classes to specify which
        /// properties should be included in equality checks.</remarks>
        /// <returns>An <see cref="IEnumerable{T}"/> of objects representing the components used for equality comparison.</returns>
        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Latitude;
            yield return Longitude;
        }
        /// <summary>
        /// Returns a string representation of the geographic coordinates in the format "Latitude, Longitude".
        /// </summary>
        /// <returns>A string containing the latitude and longitude values formatted to six decimal places, separated by a comma.</returns>
        public override string ToString()
        {
            return $"{Latitude:F6}, {Longitude:F6}";
        }
    }
}