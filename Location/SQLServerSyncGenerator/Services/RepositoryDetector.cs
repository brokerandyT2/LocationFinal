using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace SQLServerSyncGenerator.Services;

public class RepositoryDetector
{
    private readonly ILogger<RepositoryDetector> _logger;

    public RepositoryDetector(ILogger<RepositoryDetector> logger)
    {
        _logger = logger;
    }

    public async Task<RepositoryInfo> DetectRepositoryAsync(string? startPath = null)
    {
        var currentPath = startPath ?? Directory.GetCurrentDirectory();

        // 1. Check pipeline environment variables first
        var pipelineRepo = DetectFromPipelineVariables();
        if (pipelineRepo != null)
        {
            _logger.LogInformation("Repository detected from pipeline: {RepoName} at {RepoPath}",
                pipelineRepo.Name, pipelineRepo.RootPath);
            return pipelineRepo;
        }

        // 2. Check git repository
        var gitRepo = await DetectFromGitAsync(currentPath);
        if (gitRepo != null)
        {
            _logger.LogInformation("Repository detected from git: {RepoName} at {RepoPath}",
                gitRepo.Name, gitRepo.RootPath);
            return gitRepo;
        }

        // 3. Fallback to directory structure analysis
        var directoryRepo = DetectFromDirectoryStructure(currentPath);
        if (directoryRepo != null)
        {
            _logger.LogInformation("Repository detected from directory: {RepoName} at {RepoPath}",
                directoryRepo.Name, directoryRepo.RootPath);
            return directoryRepo;
        }

        // 4. Create default repository info
        var defaultRepo = CreateDefaultRepository(currentPath);
        _logger.LogWarning("Could not detect repository - using default: {RepoName} at {RepoPath}",
            defaultRepo.Name, defaultRepo.RootPath);
        return defaultRepo;
    }

    private RepositoryInfo? DetectFromPipelineVariables()
    {
        // Azure DevOps
        var buildSourcesDirectory = Environment.GetEnvironmentVariable("BUILD_SOURCESDIRECTORY");
        var buildRepositoryName = Environment.GetEnvironmentVariable("BUILD_REPOSITORY_NAME");
        var buildRepositoryUri = Environment.GetEnvironmentVariable("BUILD_REPOSITORY_URI");

        if (!string.IsNullOrEmpty(buildSourcesDirectory) && !string.IsNullOrEmpty(buildRepositoryName))
        {
            return new RepositoryInfo
            {
                Name = ExtractRepoNameFromUri(buildRepositoryName, buildRepositoryUri),
                RootPath = buildSourcesDirectory,
                Source = RepositorySource.AzureDevOps,
                RemoteUrl = buildRepositoryUri
            };
        }

        // GitHub Actions
        var githubWorkspace = Environment.GetEnvironmentVariable("GITHUB_WORKSPACE");
        var githubRepository = Environment.GetEnvironmentVariable("GITHUB_REPOSITORY");
        var githubServerUrl = Environment.GetEnvironmentVariable("GITHUB_SERVER_URL");

        if (!string.IsNullOrEmpty(githubWorkspace) && !string.IsNullOrEmpty(githubRepository))
        {
            var repoName = githubRepository.Split('/').LastOrDefault() ?? "Unknown";
            return new RepositoryInfo
            {
                Name = repoName,
                RootPath = githubWorkspace,
                Source = RepositorySource.GitHub,
                RemoteUrl = $"{githubServerUrl}/{githubRepository}"
            };
        }

        // GitLab CI
        var ciProjectDir = Environment.GetEnvironmentVariable("CI_PROJECT_DIR");
        var ciProjectName = Environment.GetEnvironmentVariable("CI_PROJECT_NAME");
        var ciProjectUrl = Environment.GetEnvironmentVariable("CI_PROJECT_URL");

        if (!string.IsNullOrEmpty(ciProjectDir) && !string.IsNullOrEmpty(ciProjectName))
        {
            return new RepositoryInfo
            {
                Name = ciProjectName,
                RootPath = ciProjectDir,
                Source = RepositorySource.GitLab,
                RemoteUrl = ciProjectUrl
            };
        }

        // Jenkins
        var workspace = Environment.GetEnvironmentVariable("WORKSPACE");
        var jobName = Environment.GetEnvironmentVariable("JOB_NAME");
        var gitUrl = Environment.GetEnvironmentVariable("GIT_URL");

        if (!string.IsNullOrEmpty(workspace) && !string.IsNullOrEmpty(jobName))
        {
            var repoName = ExtractRepoNameFromUri(jobName, gitUrl);
            return new RepositoryInfo
            {
                Name = repoName,
                RootPath = workspace,
                Source = RepositorySource.Jenkins,
                RemoteUrl = gitUrl
            };
        }

        return null;
    }

    private async Task<RepositoryInfo?> DetectFromGitAsync(string startPath)
    {
        try
        {
            var gitRoot = FindGitRoot(startPath);
            if (gitRoot == null) return null;

            var gitConfigPath = Path.Combine(gitRoot, ".git", "config");
            if (!File.Exists(gitConfigPath)) return null;

            var gitConfig = await File.ReadAllTextAsync(gitConfigPath);
            var remoteUrl = ExtractRemoteUrlFromGitConfig(gitConfig);
            var repoName = ExtractRepoNameFromUri(Path.GetFileName(gitRoot), remoteUrl);

            return new RepositoryInfo
            {
                Name = repoName,
                RootPath = gitRoot,
                Source = RepositorySource.Git,
                RemoteUrl = remoteUrl
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not detect repository from git");
            return null;
        }
    }

    private RepositoryInfo? DetectFromDirectoryStructure(string startPath)
    {
        try
        {
            // Look for common project indicators
            var currentDir = new DirectoryInfo(startPath);

            while (currentDir != null)
            {
                // Check for solution file
                var solutionFiles = currentDir.GetFiles("*.sln");
                if (solutionFiles.Any())
                {
                    var repoName = Path.GetFileNameWithoutExtension(solutionFiles.First().Name);
                    return new RepositoryInfo
                    {
                        Name = repoName,
                        RootPath = currentDir.FullName,
                        Source = RepositorySource.DirectoryStructure
                    };
                }

                // Check for package.json (Node.js projects)
                if (File.Exists(Path.Combine(currentDir.FullName, "package.json")))
                {
                    var packageJson = JsonSerializer.Deserialize<Dictionary<string, object>>(
                        File.ReadAllText(Path.Combine(currentDir.FullName, "package.json")));

                    if (packageJson?.ContainsKey("name") == true)
                    {
                        return new RepositoryInfo
                        {
                            Name = packageJson["name"].ToString() ?? currentDir.Name,
                            RootPath = currentDir.FullName,
                            Source = RepositorySource.DirectoryStructure
                        };
                    }
                }

                // Check for specific directory patterns
                if (currentDir.Name.Contains("Location") ||
                    currentDir.GetDirectories("*Domain").Any() ||
                    currentDir.GetDirectories("*Infrastructure*").Any())
                {
                    return new RepositoryInfo
                    {
                        Name = currentDir.Name,
                        RootPath = currentDir.FullName,
                        Source = RepositorySource.DirectoryStructure
                    };
                }

                currentDir = currentDir.Parent;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not detect repository from directory structure");
            return null;
        }
    }

    private RepositoryInfo CreateDefaultRepository(string currentPath)
    {
        var directoryName = Path.GetFileName(currentPath) ?? "Unknown";
        return new RepositoryInfo
        {
            Name = directoryName,
            RootPath = currentPath,
            Source = RepositorySource.Default
        };
    }

    private string? FindGitRoot(string startPath)
    {
        var currentPath = startPath;
        while (!string.IsNullOrEmpty(currentPath))
        {
            if (Directory.Exists(Path.Combine(currentPath, ".git")))
                return currentPath;

            var parent = Directory.GetParent(currentPath)?.FullName;
            if (parent == currentPath) break;
            currentPath = parent;
        }
        return null;
    }

    private string? ExtractRemoteUrlFromGitConfig(string gitConfig)
    {
        var lines = gitConfig.Split('\n');
        var inRemoteOrigin = false;

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();

            if (trimmedLine == "[remote \"origin\"]")
            {
                inRemoteOrigin = true;
                continue;
            }

            if (inRemoteOrigin && trimmedLine.StartsWith("url = "))
            {
                return trimmedLine.Substring(6).Trim();
            }

            if (trimmedLine.StartsWith("[") && inRemoteOrigin)
            {
                inRemoteOrigin = false;
            }
        }

        return null;
    }

    private string ExtractRepoNameFromUri(string fallbackName, string? uri)
    {
        if (string.IsNullOrEmpty(uri))
            return fallbackName;

        try
        {
            // Handle various Git URL formats
            if (uri.Contains("/"))
            {
                var lastSegment = uri.TrimEnd('/').Split('/').Last();
                if (lastSegment.EndsWith(".git"))
                    lastSegment = lastSegment.Substring(0, lastSegment.Length - 4);
                return lastSegment;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not extract repo name from URI: {Uri}", uri);
        }

        return fallbackName;
    }

    public string GetCompiledSqlPath(RepositoryInfo repositoryInfo, string schemaName = "", string? domainAssemblyPath = null)
    {
        // Expected structure: Location.{Schema}.Infrastructure\Repositories\SqlScripts\CompiledSQL\
        var solutionRoot = repositoryInfo.RootPath;

        // If we have a domain assembly path, extract schema from it
        if (!string.IsNullOrEmpty(domainAssemblyPath))
        {
            schemaName = ExtractSchemaFromAssemblyPath(domainAssemblyPath);
        }

        var infrastructureProject = $"Location.{schemaName}.Infrastructure";
        var compiledSqlPath = Path.Combine(solutionRoot, infrastructureProject, "Repositories", "SqlScripts", "CompiledSQL");

        // Create directories if they don't exist
        Directory.CreateDirectory(compiledSqlPath);

        return compiledSqlPath;
    }

    public string GetSqlScriptsPath(RepositoryInfo repositoryInfo, string schemaName = "", string? domainAssemblyPath = null)
    {
        // Expected structure: Location.{Schema}.Infrastructure\Repositories\SqlScripts\
        var solutionRoot = repositoryInfo.RootPath;

        // If we have a domain assembly path, extract schema from it
        if (!string.IsNullOrEmpty(domainAssemblyPath))
        {
            schemaName = ExtractSchemaFromAssemblyPath(domainAssemblyPath);
        }

        var infrastructureProject = $"Location.{schemaName}.Infrastructure";
        return Path.Combine(solutionRoot, infrastructureProject, "Repositories", "SqlScripts");
    }

    private string ExtractSchemaFromAssemblyPath(string assemblyPath)
    {
        try
        {
            var fileName = Path.GetFileNameWithoutExtension(assemblyPath);
            // Extract from pattern: Location.{SCHEMA}.Domain
            var parts = fileName.Split('.');
            if (parts.Length >= 3 && parts[0] == "Location" && parts[^1] == "Domain")
            {
                return parts[1]; // Return the schema part (Photography, Core, Fishing, etc.)
            }

            _logger.LogWarning("Could not extract schema from assembly path: {AssemblyPath}", assemblyPath);
            return "Unknown";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error extracting schema from assembly path: {AssemblyPath}", assemblyPath);
            return "Unknown";
        }
    }
}

public class RepositoryInfo
{
    public string Name { get; set; } = string.Empty;
    public string RootPath { get; set; } = string.Empty;
    public RepositorySource Source { get; set; }
    public string? RemoteUrl { get; set; }
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
}

public enum RepositorySource
{
    AzureDevOps,
    GitHub,
    GitLab,
    Jenkins,
    Git,
    DirectoryStructure,
    Default
}