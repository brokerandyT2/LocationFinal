using Location.Core.Helpers.CodeGenerationAttributes;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Text;

namespace x3squaredcirecles.SQLSync.Generator.Services;

public class DeploymentOrchestrator
{
    private readonly ILogger<DeploymentOrchestrator> _logger;
    private readonly DdlStatementGenerator _ddlGenerator;
    private readonly SqlScriptDiscovery _scriptDiscovery;
    private readonly SqlScriptEnhancer _scriptEnhancer;
    private readonly DatabaseSchemaAnalyzer _schemaAnalyzer;
    private readonly RepositoryDetector _repositoryDetector;
    private readonly AssemblyLoader _assemblyLoader;

    public DeploymentOrchestrator(
        ILogger<DeploymentOrchestrator> logger,
        DdlStatementGenerator ddlGenerator,
        SqlScriptDiscovery scriptDiscovery,
        SqlScriptEnhancer scriptEnhancer,
        DatabaseSchemaAnalyzer schemaAnalyzer,
        RepositoryDetector repositoryDetector,
        AssemblyLoader assemblyLoader)
    {
        _logger = logger;
        _ddlGenerator = ddlGenerator;
        _scriptDiscovery = scriptDiscovery;
        _scriptEnhancer = scriptEnhancer;
        _schemaAnalyzer = schemaAnalyzer;
        _repositoryDetector = repositoryDetector;
        _assemblyLoader = assemblyLoader;
    }

    public async Task<DeploymentPlan> GenerateDeploymentPlanAsync(
        List<EntityMetadata> entities,
        string connectionString,
        List<string> assemblyPaths)
    {
        _logger.LogInformation("Generating 29-phase deployment plan");

        var repositoryInfo = await _repositoryDetector.DetectRepositoryAsync();
        var sqlScripts = await _scriptDiscovery.DiscoverScriptsAsync(repositoryInfo);
        var enhancedScripts = await _scriptEnhancer.EnhanceAllScriptsAsync(sqlScripts);

        var deploymentPlan = new DeploymentPlan
        {
            RepositoryInfo = repositoryInfo,
            DomainVersion = ExtractDomainVersion(assemblyPaths),
            GeneratedAt = DateTime.UtcNow
        };

        // Generate all 29 phases
        for (int phase = 1; phase <= 29; phase++)
        {
            var phaseExecution = await GeneratePhaseExecutionAsync(phase, entities, enhancedScripts, connectionString);
            deploymentPlan.Phases.Add(phaseExecution);
        }

        // Calculate totals
        deploymentPlan.TotalStatements = deploymentPlan.Phases.Sum(p => p.Statements.Count);
        deploymentPlan.TotalScripts = deploymentPlan.Phases.Sum(p => p.Scripts.Count);
        deploymentPlan.HasWarnings = deploymentPlan.Phases.Any(p => p.RequiresWarning);

        _logger.LogInformation("Generated deployment plan: {TotalStatements} statements, {TotalScripts} scripts, {PhaseCount} active phases",
            deploymentPlan.TotalStatements, deploymentPlan.TotalScripts,
            deploymentPlan.Phases.Count(p => p.HasContent));

        return deploymentPlan;
    }

    private async Task<PhaseExecution> GeneratePhaseExecutionAsync(
        int phase,
        List<EntityMetadata> entities,
        List<SqlScriptFile> scripts,
        string connectionString)
    {
        var phaseInfo = GetPhaseInfo(phase);
        var phaseScripts = scripts.Where(s => s.Phase == phase).OrderBy(s => s.Order).ToList();
        var phaseExecution = new PhaseExecution
        {
            Phase = phase,
            PhaseInfo = phaseInfo,
            Scripts = phaseScripts,
            RequiresWarning = phaseInfo.RequiresWarning || phaseScripts.Any(s => s.RequiresWarning)
        };

        // Generate entity-driven DDL for specific phases
        switch (phase)
        {
            case 1: // Create Tables
                phaseExecution.Statements.AddRange(await GenerateCreateTablesStatementsAsync(entities, connectionString));
                break;
            case 2: // Primary Key Indexes
                phaseExecution.Statements.AddRange(await GeneratePrimaryKeyIndexStatementsAsync(entities, connectionString));
                break;
            case 3: // Unique Indexes
                phaseExecution.Statements.AddRange(await GenerateUniqueIndexStatementsAsync(entities, connectionString));
                break;
            case 5: // Foreign Key Constraints
                phaseExecution.Statements.AddRange(await GenerateForeignKeyStatementsAsync(entities, connectionString));
                break;
            case 6: // Non-Clustered Indexes
                phaseExecution.Statements.AddRange(await GenerateNonClusteredIndexStatementsAsync(entities, connectionString));
                break;
            case 7: // Composite Indexes
                phaseExecution.Statements.AddRange(await GenerateCompositeIndexStatementsAsync(entities, connectionString));
                break;
        }

        // Add enhanced script content as statements
        foreach (var script in phaseScripts)
        {
            var content = script.EnhancedContent ?? script.Content;
            if (!string.IsNullOrWhiteSpace(content))
            {
                phaseExecution.Statements.Add(content);
            }
        }

        phaseExecution.HasContent = phaseExecution.Statements.Any() || phaseExecution.Scripts.Any();

        if (phaseExecution.HasContent)
        {
            _logger.LogDebug("Phase {Phase}: {StatementCount} statements, {ScriptCount} scripts",
                phase, phaseExecution.Statements.Count, phaseExecution.Scripts.Count);
        }

        return phaseExecution;
    }

    private async Task<List<string>> GenerateCreateTablesStatementsAsync(List<EntityMetadata> entities, string connectionString)
    {
        var statements = new List<string>();

        // Generate schema creation statements first
        var schemas = entities.Select(e => e.Schema).Distinct().ToList();
        foreach (var schema in schemas)
        {
            statements.Add(_ddlGenerator.GenerateCreateSchemaStatement(schema));
        }

        // Generate table creation statements
        foreach (var entity in entities.Where(e => !e.IsIgnored))
        {
            statements.Add(_ddlGenerator.GenerateCreateTableStatement(entity));
        }

        return statements;
    }

    private async Task<List<string>> GeneratePrimaryKeyIndexStatementsAsync(List<EntityMetadata> entities, string connectionString)
    {
        // Primary keys are handled in table creation (Phase 1)
        return new List<string>();
    }

    private async Task<List<string>> GenerateUniqueIndexStatementsAsync(List<EntityMetadata> entities, string connectionString)
    {
        var statements = new List<string>();

        foreach (var entity in entities.Where(e => !e.IsIgnored))
        {
            var uniqueIndexes = entity.Properties
                .Where(p => p.IndexType == SqlIndexType.Unique)
                .ToList();

            foreach (var property in uniqueIndexes)
            {
                var indexName = property.IndexName ?? $"UX_{entity.TableName}_{property.ColumnName}";
                var statement = $"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID('[{entity.Schema}].[{entity.TableName}]') AND name = '{indexName}')\n" +
                               $"BEGIN\n" +
                               $"    CREATE UNIQUE NONCLUSTERED INDEX [{indexName}] ON [{entity.Schema}].[{entity.TableName}] ([{property.ColumnName}])\n" +
                               $"END";
                statements.Add(statement);
            }
        }

        return statements;
    }

    private async Task<List<string>> GenerateForeignKeyStatementsAsync(List<EntityMetadata> entities, string connectionString)
    {
        var statements = new List<string>();

        foreach (var entity in entities.Where(e => !e.IsIgnored))
        {
            statements.AddRange(_ddlGenerator.GenerateForeignKeyStatements(entity));
        }

        return statements;
    }

    private async Task<List<string>> GenerateNonClusteredIndexStatementsAsync(List<EntityMetadata> entities, string connectionString)
    {
        var statements = new List<string>();

        foreach (var entity in entities.Where(e => !e.IsIgnored))
        {
            var nonClusteredIndexes = entity.Properties
                .Where(p => p.IndexType == SqlIndexType.NonClustered && string.IsNullOrEmpty(p.IndexGroup))
                .ToList();

            foreach (var property in nonClusteredIndexes)
            {
                var indexName = property.IndexName ?? $"IX_{entity.TableName}_{property.ColumnName}";
                var statement = $"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID('[{entity.Schema}].[{entity.TableName}]') AND name = '{indexName}')\n" +
                               $"BEGIN\n" +
                               $"    CREATE NONCLUSTERED INDEX [{indexName}] ON [{entity.Schema}].[{entity.TableName}] ([{property.ColumnName}])\n" +
                               $"END";
                statements.Add(statement);
            }
        }

        return statements;
    }

    private async Task<List<string>> GenerateCompositeIndexStatementsAsync(List<EntityMetadata> entities, string connectionString)
    {
        var statements = new List<string>();

        foreach (var entity in entities.Where(e => !e.IsIgnored))
        {
            statements.AddRange(_ddlGenerator.GenerateCompositeIndexStatements(entity));
        }

        return statements;
    }

    private string ExtractDomainVersion(List<string> assemblyPaths)
    {
        try
        {
            if (!assemblyPaths.Any()) return "Unknown";

            var firstAssembly = Assembly.LoadFrom(assemblyPaths.First());
            var version = firstAssembly.GetName().Version;
            return version?.ToString() ?? "Unknown";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not extract domain version from assemblies");
            return "Unknown";
        }
    }

    private PhaseInfo GetPhaseInfo(int phase)
    {
        return phase switch
        {
            1 => new PhaseInfo(1, "create-tables", "Create Tables (Structure Only)", false),
            2 => new PhaseInfo(2, "primary-key-indexes", "Create Primary Key Indexes", false),
            3 => new PhaseInfo(3, "unique-indexes", "Create Unique Indexes", false),
            4 => new PhaseInfo(4, "reference-data", "Insert Reference/Lookup Data", false),
            5 => new PhaseInfo(5, "foreign-key-constraints", "Create Foreign Key Constraints", false),
            6 => new PhaseInfo(6, "non-clustered-indexes", "Create Non-Clustered Indexes", false),
            7 => new PhaseInfo(7, "composite-indexes", "Create Composite Indexes", false),
            8 => new PhaseInfo(8, "filtered-indexes", "Create Filtered Indexes", false),
            9 => new PhaseInfo(9, "computed-columns", "Create Computed Columns", false),
            10 => new PhaseInfo(10, "column-constraints", "Add Column Constraints (Advanced)", false),
            11 => new PhaseInfo(11, "user-defined-types", "Create User-Defined Data Types", false),
            12 => new PhaseInfo(12, "scalar-functions", "Create User-Defined Functions (Scalar)", false),
            13 => new PhaseInfo(13, "table-functions", "Create User-Defined Functions (Table-Valued)", false),
            14 => new PhaseInfo(14, "views", "Create Views", false),
            15 => new PhaseInfo(15, "stored-procedures", "Create Stored Procedures", false),
            16 => new PhaseInfo(16, "triggers", "Create Triggers", true),
            17 => new PhaseInfo(17, "roles", "Create Roles", true),
            18 => new PhaseInfo(18, "users", "Create Users", true),
            19 => new PhaseInfo(19, "object-permissions", "Grant Object Permissions", true),
            20 => new PhaseInfo(20, "schema-permissions", "Grant Schema Permissions", true),
            21 => new PhaseInfo(21, "synonyms", "Create Synonyms", false),
            22 => new PhaseInfo(22, "fulltext", "Create Full-Text Catalogs and Indexes", true),
            23 => new PhaseInfo(23, "partition-functions", "Create Partition Functions and Schemes", true),
            24 => new PhaseInfo(24, "table-partitioning", "Apply Table Partitioning", true),
            25 => new PhaseInfo(25, "database-options", "Set Database Options", true),
            26 => new PhaseInfo(26, "update-statistics", "Update Statistics", false),
            27 => new PhaseInfo(27, "data-validation", "Run Data Validation Scripts", true),
            28 => new PhaseInfo(28, "documentation", "Create Database Documentation", false),
            29 => new PhaseInfo(29, "maintenance", "Final Maintenance Tasks", true),
            _ => new PhaseInfo(phase, $"phase-{phase}", $"Custom Phase {phase}", true)
        };
    }

    public string FormatDeploymentPlan(DeploymentPlan plan)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== 29-Phase Deployment Plan ===");
        sb.AppendLine($"Repository: {plan.RepositoryInfo.Name}");
        sb.AppendLine($"Domain Version: {plan.DomainVersion}");
        sb.AppendLine($"Generated: {plan.GeneratedAt:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine();

        var activePhases = plan.Phases.Where(p => p.HasContent).ToList();

        foreach (var phase in activePhases)
        {
            var warningIcon = phase.RequiresWarning ? " ⚠️" : "";
            sb.AppendLine($"Phase {phase.Phase}: {phase.PhaseInfo.Description}{warningIcon}");

            if (phase.Statements.Any())
            {
                sb.AppendLine($"  • {phase.Statements.Count} DDL statements from entities");
            }

            if (phase.Scripts.Any())
            {
                sb.AppendLine($"  • {phase.Scripts.Count} custom scripts:");
                foreach (var script in phase.Scripts.Take(3))
                {
                    var status = script.IsNew ? " (NEW)" : script.IsModified ? " (MODIFIED)" : "";
                    sb.AppendLine($"    - {script.FileName}{status}");
                }
                if (phase.Scripts.Count > 3)
                {
                    sb.AppendLine($"    - ... and {phase.Scripts.Count - 3} more");
                }
            }

            sb.AppendLine();
        }

        sb.AppendLine($"Total: {plan.TotalStatements} operations across {activePhases.Count} phases");

        if (plan.HasWarnings)
        {
            sb.AppendLine();
            sb.AppendLine("⚠️  WARNING: Some phases require manual approval in production");
        }

        return sb.ToString();
    }
}

public class DeploymentPlan
{
    public RepositoryInfo RepositoryInfo { get; set; } = new RepositoryInfo();
    public string DomainVersion { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public List<PhaseExecution> Phases { get; set; } = new List<PhaseExecution>();
    public int TotalStatements { get; set; }
    public int TotalScripts { get; set; }
    public bool HasWarnings { get; set; }
}

public class PhaseExecution
{
    public int Phase { get; set; }
    public PhaseInfo PhaseInfo { get; set; } = new PhaseInfo(0, "", "", false);
    public List<string> Statements { get; set; } = new List<string>();
    public List<SqlScriptFile> Scripts { get; set; } = new List<SqlScriptFile>();
    public bool RequiresWarning { get; set; }
    public bool HasContent { get; set; }
}