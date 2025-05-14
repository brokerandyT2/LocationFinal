using Location.Core.Application.Common.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Core.Application.Common.Interfaces
{
    public interface ILocationRepository
    {
        Task<Result<Domain.Entities.Location>> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<Result<List<Domain.Entities.Location>>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<Result<List<Domain.Entities.Location>>> GetActiveAsync(CancellationToken cancellationToken = default);
        Task<Result<Domain.Entities.Location>> CreateAsync(Domain.Entities.Location location, CancellationToken cancellationToken = default);
        Task<Result<Domain.Entities.Location>> UpdateAsync(Domain.Entities.Location location, CancellationToken cancellationToken = default);
        Task<Result<bool>> DeleteAsync(int id, CancellationToken cancellationToken = default);
        Task<Result<bool>> SoftDeleteAsync(int id, CancellationToken cancellationToken = default);
        Task<Result<List<Domain.Entities.Location>>> GetByCoordinatesAsync(double latitude, double longitude, double radiusKm = 10, CancellationToken cancellationToken = default);
    }
}