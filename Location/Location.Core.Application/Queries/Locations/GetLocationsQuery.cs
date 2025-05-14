using AutoMapper;
using Location.Core.Application.Common;
using Location.Core.Application.Common.Interfaces.Persistence;
using Location.Core.Application.Common.Models;
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
                // Get all locations (or active ones based on request)
                var locations = request.IncludeDeleted
                    ? await _locationRepository.GetAllAsync(cancellationToken)
                    : await _locationRepository.GetActiveAsync(cancellationToken);

                // Apply search filter if provided
                if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                {
                    locations = locations.Where(l =>
                        l.Title.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                        l.Description.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                        l.Address.City.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                        l.Address.State.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase));
                }

                // Convert to list for paging
                var locationsList = locations.ToList();

                // Map to DTOs
                var locationDtos = _mapper.Map<List<LocationListDto>>(locationsList);

                // Create paged result
                var pagedList = PagedList<LocationListDto>.Create(
                    locationDtos,
                    request.PageNumber,
                    request.PageSize);

                return Result<PagedList<LocationListDto>>.Success(pagedList);
            }
            catch (Exception ex)
            {
                return Result<PagedList<LocationListDto>>.Failure($"Failed to retrieve locations: {ex.Message}");
            }
        }
    }
}