using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace SQLServerSyncGenerator.Services;

public class DatabaseSchemaAnalyzer
{
    private readonly ILogger<DatabaseSchemaAnalyzer> _logger;

    public DatabaseSchemaAnalyzer(ILogger<DatabaseSchemaAnalyzer> logger)
    {
        _logger = logger;
    }

    public async Task<List<string>> GetExistingSchemasAsync(SqlConnection connection)
    {
        var schemas = new List<string>();
        var query = "SELECT name FROM sys.schemas WHERE name NOT IN ('dbo', 'guest', 'INFORMATION_SCHEMA', 'sys', 'db_owner', 'db_accessadmin', 'db_securityadmin', 'db_ddladmin', 'db_backupoperator', 'db_datareader', 'db_datawriter', 'db_denydatareader', 'db_denydatawriter')";

        using var command = new SqlCommand(query, connection);
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            schemas.Add(reader.GetString(0));
        }

        return schemas;
    }

    public async Task<Dictionary<string, bool>> GetExistingTablesAsync(SqlConnection connection)
    {
        var tables = new Dictionary<string, bool>();
        var query = @"
            SELECT s.name AS SchemaName, t.name AS TableName
            FROM sys.tables t 
            INNER JOIN sys.schemas s ON t.schema_id = s.schema_id";

        using var command = new SqlCommand(query, connection);
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var schemaName = reader.GetString(0);
            var tableName = reader.GetString(1);
            var tableKey = $"{schemaName}.{tableName}";
            tables[tableKey] = true;
        }

        return tables;
    }

    public async Task<Dictionary<string, List<string>>> GetExistingColumnsAsync(SqlConnection connection)
    {
        var columns = new Dictionary<string, List<string>>();
        var query = @"
            SELECT s.name AS SchemaName, t.name AS TableName, c.name AS ColumnName
            FROM sys.columns c
            INNER JOIN sys.tables t ON c.object_id = t.object_id
            INNER JOIN sys.schemas s ON t.schema_id = s.schema_id";

        using var command = new SqlCommand(query, connection);
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var schemaName = reader.GetString(0);
            var tableName = reader.GetString(1);
            var columnName = reader.GetString(2);
            var tableKey = $"{schemaName}.{tableName}";

            if (!columns.ContainsKey(tableKey))
                columns[tableKey] = new List<string>();

            columns[tableKey].Add(columnName);
        }

        return columns;
    }

    public async Task<Dictionary<string, List<string>>> GetExistingIndexesAsync(SqlConnection connection)
    {
        var indexes = new Dictionary<string, List<string>>();
        var query = @"
            SELECT s.name AS SchemaName, t.name AS TableName, i.name AS IndexName
            FROM sys.indexes i
            INNER JOIN sys.tables t ON i.object_id = t.object_id
            INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
            WHERE i.name IS NOT NULL AND i.type > 0";

        using var command = new SqlCommand(query, connection);
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var schemaName = reader.GetString(0);
            var tableName = reader.GetString(1);
            var indexName = reader.GetString(2);
            var tableKey = $"{schemaName}.{tableName}";

            if (!indexes.ContainsKey(tableKey))
                indexes[tableKey] = new List<string>();

            indexes[tableKey].Add(indexName);
        }

        return indexes;
    }

    public async Task<Dictionary<string, List<string>>> GetExistingForeignKeysAsync(SqlConnection connection)
    {
        var foreignKeys = new Dictionary<string, List<string>>();
        var query = @"
            SELECT s.name AS SchemaName, t.name AS TableName, fk.name AS ForeignKeyName
            FROM sys.foreign_keys fk
            INNER JOIN sys.tables t ON fk.parent_object_id = t.object_id
            INNER JOIN sys.schemas s ON t.schema_id = s.schema_id";

        using var command = new SqlCommand(query, connection);
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var schemaName = reader.GetString(0);
            var tableName = reader.GetString(1);
            var foreignKeyName = reader.GetString(2);
            var tableKey = $"{schemaName}.{tableName}";

            if (!foreignKeys.ContainsKey(tableKey))
                foreignKeys[tableKey] = new List<string>();

            foreignKeys[tableKey].Add(foreignKeyName);
        }

        return foreignKeys;
    }

    public async Task<Dictionary<string, List<string>>> GetExistingStoredProceduresAsync(SqlConnection connection)
    {
        var procedures = new Dictionary<string, List<string>>();
        var query = @"
            SELECT s.name AS SchemaName, p.name AS ProcedureName
            FROM sys.procedures p
            INNER JOIN sys.schemas s ON p.schema_id = s.schema_id
            WHERE p.type = 'P'";

        using var command = new SqlCommand(query, connection);
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var schemaName = reader.GetString(0);
            var procedureName = reader.GetString(1);

            if (!procedures.ContainsKey(schemaName))
                procedures[schemaName] = new List<string>();

            procedures[schemaName].Add(procedureName);
        }

        return procedures;
    }

    public async Task<Dictionary<string, List<string>>> GetExistingViewsAsync(SqlConnection connection)
    {
        var views = new Dictionary<string, List<string>>();
        var query = @"
            SELECT s.name AS SchemaName, v.name AS ViewName
            FROM sys.views v
            INNER JOIN sys.schemas s ON v.schema_id = s.schema_id";

        using var command = new SqlCommand(query, connection);
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var schemaName = reader.GetString(0);
            var viewName = reader.GetString(1);

            if (!views.ContainsKey(schemaName))
                views[schemaName] = new List<string>();

            views[schemaName].Add(viewName);
        }

        return views;
    }

    public async Task<Dictionary<string, List<string>>> GetExistingFunctionsAsync(SqlConnection connection)
    {
        var functions = new Dictionary<string, List<string>>();
        var query = @"
            SELECT s.name AS SchemaName, o.name AS FunctionName, o.type_desc
            FROM sys.objects o
            INNER JOIN sys.schemas s ON o.schema_id = s.schema_id
            WHERE o.type IN ('FN', 'TF', 'IF')";

        using var command = new SqlCommand(query, connection);
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var schemaName = reader.GetString(0);
            var functionName = reader.GetString(1);
            var functionType = reader.GetString(2);

            var key = $"{schemaName}.{functionType}";
            if (!functions.ContainsKey(key))
                functions[key] = new List<string>();

            functions[key].Add(functionName);
        }

        return functions;
    }

    public async Task<Dictionary<string, List<string>>> GetExistingTriggersAsync(SqlConnection connection)
    {
        var triggers = new Dictionary<string, List<string>>();
        var query = @"
            SELECT s.name AS SchemaName, t.name AS TableName, tr.name AS TriggerName
            FROM sys.triggers tr
            INNER JOIN sys.tables t ON tr.parent_id = t.object_id
            INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
            WHERE tr.is_ms_shipped = 0";

        using var command = new SqlCommand(query, connection);
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var schemaName = reader.GetString(0);
            var tableName = reader.GetString(1);
            var triggerName = reader.GetString(2);
            var tableKey = $"{schemaName}.{tableName}";

            if (!triggers.ContainsKey(tableKey))
                triggers[tableKey] = new List<string>();

            triggers[tableKey].Add(triggerName);
        }

        return triggers;
    }

    public async Task<List<string>> GetExistingUserDefinedTypesAsync(SqlConnection connection)
    {
        var types = new List<string>();
        var query = @"
            SELECT s.name + '.' + t.name AS TypeName
            FROM sys.types t
            INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
            WHERE t.is_user_defined = 1";

        using var command = new SqlCommand(query, connection);
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            types.Add(reader.GetString(0));
        }

        return types;
    }

    public async Task<long> GetTableRowCountAsync(SqlConnection connection, string schemaName, string tableName)
    {
        try
        {
            var query = @"
                SELECT SUM(p.rows) as RowCount
                FROM sys.tables t
                INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
                INNER JOIN sys.partitions p ON t.object_id = p.object_id
                WHERE s.name = @schema AND t.name = @table AND p.index_id IN (0,1)";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@schema", schemaName);
            command.Parameters.AddWithValue("@table", tableName);

            var result = await command.ExecuteScalarAsync();
            return result != null && result != DBNull.Value ? Convert.ToInt64(result) : 0;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not get table row count for {Schema}.{Table}", schemaName, tableName);
            return 0;
        }
    }

    public async Task<decimal> GetTableSizeMBAsync(SqlConnection connection, string schemaName, string tableName)
    {
        try
        {
            var query = @"
                SELECT 
                    CAST(SUM(a.total_pages) * 8.0 / 1024 AS DECIMAL(10,2)) AS SizeMB
                FROM sys.tables t
                INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
                INNER JOIN sys.indexes i ON t.object_id = i.object_id
                INNER JOIN sys.partitions p ON i.object_id = p.object_id AND i.index_id = p.index_id
                INNER JOIN sys.allocation_units a ON p.partition_id = a.container_id
                WHERE s.name = @schema AND t.name = @table";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@schema", schemaName);
            command.Parameters.AddWithValue("@table", tableName);

            var result = await command.ExecuteScalarAsync();
            return result != null && result != DBNull.Value ? Convert.ToDecimal(result) : 0;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not get table size for {Schema}.{Table}", schemaName, tableName);
            return 0;
        }
    }
}