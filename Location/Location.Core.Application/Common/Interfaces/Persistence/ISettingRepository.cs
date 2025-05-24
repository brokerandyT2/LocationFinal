using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Location.Core.Domain.Entities;

namespace Location.Core.Application.Common.Interfaces.Persistence
{
    /// <summary>
    /// Repository interface for Setting entity
    /// </summary>
    public interface ISettingRepository
    {
        Task<Domain.Entities.Setting?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<Domain.Entities.Setting?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);
        Task<IEnumerable<Domain.Entities.Setting>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<Domain.Entities.Setting>> GetByKeysAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default);
        Task<Domain.Entities.Setting> AddAsync(Domain.Entities.Setting setting, CancellationToken cancellationToken = default);
        Task UpdateAsync(Domain.Entities.Setting setting, CancellationToken cancellationToken = default);
        Task DeleteAsync(Domain.Entities.Setting setting, CancellationToken cancellationToken = default);

        Task<Domain.Entities.Setting> UpsertAsync(string key, string value, string? description = null, CancellationToken cancellationToken = default);
    }
}