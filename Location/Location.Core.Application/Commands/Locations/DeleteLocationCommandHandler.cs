using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Events.Errors;
using MediatR;

namespace Location.Core.Application.Commands.Locations
{
    public class DeleteLocationCommandHandler : IRequestHandler<DeleteLocationCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMediator _mediator;

        public DeleteLocationCommandHandler(IUnitOfWork unitOfWork, IMediator mediator)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _mediator = mediator;
        }

        /// <summary>
        /// Handles the deletion of a location by its identifier.
        /// </summary>
        /// <remarks>If the location is not found or the update operation fails, the method returns a
        /// failure result with an appropriate error message.</remarks>
        /// <param name="request">The command containing the identifier of the location to delete.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A <see cref="Result{T}"/> indicating the success or failure of the operation.  Returns <see
        /// langword="true"/> if the location was successfully deleted; otherwise, <see langword="false"/>.</returns>
        public async Task<Result<bool>> Handle(DeleteLocationCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var locationResult = await _unitOfWork.Locations.GetByIdAsync(request.Id, cancellationToken);

                if (!locationResult.IsSuccess || locationResult.Data == null)
                {
                    await _mediator.Publish(new LocationSaveErrorEvent($"Location ID {request.Id}", LocationErrorType.DatabaseError, "Location not found"), cancellationToken);
                    return Result<bool>.Failure("Location not found");
                }

                var location = locationResult.Data;
                location.Delete();

                var updateResult = await _unitOfWork.Locations.UpdateAsync(location, cancellationToken);
                if (!updateResult.IsSuccess)
                {
                    await _mediator.Publish(new LocationSaveErrorEvent(location.Title, LocationErrorType.DatabaseError, updateResult.ErrorMessage), cancellationToken);
                    return Result<bool>.Failure("Failed to update location");
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<bool>.Success(true);
            }
            catch (Domain.Exceptions.LocationDomainException ex) when (ex.Code == "LOCATION_IN_USE")
            {
                await _mediator.Publish(new LocationSaveErrorEvent($"Location ID {request.Id}", LocationErrorType.ValidationError, ex.Message), cancellationToken);
                return Result<bool>.Failure("Cannot delete location that is currently in use");
            }
            catch (Exception ex)
            {
                await _mediator.Publish(new LocationSaveErrorEvent($"Location ID {request.Id}", LocationErrorType.DatabaseError, ex.Message), cancellationToken);
                return Result<bool>.Failure($"Failed to delete location: {ex.Message}");
            }
        }
    }
}