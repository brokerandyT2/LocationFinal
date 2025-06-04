using Location.Core.Application.Common.Models;
using Location.Core.Infrastructure.Data;
using Location.Photography.Application.Common.Interfaces;
using Location.Photography.Domain.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Photography.Infrastructure.Repositories
{
    public class LensCameraCompatibilityRepository : ILensCameraCompatibilityRepository
    {
        private readonly IDatabaseContext _context;
        private readonly ILogger<LensCameraCompatibilityRepository> _logger;

        public LensCameraCompatibilityRepository(IDatabaseContext context, ILogger<LensCameraCompatibilityRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<LensCameraCompatibility>> CreateAsync(LensCameraCompatibility compatibility, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (compatibility == null)
                {
                    return Result<LensCameraCompatibility>.Failure("Compatibility cannot be null");
                }

                await Task.Run(async () =>
                {
                    await _context.InsertAsync(compatibility).ConfigureAwait(false);
                }, cancellationToken).ConfigureAwait(false);

                _logger.LogInformation("Created lens-camera compatibility: LensId={LensId}, CameraId={CameraId}",
                    compatibility.LensId, compatibility.CameraBodyId);
                return Result<LensCameraCompatibility>.Success(compatibility);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating lens-camera compatibility");
                return Result<LensCameraCompatibility>.Failure($"Error creating compatibility: {ex.Message}");
            }
        }

        public async Task<Result<List<LensCameraCompatibility>>> CreateBatchAsync(List<LensCameraCompatibility> compatibilities, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (compatibilities == null || !compatibilities.Any())
                {
                    return Result<List<LensCameraCompatibility>>.Success(new List<LensCameraCompatibility>());
                }

                await Task.Run(async () =>
                {
                    await _context.InsertAllAsync(compatibilities).ConfigureAwait(false);
                }, cancellationToken).ConfigureAwait(false);

                _logger.LogInformation("Created {Count} lens-camera compatibilities", compatibilities.Count);
                return Result<List<LensCameraCompatibility>>.Success(compatibilities);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating batch lens-camera compatibilities");
                return Result<List<LensCameraCompatibility>>.Failure($"Error creating compatibilities: {ex.Message}");
            }
        }

        public async Task<Result<List<LensCameraCompatibility>>> GetByLensIdAsync(int lensId, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var compatibilities = await Task.Run(async () =>
                {
                    return await _context.Table<LensCameraCompatibility>()
                        .Where(c => c.LensId == lensId)
                        .ToListAsync().ConfigureAwait(false);
                }, cancellationToken).ConfigureAwait(false);

                return Result<List<LensCameraCompatibility>>.Success(compatibilities);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting compatibilities by lens ID: {LensId}", lensId);
                return Result<List<LensCameraCompatibility>>.Failure($"Error getting compatibilities: {ex.Message}");
            }
        }

        public async Task<Result<List<LensCameraCompatibility>>> GetByCameraIdAsync(int cameraBodyId, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var compatibilities = await Task.Run(async () =>
                {
                    return await _context.Table<LensCameraCompatibility>()
                        .Where(c => c.CameraBodyId == cameraBodyId)
                        .ToListAsync().ConfigureAwait(false);
                }, cancellationToken).ConfigureAwait(false);

                return Result<List<LensCameraCompatibility>>.Success(compatibilities);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting compatibilities by camera ID: {CameraBodyId}", cameraBodyId);
                return Result<List<LensCameraCompatibility>>.Failure($"Error getting compatibilities: {ex.Message}");
            }
        }

        public async Task<Result<bool>> ExistsAsync(int lensId, int cameraBodyId, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var exists = await Task.Run(async () =>
                {
                    var count = await _context.Table<LensCameraCompatibility>()
                        .CountAsync(c => c.LensId == lensId && c.CameraBodyId == cameraBodyId)
                        .ConfigureAwait(false);
                    return count > 0;
                }, cancellationToken).ConfigureAwait(false);

                return Result<bool>.Success(exists);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking compatibility existence: LensId={LensId}, CameraId={CameraBodyId}", lensId, cameraBodyId);
                return Result<bool>.Failure($"Error checking compatibility: {ex.Message}");
            }
        }

        public async Task<Result<bool>> DeleteAsync(int lensId, int cameraBodyId, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var result = await Task.Run(async () =>
                {
                    var compatibility = await _context.Table<LensCameraCompatibility>()
                        .Where(c => c.LensId == lensId && c.CameraBodyId == cameraBodyId)
                        .FirstOrDefaultAsync().ConfigureAwait(false);

                    if (compatibility != null)
                    {
                        return await _context.DeleteAsync(compatibility).ConfigureAwait(false);
                    }
                    return 0;
                }, cancellationToken).ConfigureAwait(false);

                return Result<bool>.Success(result > 0);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting compatibility: LensId={LensId}, CameraId={CameraBodyId}", lensId, cameraBodyId);
                return Result<bool>.Failure($"Error deleting compatibility: {ex.Message}");
            }
        }

        public async Task<Result<bool>> DeleteByLensIdAsync(int lensId, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var result = await Task.Run(async () =>
                {
                    var compatibilities = await _context.Table<LensCameraCompatibility>()
                        .Where(c => c.LensId == lensId)
                        .ToListAsync().ConfigureAwait(false);

                    var deletedCount = 0;
                    foreach (var compatibility in compatibilities)
                    {
                        deletedCount += await _context.DeleteAsync(compatibility).ConfigureAwait(false);
                    }
                    return deletedCount;
                }, cancellationToken).ConfigureAwait(false);

                return Result<bool>.Success(result > 0);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting compatibilities by lens ID: {LensId}", lensId);
                return Result<bool>.Failure($"Error deleting compatibilities: {ex.Message}");
            }
        }

        public async Task<Result<bool>> DeleteByCameraIdAsync(int cameraBodyId, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var result = await Task.Run(async () =>
                {
                    var compatibilities = await _context.Table<LensCameraCompatibility>()
                        .Where(c => c.CameraBodyId == cameraBodyId)
                        .ToListAsync().ConfigureAwait(false);

                    var deletedCount = 0;
                    foreach (var compatibility in compatibilities)
                    {
                        deletedCount += await _context.DeleteAsync(compatibility).ConfigureAwait(false);
                    }
                    return deletedCount;
                }, cancellationToken).ConfigureAwait(false);

                return Result<bool>.Success(result > 0);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting compatibilities by camera ID: {CameraBodyId}", cameraBodyId);
                return Result<bool>.Failure($"Error deleting compatibilities: {ex.Message}");
            }
        }
    }
}