using Location.Core.Helpers.CodeGenerationAttributes;
using Microsoft.Extensions.Logging;

using System.Data.SqlTypes;
using System.Reflection;

namespace x3squaredcirecles.SQLSync.Generator.Services;

public class EntityAnalyzer
{
    private readonly ILogger<EntityAnalyzer> _logger;
    private readonly AssemblyLoader _assemblyLoader;

    public EntityAnalyzer(ILogger<EntityAnalyzer> logger, AssemblyLoader assemblyLoader)
    {
        _logger = logger;
        _assemblyLoader = assemblyLoader;
    }

    public async Task<List<EntityMetadata>> AnalyzeAssembliesAsync(List<string> assemblyPaths)
    {
        var allEntities = new List<EntityMetadata>();

        foreach (var assemblyPath in assemblyPaths)
        {
            var schema = _assemblyLoader.DetermineSchemaFromAssembly(assemblyPath);
            var entities = await AnalyzeAssemblyPathAsync(assemblyPath, schema);
            allEntities.AddRange(entities);
        }

        _logger.LogInformation("Analyzed {TotalCount} entities ({CoreCount} Core, {PhotoCount} Photography)",
            allEntities.Count,
            allEntities.Count(e => e.Schema == "Core"),
            allEntities.Count(e => e.Schema == "Photography"));

        return allEntities;
    }

    private async Task<List<EntityMetadata>> AnalyzeAssemblyPathAsync(string assemblyPath, string schema)
    {
        var entities = new List<EntityMetadata>();

        try
        {
            _logger.LogDebug("Analyzing assembly: {AssemblyPath}", assemblyPath);

            var assembly = Assembly.LoadFrom(assemblyPath);

            var entityTypes = assembly.GetTypes()
                .Where(t => t.IsClass &&
                           t.IsPublic &&
                           !t.IsAbstract &&
                           !t.HasCustomAttribute<SqlIgnoreAttribute>() &&
                           t.HasCustomAttribute<ExportToSQLAttribute>() &&
                           IsEntityType(t))
                .ToList();

            _logger.LogInformation("Found {Count} entities in {Schema} assembly: {Entities}",
                entityTypes.Count, schema, string.Join(", ", entityTypes.Select(t => t.Name)));

            foreach (var type in entityTypes)
            {
                try
                {
                    var metadata = CreateEntityMetadata(type, schema);
                    entities.Add(metadata);
                    _logger.LogDebug("Analyzed entity: {Name} - {PropertyCount} properties, {IndexCount} composite indexes",
                        type.Name, metadata.Properties.Count, metadata.CompositeIndexes.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to analyze entity: {Name}", type.Name);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze assembly: {AssemblyPath}", assemblyPath);
        }

        return entities;
    }

    private bool IsEntityType(Type type)
    {
        // Skip types that are clearly not entities
        if (type.Name.EndsWith("Enum") || type.IsEnum)
            return false;

        if (type.Name.EndsWith("Exception") || type.IsSubclassOf(typeof(Exception)))
            return false;

        if (type.Name.EndsWith("Attribute") || type.IsSubclassOf(typeof(Attribute)))
            return false;

        if (type.IsInterface || type.IsAbstract)
            return false;

        // Must have at least one public property that could be a column
        var hasProperties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Any(p => p.CanRead && !p.HasCustomAttribute<SqlIgnoreAttribute>());

        return hasProperties;
    }

    private EntityMetadata CreateEntityMetadata(Type type, string schema)
    {
        var metadata = new EntityMetadata
        {
            Name = type.Name,
            TableName = GetTableName(type),
            Schema = GetSchemaName(type, schema),
            FullName = type.FullName ?? type.Name,
            Namespace = type.Namespace ?? "",
            Source = schema,
            EntityType = type,
            IsIgnored = type.HasCustomAttribute<SqlIgnoreAttribute>()
        };

        metadata.Properties = GetEntityProperties(type);
        metadata.CompositeIndexes = GetCompositeIndexes(metadata.Properties);

        return metadata;
    }

    private string GetTableName(Type type)
    {
        var tableAttribute = type.GetCustomAttribute<SqlTableAttribute>();
        if (tableAttribute != null)
            return tableAttribute.TableName;

        // Check for SQLite Table attribute for backward compatibility
        var sqliteTableAttribute = type.GetCustomAttribute<SQLite.TableAttribute>();
        if (sqliteTableAttribute != null)
            return sqliteTableAttribute.Name;

        return type.Name;
    }

    private string GetSchemaName(Type type, string defaultSchema)
    {
        var schemaAttribute = type.GetCustomAttribute<SqlSchemaAttribute>();
        if (schemaAttribute != null)
            return schemaAttribute.SchemaName;

        return defaultSchema;
    }

    private List<PropertyMetadata> GetEntityProperties(Type type)
    {
        return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead &&
                       p.GetMethod?.IsPublic == true &&
                       !p.HasCustomAttribute<SqlIgnoreAttribute>() &&
                       !IsComputedProperty(p))
            .Select(CreatePropertyMetadata)
            .ToList();
    }

    private bool IsComputedProperty(PropertyInfo property)
    {
        // Skip properties that are get-only without backing fields (computed properties)
        if (property.CanRead && !property.CanWrite)
        {
            // Check if it has a backing field by looking for compiler-generated backing field
            var backingField = property.DeclaringType?.GetField($"<{property.Name}>k__BackingField",
                BindingFlags.NonPublic | BindingFlags.Instance);
            return backingField == null;
        }

        return false;
    }

    private PropertyMetadata CreatePropertyMetadata(PropertyInfo property)
    {
        var metadata = new PropertyMetadata
        {
            Name = property.Name,
            ColumnName = GetColumnName(property),
            PropertyType = property.PropertyType,
            PropertyInfo = property,
            IsNullable = IsNullableType(property.PropertyType),
            IsIgnored = property.HasCustomAttribute<SqlIgnoreAttribute>()
        };

        // Extract SQL type information
        ExtractSqlTypeInfo(property, metadata);

        // Extract constraint information
        ExtractConstraintInfo(property, metadata);

        // Extract default value information
        ExtractDefaultInfo(property, metadata);

        // Extract index information
        ExtractIndexInfo(property, metadata);

        // Extract foreign key information
        ExtractForeignKeyInfo(property, metadata);

        return metadata;
    }

    private string GetColumnName(PropertyInfo property)
    {
        var columnAttribute = property.GetCustomAttribute<SqlColumnAttribute>();
        if (columnAttribute != null)
            return columnAttribute.ColumnName;

        return property.Name;
    }

    private bool IsNullableType(Type type)
    {
        return Nullable.GetUnderlyingType(type) != null || !type.IsValueType;
    }

    private void ExtractSqlTypeInfo(PropertyInfo property, PropertyMetadata metadata)
    {
        var sqlTypeAttribute = property.GetCustomAttribute<SqlTypeAttribute>();
        if (sqlTypeAttribute != null)
        {
            metadata.SqlDataType = sqlTypeAttribute.DataType;
            metadata.Length = sqlTypeAttribute.Length;
            metadata.Precision = sqlTypeAttribute.Precision;
            metadata.Scale = sqlTypeAttribute.Scale;
        }
    }

    private void ExtractConstraintInfo(PropertyInfo property, PropertyMetadata metadata)
    {
        var constraintsAttribute = property.GetCustomAttribute<SqlConstraintsAttribute>();
        if (constraintsAttribute != null)
        {
            metadata.Constraints.AddRange(constraintsAttribute.Constraints);
        }

        // Check for SQLite attributes for backward compatibility
        if (property.HasCustomAttribute<SQLite.PrimaryKeyAttribute>())
        {
            metadata.IsPrimaryKey = true;
            metadata.Constraints.Add(SqlConstraint.PrimaryKey);
        }

        if (property.HasCustomAttribute<SQLite.AutoIncrementAttribute>())
        {
            metadata.IsIdentity = true;
            metadata.Constraints.Add(SqlConstraint.Identity);
        }

        if (property.HasCustomAttribute<SQLite.NotNullAttribute>())
        {
            metadata.Constraints.Add(SqlConstraint.NotNull);
        }
    }

    private void ExtractDefaultInfo(PropertyInfo property, PropertyMetadata metadata)
    {
        var defaultAttribute = property.GetCustomAttribute<SqlDefaultAttribute>();
        if (defaultAttribute != null)
        {
            metadata.DefaultType = defaultAttribute.DefaultType;
            metadata.CustomDefault = defaultAttribute.CustomValue;
        }
    }

    private void ExtractIndexInfo(PropertyInfo property, PropertyMetadata metadata)
    {
        var indexAttribute = property.GetCustomAttribute<SqlIndexAttribute>();
        if (indexAttribute != null)
        {
            metadata.IndexType = indexAttribute.Type;
            metadata.IndexGroup = indexAttribute.Group;
            metadata.IndexOrder = indexAttribute.Order;
            metadata.IndexName = indexAttribute.Name;
        }

        // Check for SQLite indexed attribute for backward compatibility
        if (property.HasCustomAttribute<SQLite.IndexedAttribute>())
        {
            metadata.IndexType = SqlIndexType.NonClustered;
        }
    }

    private void ExtractForeignKeyInfo(PropertyInfo property, PropertyMetadata metadata)
    {
        // Look for generic SqlForeignKeyAttribute<T>
        var foreignKeyAttribute = property.GetCustomAttributes()
            .FirstOrDefault(attr => attr.GetType().IsGenericType &&
                                  attr.GetType().GetGenericTypeDefinition() == typeof(SqlForeignKeyAttribute<>));

        if (foreignKeyAttribute != null)
        {
            var attributeType = foreignKeyAttribute.GetType();
            var referencedEntityType = attributeType.GetGenericArguments()[0];

            metadata.ForeignKey = new ForeignKeyMetadata
            {
                ReferencedEntityType = referencedEntityType,
                ReferencedColumn = GetPropertyValue<string>(foreignKeyAttribute, "ReferencedColumn") ?? "Id",
                OnDelete = GetPropertyValue<ForeignKeyAction>(foreignKeyAttribute, "OnDelete"),
                OnUpdate = GetPropertyValue<ForeignKeyAction>(foreignKeyAttribute, "OnUpdate"),
                Name = GetPropertyValue<string>(foreignKeyAttribute, "Name")
            };
        }
    }

    private T? GetPropertyValue<T>(object obj, string propertyName)
    {
        var property = obj.GetType().GetProperty(propertyName);
        var value = property?.GetValue(obj);
        return value is T result ? result : default(T);
    }

    private List<IndexMetadata> GetCompositeIndexes(List<PropertyMetadata> properties)
    {
        var compositeIndexes = new List<IndexMetadata>();

        var groupedIndexes = properties
            .Where(p => !string.IsNullOrEmpty(p.IndexGroup))
            .GroupBy(p => p.IndexGroup);

        foreach (var group in groupedIndexes)
        {
            var indexColumns = group
                .OrderBy(p => p.IndexOrder)
                .Select(p => new IndexColumnMetadata
                {
                    ColumnName = p.ColumnName,
                    Order = p.IndexOrder
                })
                .ToList();

            var firstProperty = group.First();
            var indexName = firstProperty.IndexName ?? $"IX_{group.Key}";

            compositeIndexes.Add(new IndexMetadata
            {
                Name = indexName,
                Type = firstProperty.IndexType ?? SqlIndexType.NonClustered,
                Columns = indexColumns,
                IsUnique = firstProperty.IndexType == SqlIndexType.Unique
            });
        }

        return compositeIndexes;
    }
}

public static class TypeExtensions
{
    public static bool HasCustomAttribute<T>(this Type type) where T : Attribute
    {
        return type.GetCustomAttribute<T>() != null;
    }

    public static bool HasCustomAttribute<T>(this PropertyInfo property) where T : Attribute
    {
        return property.GetCustomAttribute<T>() != null;
    }
}