using AutoMapper;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Locations.DTOs;
using MediatR;

namespace Location.Core.Application.Queries.Locations
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
    /// Handles the retrieval of a paginated list of locations based on the specified query parameters.
    /// </summary>
    /// <remarks>This handler processes a <see cref="GetLocationsQuery"/> to retrieve a filtered and paginated
    /// list of locations. It supports optional search functionality and the inclusion of deleted locations.</remarks>
    public class GetLocationsQueryHandler : IRequestHandler<GetLocationsQuery, Result<PagedList<LocationListDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        /// <summary>
        /// Initializes a new instance of the <see cref="GetLocationsQueryHandler"/> class.
        /// </summary>
        /// <param name="unitOfWork">The unit of work used to manage database transactions and access repositories. This parameter cannot be <see
        /// langword="null"/>.</param>
        /// <param name="mapper">The mapper used to transform data between domain models and DTOs. This parameter cannot be <see
        /// langword="null"/>.</param>
        public GetLocationsQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }
        /// <summary>
        /// Handles the retrieval of a paginated list of locations based on the specified query parameters.
        /// </summary>
        /// <remarks>The method retrieves locations from the data source, optionally filters them based on
        /// a search term, and maps them to DTOs. The results are then paginated according to the specified page number
        /// and page size.</remarks>
        /// <param name="request">The query parameters for retrieving locations, including pagination, search term, and whether to include
        /// deleted locations.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A <see cref="Result{T}"/> containing a <see cref="PagedList{T}"/> of <see cref="LocationListDto"/> objects
        /// if the operation is successful; otherwise, a failure result with an error message.</returns>
        public async Task<Result<PagedList<LocationListDto>>> Handle(GetLocationsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var locationsResult = request.IncludeDeleted
                    ? await _unitOfWork.Locations.GetAllAsync(cancellationToken)
                    : await _unitOfWork.Locations.GetActiveAsync(cancellationToken);

                if (!locationsResult.IsSuccess || locationsResult.Data == null)
                {
                    return Result<PagedList<LocationListDto>>.Failure(locationsResult.ErrorMessage ?? "Failed to retrieve locations");
                }

                var locationList = locationsResult.Data;

                if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                {
                    locationList = locationList.Where(l =>
                        l.Title.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                        l.Description.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                        l.Address.City.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                        l.Address.State.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                var locationDtos = _mapper.Map<List<LocationListDto>>(locationList);

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