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
    public class JavaDiscoveryEngine : IClassDiscoveryEngine
    {
        private readonly ILogger _logger;

        public JavaDiscoveryEngine(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<List<DiscoveredClass>> DiscoverClassesAsync(GeneratorConfiguration config)
        {
            var discoveredClasses = new List<DiscoveredClass>();
            var conflictTracker = new Dictionary<string, DiscoveryConflict>();

            try
            {
                _logger.LogInfo("Discovering Java classes for analysis...");

                var sourcePaths = GetSourcePaths(config);
                if (sourcePaths.Count == 0)
                {
                    _logger.LogError("No Java source paths found");
                    return discoveredClasses;
                }

                _logger.LogInfo($"Scanning {sourcePaths.Count} source paths for Java files");

                foreach (var sourcePath in sourcePaths)
                {
                    if (!Directory.Exists(sourcePath))
                    {
                        _logger.LogWarning($"Source path does not exist: {sourcePath}");
                        continue;
                    }

                    var javaFiles = Directory.GetFiles(sourcePath, "*.java", SearchOption.AllDirectories);
                    _logger.LogDebug($"Found {javaFiles.Length} Java files in {sourcePath}");

                    foreach (var javaFile in javaFiles)
                    {
                        var discoveredClass = await AnalyzeJavaFileAsync(javaFile, config, conflictTracker);
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

                _logger.LogInfo($"Java discovery completed. Found {discoveredClasses.Count} classes.");
                return discoveredClasses;
            }
            catch (Exception ex)
            {
                _logger.LogError("Java class discovery failed", ex);
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

        private async Task<DiscoveredClass> AnalyzeJavaFileAsync(string filePath, GeneratorConfiguration config, Dictionary<string, DiscoveryConflict> conflictTracker)
        {
            try
            {
                var content = await File.ReadAllTextAsync(filePath);
                var classInfo = ParseJavaClass(content, filePath);

                if (classInfo == null)
                    return null;

                var discoveryResult = CheckDiscoveryMethods(classInfo, config);
                if (discoveryResult == null)
                    return null;

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
                    Properties = classInfo.Fields.Select(f => new DiscoveredProperty
                    {
                        Name = f.Name,
                        Type = f.Type,
                        IsPublic = f.IsPublic,
                        IsNullable = !f.Type.EndsWith("[]") && !IsPrimitiveType(f.Type),
                        Attributes = f.Annotations
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
                            IsNullable = !IsPrimitiveType(p.Type)
                        }).ToList()
                    }).ToList()
                };

                discoveredClass.Metadata["IsAbstract"] = classInfo.IsAbstract;
                discoveredClass.Metadata["IsFinal"] = classInfo.IsFinal;
                discoveredClass.Metadata["IsPublic"] = classInfo.IsPublic;
                discoveredClass.Metadata["SuperClass"] = classInfo.SuperClass;
                discoveredClass.Metadata["Interfaces"] = classInfo.Interfaces;

                return discoveredClass;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to analyze Java file {filePath}: {ex.Message}");
                return null;
            }
        }

        private JavaClassInfo ParseJavaClass(string content, string filePath)
        {
            try
            {
                var classInfo = new JavaClassInfo();

                // Extract package
                var packageMatch = Regex.Match(content, @"package\s+([\w\.]+)\s*;");
                if (packageMatch.Success)
                {
                    classInfo.Package = packageMatch.Groups[1].Value;
                }

                // Extract class declaration
                var classPattern = @"(?:@\w+(?:\([^)]*\))?\s*)*(?:(public|private|protected)\s+)?(?:(abstract|final)\s+)?class\s+(\w+)(?:\s+extends\s+(\w+))?(?:\s+implements\s+([\w\s,]+))?\s*\{";
                var classMatch = Regex.Match(content, classPattern, RegexOptions.Multiline);

                if (!classMatch.Success)
                    return null;

                classInfo.Name = classMatch.Groups[3].Value;
                classInfo.FullName = string.IsNullOrEmpty(classInfo.Package) ? classInfo.Name : $"{classInfo.Package}.{classInfo.Name}";
                classInfo.IsPublic = classMatch.Groups[1].Value == "public";
                classInfo.IsAbstract = classMatch.Groups[2].Value == "abstract";
                classInfo.IsFinal = classMatch.Groups[2].Value == "final";
                classInfo.SuperClass = classMatch.Groups[4].Value;

                if (!string.IsNullOrEmpty(classMatch.Groups[5].Value))
                {
                    classInfo.Interfaces = classMatch.Groups[5].Value.Split(',').Select(i => i.Trim()).ToList();
                }

                // Extract annotations on class
                var classStart = classMatch.Index;
                var contentBeforeClass = content.Substring(0, classStart);
                classInfo.Annotations = ExtractAnnotations(contentBeforeClass);

                // Extract fields and methods
                var classBody = ExtractClassBody(content, classMatch.Index + classMatch.Length);
                classInfo.Fields = ExtractFields(classBody);
                classInfo.Methods = ExtractMethods(classBody);

                return classInfo;
            }
            catch (Exception ex)
            {
                _logger.LogDebug($"Failed to parse Java class in {filePath}: {ex.Message}");
                return null;
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
            var inChar = false;
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

                if (!inChar && ch == '"')
                {
                    inString = !inString;
                    continue;
                }

                if (!inString && ch == '\'')
                {
                    inChar = !inChar;
                    continue;
                }

                if (!inString && !inChar)
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

        private List<JavaFieldInfo> ExtractFields(string classBody)
        {
            var fields = new List<JavaFieldInfo>();
            var fieldPattern = @"(?:@\w+(?:\([^)]*\))?\s*)*(?:(public|private|protected)\s+)?(?:(static|final)\s+)?(\w+(?:<[\w\s,<>]+>)?(?:\[\])?)\s+(\w+)(?:\s*=\s*[^;]+)?\s*;";
            var matches = Regex.Matches(classBody, fieldPattern, RegexOptions.Multiline);

            foreach (Match match in matches)
            {
                var field = new JavaFieldInfo
                {
                    Name = match.Groups[4].Value,
                    Type = match.Groups[3].Value,
                    IsPublic = match.Groups[1].Value == "public",
                    IsStatic = match.Groups[2].Value == "static",
                    IsFinal = match.Groups[2].Value == "final"
                };

                // Extract annotations for this field
                var fieldStart = match.Index;
                var contentBeforeField = classBody.Substring(Math.Max(0, fieldStart - 200), Math.Min(200, fieldStart));
                field.Annotations = ExtractAnnotations(contentBeforeField);

                fields.Add(field);
            }

            return fields;
        }

        private List<JavaMethodInfo> ExtractMethods(string classBody)
        {
            var methods = new List<JavaMethodInfo>();
            var methodPattern = @"(?:@\w+(?:\([^)]*\))?\s*)*(?:(public|private|protected)\s+)?(?:(static|final|abstract)\s+)?(\w+(?:<[\w\s,<>]+>)?(?:\[\])?)\s+(\w+)\s*\(([^)]*)\)\s*(?:throws\s+[\w\s,]+)?\s*(?:\{|;)";
            var matches = Regex.Matches(classBody, methodPattern, RegexOptions.Multiline);

            foreach (Match match in matches)
            {
                var method = new JavaMethodInfo
                {
                    Name = match.Groups[4].Value,
                    ReturnType = match.Groups[3].Value,
                    IsPublic = match.Groups[1].Value == "public",
                    IsStatic = match.Groups[2].Value == "static",
                    IsAbstract = match.Groups[2].Value == "abstract"
                };

                // Parse parameters
                var parameterString = match.Groups[5].Value;
                if (!string.IsNullOrWhiteSpace(parameterString))
                {
                    method.Parameters = ParseParameters(parameterString);
                }

                // Extract annotations for this method
                var methodStart = match.Index;
                var contentBeforeMethod = classBody.Substring(Math.Max(0, methodStart - 200), Math.Min(200, methodStart));
                method.Annotations = ExtractAnnotations(contentBeforeMethod);

                methods.Add(method);
            }

            return methods;
        }

        private List<JavaParameterInfo> ParseParameters(string parameterString)
        {
            var parameters = new List<JavaParameterInfo>();
            var paramParts = parameterString.Split(',');

            foreach (var part in paramParts)
            {
                var trimmed = part.Trim();
                if (string.IsNullOrEmpty(trimmed))
                    continue;

                var lastSpace = trimmed.LastIndexOf(' ');
                if (lastSpace > 0)
                {
                    parameters.Add(new JavaParameterInfo
                    {
                        Type = trimmed.Substring(0, lastSpace).Trim(),
                        Name = trimmed.Substring(lastSpace + 1).Trim()
                    });
                }
            }

            return parameters;
        }

        private DiscoveryResult CheckDiscoveryMethods(JavaClassInfo classInfo, GeneratorConfiguration config)
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
                    if (regex.IsMatch(classInfo.FilePath))
                    {
                        return new DiscoveryResult(DiscoveryMethod.FilePath, filePattern.Trim());
                    }
                }
            }

            return null;
        }

        private void TrackDiscoveryConflict(JavaClassInfo classInfo, DiscoveryResult result, Dictionary<string, DiscoveryConflict> conflictTracker, string filePath)
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

        private bool IsPrimitiveType(string type)
        {
            var primitives = new[] { "boolean", "byte", "char", "short", "int", "long", "float", "double" };
            return primitives.Contains(type.ToLower());
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

        private class JavaClassInfo
        {
            public string Name { get; set; }
            public string FullName { get; set; }
            public string Package { get; set; }
            public string FilePath { get; set; }
            public bool IsPublic { get; set; }
            public bool IsAbstract { get; set; }
            public bool IsFinal { get; set; }
            public string SuperClass { get; set; }
            public List<string> Interfaces { get; set; } = new List<string>();
            public List<string> Annotations { get; set; } = new List<string>();
            public List<JavaFieldInfo> Fields { get; set; } = new List<JavaFieldInfo>();
            public List<JavaMethodInfo> Methods { get; set; } = new List<JavaMethodInfo>();
        }

        private class JavaFieldInfo
        {
            public string Name { get; set; }
            public string Type { get; set; }
            public bool IsPublic { get; set; }
            public bool IsStatic { get; set; }
            public bool IsFinal { get; set; }
            public List<string> Annotations { get; set; } = new List<string>();
        }

        private class JavaMethodInfo
        {
            public string Name { get; set; }
            public string ReturnType { get; set; }
            public bool IsPublic { get; set; }
            public bool IsStatic { get; set; }
            public bool IsAbstract { get; set; }
            public bool IsAsync { get; set; }
            public List<string> Annotations { get; set; } = new List<string>();
            public List<JavaParameterInfo> Parameters { get; set; } = new List<JavaParameterInfo>();
        }

        private class JavaParameterInfo
        {
            public string Name { get; set; }
            public string Type { get; set; }
        }
    }
}