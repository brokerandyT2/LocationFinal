using System;

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
        public Domain.Entities.Location ToDomainEntity()
        {
            // Create coordinate and address value objects
            var coordinate = new Domain.ValueObjects.Coordinate(Latitude, Longitude, true);
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
        internal class Coordinate
        {
            public double Latitude { get; set; }
            public double Longitude { get; set; }
            public Coordinate(double latitude, double longitude)
            {
                Latitude = Math.Round(latitude, 6);
                Longitude = Math.Round(longitude, 6);
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