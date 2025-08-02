using Location.Core.Helpers.CodeGenerationAttributes;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Text;

namespace SQLServerSyncGenerator.Services;

public class DdlStatementGenerator
{
    private readonly ILogger<DdlStatementGenerator> _logger;

    public DdlStatementGenerator(ILogger<DdlStatementGenerator> logger)
    {
        _logger = logger;
    }

    public string GenerateCreateSchemaStatement(string schemaName)
    {
        return $"IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = '{schemaName}')\n" +
               $"BEGIN\n" +
               $"    EXEC('CREATE SCHEMA [{schemaName}]')\n" +
               $"END";
    }

    public string GenerateCreateTableStatement(EntityMetadata entity)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"IF NOT EXISTS (SELECT * FROM sys.tables t INNER JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = '{entity.Schema}' AND t.name = '{entity.TableName}')");
        sb.AppendLine("BEGIN");
        sb.AppendLine($"    CREATE TABLE [{entity.Schema}].[{entity.TableName}] (");

        var columnDefinitions = new List<string>();

        foreach (var property in entity.Properties.Where(p => !p.IsIgnored))
        {
            var columnDef = GenerateColumnDefinition(property);
            columnDefinitions.Add($"        {columnDef}");
        }

        sb.AppendLine(string.Join(",\n", columnDefinitions));

        // Add primary key constraint if any
        var primaryKeyColumns = entity.Properties
            .Where(p => p.IsPrimaryKey || p.Constraints.Contains(SqlConstraint.PrimaryKey))
            .Select(p => $"[{p.ColumnName}]")
            .ToList();

        if (primaryKeyColumns.Any())
        {
            sb.AppendLine($",");
            sb.AppendLine($"        CONSTRAINT [PK_{entity.TableName}] PRIMARY KEY CLUSTERED ({string.Join(", ", primaryKeyColumns)})");
        }

        sb.AppendLine("    )");
        sb.AppendLine("END");

        return sb.ToString();
    }

    public string GenerateAddColumnStatement(EntityMetadata entity, PropertyMetadata property)
    {
        var columnDef = GenerateColumnDefinition(property);
        return $"ALTER TABLE [{entity.Schema}].[{entity.TableName}] ADD {columnDef}";
    }

    public string GenerateColumnDefinition(PropertyMetadata property)
    {
        var sb = new StringBuilder();

        sb.Append($"[{property.ColumnName}] ");
        sb.Append(GetSqlDataType(property));

        // Identity specification
        if (property.IsIdentity || property.Constraints.Contains(SqlConstraint.Identity))
        {
            sb.Append(" IDENTITY(1,1)");
        }

        // Null/Not Null
        if (!property.IsNullable || property.Constraints.Contains(SqlConstraint.NotNull))
        {
            sb.Append(" NOT NULL");
        }
        else
        {
            sb.Append(" NULL");
        }

        // Default value
        var defaultValue = GetDefaultValue(property);
        if (!string.IsNullOrEmpty(defaultValue))
        {
            sb.Append($" DEFAULT {defaultValue}");
        }

        // Unique constraint
        if (property.Constraints.Contains(SqlConstraint.Unique))
        {
            sb.Append(" UNIQUE");
        }

        return sb.ToString();
    }

    public List<string> GenerateSingleColumnIndexStatements(EntityMetadata entity)
    {
        var statements = new List<string>();

        var indexedProperties = entity.Properties
            .Where(p => p.IndexType.HasValue && p.IndexType != SqlIndexType.None && string.IsNullOrEmpty(p.IndexGroup))
            .ToList();

        foreach (var property in indexedProperties)
        {
            var indexName = property.IndexName ?? $"IX_{entity.TableName}_{property.ColumnName}";
            var unique = property.IndexType == SqlIndexType.Unique ? "UNIQUE " : "";
            var clustered = property.IndexType == SqlIndexType.Clustered ? "CLUSTERED" : "NONCLUSTERED";

            var statement = $"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID('[{entity.Schema}].[{entity.TableName}]') AND name = '{indexName}')\n" +
                           $"BEGIN\n" +
                           $"    CREATE {unique}{clustered} INDEX [{indexName}] ON [{entity.Schema}].[{entity.TableName}] ([{property.ColumnName}])\n" +
                           $"END";

            statements.Add(statement);
        }

        return statements;
    }

    public List<string> GenerateCompositeIndexStatements(EntityMetadata entity)
    {
        var statements = new List<string>();

        foreach (var index in entity.CompositeIndexes)
        {
            var unique = index.IsUnique ? "UNIQUE " : "";
            var clustered = index.Type == SqlIndexType.Clustered ? "CLUSTERED" : "NONCLUSTERED";
            var columns = string.Join(", ", index.Columns.OrderBy(c => c.Order).Select(c => $"[{c.ColumnName}]"));

            var statement = $"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID('[{entity.Schema}].[{entity.TableName}]') AND name = '{index.Name}')\n" +
                           $"BEGIN\n" +
                           $"    CREATE {unique}{clustered} INDEX [{index.Name}] ON [{entity.Schema}].[{entity.TableName}] ({columns})\n" +
                           $"END";

            statements.Add(statement);
        }

        return statements;
    }

    public List<string> GenerateForeignKeyStatements(EntityMetadata entity)
    {
        var statements = new List<string>();

        var foreignKeyProperties = entity.Properties.Where(p => p.ForeignKey != null).ToList();

        foreach (var property in foreignKeyProperties)
        {
            var fk = property.ForeignKey!;
            var constraintName = fk.Name ?? $"FK_{entity.TableName}_{property.ColumnName}";

            // Determine referenced schema and table
            var referencedSchema = DetermineReferencedSchema(fk.ReferencedEntityType);
            var referencedTable = DetermineReferencedTable(fk.ReferencedEntityType);

            var onDelete = fk.OnDelete != ForeignKeyAction.NoAction ? $" ON DELETE {FormatForeignKeyAction(fk.OnDelete)}" : "";
            var onUpdate = fk.OnUpdate != ForeignKeyAction.NoAction ? $" ON UPDATE {FormatForeignKeyAction(fk.OnUpdate)}" : "";

            var statement = $"IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID('[{constraintName}]'))\n" +
                           $"BEGIN\n" +
                           $"    ALTER TABLE [{entity.Schema}].[{entity.TableName}] ADD CONSTRAINT [{constraintName}] \n" +
                           $"        FOREIGN KEY ([{property.ColumnName}]) REFERENCES [{referencedSchema}].[{referencedTable}] ([{fk.ReferencedColumn}]){onDelete}{onUpdate}\n" +
                           $"END";

            statements.Add(statement);
        }

        return statements;
    }

    private string GetSqlDataType(PropertyMetadata property)
    {
        // If explicit SQL type is specified, use it
        if (property.SqlDataType.HasValue)
        {
            return FormatSqlDataType(property.SqlDataType.Value, property.Length, property.Precision, property.Scale);
        }

        // Auto-map from .NET type
        return MapDotNetTypeToSqlServer(property.PropertyType, property.Length);
    }

    private string FormatSqlDataType(SqlDataType dataType, int? length, int? precision, int? scale)
    {
        return dataType switch
        {
            SqlDataType.NVarChar => length.HasValue ? $"NVARCHAR({length})" : "NVARCHAR(255)",
            SqlDataType.VarChar => length.HasValue ? $"VARCHAR({length})" : "VARCHAR(255)",
            SqlDataType.NVarCharMax => "NVARCHAR(MAX)",
            SqlDataType.VarCharMax => "VARCHAR(MAX)",
            SqlDataType.Text => "TEXT",
            SqlDataType.NText => "NTEXT",
            SqlDataType.Int => "INT",
            SqlDataType.BigInt => "BIGINT",
            SqlDataType.SmallInt => "SMALLINT",
            SqlDataType.TinyInt => "TINYINT",
            SqlDataType.Decimal => precision.HasValue && scale.HasValue ? $"DECIMAL({precision},{scale})" : "DECIMAL(18,2)",
            SqlDataType.Float => "FLOAT",
            SqlDataType.Real => "REAL",
            SqlDataType.Money => "MONEY",
            SqlDataType.SmallMoney => "SMALLMONEY",
            SqlDataType.DateTime2 => "DATETIME2",
            SqlDataType.DateTime => "DATETIME",
            SqlDataType.Date => "DATE",
            SqlDataType.Time => "TIME",
            SqlDataType.SmallDateTime => "SMALLDATETIME",
            SqlDataType.DateTimeOffset => "DATETIMEOFFSET",
            SqlDataType.Bit => "BIT",
            SqlDataType.UniqueIdentifier => "UNIQUEIDENTIFIER",
            SqlDataType.Binary => length.HasValue ? $"BINARY({length})" : "BINARY(50)",
            SqlDataType.VarBinary => length.HasValue ? $"VARBINARY({length})" : "VARBINARY(255)",
            SqlDataType.VarBinaryMax => "VARBINARY(MAX)",
            SqlDataType.Image => "IMAGE",
            SqlDataType.Xml => "XML",
            SqlDataType.Geography => "GEOGRAPHY",
            SqlDataType.Geometry => "GEOMETRY",
            SqlDataType.Timestamp => "TIMESTAMP",
            _ => "NVARCHAR(255)"
        };
    }

    private string MapDotNetTypeToSqlServer(Type dotNetType, int? length)
    {
        var underlyingType = Nullable.GetUnderlyingType(dotNetType) ?? dotNetType;

        return underlyingType.Name switch
        {
            "String" => length.HasValue ? $"NVARCHAR({length})" : "NVARCHAR(255)",
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

    private string GetDefaultValue(PropertyMetadata property)
    {
        if (property.DefaultType.HasValue)
        {
            return property.DefaultType.Value switch
            {
                SqlDefaultValue.GetUtcDate => "GETUTCDATE()",
                SqlDefaultValue.GetDate => "GETDATE()",
                SqlDefaultValue.NewId => "NEWID()",
                SqlDefaultValue.Zero => "0",
                SqlDefaultValue.EmptyString => "''",
                SqlDefaultValue.Null => "NULL",
                _ => ""
            };
        }

        if (!string.IsNullOrEmpty(property.CustomDefault))
        {
            return property.CustomDefault;
        }

        return "";
    }

    private string DetermineReferencedSchema(Type referencedEntityType)
    {
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
        }

        return "dbo";
    }

    private string DetermineReferencedTable(Type referencedEntityType)
    {
        var sqlTableAttribute = referencedEntityType.GetCustomAttribute<SqlTableAttribute>();
        if (sqlTableAttribute != null)
            return sqlTableAttribute.TableName;

        var sqliteTableAttribute = referencedEntityType.GetCustomAttribute<SQLite.TableAttribute>();
        if (sqliteTableAttribute != null)
            return sqliteTableAttribute.Name;

        return referencedEntityType.Name;
    }

    private string FormatForeignKeyAction(ForeignKeyAction action)
    {
        return action switch
        {
            ForeignKeyAction.Cascade => "CASCADE",
            ForeignKeyAction.SetNull => "SET NULL",
            ForeignKeyAction.SetDefault => "SET DEFAULT",
            ForeignKeyAction.NoAction => "NO ACTION",
            _ => "NO ACTION"
        };
    }
}