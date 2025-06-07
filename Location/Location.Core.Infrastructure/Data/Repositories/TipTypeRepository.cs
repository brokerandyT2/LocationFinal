using Location.Core.Application.Common.Interfaces.Persistence;
using Location.Core.Domain.Entities;
using Location.Core.Infrastructure.Data.Entities;
using Location.Core.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using System.Reflection;

namespace Location.Core.Infrastructure.Data.Repositories
{
    public class TipTypeRepository : ITipTypeRepository
    {
        private readonly IDatabaseContext _context;
        private readonly ILogger<TipTypeRepository> _logger;
        private readonly IInfrastructureExceptionMappingService _exceptionMapper;

        // Compiled mapping delegates for performance
        private static readonly Func<TipTypeEntity, TipType> _compiledTipTypeEntityToDomain;
        private static readonly Func<TipType, TipTypeEntity> _compiledTipTypeDomainToEntity;
        private static readonly Func<TipEntity, Tip> _compiledTipEntityToDomain;
        private static readonly Func<Tip, TipEntity> _compiledTipDomainToEntity;

        // Cached property setters for reflection performance
        private static readonly Dictionary<string, Action<object, object>> _propertySetters;

        // Query cache for frequently used queries
        private static readonly Dictionary<string, string> _queryCache;

        static TipTypeRepository()
        {
            _compiledTipTypeEntityToDomain = CompileTipTypeEntityToDomainMapper();
            _compiledTipTypeDomainToEntity = CompileTipTypeDomainToEntityMapper();
            _compiledTipEntityToDomain = CompileTipEntityToDomainMapper();
            _compiledTipDomainToEntity = CompileTipDomainToEntityMapper();
            _propertySetters = CreatePropertySetters();
            _queryCache = InitializeQueryCache();
        }

        public TipTypeRepository(IDatabaseContext context, ILogger<TipTypeRepository> logger, IInfrastructureExceptionMappingService exceptionMapper)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _exceptionMapper = exceptionMapper ?? throw new ArgumentNullException(nameof(exceptionMapper));
        }

        #region Core Operations (Optimized)

        public async Task<TipType?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    var entity = await _context.GetAsync<TipTypeEntity>(id);
                    return entity != null ? _compiledTipTypeEntityToDomain(entity) : null;
                },
                _exceptionMapper,
                "GetById",
                "tiptype",
                _logger);
        }

        public async Task<IEnumerable<TipType>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    var entities = await _context.QueryAsync<TipTypeEntity>(_queryCache["GetAllTipTypes"]);
                    return entities.Select(_compiledTipTypeEntityToDomain);
                },
                _exceptionMapper,
                "GetAll",
                "tiptype",
                _logger);
        }

        public async Task<TipType> AddAsync(TipType tipType, CancellationToken cancellationToken = default)
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    return await _context.ExecuteInTransactionAsync(async () =>
                    {
                        // Insert main tip type entity
                        var entity = _compiledTipTypeDomainToEntity(tipType);
                        await _context.InsertAsync(entity);
                        SetOptimizedProperty(tipType, "Id", entity.Id);

                        // Bulk insert associated tips if any
                        if (tipType.Tips.Any())
                        {
                            var tipEntities = tipType.Tips.Select(tip =>
                            {
                                var tipEntity = _compiledTipDomainToEntity(tip);
                                tipEntity.TipTypeId = entity.Id;
                                return tipEntity;
                            }).ToList();

                            await _context.BulkInsertAsync(tipEntities, 50);

                            // Update tip IDs
                            for (int i = 0; i < tipType.Tips.Count; i++)
                            {
                                SetOptimizedProperty(tipType.Tips.ElementAt(i), "Id", tipEntities[i].Id);
                            }
                        }

                        _logger.LogInformation("Created tip type with ID {TipTypeId}", entity.Id);
                        return tipType;
                    });
                },
                _exceptionMapper,
                "Add",
                "tiptype",
                _logger);
        }

        public async Task UpdateAsync(TipType tipType, CancellationToken cancellationToken = default)
        {
            await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    await _context.ExecuteInTransactionAsync(async () =>
                    {
                        // Update main tip type entity
                        var entity = _compiledTipTypeDomainToEntity(tipType);
                        await _context.UpdateAsync(entity);

                        // Handle tip updates efficiently
                        await UpdateAssociatedTipsAsync(tipType);

                        _logger.LogInformation("Updated tip type with ID {TipTypeId}", tipType.Id);
                    });
                },
                _exceptionMapper,
                "Update",
                "tiptype",
                _logger);
        }

        public async Task DeleteAsync(TipType tipType, CancellationToken cancellationToken = default)
        {
            await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    await _context.ExecuteInTransactionAsync(async () =>
                    {
                        // Delete associated tips first (foreign key constraint)
                        await _context.ExecuteAsync(_queryCache["DeleteTipsByTipType"], tipType.Id);

                        // Delete tip type entity
                        var entity = _compiledTipTypeDomainToEntity(tipType);
                        await _context.DeleteAsync(entity);

                        _logger.LogInformation("Deleted tip type with ID {TipTypeId}", tipType.Id);
                    });
                },
                _exceptionMapper,
                "Delete",
                "tiptype",
                _logger);
        }

        public async Task<TipType?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    var entities = await _context.QueryAsync<TipTypeEntity>(_queryCache["GetTipTypeByName"], name);
                    var entity = entities.FirstOrDefault();
                    return entity != null ? _compiledTipTypeEntityToDomain(entity) : null;
                },
                _exceptionMapper,
                "GetByName",
                "tiptype",
                _logger);
        }

        public async Task<TipType?> GetWithTipsAsync(int id, CancellationToken cancellationToken = default)
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    // Get tip type and associated tips concurrently
                    var tipTypeTask = _context.GetAsync<TipTypeEntity>(id);
                    var tipsTask = _context.QueryAsync<TipEntity>(_queryCache["GetTipsByTipType"], id);

                    await Task.WhenAll(tipTypeTask, tipsTask);

                    var tipTypeEntity = await tipTypeTask;
                    if (tipTypeEntity == null)
                    {
                        return null;
                    }

                    var tipEntities = await tipsTask;
                    var tipType = _compiledTipTypeEntityToDomain(tipTypeEntity);

                    // Add tips to tip type efficiently
                    foreach (var tipEntity in tipEntities)
                    {
                        var tip = CreateTipFromEntity(tipEntity);
                        tipType.AddTip(tip);
                    }

                    return tipType;
                },
                _exceptionMapper,
                "GetWithTips",
                "tiptype",
                _logger);
        }

        #endregion

        #region Advanced Query Operations

        public async Task<IEnumerable<TipType>> GetAllWithTipsAsync(CancellationToken cancellationToken = default)
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    // Get all tip types and all tips concurrently
                    var tipTypesTask = _context.QueryAsync<TipTypeEntity>(_queryCache["GetAllTipTypes"]);
                    var allTipsTask = _context.QueryAsync<TipEntity>(_queryCache["GetAllTips"]);

                    await Task.WhenAll(tipTypesTask, allTipsTask);

                    var tipTypeEntities = await tipTypesTask;
                    var allTipEntities = await allTipsTask;

                    // Group tips by tip type ID for efficient lookup
                    var tipsByTypeId = allTipEntities
                        .GroupBy(t => t.TipTypeId)
                        .ToDictionary(g => g.Key, g => g.ToList());

                    var tipTypes = new List<TipType>();

                    foreach (var tipTypeEntity in tipTypeEntities)
                    {
                        var tipType = _compiledTipTypeEntityToDomain(tipTypeEntity);

                        // Add associated tips if they exist
                        if (tipsByTypeId.TryGetValue(tipTypeEntity.Id, out var tipEntities))
                        {
                            foreach (var tipEntity in tipEntities)
                            {
                                var tip = CreateTipFromEntity(tipEntity);
                                tipType.AddTip(tip);
                            }
                        }

                        tipTypes.Add(tipType);
                    }

                    return tipTypes;
                },
                _exceptionMapper,
                "GetAllWithTips",
                "tiptype",
                _logger);
        }

        public async Task<IEnumerable<TipType>> GetActiveWithTipCountsAsync(CancellationToken cancellationToken = default)
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    var results = await _context.QueryAsync<TipTypeWithCountDto>(_queryCache["GetTipTypesWithCounts"]);

                    return results.Select(result =>
                    {
                        var tipType = new TipType(result.Name);
                        SetOptimizedProperty(tipType, "Id", result.Id);
                        SetOptimizedProperty(tipType, "I8n", result.I8n);

                        // You could store tip count in a property if needed
                        // This is just an example of projection with aggregated data
                        return tipType;
                    });
                },
                _exceptionMapper,
                "GetActiveWithTipCounts",
                "tiptype",
                _logger);
        }

        public async Task<Dictionary<int, int>> GetTipCountsByTipTypeAsync(CancellationToken cancellationToken = default)
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    var results = await _context.QueryAsync<TipCountByTypeDto>(_queryCache["GetTipCountsByTipType"]);
                    return results.ToDictionary(r => r.TipTypeId, r => r.TipCount);
                },
                _exceptionMapper,
                "GetTipCountsByTipType",
                "tiptype",
                _logger);
        }

        #endregion

        #region Bulk Operations

        public async Task<IEnumerable<TipType>> CreateBulkAsync(IEnumerable<TipType> tipTypes, CancellationToken cancellationToken = default)
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    var tipTypeList = tipTypes.ToList();
                    if (!tipTypeList.Any()) return tipTypeList;

                    return await _context.ExecuteInTransactionAsync(async () =>
                    {
                        // Bulk insert tip type entities
                        var tipTypeEntities = tipTypeList.Select(_compiledTipTypeDomainToEntity).ToList();
                        await _context.BulkInsertAsync(tipTypeEntities, 50);

                        // Update tip type IDs
                        for (int i = 0; i < tipTypeList.Count; i++)
                        {
                            SetOptimizedProperty(tipTypeList[i], "Id", tipTypeEntities[i].Id);
                        }

                        // Bulk insert all associated tips
                        var allTipEntities = new List<TipEntity>();
                        for (int i = 0; i < tipTypeList.Count; i++)
                        {
                            var tipType = tipTypeList[i];
                            var tipTypeId = tipTypeEntities[i].Id;

                            var tipEntities = tipType.Tips.Select(tip =>
                            {
                                var entity = _compiledTipDomainToEntity(tip);
                                entity.TipTypeId = tipTypeId;
                                return entity;
                            });

                            allTipEntities.AddRange(tipEntities);
                        }

                        if (allTipEntities.Any())
                        {
                            await _context.BulkInsertAsync(allTipEntities, 100);
                        }

                        _logger.LogInformation("Bulk created {Count} tip types with tips", tipTypeList.Count);
                        return tipTypeList;
                    });
                },
                _exceptionMapper,
                "CreateBulk",
                "tiptype",
                _logger);
        }

        public async Task<int> UpdateBulkAsync(IEnumerable<TipType> tipTypes, CancellationToken cancellationToken = default)
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    var tipTypeList = tipTypes.ToList();
                    if (!tipTypeList.Any()) return 0;

                    return await _context.ExecuteInTransactionAsync(async () =>
                    {
                        // Bulk update tip type entities
                        var tipTypeEntities = tipTypeList.Select(_compiledTipTypeDomainToEntity).ToList();
                        var result = await _context.BulkUpdateAsync(tipTypeEntities, 50);

                        // Update associated tips for each tip type
                        foreach (var tipType in tipTypeList)
                        {
                            await UpdateAssociatedTipsAsync(tipType);
                        }

                        _logger.LogInformation("Bulk updated {Count} tip types", result);
                        return result;
                    });
                },
                _exceptionMapper,
                "UpdateBulk",
                "tiptype",
                _logger);
        }

        public async Task<int> DeleteBulkAsync(IEnumerable<int> tipTypeIds, CancellationToken cancellationToken = default)
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    var idList = tipTypeIds.ToList();
                    if (!idList.Any()) return 0;

                    return await _context.ExecuteInTransactionAsync(async () =>
                    {
                        // Delete associated tips first
                        const int batchSize = 50;
                        var batches = idList.Chunk(batchSize);

                        foreach (var batch in batches)
                        {
                            var placeholders = string.Join(",", batch.Select(_ => "?"));
                            var sql = $"DELETE FROM TipEntity WHERE TipTypeId IN ({placeholders})";
                            var parameters = batch.Cast<object>().ToArray();
                            await _context.ExecuteAsync(sql, parameters);
                        }

                        // Delete tip types
                        int totalDeleted = 0;
                        foreach (var batch in batches)
                        {
                            var placeholders = string.Join(",", batch.Select(_ => "?"));
                            var sql = $"DELETE FROM TipTypeEntity WHERE Id IN ({placeholders})";
                            var parameters = batch.Cast<object>().ToArray();
                            totalDeleted += await _context.ExecuteAsync(sql, parameters);
                        }

                        _logger.LogInformation("Bulk deleted {Count} tip types", totalDeleted);
                        return totalDeleted;
                    });
                },
                _exceptionMapper,
                "DeleteBulk",
                "tiptype",
                _logger);
        }

        #endregion

        #region Helper Methods

        private async Task UpdateAssociatedTipsAsync(TipType tipType)
        {
            // Delete existing tips for this tip type
            await _context.ExecuteAsync(_queryCache["DeleteTipsByTipType"], tipType.Id);

            // Insert current tips
            if (tipType.Tips.Any())
            {
                var tipEntities = tipType.Tips.Select(tip =>
                {
                    var entity = _compiledTipDomainToEntity(tip);
                    entity.TipTypeId = tipType.Id;
                    entity.Id = 0; // Reset ID for insert
                    return entity;
                }).ToList();

                await _context.BulkInsertAsync(tipEntities, 50);

                // Update tip IDs
                for (int i = 0; i < tipType.Tips.Count; i++)
                {
                    SetOptimizedProperty(tipType.Tips.ElementAt(i), "Id", tipEntities[i].Id);
                }
            }
        }

        private Tip CreateTipFromEntity(TipEntity entity)
        {
            var tip = _compiledTipEntityToDomain(entity);

            // Set additional properties
            SetOptimizedProperty(tip, "Id", entity.Id);
            SetOptimizedProperty(tip, "_fstop", entity.Fstop);
            SetOptimizedProperty(tip, "_shutterSpeed", entity.ShutterSpeed);
            SetOptimizedProperty(tip, "_iso", entity.Iso);
            SetOptimizedProperty(tip, "I8n", entity.I8n);

            return tip;
        }

        #endregion

        #region Compiled Mapping Methods

        private static Func<TipTypeEntity, TipType> CompileTipTypeEntityToDomainMapper()
        {
            var entityParam = Expression.Parameter(typeof(TipTypeEntity), "entity");

            // Create TipType using constructor
            var tipTypeConstructor = typeof(TipType).GetConstructor(new[] { typeof(string) });
            if (tipTypeConstructor == null)
            {
                throw new InvalidOperationException("Cannot find TipType constructor");
            }

            var tipTypeNew = Expression.New(tipTypeConstructor,
                Expression.Property(entityParam, nameof(TipTypeEntity.Name)));

            // Create initialization expressions for setting properties
            var tipTypeVar = Expression.Variable(typeof(TipType), "tipType");

            var initExpressions = new List<Expression>
               {
                   Expression.Assign(tipTypeVar, tipTypeNew),
       
                   // Set the Id property using direct property access
                   Expression.Assign(
                       Expression.Property(tipTypeVar, "Id"),
                       Expression.Property(entityParam, nameof(TipTypeEntity.Id))
                   ),
       
                   // Set the I8n property using direct property access
                   Expression.Assign(
                       Expression.Property(tipTypeVar, "I8n"),
                       Expression.Property(entityParam, nameof(TipTypeEntity.I8n))
                   ),

                   tipTypeVar
               };

            var body = Expression.Block(new[] { tipTypeVar }, initExpressions);
            return Expression.Lambda<Func<TipTypeEntity, TipType>>(body, entityParam).Compile();
        }

        private static Func<TipType, TipTypeEntity> CompileTipTypeDomainToEntityMapper()
        {
            var tipTypeParam = Expression.Parameter(typeof(TipType), "tipType");

            var entityNew = Expression.MemberInit(
                Expression.New(typeof(TipTypeEntity)),
                Expression.Bind(typeof(TipTypeEntity).GetProperty(nameof(TipTypeEntity.Id))!,
                    Expression.Property(tipTypeParam, "Id")),
                Expression.Bind(typeof(TipTypeEntity).GetProperty(nameof(TipTypeEntity.Name))!,
                    Expression.Property(tipTypeParam, "Name")),
                Expression.Bind(typeof(TipTypeEntity).GetProperty(nameof(TipTypeEntity.I8n))!,
                    Expression.Property(tipTypeParam, "I8n"))
            );

            return Expression.Lambda<Func<TipType, TipTypeEntity>>(entityNew, tipTypeParam).Compile();
        }

        private static Func<TipEntity, Tip> CompileTipEntityToDomainMapper()
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

            return Expression.Lambda<Func<TipEntity, Tip>>(tipNew, entityParam).Compile();
        }

        private static Func<Tip, TipEntity> CompileTipDomainToEntityMapper()
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

            // TipType property setters
            var tipTypeProps = typeof(TipType).GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var prop in tipTypeProps.Where(p => p.CanWrite))
            {
                var objParam = Expression.Parameter(typeof(object), "obj");
                var valueParam = Expression.Parameter(typeof(object), "value");

                var castObj = Expression.Convert(objParam, typeof(TipType));
                var castValue = Expression.Convert(valueParam, prop.PropertyType);
                var setProp = Expression.Call(castObj, prop.GetSetMethod(true)!, castValue);

                var lambda = Expression.Lambda<Action<object, object>>(setProp, objParam, valueParam);
                setters[$"TipType.{prop.Name}"] = lambda.Compile();
            }

            // Tip property setters
            var tipProps = typeof(Tip).GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var prop in tipProps.Where(p => p.CanWrite))
            {
                var objParam = Expression.Parameter(typeof(object), "obj");
                var valueParam = Expression.Parameter(typeof(object), "value");

                var castObj = Expression.Convert(objParam, typeof(Tip));
                var castValue = Expression.Convert(valueParam, prop.PropertyType);
                var setProp = Expression.Call(castObj, prop.GetSetMethod(true)!, castValue);

                var lambda = Expression.Lambda<Action<object, object>>(setProp, objParam, valueParam);
                setters[$"Tip.{prop.Name}"] = lambda.Compile();
            }

            return setters;
        }

        private static Dictionary<string, string> InitializeQueryCache()
        {
            return new Dictionary<string, string>
            {
                ["GetAllTipTypes"] = "SELECT * FROM TipTypeEntity ORDER BY Name",
                ["GetTipTypeByName"] = "SELECT * FROM TipTypeEntity WHERE Name = ? LIMIT 1",
                ["GetTipsByTipType"] = "SELECT * FROM TipEntity WHERE TipTypeId = ? ORDER BY Id",
                ["GetAllTips"] = "SELECT * FROM TipEntity ORDER BY TipTypeId, Id",
                ["DeleteTipsByTipType"] = "DELETE FROM TipEntity WHERE TipTypeId = ?",
                ["GetTipTypesWithCounts"] = @"
                   SELECT tt.Id, tt.Name, tt.I8n, COALESCE(COUNT(t.Id), 0) as TipCount
                   FROM TipTypeEntity tt
                   LEFT JOIN TipEntity t ON tt.Id = t.TipTypeId
                   GROUP BY tt.Id, tt.Name, tt.I8n
                   ORDER BY tt.Name",
                ["GetTipCountsByTipType"] = @"
                   SELECT TipTypeId, COUNT(*) as TipCount
                   FROM TipEntity
                   GROUP BY TipTypeId
                   ORDER BY TipTypeId"
            };
        }

        private static void SetOptimizedProperty(object obj, string propertyName, object value)
        {
            var key = $"{obj.GetType().Name}.{propertyName}";

            if (_propertySetters.TryGetValue(key, out var setter))
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

        #region DTOs for Projections

        public class TipTypeWithCountDto
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string I8n { get; set; } = string.Empty;
            public int TipCount { get; set; }
        }

        public class TipCountByTypeDto
        {
            public int TipTypeId { get; set; }
            public int TipCount { get; set; }
        }

        #endregion
    }

    // Extension methods for chunking (if not already defined elsewhere)
    public static class TipTypeEnumerableExtensions
    {
        public static IEnumerable<IEnumerable<T>> Chunk<T>(this IEnumerable<T> source, int size)
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