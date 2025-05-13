using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Location.Core.Domain.Entities;

namespace Location.Core.Application.Common.Interfaces.Persistence
{
    /// <summary>
    /// Repository interface for Tip entity
    /// </summary>
    public interface ITipRepository
    {
        /// <summary>
        /// Gets a tip by its ID
        /// </summary>
        Task<Tip?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a tip by its title
        /// </summary>
        Task<Tip?> GetByTitleAsync(string title, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all tips
        /// </summary>
        Task<IEnumerable<Tip>> GetAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets tips by tip type
        /// </summary>
        Task<IEnumerable<Tip>> GetByTipTypeIdAsync(int tipTypeId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds a new tip
        /// </summary>
        Task<Tip> AddAsync(Tip tip, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing tip
        /// </summary>
        void Update(Tip tip);

        /// <summary>
        /// Deletes a tip
        /// </summary>
        void Delete(Tip tip);
    }
}