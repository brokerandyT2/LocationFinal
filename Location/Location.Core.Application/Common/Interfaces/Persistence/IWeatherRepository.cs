using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Core.Application.Common.Interfaces.Persistence
{
    /// <summary>
    /// Repository interface for Weather aggregate root
    /// </summary>
    public interface IWeatherRepository
    {
        /// <summary>
        /// Gets weather by its ID
        /// </summary>
        Task<Domain.Entities.Weather?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets weather by location ID
        /// </summary>
        Task<Domain.Entities.Weather?> GetByLocationIdAsync(int locationId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets weather by coordinates
        /// </summary>
        Task<Domain.Entities.Weather?> GetByCoordinatesAsync(double latitude, double longitude, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all weather entries
        /// </summary>
        Task<IEnumerable<Domain.Entities.Weather>> GetAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds a new weather entry
        /// </summary>
        Task<Domain.Entities.Weather> AddAsync(Domain.Entities.Weather weather, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing weather entry
        /// </summary>
        void Update(Domain.Entities.Weather weather);

        /// <summary>
        /// Deletes a weather entry
        /// </summary>
        void Delete(Domain.Entities.Weather weather);
    }
}