using AutoMapper;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Events.Errors;
using Location.Core.Application.Locations.DTOs;
using Location.Core.Application.Resources;
using Location.Core.Domain.ValueObjects;
using MediatR;

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
        private readonly IMediator _mediator;
        /// <summary>
        /// Initializes a new instance of the <see cref="SaveLocationCommandHandler"/> class.
        /// </summary>
        /// <param name="unitOfWork">The unit of work used to manage database transactions and operations.</param>
        /// <param name="mapper">The mapper used to map between domain models and data transfer objects.</param>
        /// <param name="mediator">The mediator used to publish domain events.</param>
        public SaveLocationCommandHandler(IUnitOfWork unitOfWork, IMapper mapper, IMediator mediator)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _mediator = mediator;
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
                        await _mediator.Publish(new LocationSaveErrorEvent(request.Title, LocationErrorType.DatabaseError, AppResources.Location_Error_NotFound), cancellationToken);
                        return Result<LocationDto>.Failure(AppResources.Location_Error_NotFound);
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
                        await _mediator.Publish(new LocationSaveErrorEvent(request.Title, LocationErrorType.DatabaseError, updateResult.ErrorMessage), cancellationToken);
                        return Result<LocationDto>.Failure(AppResources.Location_Error_UpdateFailed);
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
                        await _mediator.Publish(new LocationSaveErrorEvent(request.Title, LocationErrorType.DatabaseError, createResult.ErrorMessage), cancellationToken);
                        return Result<LocationDto>.Failure(AppResources.Location_Error_CreateFailed);
                    }
                    location = createResult.Data;
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                var locationDto = _mapper.Map<LocationDto>(location);
                return Result<LocationDto>.Success(locationDto);
            }
            catch (Domain.Exceptions.LocationDomainException ex) when (ex.Code == "DUPLICATE_TITLE")
            {
                await _mediator.Publish(new LocationSaveErrorEvent(request.Title, LocationErrorType.DuplicateTitle), cancellationToken);
                return Result<LocationDto>.Failure(string.Format(AppResources.Location_Error_DuplicateTitle, request.Title));
            }
            catch (Domain.Exceptions.LocationDomainException ex) when (ex.Code == "INVALID_COORDINATES")
            {
                await _mediator.Publish(new LocationSaveErrorEvent(request.Title, LocationErrorType.InvalidCoordinates), cancellationToken);
                return Result<LocationDto>.Failure(AppResources.Location_Error_InvalidCoordinates);
            }
            catch (Exception ex)
            {
                await _mediator.Publish(new LocationSaveErrorEvent(request.Title, LocationErrorType.NetworkError, ex.Message), cancellationToken);
                return Result<LocationDto>.Failure(string.Format(AppResources.Location_Error_SaveFailed, ex.Message));
            }
        }
    }
}