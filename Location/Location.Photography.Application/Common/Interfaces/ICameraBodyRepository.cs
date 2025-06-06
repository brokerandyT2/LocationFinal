using Location.Core.Application.Common.Models;
using Location.Photography.Domain.Entities;
using Location.Photography.Domain.Enums;

namespace Location.Photography.Application.Common.Interfaces
{
    public interface ICameraBodyRepository
    {
        /// <summary>
        /// Creates a new camera body
        /// </summary>
        Task<Result<CameraBody>> CreateAsync(CameraBody cameraBody, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a camera body by ID
        /// </summary>
        Task<Result<CameraBody>> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets camera bodies with paging, user cameras first
        /// </summary>
        Task<Result<List<CameraBody>>> GetPagedAsync(int skip, int take, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all user-created camera bodies
        /// </summary>
        Task<Result<List<CameraBody>>> GetUserCamerasAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing camera body
        /// </summary>
        Task<Result<CameraBody>> UpdateAsync(CameraBody cameraBody, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a camera body
        /// </summary>
        Task<Result<bool>> DeleteAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Searches camera bodies by name with fuzzy matching
        /// </summary>
        Task<Result<List<CameraBody>>> SearchByNameAsync(string name, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets camera bodies by mount type
        /// </summary>
        Task<Result<List<CameraBody>>> GetByMountTypeAsync(MountType mountType, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets total count of camera bodies
        /// </summary>
        Task<Result<int>> GetTotalCountAsync(CancellationToken cancellationToken = default);
    }
}