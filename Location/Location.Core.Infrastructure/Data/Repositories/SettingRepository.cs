using Location.Core.Application.Common.Interfaces.Persistence;
using Location.Core.Domain.Entities;
using Location.Core.Infrastructure.Data.Entities;
using Location.Core.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace Location.Core.Infrastructure.Data.Repositories
{
    public class SettingRepository : ISettingRepository
    {
        private readonly IDatabaseContext _context;
        private readonly ILogger<SettingRepository> _logger;
        private readonly IInfrastructureExceptionMappingService _exceptionMapper;

        // Compiled mapping delegates for performance
        private static readonly Func<SettingEntity, Setting> _compiledEntityToDomain;
        private static readonly Func<Setting, SettingEntity> _compiledDomainToEntity;

        // Cached property setters for reflection performance
        private static readonly Dictionary<string, Action<object, object>> _propertySetters;

        // Query cache for frequently used queries
        private static readonly Dictionary<string, string> _queryCache;

        // In-memory cache for frequently accessed settings
        private readonly ConcurrentDictionary<string, CachedSetting> _settingsCache;
        private readonly Timer _cacheCleanupTimer;
        private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(15);
        private static readonly TimeSpan CleanupInterval = TimeSpan.FromMinutes(5);

        static SettingRepository()
        {
            _compiledEntityToDomain = CompileEntityToDomainMapper();
            _compiledDomainToEntity = CompileDomainToEntityMapper();
            _propertySetters = CreatePropertySetters();
            _queryCache = InitializeQueryCache();
        }

        public SettingRepository(IDatabaseContext context, ILogger<SettingRepository> logger, IInfrastructureExceptionMappingService exceptionMapper)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _exceptionMapper = exceptionMapper ?? throw new ArgumentNullException(nameof(exceptionMapper));

            _settingsCache = new ConcurrentDictionary<string, CachedSetting>();
            _cacheCleanupTimer = new Timer(CleanupExpiredCache, null, CleanupInterval, CleanupInterval);
        }

        #region Core Operations (Optimized)

        public async Task<Setting?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    var entity = await _context.GetAsync<SettingEntity>(id);
                    return entity != null ? _compiledEntityToDomain(entity) : null;
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
                    // Check cache first for frequently accessed settings
                    if (_settingsCache.TryGetValue(key, out var cachedSetting) && !cachedSetting.IsExpired)
                    {
                        _logger.LogDebug("Retrieved setting {Key} from cache", key);
                        return cachedSetting.Setting;
                    }

                    // Query database
                    var entities = await _context.QueryAsync<SettingEntity>(_queryCache["GetSettingByKey"], key);
                    var entity = entities.FirstOrDefault();

                    if (entity == null)
                    {
                        // Cache null result to avoid repeated database hits
                        _settingsCache.TryAdd(key, new CachedSetting(null, DateTime.UtcNow.Add(CacheExpiration)));
                        return null;
                    }

                    var setting = _compiledEntityToDomain(entity);

                    // Cache the result
                    _settingsCache.AddOrUpdate(key,
                        new CachedSetting(setting, DateTime.UtcNow.Add(CacheExpiration)),
                        (k, old) => new CachedSetting(setting, DateTime.UtcNow.Add(CacheExpiration)));

                    return setting;
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
                    var entities = await _context.QueryAsync<SettingEntity>(_queryCache["GetAllSettings"]);
                    return entities.Select(_compiledEntityToDomain);
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
                    if (!keysList.Any()) return Enumerable.Empty<Setting>();

                    var settings = new List<Setting>();
                    var uncachedKeys = new List<string>();

                    // Check cache for each key
                    foreach (var key in keysList)
                    {
                        if (_settingsCache.TryGetValue(key, out var cachedSetting) && !cachedSetting.IsExpired)
                        {
                            if (cachedSetting.Setting != null)
                            {
                                settings.Add(cachedSetting.Setting);
                            }
                        }
                        else
                        {
                            uncachedKeys.Add(key);
                        }
                    }

                    // Query database for uncached keys
                    if (uncachedKeys.Any())
                    {
                        var placeholders = string.Join(",", uncachedKeys.Select(_ => "?"));
                        var sql = $"SELECT * FROM SettingEntity WHERE Key IN ({placeholders}) ORDER BY Key";
                        var parameters = uncachedKeys.Cast<object>().ToArray();

                        var entities = await _context.QueryAsync<SettingEntity>(sql, parameters);
                        var uncachedSettings = entities.Select(_compiledEntityToDomain).ToList();

                        // Cache the results
                        var foundKeys = uncachedSettings.Select(s => s.Key).ToHashSet();
                        foreach (var setting in uncachedSettings)
                        {
                            _settingsCache.AddOrUpdate(setting.Key,
                                new CachedSetting(setting, DateTime.UtcNow.Add(CacheExpiration)),
                                (k, old) => new CachedSetting(setting, DateTime.UtcNow.Add(CacheExpiration)));
                        }

                        // Cache null results for keys that weren't found
                        foreach (var key in uncachedKeys.Where(k => !foundKeys.Contains(k)))
                        {
                            _settingsCache.TryAdd(key, new CachedSetting(null, DateTime.UtcNow.Add(CacheExpiration)));
                        }

                        settings.AddRange(uncachedSettings);
                    }

                    return settings;
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
                    // Check if key already exists using optimized query
                    var existsResult = await _context.ExecuteScalarAsync<long>(_queryCache["CheckSettingExists"], setting.Key);
                    if (existsResult > 0)
                    {
                        throw new InvalidOperationException($"Setting with key '{setting.Key}' already exists");
                    }

                    var entity = _compiledDomainToEntity(setting);
                    entity.Timestamp = DateTime.UtcNow;

                    await _context.InsertAsync(entity);

                    // Update domain object with generated ID
                    SetOptimizedProperty(setting, "Id", entity.Id);
                    SetOptimizedProperty(setting, "Timestamp", entity.Timestamp);

                    // Update cache
                    _settingsCache.AddOrUpdate(setting.Key,
                        new CachedSetting(setting, DateTime.UtcNow.Add(CacheExpiration)),
                        (k, old) => new CachedSetting(setting, DateTime.UtcNow.Add(CacheExpiration)));

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
                    var entity = _compiledDomainToEntity(setting);
                    entity.Timestamp = DateTime.UtcNow;

                    var rowsAffected = await _context.UpdateAsync(entity);
                    if (rowsAffected == 0)
                    {
                        throw new InvalidOperationException($"Setting with key '{setting.Key}' not found for update");
                    }

                    // Update timestamp in domain object
                    SetOptimizedProperty(setting, "Timestamp", entity.Timestamp);

                    // Update cache
                    _settingsCache.AddOrUpdate(setting.Key,
                        new CachedSetting(setting, DateTime.UtcNow.Add(CacheExpiration)),
                        (k, old) => new CachedSetting(setting, DateTime.UtcNow.Add(CacheExpiration)));

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
                    var entity = _compiledDomainToEntity(setting);
                    var rowsAffected = await _context.DeleteAsync(entity);

                    if (rowsAffected == 0)
                    {
                        throw new InvalidOperationException($"Setting with key '{setting.Key}' not found for deletion");
                    }

                    // Remove from cache
                    _settingsCache.TryRemove(setting.Key, out _);

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
                    return await _context.ExecuteInTransactionAsync(async () =>
                    {
                        // Try to get existing setting using optimized query
                        var entities = await _context.QueryAsync<SettingEntity>(_queryCache["GetSettingByKey"], key);
                        var existingEntity = entities.FirstOrDefault();

                        Setting setting;
                        SettingEntity entity;

                        if (existingEntity != null)
                        {
                            // Update existing
                            setting = _compiledEntityToDomain(existingEntity);
                            setting.UpdateValue(value);

                            entity = _compiledDomainToEntity(setting);
                            entity.Timestamp = DateTime.UtcNow;

                            await _context.UpdateAsync(entity);
                            SetOptimizedProperty(setting, "Timestamp", entity.Timestamp);

                            _logger.LogInformation("Updated setting with key {Key} via upsert", key);
                        }
                        else
                        {
                            // Create new
                            setting = new Setting(key, value, description ?? string.Empty);
                            entity = _compiledDomainToEntity(setting);
                            entity.Timestamp = DateTime.UtcNow;

                            await _context.InsertAsync(entity);
                            SetOptimizedProperty(setting, "Id", entity.Id);
                            SetOptimizedProperty(setting, "Timestamp", entity.Timestamp);

                            _logger.LogInformation("Created setting with key {Key} via upsert", key);
                        }

                        // Update cache
                        _settingsCache.AddOrUpdate(key,
                            new CachedSetting(setting, DateTime.UtcNow.Add(CacheExpiration)),
                            (k, old) => new CachedSetting(setting, DateTime.UtcNow.Add(CacheExpiration)));

                        return setting;
                    });
                },
                _exceptionMapper,
                "Upsert",
                "setting",
                _logger);
        }

        #endregion

        #region Advanced Operations

        public async Task<Dictionary<string, string>> GetAllAsDictionaryAsync(CancellationToken cancellationToken = default)
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    var entities = await _context.QueryAsync<SettingKeyValueDto>(_queryCache["GetAllSettingsKeyValue"]);
                    return entities.ToDictionary(e => e.Key, e => e.Value);
                },
                _exceptionMapper,
                "GetAllAsDictionary",
                "setting",
                _logger);
        }

        public async Task<IEnumerable<Setting>> GetByPrefixAsync(string keyPrefix, CancellationToken cancellationToken = default)
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    var searchPattern = $"{keyPrefix}%";
                    var entities = await _context.QueryAsync<SettingEntity>(_queryCache["GetSettingsByPrefix"], searchPattern);
                    return entities.Select(_compiledEntityToDomain);
                },
                _exceptionMapper,
                "GetByPrefix",
                "setting",
                _logger);
        }

        public async Task<IEnumerable<Setting>> GetRecentlyModifiedAsync(int count = 10, CancellationToken cancellationToken = default)
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    var entities = await _context.QueryAsync<SettingEntity>(_queryCache["GetRecentlyModifiedSettings"], count);
                    return entities.Select(_compiledEntityToDomain);
                },
                _exceptionMapper,
                "GetRecentlyModified",
                "setting",
                _logger);
        }

        public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    // Check cache first
                    if (_settingsCache.TryGetValue(key, out var cachedSetting) && !cachedSetting.IsExpired)
                    {
                        return cachedSetting.Setting != null;
                    }

                    // Check database
                    var result = await _context.ExecuteScalarAsync<long>(_queryCache["CheckSettingExists"], key);
                    return result > 0;
                },
                _exceptionMapper,
                "Exists",
                "setting",
                _logger);
        }

        #endregion

        #region Bulk Operations

        public async Task<IEnumerable<Setting>> CreateBulkAsync(IEnumerable<Setting> settings, CancellationToken cancellationToken = default)
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    var settingsList = settings.ToList();
                    if (!settingsList.Any()) return settingsList;

                    return await _context.ExecuteInTransactionAsync(async () =>
                    {
                        // Check for duplicate keys in batch
                        var keys = settingsList.Select(s => s.Key).ToList();
                        var duplicateKeys = keys.GroupBy(k => k).Where(g => g.Count() > 1).Select(g => g.Key);
                        if (duplicateKeys.Any())
                        {
                            throw new InvalidOperationException($"Duplicate keys in batch: {string.Join(", ", duplicateKeys)}");
                        }

                        // Check for existing keys in database
                        // Check for existing keys in database
                        var placeholders = string.Join(",", keys.Select(_ => "?"));
                        var sql = $"SELECT Key FROM SettingEntity WHERE Key IN ({placeholders})";
                        var parameters = keys.Cast<object>().ToArray();

                        var existingKeyEntities = await _context.QueryAsync<SettingKeyOnlyDto>(sql, parameters);
                        var existingKeys = existingKeyEntities.Select(e => e.Key).ToList();
                        if (existingKeys.Any())
                        {
                            throw new InvalidOperationException($"Settings with these keys already exist: {string.Join(", ", existingKeys)}");
                        }

                        // Bulk insert
                        var entities = settingsList.Select(s =>
                        {
                            var entity = _compiledDomainToEntity(s);
                            entity.Timestamp = DateTime.UtcNow;
                            return entity;
                        }).ToList();

                        await _context.BulkInsertAsync(entities, 100);

                        // Update domain objects with generated IDs and timestamps
                        for (int i = 0; i < settingsList.Count; i++)
                        {
                            SetOptimizedProperty(settingsList[i], "Id", entities[i].Id);
                            SetOptimizedProperty(settingsList[i], "Timestamp", entities[i].Timestamp);

                            // Update cache
                            _settingsCache.AddOrUpdate(settingsList[i].Key,
                                new CachedSetting(settingsList[i], DateTime.UtcNow.Add(CacheExpiration)),
                                (k, old) => new CachedSetting(settingsList[i], DateTime.UtcNow.Add(CacheExpiration)));
                        }

                        _logger.LogInformation("Bulk created {Count} settings", settingsList.Count);
                        return settingsList;
                    });
                },
                _exceptionMapper,
                "CreateBulk",
                "setting",
                _logger);
        }

        public async Task<int> UpdateBulkAsync(IEnumerable<Setting> settings, CancellationToken cancellationToken = default)
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    var settingsList = settings.ToList();
                    if (!settingsList.Any()) return 0;

                    return await _context.ExecuteInTransactionAsync(async () =>
                    {
                        var entities = settingsList.Select(s =>
                        {
                            var entity = _compiledDomainToEntity(s);
                            entity.Timestamp = DateTime.UtcNow;
                            return entity;
                        }).ToList();

                        var result = await _context.BulkUpdateAsync(entities, 100);

                        // Update timestamps and cache
                        for (int i = 0; i < settingsList.Count; i++)
                        {
                            SetOptimizedProperty(settingsList[i], "Timestamp", entities[i].Timestamp);

                            // Update cache
                            _settingsCache.AddOrUpdate(settingsList[i].Key,
                                new CachedSetting(settingsList[i], DateTime.UtcNow.Add(CacheExpiration)),
                                (k, old) => new CachedSetting(settingsList[i], DateTime.UtcNow.Add(CacheExpiration)));
                        }

                        _logger.LogInformation("Bulk updated {Count} settings", result);
                        return result;
                    });
                },
                _exceptionMapper,
                "UpdateBulk",
                "setting",
                _logger);
        }

        public async Task<int> DeleteBulkAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default)
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    var keysList = keys.ToList();
                    if (!keysList.Any()) return 0;

                    return await _context.ExecuteInTransactionAsync(async () =>
                    {
                        // Delete in batches
                        const int batchSize = 100;
                        int totalDeleted = 0;

                        var batches = keysList.Chunkier(batchSize);
                        foreach (var batch in batches)
                        {
                            var placeholders = string.Join(",", batch.Select(_ => "?"));
                            var sql = $"DELETE FROM SettingEntity WHERE Key IN ({placeholders})";
                            var parameters = batch.Cast<object>().ToArray();

                            var deleted = await _context.ExecuteAsync(sql, parameters);
                            totalDeleted += deleted;

                            // Remove from cache
                            foreach (var key in batch)
                            {
                                _settingsCache.TryRemove(key, out _);
                            }
                        }

                        _logger.LogInformation("Bulk deleted {Count} settings", totalDeleted);
                        return totalDeleted;
                    });
                },
                _exceptionMapper,
                "DeleteBulk",
                "setting",
                _logger);
        }

        public async Task<Dictionary<string, string>> UpsertBulkAsync(Dictionary<string, string> keyValuePairs, CancellationToken cancellationToken = default)
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    if (!keyValuePairs.Any()) return new Dictionary<string, string>();

                    return await _context.ExecuteInTransactionAsync(async () =>
                    {
                        var result = new Dictionary<string, string>();

                        // Get existing settings
                        var keys = keyValuePairs.Keys.ToList();
                        var placeholders = string.Join(",", keys.Select(_ => "?"));
                        var sql = $"SELECT * FROM SettingEntity WHERE Key IN ({placeholders})";
                        var parameters = keys.Cast<object>().ToArray();

                        var existingEntities = await _context.QueryAsync<SettingEntity>(sql, parameters);
                        var existingByKey = existingEntities.ToDictionary(e => e.Key);

                        var toUpdate = new List<SettingEntity>();
                        var toInsert = new List<SettingEntity>();

                        foreach (var kvp in keyValuePairs)
                        {
                            if (existingByKey.TryGetValue(kvp.Key, out var existing))
                            {
                                // Update existing
                                existing.Value = kvp.Value;
                                existing.Timestamp = DateTime.UtcNow;
                                toUpdate.Add(existing);
                            }
                            else
                            {
                                // Insert new
                                var newEntity = new SettingEntity
                                {
                                    Key = kvp.Key,
                                    Value = kvp.Value,
                                    Description = string.Empty,
                                    Timestamp = DateTime.UtcNow
                                };
                                toInsert.Add(newEntity);
                            }
                        }

                        // Perform bulk operations
                        if (toUpdate.Any())
                        {
                            await _context.BulkUpdateAsync(toUpdate, 100);
                        }

                        if (toInsert.Any())
                        {
                            await _context.BulkInsertAsync(toInsert, 100);
                        }

                        // Update cache and build result
                        foreach (var kvp in keyValuePairs)
                        {
                            result[kvp.Key] = kvp.Value;

                            // Create/update cached setting
                            var setting = new Setting(kvp.Key, kvp.Value, string.Empty);
                            _settingsCache.AddOrUpdate(kvp.Key,
                                new CachedSetting(setting, DateTime.UtcNow.Add(CacheExpiration)),
                                (k, old) => new CachedSetting(setting, DateTime.UtcNow.Add(CacheExpiration)));
                        }

                        _logger.LogInformation("Bulk upserted {Count} settings", keyValuePairs.Count);
                        return result;
                    });
                },
                _exceptionMapper,
                "UpsertBulk",
                "setting",
                _logger);
        }

        #endregion

        #region Cache Management

        public void ClearCache()
        {
            _settingsCache.Clear();
            _logger.LogInformation("Settings cache cleared");
        }

        public void ClearCache(string key)
        {
            _settingsCache.TryRemove(key, out _);
            _logger.LogDebug("Removed setting {Key} from cache", key);
        }

        private void CleanupExpiredCache(object? state)
        {
            try
            {
                var expiredKeys = _settingsCache
                    .Where(kvp => kvp.Value.IsExpired)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var key in expiredKeys)
                {
                    _settingsCache.TryRemove(key, out _);
                }

                if (expiredKeys.Any())
                {
                    _logger.LogDebug("Cleaned up {Count} expired cache entries", expiredKeys.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cache cleanup");
            }
        }

        #endregion

        #region Compiled Mapping Methods

        private static Func<SettingEntity, Setting> CompileEntityToDomainMapper()
        {
            var entityParam = Expression.Parameter(typeof(SettingEntity), "entity");

            // Create Setting using constructor
            var settingConstructor = typeof(Setting).GetConstructor(
                new[] { typeof(string), typeof(string), typeof(string) });

            if (settingConstructor == null)
            {
                throw new InvalidOperationException("Cannot find Setting constructor");
            }

            var settingNew = Expression.New(settingConstructor,
                Expression.Property(entityParam, nameof(SettingEntity.Key)),
                Expression.Property(entityParam, nameof(SettingEntity.Value)),
                Expression.Property(entityParam, nameof(SettingEntity.Description)));

            return Expression.Lambda<Func<SettingEntity, Setting>>(settingNew, entityParam).Compile();
        }

        private static Func<Setting, SettingEntity> CompileDomainToEntityMapper()
        {
            var settingParam = Expression.Parameter(typeof(Setting), "setting");

            var entityNew = Expression.MemberInit(
                Expression.New(typeof(SettingEntity)),
                Expression.Bind(typeof(SettingEntity).GetProperty(nameof(SettingEntity.Id))!,
                    Expression.Property(settingParam, "Id")),
                Expression.Bind(typeof(SettingEntity).GetProperty(nameof(SettingEntity.Key))!,
                    Expression.Property(settingParam, "Key")),
                Expression.Bind(typeof(SettingEntity).GetProperty(nameof(SettingEntity.Value))!,
                    Expression.Property(settingParam, "Value")),
                Expression.Bind(typeof(SettingEntity).GetProperty(nameof(SettingEntity.Description))!,
                    Expression.Property(settingParam, "Description")),
                Expression.Bind(typeof(SettingEntity).GetProperty(nameof(SettingEntity.Timestamp))!,
                    Expression.Property(settingParam, "Timestamp"))
            );

            return Expression.Lambda<Func<Setting, SettingEntity>>(entityNew, settingParam).Compile();
        }

        private static Dictionary<string, Action<object, object>> CreatePropertySetters()
        {
            var setters = new Dictionary<string, Action<object, object>>();
            var settingProps = typeof(Setting).GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var prop in settingProps.Where(p => p.CanWrite))
            {
                var objParam = Expression.Parameter(typeof(object), "obj");
                var valueParam = Expression.Parameter(typeof(object), "value");

                var castObj = Expression.Convert(objParam, typeof(Setting));
                var castValue = Expression.Convert(valueParam, prop.PropertyType);
                var setProp = Expression.Call(castObj, prop.GetSetMethod(true)!, castValue);

                var lambda = Expression.Lambda<Action<object, object>>(setProp, objParam, valueParam);
                setters[prop.Name] = lambda.Compile();
            }

            return setters;
        }

        private static Dictionary<string, string> InitializeQueryCache()
        {
            return new Dictionary<string, string>
            {
                ["GetSettingByKey"] = "SELECT * FROM SettingEntity WHERE Key = ? LIMIT 1",
                ["GetAllSettings"] = "SELECT * FROM SettingEntity ORDER BY Key",
                ["CheckSettingExists"] = "SELECT EXISTS(SELECT 1 FROM SettingEntity WHERE Key = ?)",
                ["GetAllSettingsKeyValue"] = "SELECT Key, Value FROM SettingEntity ORDER BY Key",
                ["GetSettingsByPrefix"] = "SELECT * FROM SettingEntity WHERE Key LIKE ? ORDER BY Key",
                ["GetRecentlyModifiedSettings"] = "SELECT * FROM SettingEntity ORDER BY Timestamp DESC LIMIT ?"
            };
        }

        private static void SetOptimizedProperty(object obj, string propertyName, object value)
        {
            if (_propertySetters.TryGetValue(propertyName, out var setter))
            {
                setter(obj, value);
            }
            else
            {
                // Fallback to reflection for properties not in cache
                var property = obj.GetType().GetProperty(propertyName);
                if (property != null && property.CanWrite)
                {
                    property.SetValue(obj, value);
                }
                else
                {
                    // Try private field access as last resort
                    var field = obj.GetType().GetField($"_{propertyName.ToLowerInvariant()}",
                        BindingFlags.NonPublic | BindingFlags.Instance);
                    field?.SetValue(obj, value);
                }
            }
        }

        #endregion

        #region Legacy Mapping Methods (Kept for Backward Compatibility)


        #endregion

        #region DTOs and Cache Classes
        public class SettingKeyOnlyDto
        {
            public string Key { get; set; } = string.Empty;
        }
        public class SettingKeyValueDto
        {
            public string Key { get; set; } = string.Empty;
            public string Value { get; set; } = string.Empty;
        }

        private class CachedSetting
        {
            public Setting? Setting { get; }
            public DateTime ExpiresAt { get; }

            public CachedSetting(Setting? setting, DateTime expiresAt)
            {
                Setting = setting;
                ExpiresAt = expiresAt;
            }

            public bool IsExpired => DateTime.UtcNow > ExpiresAt;
        }

        #endregion

        #region Disposal

        private bool _disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                try
                {
                    _cacheCleanupTimer?.Dispose();
                    _settingsCache?.Clear();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during SettingRepository disposal");
                }
            }

            _disposed = true;
        }

        #endregion
    }

    // Extension methods for chunking (if not already defined elsewhere)
    public static class SettingEnumerableExtensions
    {
        public static IEnumerable<IEnumerable<T>> Chunkier<T>(this IEnumerable<T> source, int size)
        {
            var enumerator = source.GetEnumerator();
            while (enumerator.MoveNext())
            {
                yield return GetChunk(enumerator, size);
            }
        }

        private static IEnumerable<T> GetChunk<T>(IEnumerator<T> enumerator, int size)
        {
            int count = 0;
            do
            {
                yield return enumerator.Current;
                count++;
            } while (count < size && enumerator.MoveNext());
        }
    }
}