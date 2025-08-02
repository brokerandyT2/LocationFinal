# Location.Core.Application

A comprehensive .NET 9 application layer implementing Clean Architecture principles for location-based photography applications. This library provides the core business logic, command/query patterns, validation, and cross-cutting concerns for managing locations, weather data, photography tips, and system settings.

## 🏗️ Architecture Overview

This application layer follows **Clean Architecture** and **CQRS** patterns:

- **Commands**: Handle write operations (Create, Update, Delete)
- **Queries**: Handle read operations with optimized projections
- **Behaviors**: Cross-cutting concerns (Validation, Logging)
- **Events**: Domain event handling for decoupled communication
- **Services**: External integrations (Weather, Geolocation, Media)

## 📦 Core Components

### 🔧 Commands & Queries (CQRS)

#### Location Management
```csharp
// Save a location
var command = new SaveLocationCommand 
{
    Title = "Golden Gate Bridge",
    Latitude = 37.8199,
    Longitude = -122.4783,
    City = "San Francisco",
    State = "California"
};
var result = await mediator.Send(command);

// Get locations with pagination
var query = new GetLocationsQuery 
{
    PageNumber = 1,
    PageSize = 10,
    SearchTerm = "bridge"
};
var locations = await mediator.Send(query);
```

#### Weather Integration
```csharp
// Update weather for a location
var weatherCommand = new UpdateWeatherCommand 
{
    LocationId = 1,
    ForceUpdate = true
};
var weather = await mediator.Send(weatherCommand);

// Get weather forecast
var forecastQuery = new GetWeatherForecastQuery 
{
    Latitude = 37.8199,
    Longitude = -122.4783,
    Days = 7
};
var forecast = await mediator.Send(forecastQuery);
```

### 🎯 Pipeline Behaviors

#### Validation Behavior
Automatic validation using FluentValidation with optimized fail-fast processing:

```csharp
public class SaveLocationCommandValidator : AbstractValidator<SaveLocationCommand>
{
    public SaveLocationCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(100);
            
        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90, 90);
            
        // Null Island validation (0,0 coordinates)
        RuleFor(x => x)
            .Must(x => !(x.Latitude == 0 && x.Longitude == 0))
            .WithMessage("Invalid coordinates: Cannot use Null Island (0,0)");
    }
}
```

#### Logging Behavior
High-performance logging with structured data and performance monitoring:

```csharp
// Automatic request/response logging
// Slow operation detection (>500ms)
// Error event publishing
// Optimized serialization with size limits
```

### 📡 Event System

#### Domain Events
Type-safe error handling and event publishing:

```csharp
// Location save error
public class LocationSaveErrorEvent : DomainErrorEvent
{
    public string LocationTitle { get; }
    public LocationErrorType ErrorType { get; }
    
    // Automatic localization support
    public override string GetResourceKey() => ErrorType switch
    {
        LocationErrorType.DuplicateTitle => "Location_Error_DuplicateTitle",
        LocationErrorType.InvalidCoordinates => "Location_Error_InvalidCoordinates",
        _ => "Location_Error_Unknown"
    };
}
```

#### Error Display Service
Real-time error aggregation with background processing:

```csharp
public class ErrorDisplayService : IErrorDisplayService
{
    // Lock-free error collection
    // Batch processing with 500ms aggregation window
    // Intelligent error grouping
    // Fire-and-forget event publishing
}
```

### 🗂️ Repository Patterns

#### SQLite-Optimized Repositories
High-performance data access with raw SQL projections:

```csharp
// Specification pattern for complex queries
var spec = new LocationSpecifications.NearbyLocationsSpec(
    latitude: 37.8199, 
    longitude: -122.4783, 
    distanceKm: 10.0
);
var nearbyLocations = await repository.GetBySpecificationAsync(spec);

// Bulk operations
var locations = new List<Location> { /* ... */ };
var result = await repository.CreateBulkAsync(locations);

// Projected queries for performance
var lightweightData = await repository.GetActiveProjectedAsync<LocationListDto>(
    selectColumns: "Id, Title, City, State, PhotoPath",
    additionalWhere: "PhotoPath IS NOT NULL"
);
```

### 🌍 External Service Integrations

#### Geolocation Service
```csharp
public interface IGeolocationService
{
    Task<Result<GeolocationDto>> GetCurrentLocationAsync();
    Task<Result<bool>> RequestPermissionsAsync();
    Task<Result<bool>> StartTrackingAsync(GeolocationAccuracy accuracy);
}
```

#### Weather Service
```csharp
public interface IWeatherService
{
    Task<Result<WeatherDto>> GetWeatherAsync(double lat, double lng);
    Task<Result<WeatherForecastDto>> GetForecastAsync(double lat, double lng, int days);
    Task<Result<int>> UpdateAllWeatherAsync();
}
```

#### Media Service
```csharp
public interface IMediaService
{
    Task<Result<string>> CapturePhotoAsync();
    Task<Result<string>> PickPhotoAsync();
    Task<Result<bool>> IsCaptureSupported();
    Task<Result<bool>> DeletePhotoAsync(string filePath);
}
```

## 📋 Feature Modules

### 📍 Locations
- **CRUD Operations**: Create, read, update, delete locations
- **Photo Management**: Attach/remove photos from locations
- **Soft Delete**: Restore deleted locations
- **Geospatial Queries**: Find nearby locations
- **Search & Pagination**: Efficient data retrieval

### 🌤️ Weather
- **Real-time Data**: Fetch current weather conditions
- **Forecasting**: 7-day weather forecasts
- **Hourly Data**: Detailed hourly forecasts
- **Offline-First**: Local caching with smart updates
- **Batch Updates**: Update weather for all locations

### 💡 Photography Tips
- **Categorized Tips**: Organize by tip types
- **Technical Details**: F-stop, shutter speed, ISO settings
- **Localization**: Multi-language support
- **Random Selection**: Get random tips by category

### ⚙️ Settings
- **Key-Value Store**: Flexible application settings
- **Validation**: Type-safe setting values
- **Read-Only Support**: Protect system settings

## 🔄 Result Pattern

All operations use a consistent `Result<T>` pattern for error handling:

```csharp
public class Result<T> : IResult<T>
{
    public bool IsSuccess { get; }
    public T? Data { get; }
    public string? ErrorMessage { get; }
    public IEnumerable<Error> Errors { get; }
    
    public static Result<T> Success(T data);
    public static Result<T> Failure(string errorMessage);
    public static Result<T> Failure(IEnumerable<Error> errors);
}

// Usage
var result = await mediator.Send(command);
if (result.IsSuccess)
{
    var data = result.Data;
    // Handle success
}
else
{
    var errors = result.Errors;
    // Handle errors
}
```

## 🌐 Localization

Comprehensive localization support using resource files:

```csharp
// AppResources.resx contains all user-facing strings
public static class AppResources 
{
    public static string Location_Error_NotFound => "Location not found";
    public static string Location_ValidationError_TitleRequired => "Title is required";
    // 500+ localized strings
}

// Automatic error message localization
var error = string.Format(AppResources.Location_Error_DuplicateTitle, locationTitle);
```

## 🚀 Performance Optimizations

### Database Access
- **Projection Queries**: Select only needed columns
- **Bulk Operations**: Batch inserts/updates
- **Specification Pattern**: Reusable query logic
- **Connection Pooling**: Efficient resource usage

### Memory Management
- **Object Pooling**: Reuse expensive objects
- **Lazy Loading**: Load data on demand
- **Weak References**: Prevent memory leaks
- **Disposal Patterns**: Proper resource cleanup

### Caching Strategy
- **In-Memory Caching**: Fast data access
- **Cache Invalidation**: Smart cache updates
- **TTL Support**: Time-based expiration
- **Size Limits**: Memory-bounded caches

## 🧪 Testing Strategy

### Unit Tests
```csharp
[Test]
public async Task SaveLocationCommand_WithValidData_ReturnsSuccess()
{
    // Arrange
    var command = new SaveLocationCommand { /* valid data */ };
    
    // Act
    var result = await handler.Handle(command, CancellationToken.None);
    
    // Assert
    Assert.That(result.IsSuccess, Is.True);
    Assert.That(result.Data.Title, Is.EqualTo(command.Title));
}
```

### Integration Tests
```csharp
[Test]
public async Task LocationRepository_GetNearby_ReturnsCorrectResults()
{
    // Test with real database
    var nearby = await repository.GetNearbyAsync(37.8199, -122.4783, 10.0);
    Assert.That(nearby.IsSuccess, Is.True);
}
```

## 📊 Monitoring & Observability

### Structured Logging
```csharp
_logger.LogInformation(
    "Request completed {RequestGuid} {RequestName} in {ElapsedMs}ms",
    requestGuid,
    requestName,
    stopwatch.ElapsedMilliseconds
);
```

### Performance Metrics
- Request/response times
- Error rates by operation
- Cache hit ratios
- Database query performance

### Error Tracking
- Automatic error categorization
- Error aggregation and deduplication
- Performance impact analysis

## 🔧 Configuration

### Dependency Injection
```csharp
public static IServiceCollection AddApplication(this IServiceCollection services)
{
    // AutoMapper
    services.AddSingleton<IMapper>(provider => {
        var config = new MapperConfiguration(cfg => cfg.AddMaps(assembly));
        return config.CreateMapper();
    });

    // MediatR with behaviors
    services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
    services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
    services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

    // Validators
    services.AddValidatorsFromAssembly(assembly);

    return services;
}
```

## 📁 Project Structure

```
Location.Core.Application/
├── Alerts/                     # Alert system
├── Commands/                   # Write operations
│   ├── Locations/             # Location commands
│   ├── TipTypes/              # Tip type commands
│   ├── Tips/                  # Photography tips
│   └── Weather/               # Weather updates
├── Common/                     # Shared components
│   ├── Behaviors/             # Pipeline behaviors
│   ├── Interfaces/            # Contracts
│   └── Models/                # Shared models
├── Events/                     # Domain events
│   └── Errors/                # Error events
├── Mappings/                   # AutoMapper profiles
├── Queries/                    # Read operations
├── Resources/                  # Localization
└── Services/                   # External integrations
```

## 🔗 Dependencies

### Core Framework
- **.NET 9.0**: Latest framework features
- **MediatR 12.5.0**: CQRS pattern implementation
- **AutoMapper 14.0.0**: Object-to-object mapping
- **FluentValidation 12.0.0**: Validation library

### Platform Integration
- **CommunityToolkit.Maui 11.2.0**: MAUI helpers
- **CommunityToolkit.Mvvm 8.4.0**: MVVM patterns

### Specialized Libraries
- **SkiaSharp 3.119.0**: Image processing
- **Plugin.InAppBilling 8.0.5**: In-app purchases

## 🚀 Getting Started

### 1. Add Package Reference
```xml
<PackageReference Include="Location.Core.Application" Version="1.0.0" />
```

### 2. Configure Services
```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddApplication();
    // Add your infrastructure services
}
```

### 3. Use in Controllers/ViewModels
```csharp
public class LocationController : ControllerBase
{
    private readonly IMediator _mediator;
    
    public LocationController(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    [HttpPost]
    public async Task<IActionResult> CreateLocation(SaveLocationCommand command)
    {
        var result = await _mediator.Send(command);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Errors);
    }
}
```

## 🐛 Troubleshooting

### Common Issues

1. **Validation Errors**: Check FluentValidation rules in validator classes
2. **Null Island Coordinates**: Ensure coordinates aren't (0,0)
3. **Photo Path Issues**: Verify file paths are valid and accessible
4. **Weather API Limits**: Check API key and rate limiting
5. **Performance Issues**: Review query projections and caching

### Debug Logging
Enable detailed logging to troubleshoot issues:

```csharp
builder.Logging.SetMinimumLevel(LogLevel.Debug);
```

## 👥 Contributing

### Code Standards
- Follow C# naming conventions
- Add XML documentation for public APIs
- Include unit tests for new features
- Use Result pattern for error handling
- Implement proper disposal patterns

### Pull Request Process
1. Create feature branch
2. Add comprehensive tests
3. Update documentation
4. Ensure all tests pass
5. Request code review

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

---

**For more information or support, contact the development team.**