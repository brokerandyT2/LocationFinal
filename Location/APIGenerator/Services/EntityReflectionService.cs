using Location.Core.Helpers.CodeGenerationAttributes;
using Location.Tools.APIGenerator.Models;
using Microsoft.Extensions.Logging;
using SQLite;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using TableAttribute = SQLite.TableAttribute;

namespace Location.Tools.APIGenerator.Services;

public class EntityReflectionService
{
    private readonly ILogger<EntityReflectionService> _logger;

    public EntityReflectionService(ILogger<EntityReflectionService> logger)
    {
        _logger = logger;
    }

    public async Task<List<ExtractableEntity>> DiscoverExtractableEntitiesAsync(AssemblyInfo assemblyInfo, GeneratorOptions options)
    {
        try
        {
            var assembly = Assembly.LoadFrom(assemblyInfo.AssemblyPath);

            if (options.AutoDiscover)
            {
                return await DiscoverEntitiesWithExportAttributeAsync(assembly, assemblyInfo);
            }
            else
            {
                return await DiscoverManualExtractorsAsync(assembly, assemblyInfo, options);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to discover extractable entities from assembly: {Path}", assemblyInfo.AssemblyPath);
            throw;
        }
    }

    private async Task<List<ExtractableEntity>> DiscoverEntitiesWithExportAttributeAsync(Assembly assembly, AssemblyInfo assemblyInfo)
    {
        _logger.LogDebug("Auto-discovering entities with [ExportToSQL] attribute");

        var extractableEntities = new List<ExtractableEntity>();

        var entityTypes = assembly.GetTypes()
            .Where(t => t.IsClass &&
                       t.IsPublic &&
                       !t.IsAbstract &&
                       t.GetCustomAttribute<ExportToSQLAttribute>() != null)
            .ToList();

        _logger.LogInformation("Found {Count} entities with [ExportToSQL] attribute: {Entities}",
            entityTypes.Count, string.Join(", ", entityTypes.Select(t => t.Name)));

        foreach (var entityType in entityTypes)
        {
            try
            {
                var extractableEntity = await CreateExtractableEntityAsync(entityType, assemblyInfo);
                extractableEntities.Add(extractableEntity);

                _logger.LogDebug("Processed entity: {EntityName} → Table: {TableName}",
                    entityType.Name, extractableEntity.TableName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to process entity: {EntityName}", entityType.Name);
            }
        }

        return extractableEntities;
    }

    private async Task<List<ExtractableEntity>> DiscoverManualExtractorsAsync(Assembly assembly, AssemblyInfo assemblyInfo, GeneratorOptions options)
    {
        _logger.LogDebug("Using manual extractor override");

        if (string.IsNullOrEmpty(options.ManualExtractors))
        {
            throw new ArgumentException("Manual extractors list cannot be empty when not using auto-discover");
        }

        var extractableEntities = new List<ExtractableEntity>();
        var requestedTables = options.ManualExtractors.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .ToList();

        _logger.LogInformation("Manual extractor list: {Tables}", string.Join(", ", requestedTables));

        foreach (var tableName in requestedTables)
        {
            var entityType = FindEntityTypeByTableName(assembly, tableName, options.IgnoreExportAttribute);

            if (entityType == null)
            {
                _logger.LogWarning("Could not find entity for table: {TableName}", tableName);
                continue;
            }

            try
            {
                var extractableEntity = await CreateExtractableEntityAsync(entityType, assemblyInfo);
                extractableEntities.Add(extractableEntity);

                _logger.LogDebug("Manual override - processed entity: {EntityName} → Table: {TableName}",
                    entityType.Name, extractableEntity.TableName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to process manual entity: {EntityName}", entityType.Name);
            }
        }

        return extractableEntities;
    }

    private Type? FindEntityTypeByTableName(Assembly assembly, string tableName, bool ignoreExportAttribute)
    {
        // First, try to find by [Table] attribute
        var entityTypes = assembly.GetTypes()
            .Where(t => t.IsClass && t.IsPublic && !t.IsAbstract)
            .ToList();

        foreach (var entityType in entityTypes)
        {
            // Check [Table] attribute
            var tableAttribute = entityType.GetCustomAttribute<TableAttribute>();
            if (tableAttribute != null && tableAttribute.Name == tableName)
            {
                if (!ignoreExportAttribute && entityType.GetCustomAttribute<ExportToSQLAttribute>() == null)
                {
                    _logger.LogWarning("Entity {EntityName} for table {TableName} lacks [ExportToSQL] attribute",
                        entityType.Name, tableName);
                }
                return entityType;
            }

            // Check class name matches table name
            if (entityType.Name == tableName)
            {
                if (!ignoreExportAttribute && entityType.GetCustomAttribute<ExportToSQLAttribute>() == null)
                {
                    _logger.LogWarning("Entity {EntityName} lacks [ExportToSQL] attribute", entityType.Name);
                }
                return entityType;
            }
        }

        return null;
    }

    private async Task<ExtractableEntity> CreateExtractableEntityAsync(Type entityType, AssemblyInfo assemblyInfo)
    {
        var extractableEntity = new ExtractableEntity
        {
            EntityType = entityType,
            TableName = GetTableName(entityType),
            SchemaName = GetSchemaName(entityType, assemblyInfo.Source),
            ExportReason = GetExportReason(entityType),
            PropertyMappings = await GetPropertyMappingsAsync(entityType)
        };

        return extractableEntity;
    }

    private string GetTableName(Type entityType)
    {
        var tableAttribute = entityType.GetCustomAttribute<TableAttribute>();
        return tableAttribute?.Name ?? entityType.Name;
    }

    private string GetSchemaName(Type entityType, string defaultSchema)
    {
        var schemaAttribute = entityType.GetCustomAttribute<SqlSchemaAttribute>();
        return schemaAttribute?.SchemaName ?? defaultSchema;
    }

    private string GetExportReason(Type entityType)
    {
        var exportAttribute = entityType.GetCustomAttribute<ExportToSQLAttribute>();
        return exportAttribute?.Reason ?? "Marked for SQL Server export";
    }

    private async Task<List<PropertyMappingInfo>> GetPropertyMappingsAsync(Type entityType)
    {
        var propertyMappings = new List<PropertyMappingInfo>();

        var properties = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead &&
                       p.GetMethod?.IsPublic == true &&
                       !p.GetCustomAttributes<SqlIgnoreAttribute>().Any())
            .ToList();

        foreach (var property in properties)
        {
            var mapping = new PropertyMappingInfo
            {
                PropertyName = property.Name,
                ColumnName = GetColumnName(property),
                PropertyType = property.PropertyType,
                SqlServerType = GetSqlServerType(property),
                HasCustomMapping = property.GetCustomAttribute<SqlTypeAttribute>() != null
            };

            propertyMappings.Add(mapping);
        }

        return propertyMappings;
    }

    private string GetColumnName(PropertyInfo property)
    {
        var columnAttribute = property.GetCustomAttribute<SqlColumnAttribute>();
        return columnAttribute?.ColumnName ?? property.Name;
    }

    private string GetSqlServerType(PropertyInfo property)
    {
        // Check for explicit SqlType attribute
        var sqlTypeAttribute = property.GetCustomAttribute<SqlTypeAttribute>();
        if (sqlTypeAttribute != null)
        {
            return FormatSqlDataType(sqlTypeAttribute);
        }

        // Auto-map from .NET type
        return MapDotNetTypeToSqlServer(property.PropertyType);
    }

    private string FormatSqlDataType(SqlTypeAttribute sqlTypeAttribute)
    {
        return sqlTypeAttribute.DataType switch
        {
            SqlDataType.NVarChar => sqlTypeAttribute.Length.HasValue ? $"NVARCHAR({sqlTypeAttribute.Length})" : "NVARCHAR(255)",
            SqlDataType.VarChar => sqlTypeAttribute.Length.HasValue ? $"VARCHAR({sqlTypeAttribute.Length})" : "VARCHAR(255)",
            SqlDataType.NVarCharMax => "NVARCHAR(MAX)",
            SqlDataType.VarCharMax => "VARCHAR(MAX)",
            SqlDataType.Int => "INT",
            SqlDataType.BigInt => "BIGINT",
            SqlDataType.SmallInt => "SMALLINT",
            SqlDataType.TinyInt => "TINYINT",
            SqlDataType.Decimal => sqlTypeAttribute.Precision.HasValue && sqlTypeAttribute.Scale.HasValue
                ? $"DECIMAL({sqlTypeAttribute.Precision},{sqlTypeAttribute.Scale})"
                : "DECIMAL(18,2)",
            SqlDataType.Float => "FLOAT",
            SqlDataType.Real => "REAL",
            SqlDataType.DateTime2 => "DATETIME2",
            SqlDataType.DateTime => "DATETIME",
            SqlDataType.Date => "DATE",
            SqlDataType.Time => "TIME",
            SqlDataType.Bit => "BIT",
            SqlDataType.UniqueIdentifier => "UNIQUEIDENTIFIER",
            _ => "NVARCHAR(255)"
        };
    }

    private string MapDotNetTypeToSqlServer(Type dotNetType)
    {
        var underlyingType = Nullable.GetUnderlyingType(dotNetType) ?? dotNetType;

        return underlyingType.Name switch
        {
            "String" => "NVARCHAR(255)",
            "Int32" => "INT",
            "Int64" => "BIGINT",
            "Int16" => "SMALLINT",
            "Byte" => "TINYINT",
            "Boolean" => "BIT",
            "Double" => "FLOAT",
            "Single" => "REAL",
            "Decimal" => "DECIMAL(18,2)",
            "DateTime" => "DATETIME2",
            "DateTimeOffset" => "DATETIMEOFFSET",
            "TimeSpan" => "TIME",
            "Guid" => "UNIQUEIDENTIFIER",
            "Byte[]" => "VARBINARY(MAX)",
            _ => "NVARCHAR(255)"
        };
    }
}