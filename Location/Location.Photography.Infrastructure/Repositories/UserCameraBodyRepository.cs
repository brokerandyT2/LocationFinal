// Location.Photography.Infrastructure/Repositories/UserCameraBodyRepository.cs
using Location.Core.Application.Common.Models;
using Location.Core.Infrastructure.Data;
using Location.Photography.Application.Common.Interfaces;
using Location.Photography.Domain.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Photography.Infrastructure.Repositories
{
    public class UserCameraBodyRepository : IUserCameraBodyRepository
    {
        private readonly IDatabaseContext _context;
        private readonly ILogger<UserCameraBodyRepository> _logger;

        public UserCameraBodyRepository(IDatabaseContext context, ILogger<UserCameraBodyRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<UserCameraBody>> CreateAsync(UserCameraBody userCameraBody, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (userCameraBody == null)
                {
                    return Result<UserCameraBody>.Failure("UserCameraBody cannot be null");
                }

                // Check if already exists
                var existsResult = await ExistsAsync(userCameraBody.UserId, userCameraBody.CameraBodyId, cancellationToken);
                if (existsResult.IsSuccess && existsResult.Data)
                {
                    return Result<UserCameraBody>.Failure("Camera body is already saved by this user");
                }

                await _context.InsertAsync(userCameraBody);
                return Result<UserCameraBody>.Success(userCameraBody);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user camera body for UserId: {UserId}, CameraBodyId: {CameraBodyId}",
                    userCameraBody?.UserId, userCameraBody?.CameraBodyId);
                return Result<UserCameraBody>.Failure($"Error saving camera body: {ex.Message}");
            }
        }

        public async Task<Result<List<UserCameraBody>>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var userCameraBodies = await _context.Table<UserCameraBody>()
                    .ToListAsync();

                return Result<List<UserCameraBody>>.Success(userCameraBodies);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user camera bodies for UserId: {UserId}", userId);
                return Result<List<UserCameraBody>>.Failure($"Error retrieving saved cameras: {ex.Message}");
            }
        }

        public async Task<Result<UserCameraBody>> GetByUserAndCameraIdAsync(string userId, int cameraBodyId, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Result<UserCameraBody>.Failure("UserId cannot be null or empty");
                }

                var userCameraBody = await _context.Table<UserCameraBody>()
                    .Where(ucb => ucb.UserId == userId && ucb.CameraBodyId == cameraBodyId)
                    .FirstOrDefaultAsync();

                if (userCameraBody == null)
                {
                    return Result<UserCameraBody>.Failure("User camera body not found");
                }

                return Result<UserCameraBody>.Success(userCameraBody);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user camera body for UserId: {UserId}, CameraBodyId: {CameraBodyId}",
                    userId, cameraBodyId);
                return Result<UserCameraBody>.Failure($"Error retrieving saved camera: {ex.Message}");
            }
        }

        public async Task<Result<bool>> ExistsAsync(string userId, int cameraBodyId, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Result<bool>.Success(false);
                }

                var count = await _context.Table<UserCameraBody>()
                    .Where(ucb => ucb.UserId == userId && ucb.CameraBodyId == cameraBodyId)
                    .CountAsync();

                return Result<bool>.Success(count > 0);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if user camera body exists for UserId: {UserId}, CameraBodyId: {CameraBodyId}",
                    userId, cameraBodyId);
                return Result<bool>.Failure($"Error checking saved camera: {ex.Message}");
            }
        }

        public async Task<Result<UserCameraBody>> UpdateAsync(UserCameraBody userCameraBody, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (userCameraBody == null)
                {
                    return Result<UserCameraBody>.Failure("UserCameraBody cannot be null");
                }

                await _context.UpdateAsync(userCameraBody);
                return Result<UserCameraBody>.Success(userCameraBody);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user camera body");
                return Result<UserCameraBody>.Failure($"Error updating saved camera: {ex.Message}");
            }
        }

        public async Task<Result<bool>> DeleteAsync(string userId, int cameraBodyId, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var getUserCameraBodyResult = await GetByUserAndCameraIdAsync(userId, cameraBodyId, cancellationToken);
                if (!getUserCameraBodyResult.IsSuccess)
                {
                    return Result<bool>.Failure(getUserCameraBodyResult.ErrorMessage ?? "User camera body not found");
                }

                var deleted = await _context.DeleteAsync(getUserCameraBodyResult.Data);
                return Result<bool>.Success(deleted > 0);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user camera body for UserId: {UserId}, CameraBodyId: {CameraBodyId}",
                    userId, cameraBodyId);
                return Result<bool>.Failure($"Error removing saved camera: {ex.Message}");
            }
        }

        public async Task<Result<List<UserCameraBody>>> GetFavoritesByUserIdAsync(string userId, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var favorites = await _context.Table<UserCameraBody>()
                    .Where(ucb => ucb.UserId == userId && ucb.IsFavorite)
                    .OrderBy(ucb => ucb.DateSaved)
                    .ToListAsync();

                return Result<List<UserCameraBody>>.Success(favorites);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving favorite cameras for UserId: {UserId}", userId);
                return Result<List<UserCameraBody>>.Failure($"Error retrieving favorite cameras: {ex.Message}");
            }
        }
    }
}