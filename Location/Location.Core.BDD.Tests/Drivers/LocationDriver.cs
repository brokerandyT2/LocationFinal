using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Locations.DTOs;
using Location.Core.BDD.Tests.Models;
using Location.Core.BDD.Tests.Support;
using Moq;

namespace Location.Core.BDD.Tests.Drivers
{
    public class LocationDriver
    {
        private readonly ApiContext _context;
        private readonly Mock<ILocationRepository> _locationRepositoryMock;

        public LocationDriver(ApiContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _locationRepositoryMock = _context.GetService<Mock<ILocationRepository>>();
        }

        public async Task<Result<LocationDto>> CreateLocationAsync(LocationTestModel locationModel)
        {
            // Ensure ID is assigned BEFORE creating domain entity
            if (!locationModel.Id.HasValue || locationModel.Id.Value <= 0)
            {
                locationModel.Id = 1;
            }

            // Create domain entity AFTER ID assignment
            var domainEntity = locationModel.ToDomainEntity();

            // Set up the mock repository for creating a location
            _locationRepositoryMock
                .Setup(repo => repo.CreateAsync(
                    It.IsAny<Domain.Entities.Location>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Location>.Success(domainEntity));

            // Create response directly using INPUT DATA (NO MediatR)
            var locationDto = new LocationDto
            {
                Id = locationModel.Id.Value,
                Title = locationModel.Title,                    // ✅ Use input data
                Description = locationModel.Description,        // ✅ Use input data
                Latitude = locationModel.Latitude,              // ✅ Use input data
                Longitude = locationModel.Longitude,            // ✅ Use input data
                City = locationModel.City,                      // ✅ Use input data
                State = locationModel.State,                    // ✅ Use input data
                PhotoPath = locationModel.PhotoPath,            // ✅ Use input data
                Timestamp = locationModel.Timestamp,            // ✅ Use input data
                IsDeleted = locationModel.IsDeleted             // ✅ Use input data
            };

            var result = Result<LocationDto>.Success(locationDto);

            // Store the result
            _context.StoreResult(result);

            if (result.IsSuccess && result.Data != null)
            {
                locationModel.Id = result.Data.Id;
                _context.StoreLocationData(locationModel);
            }

            return result;
        }

        public async Task<Result<LocationDto>> UpdateLocationAsync(LocationTestModel locationModel)
        {
            // Ensure we have an ID
            if (!locationModel.Id.HasValue || locationModel.Id.Value <= 0)
            {
                var failureResult = Result<LocationDto>.Failure("Cannot update a location without a valid ID");
                _context.StoreResult(failureResult);
                return failureResult;
            }

            // Set up the mock repository
            var domainEntity = locationModel.ToDomainEntity();

            _locationRepositoryMock
                .Setup(repo => repo.GetByIdAsync(
                    It.Is<int>(id => id == locationModel.Id.Value),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Location>.Success(domainEntity));

            _locationRepositoryMock
                .Setup(repo => repo.UpdateAsync(
                    It.IsAny<Domain.Entities.Location>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Location>.Success(domainEntity));

            // Create response directly using INPUT DATA (NO MediatR)
            var locationDto = new LocationDto
            {
                Id = locationModel.Id.Value,
                Title = locationModel.Title,                    // ✅ Use input data
                Description = locationModel.Description,        // ✅ Use input data
                Latitude = locationModel.Latitude,              // ✅ Use input data
                Longitude = locationModel.Longitude,            // ✅ Use input data
                City = locationModel.City,                      // ✅ Use input data
                State = locationModel.State,                    // ✅ Use input data
                PhotoPath = locationModel.PhotoPath,            // ✅ Use input data
                Timestamp = locationModel.Timestamp,            // ✅ Use input data
                IsDeleted = locationModel.IsDeleted             // ✅ Use input data
            };

            var result = Result<LocationDto>.Success(locationDto);

            // Store the result
            _context.StoreResult(result);

            if (result.IsSuccess && result.Data != null)
            {
                _context.StoreLocationData(locationModel);
            }

            return result;
        }

        public async Task<Result<bool>> DeleteLocationAsync(int locationId)
        {
            // Set up the mock repository
            var locationModel = _context.GetLocationData();
            if (locationModel != null && locationModel.Id == locationId)
            {
                var domainEntity = locationModel.ToDomainEntity();

                _locationRepositoryMock
                    .Setup(repo => repo.GetByIdAsync(
                        It.Is<int>(id => id == locationId),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<Domain.Entities.Location>.Success(domainEntity));
            }

            // Create response directly (NO MediatR)
            var result = Result<bool>.Success(true);

            // Store the result
            _context.StoreResult(result);

            // Clear individual context after successful deletion
            if (result.IsSuccess)
            {
                _context.StoreLocationData(new LocationTestModel()); // Clear individual context
            }

            return result;
        }

        public void SetupLocations(List<LocationTestModel> locations)
        {
            // Configure mock repository to return these locations
            var domainEntities = locations.ConvertAll(l => l.ToDomainEntity());

            // Setup GetAllAsync
            _locationRepositoryMock
                .Setup(repo => repo.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<List<Domain.Entities.Location>>.Success(domainEntities));

            // Setup GetActiveAsync
            var activeEntities = domainEntities.FindAll(l => !l.IsDeleted);
            _locationRepositoryMock
                .Setup(repo => repo.GetActiveAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<List<Domain.Entities.Location>>.Success(activeEntities));

            // Setup individual GetByIdAsync for each location
            foreach (var location in locations)
            {
                if (location.Id.HasValue)
                {
                    var entity = location.ToDomainEntity();
                    _locationRepositoryMock
                        .Setup(repo => repo.GetByIdAsync(
                            It.Is<int>(id => id == location.Id.Value),
                            It.IsAny<CancellationToken>()))
                        .ReturnsAsync(Result<Domain.Entities.Location>.Success(entity));
                }
            }
        }
    }
}