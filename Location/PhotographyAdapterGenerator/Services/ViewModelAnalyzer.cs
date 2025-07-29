using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Reflection;

namespace Location.Photography.Tools.AdapterGenerator.Services;

public class ViewModelAnalyzer
{
    private readonly ILogger<ViewModelAnalyzer> _logger;

    public ViewModelAnalyzer(ILogger<ViewModelAnalyzer> logger)
    {
        _logger = logger;
    }

    public async Task<List<ViewModelMetadata>> AnalyzeAssembliesAsync(List<Assembly> assemblies)
    {
        var allViewModels = new List<ViewModelMetadata>();

        foreach (var assembly in assemblies)
        {
            var source = DetermineAssemblySource(assembly);
            var viewModels = await AnalyzeAssemblyAsync(assembly, source);
            allViewModels.AddRange(viewModels);
        }

        _logger.LogInformation("Analyzed {TotalCount} ViewModels ({CoreCount} Core, {PhotoCount} Photography)",
            allViewModels.Count,
            allViewModels.Count(vm => vm.Source == "Core"),
            allViewModels.Count(vm => vm.Source == "Photography"));

        return allViewModels;
    }

    private async Task<List<ViewModelMetadata>> AnalyzeAssemblyAsync(Assembly assembly, string source)
    {
        var viewModels = new List<ViewModelMetadata>();

        var viewModelTypes = assembly.GetTypes()
            .Where(t => t.Name.EndsWith("ViewModel") &&
                       t.IsClass &&
                       !t.IsAbstract &&
                       IsValidViewModelType(t))
            .ToList();

        _logger.LogInformation("Found {Count} ViewModels in {Source} assembly: {ViewModels}",
            viewModelTypes.Count, source, string.Join(", ", viewModelTypes.Select(t => t.Name)));

        foreach (var type in viewModelTypes)
        {
            try
            {
                var metadata = await AnalyzeViewModelAsync(type, source);
                viewModels.Add(metadata);
                _logger.LogDebug("Analyzed ViewModel: {Name} - {PropertyCount} properties, {CommandCount} commands",
                    type.Name, metadata.Properties.Count, metadata.Commands.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to analyze ViewModel: {Name}", type.Name);
            }
        }

        return viewModels;
    }

    private string DetermineAssemblySource(Assembly assembly)
    {
        var assemblyName = assembly.GetName().Name ?? "";

        if (assemblyName.Contains("Core.ViewModels"))
            return "Core";
        else if (assemblyName.Contains("Photography.ViewModels"))
            return "Photography";
        else
            return "Unknown";
    }

    private bool IsValidViewModelType(Type type)
    {
        try
        {
            // Must implement IDisposable (from BaseViewModel/ViewModelBase)
            var implementsIDisposable = typeof(IDisposable).IsAssignableFrom(type);

            // Must inherit from BaseViewModel or ViewModelBase
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

    private async Task<ViewModelMetadata> AnalyzeViewModelAsync(Type viewModelType, string source)
    {
        var metadata = new ViewModelMetadata
        {
            Name = viewModelType.Name,
            AdapterName = GenerateAdapterName(viewModelType.Name),
            FullName = viewModelType.FullName ?? viewModelType.Name,
            Namespace = viewModelType.Namespace ?? "",
            Source = source,
            IsDisposable = true, // All ViewModels inherit from BaseViewModel/ViewModelBase
            BaseClass = DetermineBaseClass(viewModelType)
        };

        // Analyze properties
        metadata.Properties = AnalyzeProperties(viewModelType);

        // Analyze commands  
        metadata.Commands = AnalyzeCommands(viewModelType);

        // Analyze events
        metadata.Events = AnalyzeEvents(viewModelType);

        // Analyze constructor
        metadata.Constructor = AnalyzeConstructor(viewModelType);

        return metadata;
    }

    private string GenerateAdapterName(string viewModelName)
    {
        // AstroPhotographyCalculatorViewModel → AstroPhotographyCalculatorAdapter
        return viewModelName.Replace("ViewModel", "Adapter");
    }

    private string DetermineBaseClass(Type viewModelType)
    {
        var baseType = viewModelType.BaseType;
        while (baseType != null && baseType != typeof(object))
        {
            if (baseType.Name is "BaseViewModel" or "ViewModelBase")
                return baseType.Name;
            baseType = baseType.BaseType;
        }
        return "ViewModel"; // Default Android ViewModel
    }

    private List<PropertyMetadata> AnalyzeProperties(Type viewModelType)
    {
        var properties = new List<PropertyMetadata>();

        var publicProperties = viewModelType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => ShouldExposeProperty(p))
            .ToList();

        foreach (var prop in publicProperties)
        {
            try
            {
                var metadata = new PropertyMetadata
                {
                    Name = prop.Name,
                    CamelCaseName = ToCamelCase(prop.Name),
                    Type = prop.PropertyType,
                    IsObservableCollection = IsObservableCollectionType(prop.PropertyType),
                    ElementType = GetObservableCollectionElementType(prop.PropertyType),
                    IsReadOnly = !prop.CanWrite,
                    HasNotifyPropertyChanged = true // Both base classes implement INotifyPropertyChanged
                };

                properties.Add(metadata);
                _logger.LogTrace("Found property: {PropertyName} ({PropertyType})", prop.Name, prop.PropertyType.Name);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to analyze property: {PropertyName}", prop.Name);
            }
        }

        return properties;
    }

    private List<CommandMetadata> AnalyzeCommands(Type viewModelType)
    {
        var commands = new List<CommandMetadata>();

        var commandProperties = viewModelType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => IsCommandProperty(p))
            .ToList();

        foreach (var prop in commandProperties)
        {
            try
            {
                var metadata = new CommandMetadata
                {
                    Name = prop.Name,
                    MethodName = GenerateMethodName(prop.Name),
                    IsAsync = IsAsyncCommandType(prop.PropertyType),
                    ParameterType = ExtractCommandParameterType(prop.PropertyType),
                    HasParameter = ExtractCommandParameterType(prop.PropertyType) != null,
                    HasCanExecute = HasCanExecuteMethod(prop)
                };

                commands.Add(metadata);
                _logger.LogTrace("Found command: {CommandName} (Async: {IsAsync}, HasParam: {HasParam})",
                    prop.Name, metadata.IsAsync, metadata.HasParameter);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to analyze command: {CommandName}", prop.Name);
            }
        }

        return commands;
    }

    private List<EventMetadata> AnalyzeEvents(Type viewModelType)
    {
        var events = new List<EventMetadata>();

        var publicEvents = viewModelType.GetEvents(BindingFlags.Public | BindingFlags.Instance);

        foreach (var evt in publicEvents)
        {
            try
            {
                var metadata = new EventMetadata
                {
                    Name = evt.Name,
                    EventArgsType = GetEventArgsType(evt),
                    IsSystemEvent = evt.Name == "ErrorOccurred"
                };

                events.Add(metadata);
                _logger.LogTrace("Found event: {EventName} ({EventArgsType})", evt.Name, metadata.EventArgsType.Name);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to analyze event: {EventName}", evt.Name);
            }
        }

        return events;
    }

    private ConstructorMetadata AnalyzeConstructor(Type viewModelType)
    {
        var constructors = viewModelType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

        // Find the constructor with the most parameters (primary DI constructor)
        var primaryConstructor = constructors
            .Where(c => c.GetParameters().Length > 0) // Skip parameterless constructors
            .OrderByDescending(c => c.GetParameters().Length)
            .FirstOrDefault();

        if (primaryConstructor == null)
        {
            return new ConstructorMetadata();
        }

        var parameters = primaryConstructor.GetParameters()
            .Select(p => new ParameterMetadata
            {
                Name = p.Name ?? "",
                Type = p.ParameterType,
                IsInterface = p.ParameterType.IsInterface,
                IsOptional = p.HasDefaultValue
            })
            .ToList();

        return new ConstructorMetadata
        {
            Parameters = parameters,
            RequiresComplexDI = parameters.Count > 3 || parameters.Any(p => p.IsInterface)
        };
    }

    // Helper methods for property analysis
    private bool ShouldExposeProperty(PropertyInfo property)
    {
        // Skip inherited properties from base classes that we don't want to expose
        var skipProperties = new[]
        {
            "IsBusy", "IsError", "ErrorMessage", "HasActiveErrors",
            "LastCommand", "LastCommandParameter"
        };

        return !skipProperties.Contains(property.Name) &&
               !property.PropertyType.IsSubclassOf(typeof(Delegate)) &&
               property.GetMethod != null &&
               property.GetMethod.IsPublic;
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

    // Helper methods for command analysis
    private bool IsCommandProperty(PropertyInfo property)
    {
        var commandTypeNames = new[]
        {
            "ICommand", "IRelayCommand", "IAsyncRelayCommand",
            "RelayCommand", "AsyncRelayCommand"
        };

        var typeName = property.PropertyType.Name;
        var interfaceNames = property.PropertyType.GetInterfaces().Select(i => i.Name);

        return commandTypeNames.Any(ct =>
            typeName.Contains(ct) ||
            interfaceNames.Any(i => i.Contains(ct)));
    }

    private bool IsAsyncCommandType(Type commandType)
    {
        return commandType.Name.Contains("Async") ||
               commandType.GetInterfaces().Any(i => i.Name.Contains("Async"));
    }

    private Type? ExtractCommandParameterType(Type commandType)
    {
        if (commandType.IsGenericType)
        {
            var args = commandType.GetGenericArguments();
            return args.LastOrDefault(); // Last generic argument is usually the parameter type
        }
        return null;
    }

    private bool HasCanExecuteMethod(PropertyInfo commandProperty)
    {
        var canExecuteMethodName = $"Can{commandProperty.Name.Replace("Command", "")}";
        return commandProperty.DeclaringType?.GetMethod(canExecuteMethodName) != null;
    }

    // Helper methods for event analysis
    private Type GetEventArgsType(EventInfo eventInfo)
    {
        var handlerType = eventInfo.EventHandlerType;
        if (handlerType?.IsGenericType == true)
        {
            var args = handlerType.GetGenericArguments();
            return args.Length > 1 ? args[1] : typeof(EventArgs);
        }
        return typeof(EventArgs);
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
}

// Supporting metadata classes
public class ViewModelMetadata
{
    public string Name { get; set; } = string.Empty;
    public string AdapterName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty; // "Core" or "Photography"
    public List<PropertyMetadata> Properties { get; set; } = new();
    public List<CommandMetadata> Commands { get; set; } = new();
    public List<EventMetadata> Events { get; set; } = new();
    public ConstructorMetadata Constructor { get; set; } = new();
    public bool IsDisposable { get; set; } = true;
    public string BaseClass { get; set; } = string.Empty;
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
}

public class CommandMetadata
{
    public string Name { get; set; } = string.Empty;
    public string MethodName { get; set; } = string.Empty;
    public bool IsAsync { get; set; }
    public Type? ParameterType { get; set; }
    public bool HasParameter { get; set; }
    public bool HasCanExecute { get; set; }
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