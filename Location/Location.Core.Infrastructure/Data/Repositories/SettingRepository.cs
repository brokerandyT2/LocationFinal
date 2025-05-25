using Location.Core.Application.Common.Interfaces.Persistence;
using Location.Core.Domain.Entities;
using Location.Core.Infrastructure.Data.Entities;
using Location.Core.Infrastructure.Services;
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
        private readonly IInfrastructureExceptionMappingService _exceptionMapper;

        public SettingRepository(IDatabaseContext context, ILogger<SettingRepository> logger, IInfrastructureExceptionMappingService exceptionMapper)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _exceptionMapper = exceptionMapper ?? throw new ArgumentNullException(nameof(exceptionMapper));
        }

        public async Task<Setting?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    var entity = await _context.GetAsync<SettingEntity>(id);
                    return entity != null ? MapToDomain(entity) : null;
                },
                _exceptionMapper,
                "GetById",
                "setting",
                _logger);
        }

        public async Task<Setting?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    var entity = await _context.Table<SettingEntity>()
                        .Where(s => s.Key == key)
                        .FirstOrDefaultAsync().ConfigureAwait(false);

                    return entity != null ? MapToDomain(entity) : null;
                },
                _exceptionMapper,
                "GetByKey",
                "setting",
                _logger);
        }

        public async Task<IEnumerable<Setting>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    var entities = await _context.Table<SettingEntity>()
                        .OrderBy(s => s.Key)
                        .ToListAsync();

                    return entities.Select(MapToDomain);
                },
                _exceptionMapper,
                "GetAll",
                "setting",
                _logger);
        }

        public async Task<IEnumerable<Setting>> GetByKeysAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default)
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    var keysList = keys.ToList();
                    var entities = await _context.Table<SettingEntity>()
                        .Where(s => keysList.Contains(s.Key))
                        .ToListAsync();

                    return entities.Select(MapToDomain);
                },
                _exceptionMapper,
                "GetByKeys",
                "setting",
                _logger);
        }

        public async Task<Setting> AddAsync(Setting setting, CancellationToken cancellationToken = default)
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    // Check if key already exists
                    var existing = await _context.Table<SettingEntity>()
                        .Where(s => s.Key == setting.Key)
                        .FirstOrDefaultAsync();

                    if (existing != null)
                    {
                        throw new InvalidOperationException($"Setting with key '{setting.Key}' already exists");
                    }

                    var entity = MapToEntity(setting);
                    entity.Timestamp = DateTime.UtcNow;

                    await _context.InsertAsync(entity);

                    // Update domain object with generated ID
                    SetPrivateProperty(setting, "Id", entity.Id);

                    _logger.LogInformation("Created setting with key {Key}", setting.Key);
                    return setting;
                },
                _exceptionMapper,
                "Add",
                "setting",
                _logger);
        }

        public async Task UpdateAsync(Setting setting, CancellationToken cancellationToken = default)
        {
            await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    var entity = MapToEntity(setting);
                    entity.Timestamp = DateTime.UtcNow;

                    await _context.UpdateAsync(entity);

                    _logger.LogInformation("Updated setting with key {Key}", setting.Key);
                },
                _exceptionMapper,
                "Update",
                "setting",
                _logger);
        }

        public async Task DeleteAsync(Setting setting, CancellationToken cancellationToken = default)
        {
            await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    var entity = MapToEntity(setting);
                    await _context.DeleteAsync(entity);

                    _logger.LogInformation("Deleted setting with key {Key}", setting.Key);
                },
                _exceptionMapper,
                "Delete",
                "setting",
                _logger);
        }

        public async Task<Setting> UpsertAsync(string key, string value, string? description = null, CancellationToken cancellationToken = default)
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    var existingEntity = await _context.Table<SettingEntity>()
                        .Where(s => s.Key == key)
                        .FirstOrDefaultAsync();

                    Setting setting;
                    SettingEntity entity;

                    if (existingEntity != null)
                    {
                        // Update existing
                        setting = MapToDomain(existingEntity);
                        setting.UpdateValue(value);
                        entity = MapToEntity(setting);
                        entity.Timestamp = DateTime.UtcNow;
                        await _context.UpdateAsync(entity);
                        _logger.LogInformation("Updated setting with key {Key} via upsert", key);
                    }
                    else
                    {
                        // Create new
                        setting = new Setting(key, value, description ?? string.Empty);
                        entity = MapToEntity(setting);
                        entity.Timestamp = DateTime.UtcNow;
                        await _context.InsertAsync(entity);
                        SetPrivateProperty(setting, "Id", entity.Id);
                        _logger.LogInformation("Created setting with key {Key} via upsert", key);
                    }

                    return setting;
                },
                _exceptionMapper,
                "Upsert",
                "setting",
                _logger);
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