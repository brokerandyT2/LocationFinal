using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using x3squaredcircles.SQLSync.Generator.Models;

namespace x3squaredcircles.SQLSync.Generator.Services
{
    public interface ILanguageAnalyzerFactory
    {
        Task<ILanguageAnalyzer> GetAnalyzerAsync(string language);
    }

    public interface ILanguageAnalyzer
    {
        Task<List<DiscoveredEntity>> DiscoverEntitiesAsync(string sourcePath, string trackAttribute);
    }

    public class LanguageAnalyzerFactory : ILanguageAnalyzerFactory
    {
        private readonly ICSharpAnalyzerService _csharpAnalyzer;
        private readonly IJavaAnalyzerService _javaAnalyzer;
        private readonly IPythonAnalyzerService _pythonAnalyzer;
        private readonly IJavaScriptAnalyzerService _javascriptAnalyzer;
        private readonly ITypeScriptAnalyzerService _typescriptAnalyzer;
        private readonly IGoAnalyzerService _goAnalyzer;
        private readonly ILogger<LanguageAnalyzerFactory> _logger;

        public LanguageAnalyzerFactory(
            ICSharpAnalyzerService csharpAnalyzer,
            IJavaAnalyzerService javaAnalyzer,
            IPythonAnalyzerService pythonAnalyzer,
            IJavaScriptAnalyzerService javascriptAnalyzer,
            ITypeScriptAnalyzerService typescriptAnalyzer,
            IGoAnalyzerService goAnalyzer,
            ILogger<LanguageAnalyzerFactory> logger)
        {
            _csharpAnalyzer = csharpAnalyzer;
            _javaAnalyzer = javaAnalyzer;
            _pythonAnalyzer = pythonAnalyzer;
            _javascriptAnalyzer = javascriptAnalyzer;
            _typescriptAnalyzer = typescriptAnalyzer;
            _goAnalyzer = goAnalyzer;
            _logger = logger;
        }

        public async Task<ILanguageAnalyzer> GetAnalyzerAsync(string language)
        {
            _logger.LogDebug("Getting language analyzer for: {Language}", language);

            return language.ToLowerInvariant() switch
            {
                "csharp" => _csharpAnalyzer,
                "java" => _javaAnalyzer,
                "python" => _pythonAnalyzer,
                "javascript" => _javascriptAnalyzer,
                "typescript" => _typescriptAnalyzer,
                "go" => _goAnalyzer,
                _ => throw new SqlSchemaException(SqlSchemaExitCode.InvalidConfiguration,
                    $"Unsupported language: {language}")
            };
        }
    }

    // C# Analyzer Service
    public interface ICSharpAnalyzerService : ILanguageAnalyzer { }

    public class CSharpAnalyzerService : ICSharpAnalyzerService
    {
        private readonly ILogger<CSharpAnalyzerService> _logger;

        public CSharpAnalyzerService(ILogger<CSharpAnalyzerService> logger)
        {
            _logger = logger;
        }

        public async Task<List<DiscoveredEntity>> DiscoverEntitiesAsync(string sourcePath, string trackAttribute)
        {
            _logger.LogInformation("Analyzing C# assemblies in: {SourcePath}", sourcePath);

            var entities = new List<DiscoveredEntity>();

            try
            {
                var assemblies = await LoadAssembliesAsync(sourcePath);
                _logger.LogDebug("Loaded {AssemblyCount} assemblies", assemblies.Count);

                foreach (var assembly in assemblies)
                {
                    var assemblyEntities = await AnalyzeAssemblyAsync(assembly, trackAttribute);
                    entities.AddRange(assemblyEntities);
                }

                _logger.LogInformation("✓ C# analysis complete: {EntityCount} entities discovered", entities.Count);
                return entities;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "C# entity discovery failed");
                throw new SqlSchemaException(SqlSchemaExitCode.EntityDiscoveryFailure,
                    $"C# entity discovery failed: {ex.Message}", ex);
            }
        }

        private async Task<List<Assembly>> LoadAssembliesAsync(string sourcePath)
        {
            var assemblies = new List<Assembly>();

            if (File.Exists(sourcePath) && sourcePath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var assembly = Assembly.LoadFrom(sourcePath);
                    assemblies.Add(assembly);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load assembly: {AssemblyPath}", sourcePath);
                }
            }
            else if (Directory.Exists(sourcePath))
            {
                var dllFiles = Directory.GetFiles(sourcePath, "*.dll", SearchOption.AllDirectories);
                var exeFiles = Directory.GetFiles(sourcePath, "*.exe", SearchOption.AllDirectories);
                var allFiles = dllFiles.Concat(exeFiles);

                foreach (var assemblyFile in allFiles)
                {
                    try
                    {
                        var assembly = Assembly.LoadFrom(assemblyFile);
                        assemblies.Add(assembly);
                        _logger.LogDebug("Loaded assembly: {AssemblyName}", assembly.GetName().Name);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Could not load assembly: {AssemblyFile}", assemblyFile);
                    }
                }
            }

            return assemblies;
        }

        private async Task<List<DiscoveredEntity>> AnalyzeAssemblyAsync(Assembly assembly, string trackAttribute)
        {
            var entities = new List<DiscoveredEntity>();

            try
            {
                var types = assembly.GetTypes()
                    .Where(t => t.IsClass && !t.IsAbstract && HasTrackingAttribute(t, trackAttribute))
                    .ToList();

                foreach (var type in types)
                {
                    var entity = await AnalyzeTypeAsync(type, trackAttribute);
                    if (entity != null)
                    {
                        entities.Add(entity);
                    }
                }
            }
            catch (ReflectionTypeLoadException ex)
            {
                _logger.LogWarning("Failed to load some types from assembly {AssemblyName}: {LoaderExceptions}",
                    assembly.GetName().Name, string.Join(", ", ex.LoaderExceptions.Select(e => e?.Message)));

                var validTypes = ex.Types.Where(t => t != null && HasTrackingAttribute(t, trackAttribute));
                foreach (var type in validTypes)
                {
                    var entity = await AnalyzeTypeAsync(type!, trackAttribute);
                    if (entity != null)
                    {
                        entities.Add(entity);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to analyze assembly: {AssemblyName}", assembly.GetName().Name);
            }

            return entities;
        }

        private bool HasTrackingAttribute(Type type, string trackAttribute)
        {
            return type.GetCustomAttributes(false)
                .Any(attr => attr.GetType().Name.Equals(trackAttribute, StringComparison.OrdinalIgnoreCase) ||
                            attr.GetType().FullName?.Contains(trackAttribute, StringComparison.OrdinalIgnoreCase) == true);
        }

        private async Task<DiscoveredEntity?> AnalyzeTypeAsync(Type type, string trackAttribute)
        {
            try
            {
                var entity = new DiscoveredEntity
                {
                    Name = type.Name,
                    FullName = type.FullName ?? type.Name,
                    Namespace = type.Namespace ?? string.Empty,
                    TableName = GetTableName(type),
                    SchemaName = GetSchemaName(type),
                    SourceFile = GetSourceFileName(type),
                    Properties = new List<DiscoveredProperty>(),
                    Indexes = new List<DiscoveredIndex>(),
                    Relationships = new List<DiscoveredRelationship>(),
                    Attributes = new Dictionary<string, object>
                    {
                        ["track_attribute"] = trackAttribute,
                        ["language"] = "csharp",
                        ["assembly"] = type.Assembly.GetName().Name ?? "Unknown"
                    }
                };

                var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                foreach (var property in properties)
                {
                    var discoveredProperty = AnalyzeProperty(property);
                    if (discoveredProperty != null)
                    {
                        entity.Properties.Add(discoveredProperty);
                    }
                }

                AnalyzeRelationships(type, entity);
                AnalyzeIndexes(type, entity);

                return entity;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to analyze type: {TypeName}", type.FullName);
                return null;
            }
        }

        private DiscoveredProperty? AnalyzeProperty(PropertyInfo property)
        {
            try
            {
                var propertyType = property.PropertyType;
                var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

                var discoveredProperty = new DiscoveredProperty
                {
                    Name = property.Name,
                    Type = GetSimpleTypeName(underlyingType),
                    SqlType = MapCSharpTypeToSql(underlyingType),
                    IsNullable = propertyType != underlyingType || !propertyType.IsValueType,
                    IsPrimaryKey = HasAttribute(property, "Key") || HasAttribute(property, "KeyAttribute") || property.Name.Equals("Id", StringComparison.OrdinalIgnoreCase),
                    IsForeignKey = property.Name.EndsWith("Id", StringComparison.OrdinalIgnoreCase) && !property.Name.Equals("Id", StringComparison.OrdinalIgnoreCase),
                    IsUnique = HasAttribute(property, "Unique") || HasAttribute(property, "UniqueAttribute"),
                    IsIndexed = HasAttribute(property, "Index") || HasAttribute(property, "IndexAttribute"),
                    Attributes = new Dictionary<string, object>()
                };

                AnalyzePropertyAttributes(property, discoveredProperty);
                return discoveredProperty;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to analyze property: {PropertyName}", property.Name);
                return null;
            }
        }

        private void AnalyzePropertyAttributes(PropertyInfo property, DiscoveredProperty discoveredProperty)
        {
            foreach (var attr in property.GetCustomAttributes(false))
            {
                var attrType = attr.GetType();

                if (attrType.Name.Contains("StringLength") || attrType.Name.Contains("MaxLength"))
                {
                    var lengthProperty = attrType.GetProperty("Length") ?? attrType.GetProperty("MaximumLength");
                    if (lengthProperty != null)
                    {
                        discoveredProperty.MaxLength = (int?)lengthProperty.GetValue(attr);
                    }
                }

                if (attrType.Name.Contains("Required"))
                {
                    discoveredProperty.IsNullable = false;
                }

                if (attrType.Name.Contains("Column"))
                {
                    var nameProperty = attrType.GetProperty("Name");
                    if (nameProperty != null)
                    {
                        var columnName = nameProperty.GetValue(attr) as string;
                        if (!string.IsNullOrEmpty(columnName))
                        {
                            discoveredProperty.Attributes["column_name"] = columnName;
                        }
                    }

                    var typeProperty = attrType.GetProperty("TypeName");
                    if (typeProperty != null)
                    {
                        var typeName = typeProperty.GetValue(attr) as string;
                        if (!string.IsNullOrEmpty(typeName))
                        {
                            discoveredProperty.SqlType = typeName;
                        }
                    }
                }

                if (attrType.Name.Contains("DefaultValue"))
                {
                    var valueProperty = attrType.GetProperty("Value");
                    if (valueProperty != null)
                    {
                        discoveredProperty.DefaultValue = valueProperty.GetValue(attr)?.ToString();
                    }
                }
            }
        }

        private void AnalyzeRelationships(Type type, DiscoveredEntity entity)
        {
            var navigationProperties = type.GetProperties()
                .Where(p => IsNavigationProperty(p))
                .ToList();

            foreach (var navProperty in navigationProperties)
            {
                var relationship = new DiscoveredRelationship
                {
                    Name = navProperty.Name,
                    Type = GetRelationshipType(navProperty),
                    ReferencedEntity = GetReferencedEntityName(navProperty),
                    ReferencedTable = GetReferencedEntityName(navProperty),
                    ForeignKeyColumns = GetForeignKeyColumns(navProperty, entity),
                    ReferencedColumns = new List<string> { "Id" },
                    OnDeleteAction = GetCascadeAction(navProperty, "Delete"),
                    OnUpdateAction = GetCascadeAction(navProperty, "Update"),
                    Attributes = new Dictionary<string, object>
                    {
                        ["navigation_property"] = navProperty.Name,
                        ["property_type"] = navProperty.PropertyType.Name
                    }
                };

                entity.Relationships.Add(relationship);
            }
        }

        private void AnalyzeIndexes(Type type, DiscoveredEntity entity)
        {
            var indexAttributes = type.GetCustomAttributes(false)
                .Where(attr => attr.GetType().Name.Contains("Index"))
                .ToList();

            foreach (var indexAttr in indexAttributes)
            {
                var attrType = indexAttr.GetType();
                var nameProperty = attrType.GetProperty("Name");
                var columnsProperty = attrType.GetProperty("Columns") ?? attrType.GetProperty("PropertyNames");
                var isUniqueProperty = attrType.GetProperty("IsUnique");

                var index = new DiscoveredIndex
                {
                    Name = nameProperty?.GetValue(indexAttr) as string ?? $"IX_{entity.Name}",
                    Columns = ParseIndexColumns(columnsProperty?.GetValue(indexAttr)),
                    IsUnique = (bool?)isUniqueProperty?.GetValue(indexAttr) ?? false,
                    IsClustered = false,
                    Attributes = new Dictionary<string, object>
                    {
                        ["from_attribute"] = true
                    }
                };

                entity.Indexes.Add(index);
            }
        }

        private bool IsNavigationProperty(PropertyInfo property)
        {
            var type = property.PropertyType;

            if (type.IsClass && type != typeof(string) && !type.IsPrimitive)
            {
                return true;
            }

            if (type.IsGenericType)
            {
                var genericTypeDefinition = type.GetGenericTypeDefinition();
                return genericTypeDefinition == typeof(ICollection<>) ||
                       genericTypeDefinition == typeof(IList<>) ||
                       genericTypeDefinition == typeof(List<>) ||
                       genericTypeDefinition == typeof(IEnumerable<>);
            }

            return false;
        }

        private string GetRelationshipType(PropertyInfo property)
        {
            var type = property.PropertyType;

            if (type.IsGenericType)
            {
                var genericTypeDefinition = type.GetGenericTypeDefinition();
                if (genericTypeDefinition == typeof(ICollection<>) ||
                    genericTypeDefinition == typeof(IList<>) ||
                    genericTypeDefinition == typeof(List<>) ||
                    genericTypeDefinition == typeof(IEnumerable<>))
                {
                    return "OneToMany";
                }
            }

            return "ManyToOne";
        }

        private string GetReferencedEntityName(PropertyInfo property)
        {
            var type = property.PropertyType;

            if (type.IsGenericType)
            {
                return type.GetGenericArguments().FirstOrDefault()?.Name ?? "Unknown";
            }

            return type.Name;
        }

        private List<string> GetForeignKeyColumns(PropertyInfo navProperty, DiscoveredEntity entity)
        {
            var foreignKeyAttr = navProperty.GetCustomAttributes(false)
                .FirstOrDefault(attr => attr.GetType().Name.Contains("ForeignKey"));

            if (foreignKeyAttr != null)
            {
                var nameProperty = foreignKeyAttr.GetType().GetProperty("Name");
                var foreignKeyName = nameProperty?.GetValue(foreignKeyAttr) as string;
                if (!string.IsNullOrEmpty(foreignKeyName))
                {
                    return new List<string> { foreignKeyName };
                }
            }

            var referencedEntityName = GetReferencedEntityName(navProperty);
            return new List<string> { $"{referencedEntityName}Id" };
        }

        private string GetCascadeAction(PropertyInfo property, string actionType)
        {
            var cascadeAttrs = property.GetCustomAttributes(false)
                .Where(attr => attr.GetType().Name.Contains("Cascade") || attr.GetType().Name.Contains("DeleteBehavior"))
                .ToList();

            foreach (var attr in cascadeAttrs)
            {
                var actionProperty = attr.GetType().GetProperty($"On{actionType}") ?? attr.GetType().GetProperty("DeleteBehavior");
                if (actionProperty != null)
                {
                    var value = actionProperty.GetValue(attr)?.ToString();
                    return value ?? "NO_ACTION";
                }
            }

            return "NO_ACTION";
        }

        private List<string> ParseIndexColumns(object? columnsValue)
        {
            if (columnsValue == null) return new List<string>();

            if (columnsValue is string columnString)
            {
                return columnString.Split(',').Select(s => s.Trim()).ToList();
            }

            if (columnsValue is string[] columnArray)
            {
                return columnArray.ToList();
            }

            return new List<string>();
        }

        private string GetTableName(Type type)
        {
            var tableAttr = type.GetCustomAttributes(false)
                .FirstOrDefault(attr => attr.GetType().Name.Contains("Table"));

            if (tableAttr != null)
            {
                var nameProperty = tableAttr.GetType().GetProperty("Name");
                var tableName = nameProperty?.GetValue(tableAttr) as string;
                if (!string.IsNullOrEmpty(tableName))
                {
                    return tableName;
                }
            }

            return type.Name;
        }

        private string? GetSchemaName(Type type)
        {
            var tableAttr = type.GetCustomAttributes(false)
                .FirstOrDefault(attr => attr.GetType().Name.Contains("Table"));

            if (tableAttr != null)
            {
                var schemaProperty = tableAttr.GetType().GetProperty("Schema");
                return schemaProperty?.GetValue(tableAttr) as string;
            }

            return null;
        }

        private string GetSourceFileName(Type type)
        {
            try
            {
                return Path.GetFileName(type.Assembly.Location) ?? "Unknown.dll";
            }
            catch
            {
                return "Unknown.dll";
            }
        }

        private bool HasAttribute(PropertyInfo property, string attributeName)
        {
            return property.GetCustomAttributes(false)
                .Any(attr => attr.GetType().Name.Equals(attributeName, StringComparison.OrdinalIgnoreCase) ||
                            attr.GetType().Name.Equals(attributeName.Replace("Attribute", ""), StringComparison.OrdinalIgnoreCase));
        }

        private string GetSimpleTypeName(Type type)
        {
            if (type == typeof(int)) return "int";
            if (type == typeof(long)) return "long";
            if (type == typeof(short)) return "short";
            if (type == typeof(byte)) return "byte";
            if (type == typeof(bool)) return "bool";
            if (type == typeof(string)) return "string";
            if (type == typeof(DateTime)) return "DateTime";
            if (type == typeof(decimal)) return "decimal";
            if (type == typeof(double)) return "double";
            if (type == typeof(float)) return "float";
            if (type == typeof(Guid)) return "Guid";

            return type.Name;
        }

        private string MapCSharpTypeToSql(Type type)
        {
            if (type == typeof(int)) return "INT";
            if (type == typeof(long)) return "BIGINT";
            if (type == typeof(short)) return "SMALLINT";
            if (type == typeof(byte)) return "TINYINT";
            if (type == typeof(bool)) return "BIT";
            if (type == typeof(string)) return "NVARCHAR(255)";
            if (type == typeof(DateTime)) return "DATETIME2";
            if (type == typeof(decimal)) return "DECIMAL(18,2)";
            if (type == typeof(double)) return "FLOAT";
            if (type == typeof(float)) return "REAL";
            if (type == typeof(Guid)) return "UNIQUEIDENTIFIER";

            return "NVARCHAR(255)";
        }
    }

    // Java Analyzer Service
    public interface IJavaAnalyzerService : ILanguageAnalyzer { }

    public class JavaAnalyzerService : IJavaAnalyzerService
    {
        private readonly ILogger<JavaAnalyzerService> _logger;

        public JavaAnalyzerService(ILogger<JavaAnalyzerService> logger)
        {
            _logger = logger;
        }

        public async Task<List<DiscoveredEntity>> DiscoverEntitiesAsync(string sourcePath, string trackAttribute)
        {
            _logger.LogInformation("Analyzing Java classes in: {SourcePath}", sourcePath);
            throw new SqlSchemaException(SqlSchemaExitCode.EntityDiscoveryFailure,
                "Java entity discovery not yet implemented");
        }
    }

    // Python Analyzer Service
    public interface IPythonAnalyzerService : ILanguageAnalyzer { }

    public class PythonAnalyzerService : IPythonAnalyzerService
    {
        private readonly ILogger<PythonAnalyzerService> _logger;

        public PythonAnalyzerService(ILogger<PythonAnalyzerService> logger)
        {
            _logger = logger;
        }

        public async Task<List<DiscoveredEntity>> DiscoverEntitiesAsync(string sourcePath, string trackAttribute)
        {
            _logger.LogInformation("Analyzing Python modules in: {SourcePath}", sourcePath);
            throw new SqlSchemaException(SqlSchemaExitCode.EntityDiscoveryFailure,
                "Python entity discovery not yet implemented");
        }
    }

    // JavaScript Analyzer Service
    public interface IJavaScriptAnalyzerService : ILanguageAnalyzer { }

    public class JavaScriptAnalyzerService : IJavaScriptAnalyzerService
    {
        private readonly ILogger<JavaScriptAnalyzerService> _logger;

        public JavaScriptAnalyzerService(ILogger<JavaScriptAnalyzerService> logger)
        {
            _logger = logger;
        }

        public async Task<List<DiscoveredEntity>> DiscoverEntitiesAsync(string sourcePath, string trackAttribute)
        {
            _logger.LogInformation("Analyzing JavaScript files in: {SourcePath}", sourcePath);
            throw new SqlSchemaException(SqlSchemaExitCode.EntityDiscoveryFailure,
                "JavaScript entity discovery not yet implemented");
        }
    }

    // TypeScript Analyzer Service
    public interface ITypeScriptAnalyzerService : ILanguageAnalyzer { }

    public class TypeScriptAnalyzerService : ITypeScriptAnalyzerService
    {
        private readonly ILogger<TypeScriptAnalyzerService> _logger;

        public TypeScriptAnalyzerService(ILogger<TypeScriptAnalyzerService> logger)
        {
            _logger = logger;
        }

        public async Task<List<DiscoveredEntity>> DiscoverEntitiesAsync(string sourcePath, string trackAttribute)
        {
            _logger.LogInformation("Analyzing TypeScript files in: {SourcePath}", sourcePath);
            throw new SqlSchemaException(SqlSchemaExitCode.EntityDiscoveryFailure,
                "TypeScript entity discovery not yet implemented");
        }
    }

    // Go Analyzer Service
    public interface IGoAnalyzerService : ILanguageAnalyzer { }

    public class GoAnalyzerService : IGoAnalyzerService
    {
        private readonly ILogger<GoAnalyzerService> _logger;

        public GoAnalyzerService(ILogger<GoAnalyzerService> logger)
        {
            _logger = logger;
        }

        public async Task<List<DiscoveredEntity>> DiscoverEntitiesAsync(string sourcePath, string trackAttribute)
        {
            _logger.LogInformation("Analyzing Go packages in: {SourcePath}", sourcePath);
            throw new SqlSchemaException(SqlSchemaExitCode.EntityDiscoveryFailure,
                "Go entity discovery not yet implemented");
        }
    }
}