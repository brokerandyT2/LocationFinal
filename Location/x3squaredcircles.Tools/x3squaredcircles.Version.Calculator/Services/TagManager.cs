using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;

namespace x3squaredcircles.Version.Calculator.Services;

public class TagManager
{
    private readonly ILogger<TagManager> _logger;

    public TagManager(ILogger<TagManager> logger)
    {
        _logger = logger;
    }

    public async Task CreateSemanticTagAsync(string version)
    {
        var tagName = $"semver/{version}";
        await CreateTagAsync(tagName, $"Semantic version {version}");
    }

    public async Task CreateMarketingTagAsync(string version)
    {
        var tagName = $"marketing/{version}";
        await CreateTagAsync(tagName, $"Marketing version {version}");
    }

    public async Task CreateBuildTagAsync(string semanticVersion)
    {
        var buildId = GetBuildId();
        var tagName = $"build/{semanticVersion}+{buildId}";
        await CreateTagAsync(tagName, $"Build {semanticVersion} (Build ID: {buildId})");
    }

    private async Task CreateTagAsync(string tagName, string message)
    {
        try
        {
            _logger.LogInformation("Creating git tag: {TagName}", tagName);

            // Check if tag already exists
            var existingTag = await ExecuteGitCommandAsync($"tag -l \"{tagName}\"");
            if (!string.IsNullOrWhiteSpace(existingTag.Output))
            {
                _logger.LogWarning("Tag already exists: {TagName}", tagName);
                return;
            }

            // Create annotated tag
            var result = await ExecuteGitCommandAsync($"tag -a \"{tagName}\" -m \"{message}\"");

            if (result.ExitCode == 0)
            {
                _logger.LogInformation("Successfully created tag: {TagName}", tagName);
            }
            else
            {
                _logger.LogError("Failed to create tag {TagName}: {Error}", tagName, result.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception creating tag: {TagName}", tagName);
        }
    }

    public async Task<List<string>> GetAllVersionTagsAsync()
    {
        try
        {
            var result = await ExecuteGitCommandAsync("tag -l \"v*\" \"semver/*\" \"marketing/*\" --sort=-version:refname");

            if (result.ExitCode == 0 && !string.IsNullOrWhiteSpace(result.Output))
            {
                return result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get version tags");
        }

        return new List<string>();
    }

    public async Task<string?> GetLatestSemanticTagAsync()
    {
        try
        {
            var result = await ExecuteGitCommandAsync("tag -l \"semver/*\" --sort=-version:refname");

            if (result.ExitCode == 0 && !string.IsNullOrWhiteSpace(result.Output))
            {
                var latestTag = result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                return latestTag?.Replace("semver/", "");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get latest semantic tag");
        }

        return null;
    }

    public async Task<string?> GetLatestMarketingTagAsync()
    {
        try
        {
            var result = await ExecuteGitCommandAsync("tag -l \"marketing/*\" --sort=-version:refname");

            if (result.ExitCode == 0 && !string.IsNullOrWhiteSpace(result.Output))
            {
                var latestTag = result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                return latestTag?.Replace("marketing/", "");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get latest marketing tag");
        }

        return null;
    }

    public async Task<bool> TagExistsAsync(string tagName)
    {
        try
        {
            var result = await ExecuteGitCommandAsync($"tag -l \"{tagName}\"");
            return !string.IsNullOrWhiteSpace(result.Output);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if tag exists: {TagName}", tagName);
            return false;
        }
    }

    public async Task DeleteTagAsync(string tagName)
    {
        try
        {
            _logger.LogInformation("Deleting git tag: {TagName}", tagName);

            var result = await ExecuteGitCommandAsync($"tag -d \"{tagName}\"");

            if (result.ExitCode == 0)
            {
                _logger.LogInformation("Successfully deleted tag: {TagName}", tagName);
            }
            else
            {
                _logger.LogError("Failed to delete tag {TagName}: {Error}", tagName, result.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception deleting tag: {TagName}", tagName);
        }
    }

    public async Task PushTagsAsync()
    {
        try
        {
            _logger.LogInformation("Pushing tags to remote repository");

            var result = await ExecuteGitCommandAsync("push origin --tags");

            if (result.ExitCode == 0)
            {
                _logger.LogInformation("Successfully pushed tags to remote");
            }
            else
            {
                _logger.LogWarning("Failed to push tags to remote: {Error}", result.Error);
                // Don't throw - local tags are still created
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception pushing tags to remote");
            // Don't throw - local tags are still created
        }
    }

    public async Task<TagInfo?> GetTagInfoAsync(string tagName)
    {
        try
        {
            var result = await ExecuteGitCommandAsync($"show {tagName} --format=\"%H|%at|%s\" --no-patch");

            if (result.ExitCode == 0 && !string.IsNullOrWhiteSpace(result.Output))
            {
                var parts = result.Output.Split('|');
                if (parts.Length >= 3)
                {
                    var timestamp = long.TryParse(parts[1], out var unixTime)
                        ? DateTimeOffset.FromUnixTimeSeconds(unixTime)
                        : DateTimeOffset.UtcNow;

                    return new TagInfo
                    {
                        Name = tagName,
                        CommitHash = parts[0],
                        CreatedAt = timestamp,
                        Message = parts[2]
                    };
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get tag info: {TagName}", tagName);
        }

        return null;
    }

    private string GetBuildId()
    {
        // Try to get build ID from environment variables (Azure DevOps, GitHub Actions, etc.)
        var buildId = Environment.GetEnvironmentVariable("BUILD_BUILDID") ??           // Azure DevOps
                     Environment.GetEnvironmentVariable("GITHUB_RUN_ID") ??           // GitHub Actions
                     Environment.GetEnvironmentVariable("CI_PIPELINE_ID") ??          // GitLab CI
                     Environment.GetEnvironmentVariable("BUILD_NUMBER") ??            // Jenkins
                     DateTime.UtcNow.ToString("yyyyMMddHHmmss");                     // Fallback

        return buildId;
    }

    private async Task<GitCommandResult> ExecuteGitCommandAsync(string arguments)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = arguments,
            WorkingDirectory = Directory.GetCurrentDirectory(),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = processStartInfo };

        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        process.OutputDataReceived += (sender, e) => { if (e.Data != null) outputBuilder.AppendLine(e.Data); };
        process.ErrorDataReceived += (sender, e) => { if (e.Data != null) errorBuilder.AppendLine(e.Data); };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync();

        return new GitCommandResult
        {
            ExitCode = process.ExitCode,
            Output = outputBuilder.ToString().Trim(),
            Error = errorBuilder.ToString().Trim()
        };
    }
}

public record TagInfo
{
    public string Name { get; init; } = "";
    public string CommitHash { get; init; } = "";
    public DateTimeOffset CreatedAt { get; init; }
    public string Message { get; init; } = "";
}