using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using x3squaredcircles.SQLData.Generator.Models;

namespace x3squaredcircles.SQLData.Generator.Services;

public class SchemaAnalyzer
{
    private readonly ILogger<SchemaAnalyzer> _logger;

    public SchemaAnalyzer(ILogger<SchemaAnalyzer> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Detects new columns and tables for smart update mode
    /// This is the core "smart" functionality that only populates NEW schema additions
    /// </summary>
    public async Task<SchemaChanges> DetectSchemaChangesAsync(TestDataOptions options)
    {
        _logger.LogInformation("Analyzing schema changes for smart update mode...");

        var changes = new SchemaChanges();

        using var connection = new SqlConnection(BuildConnectionString(options));
        await connection.OpenAsync();

        // Get current database schema state
        var currentSchema = await GetCurrentDatabaseSchemaAsync(connection, options);

        // Get expected schema from domain assemblies (this would integrate with your SQLServerSyncGenerator logic)
        var expectedSchema = await GetExpectedSchemaFromDomainAsync(options);

        // Find the differences
        changes.NewTables = FindNewTables(expectedSchema, currentSchema);
        changes.NewColumns = FindNewColumns(expectedSchema, currentSchema);

        _logger.LogInformation("Schema analysis complete: {NewTables} new tables, {NewColumns} new columns",
            changes.NewTables.Count, changes.NewColumns.Count);

        return changes;
    }

    private async Task<DatabaseSchema> GetCurrentDatabaseSchemaAsync(SqlConnection connection, TestDataOptions options)
    {
        var schema = new DatabaseSchema();
        var schemasToAnalyze = GetTargetSchemas(options);

        _logger.LogDebug("Analyzing schemas: {Schemas}", string.Join(", ", schemasToAnalyze));

        foreach (var schemaName in schemasToAnalyze)
        {
            var tables = await GetTablesInSchemaAsync(connection, schemaName);
            schema.Tables.AddRange(tables);
        }

        _logger.LogDebug("Current database: {TableCount} tables", schema.Tables.Count);
        return schema;
    }

    private async Task<List<DatabaseTable>> GetTablesInSchemaAsync(SqlConnection connection, string schemaName)
    {
        var tables = new List<DatabaseTable>();

        var query = @"
            SELECT 
                s.name AS SchemaName,
                t.name AS TableName
            FROM sys.tables t 
            INNER JOIN sys.schemas s ON t.schema_id = s.schema_id 
            WHERE s.name = @schemaName
            ORDER BY t.name";

        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@schemaName", schemaName);
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var table = new DatabaseTable
            {
                SchemaName = reader.GetString(0), // SchemaName
                TableName = reader.GetString(1)   // TableName
            };

            tables.Add(table);
        }

        // Get columns for each table
        foreach (var table in tables)
        {
            table.Columns = await GetColumnsForTableAsync(connection, table.SchemaName, table.TableName);
        }

        return tables;
    }

    private async Task<List<DatabaseColumn>> GetColumnsForTableAsync(SqlConnection connection, string schemaName, string tableName)
    {
        var columns = new List<DatabaseColumn>();

        var query = @"
            SELECT 
                c.name AS ColumnName,
                t.name AS DataType,
                c.max_length,
                c.precision,
                c.scale,
                c.is_nullable,
                c.is_identity
            FROM sys.columns c
            INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
            INNER JOIN sys.tables tb ON c.object_id = tb.object_id
            INNER JOIN sys.schemas s ON tb.schema_id = s.schema_id
            WHERE s.name = @schemaName AND tb.name = @tableName
            ORDER BY c.column_id";

        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@schemaName", schemaName);
        command.Parameters.AddWithValue("@tableName", tableName);
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            columns.Add(new DatabaseColumn
            {
                ColumnName = reader.GetString(0), // ColumnName
                DataType = reader.GetString(1),   // DataType
                MaxLength = reader.IsDBNull(2) ? null : reader.GetInt16(2),  // max_length
                Precision = reader.IsDBNull(3) ? null : reader.GetByte(3),   // precision
                Scale = reader.IsDBNull(4) ? null : reader.GetByte(4),       // scale
                IsNullable = reader.GetBoolean(5),   // is_nullable
                IsIdentity = reader.GetBoolean(6)    // is_identity
            });
        }

        return columns;
    }

    private async Task<DatabaseSchema> GetExpectedSchemaFromDomainAsync(TestDataOptions options)
    {
        // This would integrate with your SQLServerSyncGenerator's entity analysis
        // For now, we'll create a simplified version that could be enhanced

        _logger.LogDebug("Analyzing expected schema from domain assemblies...");

        // In a full implementation, this would:
        // 1. Load domain assemblies like your SQLServerSyncGenerator does
        // 2. Use EntityAnalyzer to get all entities with [ExportToSQL]
        // 3. Build expected schema from those entities

        // For now, return empty schema - this is where you'd integrate with your existing code
        return new DatabaseSchema();
    }

    private List<DatabaseTable> FindNewTables(DatabaseSchema expected, DatabaseSchema current)
    {
        var currentTableKeys = current.Tables.Select(t => $"{t.SchemaName}.{t.TableName}").ToHashSet();

        return expected.Tables
            .Where(t => !currentTableKeys.Contains($"{t.SchemaName}.{t.TableName}"))
            .ToList();
    }

    private List<NewColumn> FindNewColumns(DatabaseSchema expected, DatabaseSchema current)
    {
        var newColumns = new List<NewColumn>();
        var currentTableDict = current.Tables.ToDictionary(t => $"{t.SchemaName}.{t.TableName}", t => t);

        foreach (var expectedTable in expected.Tables)
        {
            var tableKey = $"{expectedTable.SchemaName}.{expectedTable.TableName}";

            if (currentTableDict.TryGetValue(tableKey, out var currentTable))
            {
                var currentColumnNames = currentTable.Columns.Select(c => c.ColumnName).ToHashSet();

                foreach (var expectedColumn in expectedTable.Columns)
                {
                    if (!currentColumnNames.Contains(expectedColumn.ColumnName))
                    {
                        newColumns.Add(new NewColumn
                        {
                            SchemaName = expectedTable.SchemaName,
                            TableName = expectedTable.TableName,
                            ColumnName = expectedColumn.ColumnName,
                            DataType = expectedColumn.DataType,
                            MaxLength = expectedColumn.MaxLength,
                            IsNullable = expectedColumn.IsNullable
                        });
                    }
                }
            }
        }

        return newColumns;
    }

    private List<string> GetTargetSchemas(TestDataOptions options)
    {
        var schemas = new List<string>();

        if (options.IncludeCore)
        {
            schemas.Add("Core");
        }

        if (!string.IsNullOrEmpty(options.Vertical))
        {
            schemas.Add(options.Vertical);
        }

        return schemas;
    }

    private string BuildConnectionString(TestDataOptions options)
    {
        if (!string.IsNullOrEmpty(options.ConnectionString))
            return options.ConnectionString;

        var builder = new SqlConnectionStringBuilder();

        if (options.UseLocal)
        {
            builder.DataSource = options.Server ?? "localhost\\SQLEXPRESS";
            builder.InitialCatalog = options.Database ?? "LocationDev";
            builder.IntegratedSecurity = true;
        }
        else
        {
            // For Azure SQL, would need Key Vault integration like SQLServerSyncGenerator
            throw new NotImplementedException("Azure SQL connection building not implemented yet");
        }

        builder.CommandTimeout = 300; // 5 minutes
        return builder.ConnectionString;
    }
}