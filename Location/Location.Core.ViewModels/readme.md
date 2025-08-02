# Location.Core.ViewModels

## Overview

The `Location.Core.ViewModels` project contains the presentation layer ViewModels for a location-based photography application built using .NET 9.0 and MAUI. This layer implements the MVVM (Model-View-ViewModel) pattern using CommunityToolkit.Mvvm and follows Clean Architecture principles with MediatR for command/query handling.

## Architecture

### Design Patterns
- **MVVM Pattern**: Uses CommunityToolkit.Mvvm for data binding and command handling
- **Command Query Responsibility Segregation (CQRS)**: Separates read and write operations using MediatR
- **Observer Pattern**: Event-driven architecture for error handling and navigation
- **Object Pooling**: Performance optimization for frequently allocated objects

### Performance Optimizations
The codebase includes extensive performance optimizations:
- **Weak Reference Pattern**: Prevents memory leaks in event subscriptions
- **Object Pooling**: Reduces garbage collection pressure
- **String Interning**: Optimizes string allocations for common values
- **Aggressive Inlining**: Method-level optimizations for hot paths
- **Concurrent Collections**: Thread-safe operations where needed
- **ConfigureAwait(false)**: Prevents deadlocks in async operations

## Core Components

### BaseViewModel
**File**: `BaseViewModel.cs`

The foundation class for all ViewModels providing:

#### Key Features
- **Error Handling**: Dual error system for validation vs system errors
- **Command Tracking**: Automatic retry capability for failed operations
- **Thread Safety**: Volatile fields and proper synchronization
- **Memory Management**: Weak event patterns and proper disposal
- **Performance**: Object pooling for event arguments

#### Error Handling Strategy
```csharp
// Validation errors (user-fixable) - stay in UI
SetValidationError("Please enter a valid email address");

// System errors (infrastructure failures) - trigger global handling
OnSystemError("Database connection failed");
```

#### Command Tracking
```csharp
// Automatic command tracking for retry functionality
await ExecuteAndTrackAsync(SaveCommand, parameter);

// Manual retry of last failed command
await RetryLastCommandAsync();
```

### LocationViewModel
**File**: `LocationViewModel.cs`

Manages individual location creation and editing.

#### Responsibilities
- Location data entry and validation
- Photo capture and selection
- GPS coordinate tracking
- Save operations via MediatR commands

#### Key Commands
- `SaveAsync`: Persists location data
- `LoadLocationAsync`: Retrieves existing location
- `TakePhotoAsync`: Handles photo capture/selection
- `StartLocationTrackingAsync`/`StopLocationTrackingAsync`: GPS management

#### Navigation Lifecycle
Implements `INavigationAware` for automatic GPS management:
```csharp
public void OnNavigatedToAsync() => StartLocationTrackingAsync();
public void OnNavigatedFromAsync() => StopLocationTrackingAsync();
```

### LocationsViewModel
**File**: `LocationsViewModel.cs`

Manages the list view of all locations.

#### Features
- Paginated location loading
- Observable collection binding
- Automatic refresh on navigation
- Formatted coordinate display

### TipsViewModel
**File**: `TipsViewModel.cs`

Handles photography tips and tutorials.

#### Structure
- **Tip Types**: Categories like "Landscape", "Portrait", etc.
- **Tips**: Individual advice with camera settings
- **Dynamic Loading**: Type-based filtering

#### Camera Settings Display
```csharp
public string CameraSettingsDisplay =>
    $"{(string.IsNullOrEmpty(Fstop) ? "" : $"F: {Fstop} ")}" +
    $"{(string.IsNullOrEmpty(ShutterSpeed) ? "" : $"Shutter: {ShutterSpeed} ")}" +
    $"{(string.IsNullOrEmpty(Iso) ? "" : $"ISO: {Iso}")}".Trim();
```

### WeatherViewModel
**File**: `WeatherViewModel.cs`

Highly optimized weather forecast display.

#### Performance Features
- **Icon URL Caching**: Prevents repeated string operations
- **Background Processing**: Non-blocking forecast calculations
- **Batch UI Updates**: Minimizes property change notifications
- **Pre-allocated Collections**: Reduces memory allocations

#### Optimization Techniques
```csharp
// String builder reuse
private static readonly ThreadLocal<StringBuilder> _stringBuilder = 
    new(() => new StringBuilder(64));

// Icon URL caching
private readonly ConcurrentDictionary<string, string> _iconUrlCache = new();

// Pre-compiled formatters
private static readonly string _temperatureFormat = "F1";
```

## Navigation Interface

### INavigationAware
**File**: `INavigationAware.cs`

Simple interface for ViewModels that need navigation lifecycle events:
```csharp
public interface INavigationAware
{
    void OnNavigatedToAsync();
    void OnNavigatedFromAsync();
}
```

Used for:
- Starting/stopping GPS tracking
- Loading fresh data
- Cleanup operations

## Error Handling Architecture

### Two-Tier Error System

#### 1. Validation Errors (UI Level)
- User input validation
- Correctable by user action
- Displayed directly in UI
- Examples: "Required field", "Invalid format"

#### 2. System Errors (Infrastructure Level)
- Database failures
- Network issues
- Service unavailability
- Handled globally via events

### Error Flow
```
User Action → Command → MediatR → Service Layer
                ↓
            Success/Failure
                ↓
    Validation Error ← → System Error
           ↓                 ↓
    SetValidationError   OnSystemError
           ↓                 ↓
      UI Display       Global Handler
```

## Dependencies

### NuGet Packages
- **CommunityToolkit.Mvvm** (8.4.0): MVVM framework
- **MediatR** (12.5.0): Command/Query pattern
- **Microsoft.Maui.Controls** (9.0.50): MAUI framework
- **CommunityToolkit.Maui** (11.2.0): MAUI extensions
- **SkiaSharp** (3.119.0): Graphics rendering
- **CosineKitty.AstronomyEngine** (2.1.19): Astronomical calculations
- **Akavache.Core** (10.2.41): Caching framework

### Project References
- **Location.Core.Application**: Business logic and MediatR handlers
- **Location.Photography.Domain**: Domain models and interfaces

## Usage Examples

### Basic ViewModel Implementation
```csharp
public partial class MyViewModel : BaseViewModel, INavigationAware
{
    private readonly IMediator _mediator;

    public MyViewModel(IMediator mediator, IErrorDisplayService errorService) 
        : base(null, errorService)
    {
        _mediator = mediator;
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        try
        {
            IsBusy = true;
            ClearErrors();

            var result = await _mediator.Send(new GetDataQuery());
            
            if (result.IsSuccess)
            {
                // Update UI properties
            }
            else
            {
                OnSystemError(result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            OnSystemError($"Error: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void OnNavigatedToAsync() => LoadDataAsync();
    public void OnNavigatedFromAsync() { /* cleanup */ }
}
```

### Error Handling Patterns
```csharp
// Validation error - user can fix
if (string.IsNullOrEmpty(Title))
{
    SetValidationError("Title is required");
    return;
}

// System error - infrastructure issue
try
{
    var result = await _service.SaveAsync(data);
}
catch (HttpRequestException ex)
{
    OnSystemError($"Network error: {ex.Message}");
}
```

## Testing Considerations

### Unit Testing
- ViewModels have parameterless constructors for design-time support
- Dependencies are injected, making mocking straightforward
- Error scenarios can be tested via MediatR result objects

### Performance Testing
- Object pool effectiveness can be measured
- Memory allocations can be profiled
- UI responsiveness during data loading

### Integration Testing
- Navigation lifecycle events
- MediatR command/query integration
- Error propagation paths

## Best Practices

### Performance
1. **Use ConfigureAwait(false)** for non-UI async operations
2. **Cache frequently used strings** and objects
3. **Batch property updates** to minimize UI notifications
4. **Dispose resources properly** to prevent memory leaks

### Error Handling
1. **Distinguish validation vs system errors** clearly
2. **Provide actionable error messages** to users
3. **Log system errors** for debugging
4. **Handle all async exceptions** properly

### Threading
1. **Use volatile fields** for thread-safe boolean flags
2. **Employ weak event patterns** to prevent memory leaks
3. **Process data on background threads** when possible
4. **Update UI on main thread** only

## Future Enhancements

- **Offline Support**: Cache management for disconnected scenarios
- **Localization**: Multi-language support for error messages
- **Analytics**: Performance and usage tracking
- **Background Tasks**: Automatic data synchronization