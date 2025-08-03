using CommandLine;
using x3squaredcirecles.API.Generator.APIGenerator.Models;
using x3squaredcirecles.API.Generator.APIGenerator.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace x3squaredcirecles.API.Generator.APIGenerator;

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
            .AddSingleton<BuildService>()
            .AddSingleton<AzureDeploymentService>()
            .AddSingleton<AzureArtifactsService>()
            .AddSingleton<ConnectionStringBuilder>()
            .BuildServiceProvider();

        var logger = services.GetRequiredService<ILogger<Program>>();

        try
        {
            logger.LogInformation("Location API Generator v1.0.0 - Complete Pipeline");
            logger.LogInformation("Mode: {Mode}", options.AutoDiscover ? "Auto-Discover" : "Manual Override");

            if (!options.AutoDiscover && !string.IsNullOrEmpty(options.ManualExtractors))
            {
                LogManualOverrideWarnings(logger, options);
            }

            logger.LogInformation("Step 1: Discovering Infrastructure assembly...");
            var discoveryService = services.GetRequiredService<AssemblyDiscoveryService>();
            var assemblyInfo = await discoveryService.DiscoverInfrastructureAssemblyAsync(options);
            logger.LogInformation("Discovered: {Vertical} v{Version}", assemblyInfo.Vertical, assemblyInfo.MajorVersion);

            logger.LogInformation("Step 2: Analyzing entities with [ExportToSQL] attribute...");
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

            logger.LogInformation("Step 3: Building SQL Server connection...");
            var connectionBuilder = services.GetRequiredService<ConnectionStringBuilder>();
            var connectionString = await connectionBuilder.BuildConnectionStringAsync(options);

            logger.LogInformation("Step 4: Generating Function App source code...");
            var functionGenerator = services.GetRequiredService<FunctionGeneratorService>();
            var generatedAssets = await functionGenerator.GenerateAPIAssetsAsync(assemblyInfo, extractableEntities, options);

            logger.LogInformation("Step 5: Building Function App binary...");
            var buildService = services.GetRequiredService<BuildService>();
            var compiledBinaryPath = await buildService.BuildFunctionAppAsync(assemblyInfo, generatedAssets, options);

            if (!options.NoOp)
            {
                logger.LogInformation("Step 6: Deploying to Azure...");
                var deploymentService = services.GetRequiredService<AzureDeploymentService>();

                var tempZipPath = Path.Combine(Path.GetTempPath(), $"{generatedAssets.FunctionAppName}.zip");
                System.IO.Compression.ZipFile.CreateFromDirectory(compiledBinaryPath, tempZipPath);

                try
                {
                    options.FunctionCodePath = tempZipPath;
                    var updatedOptions = options;

                    await deploymentService.DeployFunctionAppAsync(assemblyInfo, generatedAssets, updatedOptions);
                }
                finally
                {
                    if (File.Exists(tempZipPath))
                    {
                        File.Delete(tempZipPath);
                    }
                }

                if (!options.SkipArtifacts)
                {
                    logger.LogInformation("Step 7: Publishing to Azure Artifacts...");
                    var artifactsService = services.GetRequiredService<AzureArtifactsService>();
                    var artifactSuccess = await artifactsService.TryPublishArtifactAsync(assemblyInfo, compiledBinaryPath, options);

                    if (!artifactSuccess)
                    {
                        logger.LogWarning("Artifact publishing failed, but deployment succeeded");
                    }
                }
                else
                {
                    logger.LogInformation("Step 7: Skipping Azure Artifacts (--skip-artifacts)");
                }
            }
            else
            {
                logger.LogInformation("No-Op Mode: Generated and compiled assets ready for review");
                LogGeneratedAssets(logger, generatedAssets, compiledBinaryPath);
            }

            logger.LogInformation("API generation pipeline completed successfully!");
            logger.LogInformation("Function App: {FunctionAppName}", generatedAssets.FunctionAppName);
            logger.LogInformation("Endpoints: {EndpointCount}", generatedAssets.GeneratedEndpoints.Count);

            return 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "API generation pipeline failed: {Message}", ex.Message);
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

    static void LogGeneratedAssets(ILogger logger, GeneratedAssets assets, string compiledBinaryPath)
    {
        logger.LogInformation("=== Generated Assets (No-Op Mode) ===");
        logger.LogInformation("Function App: {Name}", assets.FunctionAppName);
        logger.LogInformation("Controllers: {Count}", assets.Controllers.Count);
        logger.LogInformation("Endpoints: {Endpoints}", string.Join(", ", assets.GeneratedEndpoints));
        logger.LogInformation("Compiled Binary: {BinaryPath}", compiledBinaryPath);
        logger.LogInformation("Bicep Template: {Size} chars", assets.BicepTemplate.Length);
    }
}