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
    public class KotlinDiscoveryEngine : IClassDiscoveryEngine
    {
        private readonly ILogger _logger;

        public KotlinDiscoveryEngine(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<List<DiscoveredClass>> DiscoverClassesAsync(GeneratorConfiguration config)
        {
            var discoveredClasses = new List<DiscoveredClass>();
            var conflictTracker = new Dictionary<string, DiscoveryConflict>();

            try
            {
                _logger.LogInfo("Discovering Kotlin classes for analysis...");

                var sourcePaths = GetSourcePaths(config);
                if (sourcePaths.Count == 0)
                {
                    _logger.LogError("No Kotlin source paths found");
                    return discoveredClasses;
                }

                _logger.LogInfo($"Scanning {sourcePaths.Count} source paths for Kotlin files");

                foreach (var sourcePath in sourcePaths)
                {
                    if (!Directory.Exists(sourcePath))
                    {
                        _logger.LogWarning($"Source path does not exist: {sourcePath}");
                        continue;
                    }

                    var kotlinFiles = Directory.GetFiles(sourcePath, "*.kt", SearchOption.AllDirectories);
                    _logger.LogDebug($"Found {kotlinFiles.Length} Kotlin files in {sourcePath}");

                    foreach (var kotlinFile in kotlinFiles)
                    {
                        var discoveredClasses = await AnalyzeKotlinFileAsync(kotlinFile, config, conflictTracker);
                        discoveredClasses.AddRange(discoveredClasses);
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

                _logger.LogInfo($"Kotlin discovery completed. Found {discoveredClasses.Count} classes.");
                return discoveredClasses;
            }
            catch (Exception ex)
            {
                _logger.LogError("Kotlin class discovery failed", ex);
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

        private async Task<List<DiscoveredClass>> AnalyzeKotlinFileAsync(string filePath, GeneratorConfiguration config, Dictionary<string, DiscoveryConflict> conflictTracker)
        {
            var discoveredClasses = new List<DiscoveredClass>();

            try
            {
                var content = await File.ReadAllTextAsync(filePath);
                var classes = ParseKotlinClasses(content, filePath);

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
                        Namespace = classInfo.Package,
                        FilePath = filePath,
                        DiscoveryMethod = discoveryResult.Method,
                        DiscoverySource = discoveryResult.Source,
                        Attributes = classInfo.Annotations,
                        Properties = classInfo.Properties.Select(p => new DiscoveredProperty
                        {
                            Name = p.Name,
                            Type = p.Type,
                            IsPublic = p.IsPublic,
                            IsNullable = p.IsNullable,
                            IsReadOnly = p.IsReadOnly,
                            Attributes = p.Annotations
                        }).ToList(),
                        Methods = classInfo.Methods.Select(m => new DiscoveredMethod
                        {
                            Name = m.Name,
                            ReturnType = m.ReturnType,
                            IsPublic = m.IsPublic,
                            IsAsync = m.IsAsync,
                            Attributes = m.Annotations,
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
                    discoveredClass.Metadata["IsOpen"] = classInfo.IsOpen;
                    discoveredClass.Metadata["IsSealed"] = classInfo.IsSealed;
                    discoveredClass.Metadata["IsData"] = classInfo.IsData;
                    discoveredClass.Metadata["IsEnum"] = classInfo.IsEnum;
                    discoveredClass.Metadata["IsObject"] = classInfo.IsObject;
                    discoveredClass.Metadata["SuperClass"] = classInfo.SuperClass;
                    discoveredClass.Metadata["Interfaces"] = classInfo.Interfaces;

                    discoveredClasses.Add(discoveredClass);
                }

                return discoveredClasses;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to analyze Kotlin file {filePath}: {ex.Message}");
                return new List<DiscoveredClass>();
            }
        }

        private List<KotlinClassInfo> ParseKotlinClasses(string content, string filePath)
        {
            var classes = new List<KotlinClassInfo>();

            try
            {
                // Extract package
                var packageMatch = Regex.Match(content, @"package\s+([\w\.]+)");
                var packageName = packageMatch.Success ? packageMatch.Groups[1].Value : "";

                // Find all class declarations (class, interface, object, enum class, data class, sealed class)
                var classPattern = @"(?:@\w+(?:\([^)]*\))?\s*)*(?:(public|private|protected|internal)\s+)?(?:(abstract|open|sealed|data|enum|inner|inline)\s+)?(?:(class|interface|object))\s+(\w+)(?:\s*<[^>]*>)?(?:\s*\(\s*[^)]*\s*\))?(?:\s*:\s*([\w\s,<>]+?))?\s*\{";
                var matches = Regex.Matches(content, classPattern, RegexOptions.Multiline);

                foreach (Match match in matches)
                {
                    var classInfo = new KotlinClassInfo
                    {
                        Name = match.Groups[4].Value,
                        Package = packageName,
                        FilePath = filePath,
                        IsPublic = string.IsNullOrEmpty(match.Groups[1].Value) || match.Groups[1].Value == "public",
                        IsAbstract = match.Groups[2].Value == "abstract",
                        IsOpen = match.Groups[2].Value == "open",
                        IsSealed = match.Groups[2].Value == "sealed",
                        IsData = match.Groups[2].Value == "data",
                        IsEnum = match.Groups[2].Value == "enum",
                        IsInterface = match.Groups[3].Value == "interface",
                        IsObject = match.Groups[3].Value == "object"
                    };

                    classInfo.FullName = string.IsNullOrEmpty(packageName) ? classInfo.Name : $"{packageName}.{classInfo.Name}";

                    // Parse inheritance
                    if (match.Groups[5].Success)
                    {
                        var inheritance = match.Groups[5].Value.Trim();
                        var parts = inheritance.Split(',').Select(p => p.Trim()).ToList();

                        // First part is usually the superclass (unless it's an interface)
                        if (parts.Count > 0 && !classInfo.IsInterface)
                        {
                            var firstPart = parts[0];
                            if (firstPart.Contains('(') || char.IsUpper(firstPart[0]))
                            {
                                classInfo.SuperClass = firstPart.Split('(')[0].Trim();
                                classInfo.Interfaces = parts.Skip(1).ToList();
                            }
                            else
                            {
                                classInfo.Interfaces = parts;
                            }
                        }
                        else
                        {
                            classInfo.Interfaces = parts;
                        }
                    }

                    // Extract annotations
                    var classStart = match.Index;
                    var contentBeforeClass = content.Substring(Math.Max(0, classStart - 500), Math.Min(500, classStart));
                    classInfo.Annotations = ExtractAnnotations(contentBeforeClass);

                    // Extract class body
                    var classBody = ExtractClassBody(content, match.Index + match.Length);

                    // Parse properties and methods
                    classInfo.Properties = ExtractProperties(classBody);
                    classInfo.Methods = ExtractMethods(classBody);

                    classes.Add(classInfo);
                }

                return classes;
            }
            catch (Exception ex)
            {
                _logger.LogDebug($"Failed to parse Kotlin classes in {filePath}: {ex.Message}");
                return new List<KotlinClassInfo>();
            }
        }

        private List<string> ExtractAnnotations(string content)
        {
            var annotations = new List<string>();
            var annotationPattern = @"@(\w+)(?:\([^)]*\))?";
            var matches = Regex.Matches(content, annotationPattern);

            foreach (Match match in matches)
            {
                annotations.Add(match.Groups[1].Value);
            }

            return annotations;
        }

        private string ExtractClassBody(string content, int startIndex)
        {
            var braceCount = 0;
            var inString = false;
            var inTripleQuote = false;
            var escaped = false;

            for (int i = startIndex; i < content.Length; i++)
            {
                var ch = content[i];

                if (escaped)
                {
                    escaped = false;
                    continue;
                }

                if (ch == '\\')
                {
                    escaped = true;
                    continue;
                }

                // Handle triple quotes
                if (i + 2 < content.Length && content.Substring(i, 3) == "\"\"\"")
                {
                    inTripleQuote = !inTripleQuote;
                    i += 2;
                    continue;
                }

                if (!inTripleQuote && ch == '"')
                {
                    inString = !inString;
                    continue;
                }

                if (!inString && !inTripleQuote)
                {
                    if (ch == '{')
                        braceCount++;
                    else if (ch == '}')
                    {
                        braceCount--;
                        if (braceCount == 0)
                        {
                            return content.Substring(startIndex, i - startIndex);
                        }
                    }
                }
            }

            return content.Substring(startIndex);
        }

        private List<KotlinPropertyInfo> ExtractProperties(string classBody)
        {
            var properties = new List<KotlinPropertyInfo>();

            // Match val/var declarations
            var propertyPattern = @"(?:@\w+(?:\([^)]*\))?\s*)*(?:(public|private|protected|internal)\s+)?(?:(override)\s+)?(val|var)\s+(\w+)\s*:\s*([^=\n{]+?)(?:\s*=\s*[^=\n]*)?(?=\s*[\n}]|\s*get\s*\(|\s*set\s*\()";
            var matches = Regex.Matches(classBody, propertyPattern, RegexOptions.Multiline);

            foreach (Match match in matches)
            {
                var property = new KotlinPropertyInfo
                {
                    Name = match.Groups[4].Value,
                    Type = match.Groups[5].Value.Trim(),
                    IsPublic = string.IsNullOrEmpty(match.Groups[1].Value) || match.Groups[1].Value == "public",
                    IsReadOnly = match.Groups[3].Value == "val",
                    IsOverride = !string.IsNullOrEmpty(match.Groups[2].Value)
                };

                // Check if type is nullable
                property.IsNullable = property.Type.EndsWith("?");
                if (property.IsNullable)
                {
                    property.Type = property.Type.TrimEnd('?');
                }

                // Extract annotations for this property
                var propertyStart = match.Index;
                var contentBeforeProperty = classBody.Substring(Math.Max(0, propertyStart - 200), Math.Min(200, propertyStart));
                property.Annotations = ExtractAnnotations(contentBeforeProperty);

                properties.Add(property);
            }

            return properties;
        }

        private List<KotlinMethodInfo> ExtractMethods(string classBody)
        {
            var methods = new List<KotlinMethodInfo>();

            // Match function declarations
            var methodPattern = @"(?:@\w+(?:\([^)]*\))?\s*)*(?:(public|private|protected|internal)\s+)?(?:(override|abstract|open|final|suspend)\s+)?fun\s+(?:<[^>]*>\s+)?(\w+)\s*\(([^)]*)\)\s*(?::\s*([^=\n{]+?))?(?:\s*=|\s*\{)";
            var matches = Regex.Matches(classBody, methodPattern, RegexOptions.Multiline);

            foreach (Match match in matches)
            {
                var method = new KotlinMethodInfo
                {
                    Name = match.Groups[3].Value,
                    ReturnType = match.Groups[5].Success ? match.Groups[5].Value.Trim() : "Unit",
                    IsPublic = string.IsNullOrEmpty(match.Groups[1].Value) || match.Groups[1].Value == "public",
                    IsAbstract = match.Groups[2].Value.Contains("abstract"),
                    IsOverride = match.Groups[2].Value.Contains("override"),
                    IsAsync = match.Groups[2].Value.Contains("suspend")
                };

                // Parse parameters
                var parameterString = match.Groups[4].Value;
                if (!string.IsNullOrWhiteSpace(parameterString))
                {
                    method.Parameters = ParseKotlinParameters(parameterString);
                }

                // Extract annotations for this method
                var methodStart = match.Index;
                var contentBeforeMethod = classBody.Substring(Math.Max(0, methodStart - 200), Math.Min(200, methodStart));
                method.Annotations = ExtractAnnotations(contentBeforeMethod);

                methods.Add(method);
            }

            return methods;
        }

        private List<KotlinParameterInfo> ParseKotlinParameters(string parameterString)
        {
            var parameters = new List<KotlinParameterInfo>();
            var paramParts = SplitParameters(parameterString);

            foreach (var part in paramParts)
            {
                var trimmed = part.Trim();
                if (string.IsNullOrEmpty(trimmed))
                    continue;

                // Parse parameter: name: Type = defaultValue
                var colonIndex = trimmed.IndexOf(':');
                if (colonIndex > 0)
                {
                    var name = trimmed.Substring(0, colonIndex).Trim();
                    var typeAndDefault = trimmed.Substring(colonIndex + 1).Trim();

                    var equalsIndex = typeAndDefault.IndexOf('=');
                    var type = equalsIndex > 0 ? typeAndDefault.Substring(0, equalsIndex).Trim() : typeAndDefault;
                    var hasDefault = equalsIndex > 0;

                    var isNullable = type.EndsWith("?");
                    if (isNullable)
                    {
                        type = type.TrimEnd('?');
                    }

                    parameters.Add(new KotlinParameterInfo
                    {
                        Name = name,
                        Type = type,
                        IsNullable = isNullable,
                        HasDefaultValue = hasDefault
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

        private DiscoveryResult CheckDiscoveryMethods(KotlinClassInfo classInfo, GeneratorConfiguration config, string filePath)
        {
            // Attribute discovery
            if (!string.IsNullOrWhiteSpace(config.TrackAttribute))
            {
                var hasAttribute = classInfo.Annotations.Any(attr =>
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
                    if (regex.IsMatch(classInfo.Package ?? ""))
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

        private void TrackDiscoveryConflict(KotlinClassInfo classInfo, DiscoveryResult result, Dictionary<string, DiscoveryConflict> conflictTracker, string filePath)
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

        private class KotlinClassInfo
        {
            public string Name { get; set; }
            public string FullName { get; set; }
            public string Package { get; set; }
            public string FilePath { get; set; }
            public bool IsPublic { get; set; }
            public bool IsAbstract { get; set; }
            public bool IsOpen { get; set; }
            public bool IsSealed { get; set; }
            public bool IsData { get; set; }
            public bool IsEnum { get; set; }
            public bool IsInterface { get; set; }
            public bool IsObject { get; set; }
            public string SuperClass { get; set; }
            public List<string> Interfaces { get; set; } = new List<string>();
            public List<string> Annotations { get; set; } = new List<string>();
            public List<KotlinPropertyInfo> Properties { get; set; } = new List<KotlinPropertyInfo>();
            public List<KotlinMethodInfo> Methods { get; set; } = new List<KotlinMethodInfo>();
        }

        private class KotlinPropertyInfo
        {
            public string Name { get; set; }
            public string Type { get; set; }
            public bool IsPublic { get; set; }
            public bool IsNullable { get; set; }
            public bool IsReadOnly { get; set; }
            public bool IsOverride { get; set; }
            public List<string> Annotations { get; set; } = new List<string>();
        }

        private class KotlinMethodInfo
        {
            public string Name { get; set; }
            public string ReturnType { get; set; }
            public bool IsPublic { get; set; }
            public bool IsAbstract { get; set; }
            public bool IsOverride { get; set; }
            public bool IsAsync { get; set; }
            public List<string> Annotations { get; set; } = new List<string>();
            public List<KotlinParameterInfo> Parameters { get; set; } = new List<KotlinParameterInfo>();
        }

        private class KotlinParameterInfo
        {
            public string Name { get; set; }
            public string Type { get; set; }
            public bool IsNullable { get; set; }
            public bool HasDefaultValue { get; set; }
        }
    }
}