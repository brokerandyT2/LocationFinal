using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using x3squaredcircles.MobileAdapter.Generator.Configuration;
using x3squaredcircles.MobileAdapter.Generator.Logging;

namespace x3squaredcircles.MobileAdapter.Generator.Discovery
{
    public class TypeScriptDiscoveryEngine : IClassDiscoveryEngine
    {
        private readonly ILogger _logger;

        public TypeScriptDiscoveryEngine(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<List<DiscoveredClass>> DiscoverClassesAsync(GeneratorConfiguration config)
        {
            var discoveredClasses = new List<DiscoveredClass>();
            var conflictTracker = new Dictionary<string, DiscoveryConflict>();

            try
            {
                _logger.LogInfo("Discovering TypeScript classes for analysis...");

                var sourcePaths = GetSourcePaths(config);
                if (sourcePaths.Count == 0)
                {
                    _logger.LogError("No TypeScript source paths found");
                    return discoveredClasses;
                }

                _logger.LogInfo($"Scanning {sourcePaths.Count} source paths for TypeScript files");

                foreach (var sourcePath in sourcePaths)
                {
                    if (!Directory.Exists(sourcePath))
                    {
                        _logger.LogWarning($"Source path does not exist: {sourcePath}");
                        continue;
                    }

                    var tsFiles = Directory.GetFiles(sourcePath, "*.ts", SearchOption.AllDirectories)
                        .Where(f => !f.Contains("node_modules") && !f.EndsWith(".d.ts"))
                        .ToArray();

                    _logger.LogDebug($"Found {tsFiles.Length} TypeScript files in {sourcePath}");

                    foreach (var tsFile in tsFiles)
                    {
                        var fileClasses = await AnalyzeTypeScriptFileAsync(tsFile, config, conflictTracker);
                        discoveredClasses.AddRange(fileClasses);
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

                _logger.LogInfo($"TypeScript discovery completed. Found {discoveredClasses.Count} classes.");
                return discoveredClasses;
            }
            catch (Exception ex)
            {
                _logger.LogError("TypeScript class discovery failed", ex);
                throw;
            }
        }

        private List<string> GetSourcePaths(GeneratorConfiguration config)
        {
            var paths = new List<string>();

            if (!string.IsNullOrWhiteSpace(config.Source.SourcePaths))
            {
                paths.AddRange(config.Source.SourcePaths.Split(':', StringSplitOptions.RemoveEmptyEntries));
            }

            return paths.Select(p => p.Trim()).Where(p => !string.IsNullOrEmpty(p)).ToList();
        }

        private async Task<List<DiscoveredClass>> AnalyzeTypeScriptFileAsync(string filePath, GeneratorConfiguration config, Dictionary<string, DiscoveryConflict> conflictTracker)
        {
            var discoveredClasses = new List<DiscoveredClass>();

            try
            {
                var content = await File.ReadAllTextAsync(filePath);
                var classes = ParseTypeScriptClasses(content, filePath);

                foreach (var classInfo in classes)
                {
                    var discoveryResult = CheckDiscoveryMethods(classInfo, config, filePath);
                    if (discoveryResult == null)
                        continue;

                    // Track potential conflicts
                    TrackDiscoveryConflict(classInfo, discoveryResult, conflictTracker, filePath);

                    var discoveredClass = new DiscoveredClass
                    {
                        Name = classInfo.Name,
                        FullName = classInfo.FullName,
                        Namespace = classInfo.Module,
                        FilePath = filePath,
                        DiscoveryMethod = discoveryResult.Method,
                        DiscoverySource = discoveryResult.Source,
                        Attributes = classInfo.Decorators,
                        Properties = classInfo.Properties.Select(p => new DiscoveredProperty
                        {
                            Name = p.Name,
                            Type = p.Type,
                            IsPublic = p.IsPublic,
                            IsNullable = p.IsNullable,
                            IsReadOnly = p.IsReadOnly,
                            Attributes = p.Decorators
                        }).ToList(),
                        Methods = classInfo.Methods.Select(m => new DiscoveredMethod
                        {
                            Name = m.Name,
                            ReturnType = m.ReturnType,
                            IsPublic = m.IsPublic,
                            IsAsync = m.IsAsync,
                            Attributes = m.Decorators,
                            Parameters = m.Parameters.Select(p => new DiscoveredParameter
                            {
                                Name = p.Name,
                                Type = p.Type,
                                IsNullable = p.IsNullable,
                                HasDefaultValue = p.HasDefaultValue
                            }).ToList()
                        }).ToList()
                    };

                    discoveredClass.Metadata["IsAbstract"] = classInfo.IsAbstract;
                    discoveredClass.Metadata["IsExported"] = classInfo.IsExported;
                    discoveredClass.Metadata["IsDefault"] = classInfo.IsDefault;
                    discoveredClass.Metadata["SuperClass"] = classInfo.SuperClass;
                    discoveredClass.Metadata["Interfaces"] = classInfo.Interfaces;
                    discoveredClass.Metadata["TypeParameters"] = classInfo.TypeParameters;

                    discoveredClasses.Add(discoveredClass);
                }

                return discoveredClasses;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to analyze TypeScript file {filePath}: {ex.Message}");
                return new List<DiscoveredClass>();
            }
        }

        private List<TypeScriptClassInfo> ParseTypeScriptClasses(string content, string filePath)
        {
            var classes = new List<TypeScriptClassInfo>();

            try
            {
                // Remove comments
                content = RemoveComments(content);

                // Find class declarations
                var classPattern = @"(?:@\w+(?:\([^)]*\))?\s*)*(?:export\s+(?:default\s+)?)?(?:(abstract)\s+)?class\s+(\w+)(?:<([^>]+)>)?(?:\s+extends\s+(\w+)(?:<[^>]*>)?)?(?:\s+implements\s+([\w\s,<>]+?))?\s*\{([^}]*(?:\{[^}]*\}[^}]*)*)\}";
                var matches = Regex.Matches(content, classPattern, RegexOptions.Singleline);

                foreach (Match match in matches)
                {
                    var classInfo = new TypeScriptClassInfo
                    {
                        Name = match.Groups[2].Value,
                        IsAbstract = !string.IsNullOrEmpty(match.Groups[1].Value),
                        SuperClass = match.Groups[4].Success ? match.Groups[4].Value : null,
                        FilePath = filePath,
                        IsExported = match.Value.Contains("export"),
                        IsDefault = match.Value.Contains("export default")
                    };

                    // Parse type parameters
                    if (match.Groups[3].Success)
                    {
                        classInfo.TypeParameters = ParseTypeParameters(match.Groups[3].Value);
                    }

                    // Parse interfaces
                    if (match.Groups[5].Success)
                    {
                        classInfo.Interfaces = match.Groups[5].Value.Split(',')
                            .Select(i => i.Trim()).Where(i => !string.IsNullOrEmpty(i)).ToList();
                    }

                    classInfo.FullName = GetFullName(classInfo.Name, filePath);
                    classInfo.Module = GetModuleName(filePath);

                    var classBody = match.Groups[6].Value;

                    // Extract decorators
                    var classStart = match.Index;
                    var contentBeforeClass = content.Substring(Math.Max(0, classStart - 300), Math.Min(300, classStart));
                    classInfo.Decorators = ExtractDecorators(contentBeforeClass);

                    // Parse class members
                    classInfo.Properties = ExtractTypeScriptProperties(classBody);
                    classInfo.Methods = ExtractTypeScriptMethods(classBody);

                    classes.Add(classInfo);
                }

                // Find interface declarations that might be class-like
                var interfaceClasses = ParseInterfaces(content, filePath);
                classes.AddRange(interfaceClasses);

                return classes;
            }
            catch (Exception ex)
            {
                _logger.LogDebug($"Failed to parse TypeScript classes in {filePath}: {ex.Message}");
                return new List<TypeScriptClassInfo>();
            }
        }

        private string RemoveComments(string content)
        {
            // Remove single-line comments
            content = Regex.Replace(content, @"//.*$", "", RegexOptions.Multiline);

            // Remove multi-line comments
            content = Regex.Replace(content, @"/\*.*?\*/", "", RegexOptions.Singleline);

            return content;
        }

        private List<TypeScriptClassInfo> ParseInterfaces(string content, string filePath)
        {
            var interfaces = new List<TypeScriptClassInfo>();

            // Match interface declarations that look like they could be adapted
            var interfacePattern = @"(?:export\s+)?interface\s+(\w+)(?:<([^>]+)>)?(?:\s+extends\s+([\w\s,<>]+?))?\s*\{([^}]*(?:\{[^}]*\}[^}]*)*)\}";
            var matches = Regex.Matches(content, interfacePattern, RegexOptions.Singleline);

            foreach (Match match in matches)
            {
                var interfaceBody = match.Groups[4].Value;

                // Only consider interfaces with methods (not just data contracts)
                if (Regex.IsMatch(interfaceBody, @"\w+\s*\([^)]*\)\s*:"))
                {
                    var classInfo = new TypeScriptClassInfo
                    {
                        Name = match.Groups[1].Value,
                        FilePath = filePath,
                        IsInterface = true,
                        IsExported = match.Value.Contains("export")
                    };

                    if (match.Groups[2].Success)
                    {
                        classInfo.TypeParameters = ParseTypeParameters(match.Groups[2].Value);
                    }

                    if (match.Groups[3].Success)
                    {
                        classInfo.Interfaces = match.Groups[3].Value.Split(',')
                            .Select(i => i.Trim()).Where(i => !string.IsNullOrEmpty(i)).ToList();
                    }

                    classInfo.FullName = GetFullName(classInfo.Name, filePath);
                    classInfo.Module = GetModuleName(filePath);

                    // Parse interface members
                    classInfo.Properties = ExtractInterfaceProperties(interfaceBody);
                    classInfo.Methods = ExtractInterfaceMethods(interfaceBody);

                    interfaces.Add(classInfo);
                }
            }

            return interfaces;
        }

        private List<string> ParseTypeParameters(string typeParams)
        {
            return typeParams.Split(',').Select(t => t.Trim()).Where(t => !string.IsNullOrEmpty(t)).ToList();
        }

        private List<string> ExtractDecorators(string content)
        {
            var decorators = new List<string>();
            var decoratorPattern = @"@(\w+)(?:\([^)]*\))?";
            var matches = Regex.Matches(content, decoratorPattern);

            foreach (Match match in matches)
            {
                decorators.Add(match.Groups[1].Value);
            }

            return decorators;
        }

        private List<TypeScriptPropertyInfo> ExtractTypeScriptProperties(string classBody)
        {
            var properties = new List<TypeScriptPropertyInfo>();

            // Match property declarations
            var propertyPattern = @"(?:@\w+(?:\([^)]*\))?\s*)*(?:(public|private|protected|readonly)\s+)*(?:(static)\s+)?(?:(readonly)\s+)?(\w+)(?:\?)?:\s*([^=;\n]+)(?:\s*=\s*[^;\n]*)?[;\n]";
            var matches = Regex.Matches(classBody, propertyPattern, RegexOptions.Multiline);

            foreach (Match match in matches)
            {
                var visibility = match.Groups[1].Value;
                var isStatic = !string.IsNullOrEmpty(match.Groups[2].Value);
                var isReadonly = !string.IsNullOrEmpty(match.Groups[3].Value) || visibility == "readonly";
                var name = match.Groups[4].Value;
                var type = match.Groups[5].Value.Trim();

                var property = new TypeScriptPropertyInfo
                {
                    Name = name,
                    Type = type,
                    IsPublic = string.IsNullOrEmpty(visibility) || visibility == "public",
                    IsReadOnly = isReadonly,
                    IsStatic = isStatic,
                    IsNullable = match.Value.Contains('?') || type.Contains("null") || type.Contains("undefined")
                };

                // Extract decorators for this property
                var propertyStart = match.Index;
                var contentBeforeProperty = classBody.Substring(Math.Max(0, propertyStart - 100), Math.Min(100, propertyStart));
                property.Decorators = ExtractDecorators(contentBeforeProperty);

                properties.Add(property);
            }

            return properties;
        }

        private List<TypeScriptMethodInfo> ExtractTypeScriptMethods(string classBody)
        {
            var methods = new List<TypeScriptMethodInfo>();

            // Match method declarations
            var methodPattern = @"(?:@\w+(?:\([^)]*\))?\s*)*(?:(public|private|protected)\s+)?(?:(static|abstract|async)\s+)*(\w+)(?:<([^>]+)>)?\s*\(([^)]*)\)\s*:\s*([^{=\n]+)(?:\s*\{|\s*;)";
            var matches = Regex.Matches(classBody, methodPattern, RegexOptions.Multiline);

            foreach (Match match in matches)
            {
                var visibility = match.Groups[1].Value;
                var modifiers = match.Groups[2].Value;
                var name = match.Groups[3].Value;
                var typeParams = match.Groups[4].Value;
                var parameters = match.Groups[5].Value;
                var returnType = match.Groups[6].Value.Trim();

                if (name != "constructor")
                {
                    var method = new TypeScriptMethodInfo
                    {
                        Name = name,
                        ReturnType = returnType,
                        IsPublic = string.IsNullOrEmpty(visibility) || visibility == "public",
                        IsStatic = modifiers.Contains("static"),
                        IsAbstract = modifiers.Contains("abstract"),
                        IsAsync = modifiers.Contains("async")
                    };

                    if (!string.IsNullOrEmpty(typeParams))
                    {
                        method.TypeParameters = ParseTypeParameters(typeParams);
                    }

                    // Parse parameters
                    method.Parameters = ParseTypeScriptParameters(parameters);

                    // Extract decorators for this method
                    var methodStart = match.Index;
                    var contentBeforeMethod = classBody.Substring(Math.Max(0, methodStart - 200), Math.Min(200, methodStart));
                    method.Decorators = ExtractDecorators(contentBeforeMethod);

                    methods.Add(method);
                }
            }

            return methods;
        }

        private List<TypeScriptPropertyInfo> ExtractInterfaceProperties(string interfaceBody)
        {
            var properties = new List<TypeScriptPropertyInfo>();

            // Match property declarations in interfaces
            var propertyPattern = @"(?:(readonly)\s+)?(\w+)(\?)?\s*:\s*([^;\n]+)[;\n]";
            var matches = Regex.Matches(interfaceBody, propertyPattern, RegexOptions.Multiline);

            foreach (Match match in matches)
            {
                var isReadonly = !string.IsNullOrEmpty(match.Groups[1].Value);
                var name = match.Groups[2].Value;
                var isOptional = !string.IsNullOrEmpty(match.Groups[3].Value);
                var type = match.Groups[4].Value.Trim();

                properties.Add(new TypeScriptPropertyInfo
                {
                    Name = name,
                    Type = type,
                    IsPublic = true,
                    IsReadOnly = isReadonly,
                    IsNullable = isOptional || type.Contains("null") || type.Contains("undefined")
                });
            }

            return properties;
        }

        private List<TypeScriptMethodInfo> ExtractInterfaceMethods(string interfaceBody)
        {
            var methods = new List<TypeScriptMethodInfo>();

            // Match method declarations in interfaces
            var methodPattern = @"(\w+)(?:<([^>]+)>)?\s*\(([^)]*)\)\s*:\s*([^;\n]+)[;\n]";
            var matches = Regex.Matches(interfaceBody, methodPattern, RegexOptions.Multiline);

            foreach (Match match in matches)
            {
                var name = match.Groups[1].Value;
                var typeParams = match.Groups[2].Value;
                var parameters = match.Groups[3].Value;
                var returnType = match.Groups[4].Value.Trim();

                var method = new TypeScriptMethodInfo
                {
                    Name = name,
                    ReturnType = returnType,
                    IsPublic = true,
                    IsAsync = returnType.StartsWith("Promise<")
                };

                if (!string.IsNullOrEmpty(typeParams))
                {
                    method.TypeParameters = ParseTypeParameters(typeParams);
                }

                method.Parameters = ParseTypeScriptParameters(parameters);
                methods.Add(method);
            }

            return methods;
        }

        private List<TypeScriptParameterInfo> ParseTypeScriptParameters(string paramString)
        {
            var parameters = new List<TypeScriptParameterInfo>();

            if (string.IsNullOrWhiteSpace(paramString))
                return parameters;

            var paramParts = SplitParameters(paramString);
            foreach (var part in paramParts)
            {
                var trimmed = part.Trim();
                if (string.IsNullOrEmpty(trimmed))
                    continue;

                // Parse parameter: name?: Type = defaultValue
                var colonIndex = trimmed.IndexOf(':');
                if (colonIndex > 0)
                {
                    var nameAndOptional = trimmed.Substring(0, colonIndex).Trim();
                    var typeAndDefault = trimmed.Substring(colonIndex + 1).Trim();

                    var isOptional = nameAndOptional.EndsWith('?');
                    var name = isOptional ? nameAndOptional.TrimEnd('?') : nameAndOptional;

                    var equalsIndex = typeAndDefault.IndexOf('=');
                    var type = equalsIndex > 0 ? typeAndDefault.Substring(0, equalsIndex).Trim() : typeAndDefault;
                    var hasDefault = equalsIndex > 0;

                    parameters.Add(new TypeScriptParameterInfo
                    {
                        Name = name,
                        Type = type,
                        IsNullable = isOptional || type.Contains("null") || type.Contains("undefined"),
                        HasDefaultValue = hasDefault || isOptional
                    });
                }
            }

            return parameters;
        }

        private List<string> SplitParameters(string parameterString)
        {
            var parameters = new List<string>();
            var current = "";
            var parenDepth = 0;
            var angleDepth = 0;

            foreach (var ch in parameterString)
            {
                switch (ch)
                {
                    case '(':
                        parenDepth++;
                        break;
                    case ')':
                        parenDepth--;
                        break;
                    case '<':
                        angleDepth++;
                        break;
                    case '>':
                        angleDepth--;
                        break;
                    case ',' when parenDepth == 0 && angleDepth == 0:
                        parameters.Add(current);
                        current = "";
                        continue;
                }

                current += ch;
            }

            if (!string.IsNullOrWhiteSpace(current))
            {
                parameters.Add(current);
            }

            return parameters;
        }

        private string GetFullName(string className, string filePath)
        {
            var moduleName = GetModuleName(filePath);
            return string.IsNullOrEmpty(moduleName) ? className : $"{moduleName}.{className}";
        }

        private string GetModuleName(string filePath)
        {
            try
            {
                var fileName = Path.GetFileNameWithoutExtension(filePath);
                var directory = Path.GetDirectoryName(filePath);

                // Use directory structure as namespace
                var relativePath = directory?.Replace(Path.DirectorySeparatorChar, '.');
                return string.IsNullOrEmpty(relativePath) ? fileName : $"{relativePath}.{fileName}";
            }
            catch
            {
                return Path.GetFileNameWithoutExtension(filePath);
            }
        }

        private DiscoveryResult CheckDiscoveryMethods(TypeScriptClassInfo classInfo, GeneratorConfiguration config, string filePath)
        {
            // Attribute discovery (decorators)
            if (!string.IsNullOrWhiteSpace(config.TrackAttribute))
            {
                var hasAttribute = classInfo.Decorators.Any(attr =>
                    attr.Equals(config.TrackAttribute, StringComparison.OrdinalIgnoreCase));

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
                    if (regex.IsMatch(classInfo.Name) || regex.IsMatch(classInfo.FullName))
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
                    if (regex.IsMatch(classInfo.Module ?? ""))
                    {
                        return new DiscoveryResult(DiscoveryMethod.Namespace, namespacePattern.Trim());
                    }
                }
            }

            // File path discovery
            if (!string.IsNullOrWhiteSpace(config.TrackFilePattern))
            {
                var filePatterns = config.TrackFilePattern.Split('|', StringSplitOptions.RemoveEmptyEntries);
                foreach (var filePattern in filePatterns)
                {
                    var pattern = filePattern.Trim().Replace("**", ".*").Replace("*", "[^/]*");
                    var regex = new Regex(pattern, RegexOptions.IgnoreCase);
                    if (regex.IsMatch(filePath))
                    {
                        return new DiscoveryResult(DiscoveryMethod.FilePath, filePattern.Trim());
                    }
                }
            }

            return null;
        }

        private void TrackDiscoveryConflict(TypeScriptClassInfo classInfo, DiscoveryResult result, Dictionary<string, DiscoveryConflict> conflictTracker, string filePath)
        {
            var key = classInfo.FullName;
            if (!conflictTracker.ContainsKey(key))
            {
                conflictTracker[key] = new DiscoveryConflict
                {
                    ClassName = classInfo.Name,
                    FullName = classInfo.FullName
                };
            }

            conflictTracker[key].ConflictingSources.Add(new DiscoveryMethodSource
            {
                Method = result.Method,
                Source = result.Source,
                FilePath = filePath
            });
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

        private class TypeScriptClassInfo
        {
            public string Name { get; set; }
            public string FullName { get; set; }
            public string Module { get; set; }
            public string FilePath { get; set; }
            public bool IsAbstract { get; set; }
            public bool IsInterface { get; set; }
            public bool IsExported { get; set; }
            public bool IsDefault { get; set; }
            public string SuperClass { get; set; }
            public List<string> Interfaces { get; set; } = new List<string>();
            public List<string> TypeParameters { get; set; } = new List<string>();
            public List<string> Decorators { get; set; } = new List<string>();
            public List<TypeScriptPropertyInfo> Properties { get; set; } = new List<TypeScriptPropertyInfo>();
            public List<TypeScriptMethodInfo> Methods { get; set; } = new List<TypeScriptMethodInfo>();
        }

        private class TypeScriptPropertyInfo
        {
            public string Name { get; set; }
            public string Type { get; set; }
            public bool IsPublic { get; set; }
            public bool IsNullable { get; set; }
            public bool IsReadOnly { get; set; }
            public bool IsStatic { get; set; }
            public List<string> Decorators { get; set; } = new List<string>();
        }

        private class TypeScriptMethodInfo
        {
            public string Name { get; set; }
            public string ReturnType { get; set; }
            public bool IsPublic { get; set; }
            public bool IsStatic { get; set; }
            public bool IsAbstract { get; set; }
            public bool IsAsync { get; set; }
            public List<string> TypeParameters { get; set; } = new List<string>();
            public List<string> Decorators { get; set; } = new List<string>();
            public List<TypeScriptParameterInfo> Parameters { get; set; } = new List<TypeScriptParameterInfo>();
        }

        private class TypeScriptParameterInfo
        {
            public string Name { get; set; }
            public string Type { get; set; }
            public bool IsNullable { get; set; }
            public bool HasDefaultValue { get; set; }
        }
    }
}