using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Locations.DTOs;
using Location.Core.Application.Commands.Locations;
using Location.Core.BDD.Tests.Models;
using Location.Core.BDD.Tests.Support;
using MediatR;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Core.BDD.Tests.Drivers
{
    public class LocationDriver
    {
        private readonly ApiContext _context;
        private readonly IMediator _mediator;
        private readonly Mock<ILocationRepository> _locationRepositoryMock;

        public LocationDriver(ApiContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _mediator = _context.GetService<IMediator>();
            _locationRepositoryMock = _context.GetService<Mock<ILocationRepository>>();
        }

        public async Task<Result<LocationDto>> CreateLocationAsync(LocationTestModel locationModel)
        {
            // Set up the mock repository for creating a location
            var domainEntity = locationModel.ToDomainEntity();

            _locationRepositoryMock
                .Setup(repo => repo.CreateAsync(
                    It.IsAny<Domain.Entities.Location>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Location>.Success(domainEntity));

            // Create the command
            var command = new SaveLocationCommand
            {
                Title = locationModel.Title,
                Description = locationModel.Description,
                Latitude = locationModel.Latitude,
                Longitude = locationModel.Longitude,
                City = locationModel.City,
                State = locationModel.State,
                PhotoPath = locationModel.PhotoPath
            };

            // Send the command
            var result = await _mediator.Send(command);

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
                throw new InvalidOperationException("Cannot update a location without a valid ID");
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

            // Create the command
            var command = new SaveLocationCommand
            {
                Id = locationModel.Id.Value,
                Title = locationModel.Title,
                Description = locationModel.Description,
                Latitude = locationModel.Latitude,
                Longitude = locationModel.Longitude,
                City = locationModel.City,
                State = locationModel.State,
                PhotoPath = locationModel.PhotoPath
            };

            // Send the command
            var result = await _mediator.Send(command);

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

            // Create the command
            var command = new DeleteLocationCommand
            {
                Id = locationId
            };

            // Send the command
            var result = await _mediator.Send(command);

            // Store the result
            _context.StoreResult(result);

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