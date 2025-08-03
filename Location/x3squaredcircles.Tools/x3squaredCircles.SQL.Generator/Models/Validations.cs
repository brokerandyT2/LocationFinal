namespace x3squaredcirecles.SQLSync.Generator.Models;

public enum ValidationResult
{
    Safe = 0,           // ✅ Auto-deploy - no issues found
    Warnings = 1,       // ⚠️  Manual approval required - risky but allowable changes
    Blocked = 2         // ❌ Cannot deploy - unsafe changes detected
}

public class ValidationIssue
{
    public ValidationSeverity Severity { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Statement { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string ColumnName { get; set; } = string.Empty;
}

public enum ValidationSeverity
{
    Info = 0,       // Informational - safe changes
    Warning = 1,    // Risky but allowable - manual approval recommended
    Error = 2       // Unsafe - deployment should be blocked
}

public class ValidationReport
{
    public List<ValidationIssue> Issues { get; set; } = new();
    public ValidationResult OverallResult { get; set; }
    public int TotalStatements { get; set; }
    public int SafeStatements { get; set; }
    public int WarningStatements { get; set; }
    public int ErrorStatements { get; set; }
    public TimeSpan EstimatedDuration { get; set; }
    public long EstimatedSpaceImpactMB { get; set; }

    public bool HasBlockingIssues => Issues.Any(i => i.Severity == ValidationSeverity.Error);
    public bool HasWarnings => Issues.Any(i => i.Severity == ValidationSeverity.Warning);
    public bool IsSafe => !HasBlockingIssues && !HasWarnings;

    public string Summary => OverallResult switch
    {
        ValidationResult.Safe => $"✅ All {TotalStatements} statements are safe for automatic deployment",
        ValidationResult.Warnings => $"⚠️  {WarningStatements} of {TotalStatements} statements require manual approval",
        ValidationResult.Blocked => $"❌ {ErrorStatements} of {TotalStatements} statements are unsafe - deployment blocked",
        _ => "Unknown validation state"
    };
}

public static class ValidationCategories
{
    public const string DataLoss = "Data Loss Risk";
    public const string Performance = "Performance Impact";
    public const string Compatibility = "Compatibility Risk";
    public const string Security = "Security Impact";
    public const string Dependency = "Dependency Risk";
    public const string LockingImpact = "Locking Impact";
    public const string SpaceImpact = "Storage Impact";
}

public static class ValidationRules
{
    // Blocking rules (Error severity)
    public static readonly Dictionary<string, string> BlockingPatterns = new()
    {
        { "DROP TABLE", "Dropping tables causes permanent data loss" },
        { "DROP COLUMN", "Dropping columns causes permanent data loss" },
        { "TRUNCATE TABLE", "Truncating tables causes permanent data loss" },
        { "DROP DATABASE", "Dropping databases is not allowed in automated deployments" },
        { "DROP SCHEMA", "Dropping schemas can cause widespread dependency failures" }
    };

    // Warning rules (Warning severity)
    public static readonly Dictionary<string, string> WarningPatterns = new()
    {
        { "ALTER COLUMN", "Column alterations may truncate or convert data" },
        { "ADD COLUMN.*NOT NULL.*WITHOUT DEFAULT", "Adding NOT NULL columns without defaults will fail on existing data" },
        { "CREATE.*INDEX.*ON.*", "Creating indexes on large tables can cause performance impact" },
        { "ALTER TABLE.*ADD CONSTRAINT.*FOREIGN KEY", "Adding foreign key constraints may fail on existing data" },
        { "ALTER TABLE.*ADD CONSTRAINT.*CHECK", "Adding check constraints may fail on existing data" },
        { "ALTER TABLE.*ADD CONSTRAINT.*UNIQUE", "Adding unique constraints may fail on existing data" }
    };

    // Info rules (Info severity)
    public static readonly Dictionary<string, string> SafePatterns = new()
    {
        { "CREATE TABLE", "Creating new tables is generally safe" },
        { "CREATE SCHEMA", "Creating new schemas is safe" },
        { "ADD COLUMN.*NULL", "Adding nullable columns is safe" },
        { "ADD COLUMN.*DEFAULT", "Adding columns with defaults is generally safe" },
        { "CREATE.*INDEX.*WHERE.*", "Creating filtered indexes has minimal impact" }
    };
}