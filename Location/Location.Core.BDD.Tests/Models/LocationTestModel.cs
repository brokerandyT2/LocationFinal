namespace Location.Core.BDD.Tests.Models
{
    /// <summary>
    /// Model class for location data in tests
    /// </summary>
    public class LocationTestModel
    {
        /// <summary>
        /// Gets or sets the location ID
        /// </summary>
        public int? Id { get; set; }

        /// <summary>
        /// Gets or sets the location title
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the location description
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the latitude coordinate
        /// </summary>
        public double Latitude { get; set; }

        /// <summary>
        /// Gets or sets the longitude coordinate
        /// </summary>
        public double Longitude { get; set; }

        /// <summary>
        /// Gets or sets the city name
        /// </summary>
        public string City { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the state name
        /// </summary>
        public string State { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the photo path
        /// </summary>
        public string? PhotoPath { get; set; }

        /// <summary>
        /// Gets or sets the creation/update timestamp
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets whether the location is deleted
        /// </summary>
        public bool IsDeleted { get; set; }

        /// <summary>
        /// Creates a domain entity from this test model
        /// </summary>
        /// <summary>
        /// Creates a domain entity from this test model
        /// </summary>
        public Domain.Entities.Location ToDomainEntity()
        {
            // FOR TEST SCENARIOS: Use reflection to bypass coordinate validation entirely
            Domain.ValueObjects.Coordinate coordinate;

            try
            {
                // Try normal construction first
                coordinate = new Domain.ValueObjects.Coordinate(Latitude, Longitude, skipValidation: true);
            }
            catch (ArgumentOutOfRangeException)
            {
                // If validation still fails, create coordinate using reflection to bypass ALL validation
                coordinate = CreateCoordinateBypassingValidation(Latitude, Longitude);
            }

            var address = new Domain.ValueObjects.Address(City, State);

            // Create location entity
            var location = new Domain.Entities.Location(
                Title,
                Description,
                coordinate,
                address);

            // Set additional properties if needed
            if (Id.HasValue && Id.Value > 0)
            {
                SetPrivateProperty(location, "Id", Id.Value);
            }

            if (!string.IsNullOrEmpty(PhotoPath))
            {
                location.AttachPhoto(PhotoPath);
            }

            if (IsDeleted)
            {
                location.Delete();
            }

            SetPrivateProperty(location, "Timestamp", Timestamp);

            return location;
        }

        /// <summary>
        /// Creates a coordinate bypassing all validation using reflection (for test scenarios only)
        /// </summary>
        private static Domain.ValueObjects.Coordinate CreateCoordinateBypassingValidation(double latitude, double longitude)
        {
            try
            {
                // Create an uninitialized instance
                var coordinate = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(Domain.ValueObjects.Coordinate))
                    as Domain.ValueObjects.Coordinate;

                // Set the private fields directly
                SetPrivateProperty(coordinate, "Latitude", latitude);
                SetPrivateProperty(coordinate, "Longitude", longitude);

                return coordinate;
            }
            catch (Exception ex)
            {
                // If reflection fails, throw a more descriptive error
                throw new InvalidOperationException($"Failed to create test coordinate with invalid values (Lat: {latitude}, Lon: {longitude}). " +
                    "This suggests the domain layer's skipValidation parameter is not working correctly.", ex);
            }
        }

        /// <summary>
        /// Sets a private property on an object using reflection
        /// </summary>
        private static void SetPrivateProperty(object obj, string propertyName, object value)
        {
            var property = obj.GetType().GetProperty(propertyName);
            if (property != null)
            {
                property.SetValue(obj, value);
            }
            else
            {
                var field = obj.GetType().GetField(propertyName,
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                field?.SetValue(obj, value);
            }
        }

        /// <summary>
        /// Creates a test model from a domain entity
        /// </summary>
        public static LocationTestModel FromDomainEntity(Domain.Entities.Location location)
        {
            return new LocationTestModel
            {
                Id = location.Id,
                Title = location.Title,
                Description = location.Description,
                Latitude = location.Coordinate.Latitude,
                Longitude = location.Coordinate.Longitude,
                City = location.Address.City,
                State = location.Address.State,
                PhotoPath = location.PhotoPath,
                Timestamp = location.Timestamp,
                IsDeleted = location.IsDeleted
            };
        }

        /// <summary>
        /// Creates a test model from an application DTO
        /// </summary>
        public static LocationTestModel FromDto(Application.Locations.DTOs.LocationDto dto)
        {
            return new LocationTestModel
            {
                Id = dto.Id,
                Title = dto.Title,
                Description = dto.Description,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                City = dto.City,
                State = dto.State,
                PhotoPath = dto.PhotoPath,
                Timestamp = dto.Timestamp,
                IsDeleted = dto.IsDeleted
            };
        }
    }
}