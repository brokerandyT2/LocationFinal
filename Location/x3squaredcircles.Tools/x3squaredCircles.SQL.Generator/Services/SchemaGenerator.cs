using Location.Core.Helpers.CodeGenerationAttributes;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace x3squaredcirecles.SQLSync.Generator.Services;

public class SchemaGenerator
{
    private readonly AssemblyLoader _assemblyLoader;
    private readonly ILogger<SchemaGenerator> _logger;
    private readonly DdlStatementGenerator _ddlGenerator;
    private readonly DatabaseSchemaAnalyzer _schemaAnalyzer;
    private readonly DeploymentOrchestrator _deploymentOrchestrator;
    private readonly CompiledDeploymentGenerator _compiledDeploymentGenerator;
    private readonly GitIntegrationService _gitIntegrationService;
    private readonly RepositoryDetector _repositoryDetector;

    public SchemaGenerator(
        ILogger<SchemaGenerator> logger,
        DdlStatementGenerator ddlGenerator,
        DatabaseSchemaAnalyzer schemaAnalyzer,
        DeploymentOrchestrator deploymentOrchestrator,
        CompiledDeploymentGenerator compiledDeploymentGenerator,
        GitIntegrationService gitIntegrationService,
        RepositoryDetector repositoryDetector,
        AssemblyLoader assemblyLoader)
    {
        _logger = logger;
        _ddlGenerator = ddlGenerator;
        _schemaAnalyzer = schemaAnalyzer;
        _deploymentOrchestrator = deploymentOrchestrator;
        _compiledDeploymentGenerator = compiledDeploymentGenerator;
        _gitIntegrationService = gitIntegrationService;
        _repositoryDetector = repositoryDetector;
        _assemblyLoader = assemblyLoader;
    }

    public List<string> GenerateCreateTableStatements(List<EntityMetadata> entities)
    {
        _logger.LogDebug("Generating create table statements for {Count} entities", entities.Count);

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

        _logger.LogInformation("Generated {Count} create table statements", statements.Count);
        return statements;
    }

    public async Task<List<string>> GenerateDeltaDDLAsync(List<EntityMetadata> entities, string connectionString)
    {
        _logger.LogInformation("Generating delta DDL for {Count} entities", entities.Count);

        var deltaStatements = new List<string>();

        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        // Get existing database objects
        var existingSchemas = await _schemaAnalyzer.GetExistingSchemasAsync(connection);
        var existingTables = await _schemaAnalyzer.GetExistingTablesAsync(connection);
        var existingColumns = await _schemaAnalyzer.GetExistingColumnsAsync(connection);
        var existingIndexes = await _schemaAnalyzer.GetExistingIndexesAsync(connection);
        var existingForeignKeys = await _schemaAnalyzer.GetExistingForeignKeysAsync(connection);

        _logger.LogDebug("Found {SchemaCount} schemas, {TableCount} tables, {ColumnCount} columns, {IndexCount} indexes, {FKCount} foreign keys",
            existingSchemas.Count, existingTables.Count, existingColumns.Count, existingIndexes.Count, existingForeignKeys.Count);

        // Generate schema creation statements for missing schemas
        var requiredSchemas = entities.Select(e => e.Schema).Distinct().ToList();
        foreach (var schema in requiredSchemas)
        {
            if (!existingSchemas.Contains(schema))
            {
                deltaStatements.Add(_ddlGenerator.GenerateCreateSchemaStatement(schema));
                _logger.LogDebug("Schema {Schema} needs to be created", schema);
            }
        }

        // Generate table and related DDL for each entity
        foreach (var entity in entities.Where(e => !e.IsIgnored))
        {
            var tableKey = $"{entity.Schema}.{entity.TableName}";

            // Check if table exists
            if (!existingTables.ContainsKey(tableKey))
            {
                // Table doesn't exist - generate full CREATE TABLE statement
                deltaStatements.Add(_ddlGenerator.GenerateCreateTableStatement(entity));
                _logger.LogDebug("Table {Table} needs to be created", tableKey);

                // Add all indexes and foreign keys since table is new
                deltaStatements.AddRange(_ddlGenerator.GenerateSingleColumnIndexStatements(entity));
                deltaStatements.AddRange(_ddlGenerator.GenerateCompositeIndexStatements(entity));
                deltaStatements.AddRange(_ddlGenerator.GenerateForeignKeyStatements(entity));
            }
            else
            {
                // Table exists - check for missing columns
                var tableColumns = existingColumns.ContainsKey(tableKey) ? existingColumns[tableKey] : new List<string>();

                foreach (var property in entity.Properties.Where(p => !p.IsIgnored))
                {
                    if (!tableColumns.Contains(property.ColumnName))
                    {
                        var addColumnStatement = _ddlGenerator.GenerateAddColumnStatement(entity, property);
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
                        var indexStatements = _ddlGenerator.GenerateSingleColumnIndexStatements(entity);
                        deltaStatements.AddRange(indexStatements.Where(stmt => stmt.Contains(indexName)));
                        _logger.LogDebug("Index {Index} needs to be created", indexName);
                    }
                }

                // Composite indexes
                foreach (var index in entity.CompositeIndexes)
                {
                    if (!tableIndexes.Contains(index.Name))
                    {
                        var compositeIndexStatements = _ddlGenerator.GenerateCompositeIndexStatements(entity);
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
                        var foreignKeyStatements = _ddlGenerator.GenerateForeignKeyStatements(entity);
                        deltaStatements.AddRange(foreignKeyStatements.Where(stmt => stmt.Contains(constraintName)));
                        _logger.LogDebug("Foreign key {FK} needs to be created", constraintName);
                    }
                }
            }
        }

        _logger.LogInformation("Generated {Count} delta DDL statements", deltaStatements.Count);
        return deltaStatements;
    }

    public async Task<string> GenerateFullDeploymentAsync(
        List<EntityMetadata> entities,
        string connectionString,
        List<string> assemblyPaths,
        bool isProduction = false)
    {
        _logger.LogInformation("Generating full 29-phase deployment (Production: {IsProduction})", isProduction);

        // Generate the complete deployment plan
        var deploymentPlan = await _deploymentOrchestrator.GenerateDeploymentPlanAsync(entities, connectionString, assemblyPaths);

        // Generate compiled deployment
        var compiledDeploymentPath = await _compiledDeploymentGenerator.GenerateCompiledDeploymentAsync(deploymentPlan, isProduction);

        // Generate deployment summary
        await _compiledDeploymentGenerator.GenerateDeploymentSummaryAsync(deploymentPlan, compiledDeploymentPath);

        // If this is a successful production deployment, consume scripts and commit
        if (isProduction && deploymentPlan.TotalStatements > 0)
        {
            await ConsumeScriptsAndCommitAsync(deploymentPlan, compiledDeploymentPath, assemblyPaths);
        }

        _logger.LogInformation("Full deployment generation completed: {CompiledDeploymentPath}", compiledDeploymentPath);
        return compiledDeploymentPath;
    }

    private async Task ConsumeScriptsAndCommitAsync(DeploymentPlan deploymentPlan, string compiledDeploymentPath, List<string> assemblyPaths)
    {
        try
        {
            _logger.LogInformation("Consuming scripts and committing to git for version {Version}", deploymentPlan.DomainVersion);

            // Determine the schema from the first assembly
            var schemaName = "Unknown";
            if (assemblyPaths.Any())
            {
                schemaName = _assemblyLoader.DetermineSchemaFromAssembly(assemblyPaths.First());
            }

            // Get the SqlScripts path for this schema
            var sqlScriptsPath = _repositoryDetector.GetSqlScriptsPath(deploymentPlan.RepositoryInfo, schemaName, assemblyPaths.FirstOrDefault());

            // Consume scripts and commit
            await _gitIntegrationService.ConsumeAndCommitScriptsAsync(
                sqlScriptsPath,
                compiledDeploymentPath,
                deploymentPlan.DomainVersion,
                schemaName);

            // Cleanup old compiled deployments
            await _compiledDeploymentGenerator.CleanupOldCompiledDeploymentsAsync(deploymentPlan.RepositoryInfo);

            _logger.LogInformation("Successfully consumed scripts and committed changes for version {Version}", deploymentPlan.DomainVersion);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to consume scripts and commit changes");
            // Don't throw - deployment was successful, git commit failure shouldn't fail the deployment
        }
    }

    public async Task<string> GenerateNoOpAnalysisAsync(
        List<EntityMetadata> entities,
        string connectionString,
        List<string> assemblyPaths)
    {
        _logger.LogInformation("Generating no-op analysis and deployment plan");

        // Generate the complete deployment plan
        var deploymentPlan = await _deploymentOrchestrator.GenerateDeploymentPlanAsync(entities, connectionString, assemblyPaths);

        // Format the deployment plan for logging
        var formattedPlan = _deploymentOrchestrator.FormatDeploymentPlan(deploymentPlan);

        // Generate a preview compiled deployment (for analysis purposes)
        var previewPath = await _compiledDeploymentGenerator.GenerateCompiledDeploymentAsync(deploymentPlan, false);

        _logger.LogInformation("No-op analysis completed. Preview deployment: {PreviewPath}", previewPath);

        // Log the formatted plan
        Console.WriteLine(formattedPlan);

        return formattedPlan;
    }

    public async Task<bool> HasPendingChangesAsync(List<EntityMetadata> entities, string connectionString, List<string> assemblyPaths)
    {
        _logger.LogDebug("Checking for pending database changes");

        try
        {
            var deploymentPlan = await _deploymentOrchestrator.GenerateDeploymentPlanAsync(entities, connectionString, assemblyPaths);
            var hasPendingChanges = deploymentPlan.TotalStatements > 0 || deploymentPlan.TotalScripts > 0;

            _logger.LogDebug("Pending changes check: {HasChanges} ({StatementCount} statements, {ScriptCount} scripts)",
                hasPendingChanges, deploymentPlan.TotalStatements, deploymentPlan.TotalScripts);

            return hasPendingChanges;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not check for pending changes");
            return false;
        }
    }
}