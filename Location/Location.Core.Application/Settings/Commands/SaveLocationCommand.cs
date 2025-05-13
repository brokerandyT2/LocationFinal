using MediatR;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Locations.DTOs;

namespace Location.Core.Application.Locations.Commands.SaveLocation
{
    /// <summary>
    /// Command to save (create or update) a location
    /// </summary>
    public class SaveLocationCommand : IRequest<Result<LocationDto>>
    {
        /// <summary>
        /// Location ID (null for new locations)
        /// </summary>
        public int? Id { get; set; }

        /// <summary>
        /// Location title
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Location description
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Latitude coordinate
        /// </summary>
        public double Latitude { get; set; }

        /// <summary>
        /// Longitude coordinate
        /// </summary>
        public double Longitude { get; set; }

        /// <summary>
        /// City name
        /// </summary>
        public string City { get; set; } = string.Empty;

        /// <summary>
        /// State name
        /// </summary>
        public string State { get; set; } = string.Empty;

        /// <summary>
        /// Path to attached photo (optional)
        /// </summary>
        public string? PhotoPath { get; set; }
    }
}