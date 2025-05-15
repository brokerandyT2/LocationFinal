using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Location.Core.Application.Common.Interfaces
{
    public interface ITipTypeRepository
    {
        Task<Domain.Entities.TipType?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<IEnumerable<Domain.Entities.TipType>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<Domain.Entities.TipType> AddAsync(Domain.Entities.TipType tipType, CancellationToken cancellationToken = default);
        void Update(Domain.Entities.TipType tipType);
        void Delete(Domain.Entities.TipType tipType);
    }
}
