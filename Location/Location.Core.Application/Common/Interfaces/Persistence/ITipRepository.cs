﻿namespace Location.Core.Application.Common.Interfaces.Persistence
{
    /// <summary>
    /// Repository interface for Tip entity
    /// </summary>
    public interface ITipRepository
    {
        Task<Domain.Entities.Tip?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<IEnumerable<Domain.Entities.Tip>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<Domain.Entities.Tip>> GetByTipTypeIdAsync(int tipTypeId, CancellationToken cancellationToken = default);
        Task<Domain.Entities.Tip> AddAsync(Domain.Entities.Tip tip, CancellationToken cancellationToken = default);
        Task UpdateAsync(Domain.Entities.Tip setting, CancellationToken cancellationToken = default);
        Task DeleteAsync(Domain.Entities.Tip setting, CancellationToken cancellationToken = default);
        Task<Domain.Entities.Tip?> GetByTitleAsync(string title, CancellationToken cancellationToken = default);
        Task<Domain.Entities.Tip?> GetRandomByTypeAsync(int tipTypeId, CancellationToken cancellationToken = default);

    }
}