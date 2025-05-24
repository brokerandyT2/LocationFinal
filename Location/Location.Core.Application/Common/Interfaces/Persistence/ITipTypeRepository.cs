using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Location.Core.Domain.Entities;
namespace Location.Core.Application.Common.Interfaces.Persistence
{
    /// <summary>
    /// Repository interface for TipType entity
    /// </summary>
    public interface ITipTypeRepository
    {
        Task<Domain.Entities.TipType?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<IEnumerable<Domain.Entities.TipType>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<Domain.Entities.TipType> AddAsync(Domain.Entities.TipType tipType, CancellationToken cancellationToken = default);
        Task UpdateAsync(Domain.Entities.TipType setting, CancellationToken cancellationToken = default);
        Task DeleteAsync(Domain.Entities.TipType setting, CancellationToken cancellationToken = default);
        Task<Domain.Entities.TipType?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
        Task<Domain.Entities.TipType?> GetWithTipsAsync(int id, CancellationToken cancellationToken = default);
    }
}