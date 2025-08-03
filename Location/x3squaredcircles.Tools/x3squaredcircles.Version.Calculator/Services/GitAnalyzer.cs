using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;

namespace x3squaredcircles.Version.Calculator.Services;

public class GitAnalyzer
{
    private readonly ILogger<GitAnalyzer> _logger;

    public GitAnalyzer(ILogger<GitAnalyzer> logger)
    {
        _logger = logger;
    }

    public async Task<string?> GetLastTagAsync(string? pattern = null)
    {
        try
        {
            var tagPattern = pattern ?? "v*";
            var result = await ExecuteGitCommandAsync($"tag -l \"{tagPattern}\" --sort=-version:refname");

            if (result.ExitCode != 0 || string.IsNullOrWhiteSpace(result.Output))
            {
                _logger.LogInformation("No previous tags found");
                return null;
            }

            var lastTag = result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            _logger.LogInformation("Last tag: {Tag}", lastTag);
            return lastTag;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get last tag");
            return null;
        }
    }

    public async Task<List<GitChange>> GetChangesSinceAsync(string? baseline)
    {
        try
        {
            var gitCommand = baseline != null
                ? $"diff --name-status {baseline}..HEAD"
                : "diff --name-status --cached";

            var result = await ExecuteGitCommandAsync(gitCommand);

            if (result.ExitCode != 0)
            {
                _logger.LogWarning("Git diff failed: {Error}", result.Error);
                return new List<GitChange>();
            }

            var changes = await ParseGitChangesAsync(result.Output, baseline);
            _logger.LogInformation("Found {Count} changed files since {Baseline}", changes.Count, baseline ?? "staged");

            return changes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get git changes since {Baseline}", baseline);
            return new List<GitChange>();
        }
    }

    private async Task<List<GitChange>> ParseGitChangesAsync(string gitOutput, string? baseline)
    {
        var changes = new List<GitChange>();

        if (string.IsNullOrWhiteSpace(gitOutput)) return changes;

        var lines = gitOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            try
            {
                var parts = line.Split('\t', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2) continue;

                var changeType = MapGitChangeType(parts[0]);
                var filePath = parts[1];

                // Get file content for analysis
                var content = await GetFileContentAsync(filePath, changeType);

                changes.Add(new GitChange
                {
                    FilePath = filePath,
                    ChangeType = changeType,
                    Content = content
                });
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to parse git change line: {Line}", line);
            }
        }

        return changes;
    }

    private string MapGitChangeType(string gitStatus)
    {
        return gitStatus.ToUpper() switch
        {
            "A" => "Added",
            "M" => "Modified",
            "D" => "Deleted",
            "R" => "Renamed",
            "C" => "Copied",
            _ => "Modified"
        };
    }

    private async Task<string> GetFileContentAsync(string filePath, string changeType)
    {
        if (changeType == "Deleted") return "";

        try
        {
            if (File.Exists(filePath))
            {
                return await File.ReadAllTextAsync(filePath);
            }

            // Try to get content from git if file doesn't exist in working directory
            var result = await ExecuteGitCommandAsync($"show HEAD:{filePath}");
            return result.ExitCode == 0 ? result.Output : "";
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to get content for file: {FilePath}", filePath);
            return "";
        }
    }

    public async Task<List<string>> GetCommitMessagesSinceAsync(string? baseline)
    {
        try
        {
            var gitCommand = baseline != null
                ? $"log --oneline --format=\"%s\" {baseline}..HEAD"
                : "log --oneline --format=\"%s\" -10"; // Last 10 commits if no baseline

            var result = await ExecuteGitCommandAsync(gitCommand);

            if (result.ExitCode != 0 || string.IsNullOrWhiteSpace(result.Output))
            {
                return new List<string>();
            }

            return result.Output
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get commit messages since {Baseline}", baseline);
            return new List<string>();
        }
    }

    public async Task<string?> GetCurrentBranchAsync()
    {
        try
        {
            var result = await ExecuteGitCommandAsync("branch --show-current");

            if (result.ExitCode == 0 && !string.IsNullOrWhiteSpace(result.Output))
            {
                return result.Output.Trim();
            }

            // Fallback for detached HEAD (common in CI/CD)
            var headResult = await ExecuteGitCommandAsync("rev-parse --abbrev-ref HEAD");
            return headResult.ExitCode == 0 ? headResult.Output.Trim() : "unknown";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get current branch");
            return "unknown";
        }
    }

    public async Task<string?> GetCurrentCommitHashAsync()
    {
        try
        {
            var result = await ExecuteGitCommandAsync("rev-parse HEAD");
            return result.ExitCode == 0 ? result.Output.Trim() : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get current commit hash");
            return null;
        }
    }

    public async Task<bool> HasUncommittedChangesAsync()
    {
        try
        {
            var result = await ExecuteGitCommandAsync("status --porcelain");
            return !string.IsNullOrWhiteSpace(result.Output);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check for uncommitted changes");
            return false;
        }
    }

    public async Task<bool> IsValidGitRepositoryAsync()
    {
        try
        {
            var result = await ExecuteGitCommandAsync("rev-parse --git-dir");
            return result.ExitCode == 0;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Not a valid git repository");
            return false;
        }
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

public class GitCommandResult
{
    public int ExitCode { get; set; }
    public string Output { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
}