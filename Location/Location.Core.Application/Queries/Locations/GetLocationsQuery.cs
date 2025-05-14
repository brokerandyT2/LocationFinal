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
    public class GetLocationsQuery : IRequest<Result<PagedList<LocationListDto>>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public bool IncludeDeleted { get; set; } = false;
    }

    public class GetLocationsQueryHandler : IRequestHandler<GetLocationsQuery, Result<PagedList<LocationListDto>>>
    {
        private readonly ILocationRepository _locationRepository;
        private readonly IMapper _mapper;

        public GetLocationsQueryHandler(
            ILocationRepository locationRepository,
            IMapper mapper)
        {
            _locationRepository = locationRepository;
            _mapper = mapper;
        }

        public async Task<Result<PagedList<LocationListDto>>> Handle(GetLocationsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var locations = await _locationRepository.GetPagedAsync(
                    request.PageNumber,
                    request.PageSize,
                    request.SearchTerm,
                    request.IncludeDeleted,
                    cancellationToken);

                var locationDtos = _mapper.Map<List<LocationListDto>>(locations.Items);

                var pagedList = new PagedList<LocationListDto>(
                    locationDtos,
                    locations.TotalCount,
                    locations.CurrentPage,
                    locations.PageSize);

                return Result<PagedList<LocationListDto>>.Success(pagedList);
            }
            catch (Exception ex)
            {
                return Result<PagedList<LocationListDto>>.Failure($"Failed to retrieve locations: {ex.Message}");
            }
        }
    }
}