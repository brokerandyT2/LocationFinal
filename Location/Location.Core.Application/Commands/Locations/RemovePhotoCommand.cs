using AutoMapper;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Events.Errors;
using Location.Core.Application.Locations.DTOs;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Core.Application.Commands.Locations
{
    public class RemovePhotoCommand : IRequest<Result<LocationDto>>
    {
        public int LocationId { get; set; }
    }

    /// <summary>
    /// Handles the removal of a photo from a location and updates the location's state in the data store.
    /// </summary>
    /// <remarks>This handler processes a <see cref="RemovePhotoCommand"/> to remove a photo associated with a
    /// specific location. It retrieves the location, removes the photo, updates the location in the data store, and
    /// saves the changes. If the operation is successful, the updated location is returned as a <see
    /// cref="LocationDto"/>.</remarks>
    public class RemovePhotoCommandHandler : IRequestHandler<RemovePhotoCommand, Result<LocationDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IMediator _mediator;

        public RemovePhotoCommandHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IMediator mediator)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _mediator = mediator;
        }

        /// <summary>
        /// /// Handles the removal of a photo from a location and updates the location in the data store.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<Result<LocationDto>> Handle(RemovePhotoCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var locationResult = await _unitOfWork.Locations.GetByIdAsync(request.LocationId, cancellationToken);

                if (!locationResult.IsSuccess || locationResult.Data == null)
                {
                    await _mediator.Publish(new LocationSaveErrorEvent($"Location ID {request.LocationId}", LocationErrorType.DatabaseError, "Location not found"), cancellationToken);
                    return Result<LocationDto>.Failure("Location not found");
                }

                var location = locationResult.Data;
                location.RemovePhoto();

                var updateResult = await _unitOfWork.Locations.UpdateAsync(location, cancellationToken);
                if (!updateResult.IsSuccess)
                {
                    await _mediator.Publish(new LocationSaveErrorEvent(location.Title, LocationErrorType.DatabaseError, updateResult.ErrorMessage), cancellationToken);
                    return Result<LocationDto>.Failure("Failed to update location");
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                var locationDto = _mapper.Map<LocationDto>(location);
                return Result<LocationDto>.Success(locationDto);
            }
            catch (Exception ex)
            {
                await _mediator.Publish(new LocationSaveErrorEvent($"Location ID {request.LocationId}", LocationErrorType.NetworkError, ex.Message), cancellationToken);
                return Result<LocationDto>.Failure($"Failed to remove photo: {ex.Message}");
            }
        }
    }
}