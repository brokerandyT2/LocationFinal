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
            // Find Core Domain assembly path
            string? coreAssemblyPath = null;
            if (!string.IsNullOrEmpty(options.CoreAssemblyPath))
            {
                coreAssemblyPath = options.CoreAssemblyPath;
                _logger.LogInformation("Using custom Core assembly path: {Path}", coreAssemblyPath);
            }
            else
            {
                coreAssemblyPath = await FindCoreAssemblyPathAsync();
            }

            if (!string.IsNullOrEmpty(coreAssemblyPath))
            {
                assemblyPaths.Add(coreAssemblyPath);
                _logger.LogInformation("Found Core Domain assembly: {Path}", coreAssemblyPath);
            }

            // Find Photography Domain assembly path
            string? photographyAssemblyPath = null;
            if (!string.IsNullOrEmpty(options.PhotographyAssemblyPath))
            {
                photographyAssemblyPath = options.PhotographyAssemblyPath;
                _logger.LogInformation("Using custom Photography assembly path: {Path}", photographyAssemblyPath);
            }
            else
            {
                photographyAssemblyPath = await FindPhotographyAssemblyPathAsync();
            }

            if (!string.IsNullOrEmpty(photographyAssemblyPath))
            {
                assemblyPaths.Add(photographyAssemblyPath);
                _logger.LogInformation("Found Photography Domain assembly: {Path}", photographyAssemblyPath);
            }

            // TODO: Add other vertical assemblies as they're created
            // var fishingAssemblyPath = await FindFishingAssemblyPathAsync();
            // var huntingAssemblyPath = await FindHuntingAssemblyPathAsync();

            if (assemblyPaths.Count == 0)
            {
                throw new InvalidOperationException(
                    "No Domain assemblies found. Make sure Location.Core.Domain and Location.Photography.Domain are built.");
            }

            _logger.LogInformation("Successfully found {Count} Domain assembly paths", assemblyPaths.Count);
            return assemblyPaths;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to find Domain assemblies");
            throw;
        }
    }

    private async Task<string?> FindCoreAssemblyPathAsync()
    {
        const string assemblyName = "Location.Core.Domain.dll";
        const string searchPath = "Location.Core.Domain\\bin\\Debug\\net9.0";

        return await FindAssemblyPathAsync(assemblyName, searchPath, "Core Domain");
    }

    private async Task<string?> FindPhotographyAssemblyPathAsync()
    {
        const string assemblyName = "Location.Photography.Domain.dll";
        const string searchPath = "Location.Photography.Domain\\bin\\Debug\\net9.0";

        return await FindAssemblyPathAsync(assemblyName, searchPath, "Photography Domain");
    }

    private async Task<string?> FindAssemblyPathAsync(string assemblyName, string searchPath, string assemblyType)
    {
        _logger.LogDebug("Searching for {AssemblyType} assembly: {AssemblyName}", assemblyType, assemblyName);

        try
        {
            // Navigate to solution root using the same hardcoded pattern as adapter generator
            var current = new DirectoryInfo(Directory.GetCurrentDirectory());
            var parent = current.ToString();
            var replaces = parent.Replace("bin\\Debug\\net9.0", "").Replace("SqlSchemaGenerator\\", "");
            var fullSearchPath = Path.Combine(replaces, searchPath);

            _logger.LogDebug("Checking search path: {SearchPath}", fullSearchPath);

            if (!Directory.Exists(fullSearchPath))
            {
                _logger.LogDebug("Search path does not exist: {SearchPath}", fullSearchPath);
                return null;
            }

            var assemblyPath = Path.Combine(fullSearchPath, assemblyName);
            _logger.LogDebug("Looking for assembly at: {AssemblyPath}", assemblyPath);

            if (File.Exists(assemblyPath))
            {
                var fullAssemblyPath = Path.GetFullPath(assemblyPath);
                _logger.LogDebug("Found {AssemblyType} assembly at: {AssemblyPath}", assemblyType, fullAssemblyPath);
                return fullAssemblyPath;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error checking search path: {SearchPath}", searchPath);
        }

        _logger.LogWarning("Could not find {AssemblyType} assembly: {AssemblyName}", assemblyType, assemblyName);
        return null;
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
                // Try to extract schema from pattern: Location.{SCHEMA}.Domain
                var parts = fileName.Split('.');
                if (parts.Length >= 3 && parts[0] == "Location" && parts[^1] == "Domain")
                {
                    return parts[1]; // Return the middle part
                }

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