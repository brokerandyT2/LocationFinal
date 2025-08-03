# Photography ViewModel Generator

Generates truly stupid mobile adapters for Photography ViewModels. These adapters just bridge .NET ViewModels to Android/iOS platforms without any business logic.

## Installation

```bash
# Build and install as global tool
dotnet pack
dotnet tool install -g --add-source ./bin/Debug Location.Photography.AdapterGenerator
```

## Usage

```bash
# Generate Android adapters
photography-viewmodel-generator --platform android --output "src/main/kotlin/generated/"

# Generate iOS adapters  
photography-viewmodel-generator --platform ios --output "Sources/Generated/"

# With custom assembly paths
photography-viewmodel-generator --platform android --output "./generated/" \
  --core-assembly "path/to/Location.Core.ViewModels.dll" \
  --photography-assembly "path/to/Location.Photography.ViewModels.dll"

# Enable verbose logging
photography-viewmodel-generator --platform android --output "./generated/" --verbose
```

## What It Does

1. **Discovers ViewModels** - Finds all classes ending in "ViewModel" that inherit from `BaseViewModel` or `ViewModelBase`
2. **Analyzes Structure** - Uses reflection to discover properties, commands, events, and constructor dependencies
3. **Generates Adapters** - Creates "truly stupid" platform-specific adapters that just bridge types

## Generated Output

### Android (Kotlin)
```kotlin
/**
 * This is a truly stupid adapter that just bridges stuff.
 * It should never be smart. Ever.
 * (Now even more stupid because Akavache handles all the caching!)
 */
package com.3xSquaredCircles.photography.NoOpViewModelAdapters

class AstroPhotographyCalculatorAdapter @Inject constructor(
    private val dotnetViewModel: Location.Photography.ViewModels.AstroPhotographyCalculatorViewModel
) : ViewModel() {
    
    // Direct property access - let .NET handle threading
    val isCalculating: Boolean get() = dotnetViewModel.isCalculating
    
    // ObservableCollection → StateFlow
    private val _hourlyPredictions = MutableStateFlow<List<AstroHourlyPredictionDisplayModel>>(emptyList())
    val hourlyPredictions: StateFlow<List<AstroHourlyPredictionDisplayModel>> = _hourlyPredictions.asStateFlow()
    
    // Commands
    suspend fun calculateAstroData(): Result<Unit> = // ...
    
    override fun onCleared() {
        super.onCleared()
        dotnetViewModel.dispose()
    }
}
```

### iOS (Swift)
```swift
class AstroPhotographyCalculatorAdapter: ObservableObject {
    private let dotnetViewModel: Location.Photography.ViewModels.AstroPhotographyCalculatorViewModel
    
    // Direct property access
    var isCalculating: Bool {
        get { return dotnetViewModel.isCalculating }
    }
    
    // ObservableCollection → @Published
    @Published var hourlyPredictions: [AstroHourlyPredictionDisplayModel] = []
    
    // Commands
    func calculateAstroData() async -> Result<Void, Error> { // ... }
    
    deinit {
        dotnetViewModel.dispose()
    }
}
```

## Architecture

- **Assembly Loading** - Finds `Location.Core.ViewModels.dll` and `Location.Photography.ViewModels.dll`
- **Reflection Analysis** - Discovers ViewModel structure without modifying source code
- **Template Generation** - Uses Razor templates to generate clean, platform-specific code
- **Type Translation** - Maps .NET types to Kotlin/Swift equivalents

## Exit Codes

- `0` - Success
- `1` - Error (invalid arguments, assembly not found, generation failed)

## Requirements

- .NET 9 SDK
- Built ViewModel assemblies (`Location.Core.ViewModels.dll`, `Location.Photography.ViewModels.dll`)
- Target output directory must exist

## Azure DevOps Integration

```yaml
- task: PowerShell@2
  displayName: 'Generate ViewModel Adapters'
  inputs:
    script: |
      photography-viewmodel-generator --platform android --output "src/main/kotlin/generated/"
```