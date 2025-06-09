using Location.Core.Application.Common.Models;

namespace Location.Core.Application.Common.Interfaces
{
    public interface ITipTypeRepository
    {
        Task<Domain.Entities.TipType?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<IEnumerable<Domain.Entities.TipType>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<Domain.Entities.TipType> AddAsync(Domain.Entities.TipType tipType, CancellationToken cancellationToken = default);
        Task UpdateAsync(Domain.Entities.TipType setting, CancellationToken cancellationToken = default);
        Task DeleteAsync(Domain.Entities.TipType setting, CancellationToken cancellationToken = default);

        Task<Result<Core.Domain.Entities.TipType>> CreateEntityAsync(Core.Domain.Entities.TipType entity, CancellationToken cancellationToken = default);

        Task<Result<Core.Domain.Entities.Tip>> CreateEntityAsync(Core.Domain.Entities.Tip entity, CancellationToken cancellationToken = default);

        Task<Result<Core.Domain.Entities.Location>> CreateEntityAsync(Core.Domain.Entities.Location entity, CancellationToken cancellationToken = default);
    }
}
