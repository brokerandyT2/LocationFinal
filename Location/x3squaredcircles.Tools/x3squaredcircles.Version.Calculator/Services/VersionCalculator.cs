using Microsoft.Extensions.Logging;

namespace x3squaredcircles.Version.Calculator.Services;

public class VersionCalculator
{
    private readonly ILogger<VersionCalculator> _logger;

    public VersionCalculator(ILogger<VersionCalculator> logger)
    {
        _logger = logger;
    }

    public VersionImpact CalculateVersions(
        string? baseline,
        List<SchemaChange> schemaChanges,
        QuantitativeChanges quantitativeChanges,
        Solution solution)
    {
        _logger.LogInformation("Calculating version impact for solution: {Solution}", solution.Name);

        var currentVersion = GetCurrentVersion(baseline, solution);
        var semanticVersion = CalculateSemanticVersion(currentVersion, schemaChanges, quantitativeChanges);
        var marketingVersion = CalculateMarketingVersion(currentVersion, quantitativeChanges);
        var reasoning = GenerateReasoning(schemaChanges, quantitativeChanges);

        var minorChanges = quantitativeChanges.NewViewModels + quantitativeChanges.NewApiEndpoints + quantitativeChanges.NewServices;
        var patchChanges = quantitativeChanges.BugFixes + quantitativeChanges.PerformanceImprovements + quantitativeChanges.Documentation;

        var versionImpact = new VersionImpact
        {
            CurrentVersion = currentVersion,
            SemanticVersion = semanticVersion,
            MarketingVersion = marketingVersion,
            HasSchemaChanges = schemaChanges.Any(),
            SchemaChanges = schemaChanges,
            MinorChanges = minorChanges,
            PatchChanges = patchChanges,
            NewViewModels = quantitativeChanges.NewViewModels,
            NewApiEndpoints = quantitativeChanges.NewApiEndpoints,
            NewServices = quantitativeChanges.NewServices,
            BugFixes = quantitativeChanges.BugFixes,
            PerformanceImprovements = quantitativeChanges.PerformanceImprovements,
            Reasoning = reasoning
        };

        _logger.LogInformation("Version calculation complete: {Current} → {Semantic} / {Marketing}",
            currentVersion, semanticVersion, marketingVersion);

        return versionImpact;
    }

    private string GetCurrentVersion(string? baseline, Solution solution)
    {
        if (baseline != null)
        {
            // Extract version from git tag (remove 'v' prefix if present)
            var version = baseline.StartsWith("v") ? baseline[1..] : baseline;
            if (IsValidSemanticVersion(version))
            {
                return version;
            }
        }

        // Fallback to project version or default
        var projectVersion = solution.DomainProject?.CurrentVersion ?? solution.Projects.FirstOrDefault()?.CurrentVersion;
        return projectVersion ?? "1.0.0";
    }

    private string CalculateSemanticVersion(
        string currentVersion,
        List<SchemaChange> schemaChanges,
        QuantitativeChanges quantitativeChanges)
    {
        var version = ParseVersion(currentVersion);

        // Rule 1: Schema changes drive major version bumps
        if (schemaChanges.Any())
        {
            version = IncrementMajor(version);
            _logger.LogDebug("Major version bump due to schema changes");
            return FormatVersion(version);
        }

        // Rule 2: Quantitative minor version increments
        var minorIncrement = quantitativeChanges.NewViewModels +
                           quantitativeChanges.NewApiEndpoints +
                           quantitativeChanges.NewServices;

        if (minorIncrement > 0)
        {
            version = IncrementMinor(version, minorIncrement);
            _logger.LogDebug("Minor version bump: +{Increment}", minorIncrement);
            return FormatVersion(version);
        }

        // Rule 3: Quantitative patch version increments
        var patchIncrement = quantitativeChanges.BugFixes +
                           quantitativeChanges.PerformanceImprovements +
                           quantitativeChanges.Documentation;

        if (patchIncrement > 0)
        {
            version = IncrementPatch(version, patchIncrement);
            _logger.LogDebug("Patch version bump: +{Increment}", patchIncrement);
            return FormatVersion(version);
        }

        // No changes detected
        _logger.LogDebug("No version increment required");
        return FormatVersion(version);
    }

    private string CalculateMarketingVersion(string currentVersion, QuantitativeChanges quantitativeChanges)
    {
        var version = ParseVersion(currentVersion);

        // Marketing version reflects total volume of changes for business impact
        var totalMinorChanges = quantitativeChanges.NewViewModels +
                              quantitativeChanges.NewApiEndpoints +
                              quantitativeChanges.NewServices;

        var totalPatchChanges = quantitativeChanges.BugFixes +
                              quantitativeChanges.PerformanceImprovements +
                              quantitativeChanges.Documentation;

        // Add total volume to minor version for marketing impact
        if (totalMinorChanges > 0)
        {
            version = IncrementMinor(version, totalMinorChanges);
        }

        if (totalPatchChanges > 0)
        {
            version = IncrementPatch(version, totalPatchChanges);
        }

        return FormatVersion(version);
    }

    private string GenerateReasoning(List<SchemaChange> schemaChanges, QuantitativeChanges quantitativeChanges)
    {
        var reasons = new List<string>();

        if (schemaChanges.Any())
        {
            reasons.Add($"Database schema changes require major version bump ({schemaChanges.Count} changes)");
        }

        var minorTotal = quantitativeChanges.NewViewModels + quantitativeChanges.NewApiEndpoints + quantitativeChanges.NewServices;
        if (minorTotal > 0)
        {
            var details = new List<string>();
            if (quantitativeChanges.NewViewModels > 0) details.Add($"{quantitativeChanges.NewViewModels} ViewModels");
            if (quantitativeChanges.NewApiEndpoints > 0) details.Add($"{quantitativeChanges.NewApiEndpoints} API endpoints");
            if (quantitativeChanges.NewServices > 0) details.Add($"{quantitativeChanges.NewServices} services");

            reasons.Add($"New features added: {string.Join(", ", details)}");
        }

        var patchTotal = quantitativeChanges.BugFixes + quantitativeChanges.PerformanceImprovements + quantitativeChanges.Documentation;
        if (patchTotal > 0)
        {
            var details = new List<string>();
            if (quantitativeChanges.BugFixes > 0) details.Add($"{quantitativeChanges.BugFixes} bug fixes");
            if (quantitativeChanges.PerformanceImprovements > 0) details.Add($"{quantitativeChanges.PerformanceImprovements} performance improvements");
            if (quantitativeChanges.Documentation > 0) details.Add($"{quantitativeChanges.Documentation} documentation updates");

            reasons.Add($"Improvements made: {string.Join(", ", details)}");
        }

        if (!reasons.Any())
        {
            reasons.Add("No significant changes detected");
        }

        return string.Join(". ", reasons);
    }

    private (int major, int minor, int patch) ParseVersion(string version)
    {
        try
        {
            var parts = version.Split('.');
            var major = parts.Length > 0 ? int.Parse(parts[0]) : 1;
            var minor = parts.Length > 1 ? int.Parse(parts[1]) : 0;
            var patch = parts.Length > 2 ? int.Parse(parts[2]) : 0;

            return (major, minor, patch);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse version: {Version}, using 1.0.0", version);
            return (1, 0, 0);
        }
    }

    private (int major, int minor, int patch) IncrementMajor((int major, int minor, int patch) version)
    {
        return (version.major + 1, 0, 0);
    }

    private (int major, int minor, int patch) IncrementMinor((int major, int minor, int patch) version, int increment = 1)
    {
        return (version.major, version.minor + increment, 0);
    }

    private (int major, int minor, int patch) IncrementPatch((int major, int minor, int patch) version, int increment = 1)
    {
        return (version.major, version.minor, version.patch + increment);
    }

    private string FormatVersion((int major, int minor, int patch) version)
    {
        return $"{version.major}.{version.minor}.{version.patch}";
    }

    private bool IsValidSemanticVersion(string version)
    {
        var parts = version.Split('.');
        return parts.Length == 3 &&
               parts.All(part => int.TryParse(part, out _));
    }

    public VersionImpact CalculateFromCommitMessages(string? baseline, List<string> commitMessages, Solution solution)
    {
        _logger.LogInformation("Calculating version from {Count} commit messages", commitMessages.Count);

        var quantitativeChanges = AnalyzeCommitMessages(commitMessages);
        var schemaChanges = new List<SchemaChange>(); // No schema analysis from commits alone

        return CalculateVersions(baseline, schemaChanges, quantitativeChanges, solution);
    }

    private QuantitativeChanges AnalyzeCommitMessages(List<string> commitMessages)
    {
        var changes = new QuantitativeChanges();

        foreach (var message in commitMessages)
        {
            var lowerMessage = message.ToLowerInvariant();

            // Analyze commit message patterns
            if (ContainsPattern(lowerMessage, new[] { "feat:", "feature:", "add " }))
            {
                changes = changes with { NewServices = changes.NewServices + 1 };
            }

            if (ContainsPattern(lowerMessage, new[] { "fix:", "bug:", "hotfix:" }))
            {
                changes = changes with { BugFixes = changes.BugFixes + 1 };
            }

            if (ContainsPattern(lowerMessage, new[] { "perf:", "performance:", "optimize" }))
            {
                changes = changes with { PerformanceImprovements = changes.PerformanceImprovements + 1 };
            }

            if (ContainsPattern(lowerMessage, new[] { "docs:", "documentation:", "readme" }))
            {
                changes = changes with { Documentation = changes.Documentation + 1 };
            }

            if (ContainsPattern(lowerMessage, new[] { "viewmodel", "view model" }))
            {
                changes = changes with { NewViewModels = changes.NewViewModels + 1 };
            }

            if (ContainsPattern(lowerMessage, new[] { "api", "endpoint", "controller" }))
            {
                changes = changes with { NewApiEndpoints = changes.NewApiEndpoints + 1 };
            }
        }

        return changes;
    }

    private bool ContainsPattern(string text, string[] patterns)
    {
        return patterns.Any(pattern => text.Contains(pattern));
    }
}