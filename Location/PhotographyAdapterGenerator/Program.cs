using CommandLine;
using Location.Photography.Tools.AdapterGenerator.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Location.Photography.Tools.AdapterGenerator;

class Program
{
    static async Task<int> Main(string[] args)
    {
        try
        {
            // Parse command line arguments first to check for verbose flag
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
            .AddSingleton<TemplateProcessor>()
            .AddSingleton<TypeTranslator>()
            .BuildServiceProvider();

        var logger = services.GetRequiredService<ILogger<Program>>();

        try
        {
            logger.LogInformation("Photography ViewModel Generator v1.0.0");
            logger.LogInformation("Platform: {Platform}", options.Platform);
            logger.LogInformation("Output: {OutputPath}", options.OutputPath);

            // Validate arguments
            if (!IsValidPlatform(options.Platform))
            {
                logger.LogError("Invalid platform: {Platform}. Supported platforms: android, ios", options.Platform);
                return 1;
            }

            var outputDir = Path.GetDirectoryName(options.OutputPath) ?? options.OutputPath;
            if (!Directory.Exists(outputDir))
            {
                logger.LogInformation("Creating output directory: {OutputDir}", outputDir);
                Directory.CreateDirectory(outputDir);
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

            // Step 3: Generate adapters
            logger.LogInformation("Generating {Platform} adapters to {OutputPath}...", options.Platform, options.OutputPath);
            var templateProcessor = services.GetRequiredService<TemplateProcessor>();
            await templateProcessor.GenerateAdaptersAsync(viewModels, options);

            logger.LogInformation("Generation completed successfully! Generated {Count} adapters.", viewModels.Count);
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

    static bool IsValidPlatform(string platform)
    {
        return platform.ToLower() is "android" or "ios";
    }
}

public class GeneratorOptions
{
    [Option("platform", Required = true, HelpText = "Target platform (android, ios)")]
    public string Platform { get; set; } = string.Empty;

    [Option("output", Required = true, HelpText = "Output directory path")]
    public string OutputPath { get; set; } = string.Empty;

    [Option("verbose", Required = false, HelpText = "Enable verbose logging")]
    public bool Verbose { get; set; }

    [Option("core-assembly", Required = false, HelpText = "Path to Location.Core.ViewModels.dll")]
    public string? CoreAssemblyPath { get; set; }

    [Option("photography-assembly", Required = false, HelpText = "Path to Location.Photography.ViewModels.dll")]
    public string? PhotographyAssemblyPath { get; set; }
}