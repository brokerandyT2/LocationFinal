# Location Photography Infrastructure

A comprehensive data management and infrastructure library for location-based photography applications, with specialized support for astrophotography and weather-aware shooting.

## Overview

This library provides the foundational infrastructure for photography applications that need location-based features, equipment management, weather integration, and astronomical calculations. It follows Clean Architecture principles with clear separation of concerns and implements the Repository Pattern for data access.

## Architecture

```
┌─────────────────────┐
│   Application       │  ← Service interfaces, DTOs
├─────────────────────┤
│   Infrastructure    │  ← This Library
│   - Repositories    │
│   - Data Mapping    │
│   - Configuration   │
│   - Resources       │
├─────────────────────┤
│   Domain            │  ← Entities, Business Logic
├─────────────────────┤
│   Database          │  ← SQLite Data Store
└─────────────────────┘
```

## Core Features

### 🗄️ Data Management
- **Database Initialization**: Automated setup with sample data and user configurations
- **Equipment Cataloging**: 200+ camera profiles and lens compatibility database
- **Location Database**: Photography locations with coordinates and weather data
- **Configuration Management**: Centralized application settings and user preferences

### 🔍 Advanced Search & Filtering
- **Fuzzy Search**: Intelligent matching for camera and lens names
- **Compatibility Checking**: Lens-camera mount compatibility validation
- **Range Filtering**: Focal length and specification-based searches
- **User Favorites**: Personal equipment tracking and management

### 🌤️ Weather Integration
- **Real-time Data**: Weather conditions for photography planning
- **Forecast Processing**: 5-day predictions with validation and caching
- **Atmospheric Conditions**: Specialized data for astrophotography
- **Performance Caching**: 15-minute validation cache with automatic cleanup

### ⭐ Astrophotography Support
- **Celestial Object Database**: Coordinates and optimal shooting times
- **Meteor Shower Calendar**: 28 major showers with peak dates and rates
- **Astronomical Calculations**: Position calculations and coordinate conversions
- **Equipment Recommendations**: Specialized guidance for different targets

### 💳 Subscription Management
- **Enterprise-grade System**: Robust subscription handling with caching
- **Multiple Providers**: Support for various payment platforms
- **Bulk Operations**: Efficient processing of multiple subscriptions
- **Transaction Tracking**: Complete purchase and renewal history

## Quick Start

### Database Initialization

```csharp
// Basic initialization with sample data
await databaseInitializer.InitializeDatabaseWithStaticDataAsync(cancellationToken);

// Complete setup with user preferences
await databaseInitializer.InitializeDatabaseAsync(
    cancellationToken,
    hemisphere: "north",
    tempFormat: "F",
    dateFormat: "MMM/dd/yyyy",
    timeFormat: "hh:mm tt",
    windDirection: "towardsWind",
    email: "user@example.com",
    guid: "unique-user-id"
);
```

### Equipment Management

```csharp
// Search for camera equipment
var cameraResult = await cameraBodyRepository.SearchByNameAsync("Canon 5D", cancellationToken);

// Find compatible lenses
var compatibleLenses = await lensRepository.GetCompatibleLensesAsync(cameraBodyId, cancellationToken);

// Manage user favorites
var userEquipment = await userCameraBodyRepository.GetFavoritesByUserIdAsync(userId, cancellationToken);
```

### Weather Data Processing

```csharp
// Process weather data for UI
var weatherDto = await mapper.MapToWeatherDtoAsync(weatherEntity, forecastEntities);

// Batch processing for performance
var weatherDtos = await mapper.MapToWeatherDtoBatchAsync(weatherEntities, forecastsByLocationId);

// Validate data quality for predictions
var isValid = await mapper.ValidateForPredictionsAsync(weatherEntity, forecasts);
```

## Key Components

### DatabaseInitializer
**Thread-safe database setup and configuration**
- Parallel data creation for improved performance
- Build-specific configurations (Debug vs Release)
- Comprehensive error handling and logging
- Sample data: tip types, locations, camera profiles, settings

### Repository Layer
**Specialized data access for different domains**

| Repository | Purpose |
|------------|---------|
| `CameraBodyRepository` | Camera equipment with fuzzy search |
| `LensRepository` | Lens management and compatibility |
| `UserCameraBodyRepository` | Personal equipment tracking |
| `SubscriptionRepository` | Enterprise subscription handling |
| `PhoneCameraProfileRepository` | Mobile device camera profiles |

### WeatherEntityToDtoMapper
**Advanced weather data processing**
- Asynchronous mapping to prevent UI blocking
- Validation caching with configurable expiry
- Batch processing for multiple entities
- Memory management with automatic cleanup

### Resource Management
**Comprehensive localization and guidance**
- 200+ localized strings in `AppResources.resx`
- Specialized astrophotography guidance
- Error messages for all operations
- Equipment recommendations by target type

## Configuration

### Build-Specific Settings

**Debug Mode**
- All features marked as "viewed" for testing
- Premium subscription with 1-year validity
- Extended ad grace periods (24 hours)

**Release Mode**
- Clean user experience
- Free subscription tier
- Standard grace periods (12 hours)

### User Preferences
- Hemisphere selection (North/South)
- Temperature units (Fahrenheit/Celsius)
- Date/time formatting preferences
- Wind direction display options
- Personalization settings

## Performance Features

### Caching Strategy
- **Repository-level caching** for frequently accessed data
- **Validation caching** with 15-minute TTL
- **Concurrent operations** using thread-safe collections
- **Memory management** with automatic cleanup

### Database Optimization
- **Batch processing** with configurable sizes
- **Parallel task execution** for independent operations
- **Connection pooling** through UnitOfWork pattern
- **Indexed searches** on commonly queried fields

### Background Processing
- **Asynchronous operations** throughout
- **CancellationToken support** for all operations
- **Background threading** for complex calculations
- **Graceful degradation** strategies

## Error Handling

### Result Pattern Implementation
```csharp
public class Result<T>
{
    public bool IsSuccess { get; }
    public T Data { get; }
    public string ErrorMessage { get; }
}
```

**Benefits:**
- Explicit error handling without exceptions
- Consistent error messaging using localized resources
- Graceful degradation when operations fail
- Easy unit testing and assertion

### Comprehensive Logging
- Structured log messages with context
- Performance metrics for slow operations
- Error correlation with operation identifiers
- Debug-friendly error descriptions

## Specialized Data

### Astronomical Database
**Complete meteor shower calendar** (`meteor_showers.json`)
- 28 major meteor showers with detailed information
- Peak dates, rates, and visibility periods
- Radiant coordinates and parent body data
- Zenith Hourly Rates (ZHR) for planning

### Equipment Database
**Extensive camera and lens catalog**
- 200+ camera sensor specifications (2010-2024)
- Mount type compatibility matrices
- Lens focal length and aperture databases
- Mobile device camera calibration data

## Dependencies

### Required Packages
```xml
<PackageReference Include="Microsoft.Extensions.Logging" />
<PackageReference Include="Microsoft.Maui.Storage" />
<PackageReference Include="AutoMapper" />
<PackageReference Include="Microsoft.EntityFrameworkCore" />
```

### Internal Dependencies
- `Location.Core.Application` - Service interfaces
- `Location.Core.Domain` - Entity models
- `Location.Core.Infrastructure` - Data implementations

## Testing

### Unit Testing Support
- Mock `IUnitOfWork` for database operations
- `CancellationToken` timeout testing
- Build configuration testing
- Result pattern assertion helpers

### Integration Testing
- Complete initialization flow validation
- Weather data mapping accuracy
- Concurrent operation safety
- Performance under load testing

## Monitoring & Diagnostics

### Key Performance Metrics
- Database initialization duration
- Weather validation cache hit rates
- Failed operation frequencies
- Memory usage during batch operations

### Health Checks
- Database connectivity status
- Cache performance metrics
- Weather API response times
- Subscription service availability

## Contributing

### Code Quality Standards
- Follow async/await patterns consistently
- Use `ConfigureAwait(false)` for library code
- Implement proper resource disposal
- Maintain comprehensive error handling
- Document public APIs with XML comments

### Development Guidelines
- All public methods must return `Result<T>`
- Include CancellationToken parameters
- Use structured logging with context
- Write unit tests for new features
- Update localization resources

## License

[Your License Here]

## Support

For issues, feature requests, or contributions, please visit our repository or contact the development team.