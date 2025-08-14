using System;
using System.Collections.Generic;

namespace x3squaredcircles.SQLSync.Generator.Models
{
    // Configuration models
    public class SqlSchemaConfiguration
    {
        public LanguageConfiguration Language { get; set; } = new();
        public DatabaseConfiguration Database { get; set; } = new();
        public string TrackAttribute { get; set; } = string.Empty;
        public string RepoUrl { get; set; } = string.Empty;
        public string Branch { get; set; } = string.Empty;
        public LicenseConfiguration License { get; set; } = new();
        public KeyVaultConfiguration? KeyVault { get; set; }
        public TagTemplateConfiguration TagTemplate { get; set; } = new();
        public OperationConfiguration Operation { get; set; } = new();
        public SchemaAnalysisConfiguration SchemaAnalysis { get; set; } = new();
        public DeploymentConfiguration Deployment { get; set; } = new();
        public BackupConfiguration Backup { get; set; } = new();
        public LoggingConfiguration Logging { get; set; } = new();
        public AuthenticationConfiguration Authentication { get; set; } = new();
        public EnvironmentConfiguration Environment { get; set; } = new();
    }

    public class LanguageConfiguration
    {
        public bool CSharp { get; set; }
        public bool Java { get; set; }
        public bool Python { get; set; }
        public bool JavaScript { get; set; }
        public bool TypeScript { get; set; }
        public bool Go { get; set; }

        public string GetSelectedLanguage()
        {
            if (CSharp) return "csharp";
            if (Java) return "java";
            if (Python) return "python";
            if (JavaScript) return "javascript";
            if (TypeScript) return "typescript";
            if (Go) return "go";
            return string.Empty;
        }

        public bool HasSingleLanguageSelected()
        {
            var count = 0;
            if (CSharp) count++;
            if (Java) count++;
            if (Python) count++;
            if (JavaScript) count++;
            if (TypeScript) count++;
            if (Go) count++;
            return count == 1;
        }
    }

    public class DatabaseConfiguration
    {
        // Provider selection
        public bool SqlServer { get; set; }
        public bool PostgreSQL { get; set; }
        public bool MySQL { get; set; }
        public bool Oracle { get; set; }
        public bool SQLite { get; set; }

        // Connection settings
        public string Server { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;
        public string? Schema { get; set; }
        public int Port { get; set; }

        // Authentication
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string? UsernameVaultKey { get; set; }
        public string? PasswordVaultKey { get; set; }
        public bool UseIntegratedAuth { get; set; }
        public string? ConnectionString { get; set; }

        // Connection behavior
        public int ConnectionTimeoutSeconds { get; set; } = 30;
        public int CommandTimeoutSeconds { get; set; } = 300;
        public int RetryAttempts { get; set; } = 3;
        public int RetryIntervalSeconds { get; set; } = 5;

        // SQL Server specific
        public string? SqlServerInstance { get; set; }
        public bool SqlServerEncrypt { get; set; } = true;
        public bool SqlServerTrustCert { get; set; }
        public string SqlServerBackupType { get; set; } = "FULL";

        // PostgreSQL specific
        public string PostgreSqlSslMode { get; set; } = "require";
        public string? PostgreSqlSchemaSearchPath { get; set; }
        public string PostgreSqlApplicationName { get; set; } = "sql-schema-generator";

        // MySQL specific
        public string MySqlSslMode { get; set; } = "REQUIRED";
        public string MySqlCharset { get; set; } = "utf8mb4";
        public string MySqlEngine { get; set; } = "InnoDB";

        // Oracle specific
        public string? OracleServiceName { get; set; }
        public string? OracleTablespace { get; set; }
        public string? OracleTempTablespace { get; set; }

        // SQLite specific
        public string? SqliteFilePath { get; set; }
        public string SqliteJournalMode { get; set; } = "WAL";
        public string SqliteSynchronous { get; set; } = "FULL";

        public string GetSelectedProvider()
        {
            if (SqlServer) return "sqlserver";
            if (PostgreSQL) return "postgresql";
            if (MySQL) return "mysql";
            if (Oracle) return "oracle";
            if (SQLite) return "sqlite";
            return string.Empty;
        }

        public bool HasSingleProviderSelected()
        {
            var count = 0;
            if (SqlServer) count++;
            if (PostgreSQL) count++;
            if (MySQL) count++;
            if (Oracle) count++;
            if (SQLite) count++;
            return count == 1;
        }
    }

    public class LicenseConfiguration
    {
        public string ServerUrl { get; set; } = string.Empty;
        public string ToolName { get; set; } = "sql-schema-generator";
        public int TimeoutSeconds { get; set; } = 300;
        public int RetryIntervalSeconds { get; set; } = 30;
    }

    public class KeyVaultConfiguration
    {
        public string Type { get; set; } = string.Empty; // azure, aws, hashicorp
        public string Url { get; set; } = string.Empty;
        public Dictionary<string, string> Parameters { get; set; } = new();
    }

    public class TagTemplateConfiguration
    {
        public string Template { get; set; } = "{branch}/{repo}/schema/{version}";
    }

    public class OperationConfiguration
    {
        public string Mode { get; set; } = "noop"; // noop, validate, execute
        public bool ValidateOnly { get; set; }
        public bool NoOp { get; set; } = true;
        public bool SkipBackup { get; set; }
    }

    public class SchemaAnalysisConfiguration
    {
        public string? AssemblyPaths { get; set; }
        public string? BuildOutputPath { get; set; }
        public string? ScriptsPath { get; set; }
        public bool IgnoreExportAttribute { get; set; }
        public bool GenerateIndexes { get; set; } = true;
        public bool GenerateFkIndexes { get; set; } = true;
        public bool EnableCrossSchemaRefs { get; set; } = true;
        public string ValidationLevel { get; set; } = "standard"; // standard, strict, relaxed
    }

    public class DeploymentConfiguration
    {
        public bool Enable29PhaseDeployment { get; set; } = true;
        public bool SkipWarningPhases { get; set; }
        public string? CustomPhaseOrder { get; set; }
    }

    public class BackupConfiguration
    {
        public bool BackupBeforeDeployment { get; set; } = true;
        public int RetentionDays { get; set; } = 7;
        public string? RestorePointLabel { get; set; }
    }

    public class LoggingConfiguration
    {
        public bool Verbose { get; set; }
        public string LogLevel { get; set; } = "INFO";
        public bool SchemaDump { get; set; }
    }

    public class AuthenticationConfiguration
    {
        public string? PatToken { get; set; }
        public string? PatSecretName { get; set; }
        public string? PipelineToken { get; set; }
    }

    public class EnvironmentConfiguration
    {
        public string Environment { get; set; } = "dev";
        public string? Vertical { get; set; }
    }

    // Entity discovery models
    public class EntityDiscoveryResult
    {
        public List<DiscoveredEntity> Entities { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
        public DateTime DiscoveryTime { get; set; } = DateTime.UtcNow;
        public string Language { get; set; } = string.Empty;
        public string TrackAttribute { get; set; } = string.Empty;
    }

    public class DiscoveredEntity
    {
        public string Name { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Namespace { get; set; } = string.Empty;
        public string TableName { get; set; } = string.Empty;
        public string? SchemaName { get; set; }
        public List<DiscoveredProperty> Properties { get; set; } = new();
        public List<DiscoveredIndex> Indexes { get; set; } = new();
        public List<DiscoveredRelationship> Relationships { get; set; } = new();
        public Dictionary<string, object> Attributes { get; set; } = new();
        public string SourceFile { get; set; } = string.Empty;
        public int SourceLine { get; set; }
    }

    public class DiscoveredProperty
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string SqlType { get; set; } = string.Empty;
        public bool IsNullable { get; set; }
        public bool IsPrimaryKey { get; set; }
        public bool IsForeignKey { get; set; }
        public bool IsUnique { get; set; }
        public bool IsIndexed { get; set; }
        public int? MaxLength { get; set; }
        public int? Precision { get; set; }
        public int? Scale { get; set; }
        public string? DefaultValue { get; set; }
        public Dictionary<string, object> Attributes { get; set; } = new();
    }

    public class DiscoveredIndex
    {
        public string Name { get; set; } = string.Empty;
        public List<string> Columns { get; set; } = new();
        public bool IsUnique { get; set; }
        public bool IsClustered { get; set; }
        public string? FilterExpression { get; set; }
        public Dictionary<string, object> Attributes { get; set; } = new();
    }

    public class DiscoveredRelationship
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // OneToOne, OneToMany, ManyToMany
        public string ReferencedEntity { get; set; } = string.Empty;
        public string ReferencedTable { get; set; } = string.Empty;
        public List<string> ForeignKeyColumns { get; set; } = new();
        public List<string> ReferencedColumns { get; set; } = new();
        public string OnDeleteAction { get; set; } = "NO_ACTION";
        public string OnUpdateAction { get; set; } = "NO_ACTION";
        public Dictionary<string, object> Attributes { get; set; } = new();
    }

    // Schema analysis models
    public class DatabaseSchema
    {
        public string DatabaseName { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public List<SchemaTable> Tables { get; set; } = new();
        public List<SchemaView> Views { get; set; } = new();
        public List<SchemaIndex> Indexes { get; set; } = new();
        public List<SchemaConstraint> Constraints { get; set; } = new();
        public List<SchemaProcedure> Procedures { get; set; } = new();
        public List<SchemaFunction> Functions { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
        public DateTime AnalysisTime { get; set; } = DateTime.UtcNow;
    }

    public class SchemaTable
    {
        public string Name { get; set; } = string.Empty;
        public string Schema { get; set; } = string.Empty;
        public List<SchemaColumn> Columns { get; set; } = new();
        public List<SchemaIndex> Indexes { get; set; } = new();
        public List<SchemaConstraint> Constraints { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class SchemaColumn
    {
        public string Name { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public bool IsNullable { get; set; }
        public bool IsPrimaryKey { get; set; }
        public bool IsIdentity { get; set; }
        public int? MaxLength { get; set; }
        public int? Precision { get; set; }
        public int? Scale { get; set; }
        public string? DefaultValue { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class SchemaView
    {
        public string Name { get; set; } = string.Empty;
        public string Schema { get; set; } = string.Empty;
        public string Definition { get; set; } = string.Empty;
        public List<SchemaColumn> Columns { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class SchemaIndex
    {
        public string Name { get; set; } = string.Empty;
        public string TableName { get; set; } = string.Empty;
        public string Schema { get; set; } = string.Empty;
        public List<string> Columns { get; set; } = new();
        public bool IsUnique { get; set; }
        public bool IsClustered { get; set; }
        public string? FilterExpression { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class SchemaConstraint
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // PK, FK, UQ, CK
        public string TableName { get; set; } = string.Empty;
        public string Schema { get; set; } = string.Empty;
        public List<string> Columns { get; set; } = new();
        public string? ReferencedTable { get; set; }
        public string? ReferencedSchema { get; set; }
        public List<string> ReferencedColumns { get; set; } = new();
        public string? CheckExpression { get; set; }
        public string OnDeleteAction { get; set; } = "NO_ACTION";
        public string OnUpdateAction { get; set; } = "NO_ACTION";
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class SchemaProcedure
    {
        public string Name { get; set; } = string.Empty;
        public string Schema { get; set; } = string.Empty;
        public string Definition { get; set; } = string.Empty;
        public List<SchemaParameter> Parameters { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class SchemaFunction
    {
        public string Name { get; set; } = string.Empty;
        public string Schema { get; set; } = string.Empty;
        public string Definition { get; set; } = string.Empty;
        public string ReturnType { get; set; } = string.Empty;
        public List<SchemaParameter> Parameters { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class SchemaParameter
    {
        public string Name { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public bool IsOutput { get; set; }
        public string? DefaultValue { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    // Validation and risk assessment models
    public class SchemaValidationResult
    {
        public List<SchemaChange> Changes { get; set; } = new();
        public List<ValidationError> Errors { get; set; } = new();
        public List<ValidationWarning> Warnings { get; set; } = new();
        public bool IsValid { get; set; } = true;
        public Dictionary<string, object> Metadata { get; set; } = new();
        public DateTime ValidationTime { get; set; } = DateTime.UtcNow;
    }

    public class SchemaChange
    {
        public string Type { get; set; } = string.Empty; // CREATE, ALTER, DROP
        public string ObjectType { get; set; } = string.Empty; // TABLE, COLUMN, INDEX, etc.
        public string ObjectName { get; set; } = string.Empty;
        public string? Schema { get; set; }
        public string Description { get; set; } = string.Empty;
        public RiskLevel RiskLevel { get; set; } = RiskLevel.Safe;
        public Dictionary<string, object> Properties { get; set; } = new();
        public List<string> Dependencies { get; set; } = new();
    }

    public class ValidationError
    {
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string ObjectName { get; set; } = string.Empty;
        public string? Schema { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new();
    }

    public class ValidationWarning
    {
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string ObjectName { get; set; } = string.Empty;
        public string? Schema { get; set; }
        public RiskLevel RiskLevel { get; set; } = RiskLevel.Warning;
        public Dictionary<string, object> Properties { get; set; } = new();
    }

    public class RiskAssessment
    {
        public RiskLevel OverallRiskLevel { get; set; } = RiskLevel.Safe;
        public List<RiskFactor> RiskFactors { get; set; } = new();
        public int SafeOperations { get; set; }
        public int WarningOperations { get; set; }
        public int RiskyOperations { get; set; }
        public bool RequiresApproval { get; set; }
        public bool RequiresDualApproval { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
        public DateTime AssessmentTime { get; set; } = DateTime.UtcNow;
    }

    public class RiskFactor
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public RiskLevel RiskLevel { get; set; } = RiskLevel.Safe;
        public string Category { get; set; } = string.Empty;
        public List<string> AffectedObjects { get; set; } = new();
        public Dictionary<string, object> Properties { get; set; } = new();
    }

    public enum RiskLevel
    {
        Safe = 0,
        Warning = 1,
        Risky = 2
    }

    // Deployment models
    public class DeploymentPlan
    {
        public List<DeploymentPhase> Phases { get; set; } = new();
        public RiskLevel OverallRiskLevel { get; set; } = RiskLevel.Safe;
        public bool Use29PhaseDeployment { get; set; } = true;
        public Dictionary<string, object> Metadata { get; set; } = new();
        public DateTime CreatedTime { get; set; } = DateTime.UtcNow;
    }

    public class DeploymentPhase
    {
        public int PhaseNumber { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<DeploymentOperation> Operations { get; set; } = new();
        public RiskLevel RiskLevel { get; set; } = RiskLevel.Safe;
        public bool RequiresApproval { get; set; }
        public bool CanRollback { get; set; } = true;
        public List<string> Dependencies { get; set; } = new();
        public Dictionary<string, object> Properties { get; set; } = new();
    }

    public class DeploymentOperation
    {
        public string Type { get; set; } = string.Empty;
        public string ObjectName { get; set; } = string.Empty;
        public string? Schema { get; set; }
        public string SqlCommand { get; set; } = string.Empty;
        public string? RollbackCommand { get; set; }
        public RiskLevel RiskLevel { get; set; } = RiskLevel.Safe;
        public List<string> Dependencies { get; set; } = new();
        public Dictionary<string, object> Properties { get; set; } = new();
    }

    public class SqlScript
    {
        public string Content { get; set; } = string.Empty;
        public List<SqlStatement> Statements { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
        public DateTime GeneratedTime { get; set; } = DateTime.UtcNow;
    }

    public class SqlStatement
    {
        public string Sql { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string? ObjectName { get; set; }
        public string? Schema { get; set; }
        public RiskLevel RiskLevel { get; set; } = RiskLevel.Safe;
        public int PhaseNumber { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new();
    }

    public class DeploymentResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public List<PhaseResult> PhaseResults { get; set; } = new();
        public TimeSpan Duration { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class PhaseResult
    {
        public int PhaseNumber { get; set; }
        public string PhaseName { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public TimeSpan Duration { get; set; }
        public int OperationsExecuted { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    // License client models
    public class LicenseAcquireRequest
    {
        public string ToolName { get; set; } = string.Empty;
        public string ToolVersion { get; set; } = "1.0.0";
        public string IpAddress { get; set; } = string.Empty;
        public string BuildId { get; set; } = string.Empty;
    }

    public class LicenseAcquireResponse
    {
        public bool LicenseGranted { get; set; }
        public string? SessionId { get; set; }
        public bool BurstMode { get; set; }
        public int BurstCountRemaining { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string? Reason { get; set; }
        public bool BurstEventsExhausted { get; set; }
        public int RetryAfterSeconds { get; set; }
    }

    public class LicenseHeartbeatRequest
    {
        public string SessionId { get; set; } = string.Empty;
    }

    public class LicenseReleaseRequest
    {
        public string SessionId { get; set; } = string.Empty;
    }

    public class LicenseSession
    {
        public string SessionId { get; set; } = string.Empty;
        public bool BurstMode { get; set; }
        public int BurstCountRemaining { get; set; }
        public DateTime ExpiresAt { get; set; }
        public Timer? HeartbeatTimer { get; set; }
    }

    // Tag template models
    public class TagTemplateResult
    {
        public string GeneratedTag { get; set; } = string.Empty;
        public Dictionary<string, string> TokenValues { get; set; } = new();
    }

    // Output models
    public class SqlSchemaMetadata
    {
        public string ToolName { get; set; } = "sql-schema-generator";
        public string ToolVersion { get; set; } = "1.0.0";
        public DateTime ExecutionTime { get; set; }
        public string Language { get; set; } = string.Empty;
        public string DatabaseProvider { get; set; } = string.Empty;
        public string TrackAttribute { get; set; } = string.Empty;
        public string Repository { get; set; } = string.Empty;
        public string Branch { get; set; } = string.Empty;
        public EntityDiscoveryResult EntityDiscovery { get; set; } = new();
        public SchemaValidationResult SchemaValidation { get; set; } = new();
        public RiskAssessment RiskAssessment { get; set; } = new();
        public DeploymentPlan DeploymentPlan { get; set; } = new();
        public TagTemplateResult TagTemplate { get; set; } = new();
        public bool LicenseUsed { get; set; }
        public bool BurstModeUsed { get; set; }
        public string Mode { get; set; } = string.Empty;
    }

    public class PipelineToolLogEntry
    {
        public string ToolName { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public bool BurstMode { get; set; }
        public DateTime Timestamp { get; set; }
    }

    // Error models
    public enum SqlSchemaExitCode
    {
        Success = 0,
        WarningApprovalRequired = 1,
        RiskyDualApprovalRequired = 2,
        LicenseUnavailable = 3,
        AuthenticationFailure = 4,
        DatabaseConnectionFailure = 5,
        EntityDiscoveryFailure = 6,
        SchemaValidationFailure = 7,
        DeploymentExecutionFailure = 8,
        KeyVaultAccessFailure = 9,
        GitOperationFailure = 10,
        InvalidConfiguration = 11
    }

    public class SqlSchemaException : Exception
    {
        public SqlSchemaExitCode ExitCode { get; }

        public SqlSchemaException(SqlSchemaExitCode exitCode, string message) : base(message)
        {
            ExitCode = exitCode;
        }

        public SqlSchemaException(SqlSchemaExitCode exitCode, string message, Exception innerException)
            : base(message, innerException)
        {
            ExitCode = exitCode;
        }
    }
}