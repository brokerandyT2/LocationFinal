using AutoMapper;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Events.Errors;
using Location.Core.Application.Locations.DTOs;
using Location.Core.Application.Resources;
using MediatR;

namespace Location.Core.Application.Commands.Locations
{
    public class AttachPhotoCommand : IRequest<Result<LocationDto>>
    {
        public int LocationId { get; set; }
        public string PhotoPath { get; set; } = string.Empty;
    }

    public class AttachPhotoCommandHandler : IRequestHandler<AttachPhotoCommand, Result<LocationDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IMediator _mediator;

        public AttachPhotoCommandHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IMediator mediator)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _mediator = mediator;
        }

        /// <summary>
        /// Handles the process of attaching a photo to a location and updating the location in the data store.
        /// </summary>
        /// <remarks>This method retrieves the location by its ID, attaches the specified photo, updates
        /// the location in the data store, and saves the changes. If the location is not found or the update fails, a
        /// failure result is returned.</remarks>
        /// <param name="request">The command containing the location ID and the path to the photo to be attached.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A <see cref="Result{T}"/> containing a <see cref="LocationDto"/> if the operation succeeds; otherwise, a
        /// failure result with an error message.</returns>
        public async Task<Result<LocationDto>> Handle(AttachPhotoCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var locationResult = await _unitOfWork.Locations.GetByIdAsync(request.LocationId, cancellationToken);

                if (!locationResult.IsSuccess || locationResult.Data == null)
                {
                    await _mediator.Publish(new LocationSaveErrorEvent($"Location ID {request.LocationId}", LocationErrorType.DatabaseError, AppResources.Location_Error_NotFound), cancellationToken);
                    return Result<LocationDto>.Failure(AppResources.Location_Error_NotFound);
                }

                var location = locationResult.Data;
                location.AttachPhoto(request.PhotoPath);

                var updateResult = await _unitOfWork.Locations.UpdateAsync(location, cancellationToken);
                if (!updateResult.IsSuccess)
                {
                    await _mediator.Publish(new LocationSaveErrorEvent(location.Title, LocationErrorType.DatabaseError, updateResult.ErrorMessage), cancellationToken);
                    return Result<LocationDto>.Failure(AppResources.Location_Error_UpdateFailed);
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                var locationDto = _mapper.Map<LocationDto>(location);
                return Result<LocationDto>.Success(locationDto);
            }
            catch (Domain.Exceptions.LocationDomainException ex) when (ex.Code == "INVALID_PHOTO_PATH")
            {
                await _mediator.Publish(new LocationSaveErrorEvent($"Location ID {request.LocationId}", LocationErrorType.ValidationError, ex.Message), cancellationToken);
                return Result<LocationDto>.Failure(AppResources.Location_Error_PhotoPathInvalid);
            }
            catch (Exception ex)
            {
                await _mediator.Publish(new LocationSaveErrorEvent($"Location ID {request.LocationId}", LocationErrorType.NetworkError, ex.Message), cancellationToken);
                return Result<LocationDto>.Failure(string.Format(AppResources.Location_Error_AttachPhotoFailed, ex.Message));
            }
        }
    }
}