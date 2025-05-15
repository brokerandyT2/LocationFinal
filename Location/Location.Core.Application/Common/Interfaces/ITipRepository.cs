using Location.Core.Application.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Location.Core.Application.Common.Interfaces
{
    public interface ITipRepository
    {
        Task<Result<Domain.Entities.Tip>> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<Result<List<Domain.Entities.Tip>>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<Result<List<Domain.Entities.Tip>>> GetByTypeAsync(int tipTypeId, CancellationToken cancellationToken = default);
        Task<Result<Domain.Entities.Tip>> CreateAsync(Domain.Entities.Tip tip, CancellationToken cancellationToken = default);
        Task<Result<Domain.Entities.Tip>> UpdateAsync(Domain.Entities.Tip tip, CancellationToken cancellationToken = default);
        Task<Result<bool>> DeleteAsync(int id, CancellationToken cancellationToken = default);
        Task<Result<Domain.Entities.Tip>> GetRandomByTypeAsync(int tipTypeId, CancellationToken cancellationToken = default);
        Task<Result<List<Domain.Entities.TipType>>> GetTipTypesAsync(CancellationToken cancellationToken = default);
        Task<Result<Domain.Entities.TipType>> CreateTipTypeAsync(Domain.Entities.TipType tipType, CancellationToken cancellationToken = default);
    }
}
