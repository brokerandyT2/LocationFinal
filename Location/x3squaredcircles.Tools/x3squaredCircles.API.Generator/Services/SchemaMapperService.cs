// Enhanced SchemaMapperService with Extended ASCII support
// File: x3squaredCircles.API.Generator/Services/SchemaMapperService.cs

using x3squaredcirecles.API.Generator.APIGenerator.Models;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Globalization;

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
            _logger.LogInformation("Generating SQL Server INSERT statements for {TableCount} tables with extended ASCII support", entities.Count);

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

            _logger.LogInformation("Generated {TotalStatements} total INSERT statements with extended ASCII preservation", insertStatements.Count);
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
                var insertStatement = GenerateRowInsertStatementWithExtendedASCII(entity, row, userInfo);
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

    private string GenerateRowInsertStatementWithExtendedASCII(ExtractableEntity entity, Dictionary<string, object> row, UserInfo userInfo)
    {
        var sb = new StringBuilder();

        // Start INSERT statement with proper schema and table names
        sb.Append($"INSERT INTO [{entity.SchemaName}].[{entity.TableName}] (");

        // Build column list (skip SQLite Id - let SQL Server generate new ones)
        var columns = new List<string>();
        var values = new List<string>();

        // Add user identification columns with extended ASCII support
        columns.Add("UserEmail");
        values.Add(FormatStringValueForSqlServerWithExtendedASCII(userInfo.Email));

        columns.Add("UserAppGuid");
        values.Add(FormatStringValueForSqlServerWithExtendedASCII(userInfo.AppGuid));

        // Add data columns (skip Id/primary key columns) with extended ASCII handling
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
                values.Add(FormatValueForSqlServerWithExtendedASCII(value, propertyMapping));
            }
            else
            {
                // Fallback - use column name as-is with extended ASCII handling
                columns.Add($"[{columnName}]");
                values.Add(FormatValueForSqlServerWithExtendedASCII(value, null));
            }
        }

        // Complete INSERT statement
        sb.Append(string.Join(", ", columns));
        sb.Append(") VALUES (");
        sb.Append(string.Join(", ", values));
        sb.Append(");");

        return sb.ToString();
    }

    private string FormatValueForSqlServerWithExtendedASCII(object? value, PropertyMappingInfo? propertyMapping)
    {
        if (value == null)
        {
            return "NULL";
        }

        // Handle based on property mapping or value type with extended ASCII consideration
        if (propertyMapping?.SqlServerType != null)
        {
            return FormatValueByDataTypeWithExtendedASCII(value, propertyMapping.SqlServerType);
        }

        // Fallback - format by actual value type with extended ASCII support
        return FormatValueByValueTypeWithExtendedASCII(value);
    }

    private string FormatValueByDataTypeWithExtendedASCII(object value, string sqlServerType)
    {
        var upperType = sqlServerType.ToUpperInvariant();

        if (upperType.StartsWith("NVARCHAR") || upperType.StartsWith("VARCHAR") || upperType == "TEXT")
        {
            return FormatStringValueForSqlServerWithExtendedASCII(value.ToString() ?? "");
        }

        if (upperType.StartsWith("INT") || upperType == "BIGINT" || upperType == "SMALLINT" || upperType == "TINYINT")
        {
            return FormatNumericValue(value);
        }

        if (upperType == "BIT")
        {
            return FormatBooleanValue(value);
        }

        if (upperType.StartsWith("FLOAT") || upperType.StartsWith("REAL") || upperType.StartsWith("DECIMAL"))
        {
            return FormatDecimalValue(value);
        }

        if (upperType.StartsWith("DATETIME"))
        {
            return FormatDateTimeValue(value);
        }

        if (upperType == "UNIQUEIDENTIFIER")
        {
            return FormatGuidValue(value);
        }

        // Default fallback for unknown types - treat as extended ASCII string
        return FormatStringValueForSqlServerWithExtendedASCII(value.ToString() ?? "");
    }

    private string FormatValueByValueTypeWithExtendedASCII(object value)
    {
        return value switch
        {
            string strVal => FormatStringValueForSqlServerWithExtendedASCII(strVal),
            int or long or short or byte => FormatNumericValue(value),
            bool boolVal => FormatBooleanValue(boolVal),
            double or float or decimal => FormatDecimalValue(value),
            DateTime dateTime => FormatDateTimeValue(dateTime),
            Guid guid => FormatGuidValue(guid),
            null => "NULL",
            _ => FormatStringValueForSqlServerWithExtendedASCII(value.ToString() ?? "")
        };
    }

    private string FormatStringValueForSqlServerWithExtendedASCII(string input)
    {
        if (string.IsNullOrEmpty(input))
            return "N''";

        // Normalize the string to ensure consistent extended ASCII character representation
        var normalizedInput = input.Normalize(NormalizationForm.FormC);

        // Escape SQL string with proper extended ASCII handling
        var escapedInput = EscapeSqlStringWithExtendedASCII(normalizedInput);

        // Use NVARCHAR (N prefix) to ensure Unicode/extended ASCII support
        return $"N'{escapedInput}'";
    }

    private string FormatNumericValue(object value)
    {
        return value.ToString() ?? "0";
    }

    private string FormatBooleanValue(object value)
    {
        if (value is bool boolVal)
            return boolVal ? "1" : "0";

        // Handle SQLite integer boolean (0/1)
        if (int.TryParse(value.ToString(), out var intVal))
            return intVal == 0 ? "0" : "1";

        return "0";
    }

    private string FormatDecimalValue(object value)
    {
        // Use invariant culture to ensure consistent decimal formatting
        if (value is decimal decVal)
            return decVal.ToString(CultureInfo.InvariantCulture);

        if (value is double doubleVal)
            return doubleVal.ToString(CultureInfo.InvariantCulture);

        if (value is float floatVal)
            return floatVal.ToString(CultureInfo.InvariantCulture);

        return value.ToString() ?? "0.0";
    }

    private string FormatDateTimeValue(object value)
    {
        if (value is DateTime dateTime)
            return $"'{dateTime:yyyy-MM-dd HH:mm:ss.fff}'";

        if (DateTime.TryParse(value.ToString(), out var parsedDate))
            return $"'{parsedDate:yyyy-MM-dd HH:mm:ss.fff}'";

        return "NULL";
    }

    private string FormatGuidValue(object value)
    {
        if (value is Guid guid)
            return $"'{guid}'";

        if (Guid.TryParse(value.ToString(), out var parsedGuid))
            return $"'{parsedGuid}'";

        return "NULL";
    }

    private string EscapeSqlStringWithExtendedASCII(string input)
    {
        if (string.IsNullOrEmpty(input))
            return "";

        var sb = new StringBuilder(input.Length * 2); // Pre-allocate for efficiency

        foreach (char c in input)
        {
            switch (c)
            {
                case '\'':
                    sb.Append("''"); // Escape single quotes by doubling them
                    break;
                case '\r':
                    sb.Append("\\r"); // Escape carriage return
                    break;
                case '\n':
                    sb.Append("\\n"); // Escape line feed
                    break;
                case '\t':
                    sb.Append("\\t"); // Escape tab
                    break;
                case '\\':
                    sb.Append("\\\\"); // Escape backslash
                    break;
                case '\0':
                    sb.Append("\\0"); // Escape null character
                    break;
                default:
                    // For extended ASCII characters (128-255) and Unicode, preserve as-is
                    // SQL Server with NVARCHAR and proper collation will handle them correctly
                    if (c >= 32 && c != 127) // Printable characters including extended ASCII
                    {
                        sb.Append(c);
                    }
                    else if (c > 127) // Extended ASCII and Unicode characters
                    {
                        sb.Append(c); // Preserve extended ASCII and Unicode characters
                    }
                    else
                    {
                        // For non-printable control characters, use Unicode escape
                        sb.Append($"\\u{(int)c:X4}");
                    }
                    break;
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Validates that extended ASCII characters will be properly handled in the target database
    /// </summary>
    public async Task<bool> ValidateExtendedASCIISupportAsync(string connectionString)
    {
        try
        {
            using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
            await connection.OpenAsync();

            // Test extended ASCII character round-trip
            var testQuery = @"
                DECLARE @TestTable TABLE (
                    Id INT IDENTITY(1,1),
                    TestText NVARCHAR(255) COLLATE SQL_Latin1_General_CP1252_CI_AS
                );
                
                INSERT INTO @TestTable (TestText) VALUES 
                    (N'Café München résumé naïve'),
                    (N'Price: €50, £40, ¥500, ©2024'),
                    (N'Symbols: ± × ÷ ≠ ≤ ≥ α β γ');
                
                SELECT TestText FROM @TestTable;
            ";

            using var command = new Microsoft.Data.SqlClient.SqlCommand(testQuery, connection);
            using var reader = await command.ExecuteReaderAsync();

            var testResults = new List<string>();
            while (await reader.ReadAsync())
            {
                testResults.Add(reader.GetString(0));
            }

            // Verify extended ASCII characters were preserved
            var success = testResults.Count == 3 &&
                         testResults[0].Contains("Café") && testResults[0].Contains("München") &&
                         testResults[1].Contains("€") && testResults[1].Contains("£") && testResults[1].Contains("¥") &&
                         testResults[2].Contains("±") && testResults[2].Contains("×") && testResults[2].Contains("≠");

            if (success)
            {
                _logger.LogInformation("✅ Extended ASCII character support validated successfully");
            }
            else
            {
                _logger.LogWarning("⚠️ Extended ASCII character support validation failed");
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate extended ASCII support");
            return false;
        }
    }

    /// <summary>
    /// Analyzes extracted data for extended ASCII character usage
    /// </summary>
    public DataExtendedASCIIAnalysis AnalyzeExtendedASCIIUsage(ExtractedData extractedData)
    {
        var analysis = new DataExtendedASCIIAnalysis();

        foreach (var tableData in extractedData.TableData)
        {
            foreach (var row in tableData.Value)
            {
                foreach (var column in row)
                {
                    if (column.Value is string stringValue && !string.IsNullOrEmpty(stringValue))
                    {
                        analysis.TotalStringFields++;

                        // Check for extended ASCII characters (128-255)
                        if (stringValue.Any(c => c > 127 && c <= 255))
                        {
                            analysis.ExtendedASCIIFields++;

                            if (!analysis.SampleExtendedASCIIValues.ContainsKey(tableData.Key))
                            {
                                analysis.SampleExtendedASCIIValues[tableData.Key] = new List<string>();
                            }

                            if (analysis.SampleExtendedASCIIValues[tableData.Key].Count < 3)
                            {
                                analysis.SampleExtendedASCIIValues[tableData.Key].Add(stringValue);
                            }
                        }

                        // Check for Unicode characters (> 255)
                        if (stringValue.Any(c => c > 255))
                        {
                            analysis.UnicodeFields++;
                        }
                    }
                }
            }
        }

        analysis.ExtendedASCIIPercentage = analysis.TotalStringFields > 0
            ? (double)analysis.ExtendedASCIIFields / analysis.TotalStringFields * 100
            : 0;

        _logger.LogInformation("Extended ASCII Analysis: {ExtendedFields}/{TotalFields} fields ({Percentage:F1}%) contain extended ASCII characters",
            analysis.ExtendedASCIIFields, analysis.TotalStringFields, analysis.ExtendedASCIIPercentage);

        return analysis;
    }
}

/// <summary>
/// Analysis results for extended ASCII character usage in extracted data
/// </summary>
public class DataExtendedASCIIAnalysis
{
    public int TotalStringFields { get; set; }
    public int ExtendedASCIIFields { get; set; }
    public int UnicodeFields { get; set; }
    public double ExtendedASCIIPercentage { get; set; }
    public Dictionary<string, List<string>> SampleExtendedASCIIValues { get; set; } = new();
}