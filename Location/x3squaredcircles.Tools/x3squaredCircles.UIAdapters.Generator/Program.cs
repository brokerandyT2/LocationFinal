using CommandLine;
using x3squaredcirecles.Adapter.Generator.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace x3squaredcirecles.Adapter.Generator;

class Program
{
    static async Task<int> Main(string[] args)
    {
        try
        {
            // Parse command line arguments with optional parameters
            var parseResult = Parser.Default.ParseArguments<GeneratorOptions>(args);

            return await parseResult.MapResult(async options => await RunGeneratorAsync(options),
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
        // Setup dependency injection and logging based on verbose flag
        var services = new ServiceCollection()
            .AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(options.Verbose ? LogLevel.Debug : LogLevel.Information);
            })
            .AddSingleton<AssemblyLoader>()
            .AddSingleton<ViewModelAnalyzer>()
            .AddSingleton<TypeTranslator>()
            .AddSingleton<AndroidAdapterGenerator>()
            .AddSingleton<IOSAdapterGenerator>()
            .AddSingleton<TemplateProcessor>() // Updated constructor signature
            .BuildServiceProvider();

        var logger = services.GetRequiredService<ILogger<Program>>();

        try
        {
            // Calculate solution root output path if using default
            string actualOutputPath = options.OutputPath;
            if (options.OutputPath == "Kotlin-Adapters") // Using default
            {
                var current = new DirectoryInfo(Directory.GetCurrentDirectory());
                var parent = current.ToString();
                var solutionRoot = parent.Replace("bin\\Debug\\net9.0", "").Replace("PhotographyAdapterGenerator\\", "");
                actualOutputPath = Path.Combine(solutionRoot, "Kotlin-Adapters");
            }

            // NEW: Enhanced version logging with attribute support
            logger.LogInformation("Photography ViewModel Generator v1.2.0 (with attribute support)");
            logger.LogInformation("Platform: {Platform}", options.Platform);
            logger.LogInformation("Base Output: {OutputPath}", actualOutputPath);

            // Validate platform
            if (!IsValidPlatform(options.Platform))
            {
                logger.LogError("Invalid platform: {Platform}. Supported platforms: android, ios, both", options.Platform);
                return 1;
            }

            // Step 1: Load ViewModels from both Core and Photography assemblies
            logger.LogInformation("Loading ViewModels from Core and Photography assemblies...");
            var assemblyLoader = services.GetRequiredService<AssemblyLoader>();
            var assemblyPaths = await assemblyLoader.LoadViewModelAssemblyPathsAsync(options);

            if (assemblyPaths.Count == 0)
            {
                logger.LogError("No ViewModel assemblies found. Make sure projects are built.");
                return 1;
            }

            // Step 2: Analyze all ViewModels
            logger.LogInformation("Analyzing {Count} assemblies for ViewModels...", assemblyPaths.Count);
            var analyzer = services.GetRequiredService<ViewModelAnalyzer>();
            var viewModels = await analyzer.AnalyzeAssembliesAsync(assemblyPaths);

            if (viewModels.Count == 0)
            {
                logger.LogError("No ViewModels found in loaded assemblies.");
                return 1;
            }

            logger.LogInformation("Found {CoreCount} Core ViewModels and {PhotoCount} Photography ViewModels",
                viewModels.Count(vm => vm.Source == "Core"),
                viewModels.Count(vm => vm.Source == "Photography"));

            // NEW: Log attribute usage statistics
            LogAttributeUsageStatistics(logger, viewModels);

            // Step 3: Generate adapters for specified platform(s)
            var templateProcessor = services.GetRequiredService<TemplateProcessor>();
            var platformsToGenerate = GetPlatformsToGenerate(options.Platform);
            var totalGenerated = 0;

            foreach (var targetPlatform in platformsToGenerate)
            {
                await GenerateForPlatform(targetPlatform, actualOutputPath, viewModels, options, templateProcessor, logger);
                totalGenerated += viewModels.Count;
            }

            logger.LogInformation("Generation completed successfully! Generated {Count} total adapters across {PlatformCount} platform(s).",
                totalGenerated, platformsToGenerate.Count);
            return 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Generation failed: {Message}", ex.Message);
            return 1;
        }
        finally
        {
            services.Dispose();
        }
    }

    // NEW: Log statistics about attribute usage
    static void LogAttributeUsageStatistics(ILogger logger, List<ViewModelMetadata> viewModels)
    {
        var totalViewModels = viewModels.Count;
        var viewModelsWithAttributes = viewModels.Count(vm =>
            vm.AvailableAttribute != null || vm.ExcludeAttribute != null || vm.GenerateAsAttribute != null ||
            vm.Properties.Any(p => HasAnyAttribute(p)) ||
            vm.Commands.Any(c => HasAnyCommandAttribute(c)));

        var propertiesWithMapTo = viewModels.SelectMany(vm => vm.Properties).Count(p => p.MapToAttribute != null);
        var propertiesWithDateType = viewModels.SelectMany(vm => vm.Properties).Count(p => p.DateTypeAttribute != null);
        var propertiesWithAvailable = viewModels.SelectMany(vm => vm.Properties).Count(p => p.AvailableAttribute != null);
        var propertiesWithExclude = viewModels.SelectMany(vm => vm.Properties).Count(p => p.ExcludeAttribute != null);

        if (viewModelsWithAttributes > 0)
        {
            logger.LogInformation("Attribute Usage Statistics:");
            logger.LogInformation("  ViewModels using attributes: {Count}/{Total} ({Percentage:F1}%)",
                viewModelsWithAttributes, totalViewModels, (viewModelsWithAttributes * 100.0 / totalViewModels));

            if (propertiesWithMapTo > 0)
                logger.LogInformation("  Properties with [MapTo]: {Count}", propertiesWithMapTo);

            if (propertiesWithDateType > 0)
                logger.LogInformation("  Properties with [DateType]: {Count}", propertiesWithDateType);

            if (propertiesWithAvailable > 0)
                logger.LogInformation("  Properties with [Available]: {Count}", propertiesWithAvailable);

            if (propertiesWithExclude > 0)
                logger.LogInformation("  Properties with [Exclude]: {Count}", propertiesWithExclude);
        }
        else
        {
            logger.LogInformation("No custom attributes detected - using default generation for all ViewModels");
        }
    }

    // NEW: Helper method to check if property has any attributes
    static bool HasAnyAttribute(PropertyMetadata property)
    {
        return property.MapToAttribute != null ||
               property.DateTypeAttribute != null ||
               property.AvailableAttribute != null ||
               property.ExcludeAttribute != null ||
               property.GenerateAsAttribute != null ||
               property.WarnCustomImplementationNeededAttribute != null ||
               property.CommandBehaviorAttribute != null ||
               property.CollectionBehaviorAttribute != null ||
               property.ValidationBehaviorAttribute != null ||
               property.ThreadingBehaviorAttribute != null;
    }

    // NEW: Helper method to check if command has any attributes
    static bool HasAnyCommandAttribute(CommandMetadata command)
    {
        return command.AvailableAttribute != null ||
               command.ExcludeAttribute != null ||
               command.GenerateAsAttribute != null ||
               command.CommandBehaviorAttribute != null ||
               command.ThreadingBehaviorAttribute != null;
    }

    static List<string> GetPlatformsToGenerate(string platform)
    {
        return platform.ToLower() switch
        {
            "both" => new List<string> { "android", "ios" },
            "android" => new List<string> { "android" },
            "ios" => new List<string> { "ios" },
            _ => new List<string> { "android" } // fallback
        };
    }

    static async Task GenerateForPlatform(
        string targetPlatform,
        string baseOutputPath,
        List<ViewModelMetadata> viewModels,
        GeneratorOptions originalOptions,
        TemplateProcessor templateProcessor,
        ILogger logger)
    {
        // Create platform-specific output path
        var platformOutputPath = Path.Combine(baseOutputPath, targetPlatform == "android" ? "android" : "iOS");

        logger.LogInformation("Generating {Platform} adapters to {OutputPath}...", targetPlatform, platformOutputPath);

        // Create and clean platform-specific output directory
        if (Directory.Exists(platformOutputPath))
        {
            logger.LogInformation("Cleaning existing {Platform} output directory: {OutputDir}", targetPlatform, platformOutputPath);

            // Clean out old generated adapter files for this platform
            var filePattern = targetPlatform == "android" ? "*Adapter.kt" : "*Adapter.swift";
            var existingFiles = Directory.GetFiles(platformOutputPath, filePattern).ToList();

            foreach (var file in existingFiles)
            {
                File.Delete(file);
                logger.LogDebug("Deleted old {Platform} adapter file: {FileName}", targetPlatform, Path.GetFileName(file));
            }

            if (existingFiles.Any())
            {
                logger.LogInformation("Cleaned {Count} old {Platform} adapter files", existingFiles.Count, targetPlatform);
            }
        }
        else
        {
            logger.LogInformation("Creating {Platform} output directory: {OutputDir}", targetPlatform, platformOutputPath);
            Directory.CreateDirectory(platformOutputPath);
        }

        // Create platform-specific options
        var platformOptions = new GeneratorOptions
        {
            Platform = targetPlatform,
            OutputPath = platformOutputPath,
            Verbose = originalOptions.Verbose,
            CoreAssemblyPath = originalOptions.CoreAssemblyPath,
            PhotographyAssemblyPath = originalOptions.PhotographyAssemblyPath
        };

        // Generate adapters for this platform
        await templateProcessor.GenerateAdaptersAsync(viewModels, platformOptions);

        logger.LogInformation("Completed {Platform} generation: {Count} adapters", targetPlatform, viewModels.Count);
    }

    static bool IsValidPlatform(string platform)
    {
        return platform.ToLower() is "android" or "ios" or "both";
    }
}

public class GeneratorOptions
{
    [Option("platform", Required = false, HelpText = "Target platform (android, ios, both). Default: both")]
    public string Platform { get; set; } = "both";

    [Option("output", Required = false, Default = "Kotlin-Adapters", HelpText = "Output directory path")]
    public string OutputPath { get; set; } = "Kotlin-Adapters";

    [Option("verbose", Required = false, Default = false, HelpText = "Enable verbose logging")]
    public bool Verbose { get; set; }

    [Option("core-assembly", Required = false, HelpText = "Path to Location.Core.ViewModels.dll")]
    public string? CoreAssemblyPath { get; set; }

    [Option("photography-assembly", Required = false, HelpText = "Path to Location.Photography.ViewModels.dll")]
    public string? PhotographyAssemblyPath { get; set; }
}