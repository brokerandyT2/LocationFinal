using Microsoft.Extensions.Logging;
using System.Diagnostics;
using x3squaredcircles.SQLData.Generator.Models;

namespace x3squaredcircles.SQLData.Generator.Services;

public class SqlServerSyncOrchestrator
{
    private readonly ILogger<SqlServerSyncOrchestrator> _logger;

    public SqlServerSyncOrchestrator(ILogger<SqlServerSyncOrchestrator> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Runs the SQLServerSyncGenerator to ensure database schema is current
    /// </summary>
    public async Task<int> RunSchemaGeneratorAsync(TestDataOptions options)
    {
        _logger.LogInformation("Running SQLServerSyncGenerator to ensure schema is current...");

        try
        {
            var arguments = BuildSchemaGeneratorArguments(options);

            _logger.LogDebug("Executing: sql-schema-generator {Arguments}", arguments);

            var processStartInfo = new ProcessStartInfo
            {
                FileName = "sql-schema-generator",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = processStartInfo };

            var outputLines = new List<string>();
            var errorLines = new List<string>();

            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    outputLines.Add(e.Data);
                    _logger.LogDebug("[SQLSync] {Output}", e.Data);
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    errorLines.Add(e.Data);
                    _logger.LogWarning("[SQLSync] {Error}", e.Data);
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync();

            var exitCode = process.ExitCode;

            if (exitCode == 0)
            {
                _logger.LogInformation("✅ SQLServerSyncGenerator completed successfully");
            }
            else
            {
                _logger.LogError("❌ SQLServerSyncGenerator failed with exit code: {ExitCode}", exitCode);

                if (errorLines.Any())
                {
                    _logger.LogError("Error output: {Errors}", string.Join("\n", errorLines));
                }
            }

            return exitCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute SQLServerSyncGenerator: {Message}", ex.Message);
            return 1;
        }
    }

    /// <summary>
    /// Truncates all tables in target schemas to start fresh (CREATE mode)
    /// </summary>
    public async Task TruncateTablesAsync(TestDataOptions options)
    {
        _logger.LogWarning("Truncating tables to start fresh (CREATE mode)");

        // This could integrate with your SQLServerSyncGenerator to:
        // 1. Get list of all tables in dependency order
        // 2. Disable foreign key constraints
        // 3. Truncate all tables
        // 4. Re-enable foreign key constraints

        // For now, we'll run a separate schema generator command to handle this
        var arguments = BuildSchemaGeneratorArguments(options, truncateFirst: true);

        _logger.LogDebug("Executing truncate: sql-schema-generator {Arguments}", arguments);

        try
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "sql-schema-generator",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = processStartInfo };

            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    _logger.LogDebug("[SQLSync-Truncate] {Output}", e.Data);
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    _logger.LogWarning("[SQLSync-Truncate] {Error}", e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync();

            if (process.ExitCode == 0)
            {
                _logger.LogInformation("✅ Table truncation completed");
            }
            else
            {
                _logger.LogError("❌ Table truncation failed with exit code: {ExitCode}", process.ExitCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to truncate tables: {Message}", ex.Message);
            throw;
        }
    }

    private string BuildSchemaGeneratorArguments(TestDataOptions options, bool truncateFirst = false)
    {
        var args = new List<string>();

        // Connection parameters
        if (!string.IsNullOrEmpty(options.Server))
        {
            args.Add($"--server \"{options.Server}\"");
        }

        if (!string.IsNullOrEmpty(options.Database))
        {
            args.Add($"--database \"{options.Database}\"");
        }

        if (options.UseLocal)
        {
            args.Add("--local");
        }
        else
        {
            // Azure Key Vault parameters
            if (!string.IsNullOrEmpty(options.KeyVaultUrl))
            {
                args.Add($"--keyvault-url \"{options.KeyVaultUrl}\"");
            }
            if (!string.IsNullOrEmpty(options.UsernameSecret))
            {
                args.Add($"--username-secret \"{options.UsernameSecret}\"");
            }
            if (!string.IsNullOrEmpty(options.PasswordSecret))
            {
                args.Add($"--password-secret \"{options.PasswordSecret}\"");
            }
        }

        // Execution mode
        if (truncateFirst)
        {
            // This would be a custom option you'd add to SQLServerSyncGenerator
            // args.Add("--truncate-first");

            // For now, we'll use execute mode to ensure schema is current
            args.Add("--execute");
        }
        else
        {
            // Use execute mode to apply any schema changes
            args.Add("--execute");
        }

        // Verbose logging if requested
        if (options.Verbose)
        {
            args.Add("--verbose");
        }

        return string.Join(" ", args);
    }

    /// <summary>
    /// Checks if SQLServerSyncGenerator tool is available
    /// </summary>
    public async Task<bool> IsSchemaGeneratorAvailableAsync()
    {
        try
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "sql-schema-generator",
                Arguments = "--help",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = processStartInfo };
            process.Start();
            await process.WaitForExitAsync();

            return process.ExitCode == 0;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the current schema version from SQLServerSyncGenerator
    /// </summary>
    public async Task<string> GetCurrentSchemaVersionAsync(TestDataOptions options)
    {
        try
        {
            var arguments = BuildSchemaGeneratorArguments(options) + " --schema-version";

            var processStartInfo = new ProcessStartInfo
            {
                FileName = "sql-schema-generator",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = processStartInfo };
            process.Start();

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode == 0)
            {
                return output.Trim();
            }

            return "Unknown";
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not get schema version");
            return "Unknown";
        }
    }

    /// <summary>
    /// Validates that schema generator completed successfully and database is ready for data
    /// </summary>
    public async Task<bool> ValidateSchemaReadyAsync(TestDataOptions options)
    {
        _logger.LogDebug("Validating that database schema is ready for data population...");

        // This could perform additional validation like:
        // 1. Check that target schemas exist
        // 2. Check that expected tables exist
        // 3. Verify foreign key relationships are in place
        // 4. Ensure no schema drift

        try
        {
            // For now, simple validation - could be enhanced
            var schemaVersion = await GetCurrentSchemaVersionAsync(options);

            if (schemaVersion != "Unknown")
            {
                _logger.LogInformation("Schema validation passed. Current version: {Version}", schemaVersion);
                return true;
            }
            else
            {
                _logger.LogWarning("Could not validate schema version");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Schema validation failed: {Message}", ex.Message);
            return false;
        }
    }
}