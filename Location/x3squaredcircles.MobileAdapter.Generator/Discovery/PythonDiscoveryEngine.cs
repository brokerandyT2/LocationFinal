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
    public class PythonDiscoveryEngine : IClassDiscoveryEngine
    {
        private readonly ILogger _logger;

        public PythonDiscoveryEngine(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<List<DiscoveredClass>> DiscoverClassesAsync(GeneratorConfiguration config)
        {
            var discoveredClasses = new List<DiscoveredClass>();
            var conflictTracker = new Dictionary<string, DiscoveryConflict>();

            try
            {
                _logger.LogInfo("Discovering Python classes for analysis...");

                var sourcePaths = GetSourcePaths(config);
                if (sourcePaths.Count == 0)
                {
                    _logger.LogError("No Python source paths found");
                    return discoveredClasses;
                }

                _logger.LogInfo($"Scanning {sourcePaths.Count} source paths for Python files");

                foreach (var sourcePath in sourcePaths)
                {
                    if (!Directory.Exists(sourcePath))
                    {
                        _logger.LogWarning($"Source path does not exist: {sourcePath}");
                        continue;
                    }

                    var pyFiles = Directory.GetFiles(sourcePath, "*.py", SearchOption.AllDirectories)
                        .Where(f => !Path.GetFileName(f).StartsWith("__") || Path.GetFileName(f) == "__init__.py")
                        .ToArray();

                    _logger.LogDebug($"Found {pyFiles.Length} Python files in {sourcePath}");

                    foreach (var pyFile in pyFiles)
                    {
                        var fileClasses = await AnalyzePythonFileAsync(pyFile, config, conflictTracker);
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

                _logger.LogInfo($"Python discovery completed. Found {discoveredClasses.Count} classes.");
                return discoveredClasses;
            }
            catch (Exception ex)
            {
                _logger.LogError("Python class discovery failed", ex);
                throw;
            }
        }

        private List<string> GetSourcePaths(GeneratorConfiguration config)
        {
            var paths = new List<string>();

            if (!string.IsNullOrWhiteSpace(config.Source.PythonPaths))
            {
                paths.AddRange(config.Source.PythonPaths.Split(':', StringSplitOptions.RemoveEmptyEntries));
            }

            return paths.Select(p => p.Trim()).Where(p => !string.IsNullOrEmpty(p)).ToList();
        }

        private async Task<List<DiscoveredClass>> AnalyzePythonFileAsync(string filePath, GeneratorConfiguration config, Dictionary<string, DiscoveryConflict> conflictTracker)
        {
            var discoveredClasses = new List<DiscoveredClass>();

            try
            {
                var content = await File.ReadAllTextAsync(filePath);
                var classes = ParsePythonClasses(content, filePath);

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
                            Type = p.Type ?? "Any",
                            IsPublic = p.IsPublic,
                            IsNullable = true, // Python is dynamically typed
                            Attributes = p.Decorators
                        }).ToList(),
                        Methods = classInfo.Methods.Select(m => new DiscoveredMethod
                        {
                            Name = m.Name,
                            ReturnType = m.ReturnType ?? "Any",
                            IsPublic = m.IsPublic,
                            IsAsync = m.IsAsync,
                            Attributes = m.Decorators,
                            Parameters = m.Parameters.Select(p => new DiscoveredParameter
                            {
                                Name = p.Name,
                                Type = p.Type ?? "Any",
                                IsNullable = true,
                                HasDefaultValue = p.HasDefaultValue
                            }).ToList()
                        }).ToList()
                    };

                    discoveredClass.Metadata["IsAbstract"] = classInfo.IsAbstract;
                    discoveredClass.Metadata["BaseClasses"] = classInfo.BaseClasses;
                    discoveredClass.Metadata["Metaclass"] = classInfo.Metaclass;
                    discoveredClass.Metadata["HasSlots"] = classInfo.HasSlots;

                    discoveredClasses.Add(discoveredClass);
                }

                return discoveredClasses;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to analyze Python file {filePath}: {ex.Message}");
                return new List<DiscoveredClass>();
            }
        }

        private List<PythonClassInfo> ParsePythonClasses(string content, string filePath)
        {
            var classes = new List<PythonClassInfo>();

            try
            {
                // Remove string literals and comments to avoid false matches
                var cleanedContent = RemoveStringsAndComments(content);

                // Find class declarations
                var classPattern = @"^(\s*)(?:@\w+(?:\([^)]*\))?\s*)*class\s+(\w+)(?:\s*\(([^)]*)\))?\s*:";
                var matches = Regex.Matches(cleanedContent, classPattern, RegexOptions.Multiline);

                foreach (Match match in matches)
                {
                    var indentation = match.Groups[1].Value;
                    var className = match.Groups[2].Value;
                    var inheritance = match.Groups[3].Success ? match.Groups[3].Value : "";

                    var classInfo = new PythonClassInfo
                    {
                        Name = className,
                        FilePath = filePath,
                        Indentation = indentation.Length
                    };

                    classInfo.FullName = GetFullName(classInfo.Name, filePath);
                    classInfo.Module = GetModuleName(filePath);

                    // Parse inheritance
                    if (!string.IsNullOrWhiteSpace(inheritance))
                    {
                        classInfo.BaseClasses = inheritance.Split(',')
                            .Select(b => b.Trim())
                            .Where(b => !string.IsNullOrEmpty(b))
                            .ToList();

                        // Check for ABC (Abstract Base Class)
                        classInfo.IsAbstract = classInfo.BaseClasses.Any(b =>
                            b.Contains("ABC") || b.Contains("Abstract"));

                        // Check for metaclass
                        var metaclassMatch = Regex.Match(inheritance, @"metaclass\s*=\s*(\w+)");
                        if (metaclassMatch.Success)
                        {
                            classInfo.Metaclass = metaclassMatch.Groups[1].Value;
                        }
                    }

                    // Extract decorators
                    var classStart = match.Index;
                    var contentBeforeClass = content.Substring(Math.Max(0, classStart - 500), Math.Min(500, classStart));
                    classInfo.Decorators = ExtractDecorators(contentBeforeClass);

                    // Extract class body
                    var classBody = ExtractClassBody(content, match.Index + match.Length, indentation.Length);

                    // Check for __slots__
                    classInfo.HasSlots = classBody.Contains("__slots__");

                    // Parse class members
                    classInfo.Properties = ExtractPythonProperties(classBody, indentation.Length + 4);
                    classInfo.Methods = ExtractPythonMethods(classBody, indentation.Length + 4);

                    classes.Add(classInfo);
                }

                return classes;
            }
            catch (Exception ex)
            {
                _logger.LogDebug($"Failed to parse Python classes in {filePath}: {ex.Message}");
                return new List<PythonClassInfo>();
            }
        }

        private string RemoveStringsAndComments(string content)
        {
            var result = content;

            // Remove triple-quoted strings
            result = Regex.Replace(result, @"\"\"\".*?\"\"\"|'''.*?'''", "", RegexOptions.Singleline);

            // Remove single and double quoted strings
            result = Regex.Replace(result, @"""[^""]*""|'[^']*'", "\"\"", RegexOptions.Multiline);

            // Remove comments
            result = Regex.Replace(result, @"#.*$", "", RegexOptions.Multiline);

            return result;
        }

        private string ExtractClassBody(string content, int startIndex, int classIndentation)
        {
            var lines = content.Substring(startIndex).Split('\n');
            var classBody = "";
            var inClassBody = false;

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    if (inClassBody) classBody += line + "\n";
                    continue;
                }

                var lineIndentation = line.Length - line.TrimStart().Length;

                if (!inClassBody)
                {
                    if (lineIndentation > classIndentation)
                    {
                        inClassBody = true;
                        classBody += line + "\n";
                    }
                }
                else
                {
                    if (lineIndentation <= classIndentation && !string.IsNullOrWhiteSpace(line.Trim()))
                    {
                        break; // End of class body
                    }
                    classBody += line + "\n";
                }
            }

            return classBody;
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

        private List<PythonPropertyInfo> ExtractPythonProperties(string classBody, int expectedIndentation)
        {
            var properties = new List<PythonPropertyInfo>();

            // Look for property declarations and @property decorators
            var lines = classBody.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var trimmedLine = line.Trim();

                if (string.IsNullOrWhiteSpace(trimmedLine))
                    continue;

                var lineIndentation = line.Length - line.TrimStart().Length;

                // Skip if not at expected class member level
                if (lineIndentation != expectedIndentation)
                    continue;

                // Check for @property decorator
                if (trimmedLine.StartsWith("@property"))
                {
                    // Look for the method definition on next non-empty line
                    for (int j = i + 1; j < lines.Length; j++)
                    {
                        var nextLine = lines[j].Trim();
                        if (string.IsNullOrWhiteSpace(nextLine))
                            continue;

                        var methodMatch = Regex.Match(nextLine, @"def\s+(\w+)\s*\(");
                        if (methodMatch.Success)
                        {
                            properties.Add(new PythonPropertyInfo
                            {
                                Name = methodMatch.Groups[1].Value,
                                Type = "Any",
                                IsPublic = !methodMatch.Groups[1].Value.StartsWith("_"),
                                IsProperty = true,
                                Decorators = new List<string> { "property" }
                            });
                            break;
                        }
                    }
                }
                // Check for type annotations
                else if (Regex.IsMatch(trimmedLine, @"^\w+\s*:\s*\w+"))
                {
                    var annotationMatch = Regex.Match(trimmedLine, @"^(\w+)\s*:\s*([^=\n]+)(?:\s*=.*)?");
                    if (annotationMatch.Success)
                    {
                        var name = annotationMatch.Groups[1].Value;
                        var type = annotationMatch.Groups[2].Value.Trim();

                        properties.Add(new PythonPropertyInfo
                        {
                            Name = name,
                            Type = type,
                            IsPublic = !name.StartsWith("_"),
                            IsProperty = false
                        });
                    }
                }
                // Check for simple assignments that might be attributes
                else if (Regex.IsMatch(trimmedLine, @"^self\.\w+\s*=") && trimmedLine.Contains("self."))
                {
                    var assignmentMatch = Regex.Match(trimmedLine, @"self\.(\w+)\s*=");
                    if (assignmentMatch.Success)
                    {
                        var name = assignmentMatch.Groups[1].Value;
                        properties.Add(new PythonPropertyInfo
                        {
                            Name = name,
                            Type = "Any",
                            IsPublic = !name.StartsWith("_"),
                            IsProperty = false
                        });
                    }
                }
            }

            return properties.Distinct(new PythonPropertyInfoComparer()).ToList();
        }

        private List<PythonMethodInfo> ExtractPythonMethods(string classBody, int expectedIndentation)
        {
            var methods = new List<PythonMethodInfo>();

            // Match method definitions
            var methodPattern = @"^(\s*)(?:@\w+(?:\([^)]*\))?\s*)*(?:(async)\s+)?def\s+(\w+)\s*\(([^)]*)\)(?:\s*->\s*([^:]+))?\s*:";
            var matches = Regex.Matches(classBody, methodPattern, RegexOptions.Multiline);

            foreach (Match match in matches)
            {
                var methodIndentation = match.Groups[1].Value.Length;

                // Only include methods at the expected class member level
                if (methodIndentation != expectedIndentation)
                    continue;

                var isAsync = !string.IsNullOrEmpty(match.Groups[2].Value);
                var methodName = match.Groups[3].Value;
                var parameters = match.Groups[4].Value;
                var returnType = match.Groups[5].Success ? match.Groups[5].Value.Trim() : "Any";

                // Skip special methods we don't want to adapt
                if (methodName.StartsWith("__") && methodName.EndsWith("__") &&
                    methodName != "__init__" && methodName != "__call__")
                    continue;

                var method = new PythonMethodInfo
                {
                    Name = methodName,
                    ReturnType = returnType,
                    IsPublic = !methodName.StartsWith("_"),
                    IsAsync = isAsync,
                    IsStatic = false, // Will be determined by decorators
                    IsClassMethod = false
                };

                // Parse parameters
                method.Parameters = ParsePythonParameters(parameters);

                // Extract decorators for this method
                var methodStart = match.Index;
                var contentBeforeMethod = classBody.Substring(Math.Max(0, methodStart - 200), Math.Min(200, methodStart));
                method.Decorators = ExtractDecorators(contentBeforeMethod);

                // Check for static/class method decorators
                method.IsStatic = method.Decorators.Contains("staticmethod");
                method.IsClassMethod = method.Decorators.Contains("classmethod");

                methods.Add(method);
            }

            return methods;
        }

        private List<PythonParameterInfo> ParsePythonParameters(string paramString)
        {
            var parameters = new List<PythonParameterInfo>();

            if (string.IsNullOrWhiteSpace(paramString))
                return parameters;

            var paramParts = SplitParameters(paramString);
            foreach (var part in paramParts)
            {
                var trimmed = part.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed == "self" || trimmed == "cls")
                    continue;

                var hasDefault = trimmed.Contains('=');
                var hasTypeAnnotation = trimmed.Contains(':') && !trimmed.StartsWith("*");

                string name, type = "Any";

                if (hasTypeAnnotation)
                {
                    var colonIndex = trimmed.IndexOf(':');
                    name = trimmed.Substring(0, colonIndex).Trim();
                    var typeAndDefault = trimmed.Substring(colonIndex + 1).Trim();

                    if (hasDefault)
                    {
                        var equalsIndex = typeAndDefault.IndexOf('=');
                        type = typeAndDefault.Substring(0, equalsIndex).Trim();
                    }
                    else
                    {
                        type = typeAndDefault;
                    }
                }
                else
                {
                    name = hasDefault ? trimmed.Split('=')[0].Trim() : trimmed;
                }

                // Handle *args and **kwargs
                if (name.StartsWith("**"))
                {
                    name = name.Substring(2);
                    type = "Dict[str, Any]";
                }
                else if (name.StartsWith("*"))
                {
                    name = name.Substring(1);
                    type = "Tuple[Any, ...]";
                }

                parameters.Add(new PythonParameterInfo
                {
                    Name = name,
                    Type = type,
                    HasDefaultValue = hasDefault
                });
            }

            return parameters;
        }

        private List<string> SplitParameters(string parameterString)
        {
            var parameters = new List<string>();
            var current = "";
            var parenDepth = 0;
            var bracketDepth = 0;

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
                    case '[':
                        bracketDepth++;
                        break;
                    case ']':
                        bracketDepth--;
                        break;
                    case ',' when parenDepth == 0 && bracketDepth == 0:
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
                if (fileName == "__init__")
                {
                    fileName = Path.GetFileName(Path.GetDirectoryName(filePath));
                }

                var directory = Path.GetDirectoryName(filePath);

                // Use directory structure as namespace, replacing path separators with dots
                var relativePath = directory?.Replace(Path.DirectorySeparatorChar, '.');
                return string.IsNullOrEmpty(relativePath) ? fileName : $"{relativePath}.{fileName}";
            }
            catch
            {
                return Path.GetFileNameWithoutExtension(filePath);
            }
        }

        private DiscoveryResult CheckDiscoveryMethods(PythonClassInfo classInfo, GeneratorConfiguration config, string filePath)
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

        private void TrackDiscoveryConflict(PythonClassInfo classInfo, DiscoveryResult result, Dictionary<string, DiscoveryConflict> conflictTracker, string filePath)
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

        private class PythonClassInfo
        {
            public string Name { get; set; }
            public string FullName { get; set; }
            public string Module { get; set; }
            public string FilePath { get; set; }
            public int Indentation { get; set; }
            public bool IsAbstract { get; set; }
            public List<string> BaseClasses { get; set; } = new List<string>();
            public string Metaclass { get; set; }
            public bool HasSlots { get; set; }
            public List<string> Decorators { get; set; } = new List<string>();
            public List<PythonPropertyInfo> Properties { get; set; } = new List<PythonPropertyInfo>();
            public List<PythonMethodInfo> Methods { get; set; } = new List<PythonMethodInfo>();
        }

        private class PythonPropertyInfo
        {
            public string Name { get; set; }
            public string Type { get; set; }
            public bool IsPublic { get; set; }
            public bool IsProperty { get; set; }
            public List<string> Decorators { get; set; } = new List<string>();
        }

        private class PythonMethodInfo
        {
            public string Name { get; set; }
            public string ReturnType { get; set; }
            public bool IsPublic { get; set; }
            public bool IsAsync { get; set; }
            public bool IsStatic { get; set; }
            public bool IsClassMethod { get; set; }
            public List<string> Decorators { get; set; } = new List<string>();
            public List<PythonParameterInfo> Parameters { get; set; } = new List<PythonParameterInfo>();
        }

        private class PythonParameterInfo
        {
            public string Name { get; set; }
            public string Type { get; set; }
            public bool HasDefaultValue { get; set; }
        }

        private class PythonPropertyInfoComparer : IEqualityComparer<PythonPropertyInfo>
        {
            public bool Equals(PythonPropertyInfo x, PythonPropertyInfo y)
            {
                return x?.Name == y?.Name;
            }

            public int GetHashCode(PythonPropertyInfo obj)
            {
                return obj?.Name?.GetHashCode() ?? 0;
            }
        }
    }
}