using Location.Core.Application.Common.Interfaces.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Location.Core.Infrastructure.Data.Repositories
{
    public class LocationRepositoryAdapter : ILocationRepository
    {
        private readonly Location.Core.Application.Common.Interfaces.Persistence.ILocationRepository _innerRepository;

        public LocationRepositoryAdapter(Location.Core.Application.Common.Interfaces.Persistence.ILocationRepository innerRepository)
        {
            _innerRepository = innerRepository ?? throw new ArgumentNullException(nameof(innerRepository));
        }

        public Task<Location.Core.Domain.Entities.Location?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
            => _innerRepository.GetByIdAsync(id, cancellationToken);

        public Task<IEnumerable<Location.Core.Domain.Entities.Location>> GetAllAsync(CancellationToken cancellationToken = default)
            => _innerRepository.GetAllAsync(cancellationToken);

        public Task<IEnumerable<Location.Core.Domain.Entities.Location>> GetActiveAsync(CancellationToken cancellationToken = default)
            => _innerRepository.GetActiveAsync(cancellationToken);

        public Task<Location.Core.Domain.Entities.Location> AddAsync(Location.Core.Domain.Entities.Location location, CancellationToken cancellationToken = default)
            => _innerRepository.AddAsync(location, cancellationToken);

        public void Update(Location.Core.Domain.Entities.Location location)
            => _innerRepository.Update(location);

        public void Delete(Location.Core.Domain.Entities.Location location)
            => _innerRepository.Delete(location);

        public Task<Location.Core.Domain.Entities.Location?> GetByTitleAsync(string title, CancellationToken cancellationToken = default)
            => _innerRepository.GetByTitleAsync(title, cancellationToken);

        public Task<IEnumerable<Location.Core.Domain.Entities.Location>> GetNearbyAsync(double latitude, double longitude, double distanceKm, CancellationToken cancellationToken = default)
            => _innerRepository.GetNearbyAsync(latitude, longitude, distanceKm, cancellationToken);
    }
}
