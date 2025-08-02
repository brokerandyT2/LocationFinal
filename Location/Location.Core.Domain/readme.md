# Location.Core.Domain

A comprehensive Domain-Driven Design (DDD) implementation for location-based applications with weather integration and photography tips functionality. Built with .NET 9, this domain layer provides a robust foundation for location tracking, weather forecasting, and photography guidance systems.

## 🏗️ Architecture Overview

This project follows Domain-Driven Design principles with a clean architecture approach:

- **Entities**: Core business objects with identity and lifecycle
- **Value Objects**: Immutable objects representing domain concepts
- **Aggregate Roots**: Entity clusters with transaction boundaries
- **Domain Events**: Capture significant business occurrences
- **Business Rules**: Encapsulated domain validation logic
- **Exceptions**: Domain-specific error handling

## 📦 Project Structure

```
Location.Core.Domain/
├── Common/                     # Base classes and shared components
│   ├── AggregateRoot.cs       # Base aggregate root with domain events
│   ├── DomainEvent.cs         # Base domain event implementation
│   └── Entity.cs              # Base entity with identity and equality
├── Entities/                   # Domain entities
│   ├── Location.cs            # Location aggregate root
│   ├── Weather.cs             # Weather aggregate root
│   ├── WeatherForecast.cs     # Daily weather forecast
│   ├── HourlyForecast.cs      # Hourly weather forecast
│   ├── Setting.cs             # User configuration settings
│   ├── Tip.cs                 # Photography tips
│   └── TipType.cs             # Photography tip categories
├── ValueObjects/               # Immutable value objects
│   ├── Coordinate.cs          # Geographic coordinates (performance optimized)
│   ├── Address.cs             # Physical address representation
│   ├── Temperature.cs         # Temperature with unit conversions
│   ├── WindInfo.cs            # Wind speed, direction, and gusts
│   └── ValueObject.cs         # Base value object implementation
├── Events/                     # Domain events
│   ├── LocationSavedEvent.cs
│   ├── LocationDeletedEvent.cs
│   ├── PhotoAttachedEvent.cs
│   └── WeatherUpdatedEvent.cs
├── Exceptions/                 # Domain-specific exceptions
│   ├── LocationDomainException.cs
│   ├── InvalidCoordinateException.cs
│   ├── WeatherDomainException.cs
│   ├── SettingDomainException.cs
│   ├── TipDomainException.cs
│   └── TipTypeDomainException.cs
├── Rules/                      # Business validation rules
│   ├── CoordinateValidationRules.cs
│   ├── LocationValidationRules.cs
│   └── WeatherValidationRules.cs
└── Interfaces/                 # Domain contracts
    ├── IAggregateRoot.cs
    ├── IDomainEvent.cs
    └── IEntity.cs
```

## 🎯 Core Features

### Location Management
- **Location Entity**: Stores location information with title, description, coordinates, and address
- **Photo Attachment**: Associate photos with locations
- **Soft Delete**: Mark locations as deleted without permanent removal
- **Domain Events**: Automatic event publishing for location changes

### Weather Integration
- **Current Weather**: Real-time weather data for locations
- **7-Day Forecasts**: Daily weather predictions with detailed information
- **48-Hour Forecasts**: Hourly weather data for short-term planning
- **Moon Phases**: Lunar cycle information for photography planning
- **Performance Caching**: Optimized weather data retrieval

### Photography Tips System
- **Categorized Tips**: Organize photography advice by type
- **Camera Settings**: Store F-stop, shutter speed, and ISO recommendations
- **Localization**: Multi-language support for international users
- **Content Management**: Update and maintain tip collections

### Geographic Calculations
- **Distance Calculations**: Haversine formula for accurate distance measurement
- **Proximity Search**: Find locations within specified radius
- **Bearing Calculation**: Determine direction between coordinates
- **Spatial Optimization**: Performance-tuned geographic operations

## 🚀 Key Components

### Entities

#### Location (Aggregate Root)
```csharp
public class Location : AggregateRoot
{
    public string Title { get; private set; }
    public string Description { get; private set; }
    public Coordinate Coordinate { get; private set; }
    public Address Address { get; private set; }
    public string? PhotoPath { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime Timestamp { get; private set; }
}
```

**Key Features:**
- Immutable properties with validation
- Domain event publishing on changes
- Photo attachment management
- Soft delete functionality

#### Weather (Aggregate Root)
```csharp
public class Weather : AggregateRoot
{
    public int LocationId { get; private set; }
    public Coordinate Coordinate { get; private set; }
    public DateTime LastUpdate { get; private set; }
    public IReadOnlyCollection<WeatherForecast> Forecasts { get; }
    public IReadOnlyCollection<HourlyForecast> HourlyForecasts { get; }
}
```

**Key Features:**
- 7-day daily forecasts (limited collection)
- 48-hour hourly forecasts (limited collection)
- Timezone information
- Automatic update timestamp management

### Value Objects

#### Coordinate (Performance Optimized)
```csharp
public class Coordinate : ValueObject
{
    public double Latitude { get; }
    public double Longitude { get; }
    
    // Performance features
    public double DistanceTo(Coordinate other)
    public bool IsWithinDistance(Coordinate other, double maxDistanceKm)
    public Coordinate FindNearest(IReadOnlyList<Coordinate> candidates)
}
```

**Performance Optimizations:**
- ✅ **Distance Caching**: Frequently calculated distances cached in memory
- ✅ **String Caching**: ToString() results cached to avoid repeated formatting
- ✅ **Validation Caching**: Coordinate validation results cached
- ✅ **Batch Operations**: Optimized bulk distance calculations
- ✅ **Spatial Filtering**: Bounding box pre-filtering for large datasets
- ✅ **Early Exit**: Performance shortcuts for very close matches

#### Temperature
```csharp
public class Temperature : ValueObject
{
    public double Celsius { get; }
    public double Fahrenheit { get; }
    public double Kelvin { get; }
    
    public static Temperature FromCelsius(double celsius)
    public static Temperature FromFahrenheit(double fahrenheit)
    public static Temperature FromKelvin(double kelvin)
}
```

### Domain Events

All domain events inherit from `DomainEvent` and are automatically timestamped:

- **LocationSavedEvent**: Triggered when location is created or updated
- **LocationDeletedEvent**: Triggered when location is soft deleted
- **PhotoAttachedEvent**: Triggered when photo is attached to location
- **WeatherUpdatedEvent**: Triggered when weather data is refreshed

### Business Rules

#### Coordinate Validation
```csharp
public static class CoordinateValidationRules
{
    public static bool IsValid(double latitude, double longitude, out List<string> errors)
    public static bool IsValidDistance(Coordinate from, Coordinate to, double maxDistanceKm)
}
```

#### Weather Validation
```csharp
public static class WeatherValidationRules
{
    public static bool IsValid(Weather weather, out List<string> errors)
    public static bool IsStale(Weather weather, TimeSpan maxAge)
}
```

## 🔧 Dependencies

```xml
<PackageReference Include="MediatR" Version="12.5.0" />
<PackageReference Include="CommunityToolkit.Mvvm" Version="8.4.0" />
<PackageReference Include="CommunityToolkit.Maui" Version="11.2.0" />
<PackageReference Include="akavache.core" Version="10.2.41" />
<PackageReference Include="Plugin.InAppBilling" Version="8.0.5" />
<PackageReference Include="SkiaSharp" Version="3.119.0" />
```

## 💡 Usage Examples

### Creating a Location
```csharp
var coordinate = new Coordinate(40.7128, -74.0060); // New York City
var address = new Address("New York", "NY");
var location = new Location("Central Park", "Beautiful urban park", coordinate, address);

// Domain event automatically added: LocationSavedEvent
```

### Distance Calculations
```csharp
var coord1 = new Coordinate(40.7128, -74.0060); // NYC
var coord2 = new Coordinate(34.0522, -118.2437); // LA

var distance = coord1.DistanceTo(coord2); // ~3944 km
var isNearby = coord1.IsWithinDistance(coord2, 100); // false
```

### Weather Management
```csharp
var weather = new Weather(locationId: 1, coordinate, "America/New_York", -5);

var forecasts = GetForecastsFromAPI();
weather.UpdateForecasts(forecasts); // Automatically limits to 7 days

var currentForecast = weather.GetCurrentForecast();
var tomorrowForecasts = weather.GetHourlyForecastsForDate(DateTime.Today.AddDays(1));
```

### Photography Tips
```csharp
var landscapeType = new TipType("Landscape Photography");
var tip = new Tip(landscapeType.Id, "Golden Hour Shots", "Shoot during the golden hour for warm lighting");
tip.UpdatePhotographySettings("f/8", "1/125", "ISO 100");

landscapeType.AddTip(tip);
```

## 🧪 Testing Considerations

### Unit Testing Focus Areas
1. **Entity Invariants**: Ensure entities maintain valid state
2. **Value Object Immutability**: Verify value objects cannot be modified
3. **Business Rules**: Test validation logic thoroughly
4. **Domain Events**: Verify events are raised correctly
5. **Performance**: Test coordinate calculations with large datasets

### Test Data Builders
```csharp
public class LocationBuilder
{
    public LocationBuilder WithTitle(string title) { /* ... */ }
    public LocationBuilder WithCoordinate(double lat, double lon) { /* ... */ }
    public Location Build() { /* ... */ }
}
```

## 🔒 Security Considerations

- **Input Validation**: All public methods validate inputs
- **Encapsulation**: Private setters prevent unauthorized modifications
- **Exception Handling**: Domain-specific exceptions with error codes
- **Coordinate Bounds**: Strict latitude/longitude validation

## ⚡ Performance Features

### Coordinate Optimizations
- **Memory Caching**: Three-tier caching system for distances, strings, and validation
- **Spatial Indexing**: Bounding box pre-filtering for proximity searches
- **Batch Processing**: Optimized bulk operations for multiple coordinates
- **Cache Management**: Automatic cache size limits and cleanup methods

### Memory Management
```csharp
// Cache statistics and cleanup
var (distanceCache, stringCache, validationCache) = Coordinate.GetCacheStats();
Coordinate.ClearCaches(); // Manual cleanup when needed
```

## 🚀 Getting Started

1. **Clone the repository**
2. **Restore dependencies**: `dotnet restore`
3. **Build the project**: `dotnet build`
4. **Run tests**: `dotnet test`

## 📝 API Compatibility

This domain layer is designed to integrate with:
- **Web APIs**: RESTful services for location and weather data
- **Mobile Apps**: Cross-platform applications using .NET MAUI
- **Desktop Applications**: WPF/WinUI applications
- **Background Services**: Weather data synchronization services

## 🎯 Future Enhancements

- **Geocoding Integration**: Address to coordinate conversion
- **Weather Alerts**: Severe weather notification system
- **Photo Metadata**: EXIF data extraction and analysis
- **Route Planning**: Multi-location journey optimization
- **Offline Support**: Local caching for disconnected scenarios

## 📄 License

[License information would go here]

## 🤝 Contributing

[Contributing guidelines would go here]

---

**Target Framework**: .NET 9.0  
**Architecture**: Domain-Driven Design  
**Performance**: Production-optimized with extensive caching  
**Testability**: Designed for comprehensive unit testing