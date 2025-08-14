using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Npgsql;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using x3squaredcircles.SQLSync.Generator.Models;
using x3squaredcircles.SQLSync.Generator.Services;

namespace x3squaredcircles.SQLSync.Generator.Services
{
    public interface IDatabaseProviderFactory
    {
        Task<IDatabaseProvider> GetProviderAsync(string providerName);
    }

    public interface IDatabaseProvider
    {
        Task<DatabaseSchema> GetCurrentSchemaAsync(SqlSchemaConfiguration config);
        Task<IDbConnection> CreateConnectionAsync(SqlSchemaConfiguration config);
        Task<bool> TestConnectionAsync(SqlSchemaConfiguration config);
        string BuildConnectionString(SqlSchemaConfiguration config);
        Task ExecuteSqlAsync(string sql, SqlSchemaConfiguration config);
        Task<DataTable> ExecuteQueryAsync(string sql, SqlSchemaConfiguration config);
    }

    public class DatabaseProviderFactory : IDatabaseProviderFactory
    {
        private readonly ISqlServerProviderService _sqlServerProvider;
        private readonly IPostgreSqlProviderService _postgreSqlProvider;
        private readonly IMySqlProviderService _mySqlProvider;
        private readonly IOracleProviderService _oracleProvider;
        private readonly ISqliteProviderService _sqliteProvider;
        private readonly ILogger<DatabaseProviderFactory> _logger;

        public DatabaseProviderFactory(
            ISqlServerProviderService sqlServerProvider,
            IPostgreSqlProviderService postgreSqlProvider,
            IMySqlProviderService mySqlProvider,
            IOracleProviderService oracleProvider,
            ISqliteProviderService sqliteProvider,
            ILogger<DatabaseProviderFactory> logger)
        {
            _sqlServerProvider = sqlServerProvider;
            _postgreSqlProvider = postgreSqlProvider;
            _mySqlProvider = mySqlProvider;
            _oracleProvider = oracleProvider;
            _sqliteProvider = sqliteProvider;
            _logger = logger;
        }

        public async Task<IDatabaseProvider> GetProviderAsync(string providerName)
        {
            _logger.LogDebug("Getting database provider for: {ProviderName}", providerName);

            return providerName.ToLowerInvariant() switch
            {
                "sqlserver" => _sqlServerProvider,
                "postgresql" => _postgreSqlProvider,
                "mysql" => _mySqlProvider,
                "oracle" => _oracleProvider,
                "sqlite" => _sqliteProvider,
                _ => throw new SqlSchemaException(SqlSchemaExitCode.InvalidConfiguration,
                    $"Unsupported database provider: {providerName}")
            };
        }
    }

    // Base provider class with common functionality
    public abstract class BaseDatabaseProvider : IDatabaseProvider
    {
        protected readonly ILogger _logger;

        protected BaseDatabaseProvider(ILogger logger)
        {
            _logger = logger;
        }

        public abstract Task<DatabaseSchema> GetCurrentSchemaAsync(SqlSchemaConfiguration config);
        public abstract Task<IDbConnection> CreateConnectionAsync(SqlSchemaConfiguration config);
        public abstract string BuildConnectionString(SqlSchemaConfiguration config);

        public virtual async Task<bool> TestConnectionAsync(SqlSchemaConfiguration config)
        {
            try
            {
                using var connection = await CreateConnectionAsync(config);
                await connection.OpenAsync();
                return connection.State == ConnectionState.Open;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database connection test failed");
                return false;
            }
        }

        public virtual async Task ExecuteSqlAsync(string sql, SqlSchemaConfiguration config)
        {
            using var connection = await CreateConnectionAsync(config);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.CommandTimeout = config.Database.CommandTimeoutSeconds;

            await command.ExecuteNonQueryAsync();
        }

        public virtual async Task<DataTable> ExecuteQueryAsync(string sql, SqlSchemaConfiguration config)
        {
            using var connection = await CreateConnectionAsync(config);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.CommandTimeout = config.Database.CommandTimeoutSeconds;

            var dataTable = new DataTable();
            using var reader = await command.ExecuteReaderAsync();
            dataTable.Load(reader);

            return dataTable;
        }

        protected virtual async Task<IDbConnection> CreateConnectionWithRetryAsync(string connectionString, Func<string, IDbConnection> connectionFactory, SqlSchemaConfiguration config)
        {
            Exception lastException = null;

            for (int attempt = 1; attempt <= config.Database.RetryAttempts; attempt++)
            {
                try
                {
                    var connection = connectionFactory(connectionString);
                    return connection;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    _logger.LogWarning("Connection attempt {Attempt} failed: {Message}", attempt, ex.Message);

                    if (attempt < config.Database.RetryAttempts)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(config.Database.RetryIntervalSeconds * attempt));
                    }
                }
            }

            throw new SqlSchemaException(SqlSchemaExitCode.DatabaseConnectionFailure,
                $"Failed to create database connection after {config.Database.RetryAttempts} attempts: {lastException?.Message}", lastException);
        }
    }

    // SQL Server Provider
    public interface ISqlServerProviderService : IDatabaseProvider { }

    public class SqlServerProviderService : BaseDatabaseProvider, ISqlServerProviderService
    {
        public SqlServerProviderService(ILogger<SqlServerProviderService> logger) : base(logger) { }

        public override async Task<DatabaseSchema> GetCurrentSchemaAsync(SqlSchemaConfiguration config)
        {
            var schema = new DatabaseSchema
            {
                DatabaseName = config.Database.DatabaseName,
                Provider = "sqlserver",
                AnalysisTime = DateTime.UtcNow,
                Tables = new List<SchemaTable>(),
                Views = new List<SchemaView>(),
                Indexes = new List<SchemaIndex>(),
                Constraints = new List<SchemaConstraint>(),
                Procedures = new List<SchemaProcedure>(),
                Functions = new List<SchemaFunction>(),
                Metadata = new Dictionary<string, object>
                {
                    ["server"] = config.Database.Server,
                    ["database"] = config.Database.DatabaseName,
                    ["schema"] = config.Database.Schema ?? "dbo"
                }
            };

            await LoadTablesAsync(schema, config);
            await LoadConstraintsAsync(schema, config);
            await LoadIndexesAsync(schema, config);
            await LoadViewsAsync(schema, config);
            await LoadProceduresAsync(schema, config);
            await LoadFunctionsAsync(schema, config);

            return schema;
        }

        public override async Task<IDbConnection> CreateConnectionAsync(SqlSchemaConfiguration config)
        {
            var connectionString = BuildConnectionString(config);
            return await CreateConnectionWithRetryAsync(connectionString, cs => new SqlConnection(cs), config);
        }

        public override string BuildConnectionString(SqlSchemaConfiguration config)
        {
            if (!string.IsNullOrEmpty(config.Database.ConnectionString))
            {
                return config.Database.ConnectionString;
            }

            var builder = new SqlConnectionStringBuilder
            {
                DataSource = config.Database.Server,
                InitialCatalog = config.Database.DatabaseName,
                ConnectTimeout = config.Database.ConnectionTimeoutSeconds,
                CommandTimeout = config.Database.CommandTimeoutSeconds,
                Encrypt = config.Database.SqlServerEncrypt,
                TrustServerCertificate = config.Database.SqlServerTrustCert
            };

            if (!string.IsNullOrEmpty(config.Database.SqlServerInstance))
            {
                builder.DataSource = $"{config.Database.Server}\\{config.Database.SqlServerInstance}";
            }

            if (config.Database.UseIntegratedAuth)
            {
                builder.IntegratedSecurity = true;
            }
            else
            {
                builder.UserID = config.Database.Username;
                builder.Password = config.Database.Password;
            }

            return builder.ConnectionString;
        }

        private async Task LoadTablesAsync(DatabaseSchema schema, SqlSchemaConfiguration config)
        {
            var sql = @"
                SELECT 
                    s.name AS SchemaName,
                    t.name AS TableName,
                    c.name AS ColumnName,
                    ty.name AS DataType,
                    c.max_length,
                    c.precision,
                    c.scale,
                    c.is_nullable,
                    c.is_identity,
                    CASE WHEN pk.column_id IS NOT NULL THEN 1 ELSE 0 END AS is_primary_key,
                    dc.definition AS default_value
                FROM sys.tables t
                INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
                INNER JOIN sys.columns c ON t.object_id = c.object_id
                INNER JOIN sys.types ty ON c.user_type_id = ty.user_type_id
                LEFT JOIN (
                    SELECT ic.object_id, ic.column_id
                    FROM sys.index_columns ic
                    INNER JOIN sys.indexes i ON ic.object_id = i.object_id AND ic.index_id = i.index_id
                    WHERE i.is_primary_key = 1
                ) pk ON c.object_id = pk.object_id AND c.column_id = pk.column_id
                LEFT JOIN sys.default_constraints dc ON c.default_object_id = dc.object_id
                WHERE s.name = @SchemaName
                ORDER BY s.name, t.name, c.column_id";

            var schemaName = config.Database.Schema ?? "dbo";
            var dataTable = await ExecuteQueryWithParametersAsync(sql, config, new Dictionary<string, object>
            {
                ["@SchemaName"] = schemaName
            });

            var tablesDict = new Dictionary<string, SchemaTable>();

            foreach (DataRow row in dataTable.Rows)
            {
                var tableKey = $"{row["SchemaName"]}.{row["TableName"]}";

                if (!tablesDict.TryGetValue(tableKey, out var table))
                {
                    table = new SchemaTable
                    {
                        Name = row["TABLE_NAME"].ToString()!,
                        Schema = row["OWNER"].ToString()!,
                        Columns = new List<SchemaColumn>(),
                        Indexes = new List<SchemaIndex>(),
                        Constraints = new List<SchemaConstraint>(),
                        Metadata = new Dictionary<string, object>()
                    };
                    tablesDict[tableKey] = table;
                    schema.Tables.Add(table);
                }

                var column = new SchemaColumn
                {
                    Name = row["COLUMN_NAME"].ToString()!,
                    DataType = FormatOracleDataType(row),
                    IsNullable = row["NULLABLE"].ToString() == "Y",
                    IsPrimaryKey = row["IS_PRIMARY_KEY"].ToString() == "Y",
                    IsIdentity = false, // Oracle identity columns would need separate detection
                    MaxLength = row["DATA_LENGTH"] != DBNull.Value ? (int?)Convert.ToInt32(row["DATA_LENGTH"]) : null,
                    Precision = row["DATA_PRECISION"] != DBNull.Value ? (int?)Convert.ToInt32(row["DATA_PRECISION"]) : null,
                    Scale = row["DATA_SCALE"] != DBNull.Value ? (int?)Convert.ToInt32(row["DATA_SCALE"]) : null,
                    DefaultValue = row["DATA_DEFAULT"]?.ToString()?.Trim(),
                    Metadata = new Dictionary<string, object>
                    {
                        ["oracle_data_type"] = row["DATA_TYPE"].ToString()!
                    }
                };

                table.Columns.Add(column);
            }
        }

        private async Task LoadOracleConstraintsAsync(DatabaseSchema schema, SqlSchemaConfiguration config)
        {
            var sql = @"
                SELECT 
                    ac.OWNER,
                    ac.TABLE_NAME,
                    ac.CONSTRAINT_NAME,
                    ac.CONSTRAINT_TYPE,
                    acc.COLUMN_NAME,
                    ac.R_OWNER as REFERENCED_OWNER,
                    ac2.TABLE_NAME as REFERENCED_TABLE,
                    acc2.COLUMN_NAME as REFERENCED_COLUMN,
                    ac.DELETE_RULE,
                    ac.SEARCH_CONDITION
                FROM ALL_CONSTRAINTS ac
                LEFT JOIN ALL_CONS_COLUMNS acc ON ac.CONSTRAINT_NAME = acc.CONSTRAINT_NAME AND ac.OWNER = acc.OWNER
                LEFT JOIN ALL_CONSTRAINTS ac2 ON ac.R_CONSTRAINT_NAME = ac2.CONSTRAINT_NAME AND ac.R_OWNER = ac2.OWNER
                LEFT JOIN ALL_CONS_COLUMNS acc2 ON ac2.CONSTRAINT_NAME = acc2.CONSTRAINT_NAME AND ac2.OWNER = acc2.OWNER
                WHERE ac.OWNER = :schema_name
                ORDER BY ac.OWNER, ac.TABLE_NAME, ac.CONSTRAINT_NAME";

            var schemaName = config.Database.Username?.ToUpper() ?? "SYSTEM";
            var dataTable = await ExecuteOracleQueryAsync(sql, config, new Dictionary<string, object>
            {
                [":schema_name"] = schemaName
            });

            var constraintsDict = new Dictionary<string, SchemaConstraint>();

            foreach (DataRow row in dataTable.Rows)
            {
                var constraintKey = $"{row["OWNER"]}.{row["CONSTRAINT_NAME"]}";

                if (!constraintsDict.TryGetValue(constraintKey, out var constraint))
                {
                    constraint = new SchemaConstraint
                    {
                        Name = row["CONSTRAINT_NAME"].ToString()!,
                        Type = MapOracleConstraintType(row["CONSTRAINT_TYPE"].ToString()!),
                        TableName = row["TABLE_NAME"].ToString()!,
                        Schema = row["OWNER"].ToString()!,
                        Columns = new List<string>(),
                        ReferencedColumns = new List<string>(),
                        ReferencedTable = row["REFERENCED_TABLE"]?.ToString(),
                        ReferencedSchema = row["REFERENCED_OWNER"]?.ToString(),
                        OnDeleteAction = row["DELETE_RULE"]?.ToString() ?? "NO ACTION",
                        OnUpdateAction = "NO ACTION", // Oracle doesn't support ON UPDATE
                        CheckExpression = row["SEARCH_CONDITION"]?.ToString(),
                        Metadata = new Dictionary<string, object>()
                    };
                    constraintsDict[constraintKey] = constraint;
                    schema.Constraints.Add(constraint);
                }

                if (row["COLUMN_NAME"] != DBNull.Value && !string.IsNullOrEmpty(row["COLUMN_NAME"].ToString()))
                {
                    var columnName = row["COLUMN_NAME"].ToString()!;
                    if (!constraint.Columns.Contains(columnName))
                    {
                        constraint.Columns.Add(columnName);
                    }
                }

                if (row["REFERENCED_COLUMN"] != DBNull.Value && !string.IsNullOrEmpty(row["REFERENCED_COLUMN"].ToString()))
                {
                    var refColumnName = row["REFERENCED_COLUMN"].ToString()!;
                    if (!constraint.ReferencedColumns.Contains(refColumnName))
                    {
                        constraint.ReferencedColumns.Add(refColumnName);
                    }
                }
            }
        }

        private async Task LoadOracleIndexesAsync(DatabaseSchema schema, SqlSchemaConfiguration config)
        {
            var sql = @"
                SELECT 
                    ai.OWNER,
                    ai.TABLE_NAME,
                    ai.INDEX_NAME,
                    ai.UNIQUENESS,
                    aic.COLUMN_NAME,
                    aic.COLUMN_POSITION
                FROM ALL_INDEXES ai
                LEFT JOIN ALL_IND_COLUMNS aic ON ai.INDEX_NAME = aic.INDEX_NAME AND ai.OWNER = aic.INDEX_OWNER
                WHERE ai.OWNER = :schema_name
                AND ai.INDEX_TYPE = 'NORMAL'
                AND NOT EXISTS (
                    SELECT 1 FROM ALL_CONSTRAINTS ac 
                    WHERE ac.INDEX_NAME = ai.INDEX_NAME AND ac.OWNER = ai.OWNER
                )
                ORDER BY ai.OWNER, ai.TABLE_NAME, ai.INDEX_NAME, aic.COLUMN_POSITION";

            var schemaName = config.Database.Username?.ToUpper() ?? "SYSTEM";
            var dataTable = await ExecuteOracleQueryAsync(sql, config, new Dictionary<string, object>
            {
                [":schema_name"] = schemaName
            });

            var indexesDict = new Dictionary<string, SchemaIndex>();

            foreach (DataRow row in dataTable.Rows)
            {
                var indexKey = $"{row["OWNER"]}.{row["INDEX_NAME"]}";

                if (!indexesDict.TryGetValue(indexKey, out var index))
                {
                    index = new SchemaIndex
                    {
                        Name = row["INDEX_NAME"].ToString()!,
                        TableName = row["TABLE_NAME"].ToString()!,
                        Schema = row["OWNER"].ToString()!,
                        Columns = new List<string>(),
                        IsUnique = row["UNIQUENESS"].ToString() == "UNIQUE",
                        IsClustered = false, // Oracle doesn't have clustered indexes
                        Metadata = new Dictionary<string, object>()
                    };
                    indexesDict[indexKey] = index;
                    schema.Indexes.Add(index);
                }

                if (row["COLUMN_NAME"] != DBNull.Value)
                {
                    index.Columns.Add(row["COLUMN_NAME"].ToString()!);
                }
            }
        }

        private async Task LoadOracleViewsAsync(DatabaseSchema schema, SqlSchemaConfiguration config)
        {
            var sql = @"
                SELECT 
                    OWNER,
                    VIEW_NAME,
                    TEXT as VIEW_DEFINITION
                FROM ALL_VIEWS
                WHERE OWNER = :schema_name
                ORDER BY OWNER, VIEW_NAME";

            var schemaName = config.Database.Username?.ToUpper() ?? "SYSTEM";
            var dataTable = await ExecuteOracleQueryAsync(sql, config, new Dictionary<string, object>
            {
                [":schema_name"] = schemaName
            });

            foreach (DataRow row in dataTable.Rows)
            {
                var view = new SchemaView
                {
                    Name = row["VIEW_NAME"].ToString()!,
                    Schema = row["OWNER"].ToString()!,
                    Definition = row["VIEW_DEFINITION"]?.ToString() ?? string.Empty,
                    Columns = new List<SchemaColumn>(),
                    Metadata = new Dictionary<string, object>()
                };

                schema.Views.Add(view);
            }
        }

        private async Task LoadOracleProceduresAsync(DatabaseSchema schema, SqlSchemaConfiguration config)
        {
            var sql = @"
                SELECT 
                    OWNER,
                    OBJECT_NAME as PROCEDURE_NAME,
                    'PROCEDURE' as OBJECT_TYPE
                FROM ALL_OBJECTS
                WHERE OWNER = :schema_name AND OBJECT_TYPE = 'PROCEDURE'
                ORDER BY OWNER, OBJECT_NAME";

            var schemaName = config.Database.Username?.ToUpper() ?? "SYSTEM";
            var dataTable = await ExecuteOracleQueryAsync(sql, config, new Dictionary<string, object>
            {
                [":schema_name"] = schemaName
            });

            foreach (DataRow row in dataTable.Rows)
            {
                var procedure = new SchemaProcedure
                {
                    Name = row["PROCEDURE_NAME"].ToString()!,
                    Schema = row["OWNER"].ToString()!,
                    Definition = string.Empty, // Oracle procedure definitions would require DBMS_METADATA
                    Parameters = new List<SchemaParameter>(),
                    Metadata = new Dictionary<string, object>()
                };

                schema.Procedures.Add(procedure);
            }
        }

        private async Task LoadOracleFunctionsAsync(DatabaseSchema schema, SqlSchemaConfiguration config)
        {
            var sql = @"
                SELECT 
                    OWNER,
                    OBJECT_NAME as FUNCTION_NAME,
                    'FUNCTION' as OBJECT_TYPE
                FROM ALL_OBJECTS
                WHERE OWNER = :schema_name AND OBJECT_TYPE = 'FUNCTION'
                ORDER BY OWNER, OBJECT_NAME";

            var schemaName = config.Database.Username?.ToUpper() ?? "SYSTEM";
            var dataTable = await ExecuteOracleQueryAsync(sql, config, new Dictionary<string, object>
            {
                [":schema_name"] = schemaName
            });

            foreach (DataRow row in dataTable.Rows)
            {
                var function = new SchemaFunction
                {
                    Name = row["FUNCTION_NAME"].ToString()!,
                    Schema = row["OWNER"].ToString()!,
                    Definition = string.Empty, // Oracle function definitions would require DBMS_METADATA
                    ReturnType = "unknown", // Would need to query ALL_ARGUMENTS for return type
                    Parameters = new List<SchemaParameter>(),
                    Metadata = new Dictionary<string, object>()
                };

                schema.Functions.Add(function);
            }
        }

        private async Task<DataTable> ExecuteOracleQueryAsync(string sql, SqlSchemaConfiguration config, Dictionary<string, object> parameters)
        {
            using var connection = await CreateConnectionAsync(config);
            await connection.OpenAsync();

            using var command = new OracleCommand(sql, (OracleConnection)connection);
            command.CommandTimeout = config.Database.CommandTimeoutSeconds;

            foreach (var param in parameters)
            {
                command.Parameters.Add(param.Key, param.Value);
            }

            var dataTable = new DataTable();
            using var reader = await command.ExecuteReaderAsync();
            dataTable.Load(reader);

            return dataTable;
        }

        private string FormatOracleDataType(DataRow row)
        {
            var dataType = row["DATA_TYPE"].ToString()!;
            var dataLength = row["DATA_LENGTH"] != DBNull.Value ? Convert.ToInt32(row["DATA_LENGTH"]) : 0;
            var precision = row["DATA_PRECISION"] != DBNull.Value ? Convert.ToInt32(row["DATA_PRECISION"]) : 0;
            var scale = row["DATA_SCALE"] != DBNull.Value ? Convert.ToInt32(row["DATA_SCALE"]) : 0;

            return dataType.ToUpperInvariant() switch
            {
                "VARCHAR2" or "CHAR" or "NVARCHAR2" or "NCHAR" => dataLength > 0 ? $"{dataType}({dataLength})" : dataType,
                "NUMBER" => precision > 0 && scale >= 0 ? $"{dataType}({precision},{scale})" :
                           precision > 0 ? $"{dataType}({precision})" : dataType,
                _ => dataType
            };
        }

        private string MapOracleConstraintType(string type)
        {
            return type switch
            {
                "P" => "PK",
                "U" => "UQ",
                "R" => "FK",
                "C" => "CK",
                _ => type
            };
        }
    }

    // SQLite Provider
    public interface ISqliteProviderService : IDatabaseProvider { }
    public class SqliteProviderService : BaseDatabaseProvider, ISqliteProviderService
    {
        public SqliteProviderService(ILogger<SqliteProviderService> logger) : base(logger) { }

        public override async Task<DatabaseSchema> GetCurrentSchemaAsync(SqlSchemaConfiguration config)
        {
            var schema = new DatabaseSchema
            {
                DatabaseName = config.Database.DatabaseName,
                Provider = "sqlite",
                AnalysisTime = DateTime.UtcNow,
                Tables = new List<SchemaTable>(),
                Views = new List<SchemaView>(),
                Indexes = new List<SchemaIndex>(),
                Constraints = new List<SchemaConstraint>(),
                Procedures = new List<SchemaProcedure>(),
                Functions = new List<SchemaFunction>(),
                Metadata = new Dictionary<string, object>
                {
                    ["file_path"] = config.Database.SqliteFilePath ?? ":memory:",
                    ["journal_mode"] = config.Database.SqliteJournalMode,
                    ["synchronous"] = config.Database.SqliteSynchronous
                }
            };

            await LoadSqliteTablesAsync(schema, config);
            await LoadSqliteIndexesAsync(schema, config);
            await LoadSqliteViewsAsync(schema, config);

            return schema;
        }

        public override async Task<IDbConnection> CreateConnectionAsync(SqlSchemaConfiguration config)
        {
            var connectionString = BuildConnectionString(config);
            return await CreateConnectionWithRetryAsync(connectionString, cs => new SqliteConnection(cs), config);
        }

        public override string BuildConnectionString(SqlSchemaConfiguration config)
        {
            if (!string.IsNullOrEmpty(config.Database.ConnectionString))
            {
                return config.Database.ConnectionString;
            }

            var builder = new SqliteConnectionStringBuilder
            {
                DataSource = config.Database.SqliteFilePath ?? ":memory:",
                Mode = SqliteOpenMode.ReadWriteCreate,
                Cache = SqliteCacheMode.Shared
            };

            return builder.ConnectionString;
        }

        private async Task LoadSqliteTablesAsync(DatabaseSchema schema, SqlSchemaConfiguration config)
        {
            // Get table names
            var tablesSql = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'";
            var tablesDataTable = await ExecuteQueryAsync(tablesSql, config);

            foreach (DataRow tableRow in tablesDataTable.Rows)
            {
                var tableName = tableRow["name"].ToString()!;

                var table = new SchemaTable
                {
                    Name = tableName,
                    Schema = string.Empty, // SQLite doesn't have schemas
                    Columns = new List<SchemaColumn>(),
                    Indexes = new List<SchemaIndex>(),
                    Constraints = new List<SchemaConstraint>(),
                    Metadata = new Dictionary<string, object>()
                };

                // Get column information
                var columnsSql = $"PRAGMA table_info('{tableName}')";
                var columnsDataTable = await ExecuteQueryAsync(columnsSql, config);

                foreach (DataRow columnRow in columnsDataTable.Rows)
                {
                    var column = new SchemaColumn
                    {
                        Name = columnRow["name"].ToString()!,
                        DataType = columnRow["type"].ToString()!,
                        IsNullable = Convert.ToInt64(columnRow["notnull"]) == 0,
                        IsPrimaryKey = Convert.ToInt64(columnRow["pk"]) > 0,
                        IsIdentity = false, // Would need to check if AUTOINCREMENT
                        DefaultValue = columnRow["dflt_value"]?.ToString(),
                        Metadata = new Dictionary<string, object>
                        {
                            ["sqlite_affinity"] = GetSqliteAffinity(columnRow["type"].ToString()!)
                        }
                    };

                    table.Columns.Add(column);
                }

                // Get foreign key information
                var foreignKeysSql = $"PRAGMA foreign_key_list('{tableName}')";
                var foreignKeysDataTable = await ExecuteQueryAsync(foreignKeysSql, config);

                var foreignKeyConstraints = new Dictionary<string, SchemaConstraint>();

                foreach (DataRow fkRow in foreignKeysDataTable.Rows)
                {
                    var constraintName = $"FK_{tableName}_{fkRow["table"]}";

                    if (!foreignKeyConstraints.TryGetValue(constraintName, out var constraint))
                    {
                        constraint = new SchemaConstraint
                        {
                            Name = constraintName,
                            Type = "FK",
                            TableName = tableName,
                            Schema = string.Empty,
                            Columns = new List<string>(),
                            ReferencedColumns = new List<string>(),
                            ReferencedTable = fkRow["table"].ToString(),
                            ReferencedSchema = string.Empty,
                            OnDeleteAction = fkRow["on_delete"].ToString() ?? "NO ACTION",
                            OnUpdateAction = fkRow["on_update"].ToString() ?? "NO ACTION",
                            Metadata = new Dictionary<string, object>()
                        };
                        foreignKeyConstraints[constraintName] = constraint;
                        schema.Constraints.Add(constraint);
                    }

                    constraint.Columns.Add(fkRow["from"].ToString()!);
                    constraint.ReferencedColumns.Add(fkRow["to"].ToString()!);
                }

                schema.Tables.Add(table);
            }
        }

        private async Task LoadSqliteIndexesAsync(DatabaseSchema schema, SqlSchemaConfiguration config)
        {
            var sql = @"
                SELECT 
                    name as index_name,
                    tbl_name as table_name,
                    sql as index_sql
                FROM sqlite_master 
                WHERE type='index' AND name NOT LIKE 'sqlite_%'
                ORDER BY tbl_name, name";

            var dataTable = await ExecuteQueryAsync(sql, config);

            foreach (DataRow row in dataTable.Rows)
            {
                var indexName = row["index_name"].ToString()!;
                var tableName = row["table_name"].ToString()!;
                var indexSql = row["index_sql"]?.ToString() ?? string.Empty;

                // Get index columns
                var indexInfoSql = $"PRAGMA index_info('{indexName}')";
                var indexInfoTable = await ExecuteQueryAsync(indexInfoSql, config);

                var columns = new List<string>();
                foreach (DataRow colRow in indexInfoTable.Rows)
                {
                    columns.Add(colRow["name"].ToString()!);
                }

                var index = new SchemaIndex
                {
                    Name = indexName,
                    TableName = tableName,
                    Schema = string.Empty,
                    Columns = columns,
                    IsUnique = indexSql.ToUpperInvariant().Contains("UNIQUE"),
                    IsClustered = false, // SQLite doesn't have clustered indexes
                    Metadata = new Dictionary<string, object>
                    {
                        ["sql"] = indexSql
                    }
                };

                schema.Indexes.Add(index);
            }
        }

        private async Task LoadSqliteViewsAsync(DatabaseSchema schema, SqlSchemaConfiguration config)
        {
            var sql = @"
                SELECT 
                    name as view_name,
                    sql as view_sql
                FROM sqlite_master 
                WHERE type='view'
                ORDER BY name";

            var dataTable = await ExecuteQueryAsync(sql, config);

            foreach (DataRow row in dataTable.Rows)
            {
                var view = new SchemaView
                {
                    Name = row["view_name"].ToString()!,
                    Schema = string.Empty,
                    Definition = row["view_sql"]?.ToString() ?? string.Empty,
                    Columns = new List<SchemaColumn>(),
                    Metadata = new Dictionary<string, object>()
                };

                schema.Views.Add(view);
            }
        }

        private string GetSqliteAffinity(string declaredType)
        {
            var type = declaredType.ToUpperInvariant();

            if (type.Contains("INT"))
                return "INTEGER";
            if (type.Contains("CHAR") || type.Contains("CLOB") || type.Contains("TEXT"))
                return "TEXT";
            if (type.Contains("BLOB") || string.IsNullOrEmpty(type))
                return "BLOB";
            if (type.Contains("REAL") || type.Contains("FLOA") || type.Contains("DOUB"))
                return "REAL";

            return "NUMERIC";
        }
    }
}
(tableKey, out var table))
                {
                    table = new SchemaTable
                            {
                                Name = row["TableName"].ToString()!,
                                Schema = row["SchemaName"].ToString()!,
                                Columns = new List<SchemaColumn>(),
                                Indexes = new List<SchemaIndex>(),
                                Constraints = new List<SchemaConstraint>(),
                                Metadata = new Dictionary<string, object>()
                            };
tablesDict[tableKey] = table;
schema.Tables.Add(table);
                }

                var column = new SchemaColumn
                {
                    Name = row["ColumnName"].ToString()!,
                    DataType = FormatSqlServerDataType(row),
                    IsNullable = (bool)row["is_nullable"],
                    IsPrimaryKey = Convert.ToBoolean(row["is_primary_key"]),
                    IsIdentity = (bool)row["is_identity"],
                    MaxLength = row["max_length"] != DBNull.Value ? (int?)Convert.ToInt32(row["max_length"]) : null,
                    Precision = row["precision"] != DBNull.Value ? (int?)Convert.ToInt32(row["precision"]) : null,
                    Scale = row["scale"] != DBNull.Value ? (int?)Convert.ToInt32(row["scale"]) : null,
                    DefaultValue = row["default_value"]?.ToString(),
                    Metadata = new Dictionary<string, object>
                    {
                        ["system_type"] = row["DataType"].ToString()!
                    }
                };

table.Columns.Add(column);
            }
        }

        private async Task LoadConstraintsAsync(DatabaseSchema schema, SqlSchemaConfiguration config)
{
    var sql = @"
                SELECT 
                    s.name AS SchemaName,
                    t.name AS TableName,
                    c.name AS ConstraintName,
                    c.type AS ConstraintType,
                    col.name AS ColumnName,
                    ref_s.name AS ReferencedSchema,
                    ref_t.name AS ReferencedTable,
                    ref_col.name AS ReferencedColumn,
                    fk.delete_referential_action_desc AS DeleteAction,
                    fk.update_referential_action_desc AS UpdateAction,
                    cc.definition AS CheckDefinition
                FROM sys.objects c
                INNER JOIN sys.schemas s ON c.schema_id = s.schema_id
                INNER JOIN sys.tables t ON c.parent_object_id = t.object_id
                LEFT JOIN sys.key_constraints kc ON c.object_id = kc.object_id
                LEFT JOIN sys.foreign_keys fk ON c.object_id = fk.object_id
                LEFT JOIN sys.check_constraints cc ON c.object_id = cc.object_id
                LEFT JOIN sys.index_columns ic ON kc.unique_index_id = ic.index_id AND ic.object_id = t.object_id
                LEFT JOIN sys.columns col ON ic.column_id = col.column_id AND ic.object_id = col.object_id
                LEFT JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
                LEFT JOIN sys.columns ref_col ON fkc.referenced_column_id = ref_col.column_id AND fkc.referenced_object_id = ref_col.object_id
                LEFT JOIN sys.tables ref_t ON fkc.referenced_object_id = ref_t.object_id
                LEFT JOIN sys.schemas ref_s ON ref_t.schema_id = ref_s.schema_id
                WHERE c.type IN ('PK', 'UQ', 'F', 'C') AND s.name = @SchemaName
                ORDER BY s.name, t.name, c.name";

    var schemaName = config.Database.Schema ?? "dbo";
    var dataTable = await ExecuteQueryWithParametersAsync(sql, config, new Dictionary<string, object>
    {
        ["@SchemaName"] = schemaName
    });

    var constraintsDict = new Dictionary<string, SchemaConstraint>();

    foreach (DataRow row in dataTable.Rows)
    {
        var constraintKey = $"{row["SchemaName"]}.{row["ConstraintName"]}";

        if (!constraintsDict.TryGetValue(constraintKey, out var constraint))
        {
            constraint = new SchemaConstraint
            {
                Name = row["ConstraintName"].ToString()!,
                Type = MapSqlServerConstraintType(row["ConstraintType"].ToString()!),
                TableName = row["TableName"].ToString()!,
                Schema = row["SchemaName"].ToString()!,
                Columns = new List<string>(),
                ReferencedColumns = new List<string>(),
                ReferencedTable = row["ReferencedTable"]?.ToString(),
                ReferencedSchema = row["ReferencedSchema"]?.ToString(),
                OnDeleteAction = row["DeleteAction"]?.ToString() ?? "NO_ACTION",
                OnUpdateAction = row["UpdateAction"]?.ToString() ?? "NO_ACTION",
                CheckExpression = row["CheckDefinition"]?.ToString(),
                Metadata = new Dictionary<string, object>()
            };
            constraintsDict[constraintKey] = constraint;
            schema.Constraints.Add(constraint);
        }

        if (row["ColumnName"] != DBNull.Value && !string.IsNullOrEmpty(row["ColumnName"].ToString()))
        {
            var columnName = row["ColumnName"].ToString()!;
            if (!constraint.Columns.Contains(columnName))
            {
                constraint.Columns.Add(columnName);
            }
        }

        if (row["ReferencedColumn"] != DBNull.Value && !string.IsNullOrEmpty(row["ReferencedColumn"].ToString()))
        {
            var refColumnName = row["ReferencedColumn"].ToString()!;
            if (!constraint.ReferencedColumns.Contains(refColumnName))
            {
                constraint.ReferencedColumns.Add(refColumnName);
            }
        }
    }
}

private async Task LoadIndexesAsync(DatabaseSchema schema, SqlSchemaConfiguration config)
{
    var sql = @"
                SELECT 
                    s.name AS SchemaName,
                    t.name AS TableName,
                    i.name AS IndexName,
                    i.is_unique,
                    i.type_desc AS IndexType,
                    c.name AS ColumnName,
                    ic.key_ordinal,
                    i.filter_definition
                FROM sys.indexes i
                INNER JOIN sys.tables t ON i.object_id = t.object_id
                INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
                INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
                INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
                WHERE i.is_primary_key = 0 AND i.is_unique_constraint = 0 AND s.name = @SchemaName
                ORDER BY s.name, t.name, i.name, ic.key_ordinal";

    var schemaName = config.Database.Schema ?? "dbo";
    var dataTable = await ExecuteQueryWithParametersAsync(sql, config, new Dictionary<string, object>
    {
        ["@SchemaName"] = schemaName
    });

    var indexesDict = new Dictionary<string, SchemaIndex>();

    foreach (DataRow row in dataTable.Rows)
    {
        var indexKey = $"{row["SchemaName"]}.{row["IndexName"]}";

        if (!indexesDict.TryGetValue(indexKey, out var index))
        {
            index = new SchemaIndex
            {
                Name = row["IndexName"].ToString()!,
                TableName = row["TableName"].ToString()!,
                Schema = row["SchemaName"].ToString()!,
                Columns = new List<string>(),
                IsUnique = (bool)row["is_unique"],
                IsClustered = row["IndexType"].ToString()!.Contains("CLUSTERED"),
                FilterExpression = row["filter_definition"]?.ToString(),
                Metadata = new Dictionary<string, object>
                {
                    ["index_type"] = row["IndexType"].ToString()!
                }
            };
            indexesDict[indexKey] = index;
            schema.Indexes.Add(index);
        }

        if (row["ColumnName"] != DBNull.Value)
        {
            index.Columns.Add(row["ColumnName"].ToString()!);
        }
    }
}

private async Task LoadViewsAsync(DatabaseSchema schema, SqlSchemaConfiguration config)
{
    var sql = @"
                SELECT 
                    s.name AS SchemaName,
                    v.name AS ViewName,
                    m.definition AS ViewDefinition
                FROM sys.views v
                INNER JOIN sys.schemas s ON v.schema_id = s.schema_id
                INNER JOIN sys.sql_modules m ON v.object_id = m.object_id
                WHERE s.name = @SchemaName";

    var schemaName = config.Database.Schema ?? "dbo";
    var dataTable = await ExecuteQueryWithParametersAsync(sql, config, new Dictionary<string, object>
    {
        ["@SchemaName"] = schemaName
    });

    foreach (DataRow row in dataTable.Rows)
    {
        var view = new SchemaView
        {
            Name = row["ViewName"].ToString()!,
            Schema = row["SchemaName"].ToString()!,
            Definition = row["ViewDefinition"]?.ToString() ?? string.Empty,
            Columns = new List<SchemaColumn>(),
            Metadata = new Dictionary<string, object>()
        };

        schema.Views.Add(view);
    }
}

private async Task LoadProceduresAsync(DatabaseSchema schema, SqlSchemaConfiguration config)
{
    var sql = @"
                SELECT 
                    s.name AS SchemaName,
                    p.name AS ProcedureName,
                    m.definition AS ProcedureDefinition
                FROM sys.procedures p
                INNER JOIN sys.schemas s ON p.schema_id = s.schema_id
                INNER JOIN sys.sql_modules m ON p.object_id = m.object_id
                WHERE s.name = @SchemaName";

    var schemaName = config.Database.Schema ?? "dbo";
    var dataTable = await ExecuteQueryWithParametersAsync(sql, config, new Dictionary<string, object>
    {
        ["@SchemaName"] = schemaName
    });

    foreach (DataRow row in dataTable.Rows)
    {
        var procedure = new SchemaProcedure
        {
            Name = row["ProcedureName"].ToString()!,
            Schema = row["SchemaName"].ToString()!,
            Definition = row["ProcedureDefinition"]?.ToString() ?? string.Empty,
            Parameters = new List<SchemaParameter>(),
            Metadata = new Dictionary<string, object>()
        };

        schema.Procedures.Add(procedure);
    }
}

private async Task LoadFunctionsAsync(DatabaseSchema schema, SqlSchemaConfiguration config)
{
    var sql = @"
                SELECT 
                    s.name AS SchemaName,
                    f.name AS FunctionName,
                    m.definition AS FunctionDefinition,
                    t.name AS ReturnType
                FROM sys.objects f
                INNER JOIN sys.schemas s ON f.schema_id = s.schema_id
                INNER JOIN sys.sql_modules m ON f.object_id = m.object_id
                LEFT JOIN sys.types t ON f.type = t.user_type_id
                WHERE f.type IN ('FN', 'IF', 'TF') AND s.name = @SchemaName";

    var schemaName = config.Database.Schema ?? "dbo";
    var dataTable = await ExecuteQueryWithParametersAsync(sql, config, new Dictionary<string, object>
    {
        ["@SchemaName"] = schemaName
    });

    foreach (DataRow row in dataTable.Rows)
    {
        var function = new SchemaFunction
        {
            Name = row["FunctionName"].ToString()!,
            Schema = row["SchemaName"].ToString()!,
            Definition = row["FunctionDefinition"]?.ToString() ?? string.Empty,
            ReturnType = row["ReturnType"]?.ToString() ?? "sql_variant",
            Parameters = new List<SchemaParameter>(),
            Metadata = new Dictionary<string, object>()
        };

        schema.Functions.Add(function);
    }
}

private async Task<DataTable> ExecuteQueryWithParametersAsync(string sql, SqlSchemaConfiguration config, Dictionary<string, object> parameters)
{
    using var connection = await CreateConnectionAsync(config);
    await connection.OpenAsync();

    using var command = new SqlCommand(sql, (SqlConnection)connection);
    command.CommandTimeout = config.Database.CommandTimeoutSeconds;

    foreach (var param in parameters)
    {
        command.Parameters.AddWithValue(param.Key, param.Value);
    }

    var dataTable = new DataTable();
    using var reader = await command.ExecuteReaderAsync();
    dataTable.Load(reader);

    return dataTable;
}

private string FormatSqlServerDataType(DataRow row)
{
    var dataType = row["DataType"].ToString()!;
    var maxLength = row["max_length"] != DBNull.Value ? Convert.ToInt32(row["max_length"]) : 0;
    var precision = row["precision"] != DBNull.Value ? Convert.ToInt32(row["precision"]) : 0;
    var scale = row["scale"] != DBNull.Value ? Convert.ToInt32(row["scale"]) : 0;

    return dataType.ToUpperInvariant() switch
    {
        "VARCHAR" or "NVARCHAR" or "CHAR" or "NCHAR" => maxLength == -1 ? $"{dataType}(MAX)" : $"{dataType}({maxLength})",
        "DECIMAL" or "NUMERIC" => $"{dataType}({precision},{scale})",
        "FLOAT" => precision > 0 ? $"{dataType}({precision})" : dataType,
        _ => dataType
    };
}

private string MapSqlServerConstraintType(string type)
{
    return type switch
    {
        "PK" => "PK",
        "UQ" => "UQ",
        "F" => "FK",
        "C" => "CK",
        _ => type
    };
}
    }

    // PostgreSQL Provider
    public interface IPostgreSqlProviderService : IDatabaseProvider { }
public class PostgreSqlProviderService : BaseDatabaseProvider, IPostgreSqlProviderService
{
    public PostgreSqlProviderService(ILogger<PostgreSqlProviderService> logger) : base(logger) { }

    public override async Task<DatabaseSchema> GetCurrentSchemaAsync(SqlSchemaConfiguration config)
    {
        var schema = new DatabaseSchema
        {
            DatabaseName = config.Database.DatabaseName,
            Provider = "postgresql",
            AnalysisTime = DateTime.UtcNow,
            Tables = new List<SchemaTable>(),
            Views = new List<SchemaView>(),
            Indexes = new List<SchemaIndex>(),
            Constraints = new List<SchemaConstraint>(),
            Procedures = new List<SchemaProcedure>(),
            Functions = new List<SchemaFunction>(),
            Metadata = new Dictionary<string, object>
            {
                ["server"] = config.Database.Server,
                ["database"] = config.Database.DatabaseName,
                ["schema"] = config.Database.Schema ?? "public"
            }
        };

        await LoadPostgreSqlTablesAsync(schema, config);
        await LoadPostgreSqlConstraintsAsync(schema, config);
        await LoadPostgreSqlIndexesAsync(schema, config);
        await LoadPostgreSqlViewsAsync(schema, config);
        await LoadPostgreSqlFunctionsAsync(schema, config);

        return schema;
    }

    public override async Task<IDbConnection> CreateConnectionAsync(SqlSchemaConfiguration config)
    {
        var connectionString = BuildConnectionString(config);
        return await CreateConnectionWithRetryAsync(connectionString, cs => new NpgsqlConnection(cs), config);
    }

    public override string BuildConnectionString(SqlSchemaConfiguration config)
    {
        if (!string.IsNullOrEmpty(config.Database.ConnectionString))
        {
            return config.Database.ConnectionString;
        }

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = config.Database.Server,
            Database = config.Database.DatabaseName,
            Timeout = config.Database.ConnectionTimeoutSeconds,
            CommandTimeout = config.Database.CommandTimeoutSeconds,
            SslMode = Enum.Parse<SslMode>(config.Database.PostgreSqlSslMode, true),
            ApplicationName = config.Database.PostgreSqlApplicationName
        };

        if (config.Database.Port > 0)
        {
            builder.Port = config.Database.Port;
        }

        if (!config.Database.UseIntegratedAuth)
        {
            builder.Username = config.Database.Username;
            builder.Password = config.Database.Password;
        }

        if (!string.IsNullOrEmpty(config.Database.PostgreSqlSchemaSearchPath))
        {
            builder.SearchPath = config.Database.PostgreSqlSchemaSearchPath;
        }

        return builder.ConnectionString;
    }

    private async Task LoadPostgreSqlTablesAsync(DatabaseSchema schema, SqlSchemaConfiguration config)
    {
        var sql = @"
                SELECT 
                    t.table_schema,
                    t.table_name,
                    c.column_name,
                    c.data_type,
                    c.character_maximum_length,
                    c.numeric_precision,
                    c.numeric_scale,
                    c.is_nullable,
                    c.column_default,
                    CASE WHEN pk.column_name IS NOT NULL THEN true ELSE false END as is_primary_key,
                    CASE WHEN c.column_default LIKE 'nextval%' THEN true ELSE false END as is_identity
                FROM information_schema.tables t
                LEFT JOIN information_schema.columns c ON t.table_name = c.table_name AND t.table_schema = c.table_schema
                LEFT JOIN (
                    SELECT ku.table_name, ku.column_name, ku.table_schema
                    FROM information_schema.table_constraints tc
                    JOIN information_schema.key_column_usage ku ON tc.constraint_name = ku.constraint_name
                    WHERE tc.constraint_type = 'PRIMARY KEY'
                ) pk ON c.table_name = pk.table_name AND c.column_name = pk.column_name AND c.table_schema = pk.table_schema
                WHERE t.table_type = 'BASE TABLE' AND t.table_schema = $1
                ORDER BY t.table_schema, t.table_name, c.ordinal_position";

        var schemaName = config.Database.Schema ?? "public";
        var dataTable = await ExecutePostgreSqlQueryAsync(sql, config, schemaName);

        var tablesDict = new Dictionary<string, SchemaTable>();

        foreach (DataRow row in dataTable.Rows)
        {
            if (row["column_name"] == DBNull.Value) continue;

            var tableKey = $"{row["table_schema"]}.{row["table_name"]}";

            if (!tablesDict.TryGetValue(tableKey, out var table))
            {
                table = new SchemaTable
                {
                    Name = row["table_name"].ToString()!,
                    Schema = row["table_schema"].ToString()!,
                    Columns = new List<SchemaColumn>(),
                    Indexes = new List<SchemaIndex>(),
                    Constraints = new List<SchemaConstraint>(),
                    Metadata = new Dictionary<string, object>()
                };
                tablesDict[tableKey] = table;
                schema.Tables.Add(table);
            }

            var column = new SchemaColumn
            {
                Name = row["column_name"].ToString()!,
                DataType = FormatPostgreSqlDataType(row),
                IsNullable = row["is_nullable"].ToString() == "YES",
                IsPrimaryKey = (bool)row["is_primary_key"],
                IsIdentity = (bool)row["is_identity"],
                MaxLength = row["character_maximum_length"] != DBNull.Value ? (int?)Convert.ToInt32(row["character_maximum_length"]) : null,
                Precision = row["numeric_precision"] != DBNull.Value ? (int?)Convert.ToInt32(row["numeric_precision"]) : null,
                Scale = row["numeric_scale"] != DBNull.Value ? (int?)Convert.ToInt32(row["numeric_scale"]) : null,
                DefaultValue = row["column_default"]?.ToString(),
                Metadata = new Dictionary<string, object>
                {
                    ["pg_data_type"] = row["data_type"].ToString()!
                }
            };

            table.Columns.Add(column);
        }
    }

    private async Task LoadPostgreSqlConstraintsAsync(DatabaseSchema schema, SqlSchemaConfiguration config)
    {
        var sql = @"
                SELECT 
                    tc.table_schema,
                    tc.table_name,
                    tc.constraint_name,
                    tc.constraint_type,
                    kcu.column_name,
                    ccu.table_schema AS foreign_table_schema,
                    ccu.table_name AS foreign_table_name,
                    ccu.column_name AS foreign_column_name,
                    rc.update_rule,
                    rc.delete_rule,
                    cc.check_clause
                FROM information_schema.table_constraints tc
                LEFT JOIN information_schema.key_column_usage kcu ON tc.constraint_name = kcu.constraint_name AND tc.table_schema = kcu.table_schema
                LEFT JOIN information_schema.constraint_column_usage ccu ON tc.constraint_name = ccu.constraint_name AND tc.table_schema = ccu.table_schema
                LEFT JOIN information_schema.referential_constraints rc ON tc.constraint_name = rc.constraint_name AND tc.table_schema = rc.constraint_schema
                LEFT JOIN information_schema.check_constraints cc ON tc.constraint_name = cc.constraint_name AND tc.constraint_schema = cc.constraint_schema
                WHERE tc.table_schema = $1
                ORDER BY tc.table_schema, tc.table_name, tc.constraint_name";

        var schemaName = config.Database.Schema ?? "public";
        var dataTable = await ExecutePostgreSqlQueryAsync(sql, config, schemaName);

        var constraintsDict = new Dictionary<string, SchemaConstraint>();

        foreach (DataRow row in dataTable.Rows)
        {
            var constraintKey = $"{row["table_schema"]}.{row["constraint_name"]}";

            if (!constraintsDict.TryGetValue(constraintKey, out var constraint))
            {
                constraint = new SchemaConstraint
                {
                    Name = row["constraint_name"].ToString()!,
                    Type = MapPostgreSqlConstraintType(row["constraint_type"].ToString()!),
                    TableName = row["table_name"].ToString()!,
                    Schema = row["table_schema"].ToString()!,
                    Columns = new List<string>(),
                    ReferencedColumns = new List<string>(),
                    ReferencedTable = row["foreign_table_name"]?.ToString(),
                    ReferencedSchema = row["foreign_table_schema"]?.ToString(),
                    OnDeleteAction = row["delete_rule"]?.ToString() ?? "NO ACTION",
                    OnUpdateAction = row["update_rule"]?.ToString() ?? "NO ACTION",
                    CheckExpression = row["check_clause"]?.ToString(),
                    Metadata = new Dictionary<string, object>()
                };
                constraintsDict[constraintKey] = constraint;
                schema.Constraints.Add(constraint);
            }

            if (row["column_name"] != DBNull.Value && !string.IsNullOrEmpty(row["column_name"].ToString()))
            {
                var columnName = row["column_name"].ToString()!;
                if (!constraint.Columns.Contains(columnName))
                {
                    constraint.Columns.Add(columnName);
                }
            }

            if (row["foreign_column_name"] != DBNull.Value && !string.IsNullOrEmpty(row["foreign_column_name"].ToString()))
            {
                var refColumnName = row["foreign_column_name"].ToString()!;
                if (!constraint.ReferencedColumns.Contains(refColumnName))
                {
                    constraint.ReferencedColumns.Add(refColumnName);
                }
            }
        }
    }

    private async Task LoadPostgreSqlIndexesAsync(DatabaseSchema schema, SqlSchemaConfiguration config)
    {
        var sql = @"
                SELECT 
                    schemaname as table_schema,
                    tablename as table_name,
                    indexname as index_name,
                    indexdef as index_definition
                FROM pg_indexes
                WHERE schemaname = $1
                AND indexname NOT LIKE '%_pkey'
                ORDER BY schemaname, tablename, indexname";

        var schemaName = config.Database.Schema ?? "public";
        var dataTable = await ExecutePostgreSqlQueryAsync(sql, config, schemaName);

        foreach (DataRow row in dataTable.Rows)
        {
            var indexDef = row["index_definition"].ToString()!;
            var index = new SchemaIndex
            {
                Name = row["index_name"].ToString()!,
                TableName = row["table_name"].ToString()!,
                Schema = row["table_schema"].ToString()!,
                Columns = ExtractColumnsFromPostgreSqlIndex(indexDef),
                IsUnique = indexDef.Contains("UNIQUE"),
                IsClustered = false, // PostgreSQL doesn't have clustered indexes
                Metadata = new Dictionary<string, object>
                {
                    ["definition"] = indexDef
                }
            };

            schema.Indexes.Add(index);
        }
    }

    private async Task LoadPostgreSqlViewsAsync(DatabaseSchema schema, SqlSchemaConfiguration config)
    {
        var sql = @"
                SELECT 
                    table_schema,
                    table_name,
                    view_definition
                FROM information_schema.views
                WHERE table_schema = $1
                ORDER BY table_schema, table_name";

        var schemaName = config.Database.Schema ?? "public";
        var dataTable = await ExecutePostgreSqlQueryAsync(sql, config, schemaName);

        foreach (DataRow row in dataTable.Rows)
        {
            var view = new SchemaView
            {
                Name = row["table_name"].ToString()!,
                Schema = row["table_schema"].ToString()!,
                Definition = row["view_definition"]?.ToString() ?? string.Empty,
                Columns = new List<SchemaColumn>(),
                Metadata = new Dictionary<string, object>()
            };

            schema.Views.Add(view);
        }
    }

    private async Task LoadPostgreSqlFunctionsAsync(DatabaseSchema schema, SqlSchemaConfiguration config)
    {
        var sql = @"
                SELECT 
                    routine_schema,
                    routine_name,
                    routine_definition,
                    data_type as return_type
                FROM information_schema.routines
                WHERE routine_schema = $1
                AND routine_type = 'FUNCTION'
                ORDER BY routine_schema, routine_name";

        var schemaName = config.Database.Schema ?? "public";
        var dataTable = await ExecutePostgreSqlQueryAsync(sql, config, schemaName);

        foreach (DataRow row in dataTable.Rows)
        {
            var function = new SchemaFunction
            {
                Name = row["routine_name"].ToString()!,
                Schema = row["routine_schema"].ToString()!,
                Definition = row["routine_definition"]?.ToString() ?? string.Empty,
                ReturnType = row["return_type"]?.ToString() ?? "void",
                Parameters = new List<SchemaParameter>(),
                Metadata = new Dictionary<string, object>()
            };

            schema.Functions.Add(function);
        }
    }

    private async Task<DataTable> ExecutePostgreSqlQueryAsync(string sql, SqlSchemaConfiguration config, params object[] parameters)
    {
        using var connection = await CreateConnectionAsync(config);
        await connection.OpenAsync();

        using var command = new NpgsqlCommand(sql, (NpgsqlConnection)connection);
        command.CommandTimeout = config.Database.CommandTimeoutSeconds;

        for (int i = 0; i < parameters.Length; i++)
        {
            command.Parameters.AddWithValue(parameters[i]);
        }

        var dataTable = new DataTable();
        using var reader = await command.ExecuteReaderAsync();
        dataTable.Load(reader);

        return dataTable;
    }

    private string FormatPostgreSqlDataType(DataRow row)
    {
        var dataType = row["data_type"].ToString()!;
        var maxLength = row["character_maximum_length"] != DBNull.Value ? Convert.ToInt32(row["character_maximum_length"]) : 0;
        var precision = row["numeric_precision"] != DBNull.Value ? Convert.ToInt32(row["numeric_precision"]) : 0;
        var scale = row["numeric_scale"] != DBNull.Value ? Convert.ToInt32(row["numeric_scale"]) : 0;

        return dataType.ToLowerInvariant() switch
        {
            "character varying" => maxLength > 0 ? $"varchar({maxLength})" : "varchar",
            "character" => maxLength > 0 ? $"char({maxLength})" : "char",
            "numeric" => precision > 0 && scale > 0 ? $"numeric({precision},{scale})" : "numeric",
            _ => dataType
        };
    }

    private string MapPostgreSqlConstraintType(string type)
    {
        return type switch
        {
            "PRIMARY KEY" => "PK",
            "UNIQUE" => "UQ",
            "FOREIGN KEY" => "FK",
            "CHECK" => "CK",
            _ => type
        };
    }

    private List<string> ExtractColumnsFromPostgreSqlIndex(string indexDef)
    {
        var columns = new List<string>();
        var start = indexDef.IndexOf('(');
        var end = indexDef.IndexOf(')', start);
        if (start > 0 && end > start)
        {
            var columnsPart = indexDef.Substring(start + 1, end - start - 1);
            columns.AddRange(columnsPart.Split(',').Select(c => c.Trim()));
        }
        return columns;
    }
}

// MySQL Provider
public interface IMySqlProviderService : IDatabaseProvider { }
public class MySqlProviderService : BaseDatabaseProvider, IMySqlProviderService
{
    public MySqlProviderService(ILogger<MySqlProviderService> logger) : base(logger) { }

    public override async Task<DatabaseSchema> GetCurrentSchemaAsync(SqlSchemaConfiguration config)
    {
        var schema = new DatabaseSchema
        {
            DatabaseName = config.Database.DatabaseName,
            Provider = "mysql",
            AnalysisTime = DateTime.UtcNow,
            Tables = new List<SchemaTable>(),
            Views = new List<SchemaView>(),
            Indexes = new List<SchemaIndex>(),
            Constraints = new List<SchemaConstraint>(),
            Procedures = new List<SchemaProcedure>(),
            Functions = new List<SchemaFunction>(),
            Metadata = new Dictionary<string, object>
            {
                ["server"] = config.Database.Server,
                ["database"] = config.Database.DatabaseName
            }
        };

        await LoadMySqlTablesAsync(schema, config);
        await LoadMySqlConstraintsAsync(schema, config);
        await LoadMySqlIndexesAsync(schema, config);
        await LoadMySqlViewsAsync(schema, config);
        await LoadMySqlProceduresAsync(schema, config);
        await LoadMySqlFunctionsAsync(schema, config);

        return schema;
    }

    public override async Task<IDbConnection> CreateConnectionAsync(SqlSchemaConfiguration config)
    {
        var connectionString = BuildConnectionString(config);
        return await CreateConnectionWithRetryAsync(connectionString, cs => new MySqlConnection(cs), config);
    }

    public override string BuildConnectionString(SqlSchemaConfiguration config)
    {
        if (!string.IsNullOrEmpty(config.Database.ConnectionString))
        {
            return config.Database.ConnectionString;
        }

        var builder = new MySqlConnectionStringBuilder
        {
            Server = config.Database.Server,
            Database = config.Database.DatabaseName,
            ConnectionTimeout = (uint)config.Database.ConnectionTimeoutSeconds,
            DefaultCommandTimeout = (uint)config.Database.CommandTimeoutSeconds,
            SslMode = Enum.Parse<MySqlSslMode>(config.Database.MySqlSslMode, true),
            CharacterSet = config.Database.MySqlCharset
        };

        if (config.Database.Port > 0)
        {
            builder.Port = (uint)config.Database.Port;
        }

        if (!config.Database.UseIntegratedAuth)
        {
            builder.UserID = config.Database.Username;
            builder.Password = config.Database.Password;
        }

        return builder.ConnectionString;
    }

    private async Task LoadMySqlTablesAsync(DatabaseSchema schema, SqlSchemaConfiguration config)
    {
        var sql = @"
                SELECT 
                    t.TABLE_SCHEMA,
                    t.TABLE_NAME,
                    c.COLUMN_NAME,
                    c.DATA_TYPE,
                    c.CHARACTER_MAXIMUM_LENGTH,
                    c.NUMERIC_PRECISION,
                    c.NUMERIC_SCALE,
                    c.IS_NULLABLE,
                    c.COLUMN_DEFAULT,
                    c.COLUMN_KEY,
                    c.EXTRA
                FROM INFORMATION_SCHEMA.TABLES t
                LEFT JOIN INFORMATION_SCHEMA.COLUMNS c ON t.TABLE_NAME = c.TABLE_NAME AND t.TABLE_SCHEMA = c.TABLE_SCHEMA
                WHERE t.TABLE_TYPE = 'BASE TABLE' AND t.TABLE_SCHEMA = ?
                ORDER BY t.TABLE_SCHEMA, t.TABLE_NAME, c.ORDINAL_POSITION";

        var dataTable = await ExecuteMySqlQueryAsync(sql, config, config.Database.DatabaseName);

        var tablesDict = new Dictionary<string, SchemaTable>();

        foreach (DataRow row in dataTable.Rows)
        {
            if (row["COLUMN_NAME"] == DBNull.Value) continue;

            var tableKey = $"{row["TABLE_SCHEMA"]}.{row["TABLE_NAME"]}";

            if (!tablesDict.TryGetValue(tableKey, out var table))
            {
                table = new SchemaTable
                {
                    Name = row["TABLE_NAME"].ToString()!,
                    Schema = row["TABLE_SCHEMA"].ToString()!,
                    Columns = new List<SchemaColumn>(),
                    Indexes = new List<SchemaIndex>(),
                    Constraints = new List<SchemaConstraint>(),
                    Metadata = new Dictionary<string, object>()
                };
                tablesDict[tableKey] = table;
                schema.Tables.Add(table);
            }

            var column = new SchemaColumn
            {
                Name = row["COLUMN_NAME"].ToString()!,
                DataType = FormatMySqlDataType(row),
                IsNullable = row["IS_NULLABLE"].ToString() == "YES",
                IsPrimaryKey = row["COLUMN_KEY"].ToString() == "PRI",
                IsIdentity = row["EXTRA"].ToString().Contains("auto_increment"),
                MaxLength = row["CHARACTER_MAXIMUM_LENGTH"] != DBNull.Value ? (int?)Convert.ToInt64(row["CHARACTER_MAXIMUM_LENGTH"]) : null,
                Precision = row["NUMERIC_PRECISION"] != DBNull.Value ? (int?)Convert.ToInt64(row["NUMERIC_PRECISION"]) : null,
                Scale = row["NUMERIC_SCALE"] != DBNull.Value ? (int?)Convert.ToInt64(row["NUMERIC_SCALE"]) : null,
                DefaultValue = row["COLUMN_DEFAULT"]?.ToString(),
                Metadata = new Dictionary<string, object>
                {
                    ["mysql_data_type"] = row["DATA_TYPE"].ToString()!,
                    ["column_key"] = row["COLUMN_KEY"].ToString()!,
                    ["extra"] = row["EXTRA"].ToString()!
                }
            };

            table.Columns.Add(column);
        }
    }

    private async Task LoadMySqlConstraintsAsync(DatabaseSchema schema, SqlSchemaConfiguration config)
    {
        var sql = @"
                SELECT 
                    tc.TABLE_SCHEMA,
                    tc.TABLE_NAME,
                    tc.CONSTRAINT_NAME,
                    tc.CONSTRAINT_TYPE,
                    kcu.COLUMN_NAME,
                    kcu.REFERENCED_TABLE_SCHEMA,
                    kcu.REFERENCED_TABLE_NAME,
                    kcu.REFERENCED_COLUMN_NAME,
                    rc.UPDATE_RULE,
                    rc.DELETE_RULE,
                    cc.CHECK_CLAUSE
                FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                LEFT JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu ON tc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME AND tc.TABLE_SCHEMA = kcu.TABLE_SCHEMA
                LEFT JOIN INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS rc ON tc.CONSTRAINT_NAME = rc.CONSTRAINT_NAME AND tc.CONSTRAINT_SCHEMA = rc.CONSTRAINT_SCHEMA
                LEFT JOIN INFORMATION_SCHEMA.CHECK_CONSTRAINTS cc ON tc.CONSTRAINT_NAME = cc.CONSTRAINT_NAME AND tc.CONSTRAINT_SCHEMA = cc.CONSTRAINT_SCHEMA
                WHERE tc.TABLE_SCHEMA = ?
                ORDER BY tc.TABLE_SCHEMA, tc.TABLE_NAME, tc.CONSTRAINT_NAME";

        var dataTable = await ExecuteMySqlQueryAsync(sql, config, config.Database.DatabaseName);

        var constraintsDict = new Dictionary<string, SchemaConstraint>();

        foreach (DataRow row in dataTable.Rows)
        {
            var constraintKey = $"{row["TABLE_SCHEMA"]}.{row["CONSTRAINT_NAME"]}";

            if (!constraintsDict.TryGetValue(constraintKey, out var constraint))
            {
                constraint = new SchemaConstraint
                {
                    Name = row["CONSTRAINT_NAME"].ToString()!,
                    Type = MapMySqlConstraintType(row["CONSTRAINT_TYPE"].ToString()!),
                    TableName = row["TABLE_NAME"].ToString()!,
                    Schema = row["TABLE_SCHEMA"].ToString()!,
                    Columns = new List<string>(),
                    ReferencedColumns = new List<string>(),
                    ReferencedTable = row["REFERENCED_TABLE_NAME"]?.ToString(),
                    ReferencedSchema = row["REFERENCED_TABLE_SCHEMA"]?.ToString(),
                    OnDeleteAction = row["DELETE_RULE"]?.ToString() ?? "NO ACTION",
                    OnUpdateAction = row["UPDATE_RULE"]?.ToString() ?? "NO ACTION",
                    CheckExpression = row["CHECK_CLAUSE"]?.ToString(),
                    Metadata = new Dictionary<string, object>()
                };
                constraintsDict[constraintKey] = constraint;
                schema.Constraints.Add(constraint);
            }

            if (row["COLUMN_NAME"] != DBNull.Value && !string.IsNullOrEmpty(row["COLUMN_NAME"].ToString()))
            {
                var columnName = row["COLUMN_NAME"].ToString()!;
                if (!constraint.Columns.Contains(columnName))
                {
                    constraint.Columns.Add(columnName);
                }
            }

            if (row["REFERENCED_COLUMN_NAME"] != DBNull.Value && !string.IsNullOrEmpty(row["REFERENCED_COLUMN_NAME"].ToString()))
            {
                var refColumnName = row["REFERENCED_COLUMN_NAME"].ToString()!;
                if (!constraint.ReferencedColumns.Contains(refColumnName))
                {
                    constraint.ReferencedColumns.Add(refColumnName);
                }
            }
        }
    }

    private async Task LoadMySqlIndexesAsync(DatabaseSchema schema, SqlSchemaConfiguration config)
    {
        var sql = @"
                SELECT 
                    TABLE_SCHEMA,
                    TABLE_NAME,
                    INDEX_NAME,
                    COLUMN_NAME,
                    NON_UNIQUE,
                    SEQ_IN_INDEX
                FROM INFORMATION_SCHEMA.STATISTICS
                WHERE TABLE_SCHEMA = ? AND INDEX_NAME != 'PRIMARY'
                ORDER BY TABLE_SCHEMA, TABLE_NAME, INDEX_NAME, SEQ_IN_INDEX";

        var dataTable = await ExecuteMySqlQueryAsync(sql, config, config.Database.DatabaseName);

        var indexesDict = new Dictionary<string, SchemaIndex>();

        foreach (DataRow row in dataTable.Rows)
        {
            var indexKey = $"{row["TABLE_SCHEMA"]}.{row["INDEX_NAME"]}";

            if (!indexesDict.TryGetValue(indexKey, out var index))
            {
                index = new SchemaIndex
                {
                    Name = row["INDEX_NAME"].ToString()!,
                    TableName = row["TABLE_NAME"].ToString()!,
                    Schema = row["TABLE_SCHEMA"].ToString()!,
                    Columns = new List<string>(),
                    IsUnique = Convert.ToInt64(row["NON_UNIQUE"]) == 0,
                    IsClustered = false, // MySQL doesn't have clustered indexes
                    Metadata = new Dictionary<string, object>()
                };
                indexesDict[indexKey] = index;
                schema.Indexes.Add(index);
            }

            if (row["COLUMN_NAME"] != DBNull.Value)
            {
                index.Columns.Add(row["COLUMN_NAME"].ToString()!);
            }
        }
    }

    private async Task LoadMySqlViewsAsync(DatabaseSchema schema, SqlSchemaConfiguration config)
    {
        var sql = @"
                SELECT 
                    TABLE_SCHEMA,
                    TABLE_NAME,
                    VIEW_DEFINITION
                FROM INFORMATION_SCHEMA.VIEWS
                WHERE TABLE_SCHEMA = ?
                ORDER BY TABLE_SCHEMA, TABLE_NAME";

        var dataTable = await ExecuteMySqlQueryAsync(sql, config, config.Database.DatabaseName);

        foreach (DataRow row in dataTable.Rows)
        {
            var view = new SchemaView
            {
                Name = row["TABLE_NAME"].ToString()!,
                Schema = row["TABLE_SCHEMA"].ToString()!,
                Definition = row["VIEW_DEFINITION"]?.ToString() ?? string.Empty,
                Columns = new List<SchemaColumn>(),
                Metadata = new Dictionary<string, object>()
            };

            schema.Views.Add(view);
        }
    }

    private async Task LoadMySqlProceduresAsync(DatabaseSchema schema, SqlSchemaConfiguration config)
    {
        var sql = @"
                SELECT 
                    ROUTINE_SCHEMA,
                    ROUTINE_NAME,
                    ROUTINE_DEFINITION
                FROM INFORMATION_SCHEMA.ROUTINES
                WHERE ROUTINE_SCHEMA = ? AND ROUTINE_TYPE = 'PROCEDURE'
                ORDER BY ROUTINE_SCHEMA, ROUTINE_NAME";

        var dataTable = await ExecuteMySqlQueryAsync(sql, config, config.Database.DatabaseName);

        foreach (DataRow row in dataTable.Rows)
        {
            var procedure = new SchemaProcedure
            {
                Name = row["ROUTINE_NAME"].ToString()!,
                Schema = row["ROUTINE_SCHEMA"].ToString()!,
                Definition = row["ROUTINE_DEFINITION"]?.ToString() ?? string.Empty,
                Parameters = new List<SchemaParameter>(),
                Metadata = new Dictionary<string, object>()
            };

            schema.Procedures.Add(procedure);
        }
    }

    private async Task LoadMySqlFunctionsAsync(DatabaseSchema schema, SqlSchemaConfiguration config)
    {
        var sql = @"
                SELECT 
                    ROUTINE_SCHEMA,
                    ROUTINE_NAME,
                    ROUTINE_DEFINITION,
                    DATA_TYPE as RETURN_TYPE
                FROM INFORMATION_SCHEMA.ROUTINES
                WHERE ROUTINE_SCHEMA = ? AND ROUTINE_TYPE = 'FUNCTION'
                ORDER BY ROUTINE_SCHEMA, ROUTINE_NAME";

        var dataTable = await ExecuteMySqlQueryAsync(sql, config, config.Database.DatabaseName);

        foreach (DataRow row in dataTable.Rows)
        {
            var function = new SchemaFunction
            {
                Name = row["ROUTINE_NAME"].ToString()!,
                Schema = row["ROUTINE_SCHEMA"].ToString()!,
                Definition = row["ROUTINE_DEFINITION"]?.ToString() ?? string.Empty,
                ReturnType = row["RETURN_TYPE"]?.ToString() ?? "void",
                Parameters = new List<SchemaParameter>(),
                Metadata = new Dictionary<string, object>()
            };

            schema.Functions.Add(function);
        }
    }

    private async Task<DataTable> ExecuteMySqlQueryAsync(string sql, SqlSchemaConfiguration config, params object[] parameters)
    {
        using var connection = await CreateConnectionAsync(config);
        await connection.OpenAsync();

        using var command = new MySqlCommand(sql, (MySqlConnection)connection);
        command.CommandTimeout = (int)config.Database.CommandTimeoutSeconds;

        for (int i = 0; i < parameters.Length; i++)
        {
            command.Parameters.AddWithValue($"@param{i}", parameters[i]);
        }

        var dataTable = new DataTable();
        using var reader = await command.ExecuteReaderAsync();
        dataTable.Load(reader);

        return dataTable;
    }

    private string FormatMySqlDataType(DataRow row)
    {
        var dataType = row["DATA_TYPE"].ToString()!;
        var maxLength = row["CHARACTER_MAXIMUM_LENGTH"] != DBNull.Value ? Convert.ToInt64(row["CHARACTER_MAXIMUM_LENGTH"]) : 0;
        var precision = row["NUMERIC_PRECISION"] != DBNull.Value ? Convert.ToInt64(row["NUMERIC_PRECISION"]) : 0;
        var scale = row["NUMERIC_SCALE"] != DBNull.Value ? Convert.ToInt64(row["NUMERIC_SCALE"]) : 0;

        return dataType.ToLowerInvariant() switch
        {
            "varchar" or "char" => maxLength > 0 ? $"{dataType}({maxLength})" : dataType,
            "decimal" or "numeric" => precision > 0 && scale >= 0 ? $"{dataType}({precision},{scale})" : dataType,
            _ => dataType
        };
    }

    private string MapMySqlConstraintType(string type)
    {
        return type switch
        {
            "PRIMARY KEY" => "PK",
            "UNIQUE" => "UQ",
            "FOREIGN KEY" => "FK",
            "CHECK" => "CK",
            _ => type
        };
    }
}

// Oracle Provider
public interface IOracleProviderService : IDatabaseProvider { }
public class OracleProviderService : BaseDatabaseProvider, IOracleProviderService
{
    public OracleProviderService(ILogger<OracleProviderService> logger) : base(logger) { }

    public override async Task<DatabaseSchema> GetCurrentSchemaAsync(SqlSchemaConfiguration config)
    {
        var schema = new DatabaseSchema
        {
            DatabaseName = config.Database.DatabaseName,
            Provider = "oracle",
            AnalysisTime = DateTime.UtcNow,
            Tables = new List<SchemaTable>(),
            Views = new List<SchemaView>(),
            Indexes = new List<SchemaIndex>(),
            Constraints = new List<SchemaConstraint>(),
            Procedures = new List<SchemaProcedure>(),
            Functions = new List<SchemaFunction>(),
            Metadata = new Dictionary<string, object>
            {
                ["server"] = config.Database.Server,
                ["database"] = config.Database.DatabaseName,
                ["service_name"] = config.Database.OracleServiceName ?? "ORCL"
            }
        };

        await LoadOracleTablesAsync(schema, config);
        await LoadOracleConstraintsAsync(schema, config);
        await LoadOracleIndexesAsync(schema, config);
        await LoadOracleViewsAsync(schema, config);
        await LoadOracleProceduresAsync(schema, config);
        await LoadOracleFunctionsAsync(schema, config);

        return schema;
    }

    public override async Task<IDbConnection> CreateConnectionAsync(SqlSchemaConfiguration config)
    {
        var connectionString = BuildConnectionString(config);
        return await CreateConnectionWithRetryAsync(connectionString, cs => new OracleConnection(cs), config);
    }

    public override string BuildConnectionString(SqlSchemaConfiguration config)
    {
        if (!string.IsNullOrEmpty(config.Database.ConnectionString))
        {
            return config.Database.ConnectionString;
        }

        var port = config.Database.Port > 0 ? config.Database.Port : 1521;
        var serviceName = config.Database.OracleServiceName ?? "ORCL";

        var builder = new OracleConnectionStringBuilder
        {
            DataSource = $"{config.Database.Server}:{port}/{serviceName}",
            ConnectionTimeout = config.Database.ConnectionTimeoutSeconds
        };

        if (!config.Database.UseIntegratedAuth)
        {
            builder.UserID = config.Database.Username;
            builder.Password = config.Database.Password;
        }

        return builder.ConnectionString;
    }

    private async Task LoadOracleTablesAsync(DatabaseSchema schema, SqlSchemaConfiguration config)
    {
        var sql = @"
                SELECT 
                    t.OWNER,
                    t.TABLE_NAME,
                    c.COLUMN_NAME,
                    c.DATA_TYPE,
                    c.DATA_LENGTH,
                    c.DATA_PRECISION,
                    c.DATA_SCALE,
                    c.NULLABLE,
                    c.DATA_DEFAULT,
                    CASE WHEN pk.COLUMN_NAME IS NOT NULL THEN 'Y' ELSE 'N' END as IS_PRIMARY_KEY
                FROM ALL_TABLES t
                LEFT JOIN ALL_TAB_COLUMNS c ON t.TABLE_NAME = c.TABLE_NAME AND t.OWNER = c.OWNER
                LEFT JOIN (
                    SELECT acc.OWNER, acc.TABLE_NAME, acc.COLUMN_NAME
                    FROM ALL_CONSTRAINTS ac
                    JOIN ALL_CONS_COLUMNS acc ON ac.CONSTRAINT_NAME = acc.CONSTRAINT_NAME AND ac.OWNER = acc.OWNER
                    WHERE ac.CONSTRAINT_TYPE = 'P'
                ) pk ON c.TABLE_NAME = pk.TABLE_NAME AND c.COLUMN_NAME = pk.COLUMN_NAME AND c.OWNER = pk.OWNER
                WHERE t.OWNER = :schema_name
                ORDER BY t.OWNER, t.TABLE_NAME, c.COLUMN_ID";

        var schemaName = config.Database.Username?.ToUpper() ?? "SYSTEM";
        var dataTable = await ExecuteOracleQueryAsync(sql, config, new Dictionary<string, object>
        {
            [":schema_name"] = schemaName
        });

        var tablesDict = new Dictionary<string, SchemaTable>();

        foreach (DataRow row in dataTable.Rows)
        {
            if (row["COLUMN_NAME"] == DBNull.Value) continue;

            var tableKey = $"{row["OWNER"]}.{row["TABLE_NAME"]}";

            if (!tablesDict.TryGetValue