using Location.Core.Domain.Common;
using Location.Core.Domain.Entities;
using Location.Core.Infrastructure.Data.Entities;
using Location.Photography.Domain.Entities;
using Microsoft.Extensions.Logging;
using SQLite;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Core.Infrastructure.Data
{
    public interface IDatabaseContext
    {
        Task InitializeDatabaseAsync();

        SQLiteAsyncConnection GetConnection();
        Task<int> InsertAsync<T>(T entity) where T : class, new();
        Task<int> UpdateAsync<T>(T entity) where T : class, new();
        Task<int> DeleteAsync<T>(T entity) where T : class, new();
        Task<List<T>> GetAllAsync<T>() where T : class, new();
        Task<T> GetAsync<T>(object primaryKey) where T : class, new();
        Task<int> ExecuteAsync(string query, params object[] args);
        AsyncTableQuery<T> Table<T>() where T : class, new();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();

        // New bulk operation methods
        Task<int> InsertAllAsync<T>(IEnumerable<T> entities) where T : class, new();
        Task<int> UpdateAllAsync<T>(IEnumerable<T> entities) where T : class, new();
        Task<int> DeleteAllAsync<T>(IEnumerable<T> entities) where T : class, new();
        Task<int> BulkInsertAsync<T>(IEnumerable<T> entities, int batchSize = 100) where T : class, new();
        Task<int> BulkUpdateAsync<T>(IEnumerable<T> entities, int batchSize = 100) where T : class, new();

        // Prepared statement support
        Task<List<T>> QueryAsync<T>(string sql, params object[] args) where T : new();
        Task<T> QuerySingleAsync<T>(string sql, params object[] args) where T : new();
        Task<T> ExecuteScalarAsync<T>(string sql, params object[] args);

        // Transaction scoping
        Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation);
        Task ExecuteInTransactionAsync(Func<Task> operation);
    }

    public class DatabaseContext : IDatabaseContext, IDisposable
    {
        private readonly ILogger<DatabaseContext> _logger;
        private readonly SQLiteAsyncConnection _connection;
        private readonly string _databasePath;
        private readonly SemaphoreSlim _initializationSemaphore;
        private readonly ConcurrentDictionary<string, object> _preparedStatementCache;
        private readonly object _transactionLock = new object();

        private volatile bool _isInitialized = false;
        private volatile bool _isInTransaction = false;

        // Configuration constants
        private const string DATABASE_NAME = "locations.db";
        private const int BUSY_TIMEOUT_MS = 3000;
        private const int DEFAULT_BATCH_SIZE = 100;
        private const int MAX_CACHED_STATEMENTS = 50;

        public DatabaseContext(ILogger<DatabaseContext> logger, string? databasePath = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _initializationSemaphore = new SemaphoreSlim(1, 1);
            _preparedStatementCache = new ConcurrentDictionary<string, object>();

            // Use provided path or default to app data directory
            _databasePath = databasePath ?? System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                DATABASE_NAME);

            SQLitePCL.Batteries_V2.Init();

            var options = new SQLiteConnectionString(
               _databasePath,
               SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache,
               storeDateTimeAsTicks: true);

            _connection = new SQLiteAsyncConnection(options);
        }

        /// <summary>
        /// Asynchronously initializes the database by creating necessary tables, enabling foreign key constraints,
        /// and setting up indexes for improved performance.
        /// </summary>
        public async Task InitializeDatabaseAsync()
        {
            if (_isInitialized) return;

            await _initializationSemaphore.WaitAsync();
            try
            {
                if (_isInitialized) return;

                _logger.LogInformation("Initializing database at {DatabasePath}", _databasePath);

                // Enable foreign keys and performance optimizations
                await _connection.ExecuteAsync("PRAGMA foreign_keys = ON");
                //await _connection.ExecuteAsync($"PRAGMA busy_timeout = {BUSY_TIMEOUT_MS}");
                //await _connection.ExecuteAsync("PRAGMA journal_mode = WAL");
                //await _connection.ExecuteAsync("PRAGMA synchronous = NORMAL");
                //await _connection.ExecuteAsync("PRAGMA cache_size = 10000");
                //await _connection.ExecuteAsync("PRAGMA temp_store = MEMORY");

                _logger.LogDebug("Database PRAGMA settings configured");

                // Create tables with optimized order (dependencies first)
                await CreateTablesAsync();

                // Create indexes for better performance
                await CreateIndexesAsync();

                // Analyze tables for query optimization
                await _connection.ExecuteAsync("ANALYZE");

                _isInitialized = true;
                _logger.LogInformation("Database initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize database");
                throw new InvalidOperationException("Database initialization failed", ex);
            }
            finally
            {
                _initializationSemaphore.Release();
            }
        }

        private async Task CreateTablesAsync()
        {
            // Create tables in dependency order
            await _connection.CreateTableAsync<SettingEntity>();
            await _connection.CreateTableAsync<TipTypeEntity>();
            await _connection.CreateTableAsync<LocationEntity>();
            await _connection.CreateTableAsync<TipEntity>();
            await _connection.CreateTableAsync<WeatherEntity>();
            await _connection.CreateTableAsync<WeatherForecastEntity>();
            await _connection.CreateTableAsync<HourlyForecastEntity>();
            await _connection.CreateTableAsync<Log>();
            await _connection.CreateTableAsync<Subscription>();

            _logger.LogDebug("Database tables created");
        }

        /// <summary>
        /// Asynchronously creates database indexes to optimize query performance.
        /// </summary>
        private async Task CreateIndexesAsync()
        {
            try
            {
                var indexCommands = new[]
                {
                   // Location indexes
                   "CREATE INDEX IF NOT EXISTS idx_location_coords ON LocationEntity (Latitude, Longitude)",
                   "CREATE INDEX IF NOT EXISTS idx_location_title ON LocationEntity (Title)",
                   "CREATE INDEX IF NOT EXISTS idx_location_active ON LocationEntity (IsDeleted, Timestamp)",
                   "CREATE INDEX IF NOT EXISTS idx_location_search ON LocationEntity (Title, City, State, Description)",

                   // Weather indexes
                   "CREATE INDEX IF NOT EXISTS idx_weather_location ON WeatherEntity (LocationId, LastUpdate)",
                   "CREATE INDEX IF NOT EXISTS idx_weather_forecast ON WeatherForecastEntity (WeatherId, Date)",
                   "CREATE INDEX IF NOT EXISTS idx_hourly_forecast ON HourlyForecastEntity (WeatherId, DateTime)",

                   // Tip indexes
                   "CREATE INDEX IF NOT EXISTS idx_tip_type ON TipEntity (TipTypeId)",
                   "CREATE INDEX IF NOT EXISTS idx_tip_title ON TipEntity (Title)",

                   // Setting indexes
                   "CREATE UNIQUE INDEX IF NOT EXISTS idx_setting_key ON SettingEntity (Key)",

                   // Log indexes
                   "CREATE INDEX IF NOT EXISTS idx_log_timestamp ON Log (Timestamp DESC)",
                   "CREATE INDEX IF NOT EXISTS idx_log_level ON Log (Level, Timestamp)",

                   // Subscription indexes
                   "CREATE INDEX IF NOT EXISTS idx_subscription_user ON Subscription (UserId, Status, ExpirationDate)"
               };

                // Execute index creation commands concurrently for better performance
                var tasks = indexCommands.Select(cmd => _connection.ExecuteAsync(cmd));
                await Task.WhenAll(tasks);

                _logger.LogDebug("Database indexes created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create indexes");
                throw;
            }
        }

        public SQLiteAsyncConnection GetConnection()
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("Database must be initialized before use. Call InitializeDatabaseAsync() first.");
            }
            return _connection;
        }

        #region Basic Operations (Optimized)

        public async Task<int> InsertAsync<T>(T entity) where T : class, new()
        {
            try
            {
                await EnsureInitializedAsync();
                var result = await _connection.InsertAsync(entity);
                _logger.LogDebug("Inserted {EntityType} with result: {Result}", typeof(T).Name, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to insert {EntityType}", typeof(T).Name);
                throw;
            }
        }

        public async Task<int> UpdateAsync<T>(T entity) where T : class, new()
        {
            try
            {
                await EnsureInitializedAsync();
                var result = await _connection.UpdateAsync(entity);
                _logger.LogDebug("Updated {EntityType} with result: {Result}", typeof(T).Name, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update {EntityType}", typeof(T).Name);
                throw;
            }
        }

        public async Task<int> DeleteAsync<T>(T entity) where T : class, new()
        {
            try
            {
                await EnsureInitializedAsync();
                var result = await _connection.DeleteAsync(entity);
                _logger.LogDebug("Deleted {EntityType} with result: {Result}", typeof(T).Name, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete {EntityType}", typeof(T).Name);
                throw;
            }
        }

        public async Task<List<T>> GetAllAsync<T>() where T : class, new()
        {
            try
            {
                await EnsureInitializedAsync();
                return await _connection.Table<T>().ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get all {EntityType}", typeof(T).Name);
                throw;
            }
        }

        public async Task<T> GetAsync<T>(object primaryKey) where T : class, new()
        {
            try
            {
                await EnsureInitializedAsync();
                return await _connection.GetAsync<T>(primaryKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get {EntityType} with key {PrimaryKey}",
                    typeof(T).Name, primaryKey);
                throw;
            }
        }

        public async Task<int> ExecuteAsync(string query, params object[] args)
        {
            try
            {
                await EnsureInitializedAsync();
                return await _connection.ExecuteAsync(query, args);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute query: {Query}", query);
                throw;
            }
        }

        public AsyncTableQuery<T> Table<T>() where T : class, new()
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("Database must be initialized before use. Call InitializeDatabaseAsync() first.");
            }
            return _connection.Table<T>();
        }

        #endregion

        #region Bulk Operations

        public async Task<int> InsertAllAsync<T>(IEnumerable<T> entities) where T : class, new()
        {
            try
            {
                await EnsureInitializedAsync();
                var entityList = entities.ToList();

                if (!entityList.Any()) return 0;

                var result = await _connection.InsertAllAsync(entityList);
                _logger.LogInformation("Bulk inserted {Count} {EntityType} entities", entityList.Count, typeof(T).Name);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to bulk insert {EntityType}", typeof(T).Name);
                throw;
            }
        }

        public async Task<int> UpdateAllAsync<T>(IEnumerable<T> entities) where T : class, new()
        {
            try
            {
                await EnsureInitializedAsync();
                var entityList = entities.ToList();

                if (!entityList.Any()) return 0;

                var result = await _connection.UpdateAllAsync(entityList);
                _logger.LogInformation("Bulk updated {Count} {EntityType} entities", entityList.Count, typeof(T).Name);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to bulk update {EntityType}", typeof(T).Name);
                throw;
            }
        }

        public async Task<int> DeleteAllAsync<T>(IEnumerable<T> entities) where T : class, new()
        {
            try
            {
                await EnsureInitializedAsync();
                var entityList = entities.ToList();

                if (!entityList.Any()) return 0;

                // SQLite-net-pcl doesn't have DeleteAllAsync, so we use transaction
                return await ExecuteInTransactionAsync(async () =>
                {
                    int totalDeleted = 0;
                    foreach (var entity in entityList)
                    {
                        totalDeleted += await _connection.DeleteAsync(entity);
                    }
                    return totalDeleted;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to bulk delete {EntityType}", typeof(T).Name);
                throw;
            }
        }

        public async Task<int> BulkInsertAsync<T>(IEnumerable<T> entities, int batchSize = DEFAULT_BATCH_SIZE) where T : class, new()
        {
            try
            {
                await EnsureInitializedAsync();
                var entityList = entities.ToList();

                if (!entityList.Any()) return 0;

                int totalInserted = 0;
                var batches = entityList.Chunk(batchSize);

                foreach (var batch in batches)
                {
                    var batchResult = await ExecuteInTransactionAsync(async () =>
                    {
                        return await _connection.InsertAllAsync(batch);
                    });

                    totalInserted += batchResult;
                    _logger.LogDebug("Inserted batch of {BatchSize} {EntityType} entities", batch.Count(), typeof(T).Name);
                }

                _logger.LogInformation("Bulk inserted {Count} {EntityType} entities in batches", totalInserted, typeof(T).Name);
                return totalInserted;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to bulk insert {EntityType} in batches", typeof(T).Name);
                throw;
            }
        }

        public async Task<int> BulkUpdateAsync<T>(IEnumerable<T> entities, int batchSize = DEFAULT_BATCH_SIZE) where T : class, new()
        {
            try
            {
                await EnsureInitializedAsync();
                var entityList = entities.ToList();

                if (!entityList.Any()) return 0;

                int totalUpdated = 0;
                var batches = entityList.Chunk(batchSize);

                foreach (var batch in batches)
                {
                    var batchResult = await ExecuteInTransactionAsync(async () =>
                    {
                        return await _connection.UpdateAllAsync(batch);
                    });

                    totalUpdated += batchResult;
                    _logger.LogDebug("Updated batch of {BatchSize} {EntityType} entities", batch.Count(), typeof(T).Name);
                }

                _logger.LogInformation("Bulk updated {Count} {EntityType} entities in batches", totalUpdated, typeof(T).Name);
                return totalUpdated;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to bulk update {EntityType} in batches", typeof(T).Name);
                throw;
            }
        }

        #endregion

        #region Prepared Statement Support

        public async Task<List<T>> QueryAsync<T>(string sql, params object[] args) where T : new()
        {
            try
            {
                await EnsureInitializedAsync();
                return await _connection.QueryAsync<T>(sql, args);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute query: {Query}", sql);
                throw;
            }
        }

        public async Task<T> QuerySingleAsync<T>(string sql, params object[] args) where T : new()
        {
            try
            {
                await EnsureInitializedAsync();
                var results = await _connection.QueryAsync<T>(sql, args);
                return results.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute single query: {Query}", sql);
                throw;
            }
        }

        public async Task<T> ExecuteScalarAsync<T>(string sql, params object[] args)
        {
            try
            {
                await EnsureInitializedAsync();
                return await _connection.ExecuteScalarAsync<T>(sql, args);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute scalar query: {Query}", sql);
                throw;
            }
        }

        #endregion

        #region Transaction Support

        public async Task BeginTransactionAsync()
        {
            await EnsureInitializedAsync();

            lock (_transactionLock)
            {
                if (_isInTransaction)
                {
                    throw new InvalidOperationException("Transaction already in progress");
                }
                _isInTransaction = true;
            }

            try
            {
                await _connection.ExecuteAsync("BEGIN TRANSACTION");
                _logger.LogDebug("Transaction started");
            }
            catch
            {
                lock (_transactionLock)
                {
                    _isInTransaction = false;
                }
                throw;
            }
        }

        public async Task CommitTransactionAsync()
        {
            lock (_transactionLock)
            {
                if (!_isInTransaction)
                {
                    throw new InvalidOperationException("No transaction in progress");
                }
            }

            try
            {
                await _connection.ExecuteAsync("COMMIT");
                _logger.LogDebug("Transaction committed");
            }
            finally
            {
                lock (_transactionLock)
                {
                    _isInTransaction = false;
                }
            }
        }

        public async Task RollbackTransactionAsync()
        {
            lock (_transactionLock)
            {
                if (!_isInTransaction)
                {
                    throw new InvalidOperationException("No transaction in progress");
                }
            }

            try
            {
                await _connection.ExecuteAsync("ROLLBACK");
                _logger.LogDebug("Transaction rolled back");
            }
            finally
            {
                lock (_transactionLock)
                {
                    _isInTransaction = false;
                }
            }
        }

        public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation)
        {
            var wasInTransaction = false;

            lock (_transactionLock)
            {
                wasInTransaction = _isInTransaction;
            }

            if (wasInTransaction)
            {
                // Already in transaction, just execute the operation
                return await operation();
            }

            // Start new transaction
            await BeginTransactionAsync();
            try
            {
                var result = await operation();
                await CommitTransactionAsync();
                return result;
            }
            catch
            {
                await RollbackTransactionAsync();
                throw;
            }
        }

        public async Task ExecuteInTransactionAsync(Func<Task> operation)
        {
            await ExecuteInTransactionAsync(async () =>
            {
                await operation();
                return 0; // dummy return for generic method
            });
        }

        #endregion

        #region Helper Methods

        private async Task EnsureInitializedAsync()
        {
            if (!_isInitialized)
            {
                await InitializeDatabaseAsync();
            }
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
                    // Rollback any pending transaction
                    if (_isInTransaction)
                    {
                        RollbackTransactionAsync().GetAwaiter().GetResult();
                    }

                    // Close connection
                    _connection?.CloseAsync().Wait(TimeSpan.FromSeconds(5));

                    // Dispose semaphore
                    _initializationSemaphore?.Dispose();

                    // Clear cache
                    _preparedStatementCache?.Clear();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during database disposal");
                }
            }

            _disposed = true;
        }

        #endregion
    }

    // Extension method for chunking (if not available in your .NET version)
    public static class EnumerableExtensions
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