using AutoMapper;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Locations.DTOs;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Core.Application.Locations.Queries.GetLocations
{
    /// <summary>
    /// Represents a query to retrieve a paginated list of locations, optionally filtered by a search term and including
    /// deleted entries.
    /// </summary>
    /// <remarks>This query is used to request a paginated list of locations from the data source.  The
    /// results can be filtered by a search term and can optionally include deleted locations.</remarks>
    public class GetLocationsQuery : IRequest<Result<PagedList<LocationListDto>>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public bool IncludeDeleted { get; set; } = false;
    }
    /// <summary>
    /// Handles the query to retrieve a paginated list of locations based on the specified criteria.
    /// </summary>
    /// <remarks>This handler processes the <see cref="GetLocationsQuery"/> to retrieve location data,
    /// optionally including deleted locations, filtering by a search term, and returning the results in a paginated
    /// format.</remarks>
    public class GetLocationsQueryHandler : IRequestHandler<GetLocationsQuery, Result<PagedList<LocationListDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        public GetLocationsQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<Result<PagedList<LocationListDto>>> Handle(GetLocationsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // Push all filtering and pagination to database level
                var pagedLocationsResult = await _unitOfWork.Locations.GetPagedAsync(
                    pageNumber: request.PageNumber,
                    pageSize: request.PageSize,
                    searchTerm: request.SearchTerm,
                    includeDeleted: request.IncludeDeleted,
                    cancellationToken: cancellationToken);

                if (!pagedLocationsResult.IsSuccess || pagedLocationsResult.Data == null)
                {
                    return Result<PagedList<LocationListDto>>.Failure(
                        pagedLocationsResult.ErrorMessage ?? "Failed to retrieve locations");
                }

                // Use AutoMapper for efficient bulk mapping
                var locationDtos = _mapper.Map<PagedList<LocationListDto>>(pagedLocationsResult.Data);

                return Result<PagedList<LocationListDto>>.Success(locationDtos);
            }
            catch (Exception ex)
            {
                return Result<PagedList<LocationListDto>>.Failure($"Failed to retrieve locations: {ex.Message}");
            }
        }
    }
}
