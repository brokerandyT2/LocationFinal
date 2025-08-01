using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using SQLServerSyncGenerator;
using SQLServerSyncGenerator.Models;
using SQLServerSyncGenerator.Services;

namespace SqlSchemaGenerator;

class Program
{
    static async Task<int> Main(string[] args)
    {
        try
        {
            // Parse command line arguments
            var parseResult = Parser.Default.ParseArguments<GeneratorOptions>(args);

            return await parseResult.MapResult(
                async options => await RunGeneratorAsync(options),
                _ => Task.FromResult(1) // Return 1 for parsing errors
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unhandled exception: {ex.Message}");
            return 1;
        }
    }

    static async Task<int> RunGeneratorAsync(GeneratorOptions options)
    {
        // Determine verbose logging based on prod flag
        var isVerbose = options.Verbose ?? !options.IsProduction;

        // Setup dependency injection and logging
        var services = new ServiceCollection()
            .AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(isVerbose ? LogLevel.Debug : LogLevel.Information);
            })
            .AddSingleton<AssemblyLoader>()
            .AddSingleton<EntityAnalyzer>()
            .AddSingleton<DependencyGraphBuilder>()
            .AddSingleton<SchemaGenerator>()
            .AddSingleton<SqlExecutor>()
            .AddSingleton<ConnectionStringBuilder>()
            .AddSingleton<ProductionValidator>() // NEW: Add validation service
            .BuildServiceProvider();

        var logger = services.GetRequiredService<ILogger<Program>>();

        try
        {
            logger.LogInformation("SQL Schema Generator v1.0.0");
            logger.LogInformation("Server: {Server}", options.Server);
            logger.LogInformation("Database: {Database}", options.Database);
            logger.LogInformation("Production Mode: {IsProduction}", options.IsProduction);
            logger.LogInformation("No-Op Mode: {IsNoOp}", options.NoOp);
            logger.LogInformation("Validate Only: {ValidateOnly}", options.ValidateOnly); // NEW
            logger.LogInformation("Verbose Logging: {IsVerbose}", isVerbose);

            // Step 1: Build connection string from Key Vault
            logger.LogInformation("Building connection string from Azure Key Vault...");
            var connectionBuilder = services.GetRequiredService<ConnectionStringBuilder>();
            var connectionString = await connectionBuilder.BuildConnectionStringAsync(options);
            logger.LogDebug("Connection string built successfully");

            // Step 2: Load Domain assemblies
            logger.LogInformation("Loading Domain assemblies...");
            var assemblyLoader = services.GetRequiredService<AssemblyLoader>();
            var assemblyPaths = await assemblyLoader.LoadDomainAssemblyPathsAsync(options);

            if (assemblyPaths.Count == 0)
            {
                logger.LogError("No Domain assemblies found. Make sure Domain projects are built.");
                return 1;
            }

            logger.LogInformation("Found {Count} Domain assemblies", assemblyPaths.Count);

            // Step 3: Analyze entities from assemblies
            logger.LogInformation("Analyzing entities from Domain assemblies...");
            var entityAnalyzer = services.GetRequiredService<EntityAnalyzer>();
            var entities = await entityAnalyzer.AnalyzeAssembliesAsync(assemblyPaths);

            if (entities.Count == 0)
            {
                logger.LogError("No entities found in Domain assemblies.");
                return 1;
            }

            logger.LogInformation("Found {CoreCount} Core entities and {PhotoCount} Photography entities",
                entities.Count(e => e.Schema == "Core"),
                entities.Count(e => e.Schema == "Photography"));

            // Step 4: Build dependency graph and sort
            logger.LogInformation("Building entity dependency graph...");
            var dependencyBuilder = services.GetRequiredService<DependencyGraphBuilder>();
            var dependencyGraph = dependencyBuilder.BuildGraph(entities);
            var sortedEntities = dependencyBuilder.TopologicalSort(dependencyGraph);

            logger.LogInformation("Entity creation order determined: {Order}",
                string.Join(" → ", sortedEntities.Select(e => $"{e.Schema}.{e.TableName}")));

            // Step 5: Generate DDL for validation/execution
            logger.LogInformation("Generating schema DDL...");
            var schemaGenerator = services.GetRequiredService<SchemaGenerator>();
            var sqlExecutor = services.GetRequiredService<SqlExecutor>();

            // NEW: Handle validation-only mode
            if (options.ValidateOnly)
            {
                return await HandleValidationOnlyAsync(schemaGenerator, sortedEntities, connectionString, logger, services);
            }

            // NEW: Pre-flight validation for production deployments
            if (options.IsProduction && !options.NoOp)
            {
                logger.LogInformation("Production mode - running pre-flight validation...");
                var validationResult = await RunPreflightValidationAsync(schemaGenerator, sortedEntities, connectionString, logger, services);

                if (validationResult != ValidationResult.Safe)
                {
                    // Validation failed or has warnings - this should trigger manual approval in pipeline
                    return (int)validationResult;
                }

                logger.LogInformation("Pre-flight validation passed - proceeding with deployment");
            }

            // Step 6: Production backup if needed (skip if NoOp)
            string? backupName = null;

            if (options.IsProduction && !options.NoOp)
            {
                logger.LogInformation("Production mode - creating database backup...");
                backupName = $"{options.Database}_PreDeploy_{DateTime.UtcNow:yyyyMMdd_HHmmss}";
                await sqlExecutor.CreateDatabaseBackupAsync(connectionString, options.Database, backupName);
                logger.LogInformation("Database backup created: {BackupName}", backupName);
            }
            else if (options.IsProduction && options.NoOp)
            {
                logger.LogInformation("No-Op mode: Would create database backup in production: {Database}_PreDeploy_{Timestamp}",
                    options.Database, DateTime.UtcNow.ToString("yyyyMMdd_HHmmss"));
            }

            // Step 7: Generate delta DDL and apply/log changes
            if (options.NoOp)
            {
                logger.LogInformation("No-Op mode: Analyzing database and generating delta DDL...");
                var deltaStatements = await schemaGenerator.GenerateDeltaDDLAsync(sortedEntities, connectionString);
                logger.LogInformation("Generated {Count} delta DDL statements", deltaStatements.Count);
                LogDDLStatements(deltaStatements, logger);
            }
            else
            {
                var ddlStatements = schemaGenerator.GenerateCreateTableStatements(sortedEntities);
                logger.LogInformation("Applying {Count} DDL statements...", ddlStatements.Count);
                await sqlExecutor.ExecuteDDLBatchAsync(connectionString, ddlStatements);
            }

            // Step 8: Cleanup backup if successful (skip if NoOp)
            if (options.IsProduction && !options.NoOp && !string.IsNullOrEmpty(backupName))
            {
                logger.LogInformation("Schema changes successful - cleaning up backup...");
                await sqlExecutor.DeleteDatabaseBackupAsync(connectionString, backupName);
                logger.LogInformation("Backup cleanup completed");
            }
            else if (options.IsProduction && options.NoOp)
            {
                logger.LogInformation("No-Op mode: Would clean up backup after successful deployment");
            }

            logger.LogInformation("Schema generation completed successfully!");
            return 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Schema generation failed: {Message}", ex.Message);

            // NEW: Automatic rollback on production failure
            if (options.IsProduction && !options.NoOp)
            {
                await HandleProductionFailureAsync(ex, options, logger, services);
            }

            return 1;
        }
        finally
        {
            services.Dispose();
        }
    }

    // NEW: Handle validation-only mode
    private static async Task<int> HandleValidationOnlyAsync(
        SchemaGenerator schemaGenerator,
        List<EntityMetadata> sortedEntities,
        string connectionString,
        ILogger logger,
        ServiceProvider services)
    {
        logger.LogInformation("=== VALIDATION-ONLY MODE ===");

        var deltaStatements = await schemaGenerator.GenerateDeltaDDLAsync(sortedEntities, connectionString);
        logger.LogInformation("Generated {Count} DDL statements for validation", deltaStatements.Count);

        if (deltaStatements.Count == 0)
        {
            logger.LogInformation("✅ No schema changes required - database is up to date");
            return (int)ValidationResult.Safe;
        }

        var validator = services.GetRequiredService<ProductionValidator>();
        var validationReport = await validator.ValidateProductionChangesAsync(deltaStatements, connectionString);

        // Output formatted report
        var formattedReport = validator.FormatValidationReport(validationReport);
        logger.LogInformation("{ValidationReport}", formattedReport);

        return (int)validationReport.OverallResult;
    }

    // NEW: Run pre-flight validation
    private static async Task<ValidationResult> RunPreflightValidationAsync(
        SchemaGenerator schemaGenerator,
        List<EntityMetadata> sortedEntities,
        string connectionString,
        ILogger logger,
        ServiceProvider services)
    {
        var deltaStatements = await schemaGenerator.GenerateDeltaDDLAsync(sortedEntities, connectionString);

        if (deltaStatements.Count == 0)
        {
            logger.LogInformation("✅ No schema changes required - skipping validation");
            return ValidationResult.Safe;
        }

        var validator = services.GetRequiredService<ProductionValidator>();
        var validationReport = await validator.ValidateProductionChangesAsync(deltaStatements, connectionString);

        logger.LogInformation("Pre-flight validation result: {Result}", validationReport.OverallResult);

        if (validationReport.OverallResult != ValidationResult.Safe)
        {
            var formattedReport = validator.FormatValidationReport(validationReport);
            logger.LogWarning("Pre-flight validation issues detected:\n{ValidationReport}", formattedReport);
        }

        return validationReport.OverallResult;
    }

    // NEW: Handle production deployment failures with automatic rollback
    private static async Task HandleProductionFailureAsync(
        Exception originalException,
        GeneratorOptions options,
        ILogger logger,
        ServiceProvider services)
    {
        logger.LogError("Production deployment failed - attempting automatic rollback...");

        try
        {
            var connectionBuilder = services.GetRequiredService<ConnectionStringBuilder>();
            var connectionString = await connectionBuilder.BuildConnectionStringAsync(options);
            var sqlExecutor = services.GetRequiredService<SqlExecutor>();

            // Look for backup created during this deployment
            var backupName = $"{options.Database}_PreDeploy_{DateTime.UtcNow:yyyyMMdd_HHmmss}";

            // Check if backup exists and restore
            if (await sqlExecutor.DatabaseExistsAsync(connectionString, backupName))
            {
                logger.LogInformation("Found backup database: {BackupName} - restoring...", backupName);
                await sqlExecutor.RestoreDatabaseFromBackupAsync(connectionString, options.Database, backupName);
                logger.LogInformation("✅ Database successfully restored from backup: {BackupName}", backupName);

                // Clean up the backup after successful restore
                await sqlExecutor.DeleteDatabaseBackupAsync(connectionString, backupName);
                logger.LogInformation("Backup cleanup completed after restore");
            }
            else
            {
                logger.LogWarning("No backup database found for automatic rollback - manual intervention may be required");
            }
        }
        catch (Exception rollbackException)
        {
            logger.LogError(rollbackException, "❌ Automatic rollback failed - manual intervention required");
            logger.LogError("Original deployment error: {OriginalError}", originalException.Message);
            logger.LogError("Rollback error: {RollbackError}", rollbackException.Message);
        }
    }

    private static void LogDDLStatements(List<string> ddlStatements, ILogger logger)
    {
        if (ddlStatements.Count == 0)
        {
            logger.LogInformation("=== No DDL Changes Required ===");
            logger.LogInformation("Database schema is already up to date with entity definitions.");
            return;
        }

        logger.LogInformation("=== Delta DDL Statements (No-Op Mode) ===");
        logger.LogInformation("The following {Count} DDL statements would be executed:", ddlStatements.Count);

        for (int i = 0; i < ddlStatements.Count; i++)
        {
            logger.LogInformation("DDL Statement {StatementNumber}:", i + 1);
            logger.LogInformation("{DDLStatement}", ddlStatements[i]);
            logger.LogInformation("--- End Statement {StatementNumber} ---", i + 1);
        }

        logger.LogInformation("=== End Delta DDL Statements ({Count} total) ===", ddlStatements.Count);
    }
}

public class GeneratorOptions
{
    [Option("server", Required = true, HelpText = "SQL Server name (e.g., myserver.database.windows.net)")]
    public string Server { get; set; } = string.Empty;

    [Option("database", Required = true, HelpText = "Database name")]
    public string Database { get; set; } = string.Empty;

    [Option("keyvault-url", Required = true, HelpText = "Azure Key Vault URL (e.g., https://myvault.vault.azure.net/)")]
    public string KeyVaultUrl { get; set; } = string.Empty;

    [Option("username-secret", Required = true, HelpText = "Key Vault secret name for SQL username")]
    public string UsernameSecret { get; set; } = string.Empty;

    [Option("password-secret", Required = true, HelpText = "Key Vault secret name for SQL password")]
    public string PasswordSecret { get; set; } = string.Empty;

    [Option("prod", Required = false, Default = false, HelpText = "Production mode - creates database backup before changes")]
    public bool IsProduction { get; set; }

    [Option("verbose", Required = false, HelpText = "Enable verbose logging (default: true for dev, false for prod)")]
    public bool? Verbose { get; set; }

    [Option("noop", Required = false, Default = false, HelpText = "No-operation mode - generate and log DDL without executing against database")]
    public bool NoOp { get; set; }

    [Option("validate-only", Required = false, Default = false, HelpText = "Validation-only mode - analyze changes and return exit code based on safety (0=safe, 1=warnings, 2=blocked)")]
    public bool ValidateOnly { get; set; } // NEW

    [Option("core-assembly", Required = false, HelpText = "Path to Location.Core.Domain.dll")]
    public string? CoreAssemblyPath { get; set; }

    [Option("vertical-assembly", Required = false, HelpText = "Path to Location.Photography.Domain.dll")]
    public string? PhotographyAssemblyPath { get; set; }
}