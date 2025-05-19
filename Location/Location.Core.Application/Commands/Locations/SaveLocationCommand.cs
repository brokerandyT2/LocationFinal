using AutoMapper;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Locations.DTOs;
using Location.Core.Domain.ValueObjects;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Core.Application.Commands.Locations
{
    /// <summary>
    /// Represents a command to save a location with its associated details.
    /// </summary>
    /// <remarks>This command is used to create or update a location record. If the <see cref="Id"/> property
    /// is null,  a new location will be created. If <see cref="Id"/> is provided, the existing location with the
    /// specified  identifier will be updated.</remarks>
    public class SaveLocationCommand : IRequest<Result<LocationDto>>
    {
        public int? Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string? PhotoPath { get; set; }
    }
    /// <summary>
    /// Handles the execution of the <see cref="SaveLocationCommand"/> to create or update a location.
    /// </summary>
    /// <remarks>This handler processes the <see cref="SaveLocationCommand"/> by either creating a new
    /// location or updating an existing one, based on the presence of the <c>Id</c> property in the command. It
    /// validates the input, updates the location's details, and persists the changes to the database. If successful, it
    /// returns a <see cref="Result{T}"/> containing the updated or newly created <see cref="LocationDto"/>. Otherwise,
    /// it returns a failure result with an appropriate error message.</remarks>
    public class SaveLocationCommandHandler : IRequestHandler<SaveLocationCommand, Result<LocationDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        /// <summary>
        /// Initializes a new instance of the <see cref="SaveLocationCommandHandler"/> class.
        /// </summary>
        /// <param name="unitOfWork">The unit of work used to manage database transactions and operations.</param>
        /// <param name="mapper">The mapper used to map between domain models and data transfer objects.</param>
        public SaveLocationCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }
        /// <summary>
        /// Handles the process of saving a location by either creating a new location or updating an existing one.
        /// </summary>
        /// <remarks>If the <paramref name="request"/> contains an existing location ID, the method
        /// attempts to update the corresponding location. If the ID is not provided, a new location is created. The
        /// method ensures that all changes are persisted to the database.</remarks>
        /// <param name="request">The command containing the details of the location to save, including its title, description, coordinates,
        /// and optional photo path.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A <see cref="Result{T}"/> containing a <see cref="LocationDto"/> representing the saved location if the
        /// operation is successful; otherwise, a failure result with an appropriate error message.</returns>
        public async Task<Result<LocationDto>> Handle(SaveLocationCommand request, CancellationToken cancellationToken)
        {
            try
            {
                Domain.Entities.Location location;

                if (request.Id.HasValue)
                {
                    var existingLocationResult = await _unitOfWork.Locations.GetByIdAsync(request.Id.Value, cancellationToken);
                    if (!existingLocationResult.IsSuccess || existingLocationResult.Data == null)
                    {
                        return Result<LocationDto>.Failure("Location not found");
                    }

                    location = existingLocationResult.Data;
                    location.UpdateDetails(request.Title, request.Description ?? string.Empty);

                    var newCoordinate = new Coordinate(request.Latitude, request.Longitude);
                    location.UpdateCoordinate(newCoordinate);

                    if (!string.IsNullOrEmpty(request.PhotoPath))
                    {
                        location.AttachPhoto(request.PhotoPath);
                    }

                    var updateResult = await _unitOfWork.Locations.UpdateAsync(location, cancellationToken);
                    if (!updateResult.IsSuccess)
                    {
                        return Result<LocationDto>.Failure("Failed to update location");
                    }
                }
                else
                {
                    var coordinate = new Coordinate(request.Latitude, request.Longitude);
                    var address = new Address(request.City, request.State);

                    location = new Domain.Entities.Location(
                        request.Title,
                        request.Description ?? string.Empty,
                        coordinate,
                        address);

                    if (!string.IsNullOrEmpty(request.PhotoPath))
                    {
                        location.AttachPhoto(request.PhotoPath);
                    }

                    var createResult = await _unitOfWork.Locations.CreateAsync(location, cancellationToken);
                    if (!createResult.IsSuccess || createResult.Data == null)
                    {
                        return Result<LocationDto>.Failure("Failed to create location");
                    }
                    location = createResult.Data;
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                var locationDto = _mapper.Map<LocationDto>(location);
                return Result<LocationDto>.Success(locationDto);
            }
            catch (Exception ex)
            {
                return Result<LocationDto>.Failure($"Failed to save location: {ex.Message}");
            }
        }
    }
}