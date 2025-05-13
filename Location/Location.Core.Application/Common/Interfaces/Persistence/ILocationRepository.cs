using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Location.Core.Domain.Entities;

namespace Location.Core.Application.Common.Interfaces.Persistence
{
    /// <summary>
    /// Repository interface for Location aggregate root
    /// </summary>
    public interface ILocationRepository
    {
        /// <summary>
        /// Gets a location by its ID
        /// </summary>
        Task<Domain.Entities.Location?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a location by its coordinates
        /// </summary>
        Task<Domain.Entities.Location?> GetByCoordinatesAsync(double latitude, double longitude, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all locations including deleted ones
        /// </summary>
        Task<IEnumerable<Domain.Entities.Location>> GetAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets only active (non-deleted) locations
        /// </summary>
        Task<IEnumerable<Domain.Entities.Location>> GetActiveAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds a new location
        /// </summary>
        Task<Domain.Entities.Location> AddAsync(Domain.Entities.Location location, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing location
        /// </summary>
        void Update(Domain.Entities.Location location);

        /// <summary>
        /// Deletes a location (soft delete)
        /// </summary>
        void Delete(Domain.Entities.Location location);
    }
}