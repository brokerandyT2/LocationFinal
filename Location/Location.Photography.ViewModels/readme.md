# Location.Photography.ViewModels

A comprehensive collection of ViewModels for a photography-focused location application built with .NET 9 and MAUI. This project provides advanced solar positioning, astrophotography planning, exposure calculation, and weather-aware photography recommendations.

## 🎯 Project Overview

This ViewModels library serves as the presentation layer for a sophisticated photography application that combines:
- **Solar calculations** for optimal lighting conditions
- **Astrophotography planning** with real astronomical data
- **Exposure calculations** with professional-grade accuracy
- **Weather integration** for shooting recommendations
- **Equipment recommendations** based on user gear and targets

## 🏗️ Architecture

### Core Design Patterns

#### MVVM Pattern with CommunityToolkit.Mvvm
```csharp
[ObservableProperty]
private string _selectedTarget;

[RelayCommand]
public async Task CalculateExposureAsync()
{
    // Command implementation
}
```

#### Performance Optimizations
- **Property Change Batching**: Reduces UI update frequency
- **Background Threading**: Heavy calculations on background threads
- **Caching**: Results cached with TTL for repeated operations
- **Throttling**: Rapid input changes throttled to prevent excessive calculations

```csharp
// Example of performance optimization
private async Task CalculateOptimizedAsync()
{
    if (!await _calculationLock.WaitAsync(100))
        return; // Skip if another operation is in progress
    
    try
    {
        await Task.Run(() => PerformCalculations());
    }
    finally
    {
        _calculationLock.Release();
    }
}
```

#### Error Handling Strategy
- **Graceful Degradation**: App continues functioning with limited features on errors
- **User-Friendly Messages**: Technical errors translated to actionable user guidance
- **Retry Mechanisms**: Last command tracking for easy retry functionality

## 📁 Project Structure

```
Location.Photography.ViewModels/
├── Core ViewModels/
│   ├── AstroPhotographyCalculatorViewModel.cs    # Astrophotography planning
│   ├── SunLocationViewModel.cs                   # Real-time sun tracking
│   ├── ExposureCalculatorViewModel.cs            # Camera exposure calculations
│   ├── LightMeterViewModel.cs                    # Light measurement tools
│   └── SceneEvaluationViewModel.cs              # Image analysis
├── Base Classes/
│   ├── ViewModelBase.cs                         # Common functionality
│   └── SubscriptionAwareViewModelBase.cs        # Premium feature management
├── Display Models/
│   ├── HourlyPredictionDisplayModel.cs          # Weather-aware predictions
│   └── OptimalWindowDisplayModel.cs             # Optimal shooting windows
├── Supporting Classes/
│   ├── Astro.cs                                 # Astrophotography data models
│   └── SettingViewModel.cs                      # Application settings
└── Interfaces/
    └── Various interface definitions
```

## 🔧 Key Components

### 1. AstroPhotographyCalculatorViewModel
**Purpose**: Comprehensive astrophotography planning with real astronomical data

**Key Features**:
- **Real Astronomical Calculations**: Uses actual sun/moon/planet positions
- **Target-Specific Recommendations**: Equipment suggestions based on chosen targets
- **User Equipment Matching**: Analyzes user's gear for target compatibility
- **Hourly Predictions**: 24-hour forecast for optimal shooting windows

**Supported Targets**:
```csharp
public enum AstroTarget
{
    MilkyWayCore, Moon, Planets, ISS, 
    DeepSkyObjects, StarTrails, MeteorShowers,
    // Specific planets: Mercury, Venus, Mars, Jupiter, Saturn, Uranus, Neptune, Pluto
    // Specific DSOs: M31_Andromeda, M42_Orion, M51_Whirlpool, etc.
    // Constellations: Orion, Cassiopeia, UrsaMajor, etc.
}
```

**Performance Features**:
- Progressive calculation with real-time UI updates
- Caching system for repeated location/date combinations
- Background threading for heavy astronomical calculations

### 2. SunLocationViewModel
**Purpose**: Real-time sun tracking with device sensors

**Key Features**:
- **Live Compass Integration**: Arrow points to sun's current position
- **Elevation Matching**: Accelerometer helps align camera angle
- **Enhanced Light Analysis**: Real-time EV calculations with weather impact
- **Haptic Feedback**: Vibration when elevation alignment achieved

**Sensor Integration**:
```csharp
public void StartSensors()
{
    // Compass for direction
    Compass.ReadingChanged += OnCompassReadingChanged;
    Compass.Start(SensorSpeed.UI);
    
    // Accelerometer for tilt
    Accelerometer.ReadingChanged += OnAccelerometerReadingChanged;
    Accelerometer.Start(SensorSpeed.UI);
}
```

### 3. ExposureCalculatorViewModel
**Purpose**: Professional exposure calculations with equivalent exposure triangles

**Key Features**:
- **Reciprocal Calculations**: Calculate any exposure parameter from the other two
- **Standard Increment Support**: Full, half, and third stops
- **Lock Mechanism**: Lock any parameter while adjusting others
- **Preset Integration**: Photography scenario presets

**Calculation Example**:
```csharp
// User locks aperture at f/8, changes shutter speed
// ViewModel automatically calculates new ISO to maintain exposure
var result = await _exposureCalculatorService.CalculateIsoAsync(
    baseExposure, newShutterSpeed, lockedAperture, ExposureIncrements.Third);
```

### 4. EnhancedSunCalculatorViewModel
**Purpose**: Advanced sun position calculations with weather integration

**Key Features**:
- **Hourly Light Predictions**: 24-48 hour forecasts
- **Weather Impact Analysis**: Cloud cover, precipitation, visibility effects
- **Optimal Window Detection**: Automatic identification of best shooting times
- **Multi-timezone Support**: Device and location timezone handling

### 5. LightMeterViewModel
**Purpose**: Digital light meter with EV calculations

**Key Features**:
- **Live Light Reading**: Real-time lux to EV conversion
- **Exposure Comparison**: Current settings vs. measured light
- **Over/Under Exposure Warnings**: Visual feedback for exposure accuracy
- **EV Compensation**: Slider for intentional over/under exposure

## 🛠️ Technology Stack

### Core Dependencies
```xml
<PackageReference Include="CommunityToolkit.Mvvm" Version="8.4.0" />
<PackageReference Include="MediatR" Version="12.5.0" />
<PackageReference Include="Microsoft.Maui.Controls" Version="9.0.50" />
<PackageReference Include="CosineKitty.AstronomyEngine" Version="2.1.19" />
```

### Design Patterns Used
- **MVVM**: Clean separation with CommunityToolkit.Mvvm
- **CQRS**: MediatR for command/query separation
- **Observer**: Property change notifications
- **Factory**: Dynamic ViewModel creation
- **Strategy**: Different calculation strategies per target type

## 🚀 Getting Started

### Prerequisites
- .NET 9 SDK
- Visual Studio 2022 (17.8+) or JetBrains Rider
- MAUI workload installed

### Basic Usage Example

```csharp
// Initialize astrophotography calculator
var astroViewModel = new AstroPhotographyCalculatorViewModel(
    mediator, errorService, astroService, /* other dependencies */);

// Load locations and equipment
await astroViewModel.LoadLocationsCommand.ExecuteAsync(null);
await astroViewModel.LoadEquipmentCommand.ExecuteAsync(null);

// Select target and calculate
astroViewModel.SelectedTarget = AstroTarget.MilkyWayCore;
await astroViewModel.CalculateAstroDataCommand.ExecuteAsync(null);

// Access results
var predictions = astroViewModel.HourlyAstroPredictions;
var recommendations = astroViewModel.EquipmentRecommendation;
```

## 🧪 Testing Considerations

### Unit Testing Focus Areas
1. **Calculation Accuracy**: Verify astronomical and exposure calculations
2. **Error Handling**: Test graceful degradation scenarios
3. **Performance**: Validate caching and throttling mechanisms
4. **Thread Safety**: Ensure proper async/await patterns

### Integration Testing
1. **Weather Service Integration**: Mock weather APIs for consistent results
2. **Sensor Integration**: Test compass and accelerometer handling
3. **Database Operations**: Verify equipment and location persistence

### UI Testing
1. **Command Execution**: Verify commands execute without errors
2. **Property Binding**: Ensure UI updates reflect ViewModel changes
3. **Performance**: Test with large datasets and rapid user interactions

## 📊 Performance Characteristics

### Caching Strategy
- **Calculation Cache**: 30-minute TTL for expensive computations
- **Weather Cache**: 60-minute TTL for weather data
- **Equipment Cache**: Session-level caching for user equipment

### Threading Model
- **UI Thread**: Property updates and command initiation
- **Background Thread**: Calculations, API calls, database operations
- **Timer Thread**: Sensor data smoothing and live updates

### Memory Management
- **Automatic Cleanup**: Expired cache entries removed automatically
- **Resource Disposal**: Proper cleanup of sensors and timers
- **Weak References**: Event handlers use weak references where appropriate

## 🐛 Common Issues & Solutions

### Issue: Calculations Taking Too Long
**Solution**: Check if caching is working properly, verify background threading

### Issue: Sensor Data Erratic
**Solution**: Implement smoothing algorithms already present in codebase

### Issue: Weather Data Unavailable
**Solution**: Graceful degradation with default weather assumptions

### Issue: Memory Leaks
**Solution**: Ensure proper disposal of ViewModels and unsubscribe from events

## 🔄 Future Enhancements

### Planned Features
- **AR Integration**: Augmented reality sun/star overlay
- **Machine Learning**: Auto-detection of optimal conditions
- **Cloud Sync**: Cross-device settings synchronization
- **Advanced Analytics**: Shot success prediction algorithms

### Architecture Improvements
- **Source Generators**: Reduce runtime reflection overhead
- **Incremental Updates**: More granular property change notifications
- **Worker Services**: Background calculation services

## 👥 Contributing

### Code Standards
- Follow existing naming conventions
- Use performance optimization patterns established in codebase
- Implement proper error handling and logging
- Add comprehensive XML documentation

### Pull Request Process
1. Ensure all calculations have unit tests
2. Verify performance impact of changes
3. Update documentation for new features
4. Test across different device types and screen sizes

## 📝 License

This project is part of a larger photography application suite. See the main project repository for licensing information.

---

*For technical questions or architecture discussions, please refer to the main project documentation or contact the development team.*