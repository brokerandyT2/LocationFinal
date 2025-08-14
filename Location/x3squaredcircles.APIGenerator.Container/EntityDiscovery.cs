using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace x3squaredcircles.APIGenerator.Container
{
    /// <summary>
    /// Discovers entities marked with tracking attributes across multiple languages
    /// </summary>
    public class EntityDiscovery
    {
        private readonly Configuration _config;
        private readonly Logger _logger;

        public EntityDiscovery(Configuration config, Logger logger)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Discover all entities marked with the tracking attribute
        /// </summary>
        /// <returns>List of discovered entities</returns>
        public async Task<List<DiscoveredEntity>> DiscoverEntitiesAsync()
        {
            _logger.LogStartPhase("Entity Discovery");

            try
            {
                using var operation = _logger.TimeOperation("Entity Discovery");

                var entities = _config.SelectedLanguage switch
                {
                    "csharp" => await DiscoverCSharpEntitiesAsync(),
                    "java" => await DiscoverJavaEntitiesAsync(),
                    "python" => await DiscoverPythonEntitiesAsync(),
                    "javascript" => await DiscoverJavaScriptEntitiesAsync(),
                    "typescript" => await DiscoverTypeScriptEntitiesAsync(),
                    "go" => await DiscoverGoEntitiesAsync(),
                    _ => throw new EntityDiscoveryException($"Unsupported language: {_config.SelectedLanguage}", 5)
                };

                _logger.LogEntityDiscovery(entities.Count, _config.TrackAttribute);
                _logger.LogEndPhase("Entity Discovery", true);

                return entities;
            }
            catch (Exception ex)
            {
                _logger.Error("Entity discovery failed", ex);
                _logger.LogEndPhase("Entity Discovery", false);
                throw new EntityDiscoveryException($"Entity discovery failed: {ex.Message}", 5);
            }
        }

        private async Task<List<DiscoveredEntity>> DiscoverCSharpEntitiesAsync()
        {
            var entities = new List<DiscoveredEntity>();

            // Get assembly paths
            var assemblyPaths = GetAssemblyPaths();

            foreach (var assemblyPath in assemblyPaths)
            {
                try
                {
                    _logger.Debug($"Analyzing assembly: {assemblyPath}");

                    var assembly = Assembly.LoadFrom(assemblyPath);
                    var types = assembly.GetTypes();

                    foreach (var type in types)
                    {
                        var attributes = type.GetCustomAttributes(false);
                        var trackingAttribute = attributes.FirstOrDefault(attr =>
                            attr.GetType().Name.Contains(_config.TrackAttribute) ||
                            attr.GetType().FullName.Contains(_config.TrackAttribute));

                        if (trackingAttribute != null || _config.IgnoreExportAttribute)
                        {
                            var entity = CreateEntityFromType(type, assemblyPath);
                            entities.Add(entity);

                            _logger.Debug($"Found entity: {entity.Name} in {entity.Namespace}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warn($"Failed to analyze assembly {assemblyPath}: {ex.Message}");
                }
            }

            return entities;
        }

        private async Task<List<DiscoveredEntity>> DiscoverJavaEntitiesAsync()
        {
            var entities = new List<DiscoveredEntity>();
            var classPaths = GetJavaClassPaths();

            foreach (var classPath in classPaths)
            {
                if (Directory.Exists(classPath))
                {
                    await DiscoverJavaEntitiesInDirectoryAsync(classPath, entities);
                }
                else if (File.Exists(classPath) && classPath.EndsWith(".jar"))
                {
                    await DiscoverJavaEntitiesInJarAsync(classPath, entities);
                }
            }

            return entities;
        }

        private async Task<List<DiscoveredEntity>> DiscoverPythonEntitiesAsync()
        {
            var entities = new List<DiscoveredEntity>();
            var pythonPaths = GetPythonPaths();

            foreach (var pythonPath in pythonPaths)
            {
                var pythonFiles = Directory.GetFiles(pythonPath, "*.py", SearchOption.AllDirectories);

                foreach (var file in pythonFiles)
                {
                    try
                    {
                        var content = await File.ReadAllTextAsync(file);
                        var fileEntities = AnalyzePythonFile(file, content);
                        entities.AddRange(fileEntities);
                    }
                    catch (Exception ex)
                    {
                        _logger.Debug($"Failed to analyze Python file {file}: {ex.Message}");
                    }
                }
            }

            return entities;
        }

        private async Task<List<DiscoveredEntity>> DiscoverJavaScriptEntitiesAsync()
        {
            var entities = new List<DiscoveredEntity>();
            var jsPaths = GetJavaScriptPaths();

            foreach (var jsPath in jsPaths)
            {
                var jsFiles = Directory.GetFiles(jsPath, "*.js", SearchOption.AllDirectories)
                    .Concat(Directory.GetFiles(jsPath, "*.mjs", SearchOption.AllDirectories));

                foreach (var file in jsFiles)
                {
                    try
                    {
                        var content = await File.ReadAllTextAsync(file);
                        var fileEntities = AnalyzeJavaScriptFile(file, content);
                        entities.AddRange(fileEntities);
                    }
                    catch (Exception ex)
                    {
                        _logger.Debug($"Failed to analyze JavaScript file {file}: {ex.Message}");
                    }
                }
            }

            return entities;
        }

        private async Task<List<DiscoveredEntity>> DiscoverTypeScriptEntitiesAsync()
        {
            var entities = new List<DiscoveredEntity>();
            var tsPaths = GetTypeScriptPaths();

            foreach (var tsPath in tsPaths)
            {
                var tsFiles = Directory.GetFiles(tsPath, "*.ts", SearchOption.AllDirectories)
                    .Concat(Directory.GetFiles(tsPath, "*.tsx", SearchOption.AllDirectories));

                foreach (var file in tsFiles)
                {
                    try
                    {
                        var content = await File.ReadAllTextAsync(file);
                        var fileEntities = AnalyzeTypeScriptFile(file, content);
                        entities.AddRange(fileEntities);
                    }
                    catch (Exception ex)
                    {
                        _logger.Debug($"Failed to analyze TypeScript file {file}: {ex.Message}");
                    }
                }
            }

            return entities;
        }

        private async Task<List<DiscoveredEntity>> DiscoverGoEntitiesAsync()
        {
            var entities = new List<DiscoveredEntity>();
            var goPaths = GetGoPaths();

            foreach (var goPath in goPaths)
            {
                var goFiles = Directory.GetFiles(goPath, "*.go", SearchOption.AllDirectories);

                foreach (var file in goFiles)
                {
                    try
                    {
                        var content = await File.ReadAllTextAsync(file);
                        var fileEntities = AnalyzeGoFile(file, content);
                        entities.AddRange(fileEntities);
                    }
                    catch (Exception ex)
                    {
                        _logger.Debug($"Failed to analyze Go file {file}: {ex.Message}");
                    }
                }
            }

            return entities;
        }

        private List<string> GetAssemblyPaths()
        {
            var paths = new List<string>();

            // Primary build output path
            if (!string.IsNullOrWhiteSpace(_config.BuildOutputPath) && Directory.Exists(_config.BuildOutputPath))
            {
                paths.AddRange(Directory.GetFiles(_config.BuildOutputPath, "*.dll", SearchOption.AllDirectories));
                paths.AddRange(Directory.GetFiles(_config.BuildOutputPath, "*.exe", SearchOption.AllDirectories));
            }

            // Entity paths (colon-separated)
            if (!string.IsNullOrWhiteSpace(_config.EntityPaths))
            {
                var entityPaths = _config.EntityPaths.Split(':', StringSplitOptions.RemoveEmptyEntries);
                foreach (var path in entityPaths)
                {
                    if (Directory.Exists(path))
                    {
                        paths.AddRange(Directory.GetFiles(path, "*.dll", SearchOption.AllDirectories));
                        paths.AddRange(Directory.GetFiles(path, "*.exe", SearchOption.AllDirectories));
                    }
                    else if (File.Exists(path) && (path.EndsWith(".dll") || path.EndsWith(".exe")))
                    {
                        paths.Add(path);
                    }
                }
            }

            // Fallback to common build output directories
            if (!paths.Any())
            {
                var commonPaths = new[] { "bin/Release", "bin/Debug", "build/libs", "target" };
                foreach (var commonPath in commonPaths)
                {
                    if (Directory.Exists(commonPath))
                    {
                        paths.AddRange(Directory.GetFiles(commonPath, "*.dll", SearchOption.AllDirectories));
                        paths.AddRange(Directory.GetFiles(commonPath, "*.exe", SearchOption.AllDirectories));
                    }
                }
            }

            return paths.Distinct().ToList();
        }

        private List<string> GetJavaClassPaths()
        {
            var paths = new List<string>();

            if (!string.IsNullOrWhiteSpace(_config.BuildOutputPath))
            {
                paths.Add(_config.BuildOutputPath);
            }

            if (!string.IsNullOrWhiteSpace(_config.EntityPaths))
            {
                paths.AddRange(_config.EntityPaths.Split(':', StringSplitOptions.RemoveEmptyEntries));
            }

            // Fallback to common Java build directories
            var commonPaths = new[] { "target/classes", "build/classes", "build/libs", "out/production" };
            paths.AddRange(commonPaths.Where(Directory.Exists));

            return paths.Distinct().ToList();
        }

        private List<string> GetPythonPaths()
        {
            var paths = new List<string> { Directory.GetCurrentDirectory() };

            if (!string.IsNullOrWhiteSpace(_config.EntityPaths))
            {
                paths.AddRange(_config.EntityPaths.Split(':', StringSplitOptions.RemoveEmptyEntries));
            }

            return paths.Where(Directory.Exists).Distinct().ToList();
        }

        private List<string> GetJavaScriptPaths()
        {
            var paths = new List<string> { Directory.GetCurrentDirectory() };

            if (!string.IsNullOrWhiteSpace(_config.EntityPaths))
            {
                paths.AddRange(_config.EntityPaths.Split(':', StringSplitOptions.RemoveEmptyEntries));
            }

            // Common JS build directories
            var commonPaths = new[] { "dist", "build", "lib", "src" };
            paths.AddRange(commonPaths.Where(Directory.Exists));

            return paths.Distinct().ToList();
        }

        private List<string> GetTypeScriptPaths()
        {
            var paths = new List<string> { Directory.GetCurrentDirectory() };

            if (!string.IsNullOrWhiteSpace(_config.EntityPaths))
            {
                paths.AddRange(_config.EntityPaths.Split(':', StringSplitOptions.RemoveEmptyEntries));
            }

            // Common TS source directories
            var commonPaths = new[] { "src", "lib", "types" };
            paths.AddRange(commonPaths.Where(Directory.Exists));

            return paths.Distinct().ToList();
        }

        private List<string> GetGoPaths()
        {
            var paths = new List<string> { Directory.GetCurrentDirectory() };

            if (!string.IsNullOrWhiteSpace(_config.EntityPaths))
            {
                paths.AddRange(_config.EntityPaths.Split(':', StringSplitOptions.RemoveEmptyEntries));
            }

            return paths.Where(Directory.Exists).Distinct().ToList();
        }

        private async Task DiscoverJavaEntitiesInDirectoryAsync(string classPath, List<DiscoveredEntity> entities)
        {
            var classFiles = Directory.GetFiles(classPath, "*.class", SearchOption.AllDirectories);

            foreach (var classFile in classFiles)
            {
                // For Java, we'd need to use reflection or bytecode analysis
                // For now, fallback to source code analysis
                var javaFile = classFile.Replace(".class", ".java");
                if (File.Exists(javaFile))
                {
                    var content = await File.ReadAllTextAsync(javaFile);
                    var fileEntities = AnalyzeJavaFile(javaFile, content);
                    entities.AddRange(fileEntities);
                }
            }
        }

        private async Task DiscoverJavaEntitiesInJarAsync(string jarPath, List<DiscoveredEntity> entities)
        {
            // For JAR files, we'd need to extract and analyze classes
            // This is a simplified implementation
            _logger.Debug($"JAR analysis not fully implemented for: {jarPath}");
        }

        private DiscoveredEntity CreateEntityFromType(Type type, string assemblyPath)
        {
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => new EntityProperty
                {
                    Name = p.Name,
                    Type = GetFriendlyTypeName(p.PropertyType),
                    IsNullable = IsNullableType(p.PropertyType),
                    Attributes = p.GetCustomAttributes(false).Select(a => a.GetType().Name).ToList()
                })
                .ToList();

            return new DiscoveredEntity
            {
                Name = type.Name,
                FullName = type.FullName,
                Namespace = type.Namespace,
                Language = "csharp",
                SourceFile = assemblyPath,
                Properties = properties,
                Attributes = type.GetCustomAttributes(false).Select(a => a.GetType().Name).ToList(),
                IsPublic = type.IsPublic,
                IsClass = type.IsClass,
                IsInterface = type.IsInterface
            };
        }

        private List<DiscoveredEntity> AnalyzeJavaFile(string filePath, string content)
        {
            var entities = new List<DiscoveredEntity>();

            // Extract package
            var packageMatch = Regex.Match(content, @"package\s+([\w\.]+);");
            var packageName = packageMatch.Success ? packageMatch.Groups[1].Value : "";

            // Extract classes with annotations
            var classPattern = @"@" + Regex.Escape(_config.TrackAttribute) + @".*?(?:public\s+)?(?:class|interface|enum)\s+(\w+)";
            var classMatches = Regex.Matches(content, classPattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);

            foreach (Match match in classMatches)
            {
                var className = match.Groups[1].Value;
                var entity = new DiscoveredEntity
                {
                    Name = className,
                    FullName = string.IsNullOrEmpty(packageName) ? className : $"{packageName}.{className}",
                    Namespace = packageName,
                    Language = "java",
                    SourceFile = filePath,
                    Properties = ExtractJavaProperties(content, className),
                    Attributes = new List<string> { _config.TrackAttribute },
                    IsPublic = content.Contains($"public class {className}") || content.Contains($"public interface {className}"),
                    IsClass = content.Contains($"class {className}"),
                    IsInterface = content.Contains($"interface {className}")
                };

                entities.Add(entity);
            }

            return entities;
        }

        private List<DiscoveredEntity> AnalyzePythonFile(string filePath, string content)
        {
            var entities = new List<DiscoveredEntity>();

            // Look for classes with decorators
            var classPattern = @"@" + Regex.Escape(_config.TrackAttribute) + @".*?\nclass\s+(\w+)";
            var classMatches = Regex.Matches(content, classPattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);

            foreach (Match match in classMatches)
            {
                var className = match.Groups[1].Value;
                var entity = new DiscoveredEntity
                {
                    Name = className,
                    FullName = className,
                    Namespace = Path.GetFileNameWithoutExtension(filePath),
                    Language = "python",
                    SourceFile = filePath,
                    Properties = ExtractPythonProperties(content, className),
                    Attributes = new List<string> { _config.TrackAttribute },
                    IsPublic = true,
                    IsClass = true,
                    IsInterface = false
                };

                entities.Add(entity);
            }

            return entities;
        }

        private List<DiscoveredEntity> AnalyzeJavaScriptFile(string filePath, string content)
        {
            var entities = new List<DiscoveredEntity>();

            // Look for classes or objects with comments containing the tracking attribute
            var patterns = new[]
            {
                @"/\*\*.*?" + Regex.Escape(_config.TrackAttribute) + @".*?\*/\s*(?:export\s+)?(?:class|function)\s+(\w+)",
                @"//" + Regex.Escape(_config.TrackAttribute) + @".*?\n(?:export\s+)?(?:class|function)\s+(\w+)"
            };

            foreach (var pattern in patterns)
            {
                var matches = Regex.Matches(content, pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);
                foreach (Match match in matches)
                {
                    var entityName = match.Groups[1].Value;
                    var entity = new DiscoveredEntity
                    {
                        Name = entityName,
                        FullName = entityName,
                        Namespace = Path.GetFileNameWithoutExtension(filePath),
                        Language = "javascript",
                        SourceFile = filePath,
                        Properties = new List<EntityProperty>(),
                        Attributes = new List<string> { _config.TrackAttribute },
                        IsPublic = true,
                        IsClass = content.Contains($"class {entityName}"),
                        IsInterface = false
                    };

                    entities.Add(entity);
                }
            }

            return entities;
        }

        private List<DiscoveredEntity> AnalyzeTypeScriptFile(string filePath, string content)
        {
            var entities = new List<DiscoveredEntity>();

            // Look for interfaces, classes, or types with decorators
            var patterns = new[]
            {
                @"@" + Regex.Escape(_config.TrackAttribute) + @".*?\n(?:export\s+)?(?:interface|class|type)\s+(\w+)",
                @"/\*\*.*?" + Regex.Escape(_config.TrackAttribute) + @".*?\*/\s*(?:export\s+)?(?:interface|class|type)\s+(\w+)"
            };

            foreach (var pattern in patterns)
            {
                var matches = Regex.Matches(content, pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);
                foreach (Match match in matches)
                {
                    var entityName = match.Groups[1].Value;
                    var entity = new DiscoveredEntity
                    {
                        Name = entityName,
                        FullName = entityName,
                        Namespace = Path.GetFileNameWithoutExtension(filePath),
                        Language = "typescript",
                        SourceFile = filePath,
                        Properties = ExtractTypeScriptProperties(content, entityName),
                        Attributes = new List<string> { _config.TrackAttribute },
                        IsPublic = true,
                        IsClass = content.Contains($"class {entityName}"),
                        IsInterface = content.Contains($"interface {entityName}")
                    };

                    entities.Add(entity);
                }
            }

            return entities;
        }

        private List<DiscoveredEntity> AnalyzeGoFile(string filePath, string content)
        {
            var entities = new List<DiscoveredEntity>();

            // Look for structs with comments containing the tracking attribute
            var structPattern = @"//" + Regex.Escape(_config.TrackAttribute) + @".*?\ntype\s+(\w+)\s+struct";
            var structMatches = Regex.Matches(content, structPattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);

            foreach (Match match in structMatches)
            {
                var structName = match.Groups[1].Value;
                var entity = new DiscoveredEntity
                {
                    Name = structName,
                    FullName = structName,
                    Namespace = ExtractGoPackageName(content),
                    Language = "go",
                    SourceFile = filePath,
                    Properties = ExtractGoProperties(content, structName),
                    Attributes = new List<string> { _config.TrackAttribute },
                    IsPublic = char.IsUpper(structName[0]),
                    IsClass = true,
                    IsInterface = false
                };

                entities.Add(entity);
            }

            return entities;
        }

        private List<EntityProperty> ExtractJavaProperties(string content, string className)
        {
            var properties = new List<EntityProperty>();
            var fieldPattern = @"(?:private|public|protected)?\s+(\w+(?:<\w+>)?)\s+(\w+);";
            var matches = Regex.Matches(content, fieldPattern);

            foreach (Match match in matches)
            {
                properties.Add(new EntityProperty
                {
                    Name = match.Groups[2].Value,
                    Type = match.Groups[1].Value,
                    IsNullable = false,
                    Attributes = new List<string>()
                });
            }

            return properties;
        }

        private List<EntityProperty> ExtractPythonProperties(string content, string className)
        {
            var properties = new List<EntityProperty>();

            // Extract properties from __init__ method
            var initPattern = @"def\s+__init__\s*\([^)]*\):(.*?)(?=def|\nclass|\Z)";
            var initMatch = Regex.Match(content, initPattern, RegexOptions.Singleline);

            if (initMatch.Success)
            {
                var initBody = initMatch.Groups[1].Value;
                var propPattern = @"self\.(\w+)\s*=";
                var propMatches = Regex.Matches(initBody, propPattern);

                foreach (Match match in propMatches)
                {
                    properties.Add(new EntityProperty
                    {
                        Name = match.Groups[1].Value,
                        Type = "object",
                        IsNullable = true,
                        Attributes = new List<string>()
                    });
                }
            }

            return properties;
        }

        private List<EntityProperty> ExtractTypeScriptProperties(string content, string entityName)
        {
            var properties = new List<EntityProperty>();

            // Extract properties from interface or class definition
            var entityPattern = $@"(?:interface|class)\s+{entityName}.*?\{{(.*?)\}}";
            var entityMatch = Regex.Match(content, entityPattern, RegexOptions.Singleline);

            if (entityMatch.Success)
            {
                var body = entityMatch.Groups[1].Value;
                var propPattern = @"(\w+)(\?)?:\s*([^;,\n]+)";
                var propMatches = Regex.Matches(body, propPattern);

                foreach (Match match in propMatches)
                {
                    properties.Add(new EntityProperty
                    {
                        Name = match.Groups[1].Value,
                        Type = match.Groups[3].Value.Trim(),
                        IsNullable = !string.IsNullOrEmpty(match.Groups[2].Value),
                        Attributes = new List<string>()
                    });
                }
            }

            return properties;
        }

        private List<EntityProperty> ExtractGoProperties(string content, string structName)
        {
            var properties = new List<EntityProperty>();

            var structPattern = $@"type\s+{structName}\s+struct\s*\{{(.*?)\}}";
            var structMatch = Regex.Match(content, structPattern, RegexOptions.Singleline);

            if (structMatch.Success)
            {
                var body = structMatch.Groups[1].Value;
                var fieldPattern = @"(\w+)\s+(\*?)(\w+(?:\[\])?(?:\.\w+)*)";
                var fieldMatches = Regex.Matches(body, fieldPattern);

                foreach (Match match in fieldMatches)
                {
                    properties.Add(new EntityProperty
                    {
                        Name = match.Groups[1].Value,
                        Type = match.Groups[3].Value,
                        IsNullable = !string.IsNullOrEmpty(match.Groups[2].Value),
                        Attributes = new List<string>()
                    });
                }
            }

            return properties;
        }

        private string ExtractGoPackageName(string content)
        {
            var packageMatch = Regex.Match(content, @"package\s+(\w+)");
            return packageMatch.Success ? packageMatch.Groups[1].Value : "main";
        }

        private string GetFriendlyTypeName(Type type)
        {
            if (type.IsGenericType)
            {
                var genericName = type.Name.Split('`')[0];
                var genericArgs = string.Join(", ", type.GetGenericArguments().Select(GetFriendlyTypeName));
                return $"{genericName}<{genericArgs}>";
            }

            return type.Name switch
            {
                "String" => "string",
                "Int32" => "int",
                "Int64" => "long",
                "Boolean" => "bool",
                "Decimal" => "decimal",
                "Double" => "double",
                "Single" => "float",
                "DateTime" => "DateTime",
                "Guid" => "Guid",
                _ => type.Name
            };
        }

        private bool IsNullableType(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }
    }

    public class DiscoveredEntity
    {
        public string Name { get; set; }
        public string FullName { get; set; }
        public string Namespace { get; set; }
        public string Language { get; set; }
        public string SourceFile { get; set; }
        public List<EntityProperty> Properties { get; set; } = new List<EntityProperty>();
        public List<string> Attributes { get; set; } = new List<string>();
        public bool IsPublic { get; set; }
        public bool IsClass { get; set; }
        public bool IsInterface { get; set; }
    }

    public class EntityProperty
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public bool IsNullable { get; set; }
        public List<string> Attributes { get; set; } = new List<string>();
    }

    public class EntityDiscoveryException : Exception
    {
        public int ExitCode { get; }

        public EntityDiscoveryException(string message, int exitCode) : base(message)
        {
            ExitCode = exitCode;
        }

        public EntityDiscoveryException(string message, int exitCode, Exception innerException) : base(message, innerException)
        {
            ExitCode = exitCode;
        }
    }
}