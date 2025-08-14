using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using x3squaredcircles.MobileAdapter.Generator.Configuration;
using x3squaredcircles.MobileAdapter.Generator.Logging;

namespace x3squaredcircles.MobileAdapter.Generator.Discovery
{
    public class CSharpDiscoveryEngine : IClassDiscoveryEngine
    {
        private readonly ILogger _logger;

        public CSharpDiscoveryEngine(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<List<DiscoveredClass>> DiscoverClassesAsync(GeneratorConfiguration config)
        {
            var discoveredClasses = new List<DiscoveredClass>();
            var conflictTracker = new Dictionary<string, DiscoveryConflict>();

            try
            {
                _logger.LogInfo("Loading C# assemblies for analysis...");

                var assemblies = await LoadAssembliesAsync(config);
                if (assemblies.Count == 0)
                {
                    _logger.LogError("No assemblies loaded for analysis");
                    return discoveredClasses;
                }

                _logger.LogInfo($"Loaded {assemblies.Count} assemblies");

                foreach (var assembly in assemblies)
                {
                    _logger.LogDebug($"Analyzing assembly: {assembly.GetName().Name}");

                    var types = GetTypesFromAssembly(assembly);
                    foreach (var type in types)
                    {
                        var discoveredClass = await AnalyzeTypeAsync(type, config, conflictTracker);
                        if (discoveredClass != null)
                        {
                            discoveredClasses.Add(discoveredClass);
                        }
                    }
                }

                // Check for conflicts
                var conflicts = conflictTracker.Values.Where(c => c.ConflictingSources.Count > 1).ToList();
                if (conflicts.Any())
                {
                    foreach (var conflict in conflicts)
                    {
                        _logger.LogError($"Discovery conflict detected for class '{conflict.ClassName}':");
                        foreach (var source in conflict.ConflictingSources)
                        {
                            _logger.LogError($"  - Found by {source.Method}: {source.Source} (in {source.FilePath})");
                        }
                    }
                    throw new InvalidOperationException($"Discovery conflicts detected for {conflicts.Count} classes. Resolve conflicts before proceeding.");
                }

                _logger.LogInfo($"C# discovery completed. Found {discoveredClasses.Count} classes.");
                return discoveredClasses;
            }
            catch (Exception ex)
            {
                _logger.LogError("C# class discovery failed", ex);
                throw;
            }
        }

        private async Task<List<Assembly>> LoadAssembliesAsync(GeneratorConfiguration config)
        {
            var assemblies = new List<Assembly>();

            try
            {
                // Load core assembly
                if (!string.IsNullOrWhiteSpace(config.Assembly.CoreAssemblyPath))
                {
                    var coreAssembly = await LoadAssemblyAsync(config.Assembly.CoreAssemblyPath);
                    if (coreAssembly != null)
                    {
                        assemblies.Add(coreAssembly);
                        _logger.LogDebug($"Loaded core assembly: {config.Assembly.CoreAssemblyPath}");
                    }
                }

                // Load target assembly
                if (!string.IsNullOrWhiteSpace(config.Assembly.TargetAssemblyPath))
                {
                    var targetAssembly = await LoadAssemblyAsync(config.Assembly.TargetAssemblyPath);
                    if (targetAssembly != null)
                    {
                        assemblies.Add(targetAssembly);
                        _logger.LogDebug($"Loaded target assembly: {config.Assembly.TargetAssemblyPath}");
                    }
                }

                // Load assemblies from search folders using pattern
                if (!string.IsNullOrWhiteSpace(config.Assembly.SearchFolders) &&
                    !string.IsNullOrWhiteSpace(config.Assembly.AssemblyPattern))
                {
                    var searchFolders = config.Assembly.SearchFolders.Split(':', StringSplitOptions.RemoveEmptyEntries);
                    var pattern = new Regex(config.Assembly.AssemblyPattern, RegexOptions.IgnoreCase);

                    foreach (var folder in searchFolders)
                    {
                        if (Directory.Exists(folder))
                        {
                            var assemblyFiles = Directory.GetFiles(folder, "*.dll", SearchOption.AllDirectories)
                                .Where(file => pattern.IsMatch(Path.GetFileName(file)));

                            foreach (var assemblyFile in assemblyFiles)
                            {
                                var assembly = await LoadAssemblyAsync(assemblyFile);
                                if (assembly != null)
                                {
                                    assemblies.Add(assembly);
                                    _logger.LogDebug($"Loaded assembly from search: {assemblyFile}");
                                }
                            }
                        }
                    }
                }

                return assemblies.Distinct().ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to load assemblies", ex);
                throw;
            }
        }

        private async Task<Assembly> LoadAssemblyAsync(string assemblyPath)
        {
            try
            {
                if (!File.Exists(assemblyPath))
                {
                    _logger.LogWarning($"Assembly file not found: {assemblyPath}");
                    return null;
                }

                var bytes = await File.ReadAllBytesAsync(assemblyPath);
                return Assembly.Load(bytes);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to load assembly {assemblyPath}: {ex.Message}");
                return null;
            }
        }

        private Type[] GetTypesFromAssembly(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                _logger.LogWarning($"Failed to load some types from {assembly.GetName().Name}: {ex.Message}");
                return ex.Types.Where(t => t != null).ToArray();
            }
        }

        private async Task<DiscoveredClass> AnalyzeTypeAsync(Type type, GeneratorConfiguration config, Dictionary<string, DiscoveryConflict> conflictTracker)
        {
            try
            {
                var discoveryResult = CheckDiscoveryMethods(type, config);
                if (discoveryResult == null)
                    return null;

                // Track potential conflicts
                TrackDiscoveryConflict(type, discoveryResult, conflictTracker);

                var discoveredClass = new DiscoveredClass
                {
                    Name = type.Name,
                    FullName = type.FullName,
                    Namespace = type.Namespace,
                    FilePath = GetTypeFilePath(type),
                    DiscoveryMethod = discoveryResult.Method,
                    DiscoverySource = discoveryResult.Source,
                    Attributes = type.GetCustomAttributes().Select(a => a.GetType().Name).ToList()
                };

                // Analyze properties
                discoveredClass.Properties = AnalyzeProperties(type);

                // Analyze methods
                discoveredClass.Methods = AnalyzeMethods(type);

                // Add metadata
                discoveredClass.Metadata["IsAbstract"] = type.IsAbstract;
                discoveredClass.Metadata["IsSealed"] = type.IsSealed;
                discoveredClass.Metadata["IsPublic"] = type.IsPublic;
                discoveredClass.Metadata["BaseType"] = type.BaseType?.FullName;

                return discoveredClass;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to analyze type {type.FullName}: {ex.Message}");
                return null;
            }
        }

        private DiscoveryResult CheckDiscoveryMethods(Type type, GeneratorConfiguration config)
        {
            // Attribute discovery
            if (!string.IsNullOrWhiteSpace(config.TrackAttribute))
            {
                var hasAttribute = type.GetCustomAttributes()
                    .Any(attr => attr.GetType().Name.Equals(config.TrackAttribute, StringComparison.OrdinalIgnoreCase) ||
                                attr.GetType().Name.Replace("Attribute", "").Equals(config.TrackAttribute, StringComparison.OrdinalIgnoreCase));

                if (hasAttribute)
                {
                    return new DiscoveryResult(DiscoveryMethod.Attribute, config.TrackAttribute);
                }
            }

            // Pattern discovery
            if (!string.IsNullOrWhiteSpace(config.TrackPattern))
            {
                var patterns = config.TrackPattern.Split('|', StringSplitOptions.RemoveEmptyEntries);
                foreach (var pattern in patterns)
                {
                    var regex = new Regex(pattern.Trim(), RegexOptions.IgnoreCase);
                    if (regex.IsMatch(type.Name) || regex.IsMatch(type.FullName))
                    {
                        return new DiscoveryResult(DiscoveryMethod.Pattern, pattern.Trim());
                    }
                }
            }

            // Namespace discovery
            if (!string.IsNullOrWhiteSpace(config.TrackNamespace))
            {
                var namespacePatterns = config.TrackNamespace.Split('|', StringSplitOptions.RemoveEmptyEntries);
                foreach (var namespacePattern in namespacePatterns)
                {
                    var pattern = namespacePattern.Trim().Replace("*", ".*");
                    var regex = new Regex($"^{pattern}$", RegexOptions.IgnoreCase);
                    if (regex.IsMatch(type.Namespace ?? ""))
                    {
                        return new DiscoveryResult(DiscoveryMethod.Namespace, namespacePattern.Trim());
                    }
                }
            }

            return null;
        }

        private void TrackDiscoveryConflict(Type type, DiscoveryResult result, Dictionary<string, DiscoveryConflict> conflictTracker)
        {
            var key = type.FullName;
            if (!conflictTracker.ContainsKey(key))
            {
                conflictTracker[key] = new DiscoveryConflict
                {
                    ClassName = type.Name,
                    FullName = type.FullName
                };
            }

            conflictTracker[key].ConflictingSources.Add(new DiscoveryMethodSource
            {
                Method = result.Method,
                Source = result.Source,
                FilePath = GetTypeFilePath(type)
            });
        }

        private string GetTypeFilePath(Type type)
        {
            try
            {
                return type.Assembly.Location;
            }
            catch
            {
                return "Unknown";
            }
        }

        private List<DiscoveredProperty> AnalyzeProperties(Type type)
        {
            var properties = new List<DiscoveredProperty>();

            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var discoveredProp = new DiscoveredProperty
                {
                    Name = prop.Name,
                    Type = GetFriendlyTypeName(prop.PropertyType),
                    IsNullable = IsNullableType(prop.PropertyType),
                    IsCollection = IsCollectionType(prop.PropertyType),
                    CollectionElementType = GetCollectionElementType(prop.PropertyType),
                    IsReadOnly = !prop.CanWrite,
                    IsPublic = true,
                    Attributes = prop.GetCustomAttributes().Select(a => a.GetType().Name).ToList()
                };

                properties.Add(discoveredProp);
            }

            return properties;
        }

        private List<DiscoveredMethod> AnalyzeMethods(Type type)
        {
            var methods = new List<DiscoveredMethod>();

            var publicMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => !m.IsSpecialName && m.DeclaringType == type);

            foreach (var method in publicMethods)
            {
                var discoveredMethod = new DiscoveredMethod
                {
                    Name = method.Name,
                    ReturnType = GetFriendlyTypeName(method.ReturnType),
                    IsAsync = IsAsyncMethod(method),
                    IsPublic = method.IsPublic,
                    Attributes = method.GetCustomAttributes().Select(a => a.GetType().Name).ToList()
                };

                // Analyze parameters
                foreach (var param in method.GetParameters())
                {
                    discoveredMethod.Parameters.Add(new DiscoveredParameter
                    {
                        Name = param.Name,
                        Type = GetFriendlyTypeName(param.ParameterType),
                        IsNullable = IsNullableType(param.ParameterType),
                        HasDefaultValue = param.HasDefaultValue,
                        DefaultValue = param.DefaultValue
                    });
                }

                methods.Add(discoveredMethod);
            }

            return methods;
        }

        private string GetFriendlyTypeName(Type type)
        {
            if (type.IsGenericType)
            {
                var genericTypeName = type.GetGenericTypeDefinition().Name;
                var genericArgs = string.Join(", ", type.GetGenericArguments().Select(GetFriendlyTypeName));
                return $"{genericTypeName.Substring(0, genericTypeName.IndexOf('`'))}<{genericArgs}>";
            }

            return type.Name;
        }

        private bool IsNullableType(Type type)
        {
            return Nullable.GetUnderlyingType(type) != null || !type.IsValueType;
        }

        private bool IsCollectionType(Type type)
        {
            return type != typeof(string) &&
                   (type.IsArray ||
                    type.GetInterfaces().Any(i => i.IsGenericType &&
                                                 (i.GetGenericTypeDefinition() == typeof(IEnumerable<>) ||
                                                  i.GetGenericTypeDefinition() == typeof(ICollection<>) ||
                                                  i.GetGenericTypeDefinition() == typeof(IList<>))));
        }

        private string GetCollectionElementType(Type type)
        {
            if (type.IsArray)
                return GetFriendlyTypeName(type.GetElementType());

            var enumerableInterface = type.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

            return enumerableInterface != null ? GetFriendlyTypeName(enumerableInterface.GetGenericArguments()[0]) : null;
        }

        private bool IsAsyncMethod(MethodInfo method)
        {
            return typeof(Task).IsAssignableFrom(method.ReturnType);
        }

        private class DiscoveryResult
        {
            public DiscoveryMethod Method { get; }
            public string Source { get; }

            public DiscoveryResult(DiscoveryMethod method, string source)
            {
                Method = method;
                Source = source;
            }
        }
    }
}