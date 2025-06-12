using AutoMapper;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Events.Errors;
using Location.Core.Application.Locations.DTOs;
using Location.Core.Application.Resources;
using MediatR;

namespace Location.Core.Application.Commands.Locations
{
    public class RestoreLocationCommand : IRequest<Result<LocationDto>>
    {
        public int LocationId { get; set; }
    }

    /// <summary>
    /// Handles the restoration of a location by its identifier.
    /// </summary>
    /// <remarks>This class is responsible for processing the <see cref="RestoreLocationCommand"/> to restore
    /// a location in the system. It retrieves the location from the data store, invokes its restore operation, and
    /// persists the changes. If the location is not found or the update operation fails, a failure result is
    /// returned.</remarks>
    public class RestoreLocationCommandHandler : IRequestHandler<RestoreLocationCommand, Result<LocationDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IMediator _mediator;

        public RestoreLocationCommandHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IMediator mediator)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _mediator = mediator;
        }

        /// <summary>
        /// Handles the restoration of a location by its identifier.
        /// </summary>
        /// <remarks>This method attempts to restore a location by retrieving it from the data store,
        /// invoking its restore operation, and saving the changes. If the location is not found or the update operation
        /// fails, a failure result is returned. Any exceptions encountered during the process are captured and included
        /// in the failure result.</remarks>
        /// <param name="request">The command containing the identifier of the location to restore.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A <see cref="Result{T}"/> containing a <see cref="LocationDto"/> if the operation succeeds; otherwise, a
        /// failure result with an error message.</returns>
        public async Task<Result<LocationDto>> Handle(RestoreLocationCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var locationResult = await _unitOfWork.Locations.GetByIdAsync(request.LocationId, cancellationToken);

                if (!locationResult.IsSuccess || locationResult.Data == null)
                {
                    await _mediator.Publish(new LocationSaveErrorEvent(string.Format(AppResources.Location_Error_NotFoundById, request.LocationId), LocationErrorType.DatabaseError, AppResources.Location_Error_NotFound), cancellationToken);
                    return Result<LocationDto>.Failure(AppResources.Location_Error_NotFound);
                }

                var location = locationResult.Data;
                location.Restore();

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
            catch (Exception ex)
            {
                await _mediator.Publish(new LocationSaveErrorEvent(string.Format(AppResources.Location_Error_NotFoundById, request.LocationId), LocationErrorType.NetworkError, ex.Message), cancellationToken);
                return Result<LocationDto>.Failure(string.Format(AppResources.Location_Error_RestoreFailed, ex.Message));
            }
        }
    }
}