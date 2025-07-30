using Microsoft.Extensions.Logging;

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
            // Find Core ViewModels assembly path
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
                _logger.LogInformation("Found Core assembly: {Path}", coreAssemblyPath);
            }

            // Find Photography ViewModels assembly path
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
                _logger.LogInformation("Found Photography assembly: {Path}", photographyAssemblyPath);
            }

            if (assemblyPaths.Count == 0)
            {
                throw new InvalidOperationException(
                    "No ViewModel assemblies found. Make sure Location.Core.ViewModels and Location.Photography.ViewModels are built.");
            }

            _logger.LogInformation("Successfully found {Count} assembly paths", assemblyPaths.Count);
            return assemblyPaths;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to find ViewModel assemblies");
            throw;
        }
    }

    private async Task<string?> FindCoreAssemblyPathAsync()
    {
        const string assemblyName = "Location.Core.ViewModels.dll";
        const string searchPath = "Location.Core.ViewModels\\bin\\Debug\\net9.0";

        return await FindAssemblyPathAsync(assemblyName, searchPath, "Core");
    }

    private async Task<string?> FindPhotographyAssemblyPathAsync()
    {
        const string assemblyName = "Location.Photography.ViewModels.dll";
        const string searchPath = "Location.Photography.ViewModels\\bin\\Debug\\net9.0";

        return await FindAssemblyPathAsync(assemblyName, searchPath, "Photography");
    }

    private async Task<string?> FindAssemblyPathAsync(string assemblyName, string searchPath, string assemblyType)
    {
        _logger.LogDebug("Searching for {AssemblyType} assembly: {AssemblyName}", assemblyType, assemblyName);

        try
        {
            // Navigate to solution root using the hardcoded pattern
            var current = new DirectoryInfo(Directory.GetCurrentDirectory());
            var parent = current.ToString();
            var replaces = parent.Replace("bin\\Debug\\net9.0", "").Replace("PhotographyAdapterGenerator\\", "");
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
    /// Validates that an assembly path points to a ViewModels assembly based on naming convention
    /// </summary>
    public bool IsViewModelAssemblyPath(string assemblyPath)
    {
        try
        {
            var fileName = Path.GetFileNameWithoutExtension(assemblyPath);
            var isViewModelAssembly = fileName.EndsWith("ViewModels", StringComparison.OrdinalIgnoreCase);

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

    /// <summary>
    /// Determines the source (Core/Photography) of an assembly based on its path
    /// </summary>
    public string DetermineAssemblySource(string assemblyPath)
    {
        try
        {
            var fileName = Path.GetFileNameWithoutExtension(assemblyPath);

            if (fileName.Contains("Core.ViewModels", StringComparison.OrdinalIgnoreCase))
                return "Core";
            else if (fileName.Contains("Photography.ViewModels", StringComparison.OrdinalIgnoreCase))
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