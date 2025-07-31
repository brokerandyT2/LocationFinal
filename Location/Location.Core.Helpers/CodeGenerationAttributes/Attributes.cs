using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Location.Core.Helpers.CodeGenerationAttributes
{
    // <summary>
    /// SQL Server data types for schema generation
    /// </summary>
    public enum SqlDataType
    {
        // String types
        NVarChar,
        VarChar,
        NVarCharMax,
        VarCharMax,
        Text,
        NText,

        // Numeric types
        Int,
        BigInt,
        SmallInt,
        TinyInt,
        Decimal,
        Float,
        Real,
        Money,
        SmallMoney,

        // Date/Time types
        DateTime2,
        DateTime,
        Date,
        Time,
        SmallDateTime,
        DateTimeOffset,

        // Other types
        Bit,
        UniqueIdentifier,
        Binary,
        VarBinary,
        VarBinaryMax,
        Image,
        Xml,
        Geography,
        Geometry,
        Timestamp
    }

    /// <summary>
    /// SQL Server constraint types
    /// </summary>
    public enum SqlConstraint
    {
        NotNull,
        Unique,
        PrimaryKey,
        Identity,
        Check
    }

    /// <summary>
    /// SQL Server index types
    /// </summary>
    public enum SqlIndexType
    {
        None,
        NonClustered,
        Clustered,
        Unique
    }

    /// <summary>
    /// Foreign key referential actions
    /// </summary>
    public enum ForeignKeyAction
    {
        NoAction,
        Cascade,
        SetNull,
        SetDefault
    }

    /// <summary>
    /// SQL Server default value types
    /// </summary>
    public enum SqlDefaultValue
    {
        GetUtcDate,
        GetDate,
        NewId,
        Zero,
        EmptyString,
        Null
    }

    /// <summary>
    /// Specifies SQL Server data type for a property
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class SqlTypeAttribute : Attribute
    {
        public SqlDataType DataType { get; set; }
        public int? Length { get; set; }
        public int? Precision { get; set; }
        public int? Scale { get; set; }

        public SqlTypeAttribute(SqlDataType dataType)
        {
            DataType = dataType;
        }

        public SqlTypeAttribute(SqlDataType dataType, int length)
        {
            DataType = dataType;
            Length = length;
        }
    }

    /// <summary>
    /// Specifies SQL Server constraints for a property
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class SqlConstraintsAttribute : Attribute
    {
        public SqlConstraint[] Constraints { get; set; }

        public SqlConstraintsAttribute(params SqlConstraint[] constraints)
        {
            Constraints = constraints;
        }
    }

    /// <summary>
    /// Specifies a SQL Server index for a property
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class SqlIndexAttribute : Attribute
    {
        public SqlIndexType Type { get; set; } = SqlIndexType.NonClustered;
        public string? Group { get; set; }
        public int Order { get; set; } = 1;
        public string? Name { get; set; }

        public SqlIndexAttribute()
        {
        }

        public SqlIndexAttribute(SqlIndexType type)
        {
            Type = type;
        }
    }

    /// <summary>
    /// Specifies a foreign key relationship for a property
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class SqlForeignKeyAttribute<TReferencedEntity> : Attribute
    {
        public string ReferencedColumn { get; set; } = "Id";
        public ForeignKeyAction OnDelete { get; set; } = ForeignKeyAction.NoAction;
        public ForeignKeyAction OnUpdate { get; set; } = ForeignKeyAction.NoAction;
        public string? Name { get; set; }

        public Type ReferencedEntityType => typeof(TReferencedEntity);
    }

    /// <summary>
    /// Specifies a default value for a property
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class SqlDefaultAttribute : Attribute
    {
        public SqlDefaultValue DefaultType { get; set; }
        public string? CustomValue { get; set; }

        public SqlDefaultAttribute(SqlDefaultValue defaultType)
        {
            DefaultType = defaultType;
        }

        public SqlDefaultAttribute(string customValue)
        {
            DefaultType = SqlDefaultValue.Null; // Placeholder
            CustomValue = customValue;
        }
    }

    /// <summary>
    /// Overrides the default table name for an entity
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class SqlTableAttribute : Attribute
    {
        public string TableName { get; set; }

        public SqlTableAttribute(string tableName)
        {
            TableName = tableName;
        }
    }

    /// <summary>
    /// Overrides the default schema for an entity
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class SqlSchemaAttribute : Attribute
    {
        public string SchemaName { get; set; }

        public SqlSchemaAttribute(string schemaName)
        {
            SchemaName = schemaName;
        }
    }

    /// <summary>
    /// Overrides the default column name for a property
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class SqlColumnAttribute : Attribute
    {
        public string ColumnName { get; set; }

        public SqlColumnAttribute(string columnName)
        {
            ColumnName = columnName;
        }
    }

    /// <summary>
    /// Excludes a property from schema generation
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class)]
    public class SqlIgnoreAttribute : Attribute
    {
        public string? Reason { get; set; }

        public SqlIgnoreAttribute()
        {
        }

        public SqlIgnoreAttribute(string reason)
        {
            Reason = reason;
        }
    }
}
