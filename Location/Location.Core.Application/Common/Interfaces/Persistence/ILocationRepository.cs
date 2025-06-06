using Location.Core.Application.Common.Models;

namespace Location.Core.Application.Common.Interfaces.Persistence
{
    /// <summary>
    /// SQLite-optimized repository interface for Location aggregate root with raw SQL projections
    /// </summary>
    public interface ILocationRepository
    {
        // ===== EXISTING METHODS (for backward compatibility) =====
        Task<Domain.Entities.Location?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<IEnumerable<Domain.Entities.Location>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<Domain.Entities.Location>> GetActiveAsync(CancellationToken cancellationToken = default);
        Task<Domain.Entities.Location> AddAsync(Domain.Entities.Location location, CancellationToken cancellationToken = default);
        Task UpdateAsync(Domain.Entities.Location setting, CancellationToken cancellationToken = default);
        Task DeleteAsync(Domain.Entities.Location setting, CancellationToken cancellationToken = default);
        Task<Domain.Entities.Location?> GetByTitleAsync(string title, CancellationToken cancellationToken = default);
        Task<IEnumerable<Domain.Entities.Location>> GetNearbyAsync(double latitude, double longitude, double distanceKm, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets paginated locations with database-level filtering and pagination
        /// </summary>
        Task<PagedList<Domain.Entities.Location>> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            bool includeDeleted = false,
            CancellationToken cancellationToken = default);

        // ===== SQLITE-OPTIMIZED PROJECTION METHODS =====

        /// <summary>
        /// Gets paginated locations with raw SQL projection (SQLite optimized)
        /// </summary>
        Task<PagedList<T>> GetPagedProjectedAsync<T>(
            int pageNumber,
            int pageSize,
            string selectColumns,
            string? whereClause = null,
            Dictionary<string, object>? parameters = null,
            string? orderBy = null,
            CancellationToken cancellationToken = default) where T : class, new();

        /// <summary>
        /// Gets active locations with specific column selection (SQLite optimized)
        /// </summary>
        Task<IReadOnlyList<T>> GetActiveProjectedAsync<T>(
            string selectColumns,
            string? additionalWhere = null,
            Dictionary<string, object>? parameters = null,
            CancellationToken cancellationToken = default) where T : class, new();

        /// <summary>
        /// Gets nearby locations with raw SQL and distance calculation
        /// </summary>
        Task<IReadOnlyList<T>> GetNearbyProjectedAsync<T>(
            double latitude,
            double longitude,
            double distanceKm,
            string selectColumns,
            CancellationToken cancellationToken = default) where T : class, new();

        /// <summary>
        /// Gets location by ID with specific column projection
        /// </summary>
        Task<T?> GetByIdProjectedAsync<T>(
            int id,
            string selectColumns,
            CancellationToken cancellationToken = default) where T : class, new();

        // ===== SQLITE SPECIFICATION PATTERN =====

        /// <summary>
        /// Gets locations using SQLite-compatible specification
        /// </summary>
        Task<IReadOnlyList<Domain.Entities.Location>> GetBySpecificationAsync(
            ISqliteSpecification<Domain.Entities.Location> specification,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets paginated locations using specification with projection
        /// </summary>
        Task<PagedList<T>> GetPagedBySpecificationAsync<T>(
            ISqliteSpecification<Domain.Entities.Location> specification,
            int pageNumber,
            int pageSize,
            string selectColumns,
            CancellationToken cancellationToken = default) where T : class, new();

        // ===== BULK OPERATIONS (SQLite optimized) =====

        /// <summary>
        /// Bulk inserts using SQLite transaction
        /// </summary>
        Task<IReadOnlyList<Domain.Entities.Location>> CreateBulkAsync(
            IEnumerable<Domain.Entities.Location> locations,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Bulk updates using SQLite batch operations
        /// </summary>
        Task<int> UpdateBulkAsync(
            IEnumerable<Domain.Entities.Location> locations,
            CancellationToken cancellationToken = default);

        // ===== COUNT AND EXISTS METHODS (optimized SQLite queries) =====

        /// <summary>
        /// Gets count with raw SQL (database-level count)
        /// </summary>
        Task<int> CountAsync(
            string? whereClause = null,
            Dictionary<string, object>? parameters = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks existence with optimized SQL
        /// </summary>
        Task<bool> ExistsAsync(
            string whereClause,
            Dictionary<string, object> parameters,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if location exists by ID (optimized exists query)
        /// </summary>
        Task<bool> ExistsByIdAsync(int id, CancellationToken cancellationToken = default);

        // ===== RAW SQL EXECUTION =====

        /// <summary>
        /// Executes raw SQL query and returns strongly typed results
        /// </summary>
        Task<IReadOnlyList<T>> ExecuteQueryAsync<T>(
            string sql,
            Dictionary<string, object>? parameters = null,
            CancellationToken cancellationToken = default) where T : class, new();

        /// <summary>
        /// Executes raw SQL command (INSERT, UPDATE, DELETE)
        /// </summary>
        Task<int> ExecuteCommandAsync(
            string sql,
            Dictionary<string, object>? parameters = null,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// SQLite-compatible specification interface
    /// </summary>
    public interface ISqliteSpecification<T>
    {
        /// <summary>
        /// WHERE clause for the SQL query
        /// </summary>
        string WhereClause { get; }

        /// <summary>
        /// Parameters for the WHERE clause
        /// </summary>
        Dictionary<string, object> Parameters { get; }

        /// <summary>
        /// ORDER BY clause
        /// </summary>
        string? OrderBy { get; }

        /// <summary>
        /// Number of records to take (LIMIT)
        /// </summary>
        int? Take { get; }

        /// <summary>
        /// Number of records to skip (OFFSET)
        /// </summary>
        int? Skip { get; }

        /// <summary>
        /// Additional JOIN clauses if needed
        /// </summary>
        string? Joins { get; }
    }

    /// <summary>
    /// Base specification for SQLite queries
    /// </summary>
    public abstract class BaseSqliteSpecification<T> : ISqliteSpecification<T>
    {
        public string WhereClause { get; protected set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; protected set; } = new();
        public string? OrderBy { get; protected set; }
        public int? Take { get; protected set; }
        public int? Skip { get; protected set; }
        public string? Joins { get; protected set; }

        protected void AddWhere(string clause, Dictionary<string, object>? parameters = null)
        {
            if (!string.IsNullOrEmpty(WhereClause))
            {
                WhereClause += " AND ";
            }
            WhereClause += clause;

            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    Parameters[param.Key] = param.Value;
                }
            }
        }

        protected void AddOrderBy(string orderBy)
        {
            OrderBy = orderBy;
        }

        protected void AddPaging(int skip, int take)
        {
            Skip = skip;
            Take = take;
        }

        protected void AddJoin(string join)
        {
            if (string.IsNullOrEmpty(Joins))
            {
                Joins = join;
            }
            else
            {
                Joins += " " + join;
            }
        }
    }

    /// <summary>
    /// Common specifications for Location queries
    /// </summary>
    public static class LocationSpecifications
    {
        public class ActiveLocationsSpec : BaseSqliteSpecification<Domain.Entities.Location>
        {
            public ActiveLocationsSpec()
            {
                AddWhere("IsDeleted = 0");
                AddOrderBy("Timestamp DESC");
            }
        }

        public class LocationsBySearchTermSpec : BaseSqliteSpecification<Domain.Entities.Location>
        {
            public LocationsBySearchTermSpec(string searchTerm, bool includeDeleted = false)
            {
                var parameters = new Dictionary<string, object> { { "@searchTerm", $"%{searchTerm}%" } };

                var whereClause = "(Title LIKE @searchTerm OR Description LIKE @searchTerm OR City LIKE @searchTerm OR State LIKE @searchTerm)";

                if (!includeDeleted)
                {
                    whereClause += " AND IsDeleted = 0";
                }

                AddWhere(whereClause, parameters);
                AddOrderBy("Timestamp DESC");
            }
        }

        public class NearbyLocationsSpec : BaseSqliteSpecification<Domain.Entities.Location>
        {
            public NearbyLocationsSpec(double latitude, double longitude, double distanceKm)
            {
                // For SQLite, we'll use a bounding box for initial filtering, then calculate exact distance
                var latRange = distanceKm / 111.0; // Rough conversion: 1 degree ≈ 111 km
                var lngRange = distanceKm / (111.0 * Math.Cos(latitude * Math.PI / 180.0));

                var parameters = new Dictionary<string, object>
                {
                    { "@minLat", latitude - latRange },
                    { "@maxLat", latitude + latRange },
                    { "@minLng", longitude - lngRange },
                    { "@maxLng", longitude + lngRange }
                };

                AddWhere("Latitude BETWEEN @minLat AND @maxLat AND Longitude BETWEEN @minLng AND @maxLng AND IsDeleted = 0", parameters);
                AddOrderBy("Timestamp DESC");
            }
        }
    }
}