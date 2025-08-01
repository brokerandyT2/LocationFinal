using CommandLine;
using Location.Tools.APIGenerator.Models;
using Location.Tools.APIGenerator.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Location.Tools.APIGenerator;

class Program
{
    static async Task<int> Main(string[] args)
    {
        try
        {
            var parseResult = Parser.Default.ParseArguments<GeneratorOptions>(args);

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

    static async Task<int> RunGeneratorAsync(GeneratorOptions options)
    {
        // Setup DI and logging
        var services = new ServiceCollection()
            .AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(options.Verbose ? LogLevel.Debug : LogLevel.Information);
            })
            .AddSingleton<AssemblyDiscoveryService>()
            .AddSingleton<EntityReflectionService>()
            .AddSingleton<EFCoreExtractionService>()
            .AddSingleton<FunctionGeneratorService>()
            .AddSingleton<AzureDeploymentService>()
            .AddSingleton<ConnectionStringBuilder>()
            .BuildServiceProvider();

        var logger = services.GetRequiredService<ILogger<Program>>();

        try
        {
            logger.LogInformation("Location API Generator v1.0.0");
            logger.LogInformation("Mode: {Mode}", options.AutoDiscover ? "Auto-Discover" : "Manual Override");

            if (!options.AutoDiscover && !string.IsNullOrEmpty(options.ManualExtractors))
            {
                LogManualOverrideWarnings(logger, options);
            }

            // Step 1: Discover Infrastructure DLL and extract vertical/version
            logger.LogInformation("Discovering Infrastructure assembly...");
            var discoveryService = services.GetRequiredService<AssemblyDiscoveryService>();
            var assemblyInfo = await discoveryService.DiscoverInfrastructureAssemblyAsync(options);

            logger.LogInformation("Discovered: {Vertical} v{Version}", assemblyInfo.Vertical, assemblyInfo.MajorVersion);

            // Step 2: Reflect entities and build extraction metadata
            logger.LogInformation("Analyzing entities with [ExportToSQL] attribute...");
            var reflectionService = services.GetRequiredService<EntityReflectionService>();
            var extractableEntities = await reflectionService.DiscoverExtractableEntitiesAsync(assemblyInfo, options);

            if (extractableEntities.Count == 0)
            {
                logger.LogError("No extractable entities found. Check [ExportToSQL] attributes or manual extractor list.");
                return 1;
            }

            logger.LogInformation("Found {Count} extractable entities: {Entities}",
                extractableEntities.Count,
                string.Join(", ", extractableEntities.Select(e => e.TableName)));

            // Step 3: Build connection string and test SQL Server connectivity
            logger.LogInformation("Building SQL Server connection...");
            var connectionBuilder = services.GetRequiredService<ConnectionStringBuilder>();
            var connectionString = await connectionBuilder.BuildConnectionStringAsync(options);

            // Step 4: Generate Azure Functions controllers and Bicep templates
            logger.LogInformation("Generating Azure Functions and infrastructure...");
            var functionGenerator = services.GetRequiredService<FunctionGeneratorService>();
            var generatedAssets = await functionGenerator.GenerateAPIAssetsAsync(assemblyInfo, extractableEntities, options);

            // Step 5: Deploy to Azure (if not --noop mode)
            if (!options.NoOp)
            {
                logger.LogInformation("Deploying to Azure...");
                var deploymentService = services.GetRequiredService<AzureDeploymentService>();
                await deploymentService.DeployFunctionAppAsync(assemblyInfo, generatedAssets, options);
            }
            else
            {
                logger.LogInformation("No-Op Mode: Generated assets ready for review");
                LogGeneratedAssets(logger, generatedAssets);
            }

            logger.LogInformation("API generation completed successfully!");
            return 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "API generation failed: {Message}", ex.Message);
            return 1;
        }
        finally
        {
            services.Dispose();
        }
    }

    static void LogManualOverrideWarnings(ILogger logger, GeneratorOptions options)
    {
        logger.LogWarning("⚠️  WARNING: Manual extractor override detected");
        logger.LogWarning("⚠️  Using manual table selection instead of [ExportToSQL] attributes");
        logger.LogWarning("⚠️  This bypasses schema validation and may cause deployment issues");
        logger.LogWarning("⚠️  Recommended: Use --auto-discover for production deployments");

        if (options.IgnoreExportAttribute)
        {
            logger.LogError("🚨 DANGER: Ignoring [ExportToSQL] attributes completely");
            logger.LogError("🚨 This may export tables not designed for SQL Server");
            logger.LogError("🚨 Schema mismatches and deployment failures are likely");
            logger.LogError("🚨 This should ONLY be used for debugging/development");
        }
    }

    static void LogGeneratedAssets(ILogger logger, GeneratedAssets assets)
    {
        logger.LogInformation("=== Generated Assets (No-Op Mode) ===");
        logger.LogInformation("Function App: {Name}", assets.FunctionAppName);
        logger.LogInformation("Controllers: {Count}", assets.Controllers.Count);
        logger.LogInformation("Bicep Template: {Size} chars", assets.BicepTemplate.Length);
        logger.LogInformation("Endpoints: {Endpoints}", string.Join(", ", assets.GeneratedEndpoints));
    }
}