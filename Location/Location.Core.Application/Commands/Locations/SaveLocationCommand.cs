using AutoMapper;
using FluentValidation;
using Location.Core.Application.Common;
using Location.Core.Application.Interfaces;
using Location.Core.Application.DTOs;
using Location.Core.Domain.Entities;
using MediatR;
using Location.Core.Application.Common.Interfaces.Persistence;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Locations.DTOs;

namespace Location.Core.Application.Commands.Locations
{
    public class SaveLocationCommand : IRequest<Result<LocationDto>>
    {
        public int? Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string? PhotoPath { get; set; }
        public string? Address { get; set; }
    }

    public class SaveLocationCommandHandler : IRequestHandler<SaveLocationCommand, Result<LocationDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILocationRepository _locationRepository;

        public SaveLocationCommandHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILocationRepository locationRepository)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _locationRepository = locationRepository;
        }

        public async Task<Result<LocationDto>> Handle(SaveLocationCommand request, CancellationToken cancellationToken)
        {
            try
            {
                Domain.Entities.Location location;

                if (request.Id.HasValue)
                {
                    // Update existing location
                    location = await _locationRepository.GetByIdAsync(request.Id.Value, cancellationToken);
                    if (location == null)
                    {
                        return Result<LocationDto>.Failure("Location not found");
                    }

                    location.UpdateDetails(
                        request.Title,
                        request.Description,
                        request.Latitude,
                        request.Longitude,
                        request.PhotoPath,
                        request.Address);
                }
                else
                {
                    // Create new location
                    location = new Domain.Entities.Location(
                        request.Title,
                        request.Latitude,
                        request.Longitude,
                        request.Description,
                        request.PhotoPath,
                        request.Address);

                    await _locationRepository.AddAsync(location, cancellationToken);
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