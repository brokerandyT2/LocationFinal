# Location Photography Application - Application Layer

A comprehensive C# application layer for a photography planning app built with .NET 9, focusing on sun/moon calculations, camera equipment management, exposure calculations, and astrophotography tools.

## Overview

This application layer implements the core business logic for a photography planning application using Clean Architecture principles with CQRS and MediatR patterns. It provides features for photographers to plan shoots based on astronomical events, manage camera equipment, calculate optimal exposure settings, and analyze shooting conditions.

## Architecture

### Design Patterns
- **Clean Architecture**: Separation of concerns with clear boundaries
- **CQRS (Command Query Responsibility Segregation)**: Separate models for read and write operations
- **MediatR**: Mediator pattern for decoupled request/response handling
- **Repository Pattern**: Data access abstraction
- **Result Pattern**: Consistent error handling and return types

### Key Components
- **Commands**: Write operations (Create, Update, Delete)
- **Queries**: Read operations with optimized data retrieval
- **Services**: Business logic and external integrations
- **Validators**: FluentValidation for input validation
- **DTOs**: Data transfer objects for API boundaries

## Core Features

### 1. Sun & Moon Calculations
```csharp
// Sun position and timing calculations
ISunService.GetSunPositionAsync(latitude, longitude, dateTime)
ISunService.GetSunTimesAsync(latitude, longitude, date)

// Optimal shooting time recommendations
GetOptimalShootingTimesQuery
GetEnhancedSunTimesQuery
```

### 2. Camera Equipment Management
```csharp
// Camera body management
CreateCameraBodyCommand
GetCameraBodiesQuery

// Lens management with compatibility tracking
CreateLensCommand
GetLensesQuery
```

### 3. Exposure Calculations
```csharp
// Reciprocity calculations for exposure triangle
CalculateExposureCommand
GetExposureValuesQuery

// Supports full, half, and third stop increments
ExposureIncrements.Full | Half | Third
```

### 4. Astrophotography Tools
```csharp
// Astronomical event tracking
GetAstroEventsForDateQuery
GetAstroEquipmentRecommendationQuery

// Meteor shower and celestial object calculations
IMeteorShowerDataService
IAstroCalculationService
```

### 5. Scene Analysis
```csharp
// Image histogram analysis
AnalyzeImageCommand
EvaluateSceneCommand

// Exposure recommendations based on scene analysis
IImageAnalysisService
```

### 6. Subscription Management
```csharp
// In-app purchase handling
ProcessSubscriptionCommand
CheckSubscriptionStatusCommand

// Feature access control
ISubscriptionFeatureGuard
```

## Command/Query Examples

### Creating a Camera Body
```csharp
var command = new CreateCameraBodyCommand
{
    Name = "Canon EOS R5",
    SensorType = "Full Frame",
    SensorWidth = 36.0,
    SensorHeight = 24.0,
    MountType = MountType.CanonRF,
    IsUserCreated = true
};

var result = await _mediator.Send(command, cancellationToken);
```

### Calculating Sun Times
```csharp
var query = new GetSunTimesQuery
{
    Latitude = 37.7749,
    Longitude = -122.4194,
    Date = DateTime.Today
};

var result = await _mediator.Send(query, cancellationToken);
// Returns sunrise, sunset, golden hour, blue hour times
```

### Exposure Calculations
```csharp
var command = new CalculateExposureCommand
{
    BaseExposure = new ExposureTriangleDto
    {
        ShutterSpeed = "1/125",
        Aperture = "f/8",
        Iso = "200"
    },
    TargetAperture = "f/2.8",
    TargetIso = "800",
    Increments = ExposureIncrements.Third,
    ToCalculate = FixedValue.ShutterSpeeds
};

var result = await _mediator.Send(command, cancellationToken);
```

## Services

### Core Services
- **SunService**: Solar calculations and caching
- **TimezoneService**: Geographic timezone resolution
- **ImageAnalysisService**: Histogram generation and exposure analysis
- **CameraDataService**: Equipment management
- **ExposureCalculatorService**: Reciprocity calculations

### Specialized Services
- **AstroCalculationService**: Advanced astronomical calculations
- **SubscriptionService**: In-app purchase management
- **SceneEvaluationService**: Real-time scene analysis

## Performance Optimizations

### Caching Strategy
```csharp
// Time-based caching for expensive calculations
private readonly ConcurrentDictionary<string, (object result, DateTime expiry)> _cache;

// Cache cleanup behavior
public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
```

### Background Processing
```csharp
// CPU-intensive calculations moved to background threads
var result = await Task.Run(() => CalculateComplexAstronomy(), cancellationToken);
```

### Batch Operations
```csharp
// Parallel processing for multiple calculations
GetBatchSunPositionsAsync(List<(double lat, double lon, DateTime dt)> requests)
```

## Validation

All commands include comprehensive validation using FluentValidation:

```csharp
public class CreateCameraBodyCommandValidator : AbstractValidator<CreateCameraBodyCommand>
{
    public CreateCameraBodyCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.SensorWidth).GreaterThan(0);
        RuleFor(x => x.SensorHeight).GreaterThan(0);
    }
}
```

## Error Handling

Consistent error handling using the Result pattern:

```csharp
public class Result<T>
{
    public bool IsSuccess { get; private set; }
    public T Data { get; private set; }
    public string ErrorMessage { get; private set; }
    
    public static Result<T> Success(T data)
    public static Result<T> Failure(string errorMessage)
}
```

## Resource Management

Localized strings managed through resource files:
- `AppResources.resx`: Centralized string resources
- Support for multiple languages and cultures
- Error messages, UI labels, and user-facing text

## Dependencies

### Core Packages
- **.NET 9.0**: Latest framework features
- **MediatR**: Request/response mediation
- **FluentValidation**: Input validation
- **SkiaSharp**: Image processing and histogram generation

### Specialized Packages
- **CosineKitty.AstronomyEngine**: High-precision astronomical calculations
- **Plugin.InAppBilling**: Subscription management
- **CommunityToolkit.Maui**: Cross-platform utilities

## Project Structure

```
Location.Photography.Application/
├── Commands/
│   ├── CameraEvaluation/
│   ├── ExposureCalculator/
│   ├── SceneEvaluation/
│   ├── Subscription/
│   └── SunLocation/
├── Queries/
│   ├── AstroLocation/
│   ├── CameraEvaluation/
│   ├── ExposureCalculator/
│   ├── Subscription/
│   └── SunLocation/
├── Services/
├── Common/
│   ├── Interfaces/
│   ├── Models/
│   └── Constants/
├── Resources/
└── DTOs/
```

## Getting Started

### Prerequisites
- .NET 9.0 SDK
- Visual Studio 2022 or JetBrains Rider
- Understanding of CQRS and MediatR patterns

### Installation
1. Clone the repository
2. Restore NuGet packages
3. Build the solution
4. Run tests to verify installation

### Basic Usage
```csharp
// Register services in DI container
services.AddPhotographyApplication();

// Use MediatR to send commands/queries
var result = await _mediator.Send(new GetSunTimesQuery 
{ 
    Latitude = 40.7128, 
    Longitude = -74.0060, 
    Date = DateTime.Today 
});
```

## Testing Considerations

### Testable Design
- Dependency injection throughout
- Interface-based abstractions
- Cancellation token support
- Pure functions where possible

### Key Test Scenarios
- Sun calculation accuracy across different coordinates
- Exposure reciprocity calculations
- Equipment compatibility validation
- Subscription state management
- Image analysis accuracy

## Configuration

### Service Registration
```csharp
services.AddPhotographyApplication()
    .AddScoped<ISunService, SunService>()
    .AddScoped<IImageAnalysisService, ImageAnalysisService>()
    .AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
```

### Performance Monitoring
```csharp
// Built-in performance behavior for monitoring slow operations
public class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
```

## API Documentation

### Command Patterns
- All commands return `Result<T>` for consistent error handling
- Commands are validated using FluentValidation
- Support for cancellation tokens throughout
- Audit trail and notification support via MediatR

### Query Patterns
- Read-only operations with optimized data access
- Caching support for expensive calculations
- Pagination support for large datasets
- Batch operations for performance

## Contributing

### Code Standards
- Follow Clean Architecture principles
- Use CQRS pattern for new features
- Include comprehensive unit tests
- Document public APIs
- Follow existing naming conventions

### Pull Request Process
1. Create feature branch from develop
2. Implement feature with tests
3. Update documentation
4. Submit pull request with description

## Performance Guidelines

### Best Practices
- Use caching for expensive calculations
- Implement batch operations for multiple requests
- Move CPU-intensive work to background threads
- Use cancellation tokens for long-running operations
- Monitor performance with built-in behaviors

### Memory Management
- Dispose of resources properly
- Use object pooling for frequently created objects
- Implement cache cleanup mechanisms
- Monitor memory usage in production

## Security Considerations

### Data Protection
- Validate all inputs using FluentValidation
- Sanitize user-provided data
- Use parameterized queries
- Implement proper authentication/authorization

### API Security
- Rate limiting for expensive operations
- Input validation at service boundaries
- Secure subscription management
- Protect sensitive astronomical data

## Troubleshooting

### Common Issues
1. **Timezone Resolution Failures**: Check coordinate validity and network connectivity
2. **Subscription Validation Errors**: Verify app store configuration
3. **Astronomical Calculation Errors**: Validate date ranges and coordinates
4. **Performance Issues**: Check caching configuration and batch operation usage

### Debug Tools
- Comprehensive logging throughout
- Performance monitoring behaviors
- Validation error details
- Result pattern for error tracking

## Future Roadmap

### Planned Features
- Advanced weather integration
- Machine learning for exposure recommendations
- Real-time collaboration features
- Enhanced astrophotography planning

### Architecture Improvements
- Event sourcing implementation
- Microservices migration path
- Enhanced caching strategies
- Performance optimizations

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Support

For technical support or questions:
- Create an issue in the repository
- Contact the development team
- Check the documentation wiki
- Review existing issues and discussions