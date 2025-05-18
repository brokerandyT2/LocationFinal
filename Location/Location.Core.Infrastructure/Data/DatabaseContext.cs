using Location.Core.Domain.Common;
using Location.Core.Domain.Entities;
using Location.Core.Infrastructure.Data.Entities;
using Microsoft.Extensions.Logging;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
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
    }

    public class DatabaseContext : IDatabaseContext
    {
        private readonly ILogger<DatabaseContext> _logger;
        private readonly SQLiteAsyncConnection _connection;
        private readonly string _databasePath;
        private bool _isInitialized = false;

        // Configuration constants
        private const string DATABASE_NAME = "locations.db";
        private const int BUSY_TIMEOUT_MS = 3000;

        public DatabaseContext(ILogger<DatabaseContext> logger, string? databasePath = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Use provided path or default to app data directory
            _databasePath = databasePath ?? System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                DATABASE_NAME);

            var options = new SQLiteConnectionString(
                _databasePath,
                SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache,
                storeDateTimeAsTicks: true);

            _connection = new SQLiteAsyncConnection(options);
        }

        public async Task InitializeDatabaseAsync()
        {
            if (_isInitialized) return;

            try
            {
                //_logger.LogInformation("Initializing database at {DatabasePath}", _databasePath);

                // Enable foreign keys
                await _connection.ExecuteAsync("PRAGMA foreign_keys = ON");
               // _logger.LogDebug("Foreign key constraints enabled");

                // Create tables
                await _connection.CreateTableAsync<LocationEntity>();
                await _connection.CreateTableAsync<WeatherEntity>();
                await _connection.CreateTableAsync<WeatherForecastEntity>();
                await _connection.CreateTableAsync<TipTypeEntity>();
                await _connection.CreateTableAsync<TipEntity>();
                await _connection.CreateTableAsync<SettingEntity>();
                await _connection.CreateTableAsync<Log>();

                // Create indexes for better performance
                await CreateIndexesAsync();

                _isInitialized = true;
                _logger.LogInformation("Database initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize database");
                throw new InvalidOperationException("Database initialization failed", ex);
            }
        }

        private async Task CreateIndexesAsync()
        {
            try
            {
                // Index for location coordinates
                await _connection.ExecuteAsync(
                    "CREATE INDEX IF NOT EXISTS idx_location_coords ON LocationEntity (Latitude, Longitude)");

                // Index for weather location lookup
                await _connection.ExecuteAsync(
                    "CREATE INDEX IF NOT EXISTS idx_weather_location ON WeatherEntity (LocationId)");

                // Index for weather forecast lookup
                await _connection.ExecuteAsync(
                    "CREATE INDEX IF NOT EXISTS idx_weather_forecast ON WeatherForecastEntity (WeatherId)");

                // Index for tips by type
                await _connection.ExecuteAsync(
                    "CREATE INDEX IF NOT EXISTS idx_tip_type ON TipEntity (TipTypeId)");

                // Index for settings by key
                await _connection.ExecuteAsync(
                    "CREATE UNIQUE INDEX IF NOT EXISTS idx_setting_key ON SettingEntity (Key)");

                // Index for logs by timestamp
                await _connection.ExecuteAsync(
                    "CREATE INDEX IF NOT EXISTS idx_log_timestamp ON Log (Timestamp)");

                _logger.LogDebug("Database indexes created");
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
                throw new InvalidOperationException("Database not initialized. Call InitializeDatabaseAsync first.");
            }
            return _connection;
        }

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
                EnsureInitializedAsync().GetAwaiter().GetResult();
            }
            return _connection.Table<T>();
        }

        #region Transaction Support

        public async Task BeginTransactionAsync()
        {
            await EnsureInitializedAsync();
            await _connection.ExecuteAsync("BEGIN TRANSACTION");
            _logger.LogDebug("Transaction started");
        }

        public async Task CommitTransactionAsync()
        {
            await _connection.ExecuteAsync("COMMIT");
            _logger.LogDebug("Transaction committed");
        }

        public async Task RollbackTransactionAsync()
        {
            await _connection.ExecuteAsync("ROLLBACK");
            _logger.LogDebug("Transaction rolled back");
        }

        #endregion

        private async Task EnsureInitializedAsync()
        {
            if (!_isInitialized)
            {
                await InitializeDatabaseAsync();
            }
        }

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
                    _connection?.CloseAsync().Wait(TimeSpan.FromSeconds(5));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error closing database connection");
                }
            }

            _disposed = true;
        }

        #endregion
    }
}