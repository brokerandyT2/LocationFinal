using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using x3squaredcircles.SQLData.Generator.Models;
using x3squaredcircles.SQLData.Generator.Services;
using x3squaredcircles.SQLData.Generator.TestDataGenerator.Services;

namespace x3squaredcircles.SQLData.Generator;

class Program
{
    static async Task<int> Main(string[] args)
    {
        try
        {
            var parseResult = Parser.Default.ParseArguments<TestDataOptions>(args);
            return await parseResult.MapResult(
                async options => await RunGeneratorAsync(options),
                _ => Task.FromResult(1)
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unhandled exception: {ex.Message}");
            return 1;
        }
    }

    static async Task<int> RunGeneratorAsync(TestDataOptions options)
    {
        var services = new ServiceCollection()
            .AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(options.Verbose ? LogLevel.Debug : LogLevel.Information);
            })
            .AddSingleton<SchemaAnalyzer>()
            .AddSingleton<RealisticDataEngine>()
            .AddSingleton<ConstraintValidator>()
            .AddSingleton<DataPopulator>()
            .AddSingleton<SqlServerSyncOrchestrator>()
            .BuildServiceProvider();

        var logger = services.GetRequiredService<ILogger<Program>>();

        try
        {
            logger.LogInformation("Test Data Generator v1.0.0 - Smart Database Population");
            logger.LogInformation("Mode: {Mode}", options.Mode);
            logger.LogInformation("Include Core: {IncludeCore}", options.IncludeCore);
            logger.LogInformation("Vertical: {Vertical}", options.Vertical);

            if (options.Generate)
            {
                logger.LogInformation("Schema Generation: Enabled (will run SQLServerSyncGenerator first)");
            }

            // Step 1: Ensure schema is up-to-date if --generate is specified
            if (options.Generate)
            {
                await EnsureSchemaIsCurrentAsync(options, logger, services);
            }

            // Step 2: Run the smart data generation based on mode
            return options.Mode.ToLower() switch
            {
                "create" => await HandleCreateModeAsync(options, logger, services),
                "update" => await HandleUpdateModeAsync(options, logger, services),
                _ => throw new ArgumentException($"Invalid mode: {options.Mode}")
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Test data generation failed: {Message}", ex.Message);
            return 1;
        }
        finally
        {
            services.Dispose();
        }
    }

    private static async Task EnsureSchemaIsCurrentAsync(TestDataOptions options, ILogger logger, ServiceProvider services)
    {
        logger.LogInformation("=== SCHEMA GENERATION PHASE ===");
        logger.LogInformation("Running SQLServerSyncGenerator to ensure database schema is current...");

        var sqlSyncOrchestrator = services.GetRequiredService<SqlServerSyncOrchestrator>();

        // Drop/truncate tables if this is create mode to start fresh
        if (options.Mode.ToLower() == "create")
        {
            logger.LogWarning("CREATE MODE: Will truncate existing data to start fresh");
            await sqlSyncOrchestrator.TruncateTablesAsync(options);
        }

        // Run the SQL schema generator
        var schemaResult = await sqlSyncOrchestrator.RunSchemaGeneratorAsync(options);

        if (schemaResult != 0)
        {
            throw new InvalidOperationException("Schema generation failed. Cannot proceed with data generation.");
        }

        logger.LogInformation("✅ Schema generation completed successfully");
    }

    private static async Task<int> HandleCreateModeAsync(TestDataOptions options, ILogger logger, ServiceProvider services)
    {
        logger.LogInformation("=== CREATE MODE: Fresh Realistic Data ===");

        var dataPopulator = services.GetRequiredService<DataPopulator>();
        var constraintValidator = services.GetRequiredService<ConstraintValidator>();

        // Generate fresh realistic data
        var populationResult = await dataPopulator.PopulateAllTablesAsync(options);

        // Run constraint validation if bad data config provided
        if (!string.IsNullOrEmpty(options.BadDataConfig))
        {
            logger.LogInformation("=== CONSTRAINT VALIDATION ===");
            var validationResults = await constraintValidator.ValidateConstraintsAsync(options.BadDataConfig, options);
            LogConstraintValidationResults(validationResults, logger);
        }

        logger.LogInformation("✅ CREATE mode completed successfully");
        return 0;
    }

    private static async Task<int> HandleUpdateModeAsync(TestDataOptions options, ILogger logger, ServiceProvider services)
    {
        logger.LogInformation("=== UPDATE MODE: Smart Column Population ===");

        var schemaAnalyzer = services.GetRequiredService<SchemaAnalyzer>();
        var dataPopulator = services.GetRequiredService<DataPopulator>();
        var constraintValidator = services.GetRequiredService<ConstraintValidator>();

        // Analyze schema changes (new columns/tables)
        var schemaChanges = await schemaAnalyzer.DetectSchemaChangesAsync(options);

        logger.LogInformation("Schema analysis found: {NewColumns} new columns, {NewTables} new tables",
            schemaChanges.NewColumns.Count, schemaChanges.NewTables.Count);

        if (schemaChanges.HasChanges)
        {
            // Surgically populate only new additions
            await dataPopulator.PopulateSchemaChangesAsync(schemaChanges, options);
        }
        else
        {
            logger.LogInformation("No schema changes detected - database is already up-to-date");
        }

        // Run constraint validation if bad data config provided
        if (!string.IsNullOrEmpty(options.BadDataConfig))
        {
            logger.LogInformation("=== CONSTRAINT VALIDATION ===");
            var validationResults = await constraintValidator.ValidateConstraintsAsync(options.BadDataConfig, options);
            LogConstraintValidationResults(validationResults, logger);
        }

        logger.LogInformation("✅ UPDATE mode completed successfully");
        return 0;
    }

    private static void LogConstraintValidationResults(ConstraintValidationResults results, ILogger logger)
    {
        logger.LogInformation("=== CONSTRAINT VALIDATION RESULTS ===");
        logger.LogInformation("Total Tests: {Total}", results.TotalTests);
        logger.LogInformation("Passed: {Passed} ({PassPercent:F1}%)", results.PassedTests, results.PassPercentage);
        logger.LogInformation("Failed: {Failed} ({FailPercent:F1}%)", results.FailedTests, results.FailPercentage);

        foreach (var failure in results.Failures)
        {
            logger.LogWarning("⚠️ CONSTRAINT WEAKNESS: {Message}", failure.Message);
            logger.LogWarning("   Action Required: {Action}", failure.ActionRequired);
        }

        foreach (var success in results.Successes.Take(3)) // Show first 3 successes
        {
            logger.LogInformation("✅ {Message}", success.Message);
        }

        if (results.Successes.Count > 3)
        {
            logger.LogInformation("... and {More} more successful constraint validations", results.Successes.Count - 3);
        }
    }
}

public class TestDataOptions
{
    [Option("mode", Required = true, HelpText = "Generation mode: 'create' (fresh data) or 'update' (new columns only)")]
    public string Mode { get; set; } = string.Empty;

    [Option("includeCore", Required = false, Default = false, HelpText = "Include Core schema in data generation")]
    public bool IncludeCore { get; set; }

    [Option("vertical", Required = true, HelpText = "Target vertical schema (Photography, Fishing, Hunting, etc.)")]
    public string Vertical { get; set; } = string.Empty;

    [Option("generate", Required = false, Default = false, HelpText = "Run SQLServerSyncGenerator first to ensure schema is current")]
    public bool Generate { get; set; }

    [Option("bad-data", Required = false, HelpText = "Path to JSON file containing bad data scenarios for constraint validation")]
    public string? BadDataConfig { get; set; }

    [Option("connection-string", Required = false, HelpText = "SQL Server connection string")]
    public string? ConnectionString { get; set; }

    [Option("output", Required = false, HelpText = "Output SQLite database path (for SQLite mode)")]
    public string? OutputPath { get; set; }

    [Option("server", Required = false, HelpText = "SQL Server name")]
    public string? Server { get; set; }

    [Option("database", Required = false, HelpText = "Database name")]
    public string? Database { get; set; }

    [Option("local", Required = false, Default = false, HelpText = "Use local SQL Server with Windows Authentication")]
    public bool UseLocal { get; set; }

    [Option("keyvault-url", Required = false, HelpText = "Azure Key Vault URL")]
    public string? KeyVaultUrl { get; set; }

    [Option("username-secret", Required = false, HelpText = "Key Vault secret name for username")]
    public string? UsernameSecret { get; set; }

    [Option("password-secret", Required = false, HelpText = "Key Vault secret name for password")]
    public string? PasswordSecret { get; set; }

    [Option("verbose", Required = false, Default = false, HelpText = "Enable verbose logging")]
    public bool Verbose { get; set; }

    [Option("volume", Required = false, Default = "medium", HelpText = "Data volume: small, medium, large")]
    public string Volume { get; set; } = "medium";
}