using Location.Core.Application.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Location.Core.Application.Common.Interfaces
{
    public interface ISettingRepository
    {
        Task<Result<Domain.Entities.Setting>> GetByKeyAsync(string key, CancellationToken cancellationToken = default);
        Task<Result<List<Domain.Entities.Setting>>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<Result<Domain.Entities.Setting>> CreateAsync(Domain.Entities.Setting setting, CancellationToken cancellationToken = default);
        Task<Result<Domain.Entities.Setting>> UpdateAsync(Domain.Entities.Setting setting, CancellationToken cancellationToken = default);
        Task<Result<bool>> DeleteAsync(string key, CancellationToken cancellationToken = default);
        Task<Result<Domain.Entities.Setting>> UpsertAsync(Domain.Entities.Setting setting, CancellationToken cancellationToken = default);
        Task<Result<Dictionary<string, string>>> GetAllAsDictionaryAsync(CancellationToken cancellationToken = default);
    }
}
