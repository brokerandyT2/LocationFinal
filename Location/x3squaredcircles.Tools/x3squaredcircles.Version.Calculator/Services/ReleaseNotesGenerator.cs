using Microsoft.Extensions.Logging;
using System.Text;

namespace x3squaredcircles.Version.Calculator.Services;

public class ReleaseNotesGenerator
{
    private readonly ILogger<ReleaseNotesGenerator> _logger;
    private readonly GitAnalyzer _gitAnalyzer;

    public ReleaseNotesGenerator(ILogger<ReleaseNotesGenerator> logger, GitAnalyzer gitAnalyzer)
    {
        _logger = logger;
        _gitAnalyzer = gitAnalyzer;
    }

    public async Task<string> GenerateReleaseNotesAsync(VersionImpact versionImpact)
    {
        _logger.LogInformation("Generating release notes for version {Version}", versionImpact.SemanticVersion);

        try
        {
            var releaseNotes = await BuildReleaseNotesAsync(versionImpact);
            await SaveReleaseNotesAsync(releaseNotes, versionImpact.SemanticVersion);

            _logger.LogInformation("Release notes generated successfully");
            return releaseNotes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate release notes");
            return GenerateBasicReleaseNotes(versionImpact);
        }
    }

    private async Task<string> BuildReleaseNotesAsync(VersionImpact versionImpact)
    {
        var sb = new StringBuilder();

        // Header
        sb.AppendLine($"# Version {versionImpact.MarketingVersion} Release Notes");
        sb.AppendLine();
        sb.AppendLine($"**Technical Version:** {versionImpact.SemanticVersion}");
        sb.AppendLine($"**Release Date:** {DateTime.UtcNow:yyyy-MM-dd}");
        sb.AppendLine();

        // Marketing summary
        await AddMarketingSummaryAsync(sb, versionImpact);

        // Breaking changes section
        if (versionImpact.HasSchemaChanges)
        {
            await AddBreakingChangesAsync(sb, versionImpact);
        }

        // New features section
        if (versionImpact.MinorChanges > 0)
        {
            await AddNewFeaturesAsync(sb, versionImpact);
        }

        // Bug fixes and improvements
        if (versionImpact.PatchChanges > 0)
        {
            await AddBugFixesAndImprovementsAsync(sb, versionImpact);
        }

        // Technical details
        await AddTechnicalDetailsAsync(sb, versionImpact);

        // Footer
        await AddFooterAsync(sb, versionImpact);

        return sb.ToString();
    }

    private async Task AddMarketingSummaryAsync(StringBuilder sb, VersionImpact versionImpact)
    {
        sb.AppendLine("## 🎉 What's New");
        sb.AppendLine();

        var totalChanges = versionImpact.MinorChanges + versionImpact.PatchChanges;

        if (versionImpact.HasSchemaChanges)
        {
            sb.AppendLine("🚀 **Major Update** - This release includes significant architectural improvements with database enhancements.");
            sb.AppendLine();
        }
        else if (versionImpact.MinorChanges > 0)
        {
            sb.AppendLine($"✨ **Feature Release** - {versionImpact.MinorChanges} new features and capabilities added.");
            sb.AppendLine();
        }
        else if (versionImpact.PatchChanges > 0)
        {
            sb.AppendLine($"🐛 **Maintenance Release** - {versionImpact.PatchChanges} improvements and bug fixes.");
            sb.AppendLine();
        }

        // Value proposition summary
        if (totalChanges > 10)
        {
            sb.AppendLine("This is our biggest update yet with significant improvements across the platform!");
        }
        else if (totalChanges > 5)
        {
            sb.AppendLine("A substantial update with meaningful improvements to enhance your experience.");
        }
        else if (totalChanges > 0)
        {
            sb.AppendLine("Quality improvements and refinements to make the platform better.");
        }

        sb.AppendLine();
    }

    private async Task AddBreakingChangesAsync(StringBuilder sb, VersionImpact versionImpact)
    {
        sb.AppendLine("## 🔧 Breaking Changes");
        sb.AppendLine();
        sb.AppendLine("⚠️ **Important:** This version includes database schema changes that require migration.");
        sb.AppendLine();

        foreach (var change in versionImpact.SchemaChanges)
        {
            var icon = change.Type switch
            {
                "NewEntity" => "🆕",
                "RemovedEntity" => "🗑️",
                "NewProperty" => "➕",
                "RemovedProperty" => "➖",
                "PropertyTypeChange" => "🔄",
                _ => "🔧"
            };

            sb.AppendLine($"- {icon} **{change.Type}**: {change.Description}");
        }

        sb.AppendLine();
        sb.AppendLine("**Migration Required:** Database will be automatically updated during deployment.");
        sb.AppendLine();
    }

    private async Task AddNewFeaturesAsync(StringBuilder sb, VersionImpact versionImpact)
    {
        sb.AppendLine($"## 🎉 New Features ({versionImpact.MinorChanges})");
        sb.AppendLine();

        if (versionImpact.NewViewModels > 0)
        {
            sb.AppendLine($"### 📱 User Interface");
            sb.AppendLine($"- **{versionImpact.NewViewModels} new screens/views** for enhanced user experience");
            sb.AppendLine();
        }

        if (versionImpact.NewApiEndpoints > 0)
        {
            sb.AppendLine($"### 🔗 API Enhancements");
            sb.AppendLine($"- **{versionImpact.NewApiEndpoints} new API endpoints** for extended functionality");
            sb.AppendLine();
        }

        if (versionImpact.NewServices > 0)
        {
            sb.AppendLine($"### ⚙️ Backend Services");
            sb.AppendLine($"- **{versionImpact.NewServices} new services** for improved capabilities");
            sb.AppendLine();
        }

        // Add commit message insights
        await AddCommitInsightsAsync(sb, "feat:", "feature:");
    }

    private async Task AddBugFixesAndImprovementsAsync(StringBuilder sb, VersionImpact versionImpact)
    {
        sb.AppendLine($"## 🐛 Bug Fixes & Improvements ({versionImpact.PatchChanges})");
        sb.AppendLine();

        if (versionImpact.BugFixes > 0)
        {
            sb.AppendLine($"### 🔧 Bug Fixes");
            sb.AppendLine($"- **{versionImpact.BugFixes} issues resolved** for improved stability");
            sb.AppendLine();
        }

        if (versionImpact.PerformanceImprovements > 0)
        {
            sb.AppendLine($"### ⚡ Performance Improvements");
            sb.AppendLine($"- **{versionImpact.PerformanceImprovements} optimizations** for better performance");
            sb.AppendLine();
        }

        // Add specific commit insights
        await AddCommitInsightsAsync(sb, "fix:", "perf:", "refactor:");
    }

    private async Task AddCommitInsightsAsync(StringBuilder sb, params string[] prefixes)
    {
        try
        {
            var commitMessages = await _gitAnalyzer.GetCommitMessagesSinceAsync(null);
            var relevantCommits = commitMessages
                .Where(msg => prefixes.Any(prefix => msg.ToLowerInvariant().StartsWith(prefix)))
                .Take(5)
                .ToList();

            if (relevantCommits.Any())
            {
                sb.AppendLine("**Recent Changes:**");
                foreach (var commit in relevantCommits)
                {
                    var cleanMessage = CleanCommitMessage(commit);
                    sb.AppendLine($"- {cleanMessage}");
                }
                sb.AppendLine();
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to add commit insights");
        }
    }

    private async Task AddTechnicalDetailsAsync(StringBuilder sb, VersionImpact versionImpact)
    {
        sb.AppendLine("## 📊 Technical Details");
        sb.AppendLine();

        sb.AppendLine($"- **Semantic Version:** {versionImpact.SemanticVersion}");
        sb.AppendLine($"- **Marketing Version:** {versionImpact.MarketingVersion}");

        var currentBranch = await _gitAnalyzer.GetCurrentBranchAsync();
        var currentCommit = await _gitAnalyzer.GetCurrentCommitHashAsync();

        sb.AppendLine($"- **Branch:** {currentBranch}");
        sb.AppendLine($"- **Commit:** {currentCommit?[..8]}");

        if (versionImpact.HasSchemaChanges)
        {
            sb.AppendLine($"- **Database Changes:** {versionImpact.SchemaChanges.Count} schema modifications");
        }

        sb.AppendLine();
    }

    private async Task AddFooterAsync(StringBuilder sb, VersionImpact versionImpact)
    {
        sb.AppendLine("---");
        sb.AppendLine();

        // App store optimization messaging
        var totalChanges = versionImpact.MinorChanges + versionImpact.PatchChanges;
        if (versionImpact.HasSchemaChanges)
        {
            sb.AppendLine("🚀 **Major architectural improvements** - Our most significant update with enhanced database capabilities!");
        }
        else if (totalChanges >= 10)
        {
            sb.AppendLine($"🎯 **Massive v{versionImpact.MarketingVersion.Split('.')[1]} update!** - {totalChanges} new features and improvements");
        }
        else if (totalChanges >= 5)
        {
            sb.AppendLine($"✨ **Significant update** - {totalChanges} enhancements for better user experience");
        }

        sb.AppendLine();
        sb.AppendLine($"**Full Changelog:** Compare versions to see all changes");
        sb.AppendLine($"**Download:** Available through your usual deployment channels");
    }

    private string CleanCommitMessage(string commitMessage)
    {
        // Remove conventional commit prefixes and clean up
        var prefixes = new[] { "feat:", "fix:", "perf:", "refactor:", "docs:", "style:", "test:" };

        var cleaned = commitMessage;
        foreach (var prefix in prefixes)
        {
            if (cleaned.ToLowerInvariant().StartsWith(prefix))
            {
                cleaned = cleaned.Substring(prefix.Length).Trim();
                break;
            }
        }

        // Capitalize first letter
        if (cleaned.Length > 0)
        {
            cleaned = char.ToUpperInvariant(cleaned[0]) + cleaned.Substring(1);
        }

        return cleaned;
    }

    private string GenerateBasicReleaseNotes(VersionImpact versionImpact)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"# Version {versionImpact.SemanticVersion} Release Notes");
        sb.AppendLine();
        sb.AppendLine($"**Release Date:** {DateTime.UtcNow:yyyy-MM-dd}");
        sb.AppendLine();
        sb.AppendLine("## Summary");
        sb.AppendLine(versionImpact.Reasoning);
        sb.AppendLine();

        if (versionImpact.HasSchemaChanges)
        {
            sb.AppendLine("## Breaking Changes");
            foreach (var change in versionImpact.SchemaChanges)
            {
                sb.AppendLine($"- {change.Description}");
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private async Task SaveReleaseNotesAsync(string releaseNotes, string version)
    {
        try
        {
            var fileName = $"RELEASE_NOTES_v{version}.md";
            await File.WriteAllTextAsync(fileName, releaseNotes);
            _logger.LogInformation("Release notes saved to: {FileName}", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save release notes to file");
        }
    }

    public async Task<string> GenerateChangelogEntryAsync(VersionImpact versionImpact)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"## [{versionImpact.SemanticVersion}] - {DateTime.UtcNow:yyyy-MM-dd}");
        sb.AppendLine();

        if (versionImpact.HasSchemaChanges)
        {
            sb.AppendLine("### Changed");
            foreach (var change in versionImpact.SchemaChanges)
            {
                sb.AppendLine($"- {change.Description}");
            }
            sb.AppendLine();
        }

        if (versionImpact.MinorChanges > 0)
        {
            sb.AppendLine("### Added");
            if (versionImpact.NewViewModels > 0) sb.AppendLine($"- {versionImpact.NewViewModels} new user interface components");
            if (versionImpact.NewApiEndpoints > 0) sb.AppendLine($"- {versionImpact.NewApiEndpoints} new API endpoints");
            if (versionImpact.NewServices > 0) sb.AppendLine($"- {versionImpact.NewServices} new backend services");
            sb.AppendLine();
        }

        if (versionImpact.PatchChanges > 0)
        {
            sb.AppendLine("### Fixed");
            if (versionImpact.BugFixes > 0) sb.AppendLine($"- {versionImpact.BugFixes} bug fixes and stability improvements");
            if (versionImpact.PerformanceImprovements > 0) sb.AppendLine($"- {versionImpact.PerformanceImprovements} performance optimizations");
            sb.AppendLine();
        }

        return sb.ToString();
    }
}