# SQL Server Sync Generator

Automated SQL Server schema generation from .NET Domain entities with Azure Key Vault integration. Generates tables, indexes, and foreign keys from entity metadata for pipeline-driven database deployments.

## Installation

```bash
# Build and install as global tool
dotnet pack
dotnet tool install -g --add-source ./bin/Debug SQLServerSyncGenerator
```

## Usage

```bash
# Basic usage
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

# Development with verbose logging (default)
sql-schema-generator \
  --server "dev-server.database.windows.net" \
  --database "LocationAnalytics_Dev" \
  --keyvault-url "https://devvault.vault.azure.net/" \
  --username-secret "sql-username" \
  --password-secret "sql-password" \
  --verbose

# Custom assembly paths
sql-schema-generator \
  --server "myserver.database.windows.net" \
  --database "LocationAnalytics" \
  --keyvault-url "https://myvault.vault.azure.net/" \
  --username-secret "sql-username" \
  --password-secret "sql-password" \
  --core-assembly "path/to/Location.Core.Domain.dll" \
  --photography-assembly "path/to/Location.Photography.Domain.dll"
```

## What It Does

1. **Discovers Domain Entities** - Finds all classes in `*.Domain.dll` assemblies
2. **Extracts Schema Names** - Uses DLL naming: `Location.Photography.Domain.dll` → `Photography` schema
3. **Analyzes Entity Structure** - Properties, constraints, indexes, foreign keys via attributes
4. **Builds Dependency Graph** - Topological sorting for correct table creation order
5. **Generates DDL** - Creates schemas, tables, indexes, foreign key constraints
6. **Production Safety** - Automatic database backups in production mode

## Schema Organization

### Auto-Detection
- **Schema Names**: `Location.{SCHEMA}.Domain.dll` → `{SCHEMA}` schema
- **Table Names**: Entity class names (with `[SqlTable]` override support)
- **Column Names**: Property names (Microsoft PascalCase standard)

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
- `--server` - SQL Server name (e.g., `myserver.database.windows.net`)
- `--database` - Target database name
- `--keyvault-url` - Azure Key Vault URL (e.g., `https://myvault.vault.azure.net/`)
- `--username-secret` - Key Vault secret name for SQL username
- `--password-secret` - Key Vault secret name for SQL password

### Optional Parameters
- `--prod` - Production mode (creates database backup before changes)
- `--verbose` - Enable verbose logging (default: true for dev, false for prod)
- `--core-assembly` - Custom path to Location.Core.Domain.dll
- `--photography-assembly` - Custom path to Location.Photography.Domain.dll

## Production Safety

### Development Mode (Default)
- **Verbose logging enabled** by default
- **No database backup** - apply changes directly
- **Fast iteration** for development workflow

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

### Azure Key Vault Integration
- **DefaultAzureCredential** supports multiple authentication methods:
  - Managed Identity (recommended for production)
  - Service Principal
  - Developer credentials (Azure CLI, Visual Studio)
- **Secure credential storage** - no passwords in code or config files
- **Environment-specific vaults** supported

### SQL Connection Security
- **Encrypted connections** by default
- **Certificate validation** enabled
- **Appropriate timeouts** for DDL operations (5 minutes)
- **Admin operations** use separate connection (10 minutes timeout)

## Pipeline Integration

### Azure DevOps Example
```yaml
- task: AzureCLI@2
  displayName: 'Generate Database Schema'
  inputs:
    azureSubscription: '$(AzureSubscription)'
    scriptType: 'bash'
    scriptLocation: 'inlineScript'
    inlineScript: |
      sql-schema-generator \
        --server "$(SqlServer)" \
        --database "$(DatabaseName)" \
        --keyvault-url "$(KeyVaultUrl)" \
        --username-secret "$(SqlUsernameSecret)" \
        --password-secret "$(SqlPasswordSecret)" \
        --prod
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
- Azure Key Vault access (Managed Identity, Service Principal, or developer credentials)
- SQL Server or Azure SQL Database
- Built Domain assemblies (`Location.Core.Domain.dll`, `Location.Photography.Domain.dll`)

## Troubleshooting

### Common Issues

**Assembly Not Found**
```
No Domain assemblies found. Make sure Domain projects are built.
```
- Ensure `Location.Core.Domain` and `Location.Photography.Domain` projects are built
- Check assembly paths with `--core-assembly` and `--photography-assembly` options

**Key Vault Access Denied**
```
Access denied to Azure Key Vault. Check Key Vault access policies and permissions.
```
- Verify the application has appropriate Key Vault access policies
- For Managed Identity: Grant "Key Vault Secrets User" role
- For Service Principal: Configure access policies in Key Vault

**Circular Dependency**
```
Circular dependency detected: Photography.UserCameraBodies -> Core.Users -> Photography.UserCameraBodies
```
- Review foreign key relationships between entities
- Remove circular references or restructure entity relationships

**SQL Connection Failed**
```
SQL Server connection test failed: Login failed for user 'username'
```
- Verify SQL credentials in Key Vault are correct
- Check SQL Server firewall rules
- Ensure database exists and user has appropriate permissions

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

## Development

### Building from Source
```bash
git clone [repository-url]
cd SQLServerSyncGenerator
dotnet build
dotnet pack
dotnet tool install -g --add-source ./bin/Debug SQLServerSyncGenerator
```

### Testing
```bash
# Test against development database
sql-schema-generator \
  --server "dev-server.database.windows.net" \
  --database "TestDatabase" \
  --keyvault-url "https://devvault.vault.azure.net/" \
  --username-secret "sql-username" \
  --password-secret "sql-password" \
  --verbose
```