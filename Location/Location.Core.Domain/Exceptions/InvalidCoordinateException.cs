namespace Location.Core.Domain.Exceptions
{
    /// <summary>
    /// Exception thrown when invalid coordinates are provided
    /// </summary>
    public class InvalidCoordinateException : LocationDomainException
    {
        public double Latitude { get; }
        public double Longitude { get; }
        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidCoordinateException"/> class with the specified latitude
        /// and longitude values.
        /// </summary>
        /// <param name="latitude">The latitude value that caused the exception. Must be within the valid range of -90 to 90.</param>
        /// <param name="longitude">The longitude value that caused the exception. Must be within the valid range of -180 to 180.</param>
        public InvalidCoordinateException(double latitude, double longitude)
            : base($"Invalid coordinates: Latitude={latitude}, Longitude={longitude}", "INVALID_COORDINATE")
        {
            Latitude = latitude;
            Longitude = longitude;
        }
        /// <summary>
        /// Represents an exception that is thrown when an invalid geographic coordinate is encountered.
        /// </summary>
        /// <param name="latitude">The latitude value that caused the exception. Must be in the range -90 to 90.</param>
        /// <param name="longitude">The longitude value that caused the exception. Must be in the range -180 to 180.</param>
        /// <param name="message">A message that describes the error.</param>
        public InvalidCoordinateException(double latitude, double longitude, string message)
            : base(message, "INVALID_COORDINATE")
        {
            Latitude = latitude;
            Longitude = longitude;
        }
    }
}