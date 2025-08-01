# Location.Core.Application.dll

## Overview

`Location.Core.Application.dll` is the core application layer for the Location solution, providing business logic, domain services, MediatR-based eventing, and cross-platform abstractions for .NET 9 and .NET MAUI applications. It defines the main contracts, behaviors, and events for location management, alerting, geolocation, and more.

---

## Key Features

- **Alerting & Notification:**  
  - `AlertEvent` and `AlertType` for standardized, localizable alert messages via MediatR.
- **Geolocation Services:**  
  - `IGeolocationService` interface and DTOs for device location, permissions, and tracking.
- **Repository Abstractions:**  
  - `ILocationRepository` and SQLite-optimized specifications for high-performance data access.
- **Pipeline Behaviors:**  
  - `LoggingBehavior<TRequest, TResponse>` for efficient, structured request/response logging.
- **Domain-Driven Design:**  
  - Domain entities (e.g., `Location`), value objects, and event publishing for robust business logic.
- **Cross-Platform Ready:**  
  - Designed for .NET MAUI, with abstractions for platform-specific implementations.

---

## Main Components

### Alerts

- **AlertEvent**  
  Publishes alert notifications (title, message, type) using MediatR.
- **AlertType**  
  Enum for Info, Success, Warning, Error.

### Geolocation

- **IGeolocationService**  
  Interface for device geolocation, permission checks, and tracking.
- **GeolocationDto**  
  Data transfer object for latitude, longitude, altitude, accuracy, and timestamp.
- **GeolocationAccuracy**  
  Enum for location accuracy levels.

### Data Access

- **ILocationRepository**  
  Interface for CRUD, projection, specification, and bulk operations on locations.
- **ISqliteSpecification<T>**  
  Specification pattern for SQLite queries.
- **BaseSqliteSpecification<T>**  
  Base class for building SQLite query specifications.
- **LocationSpecifications**  
  Common specifications (active, search, nearby).

### Behaviors

- **LoggingBehavior<TRequest, TResponse>**  
  MediatR pipeline behavior for structured, performant logging and error event publishing.

---

## Example Usage
// Publishing an alert 
await mediator.Publish(new AlertEvent( message: "Location saved successfully.", title: "Success", type: AlertType.Success));
// Using geolocation service 

var locationResult = await geolocationService.GetCurrentLocationAsync(); if (locationResult.IsSuccess) { var loc = locationResult.Data; // Use loc.Latitude, loc.Longitude, etc. }
// Querying locations with a specification 

var spec = new LocationSpecifications.ActiveLocationsSpec(); var activeLocations = await locationRepository.GetBySpecificationAsync(spec, CancellationToken.None);


---

## Integration

- **Dependencies:**  
  - MediatR
  - Microsoft.Extensions.Logging
  - CommunityToolkit.Mvvm (for ViewModels)
  - .NET MAUI (for platform services)
- **Target Framework:** .NET 9
- **Language Version:** C# 13.0

---

## Stakeholder Information

- **Intended Audience:**  
  - Developers building cross-platform location-based apps.
  - Technical leads and architects.
  - QA and support teams.

- **Business Value:**  
  - Centralizes business logic and contracts for maintainability.
  - Enables robust, testable, and scalable location solutions.
  - Supports rapid development of .NET MAUI and backend services.

---

## Contact

For support or questions, contact the Location application development team or project owner.
