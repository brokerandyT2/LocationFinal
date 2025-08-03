using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using x3squaredcircles.Version.Calculator.Services;

namespace x3squaredcircles.Version.Calculator;

class Program
{
    static async Task<int> Main(string[] args)
    {
        try
        {
            var parseResult = Parser.Default.ParseArguments<VersionDetectiveOptions>(args);

            return await parseResult.MapResult(
                async options => await RunVersionDetectiveAsync(options),
                _ => Task.FromResult(1)
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unhandled exception: {ex.Message}");
            return 1;
        }
    }

    static async Task<int> RunVersionDetectiveAsync(VersionDetectiveOptions options)
    {
        var isVerbose = options.Verbose ?? !options.IsProduction;

        var services = new ServiceCollection()
            .AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(isVerbose ? LogLevel.Debug : LogLevel.Information);
            })
            .AddSingleton<SolutionDetector>()
            .AddSingleton<GitAnalyzer>()
            .AddSingleton<SchemaChangeDetector>()
            .AddSingleton<QuantitativeAnalyzer>()
            .AddSingleton<VersionCalculator>()
            .AddSingleton<TagManager>()
            .AddSingleton<ReleaseNotesGenerator>()
            .AddSingleton<WikiPublisher>()
            .BuildServiceProvider();

        var logger = services.GetRequiredService<ILogger<Program>>();

        try
        {
            logger.LogInformation("Version Detective v1.0.0 - Automated versioning for .NET solutions");

            // Handle publish mode
            if (options.Publish)
            {
                return await HandlePublishModeAsync(options, services, logger);
            }

            logger.LogInformation("Branch: {Branch}", options.Branch);
            logger.LogInformation("Mode: {Mode}", options.Mode);

            var solutionDetector = services.GetRequiredService<SolutionDetector>();
            var gitAnalyzer = services.GetRequiredService<GitAnalyzer>();
            var schemaDetector = services.GetRequiredService<SchemaChangeDetector>();
            var quantitativeAnalyzer = services.GetRequiredService<QuantitativeAnalyzer>();
            var versionCalculator = services.GetRequiredService<VersionCalculator>();

            // 1. Auto-detect solution type and projects
            logger.LogInformation("Detecting solution...");
            var solution = await solutionDetector.DetectSolutionAsync();
            logger.LogInformation("Solution: {Name} ({Type})", solution.Name, solution.Type);

            // 2. Find baseline (last tag)
            logger.LogInformation("Finding baseline version...");
            var baseline = await gitAnalyzer.GetLastTagAsync();
            logger.LogInformation("Baseline: {Baseline}", baseline ?? "No previous tags");

            // 3. Analyze changes since baseline
            logger.LogInformation("Analyzing changes since baseline...");
            var gitChanges = await gitAnalyzer.GetChangesSinceAsync(baseline);
            logger.LogInformation("Found {Count} changed files", gitChanges.Count);

            // 4. Detect schema changes (major version driver)
            logger.LogInformation("Detecting schema changes...");
            var schemaChanges = await schemaDetector.DetectSchemaChangesAsync(solution, baseline);
            if (schemaChanges.Any())
            {
                logger.LogInformation("Schema changes detected: {Changes}",
                    string.Join(", ", schemaChanges.Select(c => c.Description)));
            }

            // 5. Analyze quantitative changes
            logger.LogInformation("Analyzing quantitative changes...");
            var quantitativeChanges = await quantitativeAnalyzer.AnalyzeChangesAsync(gitChanges, solution);

            // 6. Calculate version impact
            logger.LogInformation("Calculating version impact...");
            var versionImpact = versionCalculator.CalculateVersions(
                baseline, schemaChanges, quantitativeChanges, solution);

            // 7. Output results
            await OutputResultsAsync(versionImpact, options, logger);

            // 8. Execute deployment actions if in deploy mode
            if (options.Mode == "deploy" && options.Branch == "main")
            {
                await ExecuteDeploymentAsync(versionImpact, services, logger);

                // Auto-publish to wiki in production mode
                if (options.IsProduction && !string.IsNullOrEmpty(options.WikiUrl))
                {
                    await PublishToWikiAsync(versionImpact, solution, services, options.WikiUrl, logger);
                }
            }

            return 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Version Detective failed: {Message}", ex.Message);
            return 1;
        }
        finally
        {
            services.Dispose();
        }
    }

    private static async Task OutputResultsAsync(VersionImpact versionImpact, VersionDetectiveOptions options, ILogger logger)
    {
        Console.WriteLine();
        Console.WriteLine("🏷️ Version Detective Analysis");
        if (options.Mode == "pr") Console.WriteLine("(Preliminary - final version determined at deployment)");
        Console.WriteLine();

        Console.WriteLine($"Current Version: {versionImpact.CurrentVersion}");
        Console.WriteLine($"Semantic Version: {versionImpact.SemanticVersion}");
        Console.WriteLine($"Marketing Version: {versionImpact.MarketingVersion}");
        Console.WriteLine();

        if (versionImpact.HasSchemaChanges)
        {
            Console.WriteLine("🔄 MAJOR: Database schema changes detected");
            foreach (var change in versionImpact.SchemaChanges)
            {
                Console.WriteLine($"  - {change.Type}: {change.Description}");
            }
            Console.WriteLine();
        }

        if (versionImpact.MinorChanges > 0)
        {
            Console.WriteLine($"✨ MINOR: {versionImpact.MinorChanges} new features added");
            Console.WriteLine($"  - {versionImpact.NewViewModels} new ViewModels");
            Console.WriteLine($"  - {versionImpact.NewApiEndpoints} new API endpoints");
            Console.WriteLine($"  - {versionImpact.NewServices} new services");
            Console.WriteLine();
        }

        if (versionImpact.PatchChanges > 0)
        {
            Console.WriteLine($"🐛 PATCH: {versionImpact.PatchChanges} improvements made");
            Console.WriteLine($"  - {versionImpact.BugFixes} bug fixes");
            Console.WriteLine($"  - {versionImpact.PerformanceImprovements} performance improvements");
            Console.WriteLine();
        }

        Console.WriteLine($"Reasoning: {versionImpact.Reasoning}");
    }

    private static async Task ExecuteDeploymentAsync(VersionImpact versionImpact, ServiceProvider services, ILogger logger)
    {
        logger.LogInformation("Executing deployment actions...");

        var tagManager = services.GetRequiredService<TagManager>();
        var releaseNotesGenerator = services.GetRequiredService<ReleaseNotesGenerator>();

        // Create git tags
        await tagManager.CreateSemanticTagAsync(versionImpact.SemanticVersion);
        await tagManager.CreateMarketingTagAsync(versionImpact.MarketingVersion);
        await tagManager.CreateBuildTagAsync(versionImpact.SemanticVersion);

        // Update assembly versions
        await UpdateAssemblyVersionsAsync(versionImpact, logger);

        // Generate release notes
        await releaseNotesGenerator.GenerateReleaseNotesAsync(versionImpact);

        logger.LogInformation("Deployment completed successfully!");
    }

    private static async Task<int> HandlePublishModeAsync(VersionDetectiveOptions options, ServiceProvider services, ILogger logger)
    {
        if (string.IsNullOrEmpty(options.WikiUrl))
        {
            logger.LogError("Wiki URL is required for publish mode. Use --wiki <url>");
            return 1;
        }

        logger.LogInformation("Publishing release notes to wiki: {WikiUrl}", options.WikiUrl);

        try
        {
            var wikiPublisher = services.GetRequiredService<WikiPublisher>();

            // Find the most recent release notes file
            var releaseNotesFiles = Directory.GetFiles(".", "RELEASE_NOTES_v*.md")
                .OrderByDescending(f => File.GetCreationTime(f))
                .ToList();

            if (!releaseNotesFiles.Any())
            {
                logger.LogError("No release notes files found. Expected format: RELEASE_NOTES_v{version}.md");
                return 1;
            }

            var latestFile = releaseNotesFiles.First();
            logger.LogInformation("Publishing release notes from: {FileName}", Path.GetFileName(latestFile));

            var success = await wikiPublisher.PublishFromFileAsync(options.WikiUrl, latestFile);

            if (success)
            {
                logger.LogInformation("Successfully published release notes to wiki");
                return 0;
            }
            else
            {
                logger.LogError("Failed to publish release notes to wiki");
                return 1;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception during wiki publishing");
            return 1;
        }
    }

    private static async Task PublishToWikiAsync(VersionImpact versionImpact, Solution solution, ServiceProvider services, string wikiUrl, ILogger logger)
    {
        try
        {
            logger.LogInformation("Auto-publishing release notes to wiki (production mode)");

            var wikiPublisher = services.GetRequiredService<WikiPublisher>();
            var releaseNotesGenerator = services.GetRequiredService<ReleaseNotesGenerator>();

            // Generate release notes content
            var releaseNotesContent = await releaseNotesGenerator.GenerateReleaseNotesAsync(versionImpact);

            // Publish to wiki
            var success = await wikiPublisher.PublishReleaseNotesAsync(wikiUrl, versionImpact, solution, releaseNotesContent);

            if (success)
            {
                logger.LogInformation("Successfully auto-published release notes to wiki");
            }
            else
            {
                logger.LogWarning("Failed to auto-publish release notes to wiki (non-critical)");
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Exception during auto wiki publishing (non-critical)");
        }
    }

    private static async Task UpdateAssemblyVersionsAsync(VersionImpact versionImpact, ILogger logger)
    {
        logger.LogInformation("Updating assembly versions...");
        // TODO: Update all .csproj files in solution with new version
    }
}

public class VersionDetectiveOptions
{
    [Option("branch", Required = true, HelpText = "Target branch (beta/main)")]
    public string Branch { get; set; } = string.Empty;

    [Option("mode", Default = "pr", HelpText = "Mode: pr (analyze only) or deploy (full deployment)")]
    public string Mode { get; set; } = "pr";

    [Option("from", HelpText = "Analyze changes from specific commit/tag")]
    public string? From { get; set; }

    [Option("verbose", HelpText = "Enable verbose logging")]
    public bool? Verbose { get; set; }

    [Option("prod", Default = false, HelpText = "Production mode")]
    public bool IsProduction { get; set; }

    [Option("validate-only", Default = false, HelpText = "Validation-only mode")]
    public bool ValidateOnly { get; set; }

    [Option("publish", Default = false, HelpText = "Publish mode - publish existing release notes to wiki")]
    public bool Publish { get; set; }

    [Option("wiki", HelpText = "Wiki URL for publishing release notes")]
    public string? WikiUrl { get; set; }
}

// Data models
public record Solution
{
    public string Name { get; init; } = "";
    public string Type { get; init; } = ""; // "core" or "vertical"
    public string RootPath { get; init; } = "";
    public List<Project> Projects { get; init; } = new();
    public Project? DomainProject { get; init; }
}

public record Project
{
    public string Name { get; init; } = "";
    public string Path { get; init; } = "";
    public bool IsDomain { get; init; }
    public string CurrentVersion { get; init; } = "";
}

public record GitChange
{
    public string FilePath { get; init; } = "";
    public string ChangeType { get; init; } = ""; // Added, Modified, Deleted
    public string Content { get; init; } = "";
}

public record SchemaChange
{
    public string Type { get; init; } = ""; // NewEntity, RemovedEntity, etc.
    public string Description { get; init; } = "";
    public string EntityName { get; init; } = "";
}

public record QuantitativeChanges
{
    public int NewViewModels { get; init; }
    public int NewApiEndpoints { get; init; }
    public int NewServices { get; init; }
    public int BugFixes { get; init; }
    public int PerformanceImprovements { get; init; }
    public int Documentation { get; init; }
}

public record VersionImpact
{
    public string CurrentVersion { get; init; } = "";
    public string SemanticVersion { get; init; } = "";
    public string MarketingVersion { get; init; } = "";
    public bool HasSchemaChanges { get; init; }
    public List<SchemaChange> SchemaChanges { get; init; } = new();
    public int MinorChanges { get; init; }
    public int PatchChanges { get; init; }
    public int NewViewModels { get; init; }
    public int NewApiEndpoints { get; init; }
    public int NewServices { get; init; }
    public int BugFixes { get; init; }
    public int PerformanceImprovements { get; init; }
    public string Reasoning { get; init; } = "";
}