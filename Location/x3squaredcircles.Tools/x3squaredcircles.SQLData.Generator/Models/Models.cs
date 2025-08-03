namespace x3squaredcircles.SQLData.Generator.Models;

public class SchemaChanges
{
    public List<DatabaseTable> NewTables { get; set; } = new();
    public List<NewColumn> NewColumns { get; set; } = new();
    public List<ModifiedColumn> ModifiedColumns { get; set; } = new();

    public bool HasChanges => NewTables.Any() || NewColumns.Any() || ModifiedColumns.Any();
}

public class DatabaseSchema
{
    public List<DatabaseTable> Tables { get; set; } = new();
}

public class DatabaseTable
{
    public string SchemaName { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public List<DatabaseColumn> Columns { get; set; } = new();
}

public class DatabaseColumn
{
    public string ColumnName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public short? MaxLength { get; set; }
    public byte? Precision { get; set; }
    public byte? Scale { get; set; }
    public bool IsNullable { get; set; }
    public bool IsIdentity { get; set; }
}

public class NewColumn
{
    public string SchemaName { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string ColumnName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public short? MaxLength { get; set; }
    public bool IsNullable { get; set; }
}

public class ModifiedColumn
{
    public string SchemaName { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string ColumnName { get; set; } = string.Empty;
    public string OldDataType { get; set; } = string.Empty;
    public string NewDataType { get; set; } = string.Empty;
    public bool OldIsNullable { get; set; }
    public bool NewIsNullable { get; set; }
}

public class BadDataConfig
{
    public Dictionary<string, Dictionary<string, List<BadDataScenario>>> Schemas { get; set; } = new();
}

public class BadDataScenario
{
    public string Scenario { get; set; } = string.Empty;
    public int Count { get; set; } = 1;
    public Dictionary<string, object?> Overrides { get; set; } = new();
    public string? ExpectedResult { get; set; }
    public string? LogLevel { get; set; }
}

public class ConstraintValidationResults
{
    public int TotalTests { get; set; }
    public int PassedTests { get; set; }
    public int FailedTests { get; set; }
    public List<ConstraintTestSuccess> Successes { get; set; } = new();
    public List<ConstraintTestFailure> Failures { get; set; } = new();

    public double PassPercentage => TotalTests > 0 ? (PassedTests * 100.0 / TotalTests) : 0;
    public double FailPercentage => TotalTests > 0 ? (FailedTests * 100.0 / TotalTests) : 0;
}

public class ConstraintTestSuccess
{
    public string TestName { get; set; } = string.Empty;
    public string Scenario { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string ConstraintType { get; set; } = string.Empty;
    public int SqlErrorNumber { get; set; }
}

public class ConstraintTestFailure
{
    public string TestName { get; set; } = string.Empty;
    public string Scenario { get; set; } = string.Empty;
    public string Expected { get; set; } = string.Empty;
    public string Actual { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string ActionRequired { get; set; } = string.Empty;
    public string Severity { get; set; } = "warn";
}

public class TestDataOptions
{
    public string Mode { get; set; } = string.Empty;
    public bool IncludeCore { get; set; }
    public string Vertical { get; set; } = string.Empty;
    public bool Generate { get; set; }
    public string? BadDataConfig { get; set; }
    public string? ConnectionString { get; set; }
    public string? OutputPath { get; set; }
    public string? Server { get; set; }
    public string? Database { get; set; }
    public bool UseLocal { get; set; }
    public string? KeyVaultUrl { get; set; }
    public string? UsernameSecret { get; set; }
    public string? PasswordSecret { get; set; }
    public bool Verbose { get; set; }
    public string Volume { get; set; } = "medium";
}