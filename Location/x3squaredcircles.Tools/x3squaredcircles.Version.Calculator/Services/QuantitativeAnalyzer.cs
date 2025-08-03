using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace x3squaredcircles.Version.Calculator.Services;

public class QuantitativeAnalyzer
{
    private readonly ILogger<QuantitativeAnalyzer> _logger;

    public QuantitativeAnalyzer(ILogger<QuantitativeAnalyzer> logger)
    {
        _logger = logger;
    }

    public async Task<QuantitativeChanges> AnalyzeChangesAsync(List<GitChange> gitChanges, Solution solution)
    {
        _logger.LogInformation("Analyzing quantitative changes for {Count} changed files", gitChanges.Count);

        var changes = new QuantitativeChanges();

        foreach (var gitChange in gitChanges)
        {
            try
            {
                await AnalyzeFileChangeAsync(gitChange, changes);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to analyze file change: {FilePath}", gitChange.FilePath);
            }
        }

        _logger.LogInformation("Quantitative analysis complete: {Minor} minor, {Patch} patch changes",
            GetMinorTotal(changes), GetPatchTotal(changes));

        return changes;
    }

    private async Task AnalyzeFileChangeAsync(GitChange gitChange, QuantitativeChanges changes)
    {
        var fileName = Path.GetFileName(gitChange.FilePath);
        var fileContent = gitChange.Content;

        // Skip non-C# files
        if (!gitChange.FilePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        // Analyze based on file patterns and content
        await AnalyzeViewModelChangesAsync(gitChange, changes);
        await AnalyzeApiEndpointChangesAsync(gitChange, changes);
        await AnalyzeServiceChangesAsync(gitChange, changes);
        await AnalyzeBugFixesAsync(gitChange, changes);
        await AnalyzePerformanceImprovementsAsync(gitChange, changes);
        await AnalyzeDocumentationAsync(gitChange, changes);
    }

    private async Task AnalyzeViewModelChangesAsync(GitChange gitChange, QuantitativeChanges changes)
    {
        if (!gitChange.FilePath.Contains("ViewModel", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (gitChange.ChangeType == "Added")
        {
            // New ViewModel file = +1 minor
            changes = changes with { NewViewModels = changes.NewViewModels + 1 };
            _logger.LogDebug("New ViewModel detected: {FilePath}", gitChange.FilePath);
        }
        else if (gitChange.ChangeType == "Modified")
        {
            // Check for new public properties in existing ViewModels
            var newProperties = CountNewPublicProperties(gitChange.Content);
            if (newProperties > 0)
            {
                changes = changes with { NewViewModels = changes.NewViewModels + newProperties };
                _logger.LogDebug("New ViewModel properties detected: {Count} in {FilePath}", newProperties, gitChange.FilePath);
            }
        }
    }

    private async Task AnalyzeApiEndpointChangesAsync(GitChange gitChange, QuantitativeChanges changes)
    {
        if (!gitChange.FilePath.Contains("Controller", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (gitChange.ChangeType == "Added")
        {
            // New Controller file - count endpoints
            var endpoints = CountApiEndpoints(gitChange.Content);
            changes = changes with { NewApiEndpoints = changes.NewApiEndpoints + endpoints };
            _logger.LogDebug("New Controller with {Count} endpoints: {FilePath}", endpoints, gitChange.FilePath);
        }
        else if (gitChange.ChangeType == "Modified")
        {
            // Check for new public action methods
            var newEndpoints = CountNewActionMethods(gitChange.Content);
            changes = changes with { NewApiEndpoints = changes.NewApiEndpoints + newEndpoints };
            if (newEndpoints > 0)
            {
                _logger.LogDebug("New API endpoints detected: {Count} in {FilePath}", newEndpoints, gitChange.FilePath);
            }
        }
    }

    private async Task AnalyzeServiceChangesAsync(GitChange gitChange, QuantitativeChanges changes)
    {
        if (!gitChange.FilePath.Contains("Service", StringComparison.OrdinalIgnoreCase) ||
            gitChange.FilePath.Contains("Test", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (gitChange.ChangeType == "Added")
        {
            // New Service file = +1 minor
            changes = changes with { NewServices = changes.NewServices + 1 };
            _logger.LogDebug("New Service detected: {FilePath}", gitChange.FilePath);
        }
        else if (gitChange.ChangeType == "Modified")
        {
            // Check for new public methods in existing services
            var newMethods = CountNewPublicMethods(gitChange.Content);
            if (newMethods > 0)
            {
                changes = changes with { NewServices = changes.NewServices + newMethods };
                _logger.LogDebug("New Service methods detected: {Count} in {FilePath}", newMethods, gitChange.FilePath);
            }
        }
    }

    private async Task AnalyzeBugFixesAsync(GitChange gitChange, QuantitativeChanges changes)
    {
        // Look for bug fix indicators in file content and commit patterns
        var bugFixPatterns = new[]
        {
            @"(?i)\bfix\b",
            @"(?i)\bbug\b",
            @"(?i)\bissue\b",
            @"(?i)\berror\b",
            @"(?i)\bexception\b",
            @"(?i)try\s*{\s*.*?\s*}\s*catch",
            @"(?i)throw\s+new\s+\w*Exception"
        };

        var isBugFix = bugFixPatterns.Any(pattern =>
            Regex.IsMatch(gitChange.Content, pattern) ||
            Regex.IsMatch(gitChange.FilePath, pattern));

        if (isBugFix && gitChange.ChangeType == "Modified")
        {
            changes = changes with { BugFixes = changes.BugFixes + 1 };
            _logger.LogDebug("Bug fix detected: {FilePath}", gitChange.FilePath);
        }
    }

    private async Task AnalyzePerformanceImprovementsAsync(GitChange gitChange, QuantitativeChanges changes)
    {
        // Look for performance improvement indicators
        var performancePatterns = new[]
        {
            @"(?i)\bperformance\b",
            @"(?i)\boptimiz\w+",
            @"(?i)\bcache\b",
            @"(?i)\basync\b.*\bawait\b",
            @"(?i)\bparallel\b",
            @"(?i)\bIAsyncEnumerable\b",
            @"(?i)\bConfigureAwait\(false\)",
            @"(?i)\bStringBuilder\b"
        };

        var isPerformanceImprovement = performancePatterns.Any(pattern =>
            Regex.IsMatch(gitChange.Content, pattern));

        if (isPerformanceImprovement && gitChange.ChangeType == "Modified")
        {
            changes = changes with { PerformanceImprovements = changes.PerformanceImprovements + 1 };
            _logger.LogDebug("Performance improvement detected: {FilePath}", gitChange.FilePath);
        }
    }

    private async Task AnalyzeDocumentationAsync(GitChange gitChange, QuantitativeChanges changes)
    {
        var isDocumentation = gitChange.FilePath.EndsWith(".md", StringComparison.OrdinalIgnoreCase) ||
                             gitChange.FilePath.Contains("README", StringComparison.OrdinalIgnoreCase) ||
                             gitChange.FilePath.Contains("docs", StringComparison.OrdinalIgnoreCase);

        if (isDocumentation)
        {
            changes = changes with { Documentation = changes.Documentation + 1 };
            _logger.LogDebug("Documentation change detected: {FilePath}", gitChange.FilePath);
        }
    }

    private int CountNewPublicProperties(string content)
    {
        // Count public properties that look new (simplified heuristic)
        var propertyPattern = @"public\s+\w+\s+\w+\s*\{\s*get;\s*set;\s*\}";
        var matches = Regex.Matches(content, propertyPattern);

        // This is a simplified count - in practice you'd compare against baseline
        return Math.Min(matches.Count, 5); // Cap at 5 to avoid over-counting on new files
    }

    private int CountApiEndpoints(string content)
    {
        // Count HTTP method attributes
        var endpointPatterns = new[]
        {
            @"\[HttpGet\]",
            @"\[HttpPost\]",
            @"\[HttpPut\]",
            @"\[HttpDelete\]",
            @"\[HttpPatch\]"
        };

        return endpointPatterns.Sum(pattern => Regex.Matches(content, pattern).Count);
    }

    private int CountNewActionMethods(string content)
    {
        // Count public methods that return IActionResult or similar
        var actionPattern = @"public\s+(async\s+)?(Task<)?I?ActionResult";
        var matches = Regex.Matches(content, actionPattern);

        // This is simplified - would need diff analysis for accuracy
        return Math.Min(matches.Count, 3); // Cap to avoid over-counting
    }

    private int CountNewPublicMethods(string content)
    {
        // Count public method declarations
        var methodPattern = @"public\s+(virtual\s+)?(async\s+)?(Task<?\w*>?|\w+)\s+\w+\s*\(";
        var matches = Regex.Matches(content, methodPattern);

        // Filter out properties (get/set) and constructors
        var filteredMatches = matches.Cast<Match>()
            .Where(m => !m.Value.Contains("get;") && !m.Value.Contains("set;"))
            .Count();

        return Math.Min(filteredMatches, 3); // Cap to avoid over-counting
    }

    private int GetMinorTotal(QuantitativeChanges changes)
    {
        return changes.NewViewModels + changes.NewApiEndpoints + changes.NewServices;
    }

    private int GetPatchTotal(QuantitativeChanges changes)
    {
        return changes.BugFixes + changes.PerformanceImprovements + changes.Documentation;
    }
}