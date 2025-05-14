using Location.Core.Application.Common.Interfaces;
using Location.Core.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Core.Infrastructure.Data.Repositories
{
    public class WeatherRepositoryAdapter : Location.Core.Application.Common.Interfaces.IWeatherRepository
    {
        private readonly Location.Core.Application.Common.Interfaces.Persistence.IWeatherRepository _innerRepository;

        public WeatherRepositoryAdapter(Location.Core.Application.Common.Interfaces.Persistence.IWeatherRepository innerRepository)
        {
            _innerRepository = innerRepository ?? throw new ArgumentNullException(nameof(innerRepository));
        }

        public Task<Weather?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
            => _innerRepository.GetByIdAsync(id, cancellationToken);

        public Task<Weather?> GetByLocationIdAsync(int locationId, CancellationToken cancellationToken = default)
            => _innerRepository.GetByLocationIdAsync(locationId, cancellationToken);

        public Task<Weather> AddAsync(Weather weather, CancellationToken cancellationToken = default)
            => _innerRepository.AddAsync(weather, cancellationToken);

        public void Update(Weather weather)
            => _innerRepository.Update(weather);

        public void Delete(Weather weather)
            => _innerRepository.Delete(weather);

        public Task<IEnumerable<Weather>> GetRecentAsync(int count = 10, CancellationToken cancellationToken = default)
            => _innerRepository.GetRecentAsync(count, cancellationToken);

        public Task<IEnumerable<Weather>> GetExpiredAsync(TimeSpan maxAge, CancellationToken cancellationToken = default)
            => _innerRepository.GetExpiredAsync(maxAge, cancellationToken);
    }
}