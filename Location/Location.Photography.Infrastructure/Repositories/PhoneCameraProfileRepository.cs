// Location.Photography.Infrastructure/Repositories/PhoneCameraProfileRepository.cs
using Location.Core.Application.Common.Models;
using Location.Core.Infrastructure.Data;
using Location.Core.Infrastructure.Data.Entities;
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
    public class PhoneCameraProfileRepository : IPhoneCameraProfileRepository
    {
        private readonly IDatabaseContext _context;
        private readonly ILogger<PhoneCameraProfileRepository> _logger;

        public PhoneCameraProfileRepository(IDatabaseContext context, ILogger<PhoneCameraProfileRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<PhoneCameraProfile>> CreateAsync(PhoneCameraProfile profile, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (profile == null)
                {
                    return Result<PhoneCameraProfile>.Failure("Profile cannot be null");
                }

                var entity = MapToEntity(profile);

                await Task.Run(async () =>
                {
                    await _context.InsertAsync(entity);
                }, cancellationToken);

                var createdProfile = MapToDomain(entity);

                _logger.LogInformation("Created phone camera profile with ID: {ProfileId}", entity.Id);
                return Result<PhoneCameraProfile>.Success(createdProfile);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating phone camera profile");
                return Result<PhoneCameraProfile>.Failure($"Error creating profile: {ex.Message}");
            }
        }

        public async Task<Result<PhoneCameraProfile>> GetActiveProfileAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var entity = await Task.Run(async () =>
                {
                    var profiles = await _context.Table<PhoneCameraProfileEntity>()
                        .Where(p => p.IsActive)
                        .OrderByDescending(p => p.DateCalibrated)
                        .ToListAsync();

                    return profiles.FirstOrDefault();
                }, cancellationToken);

                if (entity == null)
                {
                    return Result<PhoneCameraProfile>.Failure("No active profile found");
                }

                var profile = MapToDomain(entity);
                return Result<PhoneCameraProfile>.Success(profile);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active phone camera profile");
                return Result<PhoneCameraProfile>.Failure($"Error retrieving active profile: {ex.Message}");
            }
        }

        public async Task<Result<PhoneCameraProfile>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var entity = await Task.Run(async () =>
                {
                    return await _context.GetAsync<PhoneCameraProfileEntity>(id);
                }, cancellationToken);

                if (entity == null)
                {
                    return Result<PhoneCameraProfile>.Failure("Profile not found");
                }

                var profile = MapToDomain(entity);
                return Result<PhoneCameraProfile>.Success(profile);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting phone camera profile by ID: {ProfileId}", id);
                return Result<PhoneCameraProfile>.Failure($"Error retrieving profile: {ex.Message}");
            }
        }

        public async Task<Result<List<PhoneCameraProfile>>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var entities = await Task.Run(async () =>
                {
                    return await _context.Table<PhoneCameraProfileEntity>()
                        .OrderByDescending(p => p.DateCalibrated)
                        .ToListAsync();
                }, cancellationToken);

                var profiles = entities.Select(MapToDomain).ToList();
                return Result<List<PhoneCameraProfile>>.Success(profiles);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all phone camera profiles");
                return Result<List<PhoneCameraProfile>>.Failure($"Error retrieving profiles: {ex.Message}");
            }
        }

        public async Task<Result<PhoneCameraProfile>> UpdateAsync(PhoneCameraProfile profile, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (profile == null)
                {
                    return Result<PhoneCameraProfile>.Failure("Profile cannot be null");
                }

                var entity = MapToEntity(profile);

                await Task.Run(async () =>
                {
                    await _context.UpdateAsync(entity);
                }, cancellationToken);

                _logger.LogInformation("Updated phone camera profile with ID: {ProfileId}", entity.Id);
                return Result<PhoneCameraProfile>.Success(profile);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating phone camera profile");
                return Result<PhoneCameraProfile>.Failure($"Error updating profile: {ex.Message}");
            }
        }

        public async Task<Result<bool>> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var deleteResult = await Task.Run(async () =>
                {
                    var entity = await _context.GetAsync<PhoneCameraProfileEntity>(id);
                    if (entity == null)
                        return false;

                    await _context.DeleteAsync(entity);
                    return true;
                }, cancellationToken);

                if (!deleteResult)
                {
                    return Result<bool>.Failure("Profile not found");
                }

                _logger.LogInformation("Deleted phone camera profile with ID: {ProfileId}", id);
                return Result<bool>.Success(true);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting phone camera profile with ID: {ProfileId}", id);
                return Result<bool>.Failure($"Error deleting profile: {ex.Message}");
            }
        }

        public async Task<Result<bool>> SetActiveProfileAsync(int profileId, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                await Task.Run(async () =>
                {
                    // Deactivate all profiles
                    await _context.ExecuteAsync("UPDATE PhoneCameraProfiles SET IsActive = 0");

                    // Activate the specified profile
                    await _context.ExecuteAsync("UPDATE PhoneCameraProfiles SET IsActive = 1 WHERE Id = ?", profileId);
                }, cancellationToken);

                _logger.LogInformation("Set phone camera profile {ProfileId} as active", profileId);
                return Result<bool>.Success(true);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting active phone camera profile: {ProfileId}", profileId);
                return Result<bool>.Failure($"Error setting active profile: {ex.Message}");
            }
        }

        public async Task<Result<List<PhoneCameraProfile>>> GetByPhoneModelAsync(string phoneModel, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (string.IsNullOrWhiteSpace(phoneModel))
                {
                    return Result<List<PhoneCameraProfile>>.Success(new List<PhoneCameraProfile>());
                }

                var entities = await Task.Run(async () =>
                {
                    return await _context.Table<PhoneCameraProfileEntity>()
                        .Where(p => p.PhoneModel.Contains(phoneModel))
                        .OrderByDescending(p => p.DateCalibrated)
                        .ToListAsync();
                }, cancellationToken);

                var profiles = entities.Select(MapToDomain).ToList();
                return Result<List<PhoneCameraProfile>>.Success(profiles);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting phone camera profiles by model: {PhoneModel}", phoneModel);
                return Result<List<PhoneCameraProfile>>.Failure($"Error retrieving profiles: {ex.Message}");
            }
        }

        private PhoneCameraProfileEntity MapToEntity(PhoneCameraProfile domain)
        {
            return new PhoneCameraProfileEntity
            {
                Id = domain.Id,
                PhoneModel = domain.PhoneModel,
                MainLensFocalLength = domain.MainLensFocalLength,
                MainLensFOV = domain.MainLensFOV,
                UltraWideFocalLength = domain.UltraWideFocalLength,
                TelephotoFocalLength = domain.TelephotoFocalLength,
                DateCalibrated = domain.DateCalibrated,
                IsActive = domain.IsActive
            };
        }

        private PhoneCameraProfile MapToDomain(PhoneCameraProfileEntity entity)
        {
            var profile = new PhoneCameraProfile(
                entity.PhoneModel,
                entity.MainLensFocalLength,
                entity.MainLensFOV,
                entity.UltraWideFocalLength,
                entity.TelephotoFocalLength);

            // Set the ID via reflection or expose a method to set it
            var idProperty = typeof(PhoneCameraProfile).GetProperty("Id");
            idProperty?.SetValue(profile, entity.Id);

            if (!entity.IsActive)
                profile.Deactivate();

            return profile;
        }
    }
}