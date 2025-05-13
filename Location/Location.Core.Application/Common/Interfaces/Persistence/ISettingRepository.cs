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
        /// <summary>
        /// Gets a setting by its ID
        /// </summary>
        Task<Setting?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a setting by its key
        /// </summary>
        Task<Setting?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all settings
        /// </summary>
        Task<IEnumerable<Setting>> GetAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets multiple settings by their keys
        /// </summary>
        Task<IEnumerable<Setting>> GetByKeysAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds a new setting
        /// </summary>
        Task<Setting> AddAsync(Setting setting, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing setting
        /// </summary>
        void Update(Setting setting);

        /// <summary>
        /// Deletes a setting
        /// </summary>
        void Delete(Setting setting);

        /// <summary>
        /// Adds or updates a setting
        /// </summary>
        Task<Setting> UpsertAsync(string key, string value, string? description = null, CancellationToken cancellationToken = default);
    }
}