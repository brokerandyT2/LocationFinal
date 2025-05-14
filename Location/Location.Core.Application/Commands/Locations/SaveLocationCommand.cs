using AutoMapper;
using FluentValidation;
using Location.Core.Application.Common;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Interfaces.Persistence;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Locations.DTOs;
using Location.Core.Domain.Entities;
using Location.Core.Domain.ValueObjects;
using MediatR;

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
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
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

                    location.UpdateDetails(request.Title, request.Description);

                    // Update coordinates
                    var newCoordinate = new Coordinate(request.Latitude, request.Longitude);
                    location.UpdateCoordinate(newCoordinate);

                    // Update photo if provided
                    if (!string.IsNullOrEmpty(request.PhotoPath))
                    {
                        location.AttachPhoto(request.PhotoPath);
                    }
                }
                else
                {
                    // Create new location
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