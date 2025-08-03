using Microsoft.Extensions.Logging;
using Location.Core.Helpers.CodeGenerationAttributes;
using System.Data.SqlTypes;
using System.Reflection;

namespace x3squaredcirecles.SQLSync.Generator.Services;

public class DependencyGraphBuilder
{
    private readonly ILogger<DependencyGraphBuilder> _logger;
    private readonly AssemblyLoader _assemblyLoader;

    public DependencyGraphBuilder(ILogger<DependencyGraphBuilder> logger, AssemblyLoader assemblyLoader)
    {
        _logger = logger;
        _assemblyLoader = assemblyLoader;
    }

    public EntityDependencyGraph BuildGraph(List<EntityMetadata> entities)
    {
        var graph = new EntityDependencyGraph();

        _logger.LogDebug("Building dependency graph for {Count} entities", entities.Count);

        // Add all entities to the graph
        foreach (var entity in entities)
        {
            var entityKey = graph.GetEntityKey(entity);
            graph.Entities[entityKey] = entity;
            graph.Dependencies[entityKey] = new List<string>();

            _logger.LogDebug("Added entity to graph: {EntityKey}", entityKey);
        }

        // Analyze foreign key dependencies
        foreach (var entity in entities)
        {
            var entityKey = graph.GetEntityKey(entity);

            foreach (var property in entity.Properties.Where(p => p.ForeignKey != null))
            {
                var fk = property.ForeignKey!;
                var referencedSchema = DetermineReferencedSchema(fk.ReferencedEntityType);
                var referencedTable = DetermineReferencedTable(fk.ReferencedEntityType);
                var referencedKey = graph.GetEntityKey(referencedSchema, referencedTable);

                // Only add dependency if the referenced entity exists in our graph
                if (graph.Entities.ContainsKey(referencedKey))
                {
                    if (!graph.Dependencies[entityKey].Contains(referencedKey))
                    {
                        graph.Dependencies[entityKey].Add(referencedKey);
                        _logger.LogDebug("Added dependency: {Entity} depends on {ReferencedEntity}",
                            entityKey, referencedKey);
                    }
                }
                else
                {
                    _logger.LogWarning("Foreign key reference to unknown entity: {EntityKey} -> {ReferencedKey}",
                        entityKey, referencedKey);
                }
            }
        }

        _logger.LogInformation("Dependency graph built with {EntityCount} entities and {DependencyCount} dependencies",
            graph.Entities.Count,
            graph.Dependencies.Values.Sum(deps => deps.Count));

        return graph;
    }

    public List<EntityMetadata> TopologicalSort(EntityDependencyGraph graph)
    {
        _logger.LogDebug("Performing topological sort on dependency graph");

        var result = new List<string>();
        var visited = new HashSet<string>();
        var visiting = new HashSet<string>();

        foreach (var entityKey in graph.Entities.Keys)
        {
            if (!visited.Contains(entityKey))
            {
                if (!DepthFirstSort(entityKey, graph, visited, visiting, result))
                {
                    var cycle = DetectCycle(entityKey, graph, new HashSet<string>(), new List<string>());
                    var cycleDescription = string.Join(" -> ", cycle);

                    _logger.LogError("Circular dependency detected: {Cycle}", cycleDescription);
                    throw new InvalidOperationException($"Circular dependency detected: {cycleDescription}");
                }
            }
        }

        var sortedEntities = result.Select(key => graph.Entities[key]).ToList();

        _logger.LogInformation("Topological sort completed. Entity creation order: {Order}",
            string.Join(" -> ", sortedEntities.Select(e => $"{e.Schema}.{e.TableName}")));

        return sortedEntities;
    }

    private bool DepthFirstSort(string entityKey, EntityDependencyGraph graph,
        HashSet<string> visited, HashSet<string> visiting, List<string> result)
    {
        if (visiting.Contains(entityKey))
        {
            _logger.LogDebug("Circular dependency detected at: {EntityKey}", entityKey);
            return false; // Circular dependency
        }

        if (visited.Contains(entityKey))
            return true; // Already processed

        visiting.Add(entityKey);

        // Visit all dependencies first
        foreach (var dependency in graph.Dependencies[entityKey])
        {
            if (!DepthFirstSort(dependency, graph, visited, visiting, result))
                return false;
        }

        visiting.Remove(entityKey);
        visited.Add(entityKey);
        result.Add(entityKey); // Add to result after all dependencies

        return true;
    }

    private List<string> DetectCycle(string startEntity, EntityDependencyGraph graph,
        HashSet<string> visited, List<string> path)
    {
        if (visited.Contains(startEntity))
        {
            var cycleStart = path.IndexOf(startEntity);
            return cycleStart >= 0 ? path.Skip(cycleStart).ToList() : path;
        }

        visited.Add(startEntity);
        path.Add(startEntity);

        foreach (var dependency in graph.Dependencies[startEntity])
        {
            var cycle = DetectCycle(dependency, graph, visited, path);
            if (cycle.Count > 0)
                return cycle;
        }

        path.RemoveAt(path.Count - 1);
        return new List<string>();
    }

    private string DetermineReferencedSchema(Type referencedEntityType)
    {
        // Try to determine schema from the type's assembly
        var assembly = referencedEntityType.Assembly;
        var assemblyName = assembly.GetName().Name;

        if (assemblyName != null)
        {
            if (assemblyName.Contains("Core.Domain"))
                return "Core";
            else if (assemblyName.Contains("Photography.Domain"))
                return "Photography";
            else if (assemblyName.Contains("Fishing.Domain"))
                return "Fishing";
            else if (assemblyName.Contains("Hunting.Domain"))
                return "Hunting";
            else
            {
                // Try to extract from namespace
                var namespaceParts = referencedEntityType.Namespace?.Split('.') ?? Array.Empty<string>();
                if (namespaceParts.Length >= 2 && namespaceParts[0] == "Location")
                {
                    return namespaceParts[1]; // Return the schema part
                }
            }
        }

        _logger.LogWarning("Could not determine schema for referenced entity type: {TypeName}", referencedEntityType.Name);
        return "Unknown";
    }

    private string DetermineReferencedTable(Type referencedEntityType)
    {
        // Check for SqlTable attribute
        var sqlTableAttribute = referencedEntityType.GetCustomAttribute<SqlTableAttribute>();
        if (sqlTableAttribute != null)
            return sqlTableAttribute.TableName;

        // Check for SQLite Table attribute for backward compatibility
        var sqliteTableAttribute = referencedEntityType.GetCustomAttribute<SQLite.TableAttribute>();
        if (sqliteTableAttribute != null)
            return sqliteTableAttribute.Name;

        // Default to class name
        return referencedEntityType.Name;
    }

    public void ValidateDependencyGraph(EntityDependencyGraph graph)
    {
        _logger.LogDebug("Validating dependency graph integrity");

        var issues = new List<string>();

        // Check for self-references
        foreach (var kvp in graph.Dependencies)
        {
            if (kvp.Value.Contains(kvp.Key))
            {
                issues.Add($"Self-reference detected: {kvp.Key}");
            }
        }

        // Check for missing referenced entities
        foreach (var kvp in graph.Dependencies)
        {
            foreach (var dependency in kvp.Value)
            {
                if (!graph.Entities.ContainsKey(dependency))
                {
                    issues.Add($"Missing referenced entity: {kvp.Key} -> {dependency}");
                }
            }
        }

        // Check for orphaned entities (no dependencies but also not referenced)
        var referencedEntities = graph.Dependencies.Values.SelectMany(deps => deps).ToHashSet();
        var orphanedEntities = graph.Entities.Keys
            .Where(key => !referencedEntities.Contains(key) && graph.Dependencies[key].Count == 0)
            .ToList();

        if (orphanedEntities.Any())
        {
            _logger.LogInformation("Found {Count} orphaned entities (no dependencies, not referenced): {Entities}",
                orphanedEntities.Count, string.Join(", ", orphanedEntities));
        }

        if (issues.Any())
        {
            var issueDescription = string.Join("; ", issues);
            _logger.LogError("Dependency graph validation failed: {Issues}", issueDescription);
            throw new InvalidOperationException($"Dependency graph validation failed: {issueDescription}");
        }

        _logger.LogDebug("Dependency graph validation passed");
    }

    public void LogDependencyGraph(EntityDependencyGraph graph)
    {
        if (!_logger.IsEnabled(LogLevel.Debug))
            return;

        _logger.LogDebug("=== Dependency Graph Details ===");

        foreach (var kvp in graph.Dependencies.OrderBy(x => x.Key))
        {
            var entity = kvp.Key;
            var dependencies = kvp.Value;

            if (dependencies.Any())
            {
                _logger.LogDebug("{Entity} depends on: {Dependencies}",
                    entity, string.Join(", ", dependencies));
            }
            else
            {
                _logger.LogDebug("{Entity} has no dependencies", entity);
            }
        }

        _logger.LogDebug("=== End Dependency Graph ===");
    }
}