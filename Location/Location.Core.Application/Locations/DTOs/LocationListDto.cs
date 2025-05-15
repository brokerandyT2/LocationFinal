using System;

namespace Location.Core.Application.Locations.DTOs
{
    /// <summary>
    /// Data transfer object for location list items
    /// </summary>
    public class LocationListDto
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Location title
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// City name
        /// </summary>
        public string City { get; set; } = string.Empty;

        /// <summary>
        /// State name
        /// </summary>
        public string State { get; set; } = string.Empty;

        /// <summary>
        /// Path to attached photo
        /// </summary>
        public string? PhotoPath { get; set; }

        /// <summary>
        /// Creation/update timestamp
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Indicates if the location is deleted
        /// </summary>
        public bool IsDeleted { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}