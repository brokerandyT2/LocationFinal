# Location DevSecOps - Complete Ecosystem Architecture

## Executive Summary
Complete DevSecOps ecosystem with automated versioning, database schema management, API generation, and mobile adapter creation. Handles multi-vertical architecture (Photography, Fishing, Hunting) with dual version numbering, ephemeral adapters, and perfect traceability from app crash to exact database state.

## 🎯 Core Philosophy
Architecture-Driven Versioning with 100% Developer Automation
- Architectural changes drive ALL downstream versioning
- Zero friction adoption with sane defaults  
- Complete traceability from app crash → exact database state
- Invisible productivity multipliers, not obstacles

## 🏗️ Ecosystem Overview

### Multi-Vertical Structure
```
Location Solution
├── Core (shared functionality)
│   ├── Domain, ViewModels, Infrastructure
│   └── Auto-incrementing versions (v3.1.5)
├── Photography (vertical module)  
│   ├── Domain, ViewModels, Mobile, Infrastructure
│   └── Explicit Core version pinning
├── Fishing (vertical module)
├── Hunting (vertical module)
└── [n] additional verticals
```

### Cross-Platform Support
- **Mobile**: .NET MAUI (single codebase, multiple platform builds)
- **Backend**: .NET 9, Azure SQL Database
- **APIs**: REST with Domain Driven Versioning (unlimited backward compatibility)
- **Mobile Platforms**: Android (Jetpack Compose/Kotlin), iOS (SwiftUI)

## 🚀 Developer Onboarding & Environment Management

### Zero-to-Productive Setup (Single Command)
**Cross-Platform Developer Setup Script**

#### Core Setup Capabilities
```bash
# Standard developer onboarding
./setup-dev.sh --photography
```
**What it installs:**
- **IDE**: Visual Studio Code + extensions (C#, .NET, mobile development)
- **Runtime**: .NET 9 SDK + Java (for Android development) + Docker Desktop
- **Mobile**: Android Studio + Xcode (macOS only) + development toolchains + emulators
- **Database**: Standardized SQL Server in Docker + SSMS/Azure Data Studio
- **Database Setup**: Creates "DataConsumption" database with persistent storage
- **Source Control**: Git + Azure DevOps integration via Azure AD
- **Repositories**: Location-Core + Photography (or selected vertical)
- **Tools**: Latest stable versions from Azure Artifacts feed
- **Build Integration**: Overrides native build commands to use development pipeline

#### IDE Build Command Integration
**Setup script automatically configures:**

**VS Code (.vscode/tasks.json):**
```json
{
  "tasks": [{
    "label": "build",
    "type": "shell", 
    "command": "./dev-build.sh --photography",
    "group": { "kind": "build", "isDefault": true }
  }]
}
```

**Xcode (Build Phases):**
```bash
# Pre-action script added to build scheme
./dev-build.sh --photography --ios
```

**Android Studio (Gradle integration):**
```gradle
# Post-build hook added to build.gradle
android.applicationVariants.all { variant ->
    variant.assemble.doLast {
        exec { commandLine './dev-build.sh', '--photography', '--android' }
    }
}
```

**Developer Experience:**
- **VS Code**: `Ctrl+Shift+B` → Full development pipeline
- **Xcode**: `Cmd+B` → Full development pipeline  
- **Android Studio**: Build → Full development pipeline
- **Result**: Every developer uses their natural build command, gets complete environment

#### Authentication Strategy
- **Azure AD Integration**: Uses developer's existing AD credentials
- **No Token Management**: Eliminates Personal Access Token complexity
- **Centralized Permissions**: IT controls repository and tool access
- **Audit Trail**: Complete logging of who installed what when

#### Incremental Updates & Multi-Vertical Support
```bash
# Add additional verticals without reinstalling everything
./setup-dev.sh --fishing         # Adds fishing repo, updates tools if needed
./setup-dev.sh --photography --tools  # Switches to tool development mode

# Update existing setup
./setup-dev.sh --photography    # Only updates changed components
```

#### Git Configuration & Repository Hygiene
**Critical Setup Requirements:**

**.gitignore Configuration (Enforced by Setup Script):**
```gitignore
# Generated Mobile Adapters (NEVER commit ephemeral shims)
**/generated/
**/AndroidUI/generated/
**/iOSUI/generated/
**/*Adapter.kt
**/*Adapter.swift

# Build Artifacts (DLLs managed via Azure Artifacts only)
**/bin/
**/obj/
*.dll
*.pdb
*.exe
!*.exe.config

# Tool Outputs (Compiled deployments are committed, but not tool binaries)
**/tools/bin/
**/tools/obj/
sql-schema-generator.exe
location-api-generator.exe

# Azure Artifacts Cache
**/.nuget/
**/packages/
```

**Package Source Configuration:**
```xml
<!-- nuget.config - All packages proxied through Azure Artifacts -->
<packageSources>
  <clear />
  <add key="Azure Artifacts" value="https://pkgs.dev.azure.com/x3squaredcircles/_packaging/all-packages/nuget/v3/index.json" />
</packageSources>
```

**Setup Script Git Integration:**
- **Creates/updates .gitignore** in each repository during setup
- **Configures package sources** to use Azure Artifacts exclusively
- **Sets up pre-commit hooks** to prevent accidental commits of generated files
- **Validates repository hygiene** during incremental updates

### Cross-Platform Database Strategy
**Standardized SQL Server Docker Architecture:**
- **Containerized Database Engine**: SQL Server 2022 Developer in Docker container
- **Native Management Tools**: SSMS (Windows) / Azure Data Studio (macOS/Linux)
- **Persistent Storage**: Database files mounted to host filesystem
- **Auto-Configuration**: Pre-configured connections and standard credentials

#### Docker Configuration (Managed by Tools Repository)
**Tools Repository Ownership:**
- **Tools repo contains**: All Docker configurations, setup scripts, and build orchestration
- **Single source of truth**: Dockerfile, docker-compose.yml, and init scripts versioned in tools repo
- **Automatic updates**: When tools repo updates, all developers get consistent Docker configurations

```dockerfile
# tools/docker/sqlserver/Dockerfile (in tools repo)
FROM mcr.microsoft.com/mssql/server:2022-CU8-ubuntu-20.04
ENV ACCEPT_EULA=Y
ENV SA_PASSWORD=LocationDev2024!
ENV MSSQL_PID=Developer
EXPOSE 1433
COPY init-scripts/ /docker-entrypoint-initdb.d/
```

```yaml
# tools/docker-compose.dev.yml (in tools repo)
services:
  sqlserver:
    build: ./docker/sqlserver
    container_name: location-sqlserver
    ports: ["1433:1433"]
    restart: unless-stopped  # Auto-restart on system reboot
    volumes:
      - ${HOME}/.location-dev/sqlserver/data:/var/opt/mssql/data
      - ${HOME}/.location-dev/sqlserver/log:/var/opt/mssql/log
      - ${HOME}/.location-dev/sqlserver/backup:/var/opt/mssql/backup
    environment:
      - SA_PASSWORD=LocationDev2024!
```

#### Auto-Start Container Configuration
**Setup script configures containers to auto-start on user login:**

**Windows (Task Scheduler):**
```powershell
# Created during setup - runs at user login
schtasks /create /tn "LocationContainers" /tr "docker-compose -f tools/docker-compose.dev.yml up -d" /sc onlogon
```

**macOS (launchd):**
```xml
<!-- ~/Library/LaunchAgents/com.location.containers.plist -->
<plist version="1.0">
<dict>
    <key>Label</key>
    <string>com.location.containers</string>
    <key>ProgramArguments</key>
    <array>
        <string>docker-compose</string>
        <string>-f</string>
        <string>/path/to/tools/docker-compose.dev.yml</string>
        <string>up</string>
        <string>-d</string>
    </array>
    <key>RunAtLoad</key>
    <true/>
</dict>
</plist>
```

**Linux (systemd user service):**
```ini
# ~/.config/systemd/user/location-containers.service
[Unit]
Description=Location Development Containers
After=docker.service

[Service]
Type=oneshot
RemainAfterExit=yes
ExecStart=docker-compose -f /path/to/tools/docker-compose.dev.yml up -d
ExecStop=docker-compose -f /path/to/tools/docker-compose.dev.yml down

[Install]
WantedBy=default.target
```

#### Standard Connection Configuration
```json
// Auto-configured in all projects
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=DataConsumption;User Id=sa;Password=LocationDev2024!;TrustServerCertificate=true;"
  }
}
```

**Benefits:**
- **Cross-Platform Consistency**: Same SQL Server version on Windows/macOS/Linux
- **Persistent Data**: Database survives container restarts via mounted volumes  
- **Zero Configuration**: SSMS/tools pre-configured with working connection
- **Version Locked**: Docker ensures identical database behavior across team

### Tool Distribution Architecture
**Default Mode (Azure Artifacts):**
- Latest stable tool versions from enterprise feed
- Azure AD authentication (no token management)
- Consistent versions across all developers
- Fast setup (no compilation required)

**Development Mode (--tools flag):**
- Clones tools repository from Azure DevOps
- Builds all tools from source locally
- Enables tool debugging and contribution
- Complete development environment for tool creators

### Multi-Vertical Workspace Structure
```
~/Location/
├── Location-Core/              # Always present (shared dependency)
├── Location-Photography/       # Added with --photography
├── Location-Fishing/           # Added with --fishing  
├── Location-Hunting/           # Added with --hunting
├── _tools/                     # Built from tools repo (--tools mode only)
└── .location-state.json        # Tracks installed components
```

### Automated Update Strategy (Flow-Aware)
**Critical Integration Requirement:** Tight coupling between code changes → version calculation → schema generation → API generation → mobile adapters requires tool version consistency to prevent pipeline failures.

#### Update Policy Rules
```bash
# Auto-update (silent background, daily 9 AM check)
Gap >= 3 minor versions OR major version = AUTOMATIC UPDATE
Examples:
- Current: 2.0.1 → Available: 2.3.x = AUTO (3+ minor gap)
- Current: 2.0.1 → Available: 3.0.x = AUTO (major version)

# Manual approval with flow protection (VS Code notification)
Gap = 1-2 minor versions = NOTIFY with SNOOZE OPTIONS
Examples:
- Current: 2.0.1 → Available: 2.1.x = NOTIFY (1 minor gap)
- Current: 2.0.1 → Available: 2.2.x = NOTIFY (2 minor gap)
```

#### Flow-Aware Notification Timing
- **Notifications shown only during natural break points:**
  - After successful builds
  - During VS Code startup
  - NOT during active coding sessions
- **Persistent until user action** (prevents ignore-and-forget)
- **Smart snoozing** respects development flow

#### VS Code Notification Interface
```json
{
  "message": "Tool update available: sql-schema-generator 2.0.1 → 2.1.3",
  "persistUntilClosed": true,
  "actions": [
    "Update Now",
    "Snooze 2 Hours",     // Hides for current work session
    "Skip This Version",   // Never show this specific version
    "Remind Tomorrow"      // Show again in 24 hours
  ]
}
```

#### Background Update Implementation
```bash
# Daily scheduled task/cron (9 AM)
location-update-checker --daily-check
├── Check Azure Artifacts for latest tool versions
├── Calculate version gaps for each installed tool
├── Auto-update silently if gap >= 3 minor OR major version
├── Queue flow-aware notifications for 1-2 minor gaps
├── Log all actions for audit trail
└── Trigger VS Code restart prompt if auto-updates occurred
```

#### Rollback Strategy
```bash
# Individual tool rollback
location-update-checker --rollback --tool sql-schema-generator
├── dotnet tool uninstall -g sql-schema-generator
├── dotnet tool install -g sql-schema-generator --version [previous]
└── Log: "Rolled back sql-schema-generator 2.4.0 → 2.0.1"

# Emergency rollback all tools
location-update-checker --rollback-all
```

#### State Management
```json
// ~/.location-dev/update-state.json
{
  "snoozeUntil": {
    "sql-schema-generator": "2025-08-02T16:30:00Z",  // 2 hour snooze
    "location-api-generator": "2025-08-03T09:00:00Z" // remind tomorrow
  },
  "skipVersions": {
    "sql-schema-generator": ["2.1.3"]  // never show this version
  },
  "lastAutoUpdate": "2025-08-02T09:00:00Z"
}
```

**Result:** Aggressive auto-updates maintain pipeline integrity while respecting developer flow, preventing tool drift that could break the tightly integrated version calculation → deployment chain.

## 🔧 Complete Tool Ecosystem

### 1. LocationPREnhancer (PR Orchestrator & Intelligence)
**AI-powered PR analysis tool that orchestrates all other tools and generates actionable insights**

#### Core Responsibilities
- **Orchestrates all ecosystem tools** (version calculation, schema analysis, API impact, mobile compatibility)
- **Generates comprehensive PR intelligence** with actionable recommendations
- **Risk assessment and impact analysis** across the entire technology stack
- **Automated PR comments** with architectural impact summary
- **Deployment readiness scoring** with specific blockers/warnings

#### What Goes into Enhanced PRs
```markdown
## 📊 Architectural Impact Analysis

### 🔢 Version Impact
**Current Main:** `v4.2.1` → **Expected After Merge:** `v5.0.2`

**🚨 Breaking Changes (Major Bump: v4 → v5)**
- Entity Modified: `CameraBody.cs` (+SensorType property)
- SQL Schema Impact: New column, migration required
- API Version Impact: Photography API v4 → v5 (new endpoints)

**📱 Mobile Impact**
- Android: 3 adapters regenerated, 2 new StateFlow properties
- iOS: 3 adapters regenerated, 2 new @Published properties
- MAUI Build: Both platforms affected, test on device recommended

**🗄️ Database Impact**
- 1 table altered, 0 new tables
- 2 new indexes required
- Migration time: ~30 seconds
- Rollback safety: ✅ Safe (nullable column)

**📡 API Impact**
- New endpoints: `/v5/cameras/by-sensor-type`
- Backward compatibility: ✅ Maintained (v4 still available)
- Function App: New deployment required

### 🎯 Deployment Readiness: 85/100
**✅ Safe to Deploy**
- All tests passing
- Database migration validated
- Mobile adapters generated successfully

**⚠️ Recommendations**
- Test new sensor type filtering on device
- Verify API v5 backward compatibility
- Consider coordinated mobile release

### 🔄 Estimated Deployment Time: 8 minutes
- Database migration: 30 seconds
- API deployment: 3 minutes
- Mobile builds: 4 minutes (parallel)
```

#### Smart Intelligence (Targeted PR Value)

**🔍 Code Quality Intelligence (Custom Analysis)**
- **Cyclomatic Complexity**: Method-level complexity analysis via reflection/parsing
- **Maintainability Score**: Composite score based on complexity, size, dependencies
- **Dead Code Detection**: Unused public APIs, unreferenced assemblies, orphaned methods
- **Large File Identification**: Files exceeding thresholds, growth tracking

**📊 Architecture Impact Analysis**
- **Blast Radius Calculation**: Exact assemblies/projects affected by changes
- **Historical Pattern Analysis**: Success rates, timing, and risk patterns for similar changes
- **Cross-Vertical Impact**: How changes cascade through multi-vertical architecture
- **Dependency Chain Effects**: Downstream rebuild requirements

**🗄️ Database Schema Intelligence (SQL Sync Tool Integration)**
- **Expected SQL Changes**: Exact DDL statements that will be generated
- **Migration Complexity Score**: Based on table size, operation type, and risk factors
- **Schema Impact Assessment**: Indexes, foreign keys, and constraints affected
- **Deployment Time Prediction**: Based on table sizes and operation complexity

#### PR Intelligence Output Example

```markdown
## 📊 PR Impact Analysis - Photography Entity Enhancement

### 🔢 Version Impact
**Current Main:** `v4.2.1` → **Expected After Merge:** `v5.0.2`
- **Breaking Change**: CameraBody entity modified (+SensorType property)
- **Cascade Effect**: Photography v5.0.2, Core inherited bump v3.1.5→v3.2.0

### 🔍 Code Quality Analysis
**Cyclomatic Complexity:**
- `CameraAnalysisService.ProcessMetadata()`: 8 → 12 (+4, still acceptable)
- `LensCalculator.CalculateFocalLength()`: 15 → 18 (⚠️ approaching threshold)

**Maintainability Scores:**
- `CameraBody.cs`: 85/100 (↓5 points, entity growth)
- `CameraAnalysisService.cs`: 72/100 (↓8 points, complexity increase)

**Large Files Detected:**
- `CameraAnalysisService.cs`: 267 lines (↑33 lines, 14% growth)
- `LensMetadataProcessor.cs`: 423 lines (⚠️ exceeds 400-line threshold)

**Dead Code Detection:**
- ✅ No unused public methods detected
- `CameraBody.ObsoleteSensorProperty`: Marked obsolete but still referenced (2 usages)

### 📊 Blast Radius Analysis
**Direct Impact:** 
- Photography.Domain (entity change)
- Photography.ViewModels (property binding)
- Photography.Mobile.MAUI (both platforms)

**Indirect Impact (Rebuild Required):**
- Core.ViewModels (dependency chain)
- Photography.Infrastructure (entity mapping)
- Mobile adapters (6 files: 3 Kotlin, 3 Swift)

**Historical Pattern Analysis:**
- **Similar Changes:** 47 entity modifications in Photography vertical
- **Success Rate:** 94% (44/47 successful, 3 required rollback)
- **Average Deploy Time:** 7.3 minutes (range: 4.2-12.8 minutes)
- **Common Issues:** iOS binding complexity (2/3 rollbacks), device testing gaps

### 🗄️ Expected Database Changes
**Schema Migration Preview:**
```sql
-- Generated by SQLServerSyncGenerator
ALTER TABLE [Photography].[CameraBodies] 
ADD [SensorType] NVARCHAR(50) NULL DEFAULT 'Unknown'

-- Index impact analysis
-- IX_CameraBodies_Brand: Will include new column automatically
-- IX_CameraBodies_Model_Year: No impact
```

**Migration Intelligence:**
- **Table Size**: 847,293 rows
- **Operation Type**: Add nullable column with default (SAFE)
- **Estimated Time**: 45 seconds (based on table size + historical data)
- **Rollback Complexity**: 🟢 LOW (simple column drop, no data loss)
- **Index Impact**: 2 existing indexes unaffected, 1 performance improvement opportunity

### 🎯 Deployment Readiness: 88/100
**✅ Ready to Deploy**
- All tests passing (SonarQube integration)
- Breaking changes properly versioned
- Database migration validated

**⚠️ Recommendations**
- Consider refactoring `LensCalculator.CalculateFocalLength()` (complexity: 18)
- Monitor `CameraAnalysisService.cs` growth (approaching 300-line threshold)
- iOS device testing recommended (historical binding issues with entity changes)
- Remove obsolete property after migration (currently still referenced)

### ⏱️ Deployment Estimates
- **Database Migration**: 45 seconds
- **API Deployment**: 3.2 minutes (v5 Function App)
- **Mobile Builds**: 4.8 minutes (parallel Android/iOS)
- **Total Deployment Time**: ~8.5 minutes

### 🔄 Rollback Plan
- **Database**: Simple column drop (45 seconds)
- **API**: Revert to v4 Function App (2 minutes)
- **Mobile**: Previous APK/IPA available in artifacts
- **Total Rollback Time**: ~3.2 minutes
```

#### Implementation Focus Areas

**🔍 Cyclomatic Complexity Analysis**
- **Method-level parsing**: Extract control flow statements (if, while, for, switch, catch)
- **Threshold-based warnings**: Flag methods >15 complexity, critical at >25
- **Growth tracking**: Compare complexity before/after changes
- **Refactoring suggestions**: Specific methods needing attention

**📈 Maintainability Score Calculation**
```csharp
// Composite score formula
MaintainabilityScore = (
    (100 - CyclomaticComplexity * 2) * 0.3 +
    (100 - FileSize / 10) * 0.2 +
    (100 - DependencyCount * 3) * 0.2 +
    (TestCoveragePercent) * 0.2 +
    (100 - PublicAPICount / 5) * 0.1
)
```

**💀 Dead Code Detection Logic**
- **Unused public methods**: Not referenced by any assembly
- **Orphaned classes**: No instantiation found across solution
- **Unreferenced assemblies**: Dependencies without usage
- **Obsolete API usage**: Methods/properties marked obsolete but still used

**📏 Large File Identification**
- **Size thresholds**: 200 lines (warning), 400 lines (critical)
- **Growth tracking**: File size changes over time
- **Complexity correlation**: Large files with high complexity = high priority

**🗄️ SQL Sync Tool Integration**
- **Live schema analysis**: Call SQLServerSyncGenerator in preview mode
- **Exact DDL generation**: Show actual SQL statements that will execute
- **Risk assessment**: Table sizes vs operation types
- **Historical migration data**: Previous migration times for similar operations

#### PR Enhancement Flow
1. **PR Created/Updated** → Triggers LocationPREnhancer
2. **Orchestrates Analysis**:
   - Calls LocationVersionCalculator (version impact)
   - Calls SQLServerSyncGenerator (database impact analysis)
   - Calls APIGenerator (API compatibility check)
   - Calls PhotographyAdapterGenerator (mobile impact analysis)
3. **Risk Assessment**: Analyzes combined impact across stack
4. **Generates Actionable Report**: Tailored recommendations per stakeholder
5. **Posts PR Comment**: Comprehensive but scannable summary
6. **Updates on Changes**: Re-analyzes when PR is updated

#### Advanced PR Intelligence Features
- **Deployment time prediction** based on change complexity
- **Test scenario generation** from architectural changes  
- **Risk scoring** with specific mitigation recommendations
- **Stakeholder notifications** for high-impact changes
- **Historical comparison** ("similar change took X minutes last time")
- **Dependency impact analysis** ("this change affects 3 other teams")
- **Automated release notes generation** from PR analysis and version metadata

### Automated Release Notes Generation
**Metadata-Driven Documentation:**
- **Entity Changes**: Auto-detected from `[ExportToSQL]` modifications → "New camera sensor type support"
- **API Changes**: Reflected from domain changes → "Added /v5/cameras/by-sensor-type endpoint"  
- **Database Changes**: Generated from SQLGenerator analysis → "Added SensorType column to CameraBodies table"
- **Mobile Changes**: Detected from ViewModel modifications → "Updated camera selection UI with sensor filtering"

**Release Notes Output:**
```markdown
# Photography v5.0.2 Release Notes

## 🎯 New Features
- **Camera Sensor Type Support**: Added ability to categorize cameras by sensor type (Full Frame, APS-C, Micro Four Thirds)
- **Enhanced Camera Search**: Filter cameras by sensor type in mobile app

## 📊 API Changes  
- **New Endpoint**: `POST /photography/v5/cameras/by-sensor-type`
- **Backward Compatibility**: All v4 endpoints remain fully supported

## 🗄️ Database Updates
- **Schema Changes**: Added SensorType column to CameraBodies table
- **Migration Time**: ~45 seconds (automated, zero downtime)

## 📱 Mobile Updates
- **Android**: Updated camera selection with sensor type filters
- **iOS**: New sensor type picker in camera details

## 🔧 Developer Impact
- **Breaking Changes**: None (major version maintains compatibility)
- **New Properties**: CameraBody.SensorType available in all APIs
- **Adapter Updates**: Kotlin and Swift adapters regenerated automatically

## 📈 Performance  
- **New Index**: ix_camerabodies_sensortype for optimized filtering
- **Query Performance**: 40% faster camera searches with sensor filters

Generated automatically from: Photography.5.0.2.1544 (commit abc123def)
```

**Zero Manual Documentation:**
- Release notes generated from PR intelligence analysis
- Version metadata provides exact technical details
- Historical pattern analysis predicts user impact
- Stakeholder-specific formatting (technical vs marketing versions)

### Automated Azure DevOps Wiki Integration
**Release Notes Structure:**
```
/Releases/
├── 2025/01/
│   ├── Photography, v5.0.2 - January 15, 2025
│   └── Core, v3.2.1 - January 28, 2025
└── 2025/02/
    └── Fishing, v3.1.0 - February 5, 2025
```

**Wiki Page Content (Auto-Generated):**
```markdown
# Photography v5.0.2 - January 15, 2025

## 📋 Epic Progress
### Epic: [Camera Management Enhancement](ado-link-to-epic)
- **Features Completed**: 2 of 3
- **Features Remaining**: 1

#### ✅ Feature: [Camera Sensor Type Support](ado-link-to-feature)
- **Stories Completed**: 3 of 3
  - [Add SensorType to CameraBody entity](ado-link-to-story)
  - [Update camera search API](ado-link-to-story)
- **Stories Remaining**: 0

## 🔄 Unparented Work Items
- **Closed Unparented**: 2 items
- **Open Unparented**: 1 item
```

**Complete Audit Trail:**
- Epic → Feature → Story → Code → Deployment → Production
- Zero-gap traceability eliminates traditional change control
- Automated approval through code-driven risk analysis
- Self-documenting compliance without ITIL/ITSM overhead

### 2. LocationVersionCalculator (Version Orchestrator)
**CI/CD-only versioning tool called by LocationPREnhancer for architecture-driven version calculation**

#### Installation & Distribution
- **Standard Developers**: Installed via Azure Artifacts (latest stable)
- **Tool Developers**: Built from source in tools repository
- **Authentication**: Azure AD integration with Azure DevOps
- **Updates**: Background notifications for version updates
- **Access**: Installed as global .NET tool for CI/CD integration

#### Core Responsibilities
- Architecture-driven version calculation with dependency resolution
- Scan changed files across entire solution
- Apply versioning rules (Major/Minor/Patch detection)  
- Calculate downstream impacts across verticals
- Generate version manifests for all affected components

#### Versioning Rules (Final)
**Two Version Number System:**
- **Internal**: `Major.Minor.Patch.Build` (Azure Artifacts, traceability)
- **External**: `Major.Minor.Patch` (App Stores, clean semantic versioning)

**Automatic Bump Detection Rules:**
1. **Major**: ANY `[ExportToSQL]` entity class change (applies to both internal/external)
2. **Minor**: ANY new file created
   - Internal: +1 exactly 
   - External: Qualitative amount (set in UI projects)
3. **Patch**: ANY file edited (not new, not entity)
   - Both: +1 for each edited file
4. **Build**: CI/CD build number (internal only)

#### Branching & Version Calculation Strategy
**Branch Flow:** `feature/story-123 → beta → main → downstream actions`

**Version Calculation Points:**
1. **PR to Beta (Preview Mode)**: Calculate from last main merge, show proposed version
2. **Merge to Beta (Provisional Versioning)**: Create beta tags (both internal/external)
3. **Merge to Main (Final Versioning)**: **MUST RECALCULATE** - handles rejected PRs

**Critical Decision:** Main must recalculate to handle rejected PRs accurately

#### Usage
```bash
# CI/CD only - triggered on every commit
location-version-calculator --analyze --from-commit abc123
# Output: Core: 3.1.5 → 3.2.0, Photography: 5.0.2 → 6.0.0
```

### 3. SQLServerSyncGenerator (29-Phase Database Deployment)
**Automated SQL Server schema generation with comprehensive safety features**

#### Repository-Specific Schema Generation
- **Core Repository**: NO database deployments (entities consumed via NuGet)
- **Module Repositories**: Reflect over Core + Module entities, deploy combined schema
- **sql-schema-generator**: Only runs on major version bumps (entity changes)
- **Generated Output**: `_compiled_deployment_photography_v6.0.0.sql` (includes Core + Photography schema)

#### 29-Phase Deployment Pipeline
1. **Create Tables** - Entity-driven table creation
2. **Primary Key Indexes** - Essential constraints first  
3. **Unique Indexes** - Data integrity
4. **Reference Data** - SqlScripts/04-reference-data/
5. **Foreign Key Constraints** - After referenced data exists
6. **Non-Clustered Indexes** - Performance optimization
7. **Composite Indexes** - Complex indexing strategies
...continuing through all 29 phases including stored procedures, triggers, permissions, and maintenance

#### Enterprise Safety Features
- **Azure-native point-in-time restore** before production changes
- **Comprehensive validation** (Safe/Warning/Blocked classifications)
- **Automatic rollback** on production failure
- **Single transaction deployment** with full rollback capability

#### Compiled Deployments
- **Perfect traceability**: `_compiled_deployment_v1.2.4.sql`
- **Git integration**: Auto-consumes scripts and commits compiled versions
- **Version manifests**: Complete dependency trees

#### Usage
```bash
# Auto-runs after Domain builds (MSBuild integration)
sql-schema-generator --execute --prod \
  --server "myserver.database.windows.net" \
  --database "LocationAnalytics" \
  --keyvault-url "https://myvault.vault.azure.net/"
```

### 4. APIGenerator (Domain Driven Versioning)
**Schema-perfect REST API generation with unlimited backward compatibility**

#### Core Capabilities
- **Auto-discovers** `[ExportToSQL]` entities from Infrastructure assemblies
- **Generates complete Azure Functions** with all endpoints
- **Creates Bicep infrastructure** templates  
- **Builds and deploys** to Azure
- **Publishes to Azure Artifacts** (Major.0.0 versioning)

#### Generated API Structure
For each vertical (e.g., photography v4):
- `POST /photography/v4/auth/request-qr`
- `POST /photography/v4/auth/verify-email`
- `POST /photography/v4/auth/generate-qr`
- `POST /photography/v4/backup`
- `POST /photography/v4/forgetme`

#### Deployment Strategy
- **Major version changes** → New Function App (`v4` → `v5`)
- **Minor version changes** → Same Function App (idempotent deployment)
- **Unlimited backward compatibility** - All versions maintained indefinitely

#### Usage
```bash
# Complete pipeline (recommended)
location-api-generator --auto-discover \
  --server "myserver.database.windows.net" \
  --database "LocationAnalytics" \
  --keyvault-url "https://myvault.vault.azure.net/" \
  --azure-subscription "sub-id" \
  --resource-group "rg-locationapis"
```

### 5. PhotographyAdapterGenerator (Ephemeral Mobile Adapters)
**"Truly stupid" mobile adapters with universal StateFlow two-way binding**

#### Core Philosophy
- **Ephemeral generation**: Build-time only, never checked in
- **Universal patterns**: StateFlow (Android) / @Published (iOS)
- **Zero business logic**: Pure bridging between .NET ViewModels and native platforms
- **Auto-runs**: MSBuild integration after ViewModel builds

#### Generated Output Structure
**Android (Kotlin):**
```kotlin
class CameraAdapter @Inject constructor(
    private val dotnetViewModel: CameraViewModel
) : ViewModel() {
    // Universal StateFlow pattern
    private val _brand = MutableStateFlow(dotnetViewModel.Brand)
    val brand: StateFlow<String> = _brand.asStateFlow()
    
    // Two-way binding for read-write properties
    init {
        brand.onEach { newValue ->
            dotnetViewModel.Brand = newValue
        }.launchIn(viewModelScope)
    }
}
```

**iOS (Swift):**
```swift
class CameraAdapter: ObservableObject {
    private let dotnetViewModel: CameraViewModel
    
    @Published var brand: String
    
    init(dotnetViewModel: CameraViewModel) {
        self.dotnetViewModel = dotnetViewModel
        self.brand = dotnetViewModel.Brand
        
        // Two-way binding
        $brand.dropFirst().sink { [weak self] newValue in
            self?.dotnetViewModel.Brand = newValue
        }.store(in: &cancellables)
    }
}
```

#### Platform & Channel Matrix
Since backend uses .NET MAUI (single codebase, multiple platform builds):
- **LocationMobile-Photography-Android-Beta**
- **LocationMobile-Photography-Android-Production**  
- **LocationMobile-Photography-iOS-Beta**
- **LocationMobile-Photography-iOS-Production**

#### Usage
```bash
# Auto-runs after ViewModel builds via MSBuild
# Or manual generation:
photography-viewmodel-generator --platform android --output "generated/"
```

## 🗂️ Complete Azure Artifacts Strategy

### Granular Feeds (Maximum Control)

#### Repository-Specific Feeds

**Core Domain Libraries:**
```
LocationLibraries-Core/
├── Location.Core.Domain.3.1.5.nupkg
├── Location.Core.ViewModels.3.1.5.nupkg
└── Location.Core.Infrastructure.3.1.5.nupkg

LocationLibraries-Photography/
├── Location.Photography.Domain.5.0.2.nupkg
├── Location.Photography.ViewModels.5.0.2.nupkg
└── Location.Photography.Infrastructure.5.0.2.nupkg
```

**Mobile Apps (Platform × Channel Matrix):**
```
LocationMobile-Photography-Android-Beta/
├── Photography.5.0.0-beta.1543-internal.apk
├── Photography.5.0.0-beta-external.apk

LocationMobile-Photography-Android-Production/
├── Photography.4.2.1.1544-internal.apk
├── Photography.4.2.1-external.apk

LocationMobile-Photography-iOS-Beta/
├── Photography.5.0.0-beta.1543-internal.ipa
├── Photography.5.0.0-beta-external.ipa

LocationMobile-Photography-iOS-Production/
├── Photography.4.2.1.1544-internal.ipa
├── Photography.4.2.1-external.ipa
```

**API Packages (Generated Function Apps):**
```
LocationAPIs-Photography/
├── location-photography-api-v4.0.0.zip
├── location-photography-api-v5.0.0.zip

LocationAPIs-Fishing/
├── location-fishing-api-v2.0.0.zip
```

**x3squaredcircles Tool Suite:**
```
x3squaredcircles-tools/
├── location-version-calculator.1.2.4.nupkg
├── sql-schema-generator.2.0.1.nupkg
├── location-api-generator.1.0.0.nupkg
├── photography-adapter-generator.3.1.0.nupkg
└── location-pr-enhancer.1.5.2.nupkg
```

## 🔄 Complete Traceability Chain

### Disaster Recovery Flow
1. **APK crashes** → Photography v2.1.0
2. **Azure Artifacts** → Download Photography.2.1.0-internal.apk  
3. **Source** → `_compiled_version_v2.1.0.1523.json`
4. **Dependencies** → Core v2.0.1.1247, Photography Domain v1.8.3.1245
5. **Database** → `_compiled_deployment_v1.8.3.sql`
6. **Result**: EXACT state recreation for debugging

### Git Integration
- **Compiled deployments** committed to source
- **Version manifests** with full dependency trees
- **Automatic script consumption** and cleanup
- **Perfect audit trail**: git log shows every deployment

## 🛡️ Enterprise Safety Features

### Production Deployment Protection
- **Azure-native point-in-time restore** before changes
- **Comprehensive validation** (safe/warning/blocked classification)
- **Automatic rollback** on deployment failure
- **Single transaction deployment** with full rollback capability

### Development Safety
- **Safe defaults**: All tools run in noop mode initially
- **Git hooks**: Prevent committing generated files (adapters)
- **Version validation**: Impossible to miss version implications
- **Break-glass overrides**: Production configuration when needed

## 📊 Tag Strategy & Downstream Actions

### Tag Creation (LocationVersionCalculator)
**Beta Tags (Provisional):**
```
beta/photography-v5.0.0.1543-internal
beta/photography-v5.0.0-external
beta/core-v3.2.0.1543-internal
beta/core-v3.2.0-external
```

**Main Tags (Final, Authoritative):**
```
main/photography-v5.0.0.1544-internal
main/photography-v5.0.0-external
main/core-v3.2.0.1544-internal
main/core-v3.2.0-external
```

### Downstream Actions Triggered by Main Tags
1. **Azure Artifacts Publishing** (internal versions to appropriate feeds)
2. **Mobile App Builds** (platform-specific builds using external versions)
3. **API Deployments** (backend service deployments using APIGenerator)
4. **Database Schema Updates** (SQLServerSyncGenerator with compiled deployments)
5. **Adapter Generation** (ephemeral mobile adapters via PhotographyAdapterGenerator)

## 🎯 Developer Experience Goals

### Zero Friction Adoption
```bash
# Complete developer workflow - any platform (Windows/macOS/Linux)
1. ./setup-dev.sh --photography  # Single command setup
2. dotnet build                  # Everything works immediately
# ✅ Full development environment ready in minutes
```

### Invisible Productivity
- **Auto-runs after builds** (MSBuild integration)
- **Immediate value** (instant feedback on schema drift, mobile compatibility)
- **No configuration required** (sane defaults everywhere)
- **Progressive enhancement** (more power when needed)

### Perfect MTTR (Mean Time To Recovery)
- **Before**: 2+ hours (investigate → plan → execute rollback)
- **After**: 6 minutes (`git log` → `--rollback-to-previous`)

## 📋 Required Supporting Documentation

### Branch Policies (Azure DevOps)
- **Main Branch**: Locked, requires PR approval + build validation
- **Beta Branch**: Semi-protected, requires build validation
- **Feature Branches**: No restrictions, must target beta only
- **Version Tag Protection**: Prevent manual tag creation/deletion

### Developer Documentation Requirements
- **Branching Workflow**: Step-by-step feature development process
- **Version Impact Guide**: How code changes affect version numbers
- **PR Guidelines**: What information to include in PR descriptions
- **Troubleshooting**: Common versioning scenarios and resolutions
- **Tool Configuration**: MSBuild integration setup per tool

## 🔮 Architecture Benefits

### For Developers
- **100% automated toolchain** with human oversight
- **Instant feedback** on architectural impact
- **Zero configuration** development environment
- **Perfect local/production parity**

### For Operations
- **Sub-10-minute MTTR** for production issues
- **Complete audit trail** for compliance
- **Automated deployment safety** with enterprise rollback
- **Version-driven retention** based on actual usage

### For Business
- **Rapid feature delivery** without operational risk
- **Perfect backward compatibility** (unlimited API versions)
- **Predictable release cycles** (beta→production gates)
- **Cost optimization** (sunset unused versions automatically)

## 🚀 Implementation Roadmap

### Phase 1: LocationVersionCalculator (Primary)
- Git change detection logic
- Version bump rule engine  
- Multi-component dependency resolution
- Tag creation and CI/CD integration

### Phase 2: Complete Tool Integration
- SQLServerSyncGenerator (database deployments)
- APIGenerator (REST API generation)
- PhotographyAdapterGenerator (mobile adapters)
- Azure Artifacts feed management

### Phase 3: Safety & Documentation
- Branch policy configuration
- Developer workflow documentation
- Production deployment procedures
- Monitoring and alerting setup

## 📈 Success Metrics

### Technical Metrics
- **Deployment Time**: < 10 minutes for any change
- **Rollback Time**: < 6 minutes to any previous version
- **Version Accuracy**: 100% traceability from crash to source
- **Build Success Rate**: > 99% with automated validation

### Business Metrics
- **Feature Velocity**: Faster delivery without operational risk
- **Incident Resolution**: Sub-10-minute MTTR
- **Compliance**: Perfect audit trail for all changes
- **Cost Efficiency**: Automated version lifecycle management

---

**Result: A self-healing, architecture-driven development ecosystem that transforms manual, error-prone processes into automated, auditable, enterprise-grade operations with perfect traceability and sub-10-minute MTTRs across the entire technology stack.**