using Microsoft.Extensions.Logging;

namespace x3squaredcircles.Version.Calculator.Services;

public class SolutionDetector
{
    private readonly ILogger<SolutionDetector> _logger;

    public SolutionDetector(ILogger<SolutionDetector> logger)
    {
        _logger = logger;
    }

    public async Task<Solution> DetectSolutionAsync(string? startPath = null)
    {
        var currentPath = startPath ?? Directory.GetCurrentDirectory();

        _logger.LogInformation("Detecting solution in: {Path}", currentPath);

        try
        {
            // Find solution file
            var solutionFile = await FindSolutionFileAsync(currentPath);
            if (solutionFile == null)
            {
                throw new InvalidOperationException("No .sln file found. Version Detective must run from solution root.");
            }

            var solutionName = Path.GetFileNameWithoutExtension(solutionFile);
            var solutionType = DetermineSolutionType(solutionName);

            _logger.LogInformation("Found solution: {Name} ({Type})", solutionName, solutionType);

            // Find all projects in solution
            var projects = await FindProjectsInSolutionAsync(solutionFile);
            var domainProject = FindDomainProject(projects);

            if (domainProject == null && solutionType == "vertical")
            {
                _logger.LogWarning("No domain project found in vertical solution: {Name}", solutionName);
            }

            return new Solution
            {
                Name = solutionName,
                Type = solutionType,
                RootPath = Path.GetDirectoryName(solutionFile) ?? currentPath,
                Projects = projects,
                DomainProject = domainProject
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to detect solution");
            throw;
        }
    }

    private async Task<string?> FindSolutionFileAsync(string searchPath)
    {
        var solutionFiles = Directory.GetFiles(searchPath, "*.sln");

        if (solutionFiles.Length == 0)
        {
            // Try parent directory
            var parent = Directory.GetParent(searchPath)?.FullName;
            if (parent != null && parent != searchPath)
            {
                return await FindSolutionFileAsync(parent);
            }
            return null;
        }

        if (solutionFiles.Length == 1)
        {
            return solutionFiles[0];
        }

        // Multiple solutions - prefer Location.* pattern
        var locationSolution = solutionFiles.FirstOrDefault(f =>
            Path.GetFileName(f).StartsWith("Location.", StringComparison.OrdinalIgnoreCase));

        return locationSolution ?? solutionFiles[0];
    }

    private string DetermineSolutionType(string solutionName)
    {
        if (solutionName.Equals("Location.Core", StringComparison.OrdinalIgnoreCase))
        {
            return "core";
        }

        if (solutionName.StartsWith("Location.", StringComparison.OrdinalIgnoreCase))
        {
            return "vertical";
        }

        // Fallback - assume vertical if it contains known patterns
        var verticalPatterns = new[] { "Photography", "Fishing", "Hunting" };
        if (verticalPatterns.Any(pattern => solutionName.Contains(pattern, StringComparison.OrdinalIgnoreCase)))
        {
            return "vertical";
        }

        return "unknown";
    }

    private async Task<List<Project>> FindProjectsInSolutionAsync(string solutionFile)
    {
        var projects = new List<Project>();

        try
        {
            var solutionContent = await File.ReadAllTextAsync(solutionFile);
            var solutionDir = Path.GetDirectoryName(solutionFile) ?? "";

            // Parse .sln file for project references
            // Format: Project("{GUID}") = "ProjectName", "RelativePath\ProjectName.csproj", "{GUID}"
            var projectLines = solutionContent
                .Split('\n')
                .Where(line => line.TrimStart().StartsWith("Project("))
                .ToList();

            foreach (var line in projectLines)
            {
                var project = ParseProjectLine(line, solutionDir);
                if (project != null)
                {
                    projects.Add(project);
                }
            }

            _logger.LogInformation("Found {Count} projects in solution", projects.Count);
            return projects;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse solution file: {SolutionFile}", solutionFile);

            // Fallback: scan directory for .csproj files
            return await ScanForProjectFilesAsync(Path.GetDirectoryName(solutionFile) ?? "");
        }
    }

    private Project? ParseProjectLine(string line, string solutionDir)
    {
        try
        {
            // Parse: Project("{GUID}") = "ProjectName", "RelativePath\ProjectName.csproj", "{GUID}"
            var parts = line.Split('=', StringSplitOptions.TrimEntries);
            if (parts.Length < 2) return null;

            var rightPart = parts[1];
            var projectParts = rightPart.Split(',', StringSplitOptions.TrimEntries);
            if (projectParts.Length < 2) return null;

            var projectName = projectParts[0].Trim('"');
            var relativePath = projectParts[1].Trim('"');
            var fullPath = Path.Combine(solutionDir, relativePath);

            if (!File.Exists(fullPath)) return null;

            return new Project
            {
                Name = projectName,
                Path = fullPath,
                IsDomain = projectName.EndsWith(".Domain", StringComparison.OrdinalIgnoreCase),
                CurrentVersion = GetProjectVersion(fullPath)
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to parse project line: {Line}", line);
            return null;
        }
    }

    private async Task<List<Project>> ScanForProjectFilesAsync(string directory)
    {
        var projects = new List<Project>();

        try
        {
            var projectFiles = Directory.GetFiles(directory, "*.csproj", SearchOption.AllDirectories);

            foreach (var projectFile in projectFiles)
            {
                var projectName = Path.GetFileNameWithoutExtension(projectFile);

                projects.Add(new Project
                {
                    Name = projectName,
                    Path = projectFile,
                    IsDomain = projectName.EndsWith(".Domain", StringComparison.OrdinalIgnoreCase),
                    CurrentVersion = GetProjectVersion(projectFile)
                });
            }

            _logger.LogInformation("Scanned {Count} project files from directory", projects.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to scan for project files in: {Directory}", directory);
        }

        return projects;
    }

    private Project? FindDomainProject(List<Project> projects)
    {
        return projects.FirstOrDefault(p => p.IsDomain);
    }

    private string GetProjectVersion(string projectPath)
    {
        try
        {
            var content = File.ReadAllText(projectPath);

            // Look for <Version>x.y.z</Version> or <AssemblyVersion>x.y.z</AssemblyVersion>
            var versionPatterns = new[]
            {
                @"<Version>([\d\.]+)</Version>",
                @"<AssemblyVersion>([\d\.]+)</AssemblyVersion>",
                @"<FileVersion>([\d\.]+)</FileVersion>"
            };

            foreach (var pattern in versionPatterns)
            {
                var match = System.Text.RegularExpressions.Regex.Match(content, pattern);
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
            }

            return "1.0.0"; // Default if no version found
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to get project version from: {ProjectPath}", projectPath);
            return "1.0.0";
        }
    }

    public bool IsCoreProject(Project project)
    {
        return project.Name.Contains("Core", StringComparison.OrdinalIgnoreCase);
    }

    public bool IsVerticalProject(Project project, string verticalName)
    {
        return project.Name.Contains(verticalName, StringComparison.OrdinalIgnoreCase);
    }

    public string ExtractVerticalName(Solution solution)
    {
        if (solution.Type == "core") return "Core";

        // Extract from solution name: Location.Photography -> Photography
        if (solution.Name.StartsWith("Location.", StringComparison.OrdinalIgnoreCase))
        {
            var parts = solution.Name.Split('.');
            if (parts.Length >= 2)
            {
                return parts[1];
            }
        }

        return "Unknown";
    }
}