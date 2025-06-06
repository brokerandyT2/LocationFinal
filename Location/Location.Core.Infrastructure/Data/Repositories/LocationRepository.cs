using Location.Core.Application.Common.Models;
using Location.Core.Domain.Rules;
using Location.Core.Domain.ValueObjects;
using Location.Core.Infrastructure.Data.Entities;
using Location.Core.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Location.Core.Infrastructure.Data.Repositories
{
    public class LocationRepository : Location.Core.Application.Common.Interfaces.Persistence.ILocationRepository
    {
        private readonly IDatabaseContext _context;
        private readonly ILogger<LocationRepository> _logger;
        private readonly IInfrastructureExceptionMappingService _exceptionMapper;

        // Compiled mapping delegates for performance
        private static readonly Func<LocationEntity, Domain.Entities.Location> _compiledEntityToDomain;
        private static readonly Func<Domain.Entities.Location, LocationEntity> _compiledDomainToEntity;

        // Cached property setters for reflection performance
        private static readonly Dictionary<string, Action<object, object>> _propertySetters;

        static LocationRepository()
        {
            _compiledEntityToDomain = CompileEntityToDomainMapper();
            _compiledDomainToEntity = CompileDomainToEntityMapper();
            _propertySetters = CreatePropertySetters();
        }

        public LocationRepository(IDatabaseContext context, ILogger<LocationRepository> logger, IInfrastructureExceptionMappingService exceptionMapper)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _exceptionMapper = exceptionMapper ?? throw new ArgumentNullException(nameof(exceptionMapper));
        }

        #region Existing Methods (Backward Compatibility)

        public async Task<Domain.Entities.Location?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    var entity = await _context.GetAsync<LocationEntity>(id);
                    return entity != null ? _compiledEntityToDomain(entity) : null;
                },
                _exceptionMapper,
                "GetById",
                "location",
                _logger);
        }

        public async Task<IEnumerable<Domain.Entities.Location>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    var entities = await _context.Table<LocationEntity>()
                        .OrderByDescending(l => l.Timestamp)
                        .ToListAsync();

                    return entities.Select(_compiledEntityToDomain);
                },
                _exceptionMapper,
                "GetAll",
                "location",
                _logger);
        }

        public async Task<IEnumerable<Domain.Entities.Location>> GetActiveAsync(CancellationToken cancellationToken = default)
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    var entities = await _context.Table<LocationEntity>()
                        .Where(l => !l.IsDeleted)
                        .OrderByDescending(l => l.Timestamp)
                        .ToListAsync();

                    return entities.Select(_compiledEntityToDomain);
                },
                _exceptionMapper,
                "GetActive",
                "location",
                _logger);
        }

        public async Task<Domain.Entities.Location> AddAsync(Domain.Entities.Location location, CancellationToken cancellationToken = default)
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    if (!LocationValidationRules.IsValid(location, out var errors))
                    {
                        throw new InvalidOperationException(string.Join("; ", errors));
                    }

                    var entity = _compiledDomainToEntity(location);
                    entity.Timestamp = DateTime.UtcNow;

                    await _context.InsertAsync(entity);
                    SetOptimizedProperty(location, "Id", entity.Id);

                    _logger.LogInformation("Created location with ID {LocationId}", entity.Id);
                    return location;
                },
                _exceptionMapper,
                "Add",
                "location",
                _logger);
        }

        public async Task UpdateAsync(Domain.Entities.Location location, CancellationToken cancellationToken = default)
        {
            await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    if (!LocationValidationRules.IsValid(location, out var errors))
                    {
                        throw new InvalidOperationException(string.Join("; ", errors));
                    }

                    var entity = _compiledDomainToEntity(location);
                    await _context.UpdateAsync(entity);

                    _logger.LogInformation("Updated location with ID {LocationId}", location.Id);
                },
                _exceptionMapper,
                "Update",
                "location",
                _logger);
        }

        public async Task DeleteAsync(Domain.Entities.Location location, CancellationToken cancellationToken = default)
        {
            await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    var entity = _compiledDomainToEntity(location);
                    await _context.DeleteAsync(entity);

                    _logger.LogInformation("Deleted location with ID {LocationId}", location.Id);
                },
                _exceptionMapper,
                "Delete",
                "location",
                _logger);
        }

        public async Task<Domain.Entities.Location?> GetByTitleAsync(string title, CancellationToken cancellationToken = default)
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    var entity = await _context.Table<LocationEntity>()
                        .Where(l => l.Title == title)
                        .FirstOrDefaultAsync();

                    return entity != null ? _compiledEntityToDomain(entity) : null;
                },
                _exceptionMapper,
                "GetByTitle",
                "location",
                _logger);
        }

        public async Task<IEnumerable<Domain.Entities.Location>> GetNearbyAsync(
            double latitude,
            double longitude,
            double distanceKm,
            CancellationToken cancellationToken = default)
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    // Use bounding box for initial SQLite filtering, then exact distance calculation
                    var latRange = distanceKm / 111.0;
                    var lngRange = distanceKm / (111.0 * Math.Cos(latitude * Math.PI / 180.0));

                    var entities = await _context.Table<LocationEntity>()
                        .Where(l => !l.IsDeleted &&
                                   l.Latitude >= latitude - latRange &&
                                   l.Latitude <= latitude + latRange &&
                                   l.Longitude >= longitude - lngRange &&
                                   l.Longitude <= longitude + lngRange)
                        .ToListAsync();

                    var centerCoordinate = new Coordinate(latitude, longitude);
                    var nearbyLocations = new List<Domain.Entities.Location>();

                    foreach (var entity in entities)
                    {
                        var location = _compiledEntityToDomain(entity);
                        if (location.Coordinate.DistanceTo(centerCoordinate) <= distanceKm)
                        {
                            nearbyLocations.Add(location);
                        }
                    }

                    return nearbyLocations;
                },
                _exceptionMapper,
                "GetNearby",
                "location",
                _logger);
        }

        public async Task<PagedList<Domain.Entities.Location>> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            bool includeDeleted = false,
            CancellationToken cancellationToken = default)
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    var query = _context.Table<LocationEntity>();

                    // Apply filters
                    if (!includeDeleted)
                    {
                        query = query.Where(l => !l.IsDeleted);
                    }

                    if (!string.IsNullOrWhiteSpace(searchTerm))
                    {
                        query = query.Where(l =>
                            l.Title.Contains(searchTerm) ||
                            l.Description.Contains(searchTerm) ||
                            l.City.Contains(searchTerm) ||
                            l.State.Contains(searchTerm));
                    }

                    // Get total count
                    var totalCount = await query.CountAsync();

                    // Apply paging and ordering
                    var entities = await query
                        .OrderByDescending(l => l.Timestamp)
                        .Skip((pageNumber - 1) * pageSize)
                        .Take(pageSize)
                        .ToListAsync();

                    var locations = entities.Select(_compiledEntityToDomain);

                    return PagedList<Domain.Entities.Location>.CreateOptimized(
                        locations,
                        totalCount,
                        pageNumber,
                        pageSize);
                },
                _exceptionMapper,
                "GetPaged",
                "location",
                _logger);
        }

        #endregion

        #region SQLite-Optimized Projection Methods

        public async Task<PagedList<T>> GetPagedProjectedAsync<T>(
            int pageNumber,
            int pageSize,
            string selectColumns,
            string? whereClause = null,
            Dictionary<string, object>? parameters = null,
            string? orderBy = null,
            CancellationToken cancellationToken = default) where T : class, new()
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    var countSql = "SELECT COUNT(*) FROM LocationEntity";
                    var selectSql = $"SELECT {selectColumns} FROM LocationEntity";

                    if (!string.IsNullOrEmpty(whereClause))
                    {
                        countSql += $" WHERE {whereClause}";
                        selectSql += $" WHERE {whereClause}";
                    }

                    if (!string.IsNullOrEmpty(orderBy))
                    {
                        selectSql += $" ORDER BY {orderBy}";
                    }
                    else
                    {
                        selectSql += " ORDER BY Timestamp DESC";
                    }

                    selectSql += $" LIMIT {pageSize} OFFSET {(pageNumber - 1) * pageSize}";

                    // Execute both queries concurrently for better performance
                    var countTask = ExecuteScalarAsync<int>(countSql, parameters);
                    var dataTask = ExecuteQueryInternalAsync<T>(selectSql, parameters);

                    await Task.WhenAll(countTask, dataTask);

                    var totalCount = await countTask;
                    var items = await dataTask;

                    return PagedList<T>.CreateOptimized(items, totalCount, pageNumber, pageSize);
                },
                _exceptionMapper,
                "GetPagedProjected",
                "location",
                _logger);
        }

        public async Task<IReadOnlyList<T>> GetActiveProjectedAsync<T>(
            string selectColumns,
            string? additionalWhere = null,
            Dictionary<string, object>? parameters = null,
            CancellationToken cancellationToken = default) where T : class, new()
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    var sql = $"SELECT {selectColumns} FROM LocationEntity WHERE IsDeleted = 0";

                    if (!string.IsNullOrEmpty(additionalWhere))
                    {
                        sql += $" AND ({additionalWhere})";
                    }

                    sql += " ORDER BY Timestamp DESC";

                    return await ExecuteQueryInternalAsync<T>(sql, parameters);
                },
                _exceptionMapper,
                "GetActiveProjected",
                "location",
                _logger);
        }

        public async Task<IReadOnlyList<T>> GetNearbyProjectedAsync<T>(
            double latitude,
            double longitude,
            double distanceKm,
            string selectColumns,
            CancellationToken cancellationToken = default) where T : class, new()
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    var latRange = distanceKm / 111.0;
                    var lngRange = distanceKm / (111.0 * Math.Cos(latitude * Math.PI / 180.0));

                    var sql = $@"
                       SELECT {selectColumns}
                       FROM LocationEntity 
                       WHERE IsDeleted = 0 
                         AND Latitude BETWEEN ? AND ? 
                         AND Longitude BETWEEN ? AND ?
                       ORDER BY Timestamp DESC";

                    var parameters = new Dictionary<string, object>
                    {
                       { "param1", latitude - latRange },
                       { "param2", latitude + latRange },
                       { "param3", longitude - lngRange },
                       { "param4", longitude + lngRange }
                    };

                    return await ExecuteQueryInternalAsync<T>(sql, parameters);
                },
                _exceptionMapper,
                "GetNearbyProjected",
                "location",
                _logger);
        }

        public async Task<T?> GetByIdProjectedAsync<T>(
            int id,
            string selectColumns,
            CancellationToken cancellationToken = default) where T : class, new()
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    var sql = $"SELECT {selectColumns} FROM LocationEntity WHERE Id = ?";
                    var parameters = new Dictionary<string, object> { { "param1", id } };

                    var results = await ExecuteQueryInternalAsync<T>(sql, parameters);
                    return results.FirstOrDefault();
                },
                _exceptionMapper,
                "GetByIdProjected",
                "location",
                _logger);
        }

        #endregion

        #region Specification Pattern

        public async Task<IReadOnlyList<Domain.Entities.Location>> GetBySpecificationAsync(
            Location.Core.Application.Common.Interfaces.Persistence.ISqliteSpecification<Domain.Entities.Location> specification,
            CancellationToken cancellationToken = default)
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    var sql = BuildSpecificationQuery("*", specification);
                    var entities = await ExecuteQueryInternalAsync<LocationEntity>(sql, specification.Parameters);
                    return entities.Select(_compiledEntityToDomain).ToList().AsReadOnly();
                },
                _exceptionMapper,
                "GetBySpecification",
                "location",
                _logger);
        }

        /*    public async Task<PagedList<T>> GetPagedBySpecificationAsync<T>(
                Location.Core.Application.Common.Interfaces.ISqliteSpecification<Domain.Entities.Location> specification,
                int pageNumber,
                int pageSize,
                string selectColumns,
                CancellationToken cancellationToken = default) where T : class, new()
            {
                return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                    async () =>
                    {
                        // Get count and data concurrently
                        var countSql = BuildSpecificationQuery("COUNT(*)", specification, false);
                        var dataSql = BuildSpecificationQuery(selectColumns, specification, true, pageNumber, pageSize);

                        var countTask = ExecuteScalarAsync<int>(countSql, specification.Parameters);
                        var dataTask = ExecuteQueryInternalAsync<T>(dataSql, specification.Parameters);

                        await Task.WhenAll(countTask, dataTask);

                        var totalCount = await countTask;
                        var items = await dataTask;

                        return PagedList<T>.CreateOptimized(items, totalCount, pageNumber, pageSize);
                    },
                    _exceptionMapper,
                    "GetPagedBySpecification",
                    "location",
                    _logger);
            } */

        #endregion

        #region Bulk Operations

        public async Task<IReadOnlyList<Domain.Entities.Location>> CreateBulkAsync(
            IEnumerable<Domain.Entities.Location> locations,
            CancellationToken cancellationToken = default)
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    var locationList = locations.ToList();
                    if (!locationList.Any()) return locationList.AsReadOnly();

                    // Validate all locations first
                    foreach (var location in locationList)
                    {
                        if (!LocationValidationRules.IsValid(location, out var errors))
                        {
                            throw new InvalidOperationException($"Validation failed for location '{location.Title}': {string.Join("; ", errors)}");
                        }
                    }

                    var entities = locationList.Select(l =>
                    {
                        var entity = _compiledDomainToEntity(l);
                        entity.Timestamp = DateTime.UtcNow;
                        return entity;
                    }).ToList();

                    await _context.BeginTransactionAsync();
                    try
                    {
                        // Use bulk insert for better performance
                        foreach (var entity in entities)
                        {
                            await _context.InsertAsync(entity);
                        }

                        await _context.CommitTransactionAsync();

                        // Update domain objects with generated IDs
                        for (int i = 0; i < locationList.Count; i++)
                        {
                            SetOptimizedProperty(locationList[i], "Id", entities[i].Id);
                        }

                        _logger.LogInformation("Bulk created {Count} locations", locationList.Count);
                        return locationList.AsReadOnly();
                    }
                    catch
                    {
                        await _context.RollbackTransactionAsync();
                        throw;
                    }
                },
                _exceptionMapper,
                "CreateBulk",
                "location",
                _logger);
        }

        public async Task<int> UpdateBulkAsync(
            IEnumerable<Domain.Entities.Location> locations,
            CancellationToken cancellationToken = default)
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    var locationList = locations.ToList();
                    if (!locationList.Any()) return 0;

                    // Validate all locations first
                    foreach (var location in locationList)
                    {
                        if (!LocationValidationRules.IsValid(location, out var errors))
                        {
                            throw new InvalidOperationException($"Validation failed for location '{location.Title}': {string.Join("; ", errors)}");
                        }
                    }

                    var entities = locationList.Select(_compiledDomainToEntity).ToList();

                    await _context.BeginTransactionAsync();
                    try
                    {
                        int updatedCount = 0;
                        foreach (var entity in entities)
                        {
                            var result = await _context.UpdateAsync(entity);
                            updatedCount += result;
                        }

                        await _context.CommitTransactionAsync();
                        _logger.LogInformation("Bulk updated {Count} locations", updatedCount);
                        return updatedCount;
                    }
                    catch
                    {
                        await _context.RollbackTransactionAsync();
                        throw;
                    }
                },
                _exceptionMapper,
                "UpdateBulk",
                "location",
                _logger);
        }

        #endregion

        #region Count and Exists Methods

        public async Task<int> CountAsync(
            string? whereClause = null,
            Dictionary<string, object>? parameters = null,
            CancellationToken cancellationToken = default)
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    var sql = "SELECT COUNT(*) FROM LocationEntity";
                    if (!string.IsNullOrEmpty(whereClause))
                    {
                        sql += $" WHERE {whereClause}";
                    }

                    return await ExecuteScalarAsync<int>(sql, parameters);
                },
                _exceptionMapper,
                "Count",
                "location",
                _logger);
        }

        public async Task<bool> ExistsAsync(
            string whereClause,
            Dictionary<string, object> parameters,
            CancellationToken cancellationToken = default)
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    var sql = $"SELECT EXISTS(SELECT 1 FROM LocationEntity WHERE {whereClause})";
                    var result = await ExecuteScalarAsync<long>(sql, parameters);
                    return result > 0;
                },
                _exceptionMapper,
                "Exists",
                "location",
                _logger);
        }

        public async Task<bool> ExistsByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    var sql = "SELECT EXISTS(SELECT 1 FROM LocationEntity WHERE Id = ?)";
                    var parameters = new Dictionary<string, object> { { "param1", id } };
                    var result = await ExecuteScalarAsync<long>(sql, parameters);
                    return result > 0;
                },
                _exceptionMapper,
                "ExistsById",
                "location",
                _logger);
        }

        #endregion

        #region Raw SQL Execution

        public async Task<IReadOnlyList<T>> ExecuteQueryAsync<T>(
            string sql,
            Dictionary<string, object>? parameters = null,
            CancellationToken cancellationToken = default) where T : class, new()
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    return await ExecuteQueryInternalAsync<T>(sql, parameters);
                },
                _exceptionMapper,
                "ExecuteQuery",
                "location",
                _logger);
        }

        public async Task<int> ExecuteCommandAsync(
            string sql,
            Dictionary<string, object>? parameters = null,
            CancellationToken cancellationToken = default)
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    var connection = _context.GetConnection();

                    if (parameters?.Any() == true)
                    {
                        var args = parameters.Values.ToArray();
                        return await connection.ExecuteAsync(sql, args);
                    }

                    return await connection.ExecuteAsync(sql);
                },
                _exceptionMapper,
                "ExecuteCommand",
                "location",
                _logger);
        }

        #endregion

        #region Helper Methods

        private async Task<IReadOnlyList<T>> ExecuteQueryInternalAsync<T>(string sql, Dictionary<string, object>? parameters = null) where T : class, new()
        {
            var connection = _context.GetConnection();

            if (parameters?.Any() == true)
            {
                var args = parameters.Values.ToArray();
                var results = await connection.QueryAsync<T>(sql, args);
                return results.ToList().AsReadOnly();
            }

            var directResults = await connection.QueryAsync<T>(sql);
            return directResults.ToList().AsReadOnly();
        }

        private async Task<T> ExecuteScalarAsync<T>(string sql, Dictionary<string, object>? parameters = null)
        {
            var connection = _context.GetConnection();

            if (parameters?.Any() == true)
            {
                var args = parameters.Values.ToArray();
                return await connection.ExecuteScalarAsync<T>(sql, args);
            }

            return await connection.ExecuteScalarAsync<T>(sql);
        }

        private string BuildSpecificationQuery(
            string selectColumns,
            Location.Core.Application.Common.Interfaces.Persistence.ISqliteSpecification<Domain.Entities.Location> specification,
            bool includePaging = false,
            int pageNumber = 1,
            int pageSize = 10)
        {
            var sql = new StringBuilder($"SELECT {selectColumns} FROM LocationEntity");

            if (!string.IsNullOrEmpty(specification.Joins))
            {
                sql.Append($" {specification.Joins}");
            }

            if (!string.IsNullOrEmpty(specification.WhereClause))
            {
                sql.Append($" WHERE {specification.WhereClause}");
            }

            if (!string.IsNullOrEmpty(specification.OrderBy))
            {
                sql.Append($" ORDER BY {specification.OrderBy}");
            }

            if (includePaging)
            {
                sql.Append($" LIMIT {pageSize} OFFSET {(pageNumber - 1) * pageSize}");
            }
            else if (specification.Take.HasValue)
            {
                sql.Append($" LIMIT {specification.Take.Value}");
                if (specification.Skip.HasValue)
                {
                    sql.Append($" OFFSET {specification.Skip.Value}");
                }
            }

            return sql.ToString();
        }

        #endregion

        #region Compiled Mapping Methods

        private static Func<LocationEntity, Domain.Entities.Location> CompileEntityToDomainMapper()
        {
            // Create compiled expression for entity to domain mapping
            var entityParam = Expression.Parameter(typeof(LocationEntity), "entity");

            // Create Coordinate
            var coordinateConstructor = typeof(Coordinate).GetConstructor(new[] { typeof(double), typeof(double) });
            var coordinateNew = Expression.New(coordinateConstructor!,
                Expression.Property(entityParam, nameof(LocationEntity.Latitude)),
                Expression.Property(entityParam, nameof(LocationEntity.Longitude)));

            // Create Address
            var addressConstructor = typeof(Address).GetConstructor(new[] { typeof(string), typeof(string) });
            var addressNew = Expression.New(addressConstructor!,
                Expression.Property(entityParam, nameof(LocationEntity.City)),
                Expression.Property(entityParam, nameof(LocationEntity.State)));

            // Create Location using reflection to call constructor
            var locationConstructor = typeof(Domain.Entities.Location).GetConstructor(
                new[] { typeof(string), typeof(string), typeof(Coordinate), typeof(Address) });

            var locationNew = Expression.New(locationConstructor!,
                Expression.Property(entityParam, nameof(LocationEntity.Title)),
                Expression.Property(entityParam, nameof(LocationEntity.Description)),
                coordinateNew,
                addressNew);

            // Create initialization expressions for setting properties
            var locationVar = Expression.Variable(typeof(Domain.Entities.Location), "location");
            var initExpressions = new List<Expression>
   {
       Expression.Assign(locationVar, locationNew)
   };

            // Set the Id property correctly
            var idProperty = typeof(Domain.Entities.Location).GetProperty("Id");
            if (idProperty?.CanWrite == true)
            {
                initExpressions.Add(Expression.Assign(
                    Expression.Property(locationVar, idProperty),
                    Expression.Property(entityParam, nameof(LocationEntity.Id))));
            }

            // Set other properties
            var photoPathProperty = typeof(Domain.Entities.Location).GetProperty("PhotoPath");
            if (photoPathProperty?.CanWrite == true)
            {
                initExpressions.Add(Expression.Assign(
                    Expression.Property(locationVar, photoPathProperty),
                    Expression.Property(entityParam, nameof(LocationEntity.PhotoPath))));
            }

            var isDeletedProperty = typeof(Domain.Entities.Location).GetProperty("IsDeleted");
            if (isDeletedProperty?.CanWrite == true)
            {
                initExpressions.Add(Expression.Assign(
                    Expression.Property(locationVar, isDeletedProperty),
                    Expression.Property(entityParam, nameof(LocationEntity.IsDeleted))));
            }

            var timestampProperty = typeof(Domain.Entities.Location).GetProperty("Timestamp");
            if (timestampProperty?.CanWrite == true)
            {
                initExpressions.Add(Expression.Assign(
                    Expression.Property(locationVar, timestampProperty),
                    Expression.Property(entityParam, nameof(LocationEntity.Timestamp))));
            }

            initExpressions.Add(locationVar);

            var body = Expression.Block(new[] { locationVar }, initExpressions);
            return Expression.Lambda<Func<LocationEntity, Domain.Entities.Location>>(body, entityParam).Compile();
        }

        private static Func<Domain.Entities.Location, LocationEntity> CompileDomainToEntityMapper()
        {
            var locationParam = Expression.Parameter(typeof(Domain.Entities.Location), "location");

            var entityNew = Expression.MemberInit(
                Expression.New(typeof(LocationEntity)),
                Expression.Bind(typeof(LocationEntity).GetProperty(nameof(LocationEntity.Id))!,
                    Expression.Property(locationParam, "Id")),
                Expression.Bind(typeof(LocationEntity).GetProperty(nameof(LocationEntity.Title))!,
                    Expression.Property(locationParam, "Title")),
                Expression.Bind(typeof(LocationEntity).GetProperty(nameof(LocationEntity.Description))!,
                    Expression.Property(locationParam, "Description")),
                Expression.Bind(typeof(LocationEntity).GetProperty(nameof(LocationEntity.Latitude))!,
                    Expression.Property(Expression.Property(locationParam, "Coordinate"), "Latitude")),
                Expression.Bind(typeof(LocationEntity).GetProperty(nameof(LocationEntity.Longitude))!,
                    Expression.Property(Expression.Property(locationParam, "Coordinate"), "Longitude")),
                Expression.Bind(typeof(LocationEntity).GetProperty(nameof(LocationEntity.City))!,
                    Expression.Property(Expression.Property(locationParam, "Address"), "City")),
                Expression.Bind(typeof(LocationEntity).GetProperty(nameof(LocationEntity.State))!,
                    Expression.Property(Expression.Property(locationParam, "Address"), "State")),
                Expression.Bind(typeof(LocationEntity).GetProperty(nameof(LocationEntity.PhotoPath))!,
                    Expression.Property(locationParam, "PhotoPath")),
                Expression.Bind(typeof(LocationEntity).GetProperty(nameof(LocationEntity.IsDeleted))!,
                    Expression.Property(locationParam, "IsDeleted")),
                Expression.Bind(typeof(LocationEntity).GetProperty(nameof(LocationEntity.Timestamp))!,
                    Expression.Property(locationParam, "Timestamp"))
            );

            return Expression.Lambda<Func<Domain.Entities.Location, LocationEntity>>(entityNew, locationParam).Compile();
        }

        private static Dictionary<string, Action<object, object>> CreatePropertySetters()
        {
            var setters = new Dictionary<string, Action<object, object>>();
            var locationProps = typeof(Domain.Entities.Location).GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var prop in locationProps)
            {
                if (prop.CanWrite)
                {
                    var objParam = Expression.Parameter(typeof(object), "obj");
                    var valueParam = Expression.Parameter(typeof(object), "value");

                    var castObj = Expression.Convert(objParam, typeof(Domain.Entities.Location));
                    var castValue = Expression.Convert(valueParam, prop.PropertyType);
                    var setProp = Expression.Call(castObj, prop.GetSetMethod(true)!, castValue);

                    var lambda = Expression.Lambda<Action<object, object>>(setProp, objParam, valueParam);
                    setters[prop.Name] = lambda.Compile();
                }
            }

            return setters;
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


        public async Task<PagedList<T>> GetPagedBySpecificationAsync<T>(Location.Core.Application.Common.Interfaces.Persistence.ISqliteSpecification<Domain.Entities.Location> specification, int pageNumber, int pageSize, string selectColumns, CancellationToken cancellationToken = default) where T : class, new()
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    // Get count and data concurrently
                    var countSql = BuildSpecificationQuery("COUNT(*)", specification, false);
                    var dataSql = BuildSpecificationQuery(selectColumns, specification, true, pageNumber, pageSize);

                    var countTask = ExecuteScalarAsync<int>(countSql, specification.Parameters);
                    var dataTask = ExecuteQueryInternalAsync<T>(dataSql, specification.Parameters);

                    await Task.WhenAll(countTask, dataTask);

                    var totalCount = await countTask;
                    var items = await dataTask;

                    return PagedList<T>.CreateOptimized(items, totalCount, pageNumber, pageSize);
                },
                _exceptionMapper,
                "GetPagedBySpecification",
                "location",
                _logger);
        }

       

        #endregion
    }
}
