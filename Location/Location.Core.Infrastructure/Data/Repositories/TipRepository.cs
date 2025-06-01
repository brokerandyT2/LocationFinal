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
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Location.Core.Infrastructure.Data.Repositories
{
    public class TipRepository : ITipRepository
    {
        private readonly IDatabaseContext _context;
        private readonly ILogger<TipRepository> _logger;
        private readonly IInfrastructureExceptionMappingService _exceptionMapper;

        // Compiled mapping delegates for performance
        private static readonly Func<TipEntity, Tip> _compiledEntityToDomain;
        private static readonly Func<Tip, TipEntity> _compiledDomainToEntity;

        // Cached property setters for reflection performance
        private static readonly Dictionary<string, Action<object, object>> _propertySetters;

        // Query cache for frequently used queries
        private static readonly Dictionary<string, string> _queryCache;

        static TipRepository()
        {
            _compiledEntityToDomain = CompileEntityToDomainMapper();
            _compiledDomainToEntity = CompileDomainToEntityMapper();
            _propertySetters = CreatePropertySetters();
            _queryCache = InitializeQueryCache();
        }

        public TipRepository(IDatabaseContext context, ILogger<TipRepository> logger, IInfrastructureExceptionMappingService exceptionMapper)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _exceptionMapper = exceptionMapper ?? throw new ArgumentNullException(nameof(exceptionMapper));
        }

        #region Core Operations (Optimized)

        public async Task<Tip?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    var entity = await _context.GetAsync<TipEntity>(id);
                    return entity != null ? _compiledEntityToDomain(entity) : null;
                },
                _exceptionMapper,
                "GetById",
                "tip",
                _logger);
        }

        public async Task<IEnumerable<Tip>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    var entities = await _context.QueryAsync<TipEntity>(_queryCache["GetAllTips"]);
                    return entities.Select(_compiledEntityToDomain);
                },
                _exceptionMapper,
                "GetAll",
                "tip",
                _logger);
        }

        public async Task<IEnumerable<Tip>> GetByTipTypeIdAsync(int tipTypeId, CancellationToken cancellationToken = default)
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    var entities = await _context.QueryAsync<TipEntity>(_queryCache["GetTipsByType"], tipTypeId);
                    return entities.Select(_compiledEntityToDomain);
                },
                _exceptionMapper,
                "GetByTipTypeId",
                "tip",
                _logger);
        }

        public async Task<Tip> AddAsync(Tip tip, CancellationToken cancellationToken = default)
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    var entity = _compiledDomainToEntity(tip);
                    await _context.InsertAsync(entity);

                    // Update domain object with generated ID
                    SetOptimizedProperty(tip, "Id", entity.Id);

                    _logger.LogInformation("Created tip with ID {TipId}", entity.Id);
                    return tip;
                },
                _exceptionMapper,
                "Add",
                "tip",
                _logger);
        }

        public async Task UpdateAsync(Tip tip, CancellationToken cancellationToken = default)
        {
            await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    var entity = _compiledDomainToEntity(tip);
                    await _context.UpdateAsync(entity);
                    _logger.LogInformation("Updated tip with ID {TipId}", tip.Id);
                },
                _exceptionMapper,
                "Update",
                "tip",
                _logger);
        }

        public async Task DeleteAsync(Tip tip, CancellationToken cancellationToken = default)
        {
            await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    var entity = _compiledDomainToEntity(tip);
                    await _context.DeleteAsync(entity);
                    _logger.LogInformation("Deleted tip with ID {TipId}", tip.Id);
                },
                _exceptionMapper,
                "Delete",
                "tip",
                _logger);
        }

        public async Task<Tip?> GetByTitleAsync(string title, CancellationToken cancellationToken = default)
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    var entities = await _context.QueryAsync<TipEntity>(_queryCache["GetTipByTitle"], title);
                    var entity = entities.FirstOrDefault();
                    return entity != null ? _compiledEntityToDomain(entity) : null;
                },
                _exceptionMapper,
                "GetByTitle",
                "tip",
                _logger);
        }

        public async Task<Tip?> GetRandomByTypeAsync(int tipTypeId, CancellationToken cancellationToken = default)
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    // Get count first for random selection
                    var count = await _context.ExecuteScalarAsync<int>(_queryCache["CountTipsByType"], tipTypeId);

                    if (count == 0)
                    {
                        return null;
                    }

                    // Generate random offset
                    var random = new Random();
                    var offset = random.Next(count);

                    // Get random tip using offset
                    var entities = await _context.QueryAsync<TipEntity>(_queryCache["GetRandomTipByType"], tipTypeId, offset);
                    var entity = entities.FirstOrDefault();

                    return entity != null ? _compiledEntityToDomain(entity) : null;
                },
                _exceptionMapper,
                "GetRandomByType",
                "tip",
                _logger);
        }

        #endregion

        #region Advanced Query Operations

        public async Task<IEnumerable<Tip>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default)
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    if (string.IsNullOrWhiteSpace(searchTerm))
                    {
                        return await GetAllAsync(cancellationToken);
                    }

                    var searchPattern = $"%{searchTerm.Trim()}%";
                    var entities = await _context.QueryAsync<TipEntity>(_queryCache["SearchTips"], searchPattern, searchPattern);
                    return entities.Select(_compiledEntityToDomain);
                },
                _exceptionMapper,
                "Search",
                "tip",
                _logger);
        }

        public async Task<IEnumerable<Tip>> GetByTipTypeIdWithPaginationAsync(
            int tipTypeId,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    var offset = (pageNumber - 1) * pageSize;
                    var entities = await _context.QueryAsync<TipEntity>(
                        _queryCache["GetTipsByTypeWithPagination"],
                        tipTypeId,
                        pageSize,
                        offset);

                    return entities.Select(_compiledEntityToDomain);
                },
                _exceptionMapper,
                "GetByTipTypeIdWithPagination",
                "tip",
                _logger);
        }

        public async Task<int> GetCountByTipTypeIdAsync(int tipTypeId, CancellationToken cancellationToken = default)
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    return await _context.ExecuteScalarAsync<int>(_queryCache["CountTipsByType"], tipTypeId);
                },
                _exceptionMapper,
                "GetCountByTipTypeId",
                "tip",
                _logger);
        }

        public async Task<IEnumerable<Tip>> GetRecentAsync(int count = 10, CancellationToken cancellationToken = default)
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    var entities = await _context.QueryAsync<TipEntity>(_queryCache["GetRecentTips"], count);
                    return entities.Select(_compiledEntityToDomain);
                },
                _exceptionMapper,
                "GetRecent",
                "tip",
                _logger);
        }

        #endregion

        #region Bulk Operations

        public async Task<IEnumerable<Tip>> CreateBulkAsync(IEnumerable<Tip> tips, CancellationToken cancellationToken = default)
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    var tipList = tips.ToList();
                    if (!tipList.Any()) return tipList;

                    return await _context.ExecuteInTransactionAsync(async () =>
                    {
                        var entities = tipList.Select(_compiledDomainToEntity).ToList();
                        await _context.BulkInsertAsync(entities, 100);

                        // Update domain objects with generated IDs
                        for (int i = 0; i < tipList.Count; i++)
                        {
                            SetOptimizedProperty(tipList[i], "Id", entities[i].Id);
                        }

                        _logger.LogInformation("Bulk created {Count} tips", tipList.Count);
                        return tipList;
                    });
                },
                _exceptionMapper,
                "CreateBulk",
                "tip",
                _logger);
        }

        public async Task<int> UpdateBulkAsync(IEnumerable<Tip> tips, CancellationToken cancellationToken = default)
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    var tipList = tips.ToList();
                    if (!tipList.Any()) return 0;

                    return await _context.ExecuteInTransactionAsync(async () =>
                    {
                        var entities = tipList.Select(_compiledDomainToEntity).ToList();
                        var result = await _context.BulkUpdateAsync(entities, 100);

                        _logger.LogInformation("Bulk updated {Count} tips", result);
                        return result;
                    });
                },
                _exceptionMapper,
                "UpdateBulk",
                "tip",
                _logger);
        }

        public async Task<int> DeleteBulkAsync(IEnumerable<int> tipIds, CancellationToken cancellationToken = default)
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    var idList = tipIds.ToList();
                    if (!idList.Any()) return 0;

                    return await _context.ExecuteInTransactionAsync(async () =>
                    {
                        // Delete in batches for large datasets
                        const int batchSize = 100;
                        int totalDeleted = 0;

                        var batches = idList.Chunky(batchSize);
                        foreach (var batch in batches)
                        {
                            var placeholders = string.Join(",", batch.Select(_ => "?"));
                            var sql = $"DELETE FROM TipEntity WHERE Id IN ({placeholders})";
                            var parameters = batch.Cast<object>().ToArray();

                            var deleted = await _context.ExecuteAsync(sql, parameters);
                            totalDeleted += deleted;
                        }

                        _logger.LogInformation("Bulk deleted {Count} tips", totalDeleted);
                        return totalDeleted;
                    });
                },
                _exceptionMapper,
                "DeleteBulk",
                "tip",
                _logger);
        }

        public async Task<int> DeleteByTipTypeIdAsync(int tipTypeId, CancellationToken cancellationToken = default)
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    var deletedCount = await _context.ExecuteAsync(_queryCache["DeleteTipsByType"], tipTypeId);
                    _logger.LogInformation("Deleted {Count} tips for tip type {TipTypeId}", deletedCount, tipTypeId);
                    return deletedCount;
                },
                _exceptionMapper,
                "DeleteByTipTypeId",
                "tip",
                _logger);
        }

        #endregion

        #region Projection and Analytics

        public async Task<IEnumerable<T>> GetProjectedAsync<T>(
            string selectColumns,
            string? whereClause = null,
            Dictionary<string, object>? parameters = null,
            string? orderBy = null,
            int? limit = null,
            CancellationToken cancellationToken = default) where T : class, new()
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    var sql = BuildProjectionQuery(selectColumns, whereClause, orderBy, limit);
                    var paramArray = parameters?.Values.ToArray() ?? Array.Empty<object>();
                    return await _context.QueryAsync<T>(sql, paramArray);
                },
                _exceptionMapper,
                "GetProjected",
                "tip",
                _logger);
        }

        public async Task<Dictionary<int, int>> GetTipCountsByTypeAsync(CancellationToken cancellationToken = default)
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    var results = await _context.QueryAsync<TipTypeCountDto>(_queryCache["GetTipCountsByType"]);
                    return results.ToDictionary(r => r.TipTypeId, r => r.Count);
                },
                _exceptionMapper,
                "GetTipCountsByType",
                "tip",
                _logger);
        }

        public async Task<IEnumerable<string>> GetPopularCameraSettingsAsync(int limit = 10, CancellationToken cancellationToken = default)
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    var sql = @"
                       SELECT DISTINCT 
                           (Fstop || '/' || ShutterSpeed || '/' || Iso) as Settings,
                           COUNT(*) as Count
                       FROM TipEntity 
                       WHERE Fstop != '' AND ShutterSpeed != '' AND Iso != ''
                       GROUP BY Fstop, ShutterSpeed, Iso
                       ORDER BY Count DESC, Settings
                       LIMIT ?";

                    var results = await _context.QueryAsync<PopularSettingsDto>(sql, limit);
                    return results.Select(r => r.Settings);
                },
                _exceptionMapper,
                "GetPopularCameraSettings",
                "tip",
                _logger);
        }

        #endregion

        #region Compiled Mapping Methods

        private static Func<TipEntity, Tip> CompileEntityToDomainMapper()
        {
            var entityParam = Expression.Parameter(typeof(TipEntity), "entity");

            // Create Tip using constructor
            var tipConstructor = typeof(Tip).GetConstructor(
                new[] { typeof(int), typeof(string), typeof(string) });

            if (tipConstructor == null)
            {
                throw new InvalidOperationException("Cannot find Tip constructor");
            }

            var tipNew = Expression.New(tipConstructor,
                Expression.Property(entityParam, nameof(TipEntity.TipTypeId)),
                Expression.Property(entityParam, nameof(TipEntity.Title)),
                Expression.Property(entityParam, nameof(TipEntity.Content)));

            // Create initialization expressions for setting private properties
            var tipVar = Expression.Variable(typeof(Tip), "tip");
            var initExpressions = new List<Expression>
           {
               Expression.Assign(tipVar, tipNew),
               tipVar
           };

            var body = Expression.Block(new[] { tipVar }, initExpressions);
            return Expression.Lambda<Func<TipEntity, Tip>>(body, entityParam).Compile();
        }

        private static Func<Tip, TipEntity> CompileDomainToEntityMapper()
        {
            var tipParam = Expression.Parameter(typeof(Tip), "tip");

            var entityNew = Expression.MemberInit(
                Expression.New(typeof(TipEntity)),
                Expression.Bind(typeof(TipEntity).GetProperty(nameof(TipEntity.Id))!,
                    Expression.Property(tipParam, "Id")),
                Expression.Bind(typeof(TipEntity).GetProperty(nameof(TipEntity.TipTypeId))!,
                    Expression.Property(tipParam, "TipTypeId")),
                Expression.Bind(typeof(TipEntity).GetProperty(nameof(TipEntity.Title))!,
                    Expression.Property(tipParam, "Title")),
                Expression.Bind(typeof(TipEntity).GetProperty(nameof(TipEntity.Content))!,
                    Expression.Property(tipParam, "Content")),
                Expression.Bind(typeof(TipEntity).GetProperty(nameof(TipEntity.Fstop))!,
                    Expression.Property(tipParam, "Fstop")),
                Expression.Bind(typeof(TipEntity).GetProperty(nameof(TipEntity.ShutterSpeed))!,
                    Expression.Property(tipParam, "ShutterSpeed")),
                Expression.Bind(typeof(TipEntity).GetProperty(nameof(TipEntity.Iso))!,
                    Expression.Property(tipParam, "Iso")),
                Expression.Bind(typeof(TipEntity).GetProperty(nameof(TipEntity.I8n))!,
                    Expression.Property(tipParam, "I8n"))
            );

            return Expression.Lambda<Func<Tip, TipEntity>>(entityNew, tipParam).Compile();
        }

        private static Dictionary<string, Action<object, object>> CreatePropertySetters()
        {
            var setters = new Dictionary<string, Action<object, object>>();
            var tipProps = typeof(Tip).GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var prop in tipProps.Where(p => p.CanWrite))
            {
                var objParam = Expression.Parameter(typeof(object), "obj");
                var valueParam = Expression.Parameter(typeof(object), "value");

                var castObj = Expression.Convert(objParam, typeof(Tip));
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
                ["GetAllTips"] = "SELECT * FROM TipEntity ORDER BY Id",
                ["GetTipsByType"] = "SELECT * FROM TipEntity WHERE TipTypeId = ? ORDER BY Id",
                ["GetTipByTitle"] = "SELECT * FROM TipEntity WHERE Title = ? LIMIT 1",
                ["CountTipsByType"] = "SELECT COUNT(*) FROM TipEntity WHERE TipTypeId = ?",
                ["GetRandomTipByType"] = "SELECT * FROM TipEntity WHERE TipTypeId = ? LIMIT 1 OFFSET ?",
                ["SearchTips"] = "SELECT * FROM TipEntity WHERE Title LIKE ? OR Content LIKE ? ORDER BY Title",
                ["GetTipsByTypeWithPagination"] = "SELECT * FROM TipEntity WHERE TipTypeId = ? ORDER BY Id LIMIT ? OFFSET ?",
                ["GetRecentTips"] = "SELECT * FROM TipEntity ORDER BY Id DESC LIMIT ?",
                ["DeleteTipsByType"] = "DELETE FROM TipEntity WHERE TipTypeId = ?",
                ["GetTipCountsByType"] = @"
                   SELECT TipTypeId, COUNT(*) as Count 
                   FROM TipEntity 
                   GROUP BY TipTypeId 
                   ORDER BY TipTypeId"
            };
        }

        #endregion

        #region Helper Methods

        private string BuildProjectionQuery(string selectColumns, string? whereClause, string? orderBy, int? limit)
        {
            var sql = new StringBuilder($"SELECT {selectColumns} FROM TipEntity");

            if (!string.IsNullOrEmpty(whereClause))
            {
                sql.Append($" WHERE {whereClause}");
            }

            if (!string.IsNullOrEmpty(orderBy))
            {
                sql.Append($" ORDER BY {orderBy}");
            }

            if (limit.HasValue)
            {
                sql.Append($" LIMIT {limit.Value}");
            }

            return sql.ToString();
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

        private Tip MapToDomain(TipEntity entity)
        {
            var tip = _compiledEntityToDomain(entity);

            // Set additional properties that may not be handled by compiled mapper
            SetOptimizedProperty(tip, "Id", entity.Id);
            SetOptimizedProperty(tip, "_fstop", entity.Fstop);
            SetOptimizedProperty(tip, "_shutterSpeed", entity.ShutterSpeed);
            SetOptimizedProperty(tip, "_iso", entity.Iso);
            SetOptimizedProperty(tip, "I8n", entity.I8n);

            return tip;
        }

        private TipEntity MapToEntity(Tip tip)
        {
            return _compiledDomainToEntity(tip);
        }

        private Tip CreateTipViaReflection(int tipTypeId, string title, string content)
        {
            var type = typeof(Tip);
            var constructor = type.GetConstructor(
                new[] { typeof(int), typeof(string), typeof(string) });

            if (constructor == null)
            {
                throw new InvalidOperationException("Cannot find Tip constructor");
            }

            return (Tip)constructor.Invoke(new object[] { tipTypeId, title, content });
        }

        private void SetPrivateProperty(object obj, string propertyName, object value)
        {
            SetOptimizedProperty(obj, propertyName, value);
        }

        #endregion

        #region DTOs for Projections

        public class TipTypeCountDto
        {
            public int TipTypeId { get; set; }
            public int Count { get; set; }
        }

        public class PopularSettingsDto
        {
            public string Settings { get; set; } = string.Empty;
            public int Count { get; set; }
        }

        #endregion
    }

    // Extension method for chunking (if not already available)
    public static class TipEnumerableExtensions
    {
        public static IEnumerable<IEnumerable<T>> Chunky<T>(this IEnumerable<T> source, int size)
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