using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Locations.DTOs;
using Location.Core.Domain.ValueObjects;

namespace Location.Core.Application.Locations.Commands.SaveLocation
{
    /// <summary>
    /// Handler for SaveLocationCommand
    /// </summary>
    public class SaveLocationCommandHandler : IRequestHandler<SaveLocationCommand, Result<LocationDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IEventBus _eventBus;

        public SaveLocationCommandHandler(IUnitOfWork unitOfWork, IMapper mapper, IEventBus eventBus)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _eventBus = eventBus;
        }

        public async Task<Result<LocationDto>> Handle(SaveLocationCommand request, CancellationToken cancellationToken)
        {
            try
            {
                Domain.Entities.Location location;
                Result<Domain.Entities.Location> result;

                if (request.Id.HasValue && request.Id.Value > 0)
                {
                    // Update existing location
                    var existingResult = await _unitOfWork.Locations.GetByIdAsync(request.Id.Value, cancellationToken);

                    if (!existingResult.IsSuccess || existingResult.Data == null)
                    {
                        return Result<LocationDto>.Failure(Error.NotFound($"Location with ID {request.Id.Value} not found"));
                    }

                    location = existingResult.Data;

                    // Update location details
                    location.UpdateDetails(request.Title, request.Description);

                    // Update coordinates if changed
                    var newCoordinate = new Coordinate(request.Latitude, request.Longitude);
                    if (location.Coordinate.Latitude != newCoordinate.Latitude ||
                        location.Coordinate.Longitude != newCoordinate.Longitude)
                    {
                        location.UpdateCoordinate(newCoordinate);
                    }

                    // Update photo if provided
                    if (!string.IsNullOrEmpty(request.PhotoPath))
                    {
                        location.AttachPhoto(request.PhotoPath);
                    }

                    result = await _unitOfWork.Locations.UpdateAsync(location, cancellationToken);
                }
                else
                {
                    // Create new location
                    var coordinate = new Coordinate(request.Latitude, request.Longitude);
                    var address = new Address(request.City, request.State);

                    location = new Domain.Entities.Location(
                        request.Title,
                        request.Description,
                        coordinate,
                        address);

                    if (!string.IsNullOrEmpty(request.PhotoPath))
                    {
                        location.AttachPhoto(request.PhotoPath);
                    }

                    result = await _unitOfWork.Locations.CreateAsync(location, cancellationToken);
                }

                if (!result.IsSuccess || result.Data == null)
                {
                    return Result<LocationDto>.Failure(result.ErrorMessage ?? "Failed to save location");
                }

                // Save changes
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Publish domain events
                if (result.Data.DomainEvents.Count > 0)
                {
                    await _eventBus.PublishAllAsync(result.Data.DomainEvents.ToArray(), cancellationToken);
                    result.Data.ClearDomainEvents();
                }

                // Map to DTO and return
                var dto = _mapper.Map<LocationDto>(result.Data);
                return Result<LocationDto>.Success(dto);
            }
            catch (Domain.Exceptions.InvalidCoordinateException ex)
            {
                return Result<LocationDto>.Failure(ex);
            }
            catch (Exception ex)
            {
                return Result<LocationDto>.Failure(Error.Database($"Failed to save location: {ex.Message}"));
            }
        }
    }
}