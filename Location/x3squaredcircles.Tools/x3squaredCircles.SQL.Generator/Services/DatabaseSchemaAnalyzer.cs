using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

using System.Reflection;
namespace x3squaredcirecles.SQLSync.Generator.Services;


using Location.Core.Helpers.CodeGenerationAttributes;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using SQLServerSyncGenerator;
using System.Data;



public class EnhancedDatabaseSchemaAnalyzer
{
    private readonly ILogger<EnhancedDatabaseSchemaAnalyzer> _logger;

    public EnhancedDatabaseSchemaAnalyzer(ILogger<EnhancedDatabaseSchemaAnalyzer> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Gets complete column metadata for attribute comparison, not just column names
    /// </summary>
    public async Task<Dictionary<string, List<DatabaseColumnMetadata>>> GetCompleteColumnMetadataAsync(SqlConnection connection)
    {
        var columnMetadata = new Dictionary<string, List<DatabaseColumnMetadata>>();

        var query = @"
            SELECT 
                s.name AS SchemaName,
                t.name AS TableName,
                c.name AS ColumnName,
                c.column_id AS OrdinalPosition,
                typ.name AS DataType,
                c.max_length,
                c.precision,
                c.scale,
                c.is_nullable,
                c.is_identity,
                c.is_computed,
                def.definition AS DefaultDefinition,
                cc.definition AS ComputedDefinition
            FROM sys.columns c
            INNER JOIN sys.tables t ON c.object_id = t.object_id
            INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
            INNER JOIN sys.types typ ON c.user_type_id = typ.user_type_id
            LEFT JOIN sys.default_constraints def ON c.default_object_id = def.object_id
            LEFT JOIN sys.computed_columns cc ON c.object_id = cc.object_id AND c.column_id = cc.column_id
            WHERE s.name IN ('Core', 'Photography', 'Fishing', 'Hunting')
            ORDER BY s.name, t.name, c.column_id";

        using var command = new SqlCommand(query, connection);
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var schemaName = reader.GetString("SchemaName");
            var tableName = reader.GetString("TableName");
            var tableKey = $"{schemaName}.{tableName}";

            if (!columnMetadata.ContainsKey(tableKey))
                columnMetadata[tableKey] = new List<DatabaseColumnMetadata>();

            var metadata = new DatabaseColumnMetadata
            {
                SchemaName = schemaName,
                TableName = tableName,
                ColumnName = reader.GetString("ColumnName"),
                OrdinalPosition = reader.GetInt32("OrdinalPosition"),
                DataType = reader.GetString("DataType"),
                MaxLength = reader.IsDBNull("max_length") ? null : reader.GetInt16("max_length"),
                Precision = reader.IsDBNull("precision") ? null : reader.GetByte("precision"),
                Scale = reader.IsDBNull("scale") ? null : reader.GetByte("scale"),
                IsNullable = reader.GetBoolean("is_nullable"),
                IsIdentity = reader.GetBoolean("is_identity"),
                IsComputed = reader.GetBoolean("is_computed"),
                DefaultDefinition = reader.IsDBNull("DefaultDefinition") ? null : reader.GetString("DefaultDefinition"),
                ComputedDefinition = reader.IsDBNull("ComputedDefinition") ? null : reader.GetString("ComputedDefinition")
            };

            columnMetadata[tableKey].Add(metadata);
        }

        return columnMetadata;
    }

    /// <summary>
    /// Gets complete constraint metadata for change detection
    /// </summary>
    public async Task<Dictionary<string, List<DatabaseConstraintMetadata>>> GetConstraintMetadataAsync(SqlConnection connection)
    {
        var constraintMetadata = new Dictionary<string, List<DatabaseConstraintMetadata>>();

        var query = @"
            SELECT 
                s.name AS SchemaName,
                t.name AS TableName,
                con.name AS ConstraintName,
                con.type_desc AS ConstraintType,
                col.name AS ColumnName,
                cc.definition AS CheckDefinition,
                fk.delete_referential_action_desc AS DeleteAction,
                fk.update_referential_action_desc AS UpdateAction,
                rs.name AS ReferencedSchema,
                rt.name AS ReferencedTable,
                rc.name AS ReferencedColumn
            FROM sys.objects con
            INNER JOIN sys.tables t ON con.parent_object_id = t.object_id
            INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
            LEFT JOIN sys.key_constraints kc ON con.object_id = kc.object_id
            LEFT JOIN sys.index_columns ic ON kc.parent_object_id = ic.object_id AND kc.unique_index_id = ic.index_id
            LEFT JOIN sys.columns col ON ic.object_id = col.object_id AND ic.column_id = col.column_id
            LEFT JOIN sys.check_constraints cc ON con.object_id = cc.object_id
            LEFT JOIN sys.foreign_keys fk ON con.object_id = fk.object_id
            LEFT JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
            LEFT JOIN sys.columns rc ON fkc.referenced_object_id = rc.object_id AND fkc.referenced_column_id = rc.column_id
            LEFT JOIN sys.tables rt ON fkc.referenced_object_id = rt.object_id
            LEFT JOIN sys.schemas rs ON rt.schema_id = rs.schema_id
            WHERE con.type_desc IN ('PRIMARY_KEY_CONSTRAINT', 'UNIQUE_CONSTRAINT', 'FOREIGN_KEY_CONSTRAINT', 'CHECK_CONSTRAINT')
              AND s.name IN ('Core', 'Photography', 'Fishing', 'Hunting')
            ORDER BY s.name, t.name, con.name";

        using var command = new SqlCommand(query, connection);
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var schemaName = reader.GetString("SchemaName");
            var tableName = reader.GetString("TableName");
            var tableKey = $"{schemaName}.{tableName}";

            if (!constraintMetadata.ContainsKey(tableKey))
                constraintMetadata[tableKey] = new List<DatabaseConstraintMetadata>();

            var metadata = new DatabaseConstraintMetadata
            {
                SchemaName = schemaName,
                TableName = tableName,
                ConstraintName = reader.GetString("ConstraintName"),
                ConstraintType = reader.GetString("ConstraintType"),
                ColumnName = reader.IsDBNull("ColumnName") ? null : reader.GetString("ColumnName"),
                CheckDefinition = reader.IsDBNull("CheckDefinition") ? null : reader.GetString("CheckDefinition"),
                DeleteAction = reader.IsDBNull("DeleteAction") ? null : reader.GetString("DeleteAction"),
                UpdateAction = reader.IsDBNull("UpdateAction") ? null : reader.GetString("UpdateAction"),
                ReferencedSchema = reader.IsDBNull("ReferencedSchema") ? null : reader.GetString("ReferencedSchema"),
                ReferencedTable = reader.IsDBNull("ReferencedTable") ? null : reader.GetString("ReferencedTable"),
                ReferencedColumn = reader.IsDBNull("ReferencedColumn") ? null : reader.GetString("ReferencedColumn")
            };

            constraintMetadata[tableKey].Add(metadata);
        }

        return constraintMetadata;
    }

    /// <summary>
    /// Gets complete index metadata for change detection
    /// </summary>
    public async Task<Dictionary<string, List<DatabaseIndexMetadata>>> GetIndexMetadataAsync(SqlConnection connection)
    {
        var indexMetadata = new Dictionary<string, List<DatabaseIndexMetadata>>();

        var query = @"
            SELECT 
                s.name AS SchemaName,
                t.name AS TableName,
                i.name AS IndexName,
                i.type_desc AS IndexType,
                i.is_unique,
                i.is_primary_key,
                i.is_unique_constraint,
                i.filter_definition AS FilterDefinition,
                STRING_AGG(c.name, ', ') WITHIN GROUP (ORDER BY ic.key_ordinal) AS IndexedColumns
            FROM sys.indexes i
            INNER JOIN sys.tables t ON i.object_id = t.object_id
            INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
            INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
            INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
            WHERE i.name IS NOT NULL 
              AND s.name IN ('Core', 'Photography', 'Fishing', 'Hunting')
            GROUP BY s.name, t.name, i.name, i.type_desc, i.is_unique, i.is_primary_key, i.is_unique_constraint, i.filter_definition
            ORDER BY s.name, t.name, i.name";

        using var command = new SqlCommand(query, connection);
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var schemaName = reader.GetString("SchemaName");
            var tableName = reader.GetString("TableName");
            var tableKey = $"{schemaName}.{tableName}";

            if (!indexMetadata.ContainsKey(tableKey))
                indexMetadata[tableKey] = new List<DatabaseIndexMetadata>();

            var metadata = new DatabaseIndexMetadata
            {
                SchemaName = schemaName,
                TableName = tableName,
                IndexName = reader.GetString("IndexName"),
                IndexType = reader.GetString("IndexType"),
                IsUnique = reader.GetBoolean("is_unique"),
                IsPrimaryKey = reader.GetBoolean("is_primary_key"),
                IsUniqueConstraint = reader.GetBoolean("is_unique_constraint"),
                FilterDefinition = reader.IsDBNull("FilterDefinition") ? null : reader.GetString("FilterDefinition"),
                IndexedColumns = reader.GetString("IndexedColumns")
            };

            indexMetadata[tableKey].Add(metadata);
        }

        return indexMetadata;
    }
}

// Enhanced metadata models for comparison
public class DatabaseColumnMetadata
{
    public string SchemaName { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string ColumnName { get; set; } = string.Empty;
    public int OrdinalPosition { get; set; }
    public string DataType { get; set; } = string.Empty;
    public short? MaxLength { get; set; }
    public byte? Precision { get; set; }
    public byte? Scale { get; set; }
    public bool IsNullable { get; set; }
    public bool IsIdentity { get; set; }
    public bool IsComputed { get; set; }
    public string? DefaultDefinition { get; set; }
    public string? ComputedDefinition { get; set; }

    /// <summary>
    /// Compare this column with domain model property to detect changes
    /// </summary>
    public List<string> CompareWith(PropertyMetadata domainProperty)
    {
        var changes = new List<string>();

        // Compare data type
        var expectedSqlType = MapDomainPropertyToSqlType(domainProperty);
        if (!DataTypeMatches(expectedSqlType))
        {
            changes.Add($"ALTER COLUMN [{ColumnName}] {expectedSqlType}");
        }

        // Compare nullability
        var expectedNullable = domainProperty.IsNullable;
        if (IsNullable != expectedNullable)
        {
            var nullConstraint = expectedNullable ? "NULL" : "NOT NULL";
            changes.Add($"ALTER COLUMN [{ColumnName}] {expectedSqlType} {nullConstraint}");
        }

        // Compare default values
        var expectedDefault = GetExpectedDefault(domainProperty);
        if (DefaultDefinition != expectedDefault)
        {
            if (string.IsNullOrEmpty(expectedDefault))
            {
                changes.Add($"ALTER TABLE DROP CONSTRAINT FOR [{ColumnName}]"); // Drop default
            }
            else
            {
                changes.Add($"ALTER TABLE ADD CONSTRAINT DF_{TableName}_{ColumnName} DEFAULT {expectedDefault} FOR [{ColumnName}]");
            }
        }

        return changes;
    }

    private bool DataTypeMatches(string expectedType)
    {
        // Normalize both types for comparison
        var currentType = NormalizeSqlType();
        var expected = expectedType.ToUpperInvariant();
        return currentType == expected;
    }

  

    private string MapDomainPropertyToSqlType(PropertyMetadata property)
    {
        // This would use the same logic as DdlStatementGenerator.GetSqlDataType()
        // to determine what the SQL type should be based on domain property
        // Implementation depends on your attribute mapping logic
        return "NVARCHAR(255)"; // Simplified for example
    }

    private string? GetExpectedDefault(PropertyMetadata property)
    {
        return property.DefaultType switch
        {
            SqlDefaultValue.GetUtcDate => "GETUTCDATE()",
            SqlDefaultValue.GetDate => "GETDATE()",
            SqlDefaultValue.NewId => "NEWID()",
            SqlDefaultValue.Zero => "0",
            SqlDefaultValue.EmptyString => "''",
            _ => property.CustomDefault
        };
    }

    public string NormalizeSqlType()
    {
        var normalized = DataType.ToUpperInvariant();

        // Add length/precision info
        if (DataType.ToLower() is "nvarchar" or "varchar" or "char" or "nchar")
        {
            var length = MaxLength == -1 ? "MAX" : MaxLength.ToString();
            normalized += $"({length})";
        }
        else if (DataType.ToLower() == "decimal" && Precision.HasValue)
        {
            normalized += $"({Precision},{Scale ?? 0})";
        }

        return normalized;
    }
}

public class DatabaseConstraintMetadata
{
    public string SchemaName { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string ConstraintName { get; set; } = string.Empty;
    public string ConstraintType { get; set; } = string.Empty;
    public string? ColumnName { get; set; }
    public string? CheckDefinition { get; set; }
    public string? DeleteAction { get; set; }
    public string? UpdateAction { get; set; }
    public string? ReferencedSchema { get; set; }
    public string? ReferencedTable { get; set; }
    public string? ReferencedColumn { get; set; }
}

public class DatabaseIndexMetadata
{
    public string SchemaName { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string IndexName { get; set; } = string.Empty;
    public string IndexType { get; set; } = string.Empty;
    public bool IsUnique { get; set; }
    public bool IsPrimaryKey { get; set; }
    public bool IsUniqueConstraint { get; set; }
    public string? FilterDefinition { get; set; }
    public string IndexedColumns { get; set; } = string.Empty;
}