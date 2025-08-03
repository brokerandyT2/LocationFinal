using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;

namespace x3squaredcircles.Version.Calculator.Services;

public class SchemaChangeDetector
{
    private readonly ILogger<SchemaChangeDetector> _logger;

    public SchemaChangeDetector(ILogger<SchemaChangeDetector> logger)
    {
        _logger = logger;
    }

    public async Task<List<SchemaChange>> DetectSchemaChangesAsync(Solution solution, string? baseline)
    {
        var schemaChanges = new List<SchemaChange>();

        if (solution.DomainProject == null)
        {
            _logger.LogInformation("No domain project found - skipping schema change detection");
            return schemaChanges;
        }

        try
        {
            _logger.LogInformation("Detecting schema changes in domain project: {Project}", solution.DomainProject.Name);

            // Get current entities with [ExportToSQL] attribute
            var currentEntities = await GetExportToSqlEntitiesAsync(solution.DomainProject);
            _logger.LogDebug("Found {Count} current entities with [ExportToSQL]", currentEntities.Count);

            if (baseline == null)
            {
                // No baseline - all entities are new
                foreach (var entity in currentEntities)
                {
                    schemaChanges.Add(new SchemaChange
                    {
                        Type = "NewEntity",
                        Description = $"New entity: {entity.Name}",
                        EntityName = entity.Name
                    });
                }
                return schemaChanges;
            }

            // Get baseline entities from git
            var baselineEntities = await GetBaselineEntitiesAsync(solution.DomainProject, baseline);
            _logger.LogDebug("Found {Count} baseline entities", baselineEntities.Count);

            // Compare entities
            schemaChanges.AddRange(CompareEntities(baselineEntities, currentEntities));

            _logger.LogInformation("Detected {Count} schema changes", schemaChanges.Count);
            return schemaChanges;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to detect schema changes");
            return schemaChanges;
        }
    }

    private async Task<List<EntityInfo>> GetExportToSqlEntitiesAsync(Project domainProject)
    {
        var entities = new List<EntityInfo>();

        try
        {
            // Find domain assembly
            var assemblyPath = FindDomainAssembly(domainProject);
            if (assemblyPath == null)
            {
                _logger.LogWarning("Domain assembly not found for project: {Project}", domainProject.Name);
                return entities;
            }

            // Load assembly and scan for [ExportToSQL] entities
            var assembly = Assembly.LoadFrom(assemblyPath);
            var entityTypes = assembly.GetTypes()
                .Where(t => t.IsClass &&
                           t.IsPublic &&
                           !t.IsAbstract &&
                           HasExportToSqlAttribute(t))
                .ToList();

            foreach (var type in entityTypes)
            {
                var entity = CreateEntityInfo(type);
                entities.Add(entity);
            }

            _logger.LogDebug("Loaded {Count} entities from assembly: {Assembly}", entities.Count, assemblyPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get ExportToSQL entities from domain project");
        }

        return entities;
    }

    private string? FindDomainAssembly(Project domainProject)
    {
        var projectDir = Path.GetDirectoryName(domainProject.Path);
        if (projectDir == null) return null;

        var binPath = Path.Combine(projectDir, "bin");
        if (!Directory.Exists(binPath)) return null;

        // Look for assembly in Debug/Release folders
        var searchPaths = new[]
        {
            Path.Combine(binPath, "Debug"),
            Path.Combine(binPath, "Release")
        };

        foreach (var searchPath in searchPaths)
        {
            if (!Directory.Exists(searchPath)) continue;

            // Look for net* target framework folders
            var tfmDirs = Directory.GetDirectories(searchPath, "net*");
            foreach (var tfmDir in tfmDirs)
            {
                var assemblyName = $"{Path.GetFileNameWithoutExtension(domainProject.Path)}.dll";
                var assemblyPath = Path.Combine(tfmDir, assemblyName);

                if (File.Exists(assemblyPath))
                {
                    return assemblyPath;
                }
            }
        }

        return null;
    }

    private bool HasExportToSqlAttribute(Type type)
    {
        return type.GetCustomAttributes(false)
            .Any(attr => attr.GetType().Name == "ExportToSQLAttribute");
    }

    private EntityInfo CreateEntityInfo(Type type)
    {
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.GetMethod?.IsPublic == true)
            .Where(p => !HasIgnoreAttribute(p))
            .Select(p => new Services.PropertyInfo
            {
                Name = p.Name,
                Type = GetSimpleTypeName(p.PropertyType)
            })
            .ToList();

        return new EntityInfo
        {
            Name = type.Name,
            FullName = type.FullName ?? type.Name,
            Properties = properties
        };
    }

    private bool HasIgnoreAttribute(System.Reflection.PropertyInfo property)
    {
        return property.GetCustomAttributes(false)
            .Any(attr => attr.GetType().Name.Contains("Ignore"));
    }

    private string GetSimpleTypeName(Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;
        return underlyingType.Name;
    }

    private async Task<List<EntityInfo>> GetBaselineEntitiesAsync(Project domainProject, string baseline)
    {
        var entities = new List<EntityInfo>();

        try
        {
            // Get all .cs files in domain project at baseline
            var projectDir = Path.GetDirectoryName(domainProject.Path);
            if (projectDir == null) return entities;

            var csFiles = await GetCsFilesFromGitAsync(projectDir, baseline);

            foreach (var (filePath, content) in csFiles)
            {
                var fileEntities = ParseEntitiesFromSource(content, filePath);
                entities.AddRange(fileEntities);
            }

            _logger.LogDebug("Parsed {Count} baseline entities from {FileCount} files",
                entities.Count, csFiles.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get baseline entities");
        }

        return entities;
    }

    private async Task<List<(string filePath, string content)>> GetCsFilesFromGitAsync(string projectDir, string baseline)
    {
        var files = new List<(string, string)>();

        try
        {
            // Get list of .cs files at baseline
            var result = await ExecuteGitCommandAsync($"ls-tree -r --name-only {baseline} -- {projectDir}");

            if (result.ExitCode != 0) return files;

            var csFiles = result.Output
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Where(f => f.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var file in csFiles)
            {
                var contentResult = await ExecuteGitCommandAsync($"show {baseline}:{file}");
                if (contentResult.ExitCode == 0)
                {
                    files.Add((file, contentResult.Output));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get C# files from git at baseline: {Baseline}", baseline);
        }

        return files;
    }

    private List<EntityInfo> ParseEntitiesFromSource(string sourceCode, string filePath)
    {
        var entities = new List<EntityInfo>();

        try
        {
            // Look for classes with [ExportToSQL] attribute
            var classPattern = @"\[ExportToSQL\][\s\S]*?public\s+class\s+(\w+)[\s\S]*?\{([\s\S]*?)\}";
            var matches = Regex.Matches(sourceCode, classPattern, RegexOptions.Multiline);

            foreach (Match match in matches)
            {
                var className = match.Groups[1].Value;
                var classBody = match.Groups[2].Value;

                var properties = ParsePropertiesFromClassBody(classBody);

                entities.Add(new EntityInfo
                {
                    Name = className,
                    FullName = $"{ExtractNamespace(sourceCode)}.{className}",
                    Properties = properties
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to parse entities from source file: {FilePath}", filePath);
        }

        return entities;
    }

    private List<Services.PropertyInfo> ParsePropertiesFromClassBody(string classBody)
    {
        var properties = new List<Services.PropertyInfo>();

        // Match public properties: public Type PropertyName { get; set; }
        var propertyPattern = @"public\s+(\w+(?:\?)?)\s+(\w+)\s*\{\s*get;\s*set;\s*\}";
        var matches = Regex.Matches(classBody, propertyPattern);

        foreach (Match match in matches)
        {
            var type = match.Groups[1].Value;
            var name = match.Groups[2].Value;

            properties.Add(new Services.PropertyInfo
            {
                Name = name,
                Type = type
            });
        }

        return properties;
    }

    private string ExtractNamespace(string sourceCode)
    {
        var namespaceMatch = Regex.Match(sourceCode, @"namespace\s+([\w\.]+)");
        return namespaceMatch.Success ? namespaceMatch.Groups[1].Value : "Unknown";
    }

    private List<SchemaChange> CompareEntities(List<EntityInfo> baseline, List<EntityInfo> current)
    {
        var changes = new List<SchemaChange>();

        var baselineDict = baseline.ToDictionary(e => e.Name, e => e);
        var currentDict = current.ToDictionary(e => e.Name, e => e);

        // Check for new entities
        foreach (var entity in current.Where(e => !baselineDict.ContainsKey(e.Name)))
        {
            changes.Add(new SchemaChange
            {
                Type = "NewEntity",
                Description = $"New entity: {entity.Name}",
                EntityName = entity.Name
            });
        }

        // Check for removed entities
        foreach (var entity in baseline.Where(e => !currentDict.ContainsKey(e.Name)))
        {
            changes.Add(new SchemaChange
            {
                Type = "RemovedEntity",
                Description = $"Removed entity: {entity.Name}",
                EntityName = entity.Name
            });
        }

        // Check for modified entities
        foreach (var entity in current.Where(e => baselineDict.ContainsKey(e.Name)))
        {
            var baselineEntity = baselineDict[entity.Name];
            var entityChanges = CompareEntityProperties(baselineEntity, entity);
            changes.AddRange(entityChanges);
        }

        return changes;
    }

    private List<SchemaChange> CompareEntityProperties(EntityInfo baseline, EntityInfo current)
    {
        var changes = new List<SchemaChange>();

        var baselineProps = baseline.Properties.ToDictionary(p => p.Name, p => p);
        var currentProps = current.Properties.ToDictionary(p => p.Name, p => p);

        // Check for new properties
        foreach (var prop in current.Properties.Where(p => !baselineProps.ContainsKey(p.Name)))
        {
            changes.Add(new SchemaChange
            {
                Type = "NewProperty",
                Description = $"New property: {current.Name}.{prop.Name} ({prop.Type})",
                EntityName = current.Name
            });
        }

        // Check for removed properties
        foreach (var prop in baseline.Properties.Where(p => !currentProps.ContainsKey(p.Name)))
        {
            changes.Add(new SchemaChange
            {
                Type = "RemovedProperty",
                Description = $"Removed property: {current.Name}.{prop.Name}",
                EntityName = current.Name
            });
        }

        // Check for type changes
        foreach (var prop in current.Properties.Where(p => baselineProps.ContainsKey(p.Name)))
        {
            var baselineProp = baselineProps[prop.Name];
            if (baselineProp.Type != prop.Type)
            {
                changes.Add(new SchemaChange
                {
                    Type = "PropertyTypeChange",
                    Description = $"Property type changed: {current.Name}.{prop.Name} ({baselineProp.Type} → {prop.Type})",
                    EntityName = current.Name
                });
            }
        }

        return changes;
    }

    private async Task<GitCommandResult> ExecuteGitCommandAsync(string arguments)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = arguments,
            WorkingDirectory = Directory.GetCurrentDirectory(),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = processStartInfo };
        process.Start();

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        return new GitCommandResult
        {
            ExitCode = process.ExitCode,
            Output = output.Trim(),
            Error = error.Trim()
        };
    }
}

public record EntityInfo
{
    public string Name { get; init; } = "";
    public string FullName { get; init; } = "";
    public List<Services.PropertyInfo> Properties { get; init; } = new();
}

public record PropertyInfo
{
    public string Name { get; init; } = "";
    public string Type { get; init; } = "";
}