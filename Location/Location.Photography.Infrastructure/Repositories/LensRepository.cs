using Location.Core.Application.Common.Models;
using Location.Core.Infrastructure.Data;
using Location.Photography.Application.Common.Interfaces;
using Location.Photography.Domain.Entities;
using Location.Photography.Infrastructure.Resources;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Photography.Infrastructure.Repositories
{
    public class LensRepository : ILensRepository
    {
        private readonly IDatabaseContext _context;
        private readonly ILogger<LensRepository> _logger;

        public LensRepository(IDatabaseContext context, ILogger<LensRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<Lens>> CreateAsync(Lens lens, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (lens == null)
                {
                    return Result<Lens>.Failure(AppResources.Lens_Error_CannotBeNull);
                }

                await Task.Run(async () =>
                {
                    await _context.InsertAsync(lens).ConfigureAwait(false);
                }, cancellationToken).ConfigureAwait(false);

                _logger.LogInformation("Created lens with ID: {LensId}", lens.Id);
                return Result<Lens>.Success(lens);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating lens");
                return Result<Lens>.Failure(string.Format(AppResources.Lens_Error_CreatingLens, ex.Message));
            }
        }

        public async Task<Result<Lens>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var lens = await Task.Run(async () =>
                {
                    var lenses = await _context.Table<Lens>()
                        .Where(l => l.Id == id)
                        .ToListAsync().ConfigureAwait(false);
                    return lenses.FirstOrDefault();
                }, cancellationToken).ConfigureAwait(false);

                return lens != null
                    ? Result<Lens>.Success(lens)
                    : Result<Lens>.Failure(AppResources.Lens_Error_LensNotFound);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting lens by ID: {Id}", id);
                return Result<Lens>.Failure(string.Format(AppResources.Lens_Error_GettingLens, ex.Message));
            }
        }

        public async Task<Result<List<Lens>>> GetPagedAsync(int skip, int take, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var lenses = await Task.Run(async () =>
                {
                    var allLenses = await _context.Table<Lens>()
                        .OrderBy(l => l.IsUserCreated ? 0 : 1)
                        .ThenBy(l => l.MinMM)
                        .ToListAsync().ConfigureAwait(false);

                    return allLenses.Skip(skip).Take(take).ToList();
                }, cancellationToken).ConfigureAwait(false);

                return Result<List<Lens>>.Success(lenses);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting paged lenses");
                return Result<List<Lens>>.Failure(string.Format(AppResources.Lens_Error_GettingAllLenses, ex.Message));
            }
        }

        public async Task<Result<List<Lens>>> GetUserLensesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var userLenses = await Task.Run(async () =>
                {
                    return await _context.Table<Lens>()
                        .Where(l => l.IsUserCreated)
                        .OrderBy(l => l.MinMM)
                        .ToListAsync().ConfigureAwait(false);
                }, cancellationToken).ConfigureAwait(false);

                return Result<List<Lens>>.Success(userLenses);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user lenses");
                return Result<List<Lens>>.Failure(string.Format(AppResources.Lens_Error_GettingUserLenses, ex.Message));
            }
        }

        public async Task<Result<Lens>> UpdateAsync(Lens lens, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (lens == null)
                {
                    return Result<Lens>.Failure(AppResources.Lens_Error_CannotBeNull);
                }

                await Task.Run(async () =>
                {
                    await _context.UpdateAsync(lens).ConfigureAwait(false);
                }, cancellationToken).ConfigureAwait(false);

                _logger.LogInformation("Updated lens with ID: {LensId}", lens.Id);
                return Result<Lens>.Success(lens);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating lens: {Id}", lens?.Id);
                return Result<Lens>.Failure(string.Format(AppResources.Lens_Error_UpdatingLens, ex.Message));
            }
        }

        public async Task<Result<bool>> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var result = await Task.Run(async () =>
                {
                    var lens = await _context.Table<Lens>()
                        .Where(l => l.Id == id)
                        .FirstOrDefaultAsync().ConfigureAwait(false);

                    if (lens != null)
                    {
                        return await _context.DeleteAsync(lens).ConfigureAwait(false);
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
                _logger.LogError(ex, "Error deleting lens: {Id}", id);
                return Result<bool>.Failure(string.Format(AppResources.Lens_Error_DeletingLens, ex.Message));
            }
        }

        public async Task<Result<List<Lens>>> SearchByFocalLengthAsync(double focalLength, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var lenses = await Task.Run(async () =>
                {
                    var allLenses = await _context.Table<Lens>().ToListAsync().ConfigureAwait(false);

                    return allLenses
                        .Where(l => (l.IsPrime && Math.Abs(l.MinMM - focalLength) <= 5) ||
                                   (!l.IsPrime && l.MaxMM.HasValue && focalLength >= l.MinMM && focalLength <= l.MaxMM.Value))
                        .OrderBy(l => l.IsUserCreated ? 0 : 1)
                        .ThenBy(l => Math.Abs(l.MinMM - focalLength))
                        .ToList();
                }, cancellationToken).ConfigureAwait(false);

                return Result<List<Lens>>.Success(lenses);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching lenses by focal length: {FocalLength}", focalLength);
                return Result<List<Lens>>.Failure(string.Format(AppResources.Lens_Error_SearchingLenses, ex.Message));
            }
        }

        public async Task<Result<List<Lens>>> GetCompatibleLensesAsync(int cameraBodyId, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var compatibleLenses = await Task.Run(async () =>
                {
                    var compatibilities = await _context.Table<LensCameraCompatibility>()
                        .Where(c => c.CameraBodyId == cameraBodyId)
                        .ToListAsync().ConfigureAwait(false);

                    var lensIds = compatibilities.Select(c => c.LensId).ToList();

                    var lenses = await _context.Table<Lens>()
                        .Where(l => lensIds.Contains(l.Id))
                        .OrderBy(l => l.MinMM)
                        .ThenBy(l => l.MaxMM)
                        .ToListAsync().ConfigureAwait(false);

                    return lenses;
                }, cancellationToken).ConfigureAwait(false);

                return Result<List<Lens>>.Success(compatibleLenses);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting compatible lenses for camera: {CameraBodyId}", cameraBodyId);
                return Result<List<Lens>>.Failure(string.Format(AppResources.Lens_Error_GettingAllLenses, ex.Message));
            }
        }

        public async Task<Result<int>> GetTotalCountAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var count = await Task.Run(async () =>
                {
                    return await _context.Table<Lens>().CountAsync().ConfigureAwait(false);
                }, cancellationToken).ConfigureAwait(false);

                return Result<int>.Success(count);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting total lens count");
                return Result<int>.Failure(string.Format(AppResources.Lens_Error_GettingAllLenses, ex.Message));
            }
        }
    }
}