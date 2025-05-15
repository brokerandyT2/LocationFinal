using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Locations.DTOs;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Core.Application.Locations.Queries.GetLocations
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
        private readonly IUnitOfWork _unitOfWork;

        public GetLocationsQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

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