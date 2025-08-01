# Location Automated API Generator

Automated REST API generator with Domain Driven Versioning. Creates schema-perfect Azure Functions endpoints from SQLite DLL metadata with unlimited backward compatibility.

## Installation

```bash
# Build and install as global tool
dotnet pack
dotnet tool install -g --add-source ./bin/Debug Location.AutomatedAPIGenerator
```

## Usage

### Auto-Discovery (Recommended)
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

### Manual Override (With Warnings)
```bash
location-api-generator \
  --extractors "LocationEntity,CameraBodies,Lenses" \
  --server "myserver.database.windows.net" \
  --database "LocationAnalytics" \
  --keyvault-url "https://myvault.vault.azure.net/" \
  --username-secret "sql-username" \
  --password-secret "sql-password" \
  --azure-subscription "sub-id" \
  --resource-group "rg-locationapis"
```

### No-Op Mode (Preview)
```bash
location-api-generator \
  --auto-discover \
  --noop \
  [other args...]
```

## What It Does

1. **Discovers Infrastructure.dll** - Auto-finds Location.*.Infrastructure.dll and extracts vertical/version
2. **Reflects Entities** - Finds classes with `[ExportToSQL]` attribute 
3. **Generates API Assets** - Creates Azure Functions controllers with 8 endpoints per vertical
4. **Deploys to Azure** - Creates Function App with Bicep infrastructure template
5. **Schema-Perfect Extraction** - Uses DLL metadata for exact SQLite → SQL Server mapping

## Generated Endpoints

For each vertical (e.g., photography v3):

- `POST /photography/v3/auth/request-qr`
- `POST /photography/v3/auth/verify-email`  
- `POST /photography/v3/auth/generate-qr`
- `POST /photography/v3/auth/scan-qr`
- `POST /photography/v3/auth/manual-restore`
- `POST /photography/v3/auth/send-recovery-qr`
- `POST /photography/v3/backup` - **Core data extraction endpoint**
- `POST /photography/v3/forgetme` - **GDPR deletion endpoint**

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

## Architecture

- **Domain Driven Versioning** - Major version from Infrastructure.dll drives API versioning
- **Schema-Perfect Mapping** - Uses SQL attributes for exact type mapping
- **Consumption-Based Hosting** - Each major version gets dedicated Function App
- **Unlimited Backward Compatibility** - All versions maintained indefinitely

## Exit Codes

- `0` - Success
- `1` - Error (missing assemblies, Azure authentication failure, etc.)

## Requirements

- .NET 9 SDK
- Azure subscription with Function Apps enabled
- Azure Key Vault access (Managed Identity or Service Principal)
- Built Infrastructure.dll with entities marked `[ExportToSQL]`