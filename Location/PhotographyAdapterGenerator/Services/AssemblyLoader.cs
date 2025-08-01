using Microsoft.Extensions.Logging;
using Microsoft.Extensions.FileSystemGlobbing;

namespace Location.Photography.Tools.AdapterGenerator.Services;

public class AssemblyLoader
{
    private readonly ILogger<AssemblyLoader> _logger;

    public AssemblyLoader(ILogger<AssemblyLoader> logger)
    {
        _logger = logger;
    }

    public async Task<List<string>> LoadViewModelAssemblyPathsAsync(GeneratorOptions options)
    {
        var assemblyPaths = new List<string>();

        try
        {
            // Find solution root first
            var solutionRoot = FindSolutionRoot();
            _logger.LogInformation("Found solution root: {SolutionRoot}", solutionRoot);

            // Use custom paths if provided
            if (!string.IsNullOrEmpty(options.CoreAssemblyPath))
            {
                assemblyPaths.Add(options.CoreAssemblyPath);
                _logger.LogInformation("Using custom Core assembly path: {Path}", options.CoreAssemblyPath);
            }

            if (!string.IsNullOrEmpty(options.PhotographyAssemblyPath))
            {
                assemblyPaths.Add(options.PhotographyAssemblyPath);
                _logger.LogInformation("Using custom Photography assembly path: {Path}", options.PhotographyAssemblyPath);
            }

            // If no custom paths, auto-discover
            if (assemblyPaths.Count == 0)
            {
                var discoveredAssemblies = await DiscoverViewModelAssembliesAsync(solutionRoot);
                assemblyPaths.AddRange(discoveredAssemblies);
            }

            // Validate all found assemblies
            var validAssemblies = assemblyPaths.Where(File.Exists).ToList();

            if (validAssemblies.Count != assemblyPaths.Count)
            {
                var missing = assemblyPaths.Except(validAssemblies);
                _logger.LogWarning("Some assemblies were not found: {MissingAssemblies}",
                    string.Join(", ", missing));
            }

            if (validAssemblies.Count == 0)
            {
                throw new InvalidOperationException(
                    "No ViewModel assemblies found. Make sure projects are built and contain 'ViewModels' in their name.");
            }

            _logger.LogInformation("Successfully found {Count} ViewModel assemblies", validAssemblies.Count);
            return validAssemblies;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to find ViewModel assemblies");
            throw;
        }
    }

    private async Task<List<string>> DiscoverViewModelAssembliesAsync(string solutionRoot)
    {
        _logger.LogInformation("Auto-discovering ViewModel assemblies in solution...");

        var foundAssemblies = new List<string>();

        // Find all directories at solution level that end with "ViewModels"
        var viewModelDirectories = Directory.GetDirectories(solutionRoot, "*ViewModels", SearchOption.TopDirectoryOnly)
            .Where(dir => !Path.GetFileName(dir).Contains("Test")) // Exclude test projects
            .ToList();

        _logger.LogInformation("Found {Count} ViewModel project directories: {Directories}",
            viewModelDirectories.Count,
            string.Join(", ", viewModelDirectories.Select(Path.GetFileName)));

        foreach (var projectDir in viewModelDirectories)
        {
            var projectName = Path.GetFileName(projectDir);
            var expectedAssemblyName = $"{projectName}.dll";

            // Look in bin folder for the assembly (prefer Debug, fallback to Release)
            var binPath = Path.Combine(projectDir, "bin");
            if (Directory.Exists(binPath))
            {
                var assemblyPath = FindAssemblyInBinFolder(binPath, expectedAssemblyName);
                if (assemblyPath != null)
                {
                    foundAssemblies.Add(assemblyPath);
                    var source = DetermineAssemblySource(assemblyPath);
                    _logger.LogInformation("Discovered {Source} assembly: {Path}", source, assemblyPath);
                }
                else
                {
                    _logger.LogWarning("No built assembly found for project {ProjectName} in {BinPath}",
                        projectName, binPath);
                }
            }
            else
            {
                _logger.LogWarning("No bin folder found for project {ProjectName}", projectName);
            }
        }

        _logger.LogInformation("Found {Count} ViewModel assemblies", foundAssemblies.Count);
        return foundAssemblies;
    }

    private string? FindAssemblyInBinFolder(string binPath, string assemblyName)
    {
        // Search pattern: bin/Debug/net*/AssemblyName.dll or bin/Release/net*/AssemblyName.dll
        var configurations = new[] { "Debug", "Release" };

        foreach (var config in configurations)
        {
            var configPath = Path.Combine(binPath, config);
            if (!Directory.Exists(configPath)) continue;

            // Look for net* folders (net9.0, net8.0, etc.)
            var targetFrameworkDirs = Directory.GetDirectories(configPath, "net*");

            foreach (var tfmDir in targetFrameworkDirs)
            {
                var assemblyPath = Path.Combine(tfmDir, assemblyName);
                if (File.Exists(assemblyPath))
                {
                    _logger.LogDebug("Found assembly {AssemblyName} at {Path}", assemblyName, assemblyPath);
                    return assemblyPath;
                }
            }
        }

        return null;
    }

    private string FindSolutionRoot(string? startPath = null)
    {
        var currentPath = startPath ?? Directory.GetCurrentDirectory();
        if (Directory.GetFiles(currentPath, "*.sln").Any()) return currentPath;
        var parent = Directory.GetParent(currentPath)?.FullName;
        return parent != null ? FindSolutionRoot(parent) : throw new InvalidOperationException("Solution root not found");
    }



    public bool IsViewModelAssembly(string assemblyPath)
    {
        try
        {
            var fileName = Path.GetFileNameWithoutExtension(assemblyPath);
            var isViewModelAssembly = fileName.Contains("ViewModels", StringComparison.OrdinalIgnoreCase);

            _logger.LogDebug("Assembly {FileName} is {Result} ViewModel assembly based on naming",
                fileName, isViewModelAssembly ? "a" : "not a");

            return isViewModelAssembly;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error validating assembly path: {AssemblyPath}", assemblyPath);
            return false;
        }
    }

    public string DetermineAssemblySource(string assemblyPath)
    {
        try
        {
            var fileName = Path.GetFileNameWithoutExtension(assemblyPath);

            if (fileName.Contains("Core.ViewModels", StringComparison.OrdinalIgnoreCase))
                return "Core";
            else if (fileName.Contains("Photography.ViewModels", StringComparison.OrdinalIgnoreCase))
                return "Photography";
            else if (fileName.Contains("Core", StringComparison.OrdinalIgnoreCase))
                return "Core";
            else if (fileName.Contains("Photography", StringComparison.OrdinalIgnoreCase))
                return "Photography";
            else
                return "Unknown";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error determining assembly source: {AssemblyPath}", assemblyPath);
            return "Unknown";
        }
    }
}