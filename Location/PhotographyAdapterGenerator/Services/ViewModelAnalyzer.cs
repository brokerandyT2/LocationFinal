using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Reflection;
using Location.Core.Helpers.AdapterGeneration;

namespace Location.Photography.Tools.AdapterGenerator.Services;

public class ViewModelAnalyzer
{
    private readonly ILogger<ViewModelAnalyzer> _logger;

    public ViewModelAnalyzer(ILogger<ViewModelAnalyzer> logger)
    {
        _logger = logger;
    }

    public async Task<List<ViewModelMetadata>> AnalyzeAssembliesAsync(List<string> assemblyPaths)
    {
        var allViewModels = new List<ViewModelMetadata>();

        foreach (var assemblyPath in assemblyPaths)
        {
            var source = DetermineAssemblySource(assemblyPath);
            var viewModels = await AnalyzeAssemblyPathAsync(assemblyPath, source);
            allViewModels.AddRange(viewModels);
        }

        _logger.LogInformation("Analyzed {TotalCount} ViewModels ({CoreCount} Core, {PhotoCount} Photography)",
            allViewModels.Count,
            allViewModels.Count(vm => vm.Source == "Core"),
            allViewModels.Count(vm => vm.Source == "Photography"));

        return allViewModels;
    }

    private async Task<List<ViewModelMetadata>> AnalyzeAssemblyPathAsync(string assemblyPath, string source)
    {
        var viewModels = new List<ViewModelMetadata>();

        try
        {
            _logger.LogDebug("Analyzing assembly: {AssemblyPath}", assemblyPath);

            // Simple approach: just load the assembly and get public surface
            var assembly = Assembly.LoadFrom(assemblyPath);

            var viewModelTypes = assembly.GetTypes()
                .Where(t => t.Name.EndsWith("ViewModel") &&
                           t.IsClass &&
                           t.IsPublic &&
                           !t.IsAbstract &&
                           !ShouldExcludeViewModel(t)) // NEW: Check for [Exclude] attribute
                .ToList();

            _logger.LogInformation("Found {Count} ViewModels in {Source} assembly: {ViewModels}",
                viewModelTypes.Count, source, string.Join(", ", viewModelTypes.Select(t => t.Name)));

            foreach (var type in viewModelTypes)
            {
                try
                {
                    var metadata = CreateSimpleViewModelMetadata(type, source);
                    viewModels.Add(metadata);
                    _logger.LogDebug("Analyzed ViewModel: {Name} - {PropertyCount} properties, {CommandCount} commands, {MethodCount} methods",
                        type.Name, metadata.Properties.Count, metadata.Commands.Count, metadata.Methods.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to analyze ViewModel: {Name}", type.Name);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze assembly: {AssemblyPath}", assemblyPath);
        }

        return viewModels;
    }

    private ViewModelMetadata CreateSimpleViewModelMetadata(Type type, string source)
    {
        return new ViewModelMetadata
        {
            Name = type.Name,
            AdapterName = type.Name.Replace("ViewModel", "Adapter"),
            FullName = type.FullName ?? type.Name,
            Namespace = type.Namespace ?? "",
            Source = source,
            IsDisposable = true, // We know all ViewModels are disposable
            Properties = GetPublicProperties(type),
            Commands = GetPublicCommands(type),
            Methods = GetPublicMethods(type),
            Events = GetPublicEvents(type),
            Constructor = new ConstructorMetadata(), // Don't need complex DI analysis
            // NEW: Extract class-level attributes
            AvailableAttribute = type.GetCustomAttribute<AvailableAttribute>(),
            ExcludeAttribute = type.GetCustomAttribute<ExcludeAttribute>(),
            GenerateAsAttribute = type.GetCustomAttribute<GenerateAsAttribute>()
        };
    }

    private List<PropertyMetadata> GetPublicProperties(Type type)
    {
        return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead &&
                       p.GetMethod?.IsPublic == true &&
                       !p.PropertyType.Name.Contains("Command") && // Exclude commands
                       !ShouldExcludeProperty(p)) // NEW: Check for exclusion attributes
            .Select(p => new PropertyMetadata
            {
                Name = p.Name,
                CamelCaseName = ToCamelCase(p.Name),
                Type = p.PropertyType,
                IsObservableCollection = IsObservableCollectionType(p.PropertyType),
                ElementType = GetObservableCollectionElementType(p.PropertyType),
                IsReadOnly = !p.CanWrite,
                HasNotifyPropertyChanged = true, // All ViewModels implement INotifyPropertyChanged
                // NEW: Extract property-level attributes
                MapToAttribute = p.GetCustomAttribute<MapToAttribute>(),
                DateTypeAttribute = p.GetCustomAttribute<DateTypeAttribute>(),
                AvailableAttribute = p.GetCustomAttribute<AvailableAttribute>(),
                ExcludeAttribute = p.GetCustomAttribute<ExcludeAttribute>(),
                GenerateAsAttribute = p.GetCustomAttribute<GenerateAsAttribute>(),
                WarnCustomImplementationNeededAttribute = p.GetCustomAttribute<WarnCustomImplementationNeededAttribute>(),
                CommandBehaviorAttribute = p.GetCustomAttribute<CommandBehaviorAttribute>(),
                CollectionBehaviorAttribute = p.GetCustomAttribute<CollectionBehaviorAttribute>(),
                ValidationBehaviorAttribute = p.GetCustomAttribute<ValidationBehaviorAttribute>(),
                ThreadingBehaviorAttribute = p.GetCustomAttribute<ThreadingBehaviorAttribute>()
            })
            .ToList();
    }

    private List<CommandMetadata> GetPublicCommands(Type type)
    {
        return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.PropertyType.Name.Contains("Command") &&
                       !ShouldExcludeProperty(p)) // NEW: Check for exclusion attributes
            .Select(p => new CommandMetadata
            {
                Name = p.Name,
                MethodName = GenerateMethodName(p.Name),
                IsAsync = p.PropertyType.Name.Contains("Async"),
                HasParameter = p.PropertyType.IsGenericType,
                ParameterType = p.PropertyType.IsGenericType ? p.PropertyType.GetGenericArguments().LastOrDefault() : null,
                // NEW: Extract command-level attributes
                AvailableAttribute = p.GetCustomAttribute<AvailableAttribute>(),
                ExcludeAttribute = p.GetCustomAttribute<ExcludeAttribute>(),
                GenerateAsAttribute = p.GetCustomAttribute<GenerateAsAttribute>(),
                WarnCustomImplementationNeededAttribute = p.GetCustomAttribute<WarnCustomImplementationNeededAttribute>(),
                CommandBehaviorAttribute = p.GetCustomAttribute<CommandBehaviorAttribute>(),
                ThreadingBehaviorAttribute = p.GetCustomAttribute<ThreadingBehaviorAttribute>()
            })
            .ToList();
    }

    private List<MethodMetadata> GetPublicMethods(Type type)
    {
        return type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => !m.IsSpecialName && // Excludes property getters/setters and event add/remove
                       !m.Name.StartsWith("get_") &&
                       !m.Name.StartsWith("set_") &&
                       !m.Name.StartsWith("add_") &&
                       !m.Name.StartsWith("remove_") &&
                       m.DeclaringType != typeof(object) && // Exclude Object methods like ToString()
                       !IsInheritedMethod(m) && // Exclude methods from base classes we don't want
                       !ShouldExcludeMethod(m)) // NEW: Check for exclusion attributes
            .Select(m => new MethodMetadata
            {
                Name = m.Name,
                MethodName = ToCamelCase(m.Name),
                IsAsync = m.ReturnType.Name.Contains("Task"),
                ReturnType = m.ReturnType,
                Parameters = m.GetParameters().Select(p => new ParameterMetadata
                {
                    Name = p.Name ?? "",
                    Type = p.ParameterType,
                    IsOptional = p.HasDefaultValue
                }).ToList(),
                // NEW: Extract method-level attributes
                AvailableAttribute = m.GetCustomAttribute<AvailableAttribute>(),
                ExcludeAttribute = m.GetCustomAttribute<ExcludeAttribute>(),
                GenerateAsAttribute = m.GetCustomAttribute<GenerateAsAttribute>(),
                WarnCustomImplementationNeededAttribute = m.GetCustomAttribute<WarnCustomImplementationNeededAttribute>(),
                ThreadingBehaviorAttribute = m.GetCustomAttribute<ThreadingBehaviorAttribute>()
            })
            .ToList();
    }

    private List<EventMetadata> GetPublicEvents(Type type)
    {
        return type.GetEvents(BindingFlags.Public | BindingFlags.Instance)
            .Select(e => new EventMetadata
            {
                Name = e.Name,
                EventArgsType = typeof(EventArgs),
                IsSystemEvent = e.Name == "ErrorOccurred"
            })
            .ToList();
    }

    // NEW: Attribute-based exclusion methods
    private bool ShouldExcludeViewModel(Type type)
    {
        var excludeAttr = type.GetCustomAttribute<ExcludeAttribute>();
        if (excludeAttr != null)
        {
            _logger.LogInformation("Excluding ViewModel {Name}: {Reason}",
                type.Name, excludeAttr.Reason ?? "Marked with [Exclude]");
            return true;
        }
        return false;
    }

    private bool ShouldExcludeProperty(PropertyInfo property)
    {
        var excludeAttr = property.GetCustomAttribute<ExcludeAttribute>();
        if (excludeAttr != null)
        {
            _logger.LogDebug("Excluding property {Property}: {Reason}",
                $"{property.DeclaringType?.Name}.{property.Name}",
                excludeAttr.Reason ?? "Marked with [Exclude]");
            return true;
        }
        return false;
    }

    private bool ShouldExcludeMethod(MethodInfo method)
    {
        var excludeAttr = method.GetCustomAttribute<ExcludeAttribute>();
        if (excludeAttr != null)
        {
            _logger.LogDebug("Excluding method {Method}: {Reason}",
                $"{method.DeclaringType?.Name}.{method.Name}",
                excludeAttr.Reason ?? "Marked with [Exclude]");
            return true;
        }
        return false;
    }

    // NEW: Platform-specific exclusion check
    public bool ShouldExcludeForPlatform(MemberInfo member, string platform)
    {
        var availableAttr = member.GetCustomAttribute<AvailableAttribute>();
        if (availableAttr != null)
        {
            return platform.ToLower() switch
            {
                "android" => !availableAttr.Android,
                "ios" => !availableAttr.iOS,
                _ => false
            };
        }
        return false;
    }

    private bool IsInheritedMethod(MethodInfo method)
    {
        // Skip common base class methods we don't want to expose
        var skipMethods = new[]
        {
            "Dispose", "GetHashCode", "GetType", "Equals", "ToString",
            "OnPropertyChanged", "SetProperty", "TrackCommand", "ExecuteAndTrackAsync",
            "RetryLastCommandAsync", "OnSystemError", "ClearErrors"
        };

        return skipMethods.Contains(method.Name);
    }

    private bool IsObservableCollectionType(Type type)
    {
        return type.IsGenericType &&
               type.GetGenericTypeDefinition() == typeof(ObservableCollection<>);
    }

    private Type? GetObservableCollectionElementType(Type type)
    {
        if (IsObservableCollectionType(type))
            return type.GetGenericArguments()[0];
        return null;
    }

    private string GenerateMethodName(string commandName)
    {
        // SaveLocationCommand → saveLocation
        return ToCamelCase(commandName.Replace("Command", ""));
    }

    private string ToCamelCase(string input)
    {
        if (string.IsNullOrEmpty(input) || char.IsLower(input[0]))
            return input;

        return char.ToLower(input[0]) + input[1..];
    }

    private string DetermineAssemblySource(string assemblyPath)
    {
        var fileName = Path.GetFileNameWithoutExtension(assemblyPath);

        if (fileName.Contains("Core.ViewModels", StringComparison.OrdinalIgnoreCase))
            return "Core";
        else if (fileName.Contains("Photography.ViewModels", StringComparison.OrdinalIgnoreCase))
            return "Photography";
        else
            return "Unknown";
    }
}

// Enhanced metadata classes with attribute support
public class ViewModelMetadata
{
    public string Name { get; set; } = string.Empty;
    public string AdapterName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty; // "Core" or "Photography"
    public List<PropertyMetadata> Properties { get; set; } = new();
    public List<CommandMetadata> Commands { get; set; } = new();
    public List<MethodMetadata> Methods { get; set; } = new();
    public List<EventMetadata> Events { get; set; } = new();
    public ConstructorMetadata Constructor { get; set; } = new();
    public bool IsDisposable { get; set; } = true;
    public string BaseClass { get; set; } = string.Empty;

    // NEW: Class-level attributes
    public AvailableAttribute? AvailableAttribute { get; set; }
    public ExcludeAttribute? ExcludeAttribute { get; set; }
    public GenerateAsAttribute? GenerateAsAttribute { get; set; }
}

public class PropertyMetadata
{
    public string Name { get; set; } = string.Empty;
    public string CamelCaseName { get; set; } = string.Empty;
    public Type Type { get; set; } = typeof(object);
    public bool IsObservableCollection { get; set; }
    public Type? ElementType { get; set; }
    public bool IsReadOnly { get; set; }
    public bool HasNotifyPropertyChanged { get; set; }

    // NEW: Property-level attributes
    public MapToAttribute? MapToAttribute { get; set; }
    public DateTypeAttribute? DateTypeAttribute { get; set; }
    public AvailableAttribute? AvailableAttribute { get; set; }
    public ExcludeAttribute? ExcludeAttribute { get; set; }
    public GenerateAsAttribute? GenerateAsAttribute { get; set; }
    public WarnCustomImplementationNeededAttribute? WarnCustomImplementationNeededAttribute { get; set; }
    public CommandBehaviorAttribute? CommandBehaviorAttribute { get; set; }
    public CollectionBehaviorAttribute? CollectionBehaviorAttribute { get; set; }
    public ValidationBehaviorAttribute? ValidationBehaviorAttribute { get; set; }
    public ThreadingBehaviorAttribute? ThreadingBehaviorAttribute { get; set; }
}

public class CommandMetadata
{
    public string Name { get; set; } = string.Empty;
    public string MethodName { get; set; } = string.Empty;
    public bool IsAsync { get; set; }
    public Type? ParameterType { get; set; }
    public bool HasParameter { get; set; }
    public bool HasCanExecute { get; set; }

    // NEW: Command-level attributes
    public AvailableAttribute? AvailableAttribute { get; set; }
    public ExcludeAttribute? ExcludeAttribute { get; set; }
    public GenerateAsAttribute? GenerateAsAttribute { get; set; }
    public WarnCustomImplementationNeededAttribute? WarnCustomImplementationNeededAttribute { get; set; }
    public CommandBehaviorAttribute? CommandBehaviorAttribute { get; set; }
    public ThreadingBehaviorAttribute? ThreadingBehaviorAttribute { get; set; }
}

public class MethodMetadata
{
    public string Name { get; set; } = string.Empty;
    public string MethodName { get; set; } = string.Empty;
    public bool IsAsync { get; set; }
    public Type ReturnType { get; set; } = typeof(void);
    public List<ParameterMetadata> Parameters { get; set; } = new();

    // NEW: Method-level attributes
    public AvailableAttribute? AvailableAttribute { get; set; }
    public ExcludeAttribute? ExcludeAttribute { get; set; }
    public GenerateAsAttribute? GenerateAsAttribute { get; set; }
    public WarnCustomImplementationNeededAttribute? WarnCustomImplementationNeededAttribute { get; set; }
    public ThreadingBehaviorAttribute? ThreadingBehaviorAttribute { get; set; }
}

public class EventMetadata
{
    public string Name { get; set; } = string.Empty;
    public Type EventArgsType { get; set; } = typeof(EventArgs);
    public bool IsSystemEvent { get; set; }
}

public class ConstructorMetadata
{
    public List<ParameterMetadata> Parameters { get; set; } = new();
    public bool RequiresComplexDI { get; set; }
}

public class ParameterMetadata
{
    public string Name { get; set; } = string.Empty;
    public Type Type { get; set; } = typeof(object);
    public bool IsInterface { get; set; }
    public bool IsOptional { get; set; }
}