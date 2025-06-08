// Location.Photography.Application/Common/Interfaces/IUserCameraBodyRepository.cs
using Location.Core.Application.Common.Models;
using Location.Photography.Domain.Entities;

namespace Location.Photography.Application.Common.Interfaces
{
    public interface IUserCameraBodyRepository
    {
        /// <summary>
        /// Saves a camera body for a user
        /// </summary>
        Task<Result<UserCameraBody>> CreateAsync(UserCameraBody userCameraBody, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all saved camera bodies for a user
        /// </summary>
        Task<Result<List<UserCameraBody>>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a specific user camera body by user and camera ID
        /// </summary>
        Task<Result<UserCameraBody>> GetByUserAndCameraIdAsync(string userId, int cameraBodyId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a camera body is already saved by a user
        /// </summary>
        Task<Result<bool>> ExistsAsync(string userId, int cameraBodyId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing user camera body
        /// </summary>
        Task<Result<UserCameraBody>> UpdateAsync(UserCameraBody userCameraBody, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes a camera body from user's saved list
        /// </summary>
        Task<Result<bool>> DeleteAsync(string userId, int cameraBodyId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets user's favorite camera bodies
        /// </summary>
        Task<Result<List<UserCameraBody>>> GetFavoritesByUserIdAsync(string userId, CancellationToken cancellationToken = default);
        Task<Result<List<UserCameraBody>>> GetAll();
    }
}