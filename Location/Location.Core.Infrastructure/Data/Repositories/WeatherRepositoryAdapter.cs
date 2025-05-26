using Location.Core.Application.Common.Interfaces;
using Location.Core.Domain.Entities;

namespace Location.Core.Infrastructure.Data.Repositories
{
    public class WeatherRepositoryAdapter : IWeatherRepository
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
        {
            // Convert synchronous call to async - this is a limitation of the adapter pattern
            // The calling code should ideally be updated to use async patterns
            Task.Run(async () => await _innerRepository.UpdateAsync(weather, CancellationToken.None));
        }

        public void Delete(Weather weather)
        {
            // Convert synchronous call to async - this is a limitation of the adapter pattern
            // The calling code should ideally be updated to use async patterns
            Task.Run(async () => await _innerRepository.DeleteAsync(weather, CancellationToken.None));
        }

        public Task<IEnumerable<Weather>> GetRecentAsync(int count = 10, CancellationToken cancellationToken = default)
            => _innerRepository.GetRecentAsync(count, cancellationToken);

        public Task<IEnumerable<Weather>> GetExpiredAsync(TimeSpan maxAge, CancellationToken cancellationToken = default)
            => _innerRepository.GetExpiredAsync(maxAge, cancellationToken);
    }
}