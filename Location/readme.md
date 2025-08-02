# Location Photography Project

A comprehensive .NET 9 photography application ecosystem featuring location-based photography planning, astrophotography calculations, weather integration, and automated cloud deployment. Built with Clean Architecture, this system provides photographers with professional-grade tools for planning shoots based on astronomical events and environmental conditions.

## 🎯 System Overview

This is a multi-platform photography application that combines:
- **Astronomical calculations** for optimal shooting times
- **Weather-aware recommendations** for photography conditions  
- **Equipment management** with compatibility tracking
- **Location-based planning** with GPS integration
- **Automated cloud infrastructure** with Azure deployment
- **Cross-platform mobile support** (iOS/Android via MAUI)

## 🏗️ Architecture

The system follows **Clean Architecture** with clear separation of concerns:

```
┌─────────────────────────────────────────────────────────────┐
│                    Presentation Layer                       │
│  📱 MAUI ViewModels  │  🌐 Generated APIs  │  🔧 CLI Tools  │
├─────────────────────────────────────────────────────────────┤
│                    Application Layer                        │
│        🎯 Commands & Queries (CQRS + MediatR)              │
├─────────────────────────────────────────────────────────────┤
│                     Domain Layer                            │
│     📋 Entities  │  💼 Business Logic  │  🌟 Astronomical   │
├─────────────────────────────────────────────────────────────┤
│                  Infrastructure Layer                       │
│  🗄️ Repositories  │  ☁️ External APIs  │  🛠️ Data Access   │
├─────────────────────────────────────────────────────────────┤
│                     Database Layer                          │
│        📊 SQLite (Mobile)  │  🏢 SQL Server (Cloud)         │
└─────────────────────────────────────────────────────────────┘
```

## 📦 Core Projects

### 🔧 Infrastructure & Data ([Infrastructure](./Location.Core.Infrastructure/readme.md))
**Location.Core.Infrastructure** - The data foundation
- **SQLite-based** repositories with compiled expression mapping
- **Weather API integration** with offline-first caching
- **Automated database initialization** with sample data
- **Enterprise-grade error handling** and logging
- **Performance optimizations** with batch operations

Key Features:
- Thread-safe database operations
- Weather data validation and caching
- Comprehensive resource localization
- Exception mapping between layers

### 🏢 Domain Models ([Core Domain](./Location.Core.Domain/readme.md) | [Photography Domain](./Location.Photography.Domain/readme.md))
**Location.Core.Domain** - Location and weather entities
- Geographic coordinate calculations with performance caching
- Weather forecasting with 7-day predictions
- Photography tips system with categorization
- Domain events for cross-cutting concerns

**Location.Photography.Domain** - Photography-specific domain
- Camera equipment specifications (200+ profiles)
- Astronomical calculations for astrophotography
- Lens compatibility matrices
- Subscription management for premium features

### 🎯 Application Logic ([Core Application](./Location.Core.Application/readme.md) | [Photography Application](./Location.Photography.Application/readme.md))
**Location.Core.Application** - Core business operations
- CQRS pattern with MediatR for clean command/query separation
- Comprehensive validation using FluentValidation
- Location management with photo attachment
- Error aggregation and display services

**Location.Photography.Application** - Photography calculations
- **Sun/moon positioning** with precision astronomical calculations
- **Exposure calculator** with reciprocity calculations
- **Equipment recommendations** based on shooting targets
- **Scene analysis** for optimal camera settings

### 📱 ViewModels ([Core ViewModels](./Location.Core.ViewModels/readme.md) | [Photography ViewModels](./Location.Photography.ViewModels/readme.md))
**MVVM presentation layer** with performance optimizations
- **Real-time sensor integration** (GPS, compass, accelerometer)
- **Background calculations** to maintain UI responsiveness
- **Object pooling** and caching for memory efficiency
- **Error handling** with user-friendly messaging

Photography ViewModels include:
- **AstroPhotographyCalculatorViewModel** - 24-hour optimal shooting predictions
- **SunLocationViewModel** - Real-time sun tracking with device sensors
- **ExposureCalculatorViewModel** - Professional exposure triangle calculations
- **LightMeterViewModel** - Digital light meter with EV calculations

## 🚀 Development Tools

### 🤖 Code Generation ([Adapter Generator](./PhotographyAdapterGenerator/readme.md))
**PhotographyAdapterGenerator** - Cross-platform mobile adapters
- Generates Kotlin (Android) and Swift (iOS) adapters from C# ViewModels
- Type-safe mapping with platform-specific optimizations
- Zero-configuration for standard types, attribute-based customization
- Handles complex types like geographic coordinates and date semantics

### 🗄️ Database Management ([SQL Server Sync](./SQLServerSyncGenerator/readme.md))
**SQLServerSyncGenerator** - Automated database deployment
- **MSBuild integration** - auto-runs after Domain builds
- **Schema generation** from .NET entities with attributes
- **Production safety** with automatic backup and rollback
- **Validation pipeline** with conditional deployment approval

### 🌐 API Generation ([API Generator](./APIGenerator/readme.md))
**Location.AutomatedAPIGenerator** - Complete REST API pipeline
- **Auto-discovers** Infrastructure.dll entities
- **Generates** Azure Functions with authentication
- **Compiles and deploys** to Azure with Bicep templates
- **Domain-driven versioning** with unlimited backward compatibility

## ⭐ Key Features

### 🌟 Astrophotography Planning
- **Real astronomical calculations** using CosineKitty.AstronomyEngine
- **28 meteor showers** with peak dates and visibility predictions
- **Celestial object positioning** for galaxies, nebulae, and planets
- **Equipment recommendations** based on target objects and user gear
- **Hourly predictions** for optimal shooting windows

### 📸 Professional Photography Tools
- **Exposure calculator** with full/half/third stop increments
- **Scene evaluation** with histogram analysis
- **Light meter** functionality with EV calculations
- **Camera equipment database** with compatibility tracking
- **Weather-aware recommendations** for shooting conditions

### 🌍 Location & Weather Integration
- **GPS tracking** with coordinate validation
- **Weather forecasting** with photography-specific analysis
- **Location management** with photo attachment
- **Offline-first** approach with intelligent caching

### 💳 Subscription Management
- **In-app purchase** handling across platforms
- **Feature gating** for premium functionality
- **Enterprise-grade** subscription tracking
- **Cross-platform** billing support

## 🛠️ Performance Features

### ⚡ Optimizations
- **Compiled expression mapping** (10x faster than reflection)
- **Background threading** for CPU-intensive calculations
- **Multi-level caching** with intelligent TTL management
- **Batch operations** for database efficiency
- **Object pooling** to reduce garbage collection

### 📊 Monitoring & Diagnostics
- **Structured logging** with performance metrics
- **Error correlation** with operation identifiers
- **Cache hit ratio** monitoring
- **Real-time performance** tracking

## 🔧 Getting Started

### Prerequisites
- .NET 9 SDK
- Visual Studio 2022 (17.8+) or JetBrains Rider
- MAUI workload for mobile development
- Azure subscription (for cloud features)

### Quick Setup
```bash
# Clone the repository
git clone [repository-url]
cd Location

# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Install development tools
dotnet tool install -g --add-source ./SQLServerSyncGenerator/bin/Debug SQLServerSyncGenerator
dotnet tool install -g --add-source ./PhotographyAdapterGenerator/bin/Debug Location.Photography.AdapterGenerator
```

### Database Setup
The SQLServerSyncGenerator automatically creates your database schema:
```bash
# For local development (auto-runs on Domain builds)
dotnet build Location.Photography.Domain

# For manual database creation
sql-schema-generator --server "localhost\SQLEXPRESS" --database "LocationDev" --local
```

### Running the Application
```bash
# MAUI application
dotnet build Location.MAUI
dotnet run --project Location.MAUI

# Generate mobile adapters
photography-viewmodel-generator --platform android --output "src/main/kotlin/generated/"
```

## 🌐 Cloud Deployment

### Automated API Deployment
```bash
# Complete pipeline: generate → compile → deploy → archive
location-api-generator \
  --auto-discover \
  --server "myserver.database.windows.net" \
  --database "LocationAnalytics" \
  --keyvault-url "https://myvault.vault.azure.net/" \
  --azure-subscription "sub-id" \
  --resource-group "rg-locationapis"
```

### Generated API Endpoints
- `POST /photography/v4/register` - Account registration
- `POST /photography/v4/backup/{email}` - Upload backup data
- `GET /photography/v4/restore/{email}` - Download backup data
- `POST /photography/v4/forgetme/{email}` - GDPR compliance

## 🧪 Testing Strategy

### Unit Testing
- **Repository pattern** enables easy mocking
- **Result pattern** simplifies assertion testing
- **Dependency injection** throughout for testability
- **CancellationToken** support for timeout testing

### Integration Testing
- **Database initialization** flow validation
- **Weather API** integration testing
- **Astronomical calculation** accuracy verification
- **Cross-platform** compatibility testing

### Performance Testing
- **Caching effectiveness** measurement
- **Database operation** performance monitoring
- **Memory allocation** profiling
- **UI responsiveness** under load

## 📈 Monitoring & Operations

### Production Monitoring
- **Application Insights** integration for telemetry
- **Health checks** for external services
- **Performance counters** for key operations
- **Error tracking** with correlation IDs

### Database Operations
- **Automatic schema** updates via MSBuild integration
- **Production rollback** protection with database backups
- **Query performance** monitoring and optimization
- **Index usage** analysis and recommendations

## 🔮 Technology Stack

### Core Framework
- **.NET 9** - Latest framework with performance improvements
- **MAUI** - Cross-platform mobile development
- **Entity Framework Core** - Data access with SQLite
- **MediatR** - CQRS pattern implementation

### Specialized Libraries
- **CosineKitty.AstronomyEngine** - High-precision astronomical calculations
- **SkiaSharp** - Image processing and histogram generation
- **CommunityToolkit.Mvvm** - MVVM framework
- **Polly** - Resilience patterns for external services

### Azure Services
- **Azure Functions** - Serverless API hosting
- **Azure SQL Database** - Cloud data storage
- **Azure Key Vault** - Secure credential management
- **Azure DevOps Artifacts** - Binary storage and versioning

## 🤝 Contributing

### Development Workflow
1. **Domain changes** trigger automatic schema analysis
2. **ViewModels** auto-generate mobile adapters
3. **Infrastructure** updates deploy via pipeline
4. **API changes** version automatically with backward compatibility

### Code Standards
- **Clean Architecture** principles throughout
- **Async/await** patterns with proper ConfigureAwait
- **Result pattern** for consistent error handling
- **Comprehensive logging** with structured data
- **Performance-first** design with caching and optimization

### Pull Request Process
1. Ensure all tests pass
2. Update relevant documentation
3. Verify mobile adapter generation
4. Test database schema changes
5. Review performance impact

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 📞 Support

For technical support or questions:
- Review project-specific README files for detailed documentation
- Check existing issues in the repository
- Contact the development team for architecture discussions

---

**🎯 Target Audience**: Professional photographers, astrophotography enthusiasts, and mobile developers interested in location-based applications with real-time sensor integration and astronomical calculations.