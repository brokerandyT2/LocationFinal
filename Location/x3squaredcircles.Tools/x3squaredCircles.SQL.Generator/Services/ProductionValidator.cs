using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using x3squaredcirecles.SQLSync.Generator.Models;
using System.Text.RegularExpressions;

namespace x3squaredcirecles.SQLSync.Generator.Services;

public class ProductionValidator
{
    private readonly ILogger<ProductionValidator> _logger;

    public ProductionValidator(ILogger<ProductionValidator> logger)
    {
        _logger = logger;
    }

    public async Task<ValidationReport> ValidateProductionChangesAsync(List<string> ddlStatements, string connectionString)
    {
        _logger.LogInformation("Starting production validation for {Count} DDL statements", ddlStatements.Count);

        var report = new ValidationReport
        {
            TotalStatements = ddlStatements.Count
        };

        // Analyze each DDL statement
        foreach (var statement in ddlStatements)
        {
            var issues = AnalyzeDDLStatement(statement);
            report.Issues.AddRange(issues);
        }

        // Get database context for advanced validation
        await EnrichWithDatabaseContext(report, connectionString);

        // Calculate summary statistics
        CalculateSummaryStatistics(report);

        // Determine overall result
        report.OverallResult = DetermineOverallResult(report);

        _logger.LogInformation("Validation completed: {Summary}", report.Summary);
        return report;
    }

    private List<ValidationIssue> AnalyzeDDLStatement(string statement)
    {
        var issues = new List<ValidationIssue>();
        var normalizedStatement = statement.Trim().ToUpperInvariant();

        // Check for blocking issues first
        foreach (var blockingPattern in ValidationRules.BlockingPatterns)
        {
            if (ContainsPattern(normalizedStatement, blockingPattern.Key))
            {
                issues.Add(new ValidationIssue
                {
                    Severity = ValidationSeverity.Error,
                    Category = ValidationCategories.DataLoss,
                    Description = blockingPattern.Value,
                    Statement = TruncateStatement(statement),
                    Recommendation = "Remove this statement or handle manually outside of automated deployment",
                    TableName = ExtractTableName(statement),
                    ColumnName = ExtractColumnName(statement)
                });
            }
        }

        // Check for warning issues
        foreach (var warningPattern in ValidationRules.WarningPatterns)
        {
            if (ContainsPattern(normalizedStatement, warningPattern.Key))
            {
                var issue = new ValidationIssue
                {
                    Severity = ValidationSeverity.Warning,
                    Statement = TruncateStatement(statement),
                    TableName = ExtractTableName(statement),
                    ColumnName = ExtractColumnName(statement)
                };

                // Categorize and provide specific recommendations
                if (warningPattern.Key.Contains("ALTER COLUMN"))
                {
                    issue.Category = ValidationCategories.DataLoss;
                    issue.Description = "Column alteration may truncate existing data or cause conversion errors";
                    issue.Recommendation = "Verify existing data is compatible with new column definition";
                }
                else if (warningPattern.Key.Contains("INDEX"))
                {
                    issue.Category = ValidationCategories.Performance;
                    issue.Description = "Creating indexes can cause table locks and performance impact on large tables";
                    issue.Recommendation = "Consider creating indexes during maintenance window for large tables";
                }
                else if (warningPattern.Key.Contains("CONSTRAINT"))
                {
                    issue.Category = ValidationCategories.Compatibility;
                    issue.Description = "Adding constraints may fail if existing data violates the constraint";
                    issue.Recommendation = "Verify existing data meets constraint requirements before deployment";
                }
                else
                {
                    issue.Category = ValidationCategories.Compatibility;
                    issue.Description = warningPattern.Value;
                    issue.Recommendation = "Review change carefully and test against production data subset";
                }

                issues.Add(issue);
            }
        }

        // Check for safe patterns (informational)
        if (issues.Count == 0) // Only if no warnings or errors found
        {
            foreach (var safePattern in ValidationRules.SafePatterns)
            {
                if (ContainsPattern(normalizedStatement, safePattern.Key))
                {
                    issues.Add(new ValidationIssue
                    {
                        Severity = ValidationSeverity.Info,
                        Category = "Safe Operation",
                        Description = safePattern.Value,
                        Statement = TruncateStatement(statement),
                        Recommendation = "Safe to deploy automatically",
                        TableName = ExtractTableName(statement)
                    });
                    break; // Only add one safe issue per statement
                }
            }
        }

        return issues;
    }

    private async Task EnrichWithDatabaseContext(ValidationReport report, string connectionString)
    {
        try
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            // Enrich with table size information for performance warnings
            foreach (var issue in report.Issues.Where(i => i.Severity == ValidationSeverity.Warning))
            {
                if (!string.IsNullOrEmpty(issue.TableName))
                {
                    var tableSize = await GetTableSizeAsync(connection, issue.TableName);

                    if (tableSize > 1000000) // More than 1M rows
                    {
                        issue.Description += $" (Table has ~{tableSize:N0} rows - high impact expected)";
                        report.EstimatedDuration = report.EstimatedDuration.Add(TimeSpan.FromMinutes(5));
                    }
                    else if (tableSize > 100000) // More than 100K rows
                    {
                        issue.Description += $" (Table has ~{tableSize:N0} rows - moderate impact expected)";
                        report.EstimatedDuration = report.EstimatedDuration.Add(TimeSpan.FromMinutes(1));
                    }
                }
            }

            // Estimate space impact for new indexes and columns
            await EstimateSpaceImpact(report, connection);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not enrich validation with database context - proceeding with basic validation");
        }
    }

    private async Task<long> GetTableSizeAsync(SqlConnection connection, string tableName)
    {
        try
        {
            var parts = tableName.Split('.');
            var schema = parts.Length > 1 ? parts[0].Trim('[', ']') : "dbo";
            var table = parts.Length > 1 ? parts[1].Trim('[', ']') : tableName.Trim('[', ']');

            var query = @"
                SELECT SUM(p.rows) as RowCount
                FROM sys.tables t
                INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
                INNER JOIN sys.partitions p ON t.object_id = p.object_id
                WHERE s.name = @schema AND t.name = @table AND p.index_id IN (0,1)";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@schema", schema);
            command.Parameters.AddWithValue("@table", table);

            var result = await command.ExecuteScalarAsync();
            return result != null && result != DBNull.Value ? Convert.ToInt64(result) : 0;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not get table size for {TableName}", tableName);
            return 0;
        }
    }

    private async Task EstimateSpaceImpact(ValidationReport report, SqlConnection connection)
    {
        // Estimate space impact for CREATE INDEX statements
        var indexStatements = report.Issues.Where(i => i.Statement.ToUpperInvariant().Contains("CREATE") &&
                                                       i.Statement.ToUpperInvariant().Contains("INDEX")).ToList();

        foreach (var indexIssue in indexStatements)
        {
            if (!string.IsNullOrEmpty(indexIssue.TableName))
            {
                var tableSize = await GetTableSizeAsync(connection, indexIssue.TableName);
                var estimatedIndexSizeMB = Math.Max(1, tableSize / 50000); // Rough estimate: 1MB per 50K rows
                report.EstimatedSpaceImpactMB += estimatedIndexSizeMB;
            }
        }
    }

    private void CalculateSummaryStatistics(ValidationReport report)
    {
        report.SafeStatements = report.Issues.Count(i => i.Severity == ValidationSeverity.Info) +
                               (report.TotalStatements - report.Issues.GroupBy(i => i.Statement).Count());
        report.WarningStatements = report.Issues.Count(i => i.Severity == ValidationSeverity.Warning);
        report.ErrorStatements = report.Issues.Count(i => i.Severity == ValidationSeverity.Error);

        // Add base duration estimate
        report.EstimatedDuration = report.EstimatedDuration.Add(TimeSpan.FromSeconds(report.TotalStatements * 2));
    }

    private ValidationResult DetermineOverallResult(ValidationReport report)
    {
        if (report.HasBlockingIssues)
            return ValidationResult.Blocked;

        if (report.HasWarnings)
            return ValidationResult.Warnings;

        return ValidationResult.Safe;
    }

    private bool ContainsPattern(string statement, string pattern)
    {
        try
        {
            return Regex.IsMatch(statement, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Regex pattern match failed for pattern: {Pattern}", pattern);
            return statement.Contains(pattern, StringComparison.OrdinalIgnoreCase);
        }
    }

    private string ExtractTableName(string statement)
    {
        try
        {
            // Try to extract table name from various DDL patterns
            var patterns = new[]
            {
                @"(?:ALTER|CREATE|DROP)\s+TABLE\s+(\[?[\w\.]+\]?)",
                @"ON\s+(\[?[\w\.]+\]?)\s*\(",
                @"REFERENCES\s+(\[?[\w\.]+\]?)"
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(statement, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    return match.Groups[1].Value.Trim();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not extract table name from statement");
        }

        return string.Empty;
    }

    private string ExtractColumnName(string statement)
    {
        try
        {
            // Try to extract column name from ALTER COLUMN statements
            var match = Regex.Match(statement, @"ALTER\s+COLUMN\s+(\[?\w+\]?)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return match.Groups[1].Value.Trim();
            }

            // Try to extract from ADD COLUMN statements
            match = Regex.Match(statement, @"ADD\s+(\[?\w+\]?)\s+", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return match.Groups[1].Value.Trim();
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not extract column name from statement");
        }

        return string.Empty;
    }

    private string TruncateStatement(string statement)
    {
        const int maxLength = 150;
        if (statement.Length <= maxLength)
            return statement.Trim();

        return statement.Substring(0, maxLength).Trim() + "...";
    }

    public string FormatValidationReport(ValidationReport report)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("=== PRE-FLIGHT VALIDATION REPORT ===");
        sb.AppendLine($"Overall Status: {report.Summary}");
        sb.AppendLine($"Estimated Duration: {report.EstimatedDuration.TotalMinutes:F1} minutes");

        if (report.EstimatedSpaceImpactMB > 0)
            sb.AppendLine($"Estimated Space Impact: {report.EstimatedSpaceImpactMB} MB");

        sb.AppendLine();

        if (report.Issues.Any())
        {
            // Group issues by severity
            var errorIssues = report.Issues.Where(i => i.Severity == ValidationSeverity.Error).ToList();
            var warningIssues = report.Issues.Where(i => i.Severity == ValidationSeverity.Warning).ToList();
            var infoIssues = report.Issues.Where(i => i.Severity == ValidationSeverity.Info).ToList();

            if (errorIssues.Any())
            {
                sb.AppendLine("❌ BLOCKING ISSUES (Deployment will be rejected):");
                foreach (var issue in errorIssues)
                {
                    sb.AppendLine($"  • [{issue.Category}] {issue.Description}");
                    sb.AppendLine($"    Statement: {issue.Statement}");
                    sb.AppendLine($"    Recommendation: {issue.Recommendation}");
                    sb.AppendLine();
                }
            }

            if (warningIssues.Any())
            {
                sb.AppendLine("⚠️  WARNING ISSUES (Manual approval required):");
                foreach (var issue in warningIssues)
                {
                    sb.AppendLine($"  • [{issue.Category}] {issue.Description}");
                    sb.AppendLine($"    Statement: {issue.Statement}");
                    sb.AppendLine($"    Recommendation: {issue.Recommendation}");
                    sb.AppendLine();
                }
            }

            if (infoIssues.Any())
            {
                sb.AppendLine("✅ SAFE OPERATIONS:");
                foreach (var issue in infoIssues.Take(3)) // Show first 3 safe operations
                {
                    sb.AppendLine($"  • {issue.Description}");
                }
                if (infoIssues.Count > 3)
                {
                    sb.AppendLine($"  • ... and {infoIssues.Count - 3} more safe operations");
                }
            }
        }
        else
        {
            sb.AppendLine("✅ No issues detected - all statements are safe for automatic deployment");
        }

        sb.AppendLine("=== END VALIDATION REPORT ===");
        return sb.ToString();
    }
}