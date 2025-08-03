# Version Detective

**Automated versioning for .NET solutions with database schema dependencies** - Provides dual versioning strategies (semantic and marketing) while ensuring database schema changes drive major version increments across all projects in a solution.

## 🎯 What Makes This Different

- **Schema-Driven Versioning**: Database schema changes automatically trigger major version bumps
- **Dual Versioning Strategy**: Semantic (`v5.0.0`) for technical audiences, Marketing (`v4.15.3`) for business impact
- **Quantitative Analysis**: Count-based increments for ViewModels, API endpoints, services, and bug fixes
- **Solution Coordination**: All projects in solution receive same final version number
- **Pipeline-First**: Designed for CI/CD environments with zero local dependencies

## 🚀 Quick Start

### Installation
```bash
dotnet tool install -g x3squaredcircles.Version.Calculator
```

### Basic Usage
```bash
# Analyze changes for PR (preview mode)
version-detective --branch beta --mode pr

# Deploy mode for main branch (creates tags and updates versions)
version-detective --branch main --mode deploy
```

## 📊 Version Detective Rules

### Rule 1: Database Schema Detection (Major Version Driver)
**Primary Rule**: `Location.{Vertical}.Domain` projects drive major version increments

**Detection Method**:
- Scans all entities with `[ExportToSQL]` attributes
- Compares current schema vs. last git tag version
- Any schema change triggers major version bump for ALL projects in solution

**Schema Changes Detected**:
- New entities
- Removed entities
- New properties on existing entities
- Removed properties from existing entities
- Property type changes

### Rule 2: Quantitative Minor Version Increments
**Change Types & Increments**:
- New ViewModel class: +1 minor
- New API endpoint: +1 minor
- New service class: +1 minor
- New feature implementation: +1 minor

### Rule 3: Quantitative Patch Version Increments
**Change Types & Increments**:
- Bug fixes: +1 patch
- Performance improvements: +1 patch
- Documentation updates: +1 patch
- Refactoring without behavior change: +1 patch

### Rule 4: Multi-Project Coordination
**Behavior**:
- Version Detective scans ALL projects in solution
- `Location.{Vertical}.Domain` drives major version decisions
- If Domain has schema changes → ALL projects get major bump
- If no schema changes → projects increment based on individual changes
- All projects in solution receive same final version number

### Rule 5: Dual Versioning Strategy
**Semantic Version**: Traditional SemVer for technical audiences
- Format: `v{MAJOR}.{MINOR}.{PATCH}`
- Follows semantic versioning principles
- Used for git tags: `semver/5.0.0`

**Marketing Version**: Quantitative for business/marketing impact
- Format: `v{MAJOR}.{VOLUME_MINOR}.{VOLUME_PATCH}`
- Reflects development effort and change volume
- Used for git tags: `marketing/4.15.3`

## 🔄 Workflow Integration

### Beta Branch Analysis (PR Mode)
**Trigger**: Pull request to beta branch
```bash
version-detective --branch beta --mode pr
```

**Behavior**:
- Analyze ALL changes since last main branch tag
- Calculate expected version (preliminary)
- Generate analysis output
- No git tags created

**Output Example**:
```
🏷️ Version Detective Analysis
(Preliminary - final version determined at deployment)

Current Version: v4.7.0
Semantic Version: v5.0.0
Marketing Version: v4.15.3

🔄 MAJOR: Database schema changes detected
  - NewProperty: CameraEntity.SensorSize property added

✨ MINOR: 8 new features added
  - 3 new ViewModels
  - 2 new API endpoints
  - 3 new services

🐛 PATCH: 3 improvements made
  - 2 bug fixes
  - 1 performance improvements

Reasoning: Database schema changes require major version bump. New features added: 3 ViewModels, 2 API endpoints, 3 services. Improvements made: 2 bug fixes, 1 performance improvements.
```

### Main Branch Deployment (Deploy Mode)
**Trigger**: Merge to main/master branch
```bash
version-detective --branch main --mode deploy
```

**Behavior**:
1. Re-analyze final changeset since last main tag
2. Apply schema change rules
3. Calculate final versions (semantic + marketing)
4. Create git tags
5. Update project assembly versions
6. Generate release notes

**Git Tags Created**:
```
semver/5.0.0
marketing/4.15.3
build/5.0.0+{azure-devops-build-id}
```

## 📁 Solution Auto-Detection

Version Detective automatically detects solution structure:

### Core Solution
```
Location.Core.sln → Independent versioning, published to Azure Artifacts
```

### Vertical Solutions
```
Location.Photography.sln → Depends on Core packages (manual version control)
Location.Fishing.sln → Depends on Core packages (manual version control)
Location.Hunting.sln → Depends on Core packages (manual version control)
```

### Dependency Management
**Manual Control Policy**:
- NO automatic Core package updates in vertical solutions
- Photography/Fishing/Hunting teams manually choose when to upgrade Core dependencies
- Version Detective ignores Core package version changes for versioning calculations
- Breaking changes in Core do not automatically force vertical solution updates

## 🔍 Schema Change Detection

### Entity Discovery
Version Detective scans Domain assemblies for entities with `[ExportToSQL]` attributes:

```csharp
[ExportToSQL]
public class CameraEntity
{
    public int Id { get; set; }
    public string Brand { get; set; }
    public string SensorSize { get; set; }  // New property = major version bump
}
```

### Detection Process
1. **Load Domain Assembly** - Finds `Location.{Vertical}.Domain.dll`
2. **Scan for Entities** - Gets all classes with `[ExportToSQL]`
3. **Compare with Baseline** - Uses git to get entities from last tag
4. **Identify Changes** - Compares current vs baseline entity structure
5. **Trigger Major Bump** - Any schema change drives major version increment

## 🎯 Pipeline Integration

### Azure DevOps
```yaml
- stage: VersionAnalysis
  jobs:
  - job: AnalyzeVersion
    steps:
    - task: DotNetCoreCLI@2
      displayName: 'PR Version Analysis'
      inputs:
        command: 'custom'
        custom: 'tool'
        arguments: 'run version-detective --branch $(Build.SourceBranchName) --mode pr'
      condition: ne(variables['Build.SourceBranch'], 'refs/heads/main')

    - task: DotNetCoreCLI@2
      displayName: 'Deploy Version'
      inputs:
        command: 'custom'
        custom: 'tool'
        arguments: 'run version-detective --branch $(Build.SourceBranchName) --mode deploy'
      condition: eq(variables['Build.SourceBranch'], 'refs/heads/main')
```

### GitHub Actions
```yaml
- name: Version Analysis (PR)
  if: github.ref != 'refs/heads/main'
  run: dotnet tool run version-detective --branch ${{ github.ref_name }} --mode pr

- name: Version Deployment (Main)
  if: github.ref == 'refs/heads/main'
  run: dotnet tool run version-detective --branch ${{ github.ref_name }} --mode deploy
```

## 📈 Release Notes Generation

Version Detective automatically generates comprehensive release notes:

```markdown
# Version 4.15.3 Release Notes

**Technical Version:** 5.0.0
**Release Date:** 2024-08-03

## 🎉 What's New
🚀 **Major Update** - This release includes significant architectural improvements with database enhancements.

## 🔧 Breaking Changes
⚠️ **Important:** This version includes database schema changes that require migration.

- 🆕 **NewProperty**: CameraEntity.SensorSize property added

## 🎉 New Features (8)
### 📱 User Interface
- **3 new screens/views** for enhanced user experience

### 🔗 API Enhancements  
- **2 new API endpoints** for extended functionality

### ⚙️ Backend Services
- **3 new services** for improved capabilities

## 🐛 Bug Fixes & Improvements (3)
### 🔧 Bug Fixes
- **2 issues resolved** for improved stability

### ⚡ Performance Improvements
- **1 optimizations** for better performance
```

## 🛠️ CLI Commands

### Core Commands
```bash
# Analyze changes for PR
version-detective --branch beta --mode pr

# Deploy mode for main branch
version-detective --branch main --mode deploy

# Analyze from specific commit
version-detective --branch main --mode pr --from abc123

# Verbose output
version-detective --branch beta --mode pr --verbose
```

### Advanced Options
```bash
# Production mode (additional validation)
version-detective --branch main --mode deploy --prod

# Validation only (no changes)
version-detective --branch main --validate-only
```

## 🔧 Configuration

Version Detective works with zero configuration by following conventions:

### Solution Detection
- Automatically finds `.sln` files
- Determines Core vs Vertical solution types
- Locates Domain projects by naming convention

### Git Integration
- Uses `git` command line tool
- Requires valid git repository
- Analyzes changes since last version tag

### Assembly Analysis
- Scans built Domain assemblies for `[ExportToSQL]` entities
- Requires projects to be built before analysis
- Uses reflection to compare entity structures

## 🎯 Requirements

- .NET 9 SDK
- Git repository with linear history
- Built Domain assemblies with `[ExportToSQL]` entities
- Git command line tools available

## 📊 Example Scenarios

### Scenario 1: New Feature Development
```bash
# Developer adds new ViewModel and API endpoint
# No schema changes

version-detective --branch beta --mode pr
# Output: v4.7.0 → v4.9.0 (semantic: v4.9.0, marketing: v4.9.0)
# Reasoning: 1 new ViewModel, 1 new API endpoint = +2 minor
```

### Scenario 2: Database Schema Change
```bash
# Developer adds new property to CameraEntity
# Schema change detected

version-detective --branch beta --mode pr
# Output: v4.7.0 → v5.0.0 (semantic: v5.0.0, marketing: v4.8.0)
# Reasoning: Schema changes require major version bump
```

### Scenario 3: Bug Fix Release
```bash
# Developer fixes 3 bugs, no new features
# No schema changes

version-detective --branch beta --mode pr
# Output: v4.7.0 → v4.7.3 (semantic: v4.7.3, marketing: v4.7.3)
# Reasoning: 3 bug fixes = +3 patch
```

## 🤝 Contributing

Version Detective is designed for enterprise use with the Location platform architecture. Contributions should align with:

- Convention over configuration
- Pipeline-first design
- Zero local dependencies
- Schema-driven versioning principles

## 📝 License

Proprietary - 3xSquaredCircles

---

**Transform your versioning from manual, error-prone processes into automated, predictable, business-aligned version management.**