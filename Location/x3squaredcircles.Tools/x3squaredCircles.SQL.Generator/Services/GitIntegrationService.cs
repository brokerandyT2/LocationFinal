using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;

namespace x3squaredcirecles.SQLSync.Generator.Services;

public class GitIntegrationService
{
    private readonly ILogger<GitIntegrationService> _logger;

    public GitIntegrationService(ILogger<GitIntegrationService> logger)
    {
        _logger = logger;
    }

    public async Task ConsumeAndCommitScriptsAsync(string sqlScriptsPath, string compiledDeploymentPath, string version, string schemaName)
    {
        _logger.LogInformation("Starting script consumption and Azure Repos commit process for version {Version}", version);

        try
        {
            // 1. Delete all consumed scripts from phase folders
            var consumedScripts = await DeleteConsumedScriptsAsync(sqlScriptsPath);

            // 2. Stage all changes in git (deletions + new compiled SQL)
            await StageGitChangesAsync(sqlScriptsPath);

            // 3. Commit with descriptive message
            var commitMessage = GenerateCommitMessage(version, schemaName, consumedScripts, compiledDeploymentPath);
            await CommitChangesAsync(commitMessage);

            // 4. Push to Azure Repos
            await PushToAzureReposAsync();

            _logger.LogInformation("Successfully committed and pushed {ConsumedCount} consumed scripts and compiled deployment v{Version}",
                consumedScripts.Count, version);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to consume scripts and commit to Azure Repos");
            throw;
        }
    }

    private async Task<List<ConsumedScript>> DeleteConsumedScriptsAsync(string sqlScriptsPath)
    {
        var consumedScripts = new List<ConsumedScript>();

        _logger.LogInformation("Deleting consumed scripts from phase folders in: {SqlScriptsPath}", sqlScriptsPath);

        // Delete scripts from all phase folders (01-29)
        for (int phase = 1; phase <= 29; phase++)
        {
            var phasePatterns = new[]
            {
                $"{phase:D2}-*",
                $"{phase:D2}_*",
                $"{phase:D2}.*"
            };

            foreach (var pattern in phasePatterns)
            {
                var phaseDirs = Directory.GetDirectories(sqlScriptsPath, pattern);

                foreach (var phaseDir in phaseDirs)
                {
                    var scriptsDeleted = await DeleteScriptsInDirectoryAsync(phaseDir, phase);
                    consumedScripts.AddRange(scriptsDeleted);
                }
            }
        }

        _logger.LogInformation("Deleted {Count} consumed scripts from phase folders", consumedScripts.Count);
        return consumedScripts;
    }

    private async Task<List<ConsumedScript>> DeleteScriptsInDirectoryAsync(string directoryPath, int phase)
    {
        var consumedScripts = new List<ConsumedScript>();

        try
        {
            var sqlFiles = Directory.GetFiles(directoryPath, "*.sql", SearchOption.AllDirectories)
                .Where(f => !Path.GetFileName(f).StartsWith("_compiled_deployment"))
                .ToList();

            foreach (var filePath in sqlFiles)
            {
                var fileName = Path.GetFileName(filePath);
                var content = await File.ReadAllTextAsync(filePath);

                consumedScripts.Add(new ConsumedScript
                {
                    FileName = fileName,
                    OriginalPath = filePath,
                    Phase = phase,
                    ContentHash = ComputeContentHash(content),
                    ConsumedAt = DateTime.UtcNow
                });

                File.Delete(filePath);
                _logger.LogDebug("Deleted consumed script: {FilePath}", filePath);
            }

            // Remove empty directories
            if (Directory.Exists(directoryPath) && !Directory.EnumerateFileSystemEntries(directoryPath).Any())
            {
                Directory.Delete(directoryPath, true);
                _logger.LogDebug("Removed empty phase directory: {DirectoryPath}", directoryPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting scripts in directory: {DirectoryPath}", directoryPath);
        }

        return consumedScripts;
    }

    private async Task StageGitChangesAsync(string workingDirectory)
    {
        _logger.LogDebug("Staging git changes in: {WorkingDirectory}", workingDirectory);

        // Stage all changes (additions, modifications, deletions)
        var result = await ExecuteGitCommandAsync("add -A", workingDirectory);

        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"Failed to stage git changes: {result.Error}");
        }

        _logger.LogDebug("Successfully staged all git changes");
    }

    private async Task CommitChangesAsync(string commitMessage)
    {
        _logger.LogDebug("Committing changes with message: {CommitMessage}", commitMessage);

        var escapedMessage = commitMessage.Replace("\"", "\\\"");
        var result = await ExecuteGitCommandAsync($"commit -m \"{escapedMessage}\"");

        if (result.ExitCode != 0)
        {
            // Check if it's because there are no changes to commit
            if (result.Error.Contains("nothing to commit"))
            {
                _logger.LogInformation("No changes to commit");
                return;
            }

            throw new InvalidOperationException($"Failed to commit changes: {result.Error}");
        }

        _logger.LogInformation("Successfully committed changes");
    }

    private async Task PushToAzureReposAsync()
    {
        _logger.LogDebug("Pushing to Azure Repos");

        var result = await ExecuteGitCommandAsync("push origin HEAD");

        if (result.ExitCode != 0)
        {
            // In CI/CD, this might fail due to permissions - log warning but don't fail
            if (IsRunningInPipeline())
            {
                _logger.LogWarning("Could not push to Azure Repos from pipeline - changes committed locally: {Error}", result.Error);
                return;
            }

            throw new InvalidOperationException($"Failed to push to Azure Repos: {result.Error}");
        }

        _logger.LogInformation("Successfully pushed to Azure Repos");
    }

    private string GenerateCommitMessage(string version, string schemaName, List<ConsumedScript> consumedScripts, string compiledDeploymentPath)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"Auto-deploy: Compiled deployment v{version}");
        sb.AppendLine();
        sb.AppendLine($"Schema: {schemaName}");
        sb.AppendLine($"Compiled SQL: {Path.GetFileName(compiledDeploymentPath)}");
        sb.AppendLine($"Scripts consumed: {consumedScripts.Count}");
        sb.AppendLine();

        if (consumedScripts.Any())
        {
            sb.AppendLine("Consumed scripts:");

            var scriptsByPhase = consumedScripts.GroupBy(s => s.Phase).OrderBy(g => g.Key);
            foreach (var phaseGroup in scriptsByPhase)
            {
                sb.AppendLine($"  Phase {phaseGroup.Key}:");
                foreach (var script in phaseGroup.OrderBy(s => s.FileName))
                {
                    sb.AppendLine($"    - {script.FileName} (hash: {script.ContentHash})");
                }
            }
        }

        sb.AppendLine();
        sb.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine();
        sb.AppendLine("[skip ci]"); // Prevent triggering another build

        return sb.ToString().Trim();
    }

    private async Task<GitCommandResult> ExecuteGitCommandAsync(string arguments, string? workingDirectory = null)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = arguments,
            WorkingDirectory = workingDirectory ?? Directory.GetCurrentDirectory(),
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

    private bool IsRunningInPipeline()
    {
        // Check for common CI/CD environment variables
        return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("BUILD_BUILDID")) || // Azure DevOps
               !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS")) || // GitHub Actions
               !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI")); // Generic CI
    }

    private string ComputeContentHash(string content)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(hashBytes)[..8]; // First 8 chars for brevity
    }

    public async Task<bool> HasUncommittedChangesAsync(string workingDirectory)
    {
        var result = await ExecuteGitCommandAsync("status --porcelain", workingDirectory);
        return !string.IsNullOrWhiteSpace(result.Output);
    }

    public async Task<string> GetCurrentBranchAsync(string workingDirectory)
    {
        var result = await ExecuteGitCommandAsync("branch --show-current", workingDirectory);
        return result.ExitCode == 0 ? result.Output.Trim() : "unknown";
    }

    public async Task<string> GetCurrentCommitHashAsync(string workingDirectory)
    {
        var result = await ExecuteGitCommandAsync("rev-parse HEAD", workingDirectory);
        return result.ExitCode == 0 ? result.Output.Trim() : "unknown";
    }

    public async Task CreateDeploymentHistoryFileAsync(string sqlScriptsPath, List<ConsumedScript> consumedScripts, string version)
    {
        var historyPath = Path.Combine(sqlScriptsPath, "CompiledSQL", $"deployment_history_v{version}.json");

        var historyEntry = new DeploymentHistoryEntry
        {
            Version = version,
            DeployedAt = DateTime.UtcNow,
            ConsumedScripts = consumedScripts,
            Branch = await GetCurrentBranchAsync(sqlScriptsPath),
            CommitHash = await GetCurrentCommitHashAsync(sqlScriptsPath)
        };

        var json = System.Text.Json.JsonSerializer.Serialize(historyEntry, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });

        await File.WriteAllTextAsync(historyPath, json);
        _logger.LogDebug("Created deployment history file: {HistoryPath}", historyPath);
    }
}

public class ConsumedScript
{
    public string FileName { get; set; } = string.Empty;
    public string OriginalPath { get; set; } = string.Empty;
    public int Phase { get; set; }
    public string ContentHash { get; set; } = string.Empty;
    public DateTime ConsumedAt { get; set; }
}

public class GitCommandResult
{
    public int ExitCode { get; set; }
    public string Output { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
}

public class DeploymentHistoryEntry
{
    public string Version { get; set; } = string.Empty;
    public DateTime DeployedAt { get; set; }
    public List<ConsumedScript> ConsumedScripts { get; set; } = new List<ConsumedScript>();
    public string Branch { get; set; } = string.Empty;
    public string CommitHash { get; set; } = string.Empty;
}