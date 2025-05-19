using AutoMapper;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Locations.DTOs;
using Location.Core.Application.Services;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

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

        public AttachPhotoCommandHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
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
                    return Result<LocationDto>.Failure("Location not found");
                }

                var location = locationResult.Data;
                location.AttachPhoto(request.PhotoPath);

                var updateResult = await _unitOfWork.Locations.UpdateAsync(location, cancellationToken);
                if (!updateResult.IsSuccess)
                {
                    return Result<LocationDto>.Failure("Failed to update location");
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                var locationDto = _mapper.Map<LocationDto>(location);
                return Result<LocationDto>.Success(locationDto);
            }
            catch (Exception ex)
            {
                return Result<LocationDto>.Failure($"Failed to attach photo: {ex.Message}");
            }
        }
    }
}