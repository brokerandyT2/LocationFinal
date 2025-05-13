using System;

namespace Location.Core.Domain.Exceptions
{
    /// <summary>
    /// Exception thrown when invalid coordinates are provided
    /// </summary>
    public class InvalidCoordinateException : LocationDomainException
    {
        public double Latitude { get; }
        public double Longitude { get; }

        public InvalidCoordinateException(double latitude, double longitude)
            : base($"Invalid coordinates: Latitude={latitude}, Longitude={longitude}", "INVALID_COORDINATE")
        {
            Latitude = latitude;
            Longitude = longitude;
        }

        public InvalidCoordinateException(double latitude, double longitude, string message)
            : base(message, "INVALID_COORDINATE")
        {
            Latitude = latitude;
            Longitude = longitude;
        }
    }
}