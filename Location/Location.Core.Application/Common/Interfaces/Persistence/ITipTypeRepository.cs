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
        /// <summary>
        /// Gets a tip type by its ID
        /// </summary>
        Task<TipType?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a tip type by name
        /// </summary>
        Task<TipType?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all tip types
        /// </summary>
        Task<IEnumerable<TipType>> GetAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a tip type with all its tips
        /// </summary>
        Task<TipType?> GetWithTipsAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds a new tip type
        /// </summary>
        Task<TipType> AddAsync(TipType tipType, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing tip type
        /// </summary>
        void Update(TipType tipType);

        /// <summary>
        /// Deletes a tip type
        /// </summary>
        void Delete(TipType tipType);
    }
}