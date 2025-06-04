using Location.Core.Application.Common.Models;
using Location.Photography.Domain.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Photography.Application.Common.Interfaces
{
    public interface ILensCameraCompatibilityRepository
    {
        /// <summary>
        /// Creates a new lens-camera compatibility relationship
        /// </summary>
        Task<Result<LensCameraCompatibility>> CreateAsync(LensCameraCompatibility compatibility, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates multiple lens-camera compatibility relationships
        /// </summary>
        Task<Result<List<LensCameraCompatibility>>> CreateBatchAsync(List<LensCameraCompatibility> compatibilities, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all compatibility relationships for a lens
        /// </summary>
        Task<Result<List<LensCameraCompatibility>>> GetByLensIdAsync(int lensId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all compatibility relationships for a camera
        /// </summary>
        Task<Result<List<LensCameraCompatibility>>> GetByCameraIdAsync(int cameraBodyId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a lens-camera compatibility exists
        /// </summary>
        Task<Result<bool>> ExistsAsync(int lensId, int cameraBodyId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a specific lens-camera compatibility
        /// </summary>
        Task<Result<bool>> DeleteAsync(int lensId, int cameraBodyId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes all compatibility relationships for a lens
        /// </summary>
        Task<Result<bool>> DeleteByLensIdAsync(int lensId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes all compatibility relationships for a camera
        /// </summary>
        Task<Result<bool>> DeleteByCameraIdAsync(int cameraBodyId, CancellationToken cancellationToken = default);
    }
}