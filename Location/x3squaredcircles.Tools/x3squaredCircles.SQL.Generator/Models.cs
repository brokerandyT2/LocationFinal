using Location.Core.Helpers.CodeGenerationAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SQLServerSyncGenerator
{
    public class EntityMetadata
    {
        public string Name { get; set; } = string.Empty;
        public string TableName { get; set; } = string.Empty;
        public string Schema { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Namespace { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty; // "Core", "Photography", etc.
        public List<PropertyMetadata> Properties { get; set; } = new();
        public List<IndexMetadata> CompositeIndexes { get; set; } = new();
        public Type EntityType { get; set; } = typeof(object);
        public bool IsIgnored { get; set; }
    }

    public class PropertyMetadata
    {
        public string Name { get; set; } = string.Empty;
        public string ColumnName { get; set; } = string.Empty;
        public Type PropertyType { get; set; } = typeof(object);
        public PropertyInfo PropertyInfo { get; set; } = null!;

        // SQL Type Information
        public SqlDataType? SqlDataType { get; set; }
        public int? Length { get; set; }
        public int? Precision { get; set; }
        public int? Scale { get; set; }

        // Constraints
        public List<SqlConstraint> Constraints { get; set; } = new();
        public bool IsNullable { get; set; }
        public bool IsPrimaryKey { get; set; }
        public bool IsIdentity { get; set; }

        // Default Values
        public SqlDefaultValue? DefaultType { get; set; }
        public string? CustomDefault { get; set; }

        // Index Information
        public SqlIndexType? IndexType { get; set; }
        public string? IndexGroup { get; set; }
        public int IndexOrder { get; set; }
        public string? IndexName { get; set; }

        // Foreign Key Information
        public ForeignKeyMetadata? ForeignKey { get; set; }

        // Flags
        public bool IsIgnored { get; set; }
        public bool IsComputed { get; set; }
    }

    public class ForeignKeyMetadata
    {
        public Type ReferencedEntityType { get; set; } = typeof(object);
        public string ReferencedSchema { get; set; } = string.Empty;
        public string ReferencedTable { get; set; } = string.Empty;
        public string ReferencedColumn { get; set; } = "Id";
        public ForeignKeyAction OnDelete { get; set; } = ForeignKeyAction.NoAction;
        public ForeignKeyAction OnUpdate { get; set; } = ForeignKeyAction.NoAction;
        public string? Name { get; set; }
    }

    public class IndexMetadata
    {
        public string Name { get; set; } = string.Empty;
        public SqlIndexType Type { get; set; } = SqlIndexType.NonClustered;
        public List<IndexColumnMetadata> Columns { get; set; } = new();
        public bool IsUnique { get; set; }
    }

    public class IndexColumnMetadata
    {
        public string ColumnName { get; set; } = string.Empty;
        public int Order { get; set; }
        public bool IsDescending { get; set; }
    }

    public class EntityDependencyGraph
    {
        public Dictionary<string, EntityMetadata> Entities { get; set; } = new();
        public Dictionary<string, List<string>> Dependencies { get; set; } = new();

        public string GetEntityKey(EntityMetadata entity)
        {
            return $"{entity.Schema}.{entity.TableName}";
        }

        public string GetEntityKey(string schema, string tableName)
        {
            return $"{schema}.{tableName}";
        }
    }
}