using Location.Core.Application.Common.Interfaces.Persistence;
using Location.Core.Application.Common.Models;
using Location.Core.Domain.Rules;
using Location.Core.Domain.ValueObjects;
using Location.Core.Infrastructure.Data.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Core.Infrastructure.Data.Repositories
{

    public class LocationRepository : Location.Core.Application.Common.Interfaces.Persistence.ILocationRepository
    {
        private readonly IDatabaseContext _context;
        private readonly ILogger<LocationRepository> _logger;

        public LocationRepository(IDatabaseContext context, ILogger<LocationRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Domain.Entities.Location?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                var entity = await _context.GetAsync<LocationEntity>(id);

                if (entity == null)
                {
                    return null;
                }

                var domainLocation = MapToDomain(entity);
                return domainLocation;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving location with ID {LocationId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<Domain.Entities.Location>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var entities = await _context.Table<LocationEntity>()
                    .OrderByDescending(l => l.Timestamp)
                    .ToListAsync();

                var domainLocations = entities.Select(MapToDomain);
                return domainLocations;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all locations");
                throw;
            }
        }

        public async Task<IEnumerable<Domain.Entities.Location>> GetActiveAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var entities = await _context.Table<LocationEntity>()
                    .Where(l => !l.IsDeleted)
                    .OrderByDescending(l => l.Timestamp)
                    .ToListAsync();

                var domainLocations = entities.Select(MapToDomain);
                return domainLocations;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active locations");
                throw;
            }
        }

        public async Task<Domain.Entities.Location> AddAsync(Domain.Entities.Location location, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate the location
                if (!LocationValidationRules.IsValid(location, out var errors))
                {
                    throw new InvalidOperationException(string.Join("; ", errors));
                }

                var entity = MapToEntity(location);
                entity.Timestamp = DateTime.UtcNow;

                await _context.InsertAsync(entity);

                // Update the domain object with the generated ID
                SetPrivateProperty(location, "Id", entity.Id);

                _logger.LogInformation("Created location with ID {LocationId}", entity.Id);
                return location;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating location");
                throw;
            }
        }

        public async Task UpdateAsync(Domain.Entities.Location location, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate the location
                if (!LocationValidationRules.IsValid(location, out var errors))
                {
                    throw new InvalidOperationException(string.Join("; ", errors));
                }

                var entity = MapToEntity(location);
                await _context.UpdateAsync(entity);

                _logger.LogInformation("Updated location with ID {LocationId}", location.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating location with ID {LocationId}", location.Id);
                throw;
            }
        }

        public async Task DeleteAsync(Domain.Entities.Location location, CancellationToken cancellationToken = default)
        {
            try
            {
                var entity = MapToEntity(location);
                await _context.DeleteAsync(entity);

                _logger.LogInformation("Deleted location with ID {LocationId}", location.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting location with ID {LocationId}", location.Id);
                throw;
            }
        }

        public async Task<Domain.Entities.Location?> GetByTitleAsync(string title, CancellationToken cancellationToken = default)
        {
            try
            {
                var entity = await _context.Table<LocationEntity>()
                    .Where(l => l.Title == title)
                    .FirstOrDefaultAsync();

                return entity != null ? MapToDomain(entity) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving location by title {Title}", title);
                throw;
            }
        }

        public async Task<IEnumerable<Domain.Entities.Location>> GetNearbyAsync(
            double latitude,
            double longitude,
            double distanceKm,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Get all locations and filter by distance in memory
                // For a production system, consider using spatial queries
                var entities = await _context.Table<LocationEntity>()
                    .Where(l => !l.IsDeleted)
                    .ToListAsync();

                var centerCoordinate = new Coordinate(latitude, longitude);
                var nearbyLocations = new List<Domain.Entities.Location>();

                foreach (var entity in entities)
                {
                    var location = MapToDomain(entity);
                    if (location.Coordinate.DistanceTo(centerCoordinate) <= distanceKm)
                    {
                        nearbyLocations.Add(location);
                    }
                }

                return nearbyLocations;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving locations by coordinates");
                throw;
            }
        }

        #region Mapping Methods

        private Domain.Entities.Location MapToDomain(LocationEntity entity)
        {
            var coordinate = new Coordinate(entity.Latitude, entity.Longitude);
            var address = new Address(entity.City, entity.State);

            // Create location using reflection since constructor is protected
            var location = CreateLocationViaReflection(
                entity.Title,
                entity.Description,
                coordinate,
                address);

            // Set properties using reflection
            SetPrivateProperty(location, "Id", entity.Id);
            SetPrivateProperty(location, "PhotoPath", entity.PhotoPath);
            SetPrivateProperty(location, "IsDeleted", entity.IsDeleted);
            SetPrivateProperty(location, "Timestamp", entity.Timestamp);

            return location;
        }

        private LocationEntity MapToEntity(Domain.Entities.Location location)
        {
            return new LocationEntity
            {
                Id = location.Id,
                Title = location.Title,
                Description = location.Description,
                Latitude = location.Coordinate.Latitude,
                Longitude = location.Coordinate.Longitude,
                City = location.Address.City,
                State = location.Address.State,
                PhotoPath = location.PhotoPath,
                IsDeleted = location.IsDeleted,
                Timestamp = location.Timestamp
            };
        }

        private Domain.Entities.Location CreateLocationViaReflection(
            string title,
            string description,
            Coordinate coordinate,
            Address address)
        {
            var type = typeof(Domain.Entities.Location);
            var constructor = type.GetConstructor(
                new[] { typeof(string), typeof(string), typeof(Coordinate), typeof(Address) });

            if (constructor == null)
            {
                throw new InvalidOperationException("Cannot find Location constructor");
            }

            return (Domain.Entities.Location)constructor.Invoke(
                new object[] { title, description, coordinate, address });
        }

        private void SetPrivateProperty(object obj, string propertyName, object value)
        {
            var property = obj.GetType().GetProperty(propertyName);
            property?.SetValue(obj, value);
        }

        #endregion
    }
}