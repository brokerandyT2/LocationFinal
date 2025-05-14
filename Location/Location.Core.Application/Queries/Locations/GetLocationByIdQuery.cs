using AutoMapper;
using Location.Core.Application.Common;
using Location.Core.Application.Common.Interfaces.Persistence;
using Location.Core.Application.Common.Models;
using Location.Core.Application.DTOs;
using Location.Core.Application.Interfaces;
using Location.Core.Application.Locations.DTOs;
using MediatR;

namespace Location.Core.Application.Queries.Locations
{
    public class GetLocationByIdQuery : IRequest<Result<LocationDto>>
    {
        public int Id { get; set; }
    }

    public class GetLocationByIdQueryHandler : IRequestHandler<GetLocationByIdQuery, Result<LocationDto>>
    {
        private readonly ILocationRepository _locationRepository;
        private readonly IMapper _mapper;

        public GetLocationByIdQueryHandler(
            ILocationRepository locationRepository,
            IMapper mapper)
        {
            _locationRepository = locationRepository;
            _mapper = mapper;
        }

        public async Task<Result<LocationDto>> Handle(GetLocationByIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var location = await _locationRepository.GetByIdAsync(request.Id, cancellationToken);

                if (location == null)
                {
                    return Result<LocationDto>.Failure("Location not found");
                }

                var locationDto = _mapper.Map<LocationDto>(location);
                return Result<LocationDto>.Success(locationDto);
            }
            catch (Exception ex)
            {
                return Result<LocationDto>.Failure($"Failed to retrieve location: {ex.Message}");
            }
        }
    }
}