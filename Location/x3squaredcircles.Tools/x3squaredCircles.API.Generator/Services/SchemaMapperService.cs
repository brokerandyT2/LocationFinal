using x3squaredcirecles.API.Generator.APIGenerator.Models;
using Microsoft.Extensions.Logging;
using System.Text;

namespace x3squaredcirecles.API.Generator.APIGenerator.Services;

public class SchemaMapperService
{
    private readonly ILogger<SchemaMapperService> _logger;

    public SchemaMapperService(ILogger<SchemaMapperService> logger)
    {
        _logger = logger;
    }

    public async Task<List<string>> GenerateInsertStatementsAsync(ExtractedData extractedData, List<ExtractableEntity> entities, string connectionString)
    {
        var insertStatements = new List<string>();

        try
        {
            _logger.LogInformation("Generating SQL Server INSERT statements for {TableCount} tables", entities.Count);

            foreach (var entity in entities)
            {
                if (!extractedData.TableData.ContainsKey(entity.TableName))
                {
                    _logger.LogWarning("No data found for table: {TableName}", entity.TableName);
                    continue;
                }

                var tableData = extractedData.TableData[entity.TableName];
                if (!tableData.Any())
                {
                    _logger.LogDebug("Table {TableName} has no data to insert", entity.TableName);
                    continue;
                }

                var statements = await GenerateTableInsertStatementsAsync(entity, tableData, extractedData.UserInfo);
                insertStatements.AddRange(statements);

                _logger.LogDebug("Generated {StatementCount} INSERT statements for table: {TableName}",
                    statements.Count, entity.TableName);
            }

            _logger.LogInformation("Generated {TotalStatements} total INSERT statements", insertStatements.Count);
            return insertStatements;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate INSERT statements");
            throw;
        }
    }

    private async Task<List<string>> GenerateTableInsertStatementsAsync(ExtractableEntity entity, List<Dictionary<string, object>> tableData, UserInfo userInfo)
    {
        var statements = new List<string>();

        foreach (var row in tableData)
        {
            try
            {
                var insertStatement = GenerateRowInsertStatement(entity, row, userInfo);
                statements.Add(insertStatement);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate INSERT for row in table: {TableName}", entity.TableName);
                // Continue with other rows
            }
        }

        return statements;
    }

    private string GenerateRowInsertStatement(ExtractableEntity entity, Dictionary<string, object> row, UserInfo userInfo)
    {
        var sb = new StringBuilder();

        // Start INSERT statement
        sb.Append($"INSERT INTO [{entity.SchemaName}].[{entity.TableName}] (");

        // Build column list (skip SQLite Id - let SQL Server generate new ones)
        var columns = new List<string>();
        var values = new List<string>();

        // Add user identification columns
        columns.Add("UserEmail");
        values.Add($"'{EscapeSqlString(userInfo.Email)}'");

        columns.Add("UserAppGuid");
        values.Add($"'{EscapeSqlString(userInfo.AppGuid)}'");

        // Add data columns (skip Id/primary key columns)
        foreach (var kvp in row)
        {
            var columnName = kvp.Key;
            var value = kvp.Value;

            // Skip SQLite identity columns - let SQL Server generate new IDs
            if (columnName.Equals("Id", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Find property mapping for this column
            var propertyMapping = entity.PropertyMappings.FirstOrDefault(p =>
                p.ColumnName.Equals(columnName, StringComparison.OrdinalIgnoreCase) ||
                p.PropertyName.Equals(columnName, StringComparison.OrdinalIgnoreCase));

            if (propertyMapping != null)
            {
                columns.Add($"[{propertyMapping.ColumnName}]");
                values.Add(FormatValueForSqlServer(value, propertyMapping));
            }
            else
            {
                // Fallback - use column name as-is
                columns.Add($"[{columnName}]");
                values.Add(FormatValueForSqlServer(value, null));
            }
        }

        // Complete INSERT statement
        sb.Append(string.Join(", ", columns));
        sb.Append(") VALUES (");
        sb.Append(string.Join(", ", values));
        sb.Append(");");

        return sb.ToString();
    }

    private string FormatValueForSqlServer(object? value, PropertyMappingInfo? propertyMapping)
    {
        if (value == null)
        {
            return "NULL";
        }

        // Handle based on property mapping or value type
        if (propertyMapping?.SqlServerType != null)
        {
            return FormatValueByDataType(value, propertyMapping.SqlServerType);
        }

        // Fallback - format by actual value type
        return FormatValueByValueType(value);
    }

    private string FormatValueByDataType(object value, string sqlServerType)
    {
        var upperType = sqlServerType.ToUpperInvariant();

        if (upperType.StartsWith("NVARCHAR") || upperType.StartsWith("VARCHAR") || upperType == "TEXT")
        {
            return $"N'{EscapeSqlString(value.ToString() ?? "")}'";
        }

        if (upperType.StartsWith("INT") || upperType == "BIGINT" || upperType == "SMALLINT" || upperType == "TINYINT")
        {
            return value.ToString() ?? "0";
        }

        if (upperType == "BIT")
        {
            if (value is bool boolVal)
                return boolVal ? "1" : "0";

            // Handle SQLite integer boolean (0/1)
            if (int.TryParse(value.ToString(), out var intVal))
                return intVal == 0 ? "0" : "1";

            return "0";
        }

        if (upperType.StartsWith("FLOAT") || upperType.StartsWith("REAL") || upperType.StartsWith("DECIMAL"))
        {
            return value.ToString() ?? "0.0";
        }

        if (upperType.StartsWith("DATETIME"))
        {
            if (value is DateTime dateTime)
                return $"'{dateTime:yyyy-MM-dd HH:mm:ss.fff}'";

            if (DateTime.TryParse(value.ToString(), out var parsedDate))
                return $"'{parsedDate:yyyy-MM-dd HH:mm:ss.fff}'";

            return "NULL";
        }

        if (upperType == "UNIQUEIDENTIFIER")
        {
            if (Guid.TryParse(value.ToString(), out var guid))
                return $"'{guid}'";

            return "NULL";
        }

        // Default fallback
        return $"N'{EscapeSqlString(value.ToString() ?? "")}'";
    }

    private string FormatValueByValueType(object value)
    {
        return value switch
        {
            string strVal => $"N'{EscapeSqlString(strVal)}'",
            int or long or short or byte => value.ToString() ?? "0",
            bool boolVal => boolVal ? "1" : "0",
            double or float or decimal => value.ToString() ?? "0.0",
            DateTime dateTime => $"'{dateTime:yyyy-MM-dd HH:mm:ss.fff}'",
            Guid guid => $"'{guid}'",
            null => "NULL",
            _ => $"N'{EscapeSqlString(value.ToString() ?? "")}'"
        };
    }

    private string EscapeSqlString(string input)
    {
        if (string.IsNullOrEmpty(input))
            return "";

        return input.Replace("'", "''");
    }
}