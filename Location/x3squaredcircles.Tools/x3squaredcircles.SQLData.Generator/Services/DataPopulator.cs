using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;
using x3squaredcircles.SQLData.Generator.Models;
using x3squaredcircles.SQLData.Generator.TestDataGenerator.Services;

namespace x3squaredcircles.SQLData.Generator.Services;

public class DataPopulator
{
    private readonly ILogger<DataPopulator> _logger;
    private readonly RealisticDataEngine _dataEngine;

    public DataPopulator(ILogger<DataPopulator> logger, RealisticDataEngine dataEngine)
    {
        _logger = logger;
        _dataEngine = dataEngine;
    }

    /// <summary>
    /// Populates all tables with realistic data (CREATE mode)
    /// </summary>
    public async Task<DataPopulationResult> PopulateAllTablesAsync(TestDataOptions options)
    {
        _logger.LogInformation("Starting full data population (CREATE mode)");

        var result = new DataPopulationResult();

        using var connection = new SqlConnection(BuildConnectionString(options));
        await connection.OpenAsync();

        // Get all tables in target schemas
        var schemas = GetTargetSchemas(options);
        var allTables = new List<DatabaseTable>();

        foreach (var schema in schemas)
        {
            var schemaTables = await GetTablesInSchemaAsync(connection, schema);
            allTables.AddRange(schemaTables);
        }

        _logger.LogInformation("Found {TableCount} tables to populate across {SchemaCount} schemas",
            allTables.Count, schemas.Count);

        // Sort tables by dependency order (Core tables first, then referenced tables)
        var sortedTables = SortTablesByDependency(allTables);

        // Populate each table
        foreach (var table in sortedTables)
        {
            try
            {
                var rowsInserted = await PopulateTableAsync(connection, table, options);
                result.TablesPopulated++;
                result.TotalRowsInserted += rowsInserted;

                _logger.LogInformation("Populated {Schema}.{Table}: {Rows} rows",
                    table.SchemaName, table.TableName, rowsInserted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to populate table {Schema}.{Table}: {Message}",
                    table.SchemaName, table.TableName, ex.Message);
                result.FailedTables++;
            }
        }

        _logger.LogInformation("Data population complete: {Success} tables populated, {Failed} failed, {TotalRows} total rows",
            result.TablesPopulated, result.FailedTables, result.TotalRowsInserted);

        return result;
    }

    /// <summary>
    /// Populates only new columns and tables (UPDATE mode - the "smart" functionality)
    /// </summary>
    public async Task<DataPopulationResult> PopulateSchemaChangesAsync(SchemaChanges changes, TestDataOptions options)
    {
        _logger.LogInformation("Starting smart data population (UPDATE mode)");
        _logger.LogInformation("Changes detected: {NewTables} new tables, {NewColumns} new columns",
            changes.NewTables.Count, changes.NewColumns.Count);

        var result = new DataPopulationResult();

        using var connection = new SqlConnection(BuildConnectionString(options));
        await connection.OpenAsync();

        // 1. Populate new tables completely
        foreach (var newTable in changes.NewTables)
        {
            try
            {
                var rowsInserted = await PopulateTableAsync(connection, newTable, options);
                result.TablesPopulated++;
                result.TotalRowsInserted += rowsInserted;

                _logger.LogInformation("Populated NEW table {Schema}.{Table}: {Rows} rows",
                    newTable.SchemaName, newTable.TableName, rowsInserted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to populate new table {Schema}.{Table}: {Message}",
                    newTable.SchemaName, newTable.TableName, ex.Message);
                result.FailedTables++;
            }
        }

        // 2. Populate only NEW columns in existing tables (the smart part!)
        var columnGroups = changes.NewColumns.GroupBy(c => new { c.SchemaName, c.TableName });

        foreach (var tableGroup in columnGroups)
        {
            try
            {
                var rowsUpdated = await PopulateNewColumnsAsync(connection, tableGroup.Key.SchemaName,
                    tableGroup.Key.TableName, tableGroup.ToList(), options);

                result.TablesUpdated++;
                result.TotalRowsUpdated += rowsUpdated;

                _logger.LogInformation("Updated NEW columns in {Schema}.{Table}: {Columns} columns, {Rows} rows updated",
                    tableGroup.Key.SchemaName, tableGroup.Key.TableName, tableGroup.Count(), rowsUpdated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to populate new columns in {Schema}.{Table}: {Message}",
                    tableGroup.Key.SchemaName, tableGroup.Key.TableName, ex.Message);
                result.FailedTables++;
            }
        }

        _logger.LogInformation("Smart update complete: {NewTables} new tables, {UpdatedTables} tables with new columns, {TotalInserted} rows inserted, {TotalUpdated} rows updated",
            result.TablesPopulated, result.TablesUpdated, result.TotalRowsInserted, result.TotalRowsUpdated);

        return result;
    }

    private async Task<int> PopulateTableAsync(SqlConnection connection, DatabaseTable table, TestDataOptions options)
    {
        // Skip tables that are likely system or junction tables
        if (ShouldSkipTable(table.TableName))
        {
            _logger.LogDebug("Skipping system/junction table: {Table}", table.TableName);
            return 0;
        }

        // Get the columns we can populate (exclude identity and computed columns)
        var populatableColumns = table.Columns
            .Where(c => !c.IsIdentity && !c.ColumnName.ToLower().EndsWith("id") || c.ColumnName.ToLower() == "userid")
            .ToList();

        if (!populatableColumns.Any())
        {
            _logger.LogDebug("No populatable columns found for table: {Table}", table.TableName);
            return 0;
        }

        var rowCount = _dataEngine.GetDataVolume(options, table.TableName);
        var rowsInserted = 0;

        // Insert data in batches for performance
        const int batchSize = 100;
        for (int batch = 0; batch < rowCount; batch += batchSize)
        {
            var currentBatchSize = Math.Min(batchSize, rowCount - batch);
            var batchInserted = await InsertBatchAsync(connection, table, populatableColumns, currentBatchSize, options);
            rowsInserted += batchInserted;
        }

        return rowsInserted;
    }

    private async Task<int> PopulateNewColumnsAsync(SqlConnection connection, string schemaName, string tableName,
        List<NewColumn> newColumns, TestDataOptions options)
    {
        // Get existing row count
        var existingRowCount = await GetTableRowCountAsync(connection, schemaName, tableName);

        if (existingRowCount == 0)
        {
            _logger.LogDebug("No existing rows to update in {Schema}.{Table}", schemaName, tableName);
            return 0;
        }

        // Build UPDATE statement for new columns only
        var setClause = string.Join(", ", newColumns.Select(c => $"[{c.ColumnName}] = @{c.ColumnName}"));
        var updateSql = $"UPDATE [{schemaName}].[{tableName}] SET {setClause} WHERE [{newColumns.First().ColumnName}] IS NULL";

        using var command = new SqlCommand(updateSql, connection);

        // Generate realistic data for each new column
        foreach (var column in newColumns)
        {
            var value = _dataEngine.GenerateRealisticValue(schemaName, tableName, column.ColumnName, column.DataType, options);
            command.Parameters.AddWithValue($"@{column.ColumnName}", value ?? DBNull.Value);
        }

        var rowsUpdated = await command.ExecuteNonQueryAsync();

        _logger.LogDebug("Updated {Rows} rows with new column data in {Schema}.{Table}",
            rowsUpdated, schemaName, tableName);

        return rowsUpdated;
    }

    private async Task<int> InsertBatchAsync(SqlConnection connection, DatabaseTable table,
        List<DatabaseColumn> columns, int batchSize, TestDataOptions options)
    {
        if (batchSize == 0) return 0;

        // Build bulk INSERT statement
        var columnNames = string.Join(", ", columns.Select(c => $"[{c.ColumnName}]"));
        var valueParams = string.Join(", ", columns.Select(c => $"@{c.ColumnName}"));

        var insertSql = $"INSERT INTO [{table.SchemaName}].[{table.TableName}] ({columnNames}) VALUES ";
        var valueRows = new List<string>();

        // Build parameterized values for batch
        for (int i = 0; i < batchSize; i++)
        {
            var rowParams = string.Join(", ", columns.Select(c => $"@{c.ColumnName}{i}"));
            valueRows.Add($"({rowParams})");
        }

        insertSql += string.Join(", ", valueRows);

        using var command = new SqlCommand(insertSql, connection);

        // Generate data for each row in the batch
        for (int rowIndex = 0; rowIndex < batchSize; rowIndex++)
        {
            foreach (var column in columns)
            {
                var value = _dataEngine.GenerateRealisticValue(table.SchemaName, table.TableName,
                    column.ColumnName, column.DataType, options);
                command.Parameters.AddWithValue($"@{column.ColumnName}{rowIndex}", value ?? DBNull.Value);
            }
        }

        try
        {
            return await command.ExecuteNonQueryAsync();
        }
        catch (SqlException ex)
        {
            _logger.LogWarning("Batch insert failed for {Schema}.{Table}: {Message}. Trying individual inserts...",
                table.SchemaName, table.TableName, ex.Message);

            // Fallback to individual inserts if batch fails
            return await InsertIndividualRowsAsync(connection, table, columns, batchSize, options);
        }
    }

    private async Task<int> InsertIndividualRowsAsync(SqlConnection connection, DatabaseTable table,
        List<DatabaseColumn> columns, int rowCount, TestDataOptions options)
    {
        var columnNames = string.Join(", ", columns.Select(c => $"[{c.ColumnName}]"));
        var valueParams = string.Join(", ", columns.Select(c => $"@{c.ColumnName}"));
        var insertSql = $"INSERT INTO [{table.SchemaName}].[{table.TableName}] ({columnNames}) VALUES ({valueParams})";

        var successCount = 0;

        for (int i = 0; i < rowCount; i++)
        {
            try
            {
                using var command = new SqlCommand(insertSql, connection);

                foreach (var column in columns)
                {
                    var value = _dataEngine.GenerateRealisticValue(table.SchemaName, table.TableName,
                        column.ColumnName, column.DataType, options);
                    command.Parameters.AddWithValue($"@{column.ColumnName}", value ?? DBNull.Value);
                }

                await command.ExecuteNonQueryAsync();
                successCount++;
            }
            catch (SqlException ex)
            {
                _logger.LogDebug("Individual insert failed for {Schema}.{Table} row {Row}: {Message}",
                    table.SchemaName, table.TableName, i + 1, ex.Message);
                // Continue with next row
            }
        }

        return successCount;
    }

    private async Task<int> GetTableRowCountAsync(SqlConnection connection, string schemaName, string tableName)
    {
        var countSql = $"SELECT COUNT(*) FROM [{schemaName}].[{tableName}]";
        using var command = new SqlCommand(countSql, connection);

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    private async Task<List<DatabaseTable>> GetTablesInSchemaAsync(SqlConnection connection, string schemaName)
    {
        // This would use the same logic as SchemaAnalyzer
        // For now, simplified implementation
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
                SchemaName = reader.GetString("SchemaName"),
                TableName = reader.GetString("TableName")
            };

            tables.Add(table);
        }

        // Get columns for each table (simplified - would use full column metadata in real implementation)
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
                ColumnName = reader.GetString("ColumnName"),
                DataType = reader.GetString("DataType"),
                MaxLength = reader.IsDBNull("max_length") ? null : reader.GetInt16("max_length"),
                Precision = reader.IsDBNull("precision") ? null : reader.GetByte("precision"),
                Scale = reader.IsDBNull("scale") ? null : reader.GetByte("scale"),
                IsNullable = reader.GetBoolean("is_nullable"),
                IsIdentity = reader.GetBoolean("is_identity")
            });
        }

        return columns;
    }

    private List<DatabaseTable> SortTablesByDependency(List<DatabaseTable> tables)
    {
        // Simple dependency sorting - Core tables first, then others
        return tables.OrderBy(t => t.SchemaName == "Core" ? 0 : 1)
                    .ThenBy(t => t.TableName.ToLower().Contains("lookup") || t.TableName.ToLower().Contains("reference") ? 0 : 1)
                    .ThenBy(t => t.TableName)
                    .ToList();
    }

    private bool ShouldSkipTable(string tableName)
    {
        var skipPatterns = new[] { "sysdiagram", "__ef", "migrations", "audit", "log" };
        return skipPatterns.Any(pattern => tableName.ToLower().Contains(pattern));
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

public class DataPopulationResult
{
    public int TablesPopulated { get; set; }
    public int TablesUpdated { get; set; }
    public int FailedTables { get; set; }
    public int TotalRowsInserted { get; set; }
    public int TotalRowsUpdated { get; set; }
}