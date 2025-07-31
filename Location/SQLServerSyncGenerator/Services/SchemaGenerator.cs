using Location.Core.Helpers.CodeGenerationAttributes;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using SQLServerSyncGenerator.Attributes;
using SQLServerSyncGenerator.Models;
using System.Reflection;
using System.Text;

namespace SQLServerSyncGenerator.Services;

public class SchemaGenerator
{
    private readonly ILogger<SchemaGenerator> _logger;

    public SchemaGenerator(ILogger<SchemaGenerator> logger)
    {
        _logger = logger;
    }

    public List<string> GenerateCreateTableStatements(List<EntityMetadata> entities)
    {
        var statements = new List<string>();

        _logger.LogDebug("Generating DDL statements for {Count} entities", entities.Count);

        // Generate schema creation statements first
        var schemas = entities.Select(e => e.Schema).Distinct().ToList();
        foreach (var schema in schemas)
        {
            statements.Add(GenerateCreateSchemaStatement(schema));
        }

        // Generate table creation statements
        foreach (var entity in entities)
        {
            if (entity.IsIgnored)
            {
                _logger.LogDebug("Skipping ignored entity: {EntityName}", entity.Name);
                continue;
            }

            var createTableStatement = GenerateCreateTableStatement(entity);
            statements.Add(createTableStatement);

            // Generate single-column indexes
            var indexStatements = GenerateSingleColumnIndexStatements(entity);
            statements.AddRange(indexStatements);

            // Generate composite indexes
            var compositeIndexStatements = GenerateCompositeIndexStatements(entity);
            statements.AddRange(compositeIndexStatements);

            // Generate foreign key constraints
            var foreignKeyStatements = GenerateForeignKeyStatements(entity);
            statements.AddRange(foreignKeyStatements);
        }

        _logger.LogInformation("Generated {Count} DDL statements", statements.Count);
        return statements;
    }

    public async Task<List<string>> GenerateDeltaDDLAsync(List<EntityMetadata> entities, string connectionString)
    {
        var deltaStatements = new List<string>();

        _logger.LogDebug("Analyzing existing database schema for delta generation");

        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        // Get existing schemas, tables, columns, indexes, and foreign keys
        var existingSchemas = await GetExistingSchemasAsync(connection);
        var existingTables = await GetExistingTablesAsync(connection);
        var existingColumns = await GetExistingColumnsAsync(connection);
        var existingIndexes = await GetExistingIndexesAsync(connection);
        var existingForeignKeys = await GetExistingForeignKeysAsync(connection);

        _logger.LogDebug("Found {SchemaCount} schemas, {TableCount} tables, {ColumnCount} columns, {IndexCount} indexes, {FKCount} foreign keys",
            existingSchemas.Count, existingTables.Count, existingColumns.Count, existingIndexes.Count, existingForeignKeys.Count);

        // Generate schema creation statements for missing schemas
        var requiredSchemas = entities.Select(e => e.Schema).Distinct().ToList();
        foreach (var schema in requiredSchemas)
        {
            if (!existingSchemas.Contains(schema))
            {
                deltaStatements.Add(GenerateCreateSchemaStatement(schema));
                _logger.LogDebug("Schema {Schema} needs to be created", schema);
            }
        }

        // Generate table and related DDL for each entity
        foreach (var entity in entities)
        {
            if (entity.IsIgnored)
            {
                _logger.LogDebug("Skipping ignored entity: {EntityName}", entity.Name);
                continue;
            }

            var tableKey = $"{entity.Schema}.{entity.TableName}";

            // Check if table exists
            if (!existingTables.ContainsKey(tableKey))
            {
                // Table doesn't exist - generate full CREATE TABLE statement
                var createTableStatement = GenerateCreateTableStatement(entity);
                deltaStatements.Add(createTableStatement);
                _logger.LogDebug("Table {Table} needs to be created", tableKey);

                // Add all indexes and foreign keys since table is new
                var indexStatements = GenerateSingleColumnIndexStatements(entity);
                deltaStatements.AddRange(indexStatements);

                var compositeIndexStatements = GenerateCompositeIndexStatements(entity);
                deltaStatements.AddRange(compositeIndexStatements);

                var foreignKeyStatements = GenerateForeignKeyStatements(entity);
                deltaStatements.AddRange(foreignKeyStatements);
            }
            else
            {
                // Table exists - check for missing columns
                var tableColumns = existingColumns.ContainsKey(tableKey) ? existingColumns[tableKey] : new List<string>();

                foreach (var property in entity.Properties.Where(p => !p.IsIgnored))
                {
                    if (!tableColumns.Contains(property.ColumnName))
                    {
                        var addColumnStatement = GenerateAddColumnStatement(entity, property);
                        deltaStatements.Add(addColumnStatement);
                        _logger.LogDebug("Column {Table}.{Column} needs to be added", tableKey, property.ColumnName);
                    }
                }

                // Check for missing indexes
                var tableIndexes = existingIndexes.ContainsKey(tableKey) ? existingIndexes[tableKey] : new List<string>();

                // Single column indexes
                var indexedProperties = entity.Properties
                    .Where(p => p.IndexType.HasValue && p.IndexType != SqlIndexType.None && string.IsNullOrEmpty(p.IndexGroup))
                    .ToList();

                foreach (var property in indexedProperties)
                {
                    var indexName = property.IndexName ?? $"IX_{entity.TableName}_{property.ColumnName}";
                    if (!tableIndexes.Contains(indexName))
                    {
                        var indexStatements = GenerateSingleColumnIndexStatements(entity);
                        deltaStatements.AddRange(indexStatements.Where(stmt => stmt.Contains(indexName)));
                        _logger.LogDebug("Index {Index} needs to be created", indexName);
                    }
                }

                // Composite indexes
                foreach (var index in entity.CompositeIndexes)
                {
                    if (!tableIndexes.Contains(index.Name))
                    {
                        var compositeIndexStatements = GenerateCompositeIndexStatements(entity);
                        deltaStatements.AddRange(compositeIndexStatements.Where(stmt => stmt.Contains(index.Name)));
                        _logger.LogDebug("Composite index {Index} needs to be created", index.Name);
                    }
                }

                // Check for missing foreign keys
                var tableForeignKeys = existingForeignKeys.ContainsKey(tableKey) ? existingForeignKeys[tableKey] : new List<string>();

                var foreignKeyProperties = entity.Properties.Where(p => p.ForeignKey != null).ToList();
                foreach (var property in foreignKeyProperties)
                {
                    var constraintName = property.ForeignKey!.Name ?? $"FK_{entity.TableName}_{property.ColumnName}";
                    if (!tableForeignKeys.Contains(constraintName))
                    {
                        var foreignKeyStatements = GenerateForeignKeyStatements(entity);
                        deltaStatements.AddRange(foreignKeyStatements.Where(stmt => stmt.Contains(constraintName)));
                        _logger.LogDebug("Foreign key {FK} needs to be created", constraintName);
                    }
                }
            }
        }

        _logger.LogInformation("Generated {Count} delta DDL statements", deltaStatements.Count);
        return deltaStatements;
    }

    private string GenerateAddColumnStatement(EntityMetadata entity, PropertyMetadata property)
    {
        var columnDef = GenerateColumnDefinition(property);
        return $"ALTER TABLE [{entity.Schema}].[{entity.TableName}] ADD {columnDef}";
    }

    private async Task<List<string>> GetExistingSchemasAsync(SqlConnection connection)
    {
        var schemas = new List<string>();
        var query = "SELECT name FROM sys.schemas WHERE name NOT IN ('dbo', 'guest', 'INFORMATION_SCHEMA', 'sys', 'db_owner', 'db_accessadmin', 'db_securityadmin', 'db_ddladmin', 'db_backupoperator', 'db_datareader', 'db_datawriter', 'db_denydatareader', 'db_denydatawriter')";

        using var command = new SqlCommand(query, connection);
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            schemas.Add(reader.GetString(0));
        }

        return schemas;
    }

    private async Task<Dictionary<string, bool>> GetExistingTablesAsync(SqlConnection connection)
    {
        var tables = new Dictionary<string, bool>();
        var query = @"
            SELECT s.name AS SchemaName, t.name AS TableName
            FROM sys.tables t 
            INNER JOIN sys.schemas s ON t.schema_id = s.schema_id";

        using var command = new SqlCommand(query, connection);
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var schemaName = reader.GetString("SchemaName");
            var tableName = reader.GetString("TableName");
            var tableKey = $"{schemaName}.{tableName}";
            tables[tableKey] = true;
        }

        return tables;
    }

    private async Task<Dictionary<string, List<string>>> GetExistingColumnsAsync(SqlConnection connection)
    {
        var columns = new Dictionary<string, List<string>>();
        var query = @"
            SELECT s.name AS SchemaName, t.name AS TableName, c.name AS ColumnName
            FROM sys.columns c
            INNER JOIN sys.tables t ON c.object_id = t.object_id
            INNER JOIN sys.schemas s ON t.schema_id = s.schema_id";

        using var command = new SqlCommand(query, connection);
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var schemaName = reader.GetString("SchemaName");
            var tableName = reader.GetString("TableName");
            var columnName = reader.GetString("ColumnName");
            var tableKey = $"{schemaName}.{tableName}";

            if (!columns.ContainsKey(tableKey))
                columns[tableKey] = new List<string>();

            columns[tableKey].Add(columnName);
        }

        return columns;
    }

    private async Task<Dictionary<string, List<string>>> GetExistingIndexesAsync(SqlConnection connection)
    {
        var indexes = new Dictionary<string, List<string>>();
        var query = @"
            SELECT s.name AS SchemaName, t.name AS TableName, i.name AS IndexName
            FROM sys.indexes i
            INNER JOIN sys.tables t ON i.object_id = t.object_id
            INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
            WHERE i.name IS NOT NULL AND i.type > 0";

        using var command = new SqlCommand(query, connection);
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var schemaName = reader.GetString("SchemaName");
            var tableName = reader.GetString("TableName");
            var indexName = reader.GetString("IndexName");
            var tableKey = $"{schemaName}.{tableName}";

            if (!indexes.ContainsKey(tableKey))
                indexes[tableKey] = new List<string>();

            indexes[tableKey].Add(indexName);
        }

        return indexes;
    }

    private async Task<Dictionary<string, List<string>>> GetExistingForeignKeysAsync(SqlConnection connection)
    {
        var foreignKeys = new Dictionary<string, List<string>>();
        var query = @"
            SELECT s.name AS SchemaName, t.name AS TableName, fk.name AS ForeignKeyName
            FROM sys.foreign_keys fk
            INNER JOIN sys.tables t ON fk.parent_object_id = t.object_id
            INNER JOIN sys.schemas s ON t.schema_id = s.schema_id";

        using var command = new SqlCommand(query, connection);
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var schemaName = reader.GetString("SchemaName");
            var tableName = reader.GetString("TableName");
            var foreignKeyName = reader.GetString("ForeignKeyName");
            var tableKey = $"{schemaName}.{tableName}";

            if (!foreignKeys.ContainsKey(tableKey))
                foreignKeys[tableKey] = new List<string>();

            foreignKeys[tableKey].Add(foreignKeyName);
        }

        return foreignKeys;
    }

    private string GenerateCreateSchemaStatement(string schemaName)
    {
        return $"IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = '{schemaName}')\n" +
               $"BEGIN\n" +
               $"    EXEC('CREATE SCHEMA [{schemaName}]')\n" +
               $"END";
    }

    private string GenerateCreateTableStatement(EntityMetadata entity)
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

    private string GenerateColumnDefinition(PropertyMetadata property)
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

    private List<string> GenerateSingleColumnIndexStatements(EntityMetadata entity)
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

    private List<string> GenerateCompositeIndexStatements(EntityMetadata entity)
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

    private List<string> GenerateForeignKeyStatements(EntityMetadata entity)
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