using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Text;

namespace x3squaredcirecles.SQLSync.Generator.Services;

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
    // Add these methods to your existing SqlExecutor.cs class

    /// <summary>
    /// Restores a database from a backup copy (Azure SQL Database approach)
    /// </summary>
    public async Task RestoreDatabaseFromBackupAsync(string connectionString, string originalDatabase, string backupDatabase)
    {
        _logger.LogInformation("Restoring database {OriginalDatabase} from backup {BackupDatabase}", originalDatabase, backupDatabase);

        try
        {
            var adminConnectionString = await GetAdminConnectionStringAsync(connectionString);

            using var connection = new SqlConnection(adminConnectionString);
            await connection.OpenAsync();

            // Step 1: Set backup database to single user mode and rename original to temp
            var tempDatabaseName = $"{originalDatabase}_FailedDeploy_{DateTime.UtcNow:yyyyMMdd_HHmmss}";

            _logger.LogInformation("Step 1: Renaming failed database to {TempName}", tempDatabaseName);

            var renameFailedCommand = $@"
                IF EXISTS (SELECT name FROM sys.databases WHERE name = '{originalDatabase}')
                BEGIN
                    ALTER DATABASE [{originalDatabase}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                    ALTER DATABASE [{originalDatabase}] MODIFY NAME = [{tempDatabaseName}];
                END";

            using var command1 = new SqlCommand(renameFailedCommand, connection);
            command1.CommandTimeout = 600;
            await command1.ExecuteNonQueryAsync();

            // Step 2: Rename backup to original name
            _logger.LogInformation("Step 2: Restoring backup database to original name");

            var restoreCommand = $@"
                IF EXISTS (SELECT name FROM sys.databases WHERE name = '{backupDatabase}')
                BEGIN
                    ALTER DATABASE [{backupDatabase}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                    ALTER DATABASE [{backupDatabase}] MODIFY NAME = [{originalDatabase}];
                    ALTER DATABASE [{originalDatabase}] SET MULTI_USER;
                END";

            using var command2 = new SqlCommand(restoreCommand, connection);
            command2.CommandTimeout = 600;
            await command2.ExecuteNonQueryAsync();

            // Step 3: Clean up the failed database
            _logger.LogInformation("Step 3: Cleaning up failed deployment database");

            var cleanupCommand = $@"
                IF EXISTS (SELECT name FROM sys.databases WHERE name = '{tempDatabaseName}')
                BEGIN
                    ALTER DATABASE [{tempDatabaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                    DROP DATABASE [{tempDatabaseName}];
                END";

            using var command3 = new SqlCommand(cleanupCommand, connection);
            command3.CommandTimeout = 600;
            await command3.ExecuteNonQueryAsync();

            _logger.LogInformation("Database rollback completed successfully: {OriginalDatabase} restored from {BackupDatabase}",
                originalDatabase, backupDatabase);
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Failed to restore database from backup: {OriginalDatabase} <- {BackupDatabase}",
                originalDatabase, backupDatabase);
            throw new InvalidOperationException($"Database rollback failed: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during database restore: {OriginalDatabase} <- {BackupDatabase}",
                originalDatabase, backupDatabase);
            throw;
        }
    }

    /// <summary>
    /// Checks if a backup database exists
    /// </summary>
    public async Task<bool> BackupDatabaseExistsAsync(string connectionString, string backupDatabaseName)
    {
        try
        {
            var adminConnectionString = await GetAdminConnectionStringAsync(connectionString);

            using var connection = new SqlConnection(adminConnectionString);
            await connection.OpenAsync();

            var checkCommand = "SELECT COUNT(*) FROM sys.databases WHERE name = @backupName";
            using var command = new SqlCommand(checkCommand, connection);
            command.Parameters.AddWithValue("@backupName", backupDatabaseName);

            var count = (int)await command.ExecuteScalarAsync();
            var exists = count > 0;

            _logger.LogDebug("Backup database {BackupName} exists: {Exists}", backupDatabaseName, exists);
            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if backup database exists: {BackupName}", backupDatabaseName);
            return false;
        }
    }

    /// <summary>
    /// Creates a database backup using Azure SQL Database COPY operation
    /// Enhanced with better error handling and validation
    /// </summary>
    public async Task CreateDatabaseBackupAsync(string connectionString, string databaseName, string backupName)
    {
        _logger.LogInformation("Creating database backup: {DatabaseName} -> {BackupName}", databaseName, backupName);

        try
        {
            // Use admin connection string (connects to master database)
            var adminConnectionString = await GetAdminConnectionStringAsync(connectionString);

            using var connection = new SqlConnection(adminConnectionString);
            await connection.OpenAsync();

            // Validate source database exists
            if (!await DatabaseExistsAsync(connectionString, databaseName))
            {
                throw new InvalidOperationException($"Source database '{databaseName}' does not exist");
            }

            // Check if backup name already exists
            if (await DatabaseExistsAsync(connectionString, backupName))
            {
                _logger.LogWarning("Backup database {BackupName} already exists - deleting before creating new backup", backupName);
                await DeleteDatabaseBackupAsync(connectionString, backupName);
            }

            // Create database copy using CREATE DATABASE ... AS COPY OF
            var createCopyCommand = $@"
                CREATE DATABASE [{backupName}] AS COPY OF [{databaseName}]
                (SERVICE_OBJECTIVE = ELASTIC_POOL(name = [default]))"; // Adjust service objective as needed

            using var command = new SqlCommand(createCopyCommand, connection);
            command.CommandTimeout = 3600; // 60 minutes for large database copies

            await command.ExecuteNonQueryAsync();

            // Wait for copy operation to complete
            await WaitForDatabaseCopyCompletionAsync(connection, backupName);

            _logger.LogInformation("Database backup created successfully: {BackupName}", backupName);
        }
        catch (SqlException ex) when (ex.Number == 40852) // Database copy limit exceeded
        {
            _logger.LogError("Database copy limit exceeded. Cannot create backup: {BackupName}", backupName);
            throw new InvalidOperationException("Cannot create database backup: copy limit exceeded. Try again later.", ex);
        }
        catch (SqlException ex) when (ex.Number == 40847) // Database copy still in progress
        {
            _logger.LogError("Previous database copy still in progress. Cannot create backup: {BackupName}", backupName);
            throw new InvalidOperationException("Cannot create database backup: previous copy operation still in progress.", ex);
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

    /// <summary>
    /// Waits for database copy operation to complete
    /// </summary>
    private async Task WaitForDatabaseCopyCompletionAsync(SqlConnection connection, string databaseName)
    {
        _logger.LogInformation("Waiting for database copy operation to complete: {DatabaseName}", databaseName);

        var timeout = TimeSpan.FromMinutes(60); // 60 minute timeout
        var startTime = DateTime.UtcNow;
        var checkInterval = TimeSpan.FromSeconds(30);

        while (DateTime.UtcNow - startTime < timeout)
        {
            var query = @"
                SELECT state_desc, percent_complete 
                FROM sys.dm_database_copies 
                WHERE database_id = DB_ID(@databaseName)";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@databaseName", databaseName);

            using var reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                // No copy operation found - copy might be complete
                reader.Close();

                // Verify database exists and is online
                if (await DatabaseExistsAsync(connection.ConnectionString, databaseName))
                {
                    _logger.LogInformation("Database copy completed: {DatabaseName}", databaseName);
                    return;
                }
            }
            else
            {
                var state = reader.GetString(0);
                var percentComplete = reader.IsDBNull(1) ? 0 : reader.GetDecimal(1);

                _logger.LogInformation("Database copy progress: {DatabaseName} - {State} ({Percent:F1}%)",
                    databaseName, state, percentComplete);

                if (state.Equals("COMPLETED", StringComparison.OrdinalIgnoreCase))
                {
                    reader.Close();
                    _logger.LogInformation("Database copy completed: {DatabaseName}", databaseName);
                    return;
                }

                if (state.Equals("FAILED", StringComparison.OrdinalIgnoreCase))
                {
                    reader.Close();
                    throw new InvalidOperationException($"Database copy failed for: {databaseName}");
                }
            }

            reader.Close();
            await Task.Delay(checkInterval);
        }

        throw new TimeoutException($"Database copy operation timed out after {timeout.TotalMinutes} minutes: {databaseName}");
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