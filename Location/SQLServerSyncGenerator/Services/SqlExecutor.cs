using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Text;

namespace SQLServerSyncGenerator.Services;

public class SqlExecutor
{
    private readonly ILogger<SqlExecutor> _logger;
    private readonly ConnectionStringBuilder _connectionBuilder;

    public SqlExecutor(ILogger<SqlExecutor> logger, ConnectionStringBuilder connectionBuilder)
    {
        _logger = logger;
        _connectionBuilder = connectionBuilder;
    }

    public async Task ExecuteDDLBatchAsync(string connectionString, List<string> ddlStatements)
    {
        if (!ddlStatements.Any())
        {
            _logger.LogInformation("No DDL statements to execute");
            return;
        }

        _logger.LogInformation("Executing {Count} DDL statements", ddlStatements.Count);

        var successCount = 0;
        var errorCount = 0;

        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        foreach (var statement in ddlStatements)
        {
            try
            {
                _logger.LogDebug("Executing DDL: {Statement}", TruncateForLogging(statement));

                using var command = new SqlCommand(statement, connection);
                command.CommandTimeout = 300; // 5 minutes for DDL operations

                var rowsAffected = await command.ExecuteNonQueryAsync();
                successCount++;

                _logger.LogDebug("DDL executed successfully, rows affected: {RowsAffected}", rowsAffected);
            }
            catch (SqlException ex)
            {
                errorCount++;
                _logger.LogError(ex, "Failed to execute DDL statement: {Statement}", TruncateForLogging(statement));

                // Decide whether to continue or fail fast
                if (IsCriticalError(ex))
                {
                    _logger.LogError("Critical SQL error encountered, aborting execution");
                    throw;
                }

                _logger.LogWarning("Non-critical error, continuing with remaining statements");
            }
            catch (Exception ex)
            {
                errorCount++;
                _logger.LogError(ex, "Unexpected error executing DDL statement: {Statement}", TruncateForLogging(statement));
                throw;
            }
        }

        _logger.LogInformation("DDL batch execution completed: {SuccessCount} successful, {ErrorCount} errors",
            successCount, errorCount);

        if (errorCount > 0 && successCount == 0)
        {
            throw new InvalidOperationException($"All {errorCount} DDL statements failed to execute");
        }
    }

    public async Task CreateDatabaseBackupAsync(string connectionString, string databaseName, string backupName)
    {
        _logger.LogInformation("Creating database backup: {DatabaseName} -> {BackupName}", databaseName, backupName);

        try
        {
            // Use admin connection string (connects to master database)
            var adminConnectionString = await GetAdminConnectionStringAsync(connectionString);

            using var connection = new SqlConnection(adminConnectionString);
            await connection.OpenAsync();

            // Create database copy using CREATE DATABASE ... AS COPY OF
            var createCopyCommand = $@"
                IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = '{backupName}')
                BEGIN
                    CREATE DATABASE [{backupName}] AS COPY OF [{databaseName}]
                END";

            using var command = new SqlCommand(createCopyCommand, connection);
            command.CommandTimeout = 1800; // 30 minutes for database copy operations

            await command.ExecuteNonQueryAsync();

            _logger.LogInformation("Database backup created successfully: {BackupName}", backupName);
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Failed to create database backup: {DatabaseName} -> {BackupName}", databaseName, backupName);
            throw new InvalidOperationException($"Failed to create database backup: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating database backup: {DatabaseName} -> {BackupName}", databaseName, backupName);
            throw;
        }
    }

    public async Task DeleteDatabaseBackupAsync(string connectionString, string backupName)
    {
        _logger.LogInformation("Deleting database backup: {BackupName}", backupName);

        try
        {
            var adminConnectionString = await GetAdminConnectionStringAsync(connectionString);

            using var connection = new SqlConnection(adminConnectionString);
            await connection.OpenAsync();

            // Drop the backup database
            var dropCommand = $@"
                IF EXISTS (SELECT name FROM sys.databases WHERE name = '{backupName}')
                BEGIN
                    ALTER DATABASE [{backupName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                    DROP DATABASE [{backupName}];
                END";

            using var command = new SqlCommand(dropCommand, connection);
            command.CommandTimeout = 300; // 5 minutes for database drop

            await command.ExecuteNonQueryAsync();

            _logger.LogInformation("Database backup deleted successfully: {BackupName}", backupName);
        }
        catch (SqlException ex)
        {
            _logger.LogWarning(ex, "Failed to delete database backup (non-critical): {BackupName}", backupName);
            // Don't throw - backup cleanup failure is not critical
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unexpected error deleting database backup (non-critical): {BackupName}", backupName);
            // Don't throw - backup cleanup failure is not critical
        }
    }

    public async Task<bool> DatabaseExistsAsync(string connectionString, string databaseName)
    {
        try
        {
            var adminConnectionString = await GetAdminConnectionStringAsync(connectionString);

            using var connection = new SqlConnection(adminConnectionString);
            await connection.OpenAsync();

            var checkCommand = "SELECT COUNT(*) FROM sys.databases WHERE name = @databaseName";
            using var command = new SqlCommand(checkCommand, connection);
            command.Parameters.AddWithValue("@databaseName", databaseName);

            var count = (int)await command.ExecuteScalarAsync();
            return count > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if database exists: {DatabaseName}", databaseName);
            return false;
        }
    }

    public async Task<List<string>> GetExistingTablesAsync(string connectionString, string schemaName)
    {
        var tables = new List<string>();

        try
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            var query = @"
                SELECT t.name 
                FROM sys.tables t 
                INNER JOIN sys.schemas s ON t.schema_id = s.schema_id 
                WHERE s.name = @schemaName
                ORDER BY t.name";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@schemaName", schemaName);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                tables.Add(reader.GetString(0));
            }

            _logger.LogDebug("Found {Count} existing tables in schema {Schema}: {Tables}",
                tables.Count, schemaName, string.Join(", ", tables));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get existing tables for schema: {Schema}", schemaName);
        }

        return tables;
    }

    public async Task<bool> TableExistsAsync(string connectionString, string schemaName, string tableName)
    {
        try
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            var checkCommand = @"
                SELECT COUNT(*) 
                FROM sys.tables t 
                INNER JOIN sys.schemas s ON t.schema_id = s.schema_id 
                WHERE s.name = @schemaName AND t.name = @tableName";

            using var command = new SqlCommand(checkCommand, connection);
            command.Parameters.AddWithValue("@schemaName", schemaName);
            command.Parameters.AddWithValue("@tableName", tableName);

            var count = (int)await command.ExecuteScalarAsync();
            return count > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if table exists: {Schema}.{Table}", schemaName, tableName);
            return false;
        }
    }

    public async Task<bool> TestConnectionAsync(string connectionString)
    {
        try
        {
            _logger.LogDebug("Testing SQL Server connection");

            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand("SELECT @@VERSION", connection);
            var version = await command.ExecuteScalarAsync();

            _logger.LogDebug("Connection test successful. SQL Server version: {Version}",
                version?.ToString()?.Substring(0, Math.Min(100, version.ToString()?.Length ?? 0)));

            return true;
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "SQL Server connection test failed: {Message}", ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connection test failed: {Message}", ex.Message);
            return false;
        }
    }

    private async Task<string> GetAdminConnectionStringAsync(string regularConnectionString)
    {
        var builder = new SqlConnectionStringBuilder(regularConnectionString);
        builder.InitialCatalog = "master";
        builder.CommandTimeout = 600; // 10 minutes for admin operations
        return builder.ConnectionString;
    }

    private bool IsCriticalError(SqlException ex)
    {
        // Define critical errors that should stop execution
        return ex.Number switch
        {
            2 => true,      // Cannot open database
            18456 => true,  // Login failed
            4060 => true,   // Invalid database name
            18461 => true,  // Login failed for user
            _ => false
        };
    }

    private string TruncateForLogging(string statement)
    {
        const int maxLength = 200;
        if (statement.Length <= maxLength)
            return statement;

        return statement.Substring(0, maxLength) + "...";
    }

    public async Task ExecuteSingleStatementAsync(string connectionString, string statement)
    {
        _logger.LogDebug("Executing single SQL statement");

        try
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(statement, connection);
            command.CommandTimeout = 300;

            var rowsAffected = await command.ExecuteNonQueryAsync();

            _logger.LogDebug("Statement executed successfully, rows affected: {RowsAffected}", rowsAffected);
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Failed to execute SQL statement: {Statement}", TruncateForLogging(statement));
            throw;
        }
    }

    public async Task<T?> ExecuteScalarAsync<T>(string connectionString, string query)
    {
        try
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(query, connection);
            command.CommandTimeout = 30;

            var result = await command.ExecuteScalarAsync();

            if (result == null || result == DBNull.Value)
                return default(T);

            return (T)Convert.ChangeType(result, typeof(T));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute scalar query: {Query}", TruncateForLogging(query));
            throw;
        }
    }
}