# Location.Core.Helpers

Shared utilities and attributes for Location projects.

## When Do You Need This Package?

**Most projects DON'T need this package.** Only add this reference if you need to customize adapter generation behavior.

### ✅ Add This Package If You Need:
- Custom type mappings (e.g., `GeographicCoordinate` → `LatLng`/`CLLocationCoordinate2D`)
- DateTime semantic mapping (e.g., `UtcInstant`, `DateOnly`, `TimeOnly`)
- Platform-specific properties (Android-only or iOS-only features)
- Custom naming or exclusions from adapter generation

### ❌ DON'T Add This Package If:
- Your ViewModels only use standard types (`string`, `int`, `bool`, `double`, etc.)
- You don't need any customization of adapter generation
- Your ViewModels work fine with default adapter generation

## Installation

```xml
<!-- Only add this to projects that need customization -->
<PackageReference Include="Location.Core.Helpers" Version="2.1.0" />
```

## Usage Examples

### Standard ViewModel (No Package Needed)
```csharp
// This ViewModel needs ZERO package references or attributes
public class SimpleLocationViewModel : BaseViewModel
{
    public string Address { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int ResultCount { get; set; }
    public bool IsSearching { get; set; }
    public ObservableCollection<Location> Results { get; set; } = new();
    public IAsyncCommand SearchCommand { get; set; }
}
// ✅ Generates perfect Android/iOS adapters automatically
```

### Customized ViewModel (Package Required)
```csharp
// Add package reference first, then use attributes
using Location.Core.Helpers.AdapterGeneration;

public class AdvancedLocationViewModel : BaseViewModel
{
    // Standard properties - no attributes needed
    public string SearchQuery { get; set; } = string.Empty;
    public bool IsSearching { get; set; }
    
    // Custom type mapping - Geographic coordinates
    [MapTo(AndroidType.LatLng, IOSType.CLLocationCoordinate2D)]
    public GeographicCoordinate SearchCenter { get; set; }
    
    // DateTime semantics - API timestamps
    [DateType(DateTimeSemantic.UtcInstant)]
    public DateTime LastApiCall { get; set; }
    
    // DateTime semantics - User-selected date
    [DateType(DateTimeSemantic.DateOnly)]
    public DateTime SelectedDate { get; set; }
    
    // Byte array signedness handling
    [MapTo(AndroidType.ByteArray, IOSType.UInt8Array)]
    public byte[] MapTileData { get; set; } = Array.Empty<byte>();
    
    // Platform-specific features
    [Available(true, false)]  // Android only
    public bool UseGoogleMapsApi { get; set; }
    
    [Available(false, true)]  // iOS only
    public bool UseAppleMapsKit { get; set; }
    
    // Exclude internal diagnostics
    [Exclude("Internal caching - not needed in mobile")]
    public Dictionary<string, object> SearchCache { get; set; } = new();
    
    // Standard commands
    public IAsyncCommand SearchCommand { get; set; }
}
```

## Available Attributes

### Type Mapping
```csharp
[MapTo(AndroidType.LatLng, IOSType.CLLocationCoordinate2D)]
[MapTo(AndroidType.ByteArray, IOSType.UInt8Array)]
[MapTo(AndroidType.UUID, IOSType.UUID)]
```

### DateTime Semantics
```csharp
[DateType(DateTimeSemantic.LocalDateTime)]  // Default - local date and time
[DateType(DateTimeSemantic.UtcInstant)]     // UTC timestamp from APIs
[DateType(DateTimeSemantic.DateOnly)]       // Date picker, birthdays
[DateType(DateTimeSemantic.TimeOnly)]       // Time picker, daily alarms
[DateType(DateTimeSemantic.Duration)]       // Elapsed time, session length
[DateType(DateTimeSemantic.ZonedDateTime)]  // Timezone-specific times
```

### Platform Availability
```csharp
[Available(true, false)]   // Android only
[Available(false, true)]   // iOS only
[Exclude("reason")]        // Skip generation completely
```

### Custom Naming
```csharp
[GenerateAs("customName")]              // Same name on both platforms
[GenerateAs("androidName", "iosName")]  // Different names per platform
```

## Supported Type Mappings

### Android Types
- **Primitives**: `String`, `Int`, `Long`, `Boolean`, `Double`, `Float`, `Byte`
- **DateTime**: `LocalDateTime`, `Instant`, `LocalDate`, `LocalTime`, `Duration`
- **Collections**: `ByteArray`, `IntArray`, `List`
- **Location**: `LatLng`, `Location`
- **Other**: `UUID`, `URI`

### iOS Types
- **Primitives**: `String`, `Int32`, `Int64`, `Bool`, `Double`, `Float`, `UInt8`
- **DateTime**: `Date`, `DateComponents`, `TimeInterval`
- **Collections**: `UInt8Array`, `Int32Array`, `Array`
- **Location**: `CLLocationCoordinate2D`, `CLLocation`
- **Other**: `UUID`, `URL`

## Common Patterns

### Geographic Coordinates
```csharp
[MapTo(AndroidType.LatLng, IOSType.CLLocationCoordinate2D)]
public GeographicCoordinate Location { get; set; }
```

### API Timestamps
```csharp
[DateType(DateTimeSemantic.UtcInstant)]
public DateTime LastSync { get; set; }
```

### User Date Selection
```csharp
[DateType(DateTimeSemantic.DateOnly)]
public DateTime EventDate { get; set; }
```

### Binary Data
```csharp
[MapTo(AndroidType.ByteArray, IOSType.UInt8Array)]
public byte[] ImageData { get; set; }
```

### Platform-Specific Features
```csharp
[Available(true, false)]  // Android only
public bool UseAndroidSpecificApi { get; set; }

[Available(false, true)]  // iOS only
public bool UseIOSSpecificFeature { get; set; }
```

## Package Philosophy

This package follows the **"Zero Configuration"** principle:

1. **Default**: Everything works without any attributes or package references
2. **Opt-in**: Add package reference and attributes only when you need customization
3. **Lightweight**: Just attributes and enums - no heavy dependencies
4. **Backward Compatible**: Adding/removing attributes doesn't break generation

## Related Tools

- **PhotographyAdapterGenerator**: The actual code generation tool (Referenced in your "module" project)

## Questions?

If your ViewModels use only standard .NET types (`string`, `int`, `bool`, `DateTime`, `List<T>`, etc.), you probably don't need this package at all. The adapter generator works great with defaults.

Only add this package when you hit specific customization needs like geographic coordinates, byte array signedness, or platform-specific features.