# SQL Server Sync Generator

**The Complete Database Deployment Solution** - Automated SQL Server schema generation from .NET Domain entities with comprehensive SQL script management, Azure Key Vault integration, and enterprise-grade safety features. Generates tables, indexes, foreign keys, and executes custom SQL scripts in proper dependency order for pipeline-driven database deployments.

## 🎯 What Makes This Different

- **Domain-Driven Versioning**: Database schema stays perfectly synchronized with your Domain entity versions
- **29-Phase Execution**: Complete database deployment pipeline from tables to triggers to permissions
- **Compiled Deployments**: Every deployment creates a single, auditable SQL file containing exactly what was executed
- **Enterprise Safety**: Azure-native point-in-time restore, comprehensive validation, automatic rollback
- **Zero-Config MSBuild**: Auto-runs after Domain builds with intelligent defaults

## 🚀 Quick Start

### Installation & Auto-Setup

```bash
# Build and install as global tool with MSBuild integration
dotnet pack
dotnet tool install -g --add-source ./bin/Debug SQLServerSyncGenerator
```

**🎉 That's it!** The tool now auto-runs after Domain project builds in **safe `--noop` mode** by default.

### Developer Experience (Auto-Run)

When you build any `*Domain` project, the tool automatically:

1. ✅ **Analyzes your database schema** against Domain entities
2. ✅ **Discovers custom SQL scripts** in SqlScripts folders
3. ✅ **Shows complete deployment plan** (29-phase execution order)
4. ✅ **Never modifies your database** unless explicitly configured
5. ✅ **Provides immediate feedback** on schema drift and pending changes

### Example Build Output
```
[SqlSchemaGenerator] Analyzing database schema after Domain build...
[SqlSchemaGenerator] Project: Location.Photography.Domain
[SqlSchemaGenerator] Mode: noop (safe default for developers)

=== 29-Phase Deployment Plan ===
Phase 1: Create Tables - 3 new tables from entities
Phase 4: Reference Data - 2 scripts in SqlScripts/04-reference-data/
Phase 15: Stored Procedures - 5 scripts in SqlScripts/15-stored-procedures/
Phase 16: Triggers - 1 script (⚠️ WARNING: Adding to existing table)

Total: 11 operations would be executed in single transaction
Compiled deployment would be: _compiled_deployment_v1.2.4.sql

[SqlSchemaGenerator] Analysis completed - use --execute to apply changes
```

## 📁 Project Structure

### Domain Entities (Your Existing Code)
```
Location.Photography.Domain/
├── Entities/
│   ├── CameraBody.cs       # [ExportToSQL] entities
│   └── Lens.cs            # Drive table generation
└── bin/Debug/net9.0/
    └── Location.Photography.Domain.dll  # v1.2.4
```

### Custom SQL Scripts (New Structure)
```
Location.Photography.Infrastructure.Repositories.SqlScripts/
├── 04-reference-data/
│   ├── 001_CameraBrands.sql
│   └── 002_LensTypes.sql
├── 12-scalar-functions/
│   └── CalculateFocalLength.sql
├── 14-views/
│   └── CameraAnalyticsView.sql
├── 15-stored-procedures/
│   ├── 001_GetCamerasByBrand.sql
│   └── 002_UpdateCameraMetadata.sql
├── 16-triggers/
│   └── CameraAuditTrigger.sql
└── _compiled_deployment_v1.2.3.sql  # Previous deployment
```

## 🔄 The Deployment Lifecycle

### Phase 1: Development
```bash
# Developer adds new entity property
public class CameraBody 
{
    public string SensorSize { get; set; }  # New property
}

# Developer adds related stored procedure
# SqlScripts/15-stored-procedures/GetCamerasBySensorSize.sql

# Build triggers auto-analysis
dotnet build  # Shows deployment plan in noop mode
```

### Phase 2: Deployment
```bash
# Production deployment with full safety
sql-schema-generator \
  --execute \
  --prod \
  --server "prod-server.database.windows.net" \
  --database "LocationAnalytics" \
  --keyvault-url "https://vault.vault.azure.net/" \
  --username-secret "sql-username" \
  --password-secret "sql-password"
```

### Phase 3: Compiled Deployment Creation
**After successful deployment:**
```sql
-- _compiled_deployment_v1.2.4.sql (auto-generated)
-- ========== COMPILED DEPLOYMENT v1.2.4 ==========
-- Domain Assembly: Location.Photography.Domain.dll v1.2.4
-- Entity Changes: Added SensorSize to CameraBody
-- Custom Scripts: 1 new stored procedure
-- Deployment Time: 2025-08-02T14:30:00Z

BEGIN TRANSACTION DeploymentTransaction
BEGIN TRY
    -- Phase 1: Entity-driven DDL
    ALTER TABLE [Photography].[CameraBodies] ADD [SensorSize] NVARCHAR(50) NULL
    
    -- Phase 15: Custom stored procedures (enhanced with guard clauses)
    IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'GetCamerasBySensorSize')
        DROP PROCEDURE GetCamerasBySensorSize
    GO
    CREATE PROCEDURE GetCamerasBySensorSize @SensorSize NVARCHAR(50)
    AS BEGIN
        SELECT * FROM Photography.CameraBodies WHERE SensorSize = @SensorSize
    END
    
    COMMIT TRANSACTION DeploymentTransaction
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION DeploymentTransaction
    THROW
END CATCH
```

### Phase 4: Repository State After Deployment
```
Location.Photography.Infrastructure.Repositories.SqlScripts/
└── _compiled_deployment_v1.2.4.sql  # Raw scripts consumed, compiled version remains
```

## 🎛️ 29-Phase Execution Order

The tool executes database changes in this precise order to ensure all dependencies are met:

1. **Create Tables (Structure Only)** - Entity-driven table creation
2. **Create Primary Key Indexes** - Essential constraints first  
3. **Create Unique Indexes** - Data integrity before population
4. **Insert Reference/Lookup Data** - SqlScripts/04-reference-data/
5. **Create Foreign Key Constraints** - After referenced data exists
6. **Create Non-Clustered Indexes** - Performance optimization
7. **Create Composite Indexes** - Complex indexing strategies
8. **Create Filtered Indexes** - Specialized indexes
9. **Create Computed Columns** - Derived data columns
10. **Add Column Constraints (Advanced)** - Complex constraints
11. **Create User-Defined Data Types** - SqlScripts/11-user-defined-types/
12. **Create User-Defined Functions (Scalar)** - SqlScripts/12-scalar-functions/
13. **Create User-Defined Functions (Table-Valued)** - SqlScripts/13-table-functions/
14. **Create Views** - SqlScripts/14-views/
15. **Create Stored Procedures** - SqlScripts/15-stored-procedures/
16. **Create Triggers** ⚠️ - SqlScripts/16-triggers/
17. **Create Roles** ⚠️ - SqlScripts/17-roles/
18. **Create Users** ⚠️ - SqlScripts/18-users/
19. **Grant Object Permissions** ⚠️ - SqlScripts/19-object-permissions/
20. **Grant Schema Permissions** ⚠️ - SqlScripts/20-schema-permissions/
21. **Create Synonyms** - SqlScripts/21-synonyms/
22. **Create Full-Text Catalogs and Indexes** ⚠️ - SqlScripts/22-fulltext/
23. **Create Partition Functions and Schemes** ⚠️ - SqlScripts/23-partition-functions/
24. **Apply Table Partitioning** ⚠️ - SqlScripts/24-table-partitioning/
25. **Set Database Options** ⚠️ - SqlScripts/25-database-options/
26. **Update Statistics** - SqlScripts/26-update-statistics/
27. **Run Data Validation Scripts** ⚠️ - SqlScripts/27-data-validation/
28. **Create Database Documentation** - SqlScripts/28-documentation/
29. **Final Maintenance Tasks** ⚠️ - SqlScripts/29-maintenance/

⚠️ = **Auto-generates warnings** (requires manual approval in production pipelines)

## 🛡️ Enterprise Safety Features

### Comprehensive Validation System
```bash
# Validation-only mode (perfect for pipeline gates)
sql-schema-generator \
  --validate-only \
  --prod \
  [connection-params]

# Exit codes:
# 0 = Safe (auto-deploy)
# 1 = Warnings (manual approval recommended) 
# 2 = Blocked (unsafe changes detected)
```

### Validation Categories

**🚫 Blocking Issues (Exit Code 2)**
- `DROP TABLE` - Permanent data loss
- `DROP COLUMN` - Permanent data loss  
- `TRUNCATE TABLE` - Permanent data loss

**⚠️ Warning Issues (Exit Code 1)**
- Triggers on existing tables (new tables OK)
- Security/permissions changes
- Structural changes (partitioning, database options)
- Performance-impacting operations

**✅ Safe Operations (Exit Code 0)**
- New tables, views, procedures
- Adding nullable columns
- Creating indexes on new tables

### Azure-Native Point-in-Time Restore
```bash
# Production deployment with automatic rollback protection
sql-schema-generator \
  --prod \
  [connection-params]

# What happens:
# 1. ✅ Records restore point timestamp
# 2. 🚀 Applies all changes in single transaction
# 3. ✅ Success: Creates compiled deployment file
# 4. ❌ Failure: Automatic Azure point-in-time restore
```

### Emergency Recovery
```bash
# Quick rollback to previous deployment
sql-schema-generator \
  --rollback-to-previous \
  [connection-params]

# Rollback to specific version
sql-schema-generator \
  --restore-from "v1.2.3" \
  [connection-params]

# List available restore points
sql-schema-generator \
  --deployment-history \
  [connection-params]
```

## 🧬 Domain-Driven Versioning

### Perfect Synchronization
```
Domain Entity Change → Assembly Version Bump → Deployment Version
    ↓                        ↓                        ↓
CameraBody.cs            v1.2.4                 v1.2.4
    ↓                        ↓                        ↓
New Property         New DDL Generated    _compiled_deployment_v1.2.4.sql
```

### Audit Trail Perfection
```bash
# See complete deployment history
git log --oneline
abc123 Auto-deploy: Compiled deployment v1.2.4
def456 Auto-deploy: Compiled deployment v1.2.3  
ghi789 Auto-deploy: Compiled deployment v1.2.2

# See exact changes in any deployment
git show abc123  # Shows entity changes + compiled SQL
```

## 📊 Script Enhancement Engine

### Automatic Idempotent Wrapping
**Developer writes:**
```sql
CREATE PROCEDURE GetCamerasByBrand @Brand NVARCHAR(50)
AS BEGIN
    SELECT * FROM Photography.CameraBodies WHERE Brand = @Brand
END
```

**Tool enhances to:**
```sql
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'GetCamerasByBrand')
    DROP PROCEDURE GetCamerasByBrand
GO
CREATE PROCEDURE GetCamerasByBrand @Brand NVARCHAR(50)
AS BEGIN
    SELECT * FROM Photography.CameraBodies WHERE Brand = @Brand
END
```

### Script Classification
- **New scripts**: No warnings (safe to deploy)
- **Updated scripts**: Automatic warnings for manual review
- **Complex scripts**: Enhanced validation and dependency checking

## ⚙️ Configuration

### MSBuild Integration (Auto-Configuration)
```xml
<!-- Directory.Build.props - Team-wide settings -->
<PropertyGroup>
  <!-- Auto-enable for all Domain projects -->
  <SqlSchemaGeneratorDatabase>LocationDev_$(USERNAME)</SqlSchemaGeneratorDatabase>
  
  <!-- Development with local SQL Server -->
  <SqlSchemaGeneratorServer>localhost\SQLEXPRESS</SqlSchemaGeneratorServer>
  <SqlSchemaGeneratorUseLocal>true</SqlSchemaGeneratorUseLocal>
  
  <!-- Or Azure for shared dev environment -->
  <SqlSchemaGeneratorUseLocal>false</SqlSchemaGeneratorUseLocal>
  <SqlSchemaGeneratorKeyVaultUrl>https://dev-vault.vault.azure.net/</SqlSchemaGeneratorKeyVaultUrl>
  <SqlSchemaGeneratorUsernameSecret>dev-sql-username</SqlSchemaGeneratorUsernameSecret>
  <SqlSchemaGeneratorPasswordSecret>dev-sql-password</SqlSchemaGeneratorPasswordSecret>
</PropertyGroup>
```

### Production Deployment
```bash
# Azure SQL Database with Key Vault
sql-schema-generator \
  --execute \
  --prod \
  --server "myserver.database.windows.net" \
  --database "LocationAnalytics" \
  --keyvault-url "https://vault.vault.azure.net/" \
  --username-secret "sql-username" \
  --password-secret "sql-password"
```

### Local Development
```bash
# SQL Server Express with Windows Authentication
sql-schema-generator \
  --execute \
  --server "localhost\SQLEXPRESS" \
  --database "LocationDev" \
  --local
```

## 🔗 Pipeline Integration

### Azure DevOps with Conditional Approval
```yaml
- stage: SchemaValidation
  jobs:
  - job: ValidateSchema
    steps:
    - task: PowerShell@2
      displayName: 'Pre-flight Schema Validation'
      inputs:
        script: |
          $output = & sql-schema-generator --validate-only --prod [params] 2>&1
          $exitCode = $LASTEXITCODE
          Write-Host "##vso[task.setvariable variable=ValidationResult]$exitCode"
          
          switch ($exitCode) {
            0 { Write-Host "✅ Safe for automatic deployment" }
            1 { Write-Host "⚠️ Manual approval required" }
            2 { Write-Host "❌ Deployment blocked"; exit 1 }
          }
    
    - task: ManualValidation@0
      displayName: 'DBA Approval Required'
      condition: eq(variables['ValidationResult'], '1')
      inputs:
        instructions: |
          ⚠️ Schema validation detected warnings. Review and approve:
          • Security/permissions changes detected
          • Performance impact possible
          • Structural database changes
          
          Deployment includes automatic rollback protection.

- stage: ProductionDeploy
  dependsOn: SchemaValidation
  jobs:
  - deployment: ApplySchemaChanges
    environment: 'Production-Database'
    strategy:
      runOnce:
        deploy:
          steps:
          - task: PowerShell@2
            displayName: 'Deploy with Rollback Protection'
            inputs:
              script: |
                sql-schema-generator --execute --prod [params]
                
                if ($LASTEXITCODE -eq 0) {
                    Write-Host "✅ Deployment successful - compiled SQL committed to repo"
                } else {
                    Write-Host "❌ Deployment failed - automatic rollback completed"
                    exit $LASTEXITCODE
                }
```

## 🎨 Strongly-Typed Entity Attributes

### Data Types & Constraints
```csharp
[SqlTable("CameraBodies")]
[ExportToSQL]
public class CameraBody
{
    [SqlConstraints(SqlConstraint.PrimaryKey, SqlConstraint.Identity)]
    public int Id { get; set; }
    
    [SqlType(SqlDataType.NVarChar, 100)]
    [SqlConstraints(SqlConstraint.NotNull)]
    public string Brand { get; set; }
    
    [SqlType(SqlDataType.Decimal, precision: 10, scale: 2)]
    [SqlDefault(SqlDefaultValue.Zero)]
    public decimal Price { get; set; }
    
    [SqlDefault(SqlDefaultValue.GetUtcDate)]
    public DateTime CreatedAt { get; set; }
}
```

### Indexes & Foreign Keys
```csharp
public class CameraBody
{
    // Single column index
    [SqlIndex(SqlIndexType.NonClustered)]
    public string Brand { get; set; }
    
    // Composite index
    [SqlIndex(group: "BrandModelIndex", order: 1)]
    public string Brand { get; set; }
    
    [SqlIndex(group: "BrandModelIndex", order: 2)]
    public string Model { get; set; }
    
    // Foreign key with cascade delete
    [SqlForeignKey<User>(onDelete: ForeignKeyAction.Cascade)]
    public int OwnerId { get; set; }
}
```

## 📈 MTTR Reduction Benefits

### Before (Traditional Approach)
1. 🔍 "What was deployed recently?" (10 minutes)
2. 🔍 "Which scripts were included?" (15 minutes)
3. 🔍 "What was the exact SQL?" (20 minutes)
4. 🔄 "Plan and execute rollback" (60+ minutes)
**Total MTTR: 2+ hours**

### After (This Tool)
1. 📋 `git log` → See exact deployment (30 seconds)
2. 📄 View compiled SQL file → See exact changes (30 seconds)
3. 🔄 `sql-schema-generator --rollback-to-previous` (5 minutes)
**Total MTTR: 6 minutes**

### Audit & Compliance Benefits
- **Perfect audit trail**: Every deployment in git history
- **Exact execution record**: Compiled SQL shows precisely what ran
- **Immutable deployment history**: Git provides tamper-proof record
- **Instant compliance reporting**: `git log --since="quarter"`

## 🔧 Troubleshooting

### Assembly Discovery Issues
```
No Domain assemblies found. Make sure projects are built.
```
- Ensure Domain projects are built: `dotnet build`
- Check naming follows `*.Domain` convention
- Verify assemblies contain `[ExportToSQL]` entities

### Script Enhancement Issues
```
Could not parse SQL script - using as-is
```
- Tool falls back to original script if parsing fails
- Complex scripts may not be auto-enhanced
- Consider manual guard clauses for complex SQL

### Azure Authentication Issues
```
Key Vault access denied
```
- Verify Azure credentials: `az login`
- Check Key Vault access policies
- Ensure secrets exist with correct names

### Local SQL Server Issues
```
Cannot connect to localhost\SQLEXPRESS
```
- Verify SQL Server Express is running
- Check Windows Authentication is enabled
- Try connecting via SSMS first

## 📚 Architecture

### What This Tool Handles
- ✅ **Complete 29-phase deployment pipeline**
- ✅ **Entity-driven schema generation** 
- ✅ **Custom SQL script management**
- ✅ **Script enhancement and validation**
- ✅ **Azure-native backup and restore**
- ✅ **Compiled deployment artifacts**
- ✅ **Domain-driven versioning**

### Integration with Other Tools
This tool focuses on **structural database deployments**. For specialized needs, consider:
- **Complex migrations**: Use alongside migration-specific tools
- **Data transformations**: ETL tools for large data operations  
- **Monitoring**: Database monitoring solutions
- **Performance tuning**: Database performance tools

## 🚀 Requirements

- .NET 9 SDK
- Azure Key Vault access (Managed Identity, Service Principal, or developer credentials) **OR** Windows Authentication for local development
- SQL Server or Azure SQL Database
- Git repository for deployment history
- Built Domain assemblies with `[ExportToSQL]` entities

## 🎯 Future Roadmap

- **Cross-database dependencies**: Support for multi-database deployments
- **Parallel execution**: Speed improvements for large deployments
- **Advanced analytics**: Deployment performance and impact analysis
- **Integration APIs**: Webhooks and event notifications

---

**Transform your database deployments from manual, error-prone processes into automated, auditable, enterprise-grade operations with perfect traceability and sub-10-minute MTTRs.**