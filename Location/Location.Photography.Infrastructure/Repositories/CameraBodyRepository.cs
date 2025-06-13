using Location.Core.Application.Common.Models;
using Location.Core.Infrastructure.Data;
using Location.Photography.Application.Common.Interfaces;
using Location.Photography.Domain.Entities;
using Location.Photography.Domain.Enums;
using Location.Photography.Infrastructure.Resources;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Photography.Infrastructure.Repositories
{
    public class CameraBodyRepository : ICameraBodyRepository
    {
        private readonly IDatabaseContext _context;
        private readonly ILogger<CameraBodyRepository> _logger;

        public CameraBodyRepository(IDatabaseContext context, ILogger<CameraBodyRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<CameraBody>> CreateAsync(CameraBody cameraBody, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (cameraBody == null)
                {
                    return Result<CameraBody>.Failure(AppResources.CameraBody_Error_CannotBeNull);
                }

                await Task.Run(async () =>
                {
                    await _context.InsertAsync(cameraBody).ConfigureAwait(false);
                }, cancellationToken).ConfigureAwait(false);

                _logger.LogInformation("Created camera body with ID: {CameraBodyId}", cameraBody.Id);
                return Result<CameraBody>.Success(cameraBody);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating camera body: {Name}", cameraBody?.Name);
                return Result<CameraBody>.Failure(string.Format(AppResources.CameraBody_Error_CreatingCameraBody, ex.Message));
            }
        }

        public async Task<Result<CameraBody>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var cameraBody = await Task.Run(async () =>
                {
                    var cameras = await _context.Table<CameraBody>()
                        .Where(c => c.Id == id)
                        .ToListAsync().ConfigureAwait(false);
                    return cameras.FirstOrDefault();
                }, cancellationToken).ConfigureAwait(false);

                return cameraBody != null
                    ? Result<CameraBody>.Success(cameraBody)
                    : Result<CameraBody>.Failure(AppResources.CameraBody_Error_CameraBodyNotFound);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting camera body by ID: {Id}", id);
                return Result<CameraBody>.Failure(string.Format(AppResources.CameraBody_Error_GettingCameraBody, ex.Message));
            }
        }

        public async Task<Result<List<CameraBody>>> GetPagedAsync(int skip, int take, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var cameraBodies = await Task.Run(async () =>
                {
                    // Get all cameras without complex ordering first
                    var allCameras = await _context.Table<CameraBody>()
                        .ToListAsync().ConfigureAwait(false);

                    // Do the conditional sorting in memory
                    return allCameras
                        .OrderBy(c => c.IsUserCreated ? 0 : 1)
                        .ThenBy(c => c.Name)
                        .Skip(skip)
                        .Take(take)
                        .ToList();
                }, cancellationToken).ConfigureAwait(false);

                return Result<List<CameraBody>>.Success(cameraBodies);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting paged camera bodies");
                return Result<List<CameraBody>>.Failure(string.Format(AppResources.CameraBody_Error_GettingCameraBodies, ex.Message));
            }
        }

        public async Task<Result<List<CameraBody>>> GetUserCamerasAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var userCameras = await Task.Run(async () =>
                {
                    return await _context.Table<CameraBody>()
                        .Where(c => c.IsUserCreated)
                        .OrderBy(c => c.Name)
                        .ToListAsync().ConfigureAwait(false);
                }, cancellationToken).ConfigureAwait(false);

                return Result<List<CameraBody>>.Success(userCameras);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user cameras");
                return Result<List<CameraBody>>.Failure(string.Format(AppResources.CameraBody_Error_GettingUserCameras, ex.Message));
            }
        }

        public async Task<Result<CameraBody>> UpdateAsync(CameraBody cameraBody, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (cameraBody == null)
                {
                    return Result<CameraBody>.Failure(AppResources.CameraBody_Error_CannotBeNull);
                }

                await Task.Run(async () =>
                {
                    await _context.UpdateAsync(cameraBody).ConfigureAwait(false);
                }, cancellationToken).ConfigureAwait(false);

                _logger.LogInformation("Updated camera body with ID: {CameraBodyId}", cameraBody.Id);
                return Result<CameraBody>.Success(cameraBody);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating camera body: {Id}", cameraBody?.Id);
                return Result<CameraBody>.Failure(string.Format(AppResources.CameraBody_Error_UpdatingCameraBody, ex.Message));
            }
        }

        public async Task<Result<bool>> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var result = await Task.Run(async () =>
                {
                    var cameraBody = await _context.Table<CameraBody>()
                        .Where(c => c.Id == id)
                        .FirstOrDefaultAsync().ConfigureAwait(false);

                    if (cameraBody != null)
                    {
                        return await _context.DeleteAsync(cameraBody).ConfigureAwait(false);
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
                _logger.LogError(ex, "Error deleting camera body: {Id}", id);
                return Result<bool>.Failure(string.Format(AppResources.CameraBody_Error_DeletingCameraBody, ex.Message));
            }
        }

        public async Task<Result<List<CameraBody>>> SearchByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (string.IsNullOrWhiteSpace(name))
                {
                    return Result<List<CameraBody>>.Success(new List<CameraBody>());
                }

                var cameraBodies = await Task.Run(async () =>
                {
                    var allCameras = await _context.Table<CameraBody>().ToListAsync().ConfigureAwait(false);
                    var normalizedSearch = NormalizeName(name);

                    return allCameras
                        .Where(c => IsFuzzyMatch(NormalizeName(c.Name), normalizedSearch))
                        .OrderBy(c => c.IsUserCreated ? 0 : 1)
                        .ThenBy(c => c.Name)
                        .ToList();
                }, cancellationToken).ConfigureAwait(false);

                return Result<List<CameraBody>>.Success(cameraBodies);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching camera bodies by name: {Name}", name);
                return Result<List<CameraBody>>.Failure(string.Format(AppResources.CameraBody_Error_SearchingCameraBodies, ex.Message));
            }
        }

        public async Task<Result<List<CameraBody>>> GetByMountTypeAsync(MountType mountType, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var cameraBodies = await Task.Run(async () =>
                {
                    return await _context.Table<CameraBody>()
                        .Where(c => c.MountType == mountType)
                        .OrderBy(c => c.IsUserCreated ? 0 : 1)
                        .ThenBy(c => c.Name)
                        .ToListAsync().ConfigureAwait(false);
                }, cancellationToken).ConfigureAwait(false);

                return Result<List<CameraBody>>.Success(cameraBodies);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting camera bodies by mount type: {MountType}", mountType);
                return Result<List<CameraBody>>.Failure(string.Format(AppResources.CameraBody_Error_GettingCameraBodies, ex.Message));
            }
        }

        public async Task<Result<int>> GetTotalCountAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var count = await Task.Run(async () =>
                {
                    return await _context.Table<CameraBody>().CountAsync().ConfigureAwait(false);
                }, cancellationToken).ConfigureAwait(false);

                return Result<int>.Success(count);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting total camera body count");
                return Result<int>.Failure(string.Format(AppResources.CameraBody_Error_GettingCount, ex.Message));
            }
        }

        private string NormalizeName(string name)
        {
            return name?.ToLowerInvariant()
                      .Replace(" ", "")
                      .Replace("-", "")
                      .Replace("_", "") ?? string.Empty;
        }

        private bool IsFuzzyMatch(string normalized1, string normalized2)
        {
            return normalized1.Contains(normalized2) || normalized2.Contains(normalized1);
        }
    }
}