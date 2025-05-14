using Location.Core.Application.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Location.Core.Application.Common
{
    public interface IWeatherRepository
    {
        Task<Result<Domain.Entities.Weather>> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<Result<Domain.Entities.Weather>> GetByLocationIdAsync(int locationId, CancellationToken cancellationToken = default);
        Task<Result<Domain.Entities.Weather>> CreateAsync(Domain.Entities.Weather weather, CancellationToken cancellationToken = default);
        Task<Result<Domain.Entities.Weather>> UpdateAsync(Domain.Entities.Weather weather, CancellationToken cancellationToken = default);
        Task<Result<bool>> DeleteAsync(int id, CancellationToken cancellationToken = default);
        Task<Result<List<Domain.Entities.Weather>>> GetRecentAsync(int count = 10, CancellationToken cancellationToken = default);
        Task<Result<List<Domain.Entities.Weather>>> GetExpiredAsync(TimeSpan maxAge, CancellationToken cancellationToken = default);
    }
}
