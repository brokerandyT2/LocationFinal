# Location Automated API Generator

Complete pipeline tool that generates, compiles, deploys, and archives REST APIs with Domain Driven Versioning. Creates schema-perfect Azure Functions endpoints from SQLite DLL metadata with unlimited backward compatibility.

## Installation

```bash
# Build and install as global tool
dotnet pack
dotnet tool install -g --add-source ./bin/Debug Location.AutomatedAPIGenerator
```

## Usage

### Complete Pipeline (Recommended)
```bash
location-api-generator \
  --auto-discover \
  --server "myserver.database.windows.net" \
  --database "LocationAnalytics" \
  --keyvault-url "https://myvault.vault.azure.net/" \
  --username-secret "sql-username" \
  --password-secret "sql-password" \
  --azure-subscription "sub-id" \
  --resource-group "rg-locationapis"
```

### With Azure Artifacts
```bash
location-api-generator \
  --auto-discover \
  --artifacts-feed "LocationAPIs" \
  --artifacts-organization "myorg" \
  [other args...]
```

### Development Mode
```bash
location-api-generator \
  --auto-discover \
  --noop \
  --verbose \
  [other args...]
```

## Complete Pipeline Flow

The tool performs end-to-end automation:

1. **Discovers Infrastructure.dll** - Auto-finds Location.*.Infrastructure.dll and extracts vertical/version
2. **Reflects Entities** - Finds classes with `[ExportToSQL]` attribute 
3. **Generates Source Code** - Creates complete Azure Functions project with controllers
4. **Compiles Binary** - Builds deployment-ready Function App binary
5. **Deploys to Azure** - Creates Function App with Bicep infrastructure template
6. **Stores Artifacts** - Uploads compiled binary to Azure Artifacts (Major.0.0 versioning)
7. **Schema-Perfect Extraction** - Uses DLL metadata for exact SQLite → SQL Server mapping

## Generated API Endpoints

For each vertical (e.g., photography v4):

### Authentication & Account Management
- `POST /photography/v4/register` - Register account with email + appId
- `POST /photography/v4/refresh` - Refresh JWT token
- `GET /photography/v4/status/{email}` - Account status & last backup time

### Core Functionality  
- `POST /photography/v4/backup/{email}` - **Upload backup zip (stores + processes)**
- `GET /photography/v4/restore/{email}` - **Download most recent backup zip**

### GDPR Compliance
- `POST /photography/v4/forgetme/{email}` - **Complete data deletion**

## Authentication Model

Simple account-based authentication:
- **1 Email + 1 AppId = 1 Account**
- **Device replacement supported** (enter same AppId GUID)
- **JWT tokens** with 1-year expiration
- **Cost control**: 1 account = 1 backup stream

## Entity Requirements

Mark entities for export with the `[ExportToSQL]` attribute:

```csharp
[Table("LocationEntity")]
[ExportToSQL("User location data for analytics")]
public class LocationEntity
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    
    [SqlType(SqlDataType.NVarChar, 100)]
    public string Title { get; set; } = string.Empty;
    
    // ... other properties
}
```

## Command Line Options

### Required Parameters
- `--server` - SQL Server name
- `--database` - Database name  
- `--keyvault-url` - Azure Key Vault URL
- `--username-secret` - Key Vault secret for SQL username
- `--password-secret` - Key Vault secret for SQL password
- `--azure-subscription` - Azure subscription ID
- `--resource-group` - Resource group for deployment

### Optional Parameters
- `--auto-discover` - Auto-discover entities (default: true)
- `--artifacts-feed` - Azure DevOps Artifacts feed (default: "LocationAPIs")
- `--artifacts-organization` - Azure DevOps organization
- `--skip-artifacts` - Skip artifact upload
- `--noop` - Generate and compile without deploying
- `--verbose` - Enable verbose logging
- `--prod` - Production deployment mode

### Development Options
- `--extractors` - Manual table list (bypasses [ExportToSQL])
- `--ignore-export-attribute` - Ignore validation (DEVELOPMENT ONLY)
- `--infrastructure-assembly` - Custom path to Infrastructure.dll

## Architecture

### Domain Driven Versioning
- **Major version changes** → New Function App (`v4` → `v5`)
- **Minor version changes** → Same Function App (idempotent deployment)
- **Schema changes** always bump major version

### Deployment Strategy
- **Consumption-based hosting** - Scales to zero, pay per use
- **Idempotent deployments** - Safe to run multiple times
- **Unlimited backward compatibility** - All versions maintained indefinitely

### Storage & Backup
- **Blob Storage**: `{email}_{appId}` containers, keep 2 most recent backups
- **SQL Server**: Analytics data with user context (email + appId)
- **Azure Artifacts**: Compiled binaries with Major.0.0 versioning

## Pipeline Integration

### Azure DevOps Pipeline
```yaml
- task: DotNetCoreCLI@2
  displayName: 'Generate Location API'
  inputs:
    command: 'custom'
    custom: 'tool'
    arguments: 'run location-api-generator -- --auto-discover --server $(SQL_SERVER) --database $(DATABASE) --keyvault-url $(KEY_VAULT_URL) --username-secret $(SQL_USERNAME_SECRET) --password-secret $(SQL_PASSWORD_SECRET) --azure-subscription $(AZURE_SUBSCRIPTION) --resource-group $(RESOURCE_GROUP)'
```

### Environment Variables
- `AZURE_DEVOPS_PAT` - Personal Access Token for artifacts
- `TOKEN_SECRET` - JWT signing key (auto-generated if not provided)

## Exit Codes

- `0` - Success (deployment completed, artifacts uploaded)
- `1` - Error (build failure, deployment failure, etc.)

## Silent Failures

The tool **fails silently** on:
- **Duplicate artifacts** - Same major version already exists in Azure Artifacts
- **Idempotent deployments** - Function App already exists with same configuration

This enables safe pipeline re-runs and handles version overlap gracefully.

## Requirements

- **.NET 9 SDK** - For building and running the tool
- **Azure subscription** - Function Apps and Storage enabled
- **Azure Key Vault access** - Managed Identity or Service Principal
- **Azure DevOps Artifacts** - For binary storage (optional)
- **Built Infrastructure.dll** - With entities marked `[ExportToSQL]`

## Generated Function App Structure

```
location-photography-api-v4/
├── LocationAPIController.cs    # Generated endpoints
├── Program.cs                  # Function App startup
├── FunctionApp.csproj         # Complete project file
├── host.json                  # Function configuration
├── infrastructure.bicep       # Azure resources
└── bin/Release/net9.0/        # Compiled binary (stored in artifacts)
    ├── FunctionApp.dll
    └── dependencies...
```