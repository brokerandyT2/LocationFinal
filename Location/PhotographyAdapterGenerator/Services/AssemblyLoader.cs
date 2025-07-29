using Microsoft.Extensions.Logging;
using System.Reflection;

namespace Location.Photography.Tools.AdapterGenerator.Services;

public class AssemblyLoader
{
    private readonly ILogger<AssemblyLoader> _logger;

    public AssemblyLoader(ILogger<AssemblyLoader> logger)
    {
        _logger = logger;
    }

    public async Task<List<Assembly>> LoadViewModelAssembliesAsync(GeneratorOptions options)
    {
        var assemblies = new List<Assembly>();

        try
        {
            // Load Core ViewModels assembly
            var coreAssembly = await LoadCoreAssemblyAsync(options.CoreAssemblyPath);
            if (coreAssembly != null)
            {
                assemblies.Add(coreAssembly);
            }

            // Load Photography ViewModels assembly  
            var photographyAssembly = await LoadPhotographyAssemblyAsync(options.PhotographyAssemblyPath);
            if (photographyAssembly != null)
            {
                assemblies.Add(photographyAssembly);
            }

            if (assemblies.Count == 0)
            {
                throw new InvalidOperationException(
                    "No ViewModel assemblies found. Make sure Location.Core.ViewModels and Location.Photography.ViewModels are built.");
            }

            _logger.LogInformation("Successfully loaded {Count} assemblies", assemblies.Count);
            return assemblies;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load ViewModel assemblies");
            throw;
        }
    }

    private async Task<Assembly?> LoadCoreAssemblyAsync(string? customPath)
    {
        const string assemblyName = "Location.Core.ViewModels.dll";

        if (!string.IsNullOrEmpty(customPath))
        {
            return await LoadAssemblyFromPathAsync(customPath, "Core (custom path)");
        }

        // Search common locations for Core assembly
        var searchPaths = new[]
        {
            // Current directory
            Directory.GetCurrentDirectory(),
            
            // Relative to generator (assuming typical repo structure)
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "Location.Core.ViewModels", "bin", "Debug", "net9.0"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "location-dotnet-core", "Location.Core.ViewModels", "bin", "Debug", "net9.0"),
            
            // Common build output paths
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "source", "repos", "location-dotnet-core", "Location.Core.ViewModels", "bin", "Debug", "net9.0"),
            @"C:\Source\location-dotnet-core\Location.Core.ViewModels\bin\Debug\net9.0",
        };

        return await FindAndLoadAssemblyAsync(assemblyName, searchPaths, "Core");
    }

    private async Task<Assembly?> LoadPhotographyAssemblyAsync(string? customPath)
    {
        const string assemblyName = "Location.Photography.ViewModels.dll";

        if (!string.IsNullOrEmpty(customPath))
        {
            return await LoadAssemblyFromPathAsync(customPath, "Photography (custom path)");
        }

        // Search common locations for Photography assembly
        var searchPaths = new[]
        {
            // Current directory
            Directory.GetCurrentDirectory(),
            
            // Relative to generator (assuming it's in the photography repo)
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "Location.Photography.ViewModels", "bin", "Debug", "net9.0"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "Location.Photography.ViewModels", "bin", "Debug", "net9.0"),
            
            // Common build output paths
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "source", "repos", "location-dotnet-photography", "Location.Photography.ViewModels", "bin", "Debug", "net9.0"),
            @"C:\Source\location-dotnet-photography\Location.Photography.ViewModels\bin\Debug\net9.0",
        };

        return await FindAndLoadAssemblyAsync(assemblyName, searchPaths, "Photography");
    }

    private async Task<Assembly?> FindAndLoadAssemblyAsync(string assemblyName, string[] searchPaths, string assemblyType)
    {
        foreach (var searchPath in searchPaths)
        {
            if (!Directory.Exists(searchPath))
            {
                _logger.LogDebug("Search path does not exist: {SearchPath}", searchPath);
                continue;
            }

            var assemblyPath = Path.Combine(searchPath, assemblyName);
            if (File.Exists(assemblyPath))
            {
                var assembly = await LoadAssemblyFromPathAsync(assemblyPath, assemblyType);
                if (assembly != null)
                {
                    return assembly;
                }
            }
        }

        _logger.LogWarning("Could not find {AssemblyType} assembly: {AssemblyName}", assemblyType, assemblyName);
        return null;
    }

    private async Task<Assembly?> LoadAssemblyFromPathAsync(string assemblyPath, string assemblyType)
    {
        try
        {
            _logger.LogDebug("Attempting to load {AssemblyType} assembly from: {AssemblyPath}", assemblyType, assemblyPath);

            // Use LoadFrom to handle dependencies properly
            var assembly = Assembly.LoadFrom(assemblyPath);

            // Validate that it's actually a ViewModel assembly
            if (!IsViewModelAssembly(assembly))
            {
                _logger.LogWarning("Assembly {AssemblyPath} does not appear to contain ViewModels", assemblyPath);
                return null;
            }

            _logger.LogInformation("Loaded {AssemblyType} assembly: {AssemblyName} from {AssemblyPath}",
                assemblyType, assembly.GetName().Name, assemblyPath);

            return assembly;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load {AssemblyType} assembly from: {AssemblyPath}", assemblyType, assemblyPath);
            return null;
        }
    }

    private bool IsViewModelAssembly(Assembly assembly)
    {
        try
        {
            // Check if assembly contains any types ending with "ViewModel"
            var viewModelTypes = assembly.GetTypes()
                .Where(t => t.Name.EndsWith("ViewModel") &&
                           t.IsClass &&
                           !t.IsAbstract)
                .Take(1); // Just check if any exist

            var hasViewModels = viewModelTypes.Any();

            if (hasViewModels)
            {
                _logger.LogDebug("Assembly {AssemblyName} contains ViewModel types", assembly.GetName().Name);
            }

            return hasViewModels;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error validating assembly {AssemblyName}", assembly.GetName().Name);
            return false; // Assume it's not a ViewModel assembly if we can't check
        }
    }

    public List<Type> GetViewModelTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes()
                .Where(t => t.Name.EndsWith("ViewModel") &&
                           t.IsClass &&
                           !t.IsAbstract &&
                           IsValidViewModelType(t))
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ViewModel types from assembly {AssemblyName}", assembly.GetName().Name);
            return new List<Type>();
        }
    }

    private bool IsValidViewModelType(Type type)
    {
        // Check if it inherits from BaseViewModel or ViewModelBase and implements IDisposable
        try
        {
            var implementsIDisposable = typeof(IDisposable).IsAssignableFrom(type);
            var inheritsFromBase = InheritsFromViewModelBase(type);

            return implementsIDisposable && inheritsFromBase;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error validating ViewModel type: {TypeName}", type.Name);
            return false;
        }
    }

    private bool InheritsFromViewModelBase(Type type)
    {
        var current = type.BaseType;
        while (current != null && current != typeof(object))
        {
            if (current.Name is "BaseViewModel" or "ViewModelBase")
            {
                return true;
            }
            current = current.BaseType;
        }
        return false;
    }
}