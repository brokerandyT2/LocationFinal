using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Location.Core.Application.Common.Interfaces
{
    public interface IWeatherRepository
    {
        Task<Domain.Entities.Weather?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<Domain.Entities.Weather?> GetByLocationIdAsync(int locationId, CancellationToken cancellationToken = default);
        Task<Domain.Entities.Weather> AddAsync(Domain.Entities.Weather weather, CancellationToken cancellationToken = default);
        void Update(Domain.Entities.Weather weather);
        void Delete(Domain.Entities.Weather weather);
        Task<IEnumerable<Domain.Entities.Weather>> GetRecentAsync(int count = 10, CancellationToken cancellationToken = default);
        Task<IEnumerable<Domain.Entities.Weather>> GetExpiredAsync(TimeSpan maxAge, CancellationToken cancellationToken = default);
    }
}
