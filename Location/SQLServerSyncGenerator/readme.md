# SQL Server Sync Generator

Automated SQL Server schema generation from .NET Domain entities with Azure Key Vault integration. Generates tables, indexes, and foreign keys from entity metadata for pipeline-driven database deployments. **Auto-runs after Domain project builds with MSBuild integration.**

## Installation & Auto-Setup

### No-Operation Mode (Preview Changes)
```bash
# Build and install as global tool with MSBuild integration
dotnet pack
dotnet tool install -g --add-source ./bin/Debug SQLServerSyncGenerator
```

**🎉 That's it!** The tool now auto-runs after Domain project builds in **safe `--noop` mode** by default.

## Developer Experience (Auto-Run)

### Automatic Schema Analysis
When you build any `*Domain` project, the tool automatically:

1. ✅ **Analyzes your database schema** against Domain entities
2. ✅ **Shows DDL changes** that would be applied (in `--noop` mode)
3. ✅ **Never modifies your database** unless explicitly configured
4. ✅ **Provides immediate feedback** on schema drift

### Example Build Output
```
[SqlSchemaGenerator] Analyzing database schema after Domain build...
[SqlSchemaGenerator] Project: Location.Photography.Domain
[SqlSchemaGenerator] Mode: noop (safe default for developers)

=== Delta DDL Statements (No-Op Mode) ===
The following 3 DDL statements would be executed:

DDL Statement 1:
ALTER TABLE [Photography].[CameraBodies] ADD [SensorSize] NVARCHAR(50) NULL

DDL Statement 2:
CREATE NONCLUSTERED INDEX [IX_CameraBodies_SensorSize] ON [Photography].[CameraBodies] ([SensorSize])

=== End Delta DDL Statements (3 total) ===

[SqlSchemaGenerator] Schema analysis completed!
```

### Developer Configuration Options

**Disable Auto-Run** (add to `Directory.Build.props` or project file):
```xml
<PropertyGroup>
  <RunSqlSchemaGenerator>false</RunSqlSchemaGenerator>
</PropertyGroup>
```

**Actually Apply Changes** (remove safety):
```xml
<PropertyGroup>
  <SqlSchemaGeneratorMode>execute</SqlSchemaGeneratorMode>
  <SqlSchemaGeneratorDatabase>MyDevDatabase</SqlSchemaGeneratorDatabase>
</PropertyGroup>
```

**Custom Database Connection**:
```xml
<PropertyGroup>
  <SqlSchemaGeneratorServer>localhost</SqlSchemaGeneratorServer>
  <SqlSchemaGeneratorDatabase>MyCustomDB</SqlSchemaGeneratorDatabase>
  <SqlSchemaGeneratorUseLocal>true</SqlSchemaGeneratorUseLocal>
</PropertyGroup>
```

## Command Line Usage (Manual/Pipeline)

### Production/Azure Usage
```bash
# Basic usage with Azure Key Vault
sql-schema-generator \
  --server "myserver.database.windows.net" \
  --database "LocationAnalytics" \
  --keyvault-url "https://myvault.vault.azure.net/" \
  --username-secret "sql-username" \
  --password-secret "sql-password"

# Production mode with automatic backup
sql-schema-generator \
  --server "prod-server.database.windows.net" \
  --database "LocationAnalytics" \
  --keyvault-url "https://prodvault.vault.azure.net/" \
  --username-secret "sql-username" \
  --password-secret "sql-password" \
  --prod
```

### Local Development Usage
```bash
# Local SQL Server Express with Windows Authentication
sql-schema-generator \
  --server "localhost\SQLEXPRESS" \
  --database "Locations_BI" \
  --local

# Local development with verbose logging
sql-schema-generator \
  --server "localhost\SQLEXPRESS" \
  --database "Locations_BI" \
  --local \
  --verbose

# See what changes would be made without executing them
sql-schema-generator \
  --server "localhost\SQLEXPRESS" \
  --database "Locations_BI" \
  --local \
  --noop
```

## Validation & Automatic Rollback

### Pre-flight Validation
The tool includes comprehensive validation to catch dangerous changes before they're applied:

```bash
# Validation-only mode (perfect for pipeline conditional logic)
sql-schema-generator \
  --validate-only \
  --prod \
  --server "myserver.database.windows.net" \
  --database "LocationAnalytics" \
  --keyvault-url "https://myvault.vault.azure.net/" \
  --username-secret "sql-username" \
  --password-secret "sql-password"

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
- `DROP DATABASE` - Not allowed in automated deployments

**⚠️ Warning Issues (Exit Code 1)**
- `ALTER COLUMN` - May truncate or convert data
- `ADD COLUMN NOT NULL` without default - Will fail on existing data
- `CREATE INDEX` on large tables - Performance impact
- `ADD CONSTRAINT` - May fail on existing data

**✅ Safe Operations (Exit Code 0)**
- `CREATE TABLE` - New tables are safe
- `ADD COLUMN NULL` - Nullable columns are safe
- `ADD COLUMN` with default - Generally safe
- `CREATE SCHEMA` - New schemas are safe

### Automatic Rollback Protection

In production mode (`--prod`), the tool provides enterprise-grade safety:

1. **🛡️ Pre-flight Validation** - Catches issues before starting
2. **💾 Automatic Backup** - Creates database copy before changes  
3. **🔄 Automatic Rollback** - Restores backup on any failure
4. **🧹 Cleanup** - Removes backup on successful deployment

```bash
# Production deployment with full protection
sql-schema-generator \
  --prod \
  --server "prod-server.database.windows.net" \
  --database "LocationAnalytics" \
  --keyvault-url "https://prodvault.vault.azure.net/" \
  --username-secret "sql-username" \
  --password-secret "sql-password"

# What happens:
# 1. ✅ Pre-flight validation runs first
# 2. 💾 Creates backup: LocationAnalytics_PreDeploy_20250131_143022  
# 3. 🚀 Applies schema changes
# 4. ✅ Success: Backup automatically deleted
# 5. ❌ Failure: Database automatically restored from backup
```
```bash
# Azure - see what DDL would be executed without running it
sql-schema-generator \
  --server "myserver.database.windows.net" \
  --database "LocationAnalytics" \
  --keyvault-url "https://myvault.vault.azure.net/" \
  --username-secret "sql-username" \
  --password-secret "sql-password" \
  --noop

# Local - see what DDL would be executed without running it
sql-schema-generator \
  --server "localhost\SQLEXPRESS" \
  --database "Locations_BI" \
  --local \
  --noop
```

## What It Does

1. **Discovers Domain Entities** - Automatically finds all `*.Domain.dll` assemblies in your solution
2. **Extracts Schema Names** - Uses DLL naming: `Location.Photography.Domain.dll` → `Photography` schema
3. **Analyzes Entity Structure** - Properties, constraints, indexes, foreign keys via attributes
4. **Builds Dependency Graph** - Topological sorting for correct table creation order
5. **Generates DDL** - Creates schemas, tables, indexes, foreign key constraints
6. **Production Safety** - Automatic database backups in production mode
7. **MSBuild Integration** - Auto-runs after Domain builds for immediate developer feedback

## Schema Organization

### Auto-Detection
- **Schema Names**: `Location.{SCHEMA}.Domain.dll` → `{SCHEMA}` schema
- **Table Names**: Entity class names (with `[SqlTable]` override support)
- **Column Names**: Property names (Microsoft PascalCase standard)
- **Assembly Discovery**: Automatically finds any `*Domain` projects in your solution

### Examples
```csharp
// Location.Photography.Domain.dll → Photography schema
namespace Location.Photography.Domain.Entities
{
    [SqlTable("CameraBodies")]  // Override table name
    public class CameraBody     // Default: CameraBody table
    {
        public int Id { get; set; }           // → Photography.CameraBodies.Id
        public string Name { get; set; }      // → Photography.CameraBodies.Name
    }
}

// Location.Core.Domain.dll → Core schema
namespace Location.Core.Domain.Entities
{
    public class User           // → Core.Users table
    {
        public int Id { get; set; }
        public string Email { get; set; }
    }
}

// Location.Fishing.Domain.dll → Fishing schema (auto-discovered!)
namespace Location.Fishing.Domain.Entities
{
    public class FishSpecies    // → Fishing.FishSpecies table
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
```

## MSBuild Integration Details

### How It Works
1. **Auto-enables** for any project with "Domain" in the name
2. **Runs after successful builds** in Debug configuration only
3. **Defaults to `--noop` mode** for database safety
4. **Uses local database** (`localhost\SQLEXPRESS`) by default
5. **Provides immediate feedback** on schema changes

### Configuration Properties
All MSBuild properties can be set in `Directory.Build.props`, project files, or via MSBuild command line:

| Property | Default | Description |
|----------|---------|-------------|
| `RunSqlSchemaGenerator` | `true` (for Domain projects) | Enable/disable auto-run |
| `SqlSchemaGeneratorMode` | `noop` | Mode: `noop`, `execute`, or `prod` |
| `SqlSchemaGeneratorServer` | `localhost\SQLEXPRESS` | SQL Server name |
| `SqlSchemaGeneratorDatabase` | `LocationDev` | Database name |
| `SqlSchemaGeneratorUseLocal` | `true` | Use Windows Authentication |
| `SqlSchemaGeneratorVerbose` | `true` | Enable verbose logging |

### Advanced Configuration
```xml
<!-- In Directory.Build.props for team-wide settings -->
<PropertyGroup>
  <!-- Enable for all Domain projects -->
  <SqlSchemaGeneratorDatabase>LocationDev_$(USERNAME)</SqlSchemaGeneratorDatabase>
  
  <!-- Or use Azure for shared dev environment -->
  <SqlSchemaGeneratorUseLocal>false</SqlSchemaGeneratorUseLocal>
  <SqlSchemaGeneratorKeyVaultUrl>https://devvault.vault.azure.net/</SqlSchemaGeneratorKeyVaultUrl>
  <SqlSchemaGeneratorUsernameSecret>dev-sql-username</SqlSchemaGeneratorUsernameSecret>
  <SqlSchemaGeneratorPasswordSecret>dev-sql-password</SqlSchemaGeneratorPasswordSecret>
</PropertyGroup>
```

## Strongly-Typed Attributes

### SQL Data Types
```csharp
[SqlType(SqlDataType.NVarChar, 100)]                    // String with length
public string Name { get; set; }

[SqlType(SqlDataType.NVarCharMax)]                      // Large text
public string Description { get; set; }

[SqlType(SqlDataType.Decimal, precision: 18, scale: 2)] // Money/precise decimals
public decimal Price { get; set; }

[SqlType(SqlDataType.VarChar, 50)]                      // ASCII-only strings
public string CountryCode { get; set; }
```

### Constraints
```csharp
[SqlConstraints(SqlConstraint.NotNull, SqlConstraint.Unique)]
public string Email { get; set; }

[SqlConstraints(SqlConstraint.NotNull)]
public string Name { get; set; }
```

### Default Values
```csharp
[SqlDefault(SqlDefaultValue.GetUtcDate)]
public DateTime CreatedAt { get; set; }

[SqlDefault(SqlDefaultValue.NewId)]
public Guid PublicId { get; set; }

[SqlDefault("0")]  // Custom default
public int Status { get; set; }
```

### Indexes
```csharp
// Single column index
[SqlIndex(SqlIndexType.NonClustered)]
public string Name { get; set; }

// Composite indexes via grouping
[SqlIndex(group: "UserLocationIndex", order: 1, type: SqlIndexType.NonClustered)]
public int UserId { get; set; }

[SqlIndex(group: "UserLocationIndex", order: 2)]
public int LocationId { get; set; }
```

### Foreign Keys
```csharp
[SqlForeignKey<User>(onDelete: ForeignKeyAction.Cascade)]
public int UserId { get; set; }  // References Core.Users.Id (auto-detected schema)

[SqlForeignKey<Location>(referencedColumn: "LocationId")]
public int LocationRef { get; set; }  // References different column
```

### Overrides
```csharp
[SqlTable("CustomTableName")]     // Override table name
[SqlSchema("CustomSchema")]       // Override schema
[SqlColumn("custom_column")]      // Override column name
[SqlIgnore]                       // Skip property entirely
```

## Command Line Options

### Required Parameters
- `--server` - SQL Server name (e.g., `myserver.database.windows.net` or `localhost\SQLEXPRESS`)
- `--database` - Target database name

### Authentication (choose one)
**Azure Key Vault Authentication:**
- `--keyvault-url` - Azure Key Vault URL (e.g., `https://myvault.vault.azure.net/`)
- `--username-secret` - Key Vault secret name for SQL username
- `--password-secret` - Key Vault secret name for SQL password

**Local Development Authentication:**
- `--local` - Use Windows Authentication (bypasses Key Vault requirements)

### Optional Parameters
- `--prod` - Production mode (creates database backup before changes)
- `--noop` - No-operation mode (analyze database and show what DDL would be executed without running it)
- `--verbose` - Enable verbose logging (default: true for dev, false for prod)
- `--core-assembly` - Custom path to Location.Core.Domain.dll
- `--vertical-assembly` - Custom path to Location.Photography.Domain.dll or other vertical assembly

## Production Safety

### Development Mode (Default for Auto-Run)
- **No-operation mode** enabled by default
- **Verbose logging enabled** by default
- **Shows DDL changes** without executing them
- **Safe for continuous development** workflow

### Local Development Mode (`--local` flag)
- **Windows Authentication** - no Key Vault credentials needed
- **Perfect for SQL Server Express** on developer machines
- **Verbose logging enabled** by default
- **Works with any local SQL Server instance**

### No-Operation Mode (`--noop` flag)
- **Connects to database** and analyzes existing schema
- **Generates only delta DDL** - shows exactly what would be executed
- **No changes applied** - perfect for validation and review
- **Works with both Azure and local authentication**
- **Default mode for MSBuild integration**

### Production Mode (`--prod` flag)
1. **Create database copy** before applying changes
2. **Apply DDL changes** to main database
3. **Success**: Delete backup copy (cleanup)
4. **Failure**: Log backup location for manual restore

### Azure SQL Database Integration
- **Automatic backups** via database copy (no infrastructure management)
- **Point-in-time restore** available via Azure portal if needed
- **Rollback strategy** handled by Azure PaaS features

## Security & Authentication

### Azure Key Vault Integration (Production)
- **DefaultAzureCredential** supports multiple authentication methods:
  - Managed Identity (recommended for production)
  - Service Principal
  - Developer credentials (Azure CLI, Visual Studio)
- **Secure credential storage** - no passwords in code or config files
- **Environment-specific vaults** supported

### Local Development Authentication
- **Windows Authentication** with current user credentials
- **No Key Vault required** for local development
- **Works with SQL Server Express** out of the box
- **Integrated Security** for seamless developer experience

### SQL Connection Security
- **Encrypted connections** by default (Azure)
- **Trusted certificates** for local development
- **Appropriate timeouts** for DDL operations (5 minutes)
- **Admin operations** use separate connection (10 minutes timeout)

## Pipeline Integration

### Pipeline Integration with Conditional Approval

```yaml
# Complete ADO Pipeline with Conditional Manual Approval
stages:
- stage: SchemaValidation
  displayName: 'Schema Validation & Conditional Approval'
  jobs:
  - job: ValidateSchema
    displayName: 'Pre-flight Schema Validation'
    steps:
    
    # Step 1: Run validation and capture results
    - task: PowerShell@2
      displayName: 'Pre-flight Schema Validation'
      inputs:
        script: |
          Write-Host "Running pre-flight schema validation..."
          
          # Run validation-only mode and capture exit code
          $output = & sql-schema-generator `
            --validate-only `
            --prod `
            --server "$(SqlServer)" `
            --database "$(Database)" `
            --keyvault-url "$(KeyVaultUrl)" `
            --username-secret "$(SqlUsernameSecret)" `
            --password-secret "$(SqlPasswordSecret)" `
            2>&1
          
          $exitCode = $LASTEXITCODE
          Write-Host "Validation exit code: $exitCode"
          Write-Host "Validation output:"
          Write-Host $output
          
          # Set pipeline variables
          Write-Host "##vso[task.setvariable variable=ValidationResult]$exitCode"
          Write-Host "##vso[task.setvariable variable=ValidationOutput]$output"
          
          # Determine next steps
          switch ($exitCode) {
            0 { Write-Host "✅ Validation passed - safe for automatic deployment" }
            1 { Write-Host "⚠️ Validation found warnings - manual approval required" }
            2 { Write-Host "❌ Validation failed - deployment blocked"; exit 1 }
            default { Write-Host "❓ Unknown validation result"; exit 1 }
          }
    
    # Step 2: Manual approval ONLY if validation found warnings
    - task: ManualValidation@0
      displayName: 'DBA Approval Required - Schema Warnings Detected'
      condition: eq(variables['ValidationResult'], '1')
      inputs:
        notifyUsers: |
          dba@company.com
          database-team@company.com
          tech-lead@company.com
        instructions: |
          ⚠️ PRE-FLIGHT SCHEMA VALIDATION DETECTED WARNINGS ⚠️
          
          The following schema validation warnings were found:
          
          $(ValidationOutput)
          
          Please review these warnings carefully:
          • Are any data loss risks acceptable for this deployment?
          • Are performance impacts manageable during business hours?
          • Are there any blocking constraints or dependency issues?
          • Should this deployment proceed or be rescheduled?
          
          📋 APPROVAL OPTIONS:
          • Click RESUME to proceed with deployment (includes automatic rollback protection)
          • Click REJECT to cancel deployment and review changes
          
          🛡️ SAFETY FEATURES:
          • Automatic database backup created before changes
          • Automatic rollback on deployment failure
          • All changes can be manually reverted if needed
        onTimeout: 'reject'
        timeoutInMinutes: 240  # 4 hours for approval

- stage: ProductionDeploy
  displayName: 'Production Schema Deployment'
  dependsOn: SchemaValidation
  condition: or(eq(variables['ValidationResult'], '0'), eq(variables['ValidationResult'], '1'))
  jobs:
  - deployment: ApplySchemaChanges
    displayName: 'Apply Schema Changes with Rollback Protection'
    environment: 'Production-Database'  # Configure environment approvals if needed
    strategy:
      runOnce:
        deploy:
          steps:
          
          # Deploy with full production safety
          - task: PowerShell@2
            displayName: 'Apply Schema Changes'
            inputs:
              script: |
                Write-Host "🚀 Starting production schema deployment..."
                
                if ($env:ValidationResult -eq "0") {
                    Write-Host "✅ Auto-deploying - validation passed with no warnings"
                } else {
                    Write-Host "✅ Deploying after manual DBA approval"
                }
                
                Write-Host "🛡️ Production safety features enabled:"
                Write-Host "   • Automatic database backup before changes"
                Write-Host "   • Automatic rollback on failure"
                Write-Host "   • Full audit logging"
                
                # Execute with production mode (includes backup + rollback)
                sql-schema-generator `
                  --prod `
                  --server "$(SqlServer)" `
                  --database "$(Database)" `
                  --keyvault-url "$(KeyVaultUrl)" `
                  --username-secret "$(SqlUsernameSecret)" `
                  --password-secret "$(SqlPasswordSecret)" `
                  --verbose
                
                if ($LASTEXITCODE -eq 0) {
                    Write-Host "✅ Schema deployment completed successfully!"
                } else {
                    Write-Host "❌ Schema deployment failed - check logs for rollback details"
                    exit $LASTEXITCODE
                }
              env:
                ValidationResult: $(ValidationResult)

          # Post-deployment verification
          - task: PowerShell@2
            displayName: 'Post-Deployment Verification'
            inputs:
              script: |
                Write-Host "🔍 Running post-deployment verification..."
                
                # Verify schema is as expected
                sql-schema-generator `
                  --noop `
                  --server "$(SqlServer)" `
                  --database "$(Database)" `
                  --keyvault-url "$(KeyVaultUrl)" `
                  --username-secret "$(SqlUsernameSecret)" `
                  --password-secret "$(SqlPasswordSecret)"
                
                Write-Host "✅ Post-deployment verification completed"
```

### Alternative: Simpler Pipeline (Manual Approval for All Production Changes)

```yaml
# Simpler approach: Always require manual approval for production
- stage: ProductionDeploy
  jobs:
  - deployment: SchemaChanges
    environment: 'Production-Database'  # Requires manual approval in environment settings
    strategy:
      runOnce:
        deploy:
          steps:
          - task: PowerShell@2
            displayName: 'Preview Schema Changes'
            inputs:
              script: |
                Write-Host "📋 Schema changes that will be applied:"
                sql-schema-generator --noop --prod [connection-params]
          
          - task: PowerShell@2
            displayName: 'Apply Schema Changes'
            inputs:
              script: |
                sql-schema-generator --prod [connection-params]
```

### GitHub Actions Example
```yaml
- name: Generate Database Schema
  run: |
    sql-schema-generator \
      --server "${{ secrets.SQL_SERVER }}" \
      --database "${{ secrets.DATABASE_NAME }}" \
      --keyvault-url "${{ secrets.KEYVAULT_URL }}" \
      --username-secret "sql-username" \
      --password-secret "sql-password" \
      --prod
  env:
    AZURE_CLIENT_ID: ${{ secrets.AZURE_CLIENT_ID }}
    AZURE_CLIENT_SECRET: ${{ secrets.AZURE_CLIENT_SECRET }}
    AZURE_TENANT_ID: ${{ secrets.AZURE_TENANT_ID }}
```

## Architecture & Integration

### What This Tool Handles
- ✅ **Tables, columns, indexes, foreign keys** from .NET entities
- ✅ **Schema structure** from DLL metadata
- ✅ **Type-safe attribute** customization
- ✅ **Production backup** automation
- ✅ **Dependency ordering** via topological sort
- ✅ **Developer feedback** via MSBuild integration
- ✅ **Automatic assembly discovery** for any Domain projects

### What This Tool Does NOT Handle
- ❌ **Triggers, functions, views** → Use [Grate](https://erikbra.github.io/grate/) for complex SQL
- ❌ **Migration scripts** → Use Grate for hand-written SQL files
- ❌ **Rollback scenarios** → Use Azure SQL Database backup/restore
- ❌ **Business logic SQL** → Use Grate for behavioral SQL

### Recommended Pipeline Flow
1. **This tool runs first** → Creates/updates table structure
2. **Grate runs second** → Applies triggers, functions, views, stored procedures
3. **Clean separation** → Structural DDL vs. Behavioral SQL

## Exit Codes

- `0` - Success
- `1` - Error (invalid arguments, Key Vault access denied, SQL errors, etc.)

## Requirements

- .NET 9 SDK
- Azure Key Vault access (Managed Identity, Service Principal, or developer credentials) **OR** Windows Authentication for local development
- SQL Server or Azure SQL Database
- Built Domain assemblies (auto-discovered in solution)

## Troubleshooting

### Common Issues

**Assembly Not Found**
```
No Domain assemblies found. Make sure projects are built and contain 'Domain' in their name.
```
- Ensure Domain projects are built (`dotnet build`)
- Check that project names follow `*.Domain` convention
- Verify projects are in solution root directory structure

**Auto-Run Not Working**
```
MSBuild integration not running after Domain build
```
- Ensure you're building in Debug configuration
- Check that `RunSqlSchemaGenerator` is not explicitly set to `false`
- Verify the tool is installed as global tool: `dotnet tool list -g`
- Check project name contains "Domain"

**Local SQL Server Connection Failed**
```
SQL Server connection test failed: A network-related or instance-specific error occurred
```
- Verify SQL Server Express is running: `services.msc` → look for "SQL Server (SQLEXPRESS)"
- Check server name: usually `localhost\SQLEXPRESS` or just `localhost`
- Ensure Windows Authentication is enabled in SQL Server configuration
- Try connecting via SQL Server Management Studio first

**Key Vault Access Denied (Azure)**
```
Access denied to Azure Key Vault. Check Key Vault access policies and permissions.
```
- Verify the application has appropriate Key Vault access policies
- For Managed Identity: Grant "Key Vault Secrets User" role
- For Service Principal: Configure access policies in Key Vault
- For local development: Ensure you're logged into Azure CLI: `az login`

**Database Does Not Exist**
```
Cannot open database "LocationDev" requested by the login.
```
- Create the database first: `CREATE DATABASE LocationDev;`
- Or let the tool create it by ensuring your user has `dbcreator` role
- Check database name in configuration matches actual database

**Circular Dependency**
```
Circular dependency detected: Photography.UserCameraBodies -> Core.Users -> Photography.UserCameraBodies
```
- Review foreign key relationships between entities
- Remove circular references or restructure entity relationships
- Check `[SqlForeignKey<T>]` attributes for unintended cycles

**MSBuild Configuration Errors**
```
SqlSchemaGeneratorServer is required when RunSqlSchemaGenerator is enabled
```
- Set required MSBuild properties in `Directory.Build.props` or project file
- Or disable auto-run: `<RunSqlSchemaGenerator>false</RunSqlSchemaGenerator>`

### Debug Mode

Run with `--verbose` to enable detailed logging:
```bash
sql-schema-generator --verbose [other options]
```

This provides detailed information about:
- Assembly discovery and loading
- Entity analysis and metadata extraction
- Dependency graph building
- DDL generation process
- SQL execution progress

### MSBuild Integration Debug

To see MSBuild integration details:
```bash
# Build with diagnostic verbosity
dotnet build --verbosity diagnostic

# Look for SqlSchemaGenerator messages in output
```

## Development

### Building from Source
```bash
git clone [repository-url]
cd SQLServerSyncGenerator
dotnet build
dotnet pack
dotnet tool install -g --add-source ./bin/Debug SQLServerSyncGenerator
```

### Testing MSBuild Integration
```bash
# Build the tool (installs automatically)
dotnet build

# Build a Domain project (should auto-run schema analysis)
cd ../Location.Photography.Domain
dotnet build

# Check for schema analysis output in build log
```

### Testing Command Line
```bash
# Test against local SQL Server Express
sql-schema-generator \
  --server "localhost\SQLEXPRESS" \
  --database "TestDatabase" \
  --local \
  --verbose

# Test against Azure SQL Database
sql-schema-generator \
  --server "dev-server.database.windows.net" \
  --database "TestDatabase" \
  --keyvault-url "https://devvault.vault.azure.net/" \
  --username-secret "sql-username" \
  --password-secret "sql-password" \
  --verbose

# Preview changes without executing
sql-schema-generator \
  --server "localhost\SQLEXPRESS" \
  --database "TestDatabase" \
  --local \
  --noop
```

## Future Extensibility

The tool automatically discovers new vertical Domain assemblies without code changes:

- ✅ `Location.Core.Domain.dll` → Core schema
- ✅ `Location.Photography.Domain.dll` → Photography schema  
- ✅ `Location.Fishing.Domain.dll` → Fishing schema (auto-discovered)
- ✅ `Location.Hunting.Domain.dll` → Hunting schema (auto-discovered)
- ✅ `Location.{AnyNew}.Domain.dll` → {AnyNew} schema (auto-discovered)

No configuration or code changes needed for new verticals!