// Location.Core.Infrastructure/Data/Repositories/LocationRepositoryAdapter.cs
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Core.Infrastructure.Data.Repositories
{
    public class LocationRepositoryAdapter : ILocationRepository
    {
        private readonly LocationRepository _innerRepository;

        public LocationRepositoryAdapter(LocationRepository innerRepository)
        {
            _innerRepository = innerRepository ?? throw new ArgumentNullException(nameof(innerRepository));
        }

        // ===== EXISTING METHODS =====

        public async Task<Result<Domain.Entities.Location>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                var location = await _innerRepository.GetByIdAsync(id, cancellationToken);
                return location != null
                    ? Result<Domain.Entities.Location>.Success(location)
                    : Result<Domain.Entities.Location>.Failure($"Location with ID {id} not found");
            }
            catch (Exception ex)
            {
                return Result<Domain.Entities.Location>.Failure($"Failed to retrieve location: {ex.Message}");
            }
        }

        public async Task<Result<List<Domain.Entities.Location>>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var locations = await _innerRepository.GetAllAsync(cancellationToken);
                return Result<List<Domain.Entities.Location>>.Success(locations.ToList());
            }
            catch (Exception ex)
            {
                return Result<List<Domain.Entities.Location>>.Failure($"Failed to retrieve locations: {ex.Message}");
            }
        }

        public async Task<Result<List<Domain.Entities.Location>>> GetActiveAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var locations = await _innerRepository.GetActiveAsync(cancellationToken);
                return Result<List<Domain.Entities.Location>>.Success(locations.ToList());
            }
            catch (Exception ex)
            {
                return Result<List<Domain.Entities.Location>>.Failure($"Failed to retrieve active locations: {ex.Message}");
            }
        }

        public async Task<Result<Domain.Entities.Location>> CreateAsync(Domain.Entities.Location location, CancellationToken cancellationToken = default)
        {
            try
            {
                var created = await _innerRepository.AddAsync(location, cancellationToken);
                return Result<Domain.Entities.Location>.Success(created);
            }
            catch (Exception ex)
            {
                return Result<Domain.Entities.Location>.Failure($"Failed to create location: {ex.Message}");
            }
        }

        public async Task<Result<Domain.Entities.Location>> UpdateAsync(Domain.Entities.Location location, CancellationToken cancellationToken = default)
        {
            try
            {
                await _innerRepository.UpdateAsync(location, cancellationToken);
                return Result<Domain.Entities.Location>.Success(location);
            }
            catch (Exception ex)
            {
                return Result<Domain.Entities.Location>.Failure($"Failed to update location: {ex.Message}");
            }
        }

        public async Task<Result<bool>> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                var location = await _innerRepository.GetByIdAsync(id, cancellationToken);
                if (location == null)
                {
                    return Result<bool>.Failure($"Location with ID {id} not found");
                }

                await _innerRepository.DeleteAsync(location, cancellationToken);
                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure($"Failed to delete location: {ex.Message}");
            }
        }

        public async Task<Result<Domain.Entities.Location>> GetByTitleAsync(string title, CancellationToken cancellationToken = default)
        {
            try
            {
                var location = await _innerRepository.GetByTitleAsync(title, cancellationToken);
                return location != null
                    ? Result<Domain.Entities.Location>.Success(location)
                    : Result<Domain.Entities.Location>.Failure($"Location with title '{title}' not found");
            }
            catch (Exception ex)
            {
                return Result<Domain.Entities.Location>.Failure($"Failed to retrieve location by title: {ex.Message}");
            }
        }

        public async Task<Result<List<Domain.Entities.Location>>> GetNearbyAsync(double latitude, double longitude, double distanceKm, CancellationToken cancellationToken = default)
        {
            try
            {
                var locations = await _innerRepository.GetNearbyAsync(latitude, longitude, distanceKm, cancellationToken);
                return Result<List<Domain.Entities.Location>>.Success(locations.ToList());
            }
            catch (Exception ex)
            {
                return Result<List<Domain.Entities.Location>>.Failure($"Failed to retrieve nearby locations: {ex.Message}");
            }
        }

        public async Task<Result<PagedList<Domain.Entities.Location>>> GetPagedAsync(int pageNumber, int pageSize, string? searchTerm = null, bool includeDeleted = false, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _innerRepository.GetPagedAsync(pageNumber, pageSize, searchTerm, includeDeleted, cancellationToken);
                return Result<PagedList<Domain.Entities.Location>>.Success(result);
            }
            catch (Exception ex)
            {
                return Result<PagedList<Domain.Entities.Location>>.Failure($"Failed to retrieve paged locations: {ex.Message}");
            }
        }

        // ===== SQLITE-OPTIMIZED METHODS =====

        public async Task<Result<PagedList<T>>> GetPagedProjectedAsync<T>(int pageNumber, int pageSize, string selectColumns, string? whereClause = null, Dictionary<string, object>? parameters = null, string? orderBy = null, CancellationToken cancellationToken = default) where T : class, new()
        {
            try
            {
                var result = await _innerRepository.GetPagedProjectedAsync<T>(pageNumber, pageSize, selectColumns, whereClause, parameters, orderBy, cancellationToken);
                return Result<PagedList<T>>.Success(result);
            }
            catch (Exception ex)
            {
                return Result<PagedList<T>>.Failure($"Failed to retrieve paged projected locations: {ex.Message}");
            }
        }

        public async Task<Result<IReadOnlyList<T>>> GetActiveProjectedAsync<T>(string selectColumns, string? additionalWhere = null, Dictionary<string, object>? parameters = null, CancellationToken cancellationToken = default) where T : class, new()
        {
            try
            {
                var result = await _innerRepository.GetActiveProjectedAsync<T>(selectColumns, additionalWhere, parameters, cancellationToken);
                return Result<IReadOnlyList<T>>.Success(result);
            }
            catch (Exception ex)
            {
                return Result<IReadOnlyList<T>>.Failure($"Failed to retrieve active projected locations: {ex.Message}");
            }
        }

        public async Task<Result<IReadOnlyList<T>>> GetNearbyProjectedAsync<T>(double latitude, double longitude, double distanceKm, string selectColumns, CancellationToken cancellationToken = default) where T : class, new()
        {
            try
            {
                var result = await _innerRepository.GetNearbyProjectedAsync<T>(latitude, longitude, distanceKm, selectColumns, cancellationToken);
                return Result<IReadOnlyList<T>>.Success(result);
            }
            catch (Exception ex)
            {
                return Result<IReadOnlyList<T>>.Failure($"Failed to retrieve nearby projected locations: {ex.Message}");
            }
        }

        public async Task<Result<T?>> GetByIdProjectedAsync<T>(int id, string selectColumns, CancellationToken cancellationToken = default) where T : class, new()
        {
            try
            {
                var result = await _innerRepository.GetByIdProjectedAsync<T>(id, selectColumns, cancellationToken);
                return Result<T?>.Success(result);
            }
            catch (Exception ex)
            {
                return Result<T?>.Failure($"Failed to retrieve projected location by ID: {ex.Message}");
            }
        }

        // ===== SPECIFICATION PATTERN METHODS =====

        public async Task<Result<IReadOnlyList<Domain.Entities.Location>>> GetBySpecificationAsync(ISqliteSpecification<Domain.Entities.Location> specification, CancellationToken cancellationToken = default)
        {
            try
            {
                var persistenceSpec = new SpecificationAdapter(specification);
                var result = await _innerRepository.GetBySpecificationAsync(persistenceSpec, cancellationToken);
                return Result<IReadOnlyList<Domain.Entities.Location>>.Success(result);
            }
            catch (Exception ex)
            {
                return Result<IReadOnlyList<Domain.Entities.Location>>.Failure($"Failed to retrieve locations by specification: {ex.Message}");
            }
        }

        public async Task<Result<PagedList<T>>> GetPagedBySpecificationAsync<T>(ISqliteSpecification<Domain.Entities.Location> specification, int pageNumber, int pageSize, string selectColumns, CancellationToken cancellationToken = default) where T : class, new()
        {
            try
            {
                var persistenceSpec = new SpecificationAdapter(specification);
                var result = await _innerRepository.GetPagedBySpecificationAsync<T>(persistenceSpec, pageNumber, pageSize, selectColumns, cancellationToken);
                return Result<PagedList<T>>.Success(result);
            }
            catch (Exception ex)
            {
                return Result<PagedList<T>>.Failure($"Failed to retrieve paged locations by specification: {ex.Message}");
            }
        }

        // ===== BULK OPERATIONS =====

        public async Task<Result<IReadOnlyList<Domain.Entities.Location>>> CreateBulkAsync(IEnumerable<Domain.Entities.Location> locations, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _innerRepository.CreateBulkAsync(locations, cancellationToken);
                return Result<IReadOnlyList<Domain.Entities.Location>>.Success(result);
            }
            catch (Exception ex)
            {
                return Result<IReadOnlyList<Domain.Entities.Location>>.Failure($"Failed to bulk create locations: {ex.Message}");
            }
        }

        public async Task<Result<int>> UpdateBulkAsync(IEnumerable<Domain.Entities.Location> locations, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _innerRepository.UpdateBulkAsync(locations, cancellationToken);
                return Result<int>.Success(result);
            }
            catch (Exception ex)
            {
                return Result<int>.Failure($"Failed to bulk update locations: {ex.Message}");
            }
        }

        // ===== COUNT AND EXISTS METHODS =====

        public async Task<Result<int>> CountAsync(string? whereClause = null, Dictionary<string, object>? parameters = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _innerRepository.CountAsync(whereClause, parameters, cancellationToken);
                return Result<int>.Success(result);
            }
            catch (Exception ex)
            {
                return Result<int>.Failure($"Failed to count locations: {ex.Message}");
            }
        }

        public async Task<Result<bool>> ExistsAsync(string whereClause, Dictionary<string, object> parameters, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _innerRepository.ExistsAsync(whereClause, parameters, cancellationToken);
                return Result<bool>.Success(result);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure($"Failed to check location existence: {ex.Message}");
            }
        }

        public async Task<Result<bool>> ExistsByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _innerRepository.ExistsByIdAsync(id, cancellationToken);
                return Result<bool>.Success(result);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure($"Failed to check location existence by ID: {ex.Message}");
            }
        }

        // ===== RAW SQL EXECUTION =====

        public async Task<Result<IReadOnlyList<T>>> ExecuteQueryAsync<T>(string sql, Dictionary<string, object>? parameters = null, CancellationToken cancellationToken = default) where T : class, new()
        {
            try
            {
                var result = await _innerRepository.ExecuteQueryAsync<T>(sql, parameters, cancellationToken);
                return Result<IReadOnlyList<T>>.Success(result);
            }
            catch (Exception ex)
            {
                return Result<IReadOnlyList<T>>.Failure($"Failed to execute query: {ex.Message}");
            }
        }

        public async Task<Result<int>> ExecuteCommandAsync(string sql, Dictionary<string, object>? parameters = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _innerRepository.ExecuteCommandAsync(sql, parameters, cancellationToken);
                return Result<int>.Success(result);
            }
            catch (Exception ex)
            {
                return Result<int>.Failure($"Failed to execute command: {ex.Message}");
            }
        }
    }

    // ===== SPECIFICATION ADAPTER =====

    /// <summary>
    /// Adapter to convert from Application ISqliteSpecification to Persistence ISqliteSpecification
    /// </summary>
    internal class SpecificationAdapter : Location.Core.Application.Common.Interfaces.Persistence.ISqliteSpecification<Domain.Entities.Location>
    {
        private readonly ISqliteSpecification<Domain.Entities.Location> _applicationSpec;

        public SpecificationAdapter(ISqliteSpecification<Domain.Entities.Location> applicationSpec)
        {
            _applicationSpec = applicationSpec ?? throw new ArgumentNullException(nameof(applicationSpec));
        }

        public string WhereClause => _applicationSpec.WhereClause;
        public Dictionary<string, object> Parameters => _applicationSpec.Parameters;
        public string? OrderBy => _applicationSpec.OrderBy;
        public int? Take => _applicationSpec.Take;
        public int? Skip => _applicationSpec.Skip;
        public string? Joins => _applicationSpec.Joins;
    }
}