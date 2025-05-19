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
        /// <summary>
        /// Initializes a new instance of the <see cref="GetLocationsQueryHandler"/> class.
        /// </summary>
        /// <param name="unitOfWork">The unit of work used to access the data layer. This parameter cannot be null.</param>
        public GetLocationsQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        /// <summary>
        /// Handles the retrieval of a paginated list of locations based on the specified query parameters.
        /// </summary>
        /// <remarks>This method retrieves locations from the data source, optionally filters them by a
        /// search term, maps them to DTOs, and returns a paginated result. If an error occurs during the operation, a
        /// failure result is returned with the corresponding error message.</remarks>
        /// <param name="request">The query parameters for retrieving locations, including pagination, search term, and whether to include
        /// deleted locations.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A <see cref="Result{T}"/> containing a <see cref="PagedList{T}"/> of <see cref="LocationListDto"/> objects
        /// if the operation is successful; otherwise, a failure result with an error message.</returns>
        public async Task<Result<PagedList<LocationListDto>>> Handle(GetLocationsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // Get locations based on query parameters
                var locations = request.IncludeDeleted
                    ? await _unitOfWork.Locations.GetAllAsync(cancellationToken)
                    : await _unitOfWork.Locations.GetActiveAsync(cancellationToken);

                if (!locations.IsSuccess || locations.Data == null)
                {
                    return Result<PagedList<LocationListDto>>.Failure(
                        locations.ErrorMessage ?? "Failed to retrieve locations");
                }

                var locationList = locations.Data;

                // Filter by search term if provided
                if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                {
                    locationList = locationList.Where(l =>
                        l.Title.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                        l.Description.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                        l.Address.City.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                        l.Address.City.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                // Map to DTOs
                var locationDtos = locationList.Select(l => new LocationListDto
                {
                    Id = l.Id,
                    Title = l.Title,
                    City = l.Address.City,
                    State = l.Address.City,
                    PhotoPath = l.PhotoPath,
                    Timestamp = l.Timestamp,
                    IsDeleted = l.IsDeleted, 
                    // Map additional properties from the domain entity to DTO
                     Latitude = l.Coordinate?.Latitude ?? 0,
                    Longitude = l.Coordinate?.Longitude ?? 0
                }).ToList();

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