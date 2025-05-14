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
        Task<Domain.Entities.Location?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<IEnumerable<Domain.Entities.Location>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<Domain.Entities.Location>> GetActiveAsync(CancellationToken cancellationToken = default);
        Task<Domain.Entities.Location> AddAsync(Domain.Entities.Location location, CancellationToken cancellationToken = default);
        void Update(Domain.Entities.Location location);
        void Delete(Domain.Entities.Location location);
        Task<Domain.Entities.Location?> GetByTitleAsync(string title, CancellationToken cancellationToken = default);
        Task<IEnumerable<Domain.Entities.Location>> GetNearbyAsync(double latitude, double longitude, double distanceKm, CancellationToken cancellationToken = default);
    }
}