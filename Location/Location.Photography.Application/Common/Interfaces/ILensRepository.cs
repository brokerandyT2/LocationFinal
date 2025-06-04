using Location.Core.Application.Common.Models;
using Location.Photography.Domain.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Photography.Application.Common.Interfaces
{
    public interface ILensRepository
    {
        /// <summary>
        /// Creates a new lens
        /// </summary>
        Task<Result<Lens>> CreateAsync(Lens lens, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a lens by ID
        /// </summary>
        Task<Result<Lens>> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets lenses with paging, user lenses first
        /// </summary>
        Task<Result<List<Lens>>> GetPagedAsync(int skip, int take, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all user-created lenses
        /// </summary>
        Task<Result<List<Lens>>> GetUserLensesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing lens
        /// </summary>
        Task<Result<Lens>> UpdateAsync(Lens lens, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a lens
        /// </summary>
        Task<Result<bool>> DeleteAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Searches lenses by focal length range with fuzzy matching
        /// </summary>
        Task<Result<List<Lens>>> SearchByFocalLengthAsync(double focalLength, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets lenses compatible with a specific camera
        /// </summary>
        Task<Result<List<Lens>>> GetCompatibleLensesAsync(int cameraBodyId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets total count of lenses
        /// </summary>
        Task<Result<int>> GetTotalCountAsync(CancellationToken cancellationToken = default);
    }
}