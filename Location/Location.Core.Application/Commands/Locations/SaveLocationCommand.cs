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

    public class SaveLocationCommandHandler : IRequestHandler<SaveLocationCommand, Result<LocationDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public SaveLocationCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

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