using Location.Core.Application.Common.Interfaces.Persistence;
using Location.Core.Application.Common.Models;
using Location.Core.Domain.Rules;
using Location.Core.Domain.ValueObjects;
using Location.Core.Infrastructure.Data.Entities;
using Location.Core.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SQLite;
using System.Text;

namespace Location.Core.Infrastructure.Data.Repositories
{
    public class LocationRepository : Location.Core.Application.Common.Interfaces.Persistence.ILocationRepository
    {
        private readonly IDatabaseContext _context;
        private readonly ILogger<LocationRepository> _logger;
        private readonly IInfrastructureExceptionMappingService _exceptionMapper;

        public LocationRepository(IDatabaseContext context, ILogger<LocationRepository> logger, IInfrastructureExceptionMappingService exceptionMapper)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _exceptionMapper = exceptionMapper ?? throw new ArgumentNullException(nameof(exceptionMapper));
        }

        #region Existing Methods (Backward Compatibility)
        #region Specification Pattern

        public async Task<IReadOnlyList<Domain.Entities.Location>> GetBySpecificationAsync(
            Location.Core.Application.Common.Interfaces.ISqliteSpecification<Domain.Entities.Location> specification,
            CancellationToken cancellationToken = default)
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    var sql = BuildSpecificationQuery("*", specification);
                    var entities = await ExecuteQueryInternalAsync<LocationEntity>(sql, specification.Parameters);
                    return entities.Select(MapToDomain).ToList().AsReadOnly();
                },
                _exceptionMapper,
                "GetBySpecification",
                "location",
                _logger);
        }

        public async Task<PagedList<T>> GetPagedBySpecificationAsync<T>(
            Location.Core.Application.Common.Interfaces.ISqliteSpecification<Domain.Entities.Location> specification,
            int pageNumber,
            int pageSize,
            string selectColumns,
            CancellationToken cancellationToken = default) where T : class, new()
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    // Get count
                    var countSql = BuildSpecificationQuery("COUNT(*)", specification, false);
                    var totalCount = await ExecuteScalarAsync<int>(countSql, specification.Parameters);

                    // Get paged data
                    var dataSql = BuildSpecificationQuery(selectColumns, specification, true, pageNumber, pageSize);
                    var items = await ExecuteQueryInternalAsync<T>(dataSql, specification.Parameters);

                    return PagedList<T>.CreateOptimized(items, totalCount, pageNumber, pageSize);
                },
                _exceptionMapper,
                "GetPagedBySpecification",
                "location",
                _logger);
        }

        #endregion
        public async Task<Domain.Entities.Location?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    var entity = await _context.GetAsync<LocationEntity>(id);
                    return entity != null ? MapToDomain(entity) : null;
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

                    return entities.Select(MapToDomain);
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

                    return entities.Select(MapToDomain);
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

                    var entity = MapToEntity(location);
                    entity.Timestamp = DateTime.UtcNow;

                    await _context.InsertAsync(entity);
                    SetPrivateProperty(location, "Id", entity.Id);

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

                    var entity = MapToEntity(location);
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
                    var entity = MapToEntity(location);
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

                    return entity != null ? MapToDomain(entity) : null;
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
                        var location = MapToDomain(entity);
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

                    var locations = entities.Select(MapToDomain);

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
                    var countSql = $"SELECT COUNT(*) FROM LocationEntity";
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

                    // Get total count
                    var totalCount = await ExecuteScalarAsync<int>(countSql, parameters);

                    // Get paged data
                    var items = await ExecuteQueryInternalAsync<T>(selectSql, parameters);

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
                          AND Latitude BETWEEN @minLat AND @maxLat 
                          AND Longitude BETWEEN @minLng AND @maxLng
                        ORDER BY Timestamp DESC";

                    var parameters = new Dictionary<string, object>
                    {
                        { "@minLat", latitude - latRange },
                        { "@maxLat", latitude + latRange },
                        { "@minLng", longitude - lngRange },
                        { "@maxLng", longitude + lngRange }
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
                    var sql = $"SELECT {selectColumns} FROM LocationEntity WHERE Id = @id";
                    var parameters = new Dictionary<string, object> { { "@id", id } };

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
                    var entities = locationList.Select(MapToEntity).ToList();

                    await _context.BeginTransactionAsync();
                    try
                    {
                        foreach (var entity in entities)
                        {
                            entity.Timestamp = DateTime.UtcNow;
                            await _context.InsertAsync(entity);
                        }

                        await _context.CommitTransactionAsync();

                        // Update domain objects with generated IDs
                        for (int i = 0; i < locationList.Count; i++)
                        {
                            SetPrivateProperty(locationList[i], "Id", entities[i].Id);
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
                    var entities = locationList.Select(MapToEntity).ToList();

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
                    var sql = "SELECT EXISTS(SELECT 1 FROM LocationEntity WHERE Id = @id)";
                    var parameters = new Dictionary<string, object> { { "@id", id } };
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
            Location.Core.Application.Common.Interfaces.ISqliteSpecification<Domain.Entities.Location> specification,
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

        #region Mapping Methods

        private Domain.Entities.Location MapToDomain(LocationEntity entity)
        {
            var coordinate = new Coordinate(entity.Latitude, entity.Longitude);
            var address = new Address(entity.City, entity.State);

            var location = CreateLocationViaReflection(entity.Title, entity.Description, coordinate, address);

            SetPrivateProperty(location, "Id", entity.Id);
            SetPrivateProperty(location, "PhotoPath", entity.PhotoPath);
            SetPrivateProperty(location, "IsDeleted", entity.IsDeleted);
            SetPrivateProperty(location, "Timestamp", entity.Timestamp);

            return location;
        }

        private LocationEntity MapToEntity(Domain.Entities.Location location)
        {
            return new LocationEntity
            {
                Id = location.Id,
                Title = location.Title,
                Description = location.Description,
                Latitude = location.Coordinate.Latitude,
                Longitude = location.Coordinate.Longitude,
                City = location.Address.City,
                State = location.Address.State,
                PhotoPath = location.PhotoPath,
                IsDeleted = location.IsDeleted,
                Timestamp = location.Timestamp
            };
        }

        private Domain.Entities.Location CreateLocationViaReflection(string title, string description, Coordinate coordinate, Address address)
        {
            var type = typeof(Domain.Entities.Location);
            var constructor = type.GetConstructor(new[] { typeof(string), typeof(string), typeof(Coordinate), typeof(Address) });

            if (constructor == null)
            {
                throw new InvalidOperationException("Cannot find Location constructor");
            }

            return (Domain.Entities.Location)constructor.Invoke(new object[] { title, description, coordinate, address });
        }

        private void SetPrivateProperty(object obj, string propertyName, object value)
        {
            var property = obj.GetType().GetProperty(propertyName);
            property?.SetValue(obj, value);
        }

      

        public Task<PagedList<T>> GetPagedBySpecificationAsync<T>(ISqliteSpecification<Domain.Entities.Location> specification, int pageNumber, int pageSize, string selectColumns, CancellationToken cancellationToken = default) where T : class, new()
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyList<Domain.Entities.Location>> GetBySpecificationAsync(ISqliteSpecification<Domain.Entities.Location> specification, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}