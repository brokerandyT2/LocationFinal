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

        public Coordinate(double latitude, double longitude)
        {
            ValidateCoordinates(latitude, longitude);
            Latitude = Math.Round(latitude, 6);
            Longitude = Math.Round(longitude, 6);
        }

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

        private static double ToRadians(double degrees)
        {
            return degrees * (Math.PI / 180);
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Latitude;
            yield return Longitude;
        }

        public override string ToString()
        {
            return $"{Latitude:F6}, {Longitude:F6}";
        }
    }
}