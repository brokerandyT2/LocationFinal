using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Interfaces.Persistence;
using Location.Core.Application.Common.Models;
using Location.Core.Domain.Entities;
using Location.Core.Infrastructure.Data.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Core.Infrastructure.Data.Repositories
{
    public class SettingRepository : ISettingRepository
    {
        private readonly IDatabaseContext _context;
        private readonly ILogger<SettingRepository> _logger;

        public SettingRepository(IDatabaseContext context, ILogger<SettingRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<Setting>> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
        {
            try
            {
                var entity = await _context.Table<SettingEntity>()
                    .Where(s => s.Key == key)
                    .FirstOrDefaultAsync();

                if (entity == null)
                {
                    return Result<Setting>.Failure($"Setting with key '{key}' not found");
                }

                var setting = MapToDomain(entity);
                return Result<Setting>.Success(setting);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving setting with key {Key}", key);
                return Result<Setting>.Failure($"Failed to retrieve setting: {ex.Message}");
            }
        }

        public async Task<Result<List<Setting>>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var entities = await _context.Table<SettingEntity>()
                    .OrderBy(s => s.Key)
                    .ToListAsync();

                var settings = entities.Select(MapToDomain).ToList();
                return Result<List<Setting>>.Success(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all settings");
                return Result<List<Setting>>.Failure($"Failed to retrieve settings: {ex.Message}");
            }
        }

        public async Task<Result<Setting>> CreateAsync(Setting setting, CancellationToken cancellationToken = default)
        {
            try
            {
                // Check if key already exists
                var existing = await _context.Table<SettingEntity>()
                    .Where(s => s.Key == setting.Key)
                    .FirstOrDefaultAsync();

                if (existing != null)
                {
                    return Result<Setting>.Failure($"Setting with key '{setting.Key}' already exists");
                }

                var entity = MapToEntity(setting);
                entity.Timestamp = DateTime.UtcNow;

                await _context.InsertAsync(entity);

                // Update domain object with generated ID
                SetPrivateProperty(setting, "Id", entity.Id);

                _logger.LogInformation("Created setting with key {Key}", setting.Key);
                return Result<Setting>.Success(setting);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating setting");
                return Result<Setting>.Failure($"Failed to create setting: {ex.Message}");
            }
        }

        public async Task<Result<Setting>> UpdateAsync(Setting setting, CancellationToken cancellationToken = default)
        {
            try
            {
                var existingResult = await GetByKeyAsync(setting.Key, cancellationToken);
                if (!existingResult.IsSuccess)
                {
                    return Result<Setting>.Failure($"Setting with key '{setting.Key}' not found");
                }

                var entity = MapToEntity(setting);
                entity.Id = existingResult.Data.Id; // Use existing ID
                entity.Timestamp = DateTime.UtcNow;

                await _context.UpdateAsync(entity);

                _logger.LogInformation("Updated setting with key {Key}", setting.Key);
                return Result<Setting>.Success(setting);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating setting with key {Key}", setting.Key);
                return Result<Setting>.Failure($"Failed to update setting: {ex.Message}");
            }
        }

        public async Task<Result<bool>> DeleteAsync(string key, CancellationToken cancellationToken = default)
        {
            try
            {
                var entity = await _context.Table<SettingEntity>()
                    .Where(s => s.Key == key)
                    .FirstOrDefaultAsync();

                if (entity == null)
                {
                    return Result<bool>.Failure($"Setting with key '{key}' not found");
                }

                await _context.DeleteAsync(entity);

                _logger.LogInformation("Deleted setting with key {Key}", key);
                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting setting with key {Key}", key);
                return Result<bool>.Failure($"Failed to delete setting: {ex.Message}");
            }
        }

        public async Task<Result<Setting>> UpsertAsync(Setting setting, CancellationToken cancellationToken = default)
        {
            try
            {
                var existingEntity = await _context.Table<SettingEntity>()
                    .Where(s => s.Key == setting.Key)
                    .FirstOrDefaultAsync();

                var entity = MapToEntity(setting);
                entity.Timestamp = DateTime.UtcNow;

                if (existingEntity != null)
                {
                    // Update existing
                    entity.Id = existingEntity.Id;
                    await _context.UpdateAsync(entity);
                    _logger.LogInformation("Updated setting with key {Key} via upsert", setting.Key);
                }
                else
                {
                    // Create new
                    await _context.InsertAsync(entity);
                    _logger.LogInformation("Created setting with key {Key} via upsert", setting.Key);
                }

                // Update domain object with ID
                SetPrivateProperty(setting, "Id", entity.Id);

                return Result<Setting>.Success(setting);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error upserting setting with key {Key}", setting.Key);
                return Result<Setting>.Failure($"Failed to upsert setting: {ex.Message}");
            }
        }

        public async Task<Result<Dictionary<string, string>>> GetAllAsDictionaryAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var entities = await _context.Table<SettingEntity>().ToListAsync();

                var dictionary = entities.ToDictionary(
                    e => e.Key,
                    e => e.Value);

                return Result<Dictionary<string, string>>.Success(dictionary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving settings as dictionary");
                return Result<Dictionary<string, string>>.Failure($"Failed to retrieve settings: {ex.Message}");
            }
        }

        #region Mapping Methods

        private Setting MapToDomain(SettingEntity entity)
        {
            // Create setting using reflection
            var setting = CreateSettingViaReflection(entity.Key, entity.Value, entity.Description);

            // Set properties
            SetPrivateProperty(setting, "Id", entity.Id);
            SetPrivateProperty(setting, "Timestamp", entity.Timestamp);

            return setting;
        }

        private SettingEntity MapToEntity(Setting setting)
        {
            return new SettingEntity
            {
                Id = setting.Id,
                Key = setting.Key,
                Value = setting.Value,
                Description = setting.Description,
                Timestamp = setting.Timestamp
            };
        }

        private Setting CreateSettingViaReflection(string key, string value, string description)
        {
            var type = typeof(Setting);
            var constructor = type.GetConstructor(
                new[] { typeof(string), typeof(string), typeof(string) });

            if (constructor == null)
            {
                throw new InvalidOperationException("Cannot find Setting constructor");
            }

            return (Setting)constructor.Invoke(new object[] { key, value, description });
        }

        private void SetPrivateProperty(object obj, string propertyName, object value)
        {
            var property = obj.GetType().GetProperty(propertyName);
            if (property == null)
            {
                var field = obj.GetType().GetField(propertyName,
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                field?.SetValue(obj, value);
            }
            else
            {
                property.SetValue(obj, value);
            }
        }

        #endregion
    }
}