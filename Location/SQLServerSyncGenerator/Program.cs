using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using SQLServerSyncGenerator;
using SQLServerSyncGenerator.Models;
using SQLServerSyncGenerator.Services;

namespace SQLServerSyncGenerator;

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
            .AddSingleton<ProductionValidator>()
            .AddSingleton<DdlStatementGenerator>()
            .AddSingleton<DatabaseSchemaAnalyzer>()
            .AddSingleton<RepositoryDetector>()
            .AddSingleton<SqlScriptDiscovery>()
            .AddSingleton<SqlScriptEnhancer>()
            .AddSingleton<DeploymentOrchestrator>()
            .AddSingleton<CompiledDeploymentGenerator>()
            .AddSingleton<GitIntegrationService>()
            .BuildServiceProvider();

        var logger = services.GetRequiredService<ILogger<Program>>();

        try
        {
            logger.LogInformation("SQL Schema Generator v2.0.0 - 29-Phase Deployment Pipeline");
            logger.LogInformation("Server: {Server}", options.Server);
            logger.LogInformation("Database: {Database}", options.Database);
            logger.LogInformation("Production Mode: {IsProduction}", options.IsProduction);
            logger.LogInformation("No-Op Mode: {IsNoOp}", options.NoOp);
            logger.LogInformation("Validate Only: {ValidateOnly}", options.ValidateOnly);
            logger.LogInformation("Execute Mode: {Execute}", options.Execute);
            logger.LogInformation("Verbose Logging: {IsVerbose}", isVerbose);

            // Step 1: Build connection string
            logger.LogInformation("Building connection string...");
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

            // Step 5: Execute based on mode
            var schemaGenerator = services.GetRequiredService<SchemaGenerator>();

            if (options.ValidateOnly)
            {
                return await HandleValidationOnlyAsync(schemaGenerator, sortedEntities, connectionString, assemblyPaths, logger, services);
            }

            if (options.NoOp)
            {
                return await HandleNoOpModeAsync(schemaGenerator, sortedEntities, connectionString, assemblyPaths, logger);
            }

            if (options.Execute)
            {
                return await HandleExecuteModeAsync(schemaGenerator, sortedEntities, connectionString, assemblyPaths, options, logger, services);
            }

            // Default: Show deployment plan
            logger.LogInformation("Generating deployment plan (use --execute to apply changes)...");
            await schemaGenerator.GenerateNoOpAnalysisAsync(sortedEntities, connectionString, assemblyPaths);

            logger.LogInformation("✅ Analysis completed. Use --execute to apply changes or --validate-only for production validation.");
            return 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Schema generation failed: {Message}", ex.Message);

            // Automatic rollback on production failure
            if (options.IsProduction && options.Execute)
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

    private static async Task<int> HandleValidationOnlyAsync(
        SchemaGenerator schemaGenerator,
        List<EntityMetadata> sortedEntities,
        string connectionString,
        List<string> assemblyPaths,
        ILogger logger,
        ServiceProvider services)
    {
        logger.LogInformation("=== VALIDATION-ONLY MODE ===");

        var validator = services.GetRequiredService<ProductionValidator>();
        var deltaStatements = await schemaGenerator.GenerateDeltaDDLAsync(sortedEntities, connectionString);

        logger.LogInformation("Generated {Count} DDL statements for validation", deltaStatements.Count);

        if (deltaStatements.Count == 0)
        {
            logger.LogInformation("✅ No schema changes required - database is up to date");
            return (int)ValidationResult.Safe;
        }

        var validationReport = await validator.ValidateProductionChangesAsync(deltaStatements, connectionString);
        var formattedReport = validator.FormatValidationReport(validationReport);
        logger.LogInformation("{ValidationReport}", formattedReport);

        return (int)validationReport.OverallResult;
    }

    private static async Task<int> HandleNoOpModeAsync(
        SchemaGenerator schemaGenerator,
        List<EntityMetadata> sortedEntities,
        string connectionString,
        List<string> assemblyPaths,
        ILogger logger)
    {
        logger.LogInformation("=== NO-OP MODE: 29-Phase Deployment Analysis ===");

        await schemaGenerator.GenerateNoOpAnalysisAsync(sortedEntities, connectionString, assemblyPaths);

        logger.LogInformation("✅ No-op analysis completed. Use --execute to apply changes.");
        return 0;
    }

    private static async Task<int> HandleExecuteModeAsync(
        SchemaGenerator schemaGenerator,
        List<EntityMetadata> sortedEntities,
        string connectionString,
        List<string> assemblyPaths,
        GeneratorOptions options,
        ILogger logger,
        ServiceProvider services)
    {
        logger.LogInformation("=== EXECUTE MODE: Applying Database Changes ===");

        // Pre-flight validation for production deployments
        if (options.IsProduction)
        {
            logger.LogInformation("Production mode - running pre-flight validation...");
            var validationResult = await RunPreflightValidationAsync(schemaGenerator, sortedEntities, connectionString, assemblyPaths, logger, services);

            if (validationResult != ValidationResult.Safe)
            {
                logger.LogError("Pre-flight validation failed - deployment blocked");
                return (int)validationResult;
            }

            logger.LogInformation("✅ Pre-flight validation passed - proceeding with deployment");
        }

        // Production backup if needed
        string? backupName = null;
        if (options.IsProduction)
        {
            logger.LogInformation("Production mode - creating database backup...");
            var sqlExecutor = services.GetRequiredService<SqlExecutor>();
            backupName = $"{options.Database}_PreDeploy_{DateTime.UtcNow:yyyyMMdd_HHmmss}";
            await sqlExecutor.CreateDatabaseBackupAsync(connectionString, options.Database, backupName);
            logger.LogInformation("✅ Database backup created: {BackupName}", backupName);
        }

        try
        {
            // Generate and execute full deployment
            var compiledDeploymentPath = await schemaGenerator.GenerateFullDeploymentAsync(
                sortedEntities,
                connectionString,
                assemblyPaths,
                options.IsProduction);

            // Execute the compiled deployment
            var sqlExecutor = services.GetRequiredService<SqlExecutor>();
            var compiledSql = await File.ReadAllTextAsync(compiledDeploymentPath);

            logger.LogInformation("Executing compiled deployment...");
            await sqlExecutor.ExecuteSingleStatementAsync(connectionString, compiledSql);

            // Cleanup backup if successful
            if (options.IsProduction && !string.IsNullOrEmpty(backupName))
            {
                logger.LogInformation("Deployment successful - cleaning up backup...");
                await sqlExecutor.DeleteDatabaseBackupAsync(connectionString, backupName);
            }

            logger.LogInformation("✅ Deployment completed successfully!");
            logger.LogInformation("📄 Compiled deployment: {CompiledDeploymentPath}", compiledDeploymentPath);

            return 0;
        }
        catch (Exception)
        {
            // Automatic rollback handled in main catch block
            throw;
        }
    }

    private static async Task<ValidationResult> RunPreflightValidationAsync(
        SchemaGenerator schemaGenerator,
        List<EntityMetadata> sortedEntities,
        string connectionString,
        List<string> assemblyPaths,
        ILogger logger,
        ServiceProvider services)
    {
        var validator = services.GetRequiredService<ProductionValidator>();
        var deltaStatements = await schemaGenerator.GenerateDeltaDDLAsync(sortedEntities, connectionString);

        if (deltaStatements.Count == 0)
        {
            logger.LogInformation("✅ No schema changes required - skipping validation");
            return ValidationResult.Safe;
        }

        var validationReport = await validator.ValidateProductionChangesAsync(deltaStatements, connectionString);
        logger.LogInformation("Pre-flight validation result: {Result}", validationReport.OverallResult);

        if (validationReport.OverallResult != ValidationResult.Safe)
        {
            var formattedReport = validator.FormatValidationReport(validationReport);
            logger.LogWarning("Pre-flight validation issues detected:\n{ValidationReport}", formattedReport);
        }

        return validationReport.OverallResult;
    }

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
}

public class GeneratorOptions
{
    [Option("server", Required = false, HelpText = "SQL Server name (e.g., myserver.database.windows.net)")]
    public string Server { get; set; } = string.Empty;

    [Option("database", Required = false, HelpText = "Database name")]
    public string Database { get; set; } = string.Empty;

    [Option("keyvault-url", Required = false, HelpText = "Azure Key Vault URL (e.g., https://myvault.vault.azure.net/)")]
    public string KeyVaultUrl { get; set; } = string.Empty;

    [Option("username-secret", Required = false, HelpText = "Key Vault secret name for SQL username")]
    public string UsernameSecret { get; set; } = string.Empty;

    [Option("password-secret", Required = false, HelpText = "Key Vault secret name for SQL password")]
    public string PasswordSecret { get; set; } = string.Empty;

    [Option("local", Required = false, Default = false, HelpText = "Use local SQL Server with Windows Authentication")]
    public bool UseLocal { get; set; }

    [Option("prod", Required = false, Default = false, HelpText = "Production mode - creates database backup before changes")]
    public bool IsProduction { get; set; }

    [Option("verbose", Required = false, HelpText = "Enable verbose logging (default: true for dev, false for prod)")]
    public bool? Verbose { get; set; }

    [Option("noop", Required = false, Default = false, HelpText = "No-operation mode - generate and log deployment plan without executing")]
    public bool NoOp { get; set; }

    [Option("validate-only", Required = false, Default = false, HelpText = "Validation-only mode - analyze changes and return exit code based on safety")]
    public bool ValidateOnly { get; set; }

    [Option("execute", Required = false, Default = false, HelpText = "Execute mode - apply all database changes")]
    public bool Execute { get; set; }

    [Option("core-assembly", Required = false, HelpText = "Path to Location.Core.Domain.dll")]
    public string? CoreAssemblyPath { get; set; }

    [Option("photography-assembly", Required = false, HelpText = "Path to Location.Photography.Domain.dll")]
    public string? PhotographyAssemblyPath { get; set; }

    [Option("rollback-to-previous", Required = false, Default = false, HelpText = "Rollback to previous deployment version")]
    public bool RollbackToPrevious { get; set; }

    [Option("restore-from", Required = false, HelpText = "Restore from specific version (e.g., v1.2.3)")]
    public string? RestoreFromVersion { get; set; }

    [Option("deployment-history", Required = false, Default = false, HelpText = "Show deployment history")]
    public bool ShowDeploymentHistory { get; set; }
}