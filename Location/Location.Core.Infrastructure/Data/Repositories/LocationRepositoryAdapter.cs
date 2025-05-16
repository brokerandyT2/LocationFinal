// Location.Core.Infrastructure/Data/Repositories/LocationRepositoryAdapter.cs
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Core.Infrastructure.Data.Repositories
{
    public class LocationRepositoryAdapter : ILocationRepository
    {
        private readonly Location.Core.Application.Common.Interfaces.Persistence.ILocationRepository _innerRepository;

        public LocationRepositoryAdapter(Location.Core.Application.Common.Interfaces.Persistence.ILocationRepository innerRepository)
        {
            _innerRepository = innerRepository ?? throw new ArgumentNullException(nameof(innerRepository));
        }

        public async Task<Result<Domain.Entities.Location>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                var location = await _innerRepository.GetByIdAsync(id, cancellationToken);
                return location != null
                    ? Result<Domain.Entities.Location>.Success(location)
                    : Result<Domain.Entities.Location>.Failure($"Location with ID {id} not found");
            }
            catch (Exception ex)
            {
                return Result<Domain.Entities.Location>.Failure($"Failed to retrieve location: {ex.Message}");
            }
        }

        public async Task<Result<List<Domain.Entities.Location>>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var locations = await _innerRepository.GetAllAsync(cancellationToken);
                return Result<List<Domain.Entities.Location>>.Success(locations.ToList());
            }
            catch (Exception ex)
            {
                return Result<List<Domain.Entities.Location>>.Failure($"Failed to retrieve locations: {ex.Message}");
            }
        }

        public async Task<Result<List<Domain.Entities.Location>>> GetActiveAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var locations = await _innerRepository.GetActiveAsync(cancellationToken);
                return Result<List<Domain.Entities.Location>>.Success(locations.ToList());
            }
            catch (Exception ex)
            {
                return Result<List<Domain.Entities.Location>>.Failure($"Failed to retrieve active locations: {ex.Message}");
            }
        }

        public async Task<Result<Domain.Entities.Location>> CreateAsync(Domain.Entities.Location location, CancellationToken cancellationToken = default)
        {
            try
            {
                var created = await _innerRepository.AddAsync(location, cancellationToken);
                return Result<Domain.Entities.Location>.Success(created);
            }
            catch (Exception ex)
            {
                return Result<Domain.Entities.Location>.Failure($"Failed to create location: {ex.Message}");
            }
        }

        public async Task<Result<Domain.Entities.Location>> UpdateAsync(Domain.Entities.Location location, CancellationToken cancellationToken = default)
        {
            try
            {
                _innerRepository.Update(location);
                // Since Update is void, we assume success
                return Result<Domain.Entities.Location>.Success(location);
            }
            catch (Exception ex)
            {
                return Result<Domain.Entities.Location>.Failure($"Failed to update location: {ex.Message}");
            }
        }

        public async Task<Result<bool>> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                var location = await _innerRepository.GetByIdAsync(id, cancellationToken);
                if (location == null)
                {
                    return Result<bool>.Failure($"Location with ID {id} not found");
                }

                _innerRepository.Delete(location);
                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure($"Failed to delete location: {ex.Message}");
            }
        }

        public async Task<Result<Domain.Entities.Location>> GetByTitleAsync(string title, CancellationToken cancellationToken = default)
        {
            try
            {
                var location = await _innerRepository.GetByTitleAsync(title, cancellationToken);
                return location != null
                    ? Result<Domain.Entities.Location>.Success(location)
                    : Result<Domain.Entities.Location>.Failure($"Location with title '{title}' not found");
            }
            catch (Exception ex)
            {
                return Result<Domain.Entities.Location>.Failure($"Failed to retrieve location by title: {ex.Message}");
            }
        }

        public async Task<Result<List<Domain.Entities.Location>>> GetNearbyAsync(double latitude, double longitude, double distanceKm, CancellationToken cancellationToken = default)
        {
            try
            {
                var locations = await _innerRepository.GetNearbyAsync(latitude, longitude, distanceKm, cancellationToken);
                return Result<List<Domain.Entities.Location>>.Success(locations.ToList());
            }
            catch (Exception ex)
            {
                return Result<List<Domain.Entities.Location>>.Failure($"Failed to retrieve nearby locations: {ex.Message}");
            }
        }

        // Additional methods to match the tests
        public async Task<Result<bool>> SoftDeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                var location = await _innerRepository.GetByIdAsync(id, cancellationToken);
                if (location == null)
                {
                    return Result<bool>.Failure($"Location with ID {id} not found");
                }

                // Mark as deleted using domain method
                location.Delete();
                _innerRepository.Update(location);

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure($"Failed to soft delete location: {ex.Message}");
            }
        }

        public async Task<Result<List<Domain.Entities.Location>>> GetByCoordinatesAsync(
            double latitude,
            double longitude,
            double radiusKm = 10,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var locations = await _innerRepository.GetNearbyAsync(latitude, longitude, radiusKm, cancellationToken);
                return Result<List<Domain.Entities.Location>>.Success(locations.ToList());
            }
            catch (Exception ex)
            {
                return Result<List<Domain.Entities.Location>>.Failure($"Failed to retrieve locations by coordinates: {ex.Message}");
            }
        }
    }
}