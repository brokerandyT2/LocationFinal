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

        public async Task<Result<LocationDto>> Handle(AttachPhotoCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var location = await _unitOfWork.Locations.GetByIdAsync(request.LocationId, cancellationToken);

                if (location == null)
                {
                    return Result<LocationDto>.Failure("Location not found");
                }

                location.AttachPhoto(request.PhotoPath);

                _unitOfWork.Locations.Update(location);
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