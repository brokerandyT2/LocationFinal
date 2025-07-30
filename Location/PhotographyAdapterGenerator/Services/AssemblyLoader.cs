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
            string coreAssemblyPath=string.Empty;

            if (options.CoreAssemblyPath != string.Empty)
            {
                coreAssemblyPath = options.CoreAssemblyPath;
            }
            else
            {
                coreAssemblyPath = await FindCoreAssemblyPathAsync("..\\Location.Core.ViewModels\\bin\\Debug\\net9.0");
            }
            if (coreAssemblyPath != null)
            {
                assemblyPaths.Add(coreAssemblyPath);
                _logger.LogInformation("Found Core assembly: {Path}", coreAssemblyPath);
            }

            // Find Photography ViewModels assembly path
            string photographyAssemblyPath = string.Empty;
            if(options.PhotographyAssemblyPath != string.Empty)
            {
                photographyAssemblyPath = options.PhotographyAssemblyPath;
            }
            else
            {
                photographyAssemblyPath = await FindPhotographyAssemblyPathAsync("..\\Location.Photography.ViewModels\\bin\\Debug\\Net9.0");
            }
            if (photographyAssemblyPath != null)
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

    private async Task<string?> FindCoreAssemblyPathAsync(string? customPath)
    {
        const string assemblyName = "Location.Core.ViewModels.dll";

        

        // Search common locations for Core assembly
        var searchPaths = new[]
        {
            
            // Relative to generator (one level up)
            Path.Combine( "Location.Core.ViewModels", "bin", "Debug", "net9.0"),
            
            // Common build output paths
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "source", "repos", "location-dotnet-core", "Location.Core.ViewModels", "bin", "Debug", "net9.0"),
            @"C:\Source\location-dotnet-core\Location.Core.ViewModels\bin\Debug\net9.0",
        };

        return await FindAssemblyPathAsync(assemblyName, searchPaths, "Core");
    }

    private async Task<string?> FindPhotographyAssemblyPathAsync(string? customPath)
    {
        const string assemblyName = "Location.Photography.ViewModels.dll";

        if (!string.IsNullOrEmpty(customPath))
        {
            var fullCustomPath = Path.GetFullPath(customPath);
            if (File.Exists(fullCustomPath))
            {
                _logger.LogInformation("Using custom Photography assembly path: {Path}", fullCustomPath);
                return fullCustomPath;
            }
            else
            {
                _logger.LogWarning("Custom Photography assembly path does not exist: {Path}", fullCustomPath);
                return null;
            }
        }

        // Search common locations for Photography assembly
        var searchPaths = new[]
        {
            // Current directory
            Directory.GetCurrentDirectory(),
            
            // Relative to generator (one level up)
            Path.Combine(Directory.GetCurrentDirectory(), "..", "Location.Photography.ViewModels", "bin", "Debug", "net9.0"),
            
            // Common build output paths
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "source", "repos", "location-dotnet-photography", "Location.Photography.ViewModels", "bin", "Debug", "net9.0"),
            @"C:\Source\location-dotnet-photography\Location.Photography.ViewModels\bin\Debug\net9.0",
        };

        return await FindAssemblyPathAsync(assemblyName, searchPaths, "Photography");
    }

    private async Task<string?> FindAssemblyPathAsync(string assemblyName, string[] searchPaths, string assemblyType)
    {
        _logger.LogDebug("Searching for {AssemblyType} assembly: {AssemblyName}", assemblyType, assemblyName);

        foreach (var searchPath in searchPaths)
        {
            try
            {
                var current = new DirectoryInfo(Directory.GetCurrentDirectory());
                var parent = current.ToString();
               
                var replaces = parent.Replace("bin\\Debug\\net9.0", "").Replace("PhotographyAdapterGenerator\\","");
                var fullSearchPath = Path.Combine(replaces, searchPath);

                _logger.LogDebug("Checking search path: {SearchPath}", fullSearchPath);

                if (!Directory.Exists(replaces))
                {
                    _logger.LogDebug("Search path does not exist: {SearchPath}", fullSearchPath);
                    continue;
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