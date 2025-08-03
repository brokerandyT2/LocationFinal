// Enhanced EntityReflectionService with Extended ASCII support
// File: x3squaredCircles.API.Generator/Services/EntityReflectionService.cs

using Location.Core.Helpers.CodeGenerationAttributes;
using x3squaredcirecles.API.Generator.APIGenerator.Models;
using Microsoft.Extensions.Logging;
using SQLite;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using System.Text;
using System.Globalization;
using TableAttribute = SQLite.TableAttribute;

namespace x3squaredcirecles.API.Generator.APIGenerator.Services;

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
        _logger.LogDebug("Auto-discovering entities with [ExportToSQL] attribute and extended ASCII support");

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
                var extractableEntity = await CreateExtractableEntityWithExtendedASCIIAsync(entityType, assemblyInfo);
                extractableEntities.Add(extractableEntity);

                _logger.LogDebug("Processed entity with extended ASCII support: {EntityName} → Table: {TableName}",
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
        _logger.LogDebug("Using manual extractor override with extended ASCII support");

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
                var extractableEntity = await CreateExtractableEntityWithExtendedASCIIAsync(entityType, assemblyInfo);
                extractableEntities.Add(extractableEntity);

                _logger.LogDebug("Manual override - processed entity with extended ASCII: {EntityName} → Table: {TableName}",
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
        var entityTypes = assembly.GetTypes()
            .Where(t => t.IsClass && t.IsPublic && !t.IsAbstract)
            .ToList();

        foreach (var entityType in entityTypes)
        {
            // Check [Table] attribute with extended ASCII table name support
            var tableAttribute = entityType.GetCustomAttribute<TableAttribute>();
            if (tableAttribute != null && NormalizeTableName(tableAttribute.Name) == NormalizeTableName(tableName))
            {
                if (!ignoreExportAttribute && entityType.GetCustomAttribute<ExportToSQLAttribute>() == null)
                {
                    _logger.LogWarning("Entity {EntityName} for table {TableName} lacks [ExportToSQL] attribute",
                        entityType.Name, tableName);
                }
                return entityType;
            }

            // Check class name matches table name (normalized for extended ASCII)
            if (NormalizeTableName(entityType.Name) == NormalizeTableName(tableName))
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

    private async Task<ExtractableEntity> CreateExtractableEntityWithExtendedASCIIAsync(Type entityType, AssemblyInfo assemblyInfo)
    {
        var extractableEntity = new ExtractableEntity
        {
            EntityType = entityType,
            TableName = GetTableNameWithExtendedASCII(entityType),
            SchemaName = GetSchemaNameWithExtendedASCII(entityType, assemblyInfo.Source),
            ExportReason = GetExportReasonWithExtendedASCII(entityType),
            PropertyMappings = await GetPropertyMappingsWithExtendedASCIIAsync(entityType)
        };

        return extractableEntity;
    }

    private string GetTableNameWithExtendedASCII(Type entityType)
    {
        var tableAttribute = entityType.GetCustomAttribute<TableAttribute>();
        if (tableAttribute != null)
        {
            var tableName = tableAttribute.Name;
            // Normalize extended ASCII characters for consistency
            var normalizedTableName = NormalizeExtendedASCII(tableName);

            if (ContainsExtendedASCII(tableName))
            {
                _logger.LogDebug("Table name contains extended ASCII: {OriginalName} -> {NormalizedName}",
                    tableName, normalizedTableName);
            }

            return normalizedTableName;
        }

        return entityType.Name;
    }

    private string GetSchemaNameWithExtendedASCII(Type entityType, string defaultSchema)
    {
        var schemaAttribute = entityType.GetCustomAttribute<SqlSchemaAttribute>();
        if (schemaAttribute != null)
        {
            var schemaName = schemaAttribute.SchemaName;
            var normalizedSchemaName = NormalizeExtendedASCII(schemaName);

            if (ContainsExtendedASCII(schemaName))
            {
                _logger.LogDebug("Schema name contains extended ASCII: {OriginalName} -> {NormalizedName}",
                    schemaName, normalizedSchemaName);
            }

            return normalizedSchemaName;
        }

        return defaultSchema;
    }

    private string GetExportReasonWithExtendedASCII(Type entityType)
    {
        var exportAttribute = entityType.GetCustomAttribute<ExportToSQLAttribute>();
        if (exportAttribute != null)
        {
            var reason = exportAttribute.Reason ?? "Marked for SQL Server export";

            // Normalize extended ASCII characters in export reason
            var normalizedReason = NormalizeExtendedASCII(reason);

            if (ContainsExtendedASCII(reason))
            {
                _logger.LogDebug("Export reason contains extended ASCII: {OriginalReason} -> {NormalizedReason}",
                    reason, normalizedReason);
            }

            return normalizedReason;
        }

        return "Marked for SQL Server export";
    }

    private async Task<List<PropertyMappingInfo>> GetPropertyMappingsWithExtendedASCIIAsync(Type entityType)
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
                ColumnName = GetColumnNameWithExtendedASCII(property),
                PropertyType = property.PropertyType,
                SqlServerType = GetSqlServerTypeWithExtendedASCII(property),
                HasCustomMapping = property.GetCustomAttribute<SqlTypeAttribute>() != null
            };

            propertyMappings.Add(mapping);
        }

        return propertyMappings;
    }

    private string GetColumnNameWithExtendedASCII(PropertyInfo property)
    {
        var columnAttribute = property.GetCustomAttribute<SqlColumnAttribute>();
        if (columnAttribute != null)
        {
            var columnName = columnAttribute.ColumnName;
            var normalizedColumnName = NormalizeExtendedASCII(columnName);

            if (ContainsExtendedASCII(columnName))
            {
                _logger.LogDebug("Column name contains extended ASCII: {OriginalName} -> {NormalizedName}",
                    columnName, normalizedColumnName);
            }

            return normalizedColumnName;
        }

        return property.Name;
    }

    private string GetSqlServerTypeWithExtendedASCII(PropertyInfo property)
    {
        // Check for explicit SqlType attribute
        var sqlTypeAttribute = property.GetCustomAttribute<SqlTypeAttribute>();
        if (sqlTypeAttribute != null)
        {
            return FormatSqlDataTypeWithExtendedASCII(sqlTypeAttribute);
        }

        // Auto-map from .NET type with extended ASCII considerations
        return MapDotNetTypeToSqlServerWithExtendedASCII(property.PropertyType);
    }

    private string FormatSqlDataTypeWithExtendedASCII(SqlTypeAttribute sqlTypeAttribute)
    {
        return sqlTypeAttribute.DataType switch
        {
            SqlDataType.NVarChar => sqlTypeAttribute.Length.HasValue
                ? $"NVARCHAR({sqlTypeAttribute.Length}) COLLATE SQL_Latin1_General_CP1252_CI_AS"
                : "NVARCHAR(255) COLLATE SQL_Latin1_General_CP1252_CI_AS",
            SqlDataType.VarChar => sqlTypeAttribute.Length.HasValue
                ? $"VARCHAR({sqlTypeAttribute.Length}) COLLATE SQL_Latin1_General_CP1252_CI_AS"
                : "VARCHAR(255) COLLATE SQL_Latin1_General_CP1252_CI_AS",
            SqlDataType.NVarCharMax => "NVARCHAR(MAX) COLLATE SQL_Latin1_General_CP1252_CI_AS",
            SqlDataType.VarCharMax => "VARCHAR(MAX) COLLATE SQL_Latin1_General_CP1252_CI_AS",
            SqlDataType.Text => "TEXT COLLATE SQL_Latin1_General_CP1252_CI_AS",
            SqlDataType.NText => "NTEXT COLLATE SQL_Latin1_General_CP1252_CI_AS",
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
            _ => "NVARCHAR(255) COLLATE SQL_Latin1_General_CP1252_CI_AS"
        };
    }

    private string MapDotNetTypeToSqlServerWithExtendedASCII(Type dotNetType)
    {
        var underlyingType = Nullable.GetUnderlyingType(dotNetType) ?? dotNetType;

        return underlyingType.Name switch
        {
            // String types always use NVARCHAR with extended ASCII collation for maximum compatibility
            "String" => "NVARCHAR(255) COLLATE SQL_Latin1_General_CP1252_CI_AS",
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
            // Default to NVARCHAR with extended ASCII support for unknown types
            _ => "NVARCHAR(255) COLLATE SQL_Latin1_General_CP1252_CI_AS"
        };
    }

    private string NormalizeExtendedASCII(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        try
        {
            // Normalize to consistent Unicode form (Canonical Composition)
            return input.Normalize(NormalizationForm.FormC);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to normalize extended ASCII for: {Input}", input);
            return input; // Return original if normalization fails
        }
    }

    private string NormalizeTableName(string tableName)
    {
        if (string.IsNullOrEmpty(tableName))
            return tableName;

        // Normalize extended ASCII and convert to consistent case for comparison
        return NormalizeExtendedASCII(tableName).ToLowerInvariant();
    }

    private bool ContainsExtendedASCII(string text)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        // Check for extended ASCII characters (128-255) and Unicode characters (> 255)
        return text.Any(c => c > 127);
    }

    /// <summary>
    /// Analyzes entity metadata for extended ASCII character usage
    /// </summary>
    public EntityExtendedASCIIAnalysis AnalyzeEntityExtendedASCII(List<ExtractableEntity> entities)
    {
        var analysis = new EntityExtendedASCIIAnalysis();

        foreach (var entity in entities)
        {
            analysis.TotalEntities++;

            // Check table name
            if (ContainsExtendedASCII(entity.TableName))
            {
                analysis.EntitiesWithExtendedASCIITableNames++;
                analysis.ExtendedASCIITableNames.Add(entity.TableName);
            }

            // Check schema name
            if (ContainsExtendedASCII(entity.SchemaName))
            {
                analysis.EntitiesWithExtendedASCIISchemaNames++;
                if (!analysis.ExtendedASCIISchemaNames.Contains(entity.SchemaName))
                {
                    analysis.ExtendedASCIISchemaNames.Add(entity.SchemaName);
                }
            }

            // Check export reason
            if (ContainsExtendedASCII(entity.ExportReason))
            {
                analysis.EntitiesWithExtendedASCIIReasons++;
            }

            // Check property mappings
            foreach (var property in entity.PropertyMappings)
            {
                analysis.TotalProperties++;

                if (ContainsExtendedASCII(property.ColumnName))
                {
                    analysis.PropertiesWithExtendedASCIIColumnNames++;
                }

                if (property.PropertyType == typeof(string))
                {
                    analysis.StringProperties++;
                }
            }
        }

        analysis.ExtendedASCIIUsagePercentage = analysis.TotalEntities > 0
            ? (double)(analysis.EntitiesWithExtendedASCIITableNames + analysis.EntitiesWithExtendedASCIISchemaNames + analysis.EntitiesWithExtendedASCIIReasons) / (analysis.TotalEntities * 3) * 100
            : 0;

        _logger.LogInformation("Entity Extended ASCII Analysis: {ExtendedEntities}/{TotalEntities} entities ({Percentage:F1}%) use extended ASCII characters",
            analysis.EntitiesWithExtendedASCIITableNames + analysis.EntitiesWithExtendedASCIISchemaNames + analysis.EntitiesWithExtendedASCIIReasons,
            analysis.TotalEntities, analysis.ExtendedASCIIUsagePercentage);

        return analysis;
    }

    /// <summary>
    /// Validates that entity reflection preserves extended ASCII characters correctly
    /// </summary>
    public async Task<bool> ValidateExtendedASCIIPreservationAsync(List<ExtractableEntity> entities)
    {
        try
        {
            var testPassed = true;

            foreach (var entity in entities)
            {
                // Test table name preservation
                if (ContainsExtendedASCII(entity.TableName))
                {
                    var originalTableName = entity.EntityType.GetCustomAttribute<TableAttribute>()?.Name ?? entity.EntityType.Name;
                    var normalizedOriginal = NormalizeExtendedASCII(originalTableName);

                    if (entity.TableName != normalizedOriginal)
                    {
                        _logger.LogWarning("Extended ASCII table name not preserved: {Original} -> {Processed}",
                            originalTableName, entity.TableName);
                        testPassed = false;
                    }
                }

                // Test property mappings by checking against the actual entity type properties
                var entityProperties = entity.EntityType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

                foreach (var property in entity.PropertyMappings)
                {
                    if (ContainsExtendedASCII(property.ColumnName))
                    {
                        // Find the corresponding PropertyInfo from the entity type
                        var entityProperty = entityProperties.FirstOrDefault(p => p.Name == property.PropertyName);
                        if (entityProperty != null)
                        {
                            var originalColumnName = entityProperty.GetCustomAttribute<SqlColumnAttribute>()?.ColumnName ?? property.PropertyName;
                            var normalizedOriginal = NormalizeExtendedASCII(originalColumnName);

                            if (property.ColumnName != normalizedOriginal)
                            {
                                _logger.LogWarning("Extended ASCII column name not preserved: {Original} -> {Processed}",
                                    originalColumnName, property.ColumnName);
                                testPassed = false;
                            }
                        }
                    }
                }
            }

            if (testPassed)
            {
                _logger.LogInformation("✅ Extended ASCII preservation validation passed for all entities");
            }
            else
            {
                _logger.LogWarning("⚠️ Extended ASCII preservation validation failed for some entities");
            }

            return testPassed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate extended ASCII preservation");
            return false;
        }
    }
}

/// <summary>
/// Analysis results for extended ASCII usage in entity metadata
/// </summary>
public class EntityExtendedASCIIAnalysis
{
    public int TotalEntities { get; set; }
    public int TotalProperties { get; set; }
    public int StringProperties { get; set; }

    public int EntitiesWithExtendedASCIITableNames { get; set; }
    public int EntitiesWithExtendedASCIISchemaNames { get; set; }
    public int EntitiesWithExtendedASCIIReasons { get; set; }
    public int PropertiesWithExtendedASCIIColumnNames { get; set; }

    public List<string> ExtendedASCIITableNames { get; set; } = new();
    public List<string> ExtendedASCIISchemaNames { get; set; } = new();

    public double ExtendedASCIIUsagePercentage { get; set; }
}