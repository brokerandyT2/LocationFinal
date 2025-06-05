using Location.Core.Application.Common.Models;
using Location.Photography.Application.Commands.CameraEvaluation;
using Location.Photography.Application.Queries.CameraEvaluation;
using Location.Photography.Domain.Enums;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Photography.Application.Services
{
    public interface ICameraDataService
    {
        /// <summary>
        /// Gets camera bodies with infinite scroll support, user cameras first
        /// </summary>
        Task<Result<GetCameraBodiesResultDto>> GetCameraBodiesAsync(
            int skip = 0,
            int take = 20,
            bool userCamerasOnly = false,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets lenses with infinite scroll support, user lenses first
        /// </summary>
        Task<Result<GetLensesResultDto>> GetLensesAsync(
            int skip = 0,
            int take = 20,
            bool userLensesOnly = false,
            int? compatibleWithCameraId = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a new camera body with fuzzy duplicate detection
        /// </summary>
        Task<Result<CameraBodyDto>> CreateCameraBodyAsync(
            string name,
            string sensorType,
            double sensorWidth,
            double sensorHeight,
            MountType mountType,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a new lens with required camera compatibility
        /// </summary>
        Task<Result<CreateLensResultDto>> CreateLensAsync(
            double minMM,
            double? maxMM,
            double? minFStop,
            double? maxFStop,
            List<int> compatibleCameraIds,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks for duplicate camera by name with fuzzy matching
        /// </summary>
        Task<Result<List<CameraBodyDto>>> CheckDuplicateCameraAsync(
            string name,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks for duplicate lens by focal length with fuzzy matching
        /// </summary>
        Task<Result<List<LensDto>>> CheckDuplicateLensAsync(
            double focalLength,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets available mount types for dropdown
        /// </summary>
        Task<Result<List<MountTypeDto>>> GetMountTypesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates lens-camera compatibility relationships
        /// </summary>
        Task<Result<bool>> UpdateLensCompatibilityAsync(
            int lensId,
            List<int> compatibleCameraIds,
            CancellationToken cancellationToken = default);
        /// <summary>
        /// Gets user's saved camera bodies with infinite scroll support
        /// </summary>
        Task<Result<GetCameraBodiesResultDto>> GetUserCameraBodiesAsync(
            string userId,
            int skip = 0,
            int take = 20,
            CancellationToken cancellationToken = default);
    }

    public class MountTypeDto
    {
        public MountType Value { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
    }
}