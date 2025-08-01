using Location.Tools.APIGenerator.Models;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace Location.Tools.APIGenerator.Services;

public class AssemblyDiscoveryService
{
    private readonly ILogger<AssemblyDiscoveryService> _logger;

    public AssemblyDiscoveryService(ILogger<AssemblyDiscoveryService> logger)
    {
        _logger = logger;
    }

    public async Task<AssemblyInfo> DiscoverInfrastructureAssemblyAsync(GeneratorOptions options)
    {
        try
        {
            // Use custom path if provided
            if (!string.IsNullOrEmpty(options.InfrastructureAssemblyPath))
            {
                _logger.LogInformation("Using custom Infrastructure assembly path: {Path}", options.InfrastructureAssemblyPath);
                return await AnalyzeAssemblyAsync(options.InfrastructureAssemblyPath);
            }

            // Auto-discover Infrastructure assembly
            var assemblyPath = await FindInfrastructureAssemblyAsync();
            if (string.IsNullOrEmpty(assemblyPath))
            {
                throw new InvalidOperationException(
                    "No Infrastructure assembly found. Make sure Location.*.Infrastructure projects are built.");
            }

            return await AnalyzeAssemblyAsync(assemblyPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to discover Infrastructure assembly");
            throw;
        }
    }

    private async Task<string?> FindInfrastructureAssemblyAsync()
    {
        _logger.LogDebug("Searching for Location.*.Infrastructure.dll assemblies");

        try
        {
            // Navigate to solution root using the same pattern as other tools
            var current = new DirectoryInfo(Directory.GetCurrentDirectory());
            var parent = current.ToString();
            var solutionRoot = parent.Replace("bin\\Debug\\net9.0", "").Replace("AutomatedAPIGenerator\\", "");

            _logger.LogDebug("Searching from solution root: {Root}", solutionRoot);

            // Search for Infrastructure assemblies
            var searchPatterns = new[]
            {
                "Location.*.Infrastructure\\bin\\Debug\\net9.0\\Location.*.Infrastructure.dll",
                "*\\bin\\Debug\\net9.0\\Location.*.Infrastructure.dll"
            };

            foreach (var pattern in searchPatterns)
            {
                var searchPath = Path.Combine(solutionRoot, pattern);
                var files = Directory.GetFiles(solutionRoot, "Location.*.Infrastructure.dll", SearchOption.AllDirectories);

                foreach (var file in files)
                {
                    if (file.Contains("\\bin\\Debug\\net9.0\\") && IsInfrastructureAssembly(file))
                    {
                        _logger.LogDebug("Found Infrastructure assembly: {Path}", file);
                        return file;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error during assembly search");
        }

        _logger.LogWarning("Could not find any Infrastructure assemblies");
        return null;
    }

    private bool IsInfrastructureAssembly(string assemblyPath)
    {
        try
        {
            var fileName = Path.GetFileNameWithoutExtension(assemblyPath);
            return fileName.Contains(".Infrastructure", StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error validating assembly path: {Path}", assemblyPath);
            return false;
        }
    }

    private async Task<AssemblyInfo> AnalyzeAssemblyAsync(string assemblyPath)
    {
        _logger.LogDebug("Analyzing assembly: {Path}", assemblyPath);

        try
        {
            if (!File.Exists(assemblyPath))
            {
                throw new FileNotFoundException($"Assembly not found: {assemblyPath}");
            }

            // Load assembly and extract version
            var assembly = Assembly.LoadFrom(assemblyPath);
            var version = assembly.GetName().Version;
            var majorVersion = version?.Major ?? 1;

            // Extract vertical from assembly name
            var vertical = ExtractVerticalFromAssemblyName(assemblyPath);
            var source = ExtractSourceFromAssemblyName(assemblyPath);

            var assemblyInfo = new AssemblyInfo
            {
                Vertical = vertical,
                MajorVersion = majorVersion,
                AssemblyPath = assemblyPath,
                Source = source
            };

            _logger.LogInformation("Assembly analysis complete: {Vertical} v{Version} from {Source}",
                vertical, majorVersion, source);

            return assemblyInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze assembly: {Path}", assemblyPath);
            throw;
        }
    }

    private string ExtractVerticalFromAssemblyName(string assemblyPath)
    {
        var fileName = Path.GetFileNameWithoutExtension(assemblyPath);

        // Extract from pattern: Location.{Vertical}.Infrastructure
        var parts = fileName.Split('.');
        if (parts.Length >= 3 && parts[0] == "Location" && parts[^1] == "Infrastructure")
        {
            return parts[1].ToLowerInvariant(); // Return "photography", "fishing", etc.
        }

        _logger.LogWarning("Could not extract vertical from assembly name: {FileName}", fileName);
        return "unknown";
    }

    private string ExtractSourceFromAssemblyName(string assemblyPath)
    {
        var fileName = Path.GetFileNameWithoutExtension(assemblyPath);

        // Extract from pattern: Location.{Source}.Infrastructure
        var parts = fileName.Split('.');
        if (parts.Length >= 3 && parts[0] == "Location" && parts[^1] == "Infrastructure")
        {
            return parts[1]; // Return "Photography", "Fishing", etc.
        }

        return "Unknown";
    }
}