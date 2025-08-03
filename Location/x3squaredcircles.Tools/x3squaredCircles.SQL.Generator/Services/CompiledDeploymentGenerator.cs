using Microsoft.Extensions.Logging;
using System.Text;

namespace x3squaredcirecles.SQLSync.Generator.Services;

public class CompiledDeploymentGenerator
{
    private readonly ILogger<CompiledDeploymentGenerator> _logger;
    private readonly RepositoryDetector _repositoryDetector;

    public CompiledDeploymentGenerator(ILogger<CompiledDeploymentGenerator> logger, RepositoryDetector repositoryDetector)
    {
        _logger = logger;
        _repositoryDetector = repositoryDetector;
    }

    public async Task<string> GenerateCompiledDeploymentAsync(DeploymentPlan plan, bool isProduction = false)
    {
        _logger.LogInformation("Generating compiled deployment for version {Version}", plan.DomainVersion);

        var compiledSql = GenerateCompiledSql(plan, isProduction);
        var fileName = $"_compiled_deployment_v{plan.DomainVersion}.sql";
        var compiledSqlPath = _repositoryDetector.GetCompiledSqlPath(plan.RepositoryInfo, "");
        var filePath = Path.Combine(compiledSqlPath, fileName);

        await File.WriteAllTextAsync(filePath, compiledSql);

        _logger.LogInformation("Compiled deployment saved: {FilePath}", filePath);
        return filePath;
    }

    private string GenerateCompiledSql(DeploymentPlan plan, bool isProduction)
    {
        var sb = new StringBuilder();

        // Header
        sb.AppendLine(GenerateHeader(plan, isProduction));

        // Transaction wrapper for production
        if (isProduction)
        {
            sb.AppendLine("BEGIN TRANSACTION DeploymentTransaction");
            sb.AppendLine("BEGIN TRY");
            sb.AppendLine();
        }

        // Generate each phase
        var activePhases = plan.Phases.Where(p => p.HasContent).ToList();

        foreach (var phase in activePhases)
        {
            sb.AppendLine(GeneratePhaseSection(phase));
        }

        // Transaction completion for production
        if (isProduction)
        {
            sb.AppendLine();
            sb.AppendLine("    COMMIT TRANSACTION DeploymentTransaction");
            sb.AppendLine("    PRINT 'Deployment completed successfully'");
            sb.AppendLine("END TRY");
            sb.AppendLine("BEGIN CATCH");
            sb.AppendLine("    ROLLBACK TRANSACTION DeploymentTransaction");
            sb.AppendLine("    DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE()");
            sb.AppendLine("    DECLARE @ErrorSeverity INT = ERROR_SEVERITY()");
            sb.AppendLine("    DECLARE @ErrorState INT = ERROR_STATE()");
            sb.AppendLine("    PRINT 'Deployment failed: ' + @ErrorMessage");
            sb.AppendLine("    RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState)");
            sb.AppendLine("END CATCH");
        }

        // Footer
        sb.AppendLine(GenerateFooter(plan));

        return sb.ToString();
    }

    private string GenerateHeader(DeploymentPlan plan, bool isProduction)
    {
        var sb = new StringBuilder();

        sb.AppendLine("-- ========== COMPILED DEPLOYMENT ==========");
        sb.AppendLine($"-- Version: {plan.DomainVersion}");
        sb.AppendLine($"-- Repository: {plan.RepositoryInfo.Name}");
        sb.AppendLine($"-- Generated: {plan.GeneratedAt:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"-- Mode: {(isProduction ? "PRODUCTION" : "DEVELOPMENT")}");
        sb.AppendLine($"-- Total Phases: {plan.Phases.Count(p => p.HasContent)}");
        sb.AppendLine($"-- Total Statements: {plan.TotalStatements}");
        sb.AppendLine($"-- Total Scripts: {plan.TotalScripts}");

        if (plan.HasWarnings)
        {
            sb.AppendLine("-- ⚠️  WARNING: Contains operations requiring manual approval");
        }

        sb.AppendLine("-- ==========================================");
        sb.AppendLine();

        // Pre-deployment checks
        sb.AppendLine("-- Pre-deployment validation");
        sb.AppendLine("IF @@TRANCOUNT > 0");
        sb.AppendLine("BEGIN");
        sb.AppendLine("    RAISERROR('Active transaction detected. Please commit or rollback before running deployment.', 16, 1)");
        sb.AppendLine("    RETURN");
        sb.AppendLine("END");
        sb.AppendLine();

        sb.AppendLine($"PRINT 'Starting deployment v{plan.DomainVersion} at ' + CONVERT(VARCHAR, GETDATE(), 120)");
        sb.AppendLine();

        return sb.ToString();
    }

    private string GeneratePhaseSection(PhaseExecution phase)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"-- ========================================");
        sb.AppendLine($"-- Phase {phase.Phase}: {phase.PhaseInfo.Description}");

        if (phase.RequiresWarning)
        {
            sb.AppendLine("-- ⚠️  WARNING: Requires manual approval");
        }

        sb.AppendLine($"-- Statements: {phase.Statements.Count}");
        sb.AppendLine($"-- Scripts: {phase.Scripts.Count}");
        sb.AppendLine($"-- ========================================");
        sb.AppendLine();

        sb.AppendLine($"PRINT 'Phase {phase.Phase}: {phase.PhaseInfo.Description}'");
        sb.AppendLine();

        // Add phase timing
        sb.AppendLine($"DECLARE @Phase{phase.Phase}StartTime DATETIME2 = GETDATE()");
        sb.AppendLine();

        // Execute all statements for this phase
        foreach (var statement in phase.Statements)
        {
            sb.AppendLine(EnsureStatementHasGo(statement));
            sb.AppendLine();
        }

        // Phase completion
        sb.AppendLine($"DECLARE @Phase{phase.Phase}Duration INT = DATEDIFF(MILLISECOND, @Phase{phase.Phase}StartTime, GETDATE())");
        sb.AppendLine($"PRINT 'Phase {phase.Phase} completed in ' + CAST(@Phase{phase.Phase}Duration AS VARCHAR) + ' ms'");
        sb.AppendLine();

        return sb.ToString();
    }

    private string GenerateFooter(DeploymentPlan plan)
    {
        var sb = new StringBuilder();

        sb.AppendLine("-- ==========================================");
        sb.AppendLine("-- Deployment Summary");
        sb.AppendLine("-- ==========================================");
        sb.AppendLine();

        sb.AppendLine($"PRINT 'Deployment v{plan.DomainVersion} completed successfully at ' + CONVERT(VARCHAR, GETDATE(), 120)");
        sb.AppendLine();

        // Record deployment in metadata table (if exists)
        sb.AppendLine("-- Record deployment metadata (if table exists)");
        sb.AppendLine("IF EXISTS (SELECT * FROM sys.tables WHERE name = 'DeploymentHistory')");
        sb.AppendLine("BEGIN");
        sb.AppendLine("    INSERT INTO DeploymentHistory (Version, Repository, DeployedAt, TotalStatements, TotalScripts)");
        sb.AppendLine($"    VALUES ('{plan.DomainVersion}', '{plan.RepositoryInfo.Name}', GETDATE(), {plan.TotalStatements}, {plan.TotalScripts})");
        sb.AppendLine("END");
        sb.AppendLine();

        sb.AppendLine($"-- End of compiled deployment v{plan.DomainVersion}");

        return sb.ToString();
    }

    private string EnsureStatementHasGo(string statement)
    {
        var trimmed = statement.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return trimmed;

        // Don't add GO if it already ends with GO
        if (trimmed.EndsWith("GO", StringComparison.OrdinalIgnoreCase))
            return trimmed;

        // Don't add GO for simple statements that don't need it
        if (IsSimpleStatement(trimmed))
            return trimmed;

        return trimmed + "\nGO";
    }

    private bool IsSimpleStatement(string statement)
    {
        var upperStatement = statement.ToUpperInvariant().Trim();

        // Simple statements that don't typically need GO
        var simplePatterns = new[]
        {
            "PRINT ",
            "DECLARE ",
            "SET ",
            "SELECT ",
            "INSERT INTO",
            "UPDATE ",
            "DELETE FROM"
        };

        return simplePatterns.Any(pattern => upperStatement.StartsWith(pattern));
    }

    public async Task<string> GenerateDeploymentSummaryAsync(DeploymentPlan plan, string compiledDeploymentPath)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"# Deployment Summary v{plan.DomainVersion}");
        sb.AppendLine();
        sb.AppendLine($"**Repository:** {plan.RepositoryInfo.Name}");
        sb.AppendLine($"**Generated:** {plan.GeneratedAt:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"**Compiled SQL:** `{Path.GetFileName(compiledDeploymentPath)}`");
        sb.AppendLine();

        sb.AppendLine("## Phase Summary");
        sb.AppendLine();

        var activePhases = plan.Phases.Where(p => p.HasContent).ToList();

        foreach (var phase in activePhases)
        {
            var warningIcon = phase.RequiresWarning ? " ⚠️" : " ✅";
            sb.AppendLine($"- **Phase {phase.Phase}:** {phase.PhaseInfo.Description}{warningIcon}");

            if (phase.Statements.Any())
            {
                sb.AppendLine($"  - {phase.Statements.Count} DDL statements");
            }

            if (phase.Scripts.Any())
            {
                sb.AppendLine($"  - {phase.Scripts.Count} custom scripts");
                foreach (var script in phase.Scripts)
                {
                    var status = script.IsNew ? " (NEW)" : script.IsModified ? " (MODIFIED)" : "";
                    sb.AppendLine($"    - `{script.FileName}`{status}");
                }
            }
        }

        sb.AppendLine();
        sb.AppendLine("## Totals");
        sb.AppendLine($"- **Total Operations:** {plan.TotalStatements}");
        sb.AppendLine($"- **Total Scripts:** {plan.TotalScripts}");
        sb.AppendLine($"- **Active Phases:** {activePhases.Count}/29");

        if (plan.HasWarnings)
        {
            sb.AppendLine();
            sb.AppendLine("## ⚠️ Warnings");
            sb.AppendLine("This deployment contains operations that require manual approval in production environments.");
        }

        var summaryPath = Path.ChangeExtension(compiledDeploymentPath, ".md");
        await File.WriteAllTextAsync(summaryPath, sb.ToString());

        _logger.LogInformation("Deployment summary saved: {SummaryPath}", summaryPath);
        return summaryPath;
    }

    public async Task CleanupOldCompiledDeploymentsAsync(RepositoryInfo repositoryInfo, int keepCount = 10)
    {
        try
        {
            var compiledSqlPath = _repositoryDetector.GetCompiledSqlPath(repositoryInfo, "");
            var compiledFiles = Directory.GetFiles(compiledSqlPath, "_compiled_deployment_*.sql")
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.CreationTime)
                .ToList();

            if (compiledFiles.Count <= keepCount)
            {
                _logger.LogDebug("No cleanup needed - {Count} compiled deployments (keeping {KeepCount})",
                    compiledFiles.Count, keepCount);
                return;
            }

            var filesToDelete = compiledFiles.Skip(keepCount).ToList();

            foreach (var file in filesToDelete)
            {
                File.Delete(file.FullName);

                // Also delete corresponding .md file if it exists
                var mdFile = Path.ChangeExtension(file.FullName, ".md");
                if (File.Exists(mdFile))
                {
                    File.Delete(mdFile);
                }

                _logger.LogDebug("Deleted old compiled deployment: {FileName}", file.Name);
            }

            _logger.LogInformation("Cleaned up {DeletedCount} old compiled deployments, kept {KeptCount}",
                filesToDelete.Count, keepCount);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cleanup old compiled deployments");
        }
    }
}