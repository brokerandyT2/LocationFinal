using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using x3squaredcircles.SQLData.Generator.Models;

namespace x3squaredcircles.SQLData.Generator.Services;

public class ConstraintValidator
{
    private readonly ILogger<ConstraintValidator> _logger;

    public ConstraintValidator(ILogger<ConstraintValidator> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Simple constraint testing: inject bad data and verify constraints catch it
    /// </summary>
    public async Task<ConstraintValidationResults> ValidateConstraintsAsync(string badDataConfigPath, TestDataOptions options)
    {
        _logger.LogInformation("Testing constraint effectiveness with bad data injection...");

        var results = new ConstraintValidationResults();

        if (!File.Exists(badDataConfigPath))
        {
            _logger.LogWarning("Bad data config file not found: {Path}", badDataConfigPath);
            return results;
        }

        var badDataConfig = await LoadBadDataConfigAsync(badDataConfigPath);

        using var connection = new SqlConnection(BuildConnectionString(options));
        await connection.OpenAsync();

        foreach (var schemaScenarios in badDataConfig.Schemas)
        {
            await ValidateSchemaConstraintsAsync(connection, schemaScenarios.Key, schemaScenarios.Value, results);
        }

        LogValidationSummary(results);
        return results;
    }

    private async Task ValidateSchemaConstraintsAsync(SqlConnection connection, string schemaName,
        Dictionary<string, List<BadDataScenario>> tableScenarios, ConstraintValidationResults results)
    {
        _logger.LogInformation("Testing constraints in schema: {Schema}", schemaName);

        foreach (var tableScenario in tableScenarios)
        {
            var tableName = tableScenario.Key;
            var scenarios = tableScenario.Value;

            foreach (var scenario in scenarios)
            {
                await RunBadDataScenarioAsync(connection, schemaName, tableName, scenario, results);
            }
        }
    }

    private async Task RunBadDataScenarioAsync(SqlConnection connection, string schemaName, string tableName,
        BadDataScenario scenario, ConstraintValidationResults results)
    {
        var testName = $"{schemaName}.{tableName}.{scenario.Scenario}";

        try
        {
            // Build INSERT statement with bad data
            var insertSql = BuildInsertStatement(schemaName, tableName, scenario.Overrides);

            _logger.LogDebug("Testing scenario: {TestName}", testName);
            _logger.LogDebug("SQL: {Sql}", insertSql);

            using var command = new SqlCommand(insertSql, connection);

            // Add parameters for the bad data
            foreach (var override_ in scenario.Overrides)
            {
                command.Parameters.AddWithValue($"@{override_.Key}", override_.Value ?? DBNull.Value);
            }

            // Attempt the insert - this SHOULD fail if constraints are working
            await command.ExecuteNonQueryAsync();

            // If we get here, the constraint FAILED to catch the bad data
            var failure = new ConstraintTestFailure
            {
                TestName = testName,
                Scenario = scenario.Scenario,
                Expected = "constraint_violation",
                Actual = "insert_succeeded",
                Message = $"⚠️ CONSTRAINT WEAKNESS: {tableName} should have rejected {FormatBadData(scenario.Overrides)}",
                ActionRequired = $"Review constraints on {tableName}",
                Severity = scenario.LogLevel ?? "warn"
            };

            results.Failures.Add(failure);
            results.FailedTests++;
        }
        catch (SqlException ex)
        {
            // Expected! The constraint caught the bad data
            var success = new ConstraintTestSuccess
            {
                TestName = testName,
                Scenario = scenario.Scenario,
                Message = $"✅ CONSTRAINT OK: {tableName} properly rejected {FormatBadData(scenario.Overrides)}",
                ConstraintType = DetermineConstraintType(ex.Number),
                SqlErrorNumber = ex.Number
            };

            results.Successes.Add(success);
            results.PassedTests++;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error testing scenario: {TestName}", testName);

            var failure = new ConstraintTestFailure
            {
                TestName = testName,
                Scenario = scenario.Scenario,
                Expected = "constraint_violation",
                Actual = "unexpected_error",
                Message = $"❌ TEST ERROR: {ex.Message}",
                ActionRequired = "Review test configuration"
            };

            results.Failures.Add(failure);
            results.FailedTests++;
        }

        results.TotalTests++;
    }

    private string BuildInsertStatement(string schemaName, string tableName, Dictionary<string, object?> badData)
    {
        var columns = string.Join(", ", badData.Keys.Select(k => $"[{k}]"));
        var parameters = string.Join(", ", badData.Keys.Select(k => $"@{k}"));

        return $"INSERT INTO [{schemaName}].[{tableName}] ({columns}) VALUES ({parameters})";
    }

    private string FormatBadData(Dictionary<string, object?> badData)
    {
        var items = badData.Select(kvp => $"{kvp.Key}={kvp.Value ?? "NULL"}");
        return string.Join(", ", items);
    }

    private string DetermineConstraintType(int sqlErrorNumber)
    {
        return sqlErrorNumber switch
        {
            515 => "NOT_NULL",
            547 => "CHECK_OR_FK",
            2627 => "UNIQUE_KEY",
            2628 => "STRING_LENGTH",
            8152 => "STRING_TRUNCATION",
            _ => $"SQL_ERROR_{sqlErrorNumber}"
        };
    }

    private void LogValidationSummary(ConstraintValidationResults results)
    {
        var passRate = results.TotalTests > 0 ? (results.PassedTests * 100.0 / results.TotalTests) : 0;

        _logger.LogInformation("=== CONSTRAINT VALIDATION SUMMARY ===");
        _logger.LogInformation("Total Tests: {Total}", results.TotalTests);
        _logger.LogInformation("Passed: {Passed} ({PassRate:F1}%)", results.PassedTests, passRate);
        _logger.LogInformation("Failed: {Failed}", results.FailedTests);

        if (results.Failures.Any())
        {
            _logger.LogWarning("⚠️ CONSTRAINT WEAKNESSES DETECTED:");
            foreach (var failure in results.Failures)
            {
                _logger.LogWarning("  • {Message}", failure.Message);
                _logger.LogWarning("    Action: {Action}", failure.ActionRequired);
            }
        }

        if (results.Successes.Any())
        {
            _logger.LogInformation("✅ WORKING CONSTRAINTS:");
            foreach (var success in results.Successes.Take(3))
            {
                _logger.LogInformation("  • {Message}", success.Message);
            }
            if (results.Successes.Count > 3)
            {
                _logger.LogInformation("  • ... and {More} more working constraints", results.Successes.Count - 3);
            }
        }
    }

    private async Task<BadDataConfig> LoadBadDataConfigAsync(string filePath)
    {
        var json = await File.ReadAllTextAsync(filePath);
        var config = JsonSerializer.Deserialize<BadDataConfig>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return config ?? new BadDataConfig();
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

        return builder.ConnectionString;
    }
}
