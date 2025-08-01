using Microsoft.Extensions.Logging;


namespace SQLServerSyncGenerator.Services;

public class AssemblyLoader
{
    private readonly ILogger<AssemblyLoader> _logger;

    public AssemblyLoader(ILogger<AssemblyLoader> logger)
    {
        _logger = logger;
    }

    public async Task<List<string>> LoadDomainAssemblyPathsAsync(GeneratorOptions options)
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
                var discoveredAssemblies = await DiscoverDomainAssembliesAsync(solutionRoot);
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
                    "No Domain assemblies found. Make sure projects are built and contain 'Domain' in their name.");
            }

            _logger.LogInformation("Successfully found {Count} Domain assemblies", validAssemblies.Count);
            return validAssemblies;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to find Domain assemblies");
            throw;
        }
    }

    private async Task<List<string>> DiscoverDomainAssembliesAsync(string solutionRoot)
    {
        _logger.LogInformation("Auto-discovering Domain assemblies in solution...");

        var foundAssemblies = new List<string>();

        // Find all directories at solution level that end with "Domain"
        var domainDirectories = Directory.GetDirectories(solutionRoot, "*Domain", SearchOption.TopDirectoryOnly)
            .Where(dir => !Path.GetFileName(dir).Contains("Test")) // Exclude test projects
            .ToList();

        _logger.LogInformation("Found {Count} Domain project directories: {Directories}",
            domainDirectories.Count,
            string.Join(", ", domainDirectories.Select(Path.GetFileName)));

        foreach (var projectDir in domainDirectories)
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
                    var schema = DetermineSchemaFromAssembly(assemblyPath);
                    _logger.LogInformation("Discovered {Schema} Domain assembly: {Path}", schema, assemblyPath);
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

        _logger.LogInformation("Found {Count} Domain assemblies", foundAssemblies.Count);
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

    /// <summary>
    /// Validates that an assembly path points to a Domain assembly based on naming convention
    /// </summary>
    public bool IsDomainAssemblyPath(string assemblyPath)
    {
        try
        {
            var fileName = Path.GetFileNameWithoutExtension(assemblyPath);
            var isDomainAssembly = fileName.EndsWith("Domain", StringComparison.OrdinalIgnoreCase);

            _logger.LogDebug("Assembly {FileName} is {Result} Domain assembly based on naming",
                fileName, isDomainAssembly ? "a" : "not a");

            return isDomainAssembly;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error validating assembly path: {AssemblyPath}", assemblyPath);
            return false;
        }
    }

    /// <summary>
    /// Determines the schema name (Core/Photography/etc.) from an assembly based on its path
    /// Extracts the middle part from Location.{SCHEMA}.Domain.dll pattern
    /// </summary>
    public string DetermineSchemaFromAssembly(string assemblyPath)
    {
        try
        {
            var fileName = Path.GetFileNameWithoutExtension(assemblyPath);

            // Try to extract schema from pattern: Location.{SCHEMA}.Domain
            var parts = fileName.Split('.');
            if (parts.Length >= 3 && parts[0] == "Location" && parts[^1] == "Domain")
            {
                return parts[1]; // Return the middle part (Photography, Core, Fishing, etc.)
            }

            // Fallback to specific pattern matching for backward compatibility
            if (fileName.Contains("Core.Domain", StringComparison.OrdinalIgnoreCase))
                return "Core";
            else if (fileName.Contains("Photography.Domain", StringComparison.OrdinalIgnoreCase))
                return "Photography";
            else if (fileName.Contains("Fishing.Domain", StringComparison.OrdinalIgnoreCase))
                return "Fishing";
            else if (fileName.Contains("Hunting.Domain", StringComparison.OrdinalIgnoreCase))
                return "Hunting";
            else
            {
                _logger.LogWarning("Could not determine schema from assembly: {AssemblyPath}", assemblyPath);
                return "Unknown";
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error determining schema from assembly: {AssemblyPath}", assemblyPath);
            return "Unknown";
        }
    }

    /// <summary>
    /// Validates that all found assemblies exist and are accessible
    /// </summary>
    public async Task<bool> ValidateAssemblyPathsAsync(List<string> assemblyPaths)
    {
        _logger.LogDebug("Validating {Count} assembly paths", assemblyPaths.Count);

        foreach (var path in assemblyPaths)
        {
            if (!File.Exists(path))
            {
                _logger.LogError("Assembly file does not exist: {Path}", path);
                return false;
            }

            if (!IsDomainAssemblyPath(path))
            {
                _logger.LogError("Assembly is not a Domain assembly: {Path}", path);
                return false;
            }

            try
            {
                // Try to get file info to ensure it's accessible
                var fileInfo = new FileInfo(path);
                if (fileInfo.Length == 0)
                {
                    _logger.LogError("Assembly file is empty: {Path}", path);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cannot access assembly file: {Path}", path);
                return false;
            }
        }

        _logger.LogDebug("All assembly paths validated successfully");
        return true;
    }
}