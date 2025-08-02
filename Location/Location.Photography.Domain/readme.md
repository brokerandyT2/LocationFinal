# Location.Photography.Domain

## Overview

The Location.Photography.Domain is a comprehensive .NET 9.0 domain layer designed for photography and astronomical applications. This library provides robust models, entities, and services for managing camera equipment, astronomical calculations, and photography planning with precise sun/moon positioning.

## Architecture

This is a domain-driven design (DDD) implementation that separates core business logic from infrastructure concerns. The domain layer is framework-agnostic and focuses on pure business rules and entities.

### Key Components

- **Entities**: Core business objects with identity and lifecycle management
- **Models**: Data transfer objects and complex data structures
- **Services**: Domain service interfaces for astronomical calculations
- **Enums**: Type-safe constants for camera mounts and other classifications

## Core Features

### 📷 Camera Equipment Management
- Camera body specifications with sensor dimensions and mount types
- Lens management with focal length ranges and aperture settings
- Compatibility tracking between lenses and camera bodies
- User-specific equipment collections with favorites and notes
- Phone camera profiling for mobile photography

### 🌟 Astronomical Calculations
- Precise sun/moon position calculations
- Solar and lunar eclipse predictions
- Golden hour, blue hour, and twilight times
- Meteor shower tracking and visibility predictions
- Deep sky object positioning (galaxies, nebulae, star clusters)
- Planetary positions and conjunction predictions
- International Space Station (ISS) pass predictions

### 📱 Subscription Management
- In-app purchase tracking
- Subscription status monitoring with expiration alerts
- Support for monthly and yearly billing cycles

## Entity Descriptions

### CameraBody
Represents physical camera bodies with sensor specifications:
```csharp
// Example usage
var camera = new CameraBody(
    name: "Canon EOS R5",
    sensorType: "Full Frame",
    sensorWidth: 36.0,
    sensorHeight: 24.0,
    mountType: MountType.CanonRF
);
```

**Key Properties:**
- `Name`: Camera model name
- `SensorType`: Sensor classification (Full Frame, APS-C, etc.)
- `SensorWidth/Height`: Physical sensor dimensions in mm
- `MountType`: Lens mount compatibility
- `IsUserCreated`: Tracks custom vs. pre-defined cameras

### Lens
Manages lens specifications with focal length and aperture ranges:
```csharp
// Prime lens example
var primeLens = new Lens(50.0, null, 1.4, 1.4);

// Zoom lens example  
var zoomLens = new Lens(24.0, 70.0, 2.8, 2.8);
```

**Key Properties:**
- `MinMM/MaxMM`: Focal length range
- `MinFStop/MaxFStop`: Aperture range
- `IsPrime`: Automatically calculated based on focal length range
- `NameForLens`: Optional custom naming

### MeteorShower
Comprehensive meteor shower modeling with activity periods:
```csharp
var shower = meteorShower.GetRadiantPosition(
    DateTime.Now, 
    latitude: 40.7128, 
    longitude: -74.0060
);
```

**Features:**
- Activity period tracking (start, peak, finish dates)
- Zenith Hourly Rate (ZHR) calculations
- Radiant position calculations
- Year-boundary crossing support (e.g., Quadrantids)

### Subscription
Manages in-app purchase subscriptions:
```csharp
var subscription = new Subscription(
    productId: "premium_monthly",
    transactionId: "txn_123",
    purchaseToken: "token_abc",
    purchaseDate: DateTime.UtcNow,
    expirationDate: DateTime.UtcNow.AddMonths(1),
    status: SubscriptionStatus.Active,
    period: SubscriptionPeriod.Monthly,
    userId: "user_456"
);
```

## Astronomical Models

### Sun Position & Timing
The `EnhancedSunDomainModels` provide comprehensive solar calculations:

- **Sun Times**: Sunrise, sunset, solar noon, twilight periods
- **Golden/Blue Hours**: Optimal photography timing
- **Shadow Calculations**: Length and direction predictions
- **Light Quality Predictions**: EV values and exposure recommendations

### Moon Data
Detailed lunar information including:
- Phase calculations with illumination percentages
- Rise/set times with position data
- Libration data for lunar photography
- Supermoon event tracking
- Eclipse predictions

### Deep Sky Objects
Support for astrophotography targets:
- Catalog objects (Messier, NGC)
- Constellation tracking
- Milky Way core visibility
- Planet positions and conjunctions
- ISS pass predictions

## Service Interfaces

### ISunCalculatorService
Comprehensive astronomical calculation service supporting:

```csharp
public interface ISunCalculatorService
{
    DateTime GetSunrise(DateTime date, double latitude, double longitude, string timezone);
    DateTime GetSunset(DateTime date, double latitude, double longitude, string timezone);
    double GetSolarAzimuth(DateTime dateTime, double latitude, double longitude, string timezone);
    double GetSolarElevation(DateTime dateTime, double latitude, double longitude, string timezone);
    
    // Lunar methods
    DateTime? GetMoonrise(DateTime date, double latitude, double longitude, string timezone);
    double GetMoonIllumination(DateTime dateTime, double latitude, double longitude, string timezone);
    
    // Performance optimizations
    Task<Dictionary<string, object>> GetBatchAstronomicalDataAsync(...);
    Task PreloadAstronomicalCalculationsAsync(...);
}
```

**Performance Features:**
- Batch calculation support for multiple data points
- Caching with cleanup mechanisms
- Async preloading for bulk operations

## Dependencies

```xml
<PackageReference Include="akavache.core" Version="10.2.41" />
<PackageReference Include="CommunityToolkit.Maui" Version="11.2.0" />
<PackageReference Include="CommunityToolkit.Mvvm" Version="8.4.0" />
<PackageReference Include="CosineKitty.AstronomyEngine" Version="2.1.19" />
<PackageReference Include="MediatR" Version="12.5.0" />
<PackageReference Include="Plugin.InAppBilling" Version="8.0.5" />
<PackageReference Include="SkiaSharp" Version="3.119.0" />
<PackageReference Include="sqlite-net-pcl" Version="1.9.172" />
```

## Database Integration

Entities are configured for SQLite with the following attributes:
- `[Table("TableName")]`: Defines table mapping
- `[PrimaryKey, AutoIncrement]`: Primary key configuration
- `[MaxLength(n), NotNull]`: Field constraints
- `[Indexed]`: Performance optimization
- `[ExportToSQL]`: Custom code generation attribute

## Validation & Business Rules

### Camera Equipment
- Camera names cannot be null/empty and are trimmed
- Sensor dimensions must be positive values
- Lens focal lengths must be positive with max > min
- F-stop values must be positive with proper range validation

### Astronomical Data
- Date ranges are validated for meteor shower activity periods
- Year boundary crossings are handled correctly
- Coordinate transformations maintain precision
- Time zone conversions are properly managed

### Subscriptions
- All subscription properties are validated on creation
- Status transitions are tracked with timestamps
- Expiration monitoring with configurable thresholds

## Usage Examples

### Equipment Management
```csharp
// Create camera and lens combination
var camera = new CameraBody("Sony A7R V", "Full Frame", 35.7, 23.8, MountType.SonyFE);
var lens = new Lens(24.0, 105.0, 4.0, 4.0, false, "Sony FE 24-105mm f/4");

// Create compatibility relationship
var compatibility = new LensCameraCompatibility(lens.Id, camera.Id);

// User saves equipment
var userCamera = new UserCameraBody(camera.Id, "user123", true, "My main camera");
```

### Astronomical Planning
```csharp
// Check meteor shower activity
var shower = meteorShowerData.GetShowerByCode("PER"); // Perseids
var isActive = shower.IsActiveOn(DateTime.Today);
var expectedZHR = shower.GetExpectedZHR(DateTime.Today);

// Get radiant position
var position = shower.GetRadiantPosition(DateTime.Now, 40.7128, -74.0060);
Console.WriteLine($"Radiant at {position.Altitude:F1}° altitude, {position.DirectionDescription}");
```

### Photography Planning
```csharp
// Calculate optimal shooting times
var sunTimes = sunCalculator.GetBatchAstronomicalDataAsync(
    DateTime.Today, 40.7128, -74.0060, "America/New_York",
    "Sunrise", "GoldenHour", "BlueHour", "Sunset"
);

// Get exposure recommendations based on conditions
var recommendation = new AstroExposureRecommendation
{
    Target = AstroTarget.MilkyWayCore,
    RecommendedISO = "ISO 3200",
    RecommendedAperture = "f/2.8",
    RecommendedShutterSpeed = "25s"
};
```

## Testing Considerations

### Unit Testing Focus Areas
1. **Entity Validation**: Test all constructor and update method validations
2. **Business Logic**: Verify complex calculations like ZHR and radiant positions
3. **Date Handling**: Test year boundary crossings and time zone conversions
4. **Edge Cases**: Null values, extreme coordinates, invalid dates

### Integration Testing
1. **Database Mapping**: Verify SQLite schema generation and data persistence
2. **Service Contracts**: Test astronomical calculation accuracy
3. **Performance**: Validate batch operations and caching mechanisms

### Data Validation Testing
```csharp
[Test]
public void CameraBody_InvalidSensorWidth_ThrowsException()
{
    Assert.Throws<ArgumentException>(() => 
        new CameraBody("Test", "Full Frame", -1.0, 24.0, MountType.CanonEF));
}

[Test]
public void MeteorShower_YearBoundaryCrossing_HandledCorrectly()
{
    // Test Quadrantids (Dec 28 - Jan 12)
    var shower = new MeteorShower { /* setup */ };
    Assert.True(shower.IsActiveOn(new DateTime(2024, 1, 5)));
    Assert.True(shower.IsActiveOn(new DateTime(2023, 12, 30)));
}
```

## Performance Considerations

### Caching Strategy
- Astronomical calculations are computationally expensive
- Implement caching for frequently accessed sun/moon data
- Use `PreloadAstronomicalCalculationsAsync` for bulk operations
- Regular cache cleanup to prevent memory leaks

### Database Optimization
- Indexed foreign keys for quick lookups
- Separate user data from reference data
- Consider read-only reference data caching

### Memory Management
- Dispose of large calculation results appropriately
- Monitor memory usage during batch astronomical calculations
- Use streaming for large dataset operations

## Security Considerations

### Data Validation
- All user inputs are validated and sanitized
- SQL injection protection through parameterized queries
- Range validation for coordinates and dates

### Subscription Security
- Purchase tokens and transaction IDs should be validated server-side
- Subscription status should be verified with platform stores
- Implement proper error handling for payment failures

## Future Extensibility

The domain design supports extension through:
- Additional mount types via enum extension
- New astronomical targets through the extensible enum system
- Custom camera profiles for specialized equipment
- Enhanced subscription tiers and features

## Troubleshooting

### Common Issues
1. **Time Zone Handling**: Ensure consistent UTC usage in calculations
2. **Date Boundary Issues**: Test meteor shower calculations around year boundaries
3. **Floating Point Precision**: Use appropriate precision for astronomical calculations
4. **Null Reference**: Handle optional properties in astronomical data properly

### Debugging Tips
- Enable detailed logging for astronomical calculations
- Validate input coordinates and dates before processing
- Check time zone conversion results
- Monitor subscription status transitions