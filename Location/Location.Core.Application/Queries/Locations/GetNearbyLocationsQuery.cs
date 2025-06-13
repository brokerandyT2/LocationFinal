using AutoMapper;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Locations.DTOs;
using Location.Core.Application.Resources;
using MediatR;

namespace Location.Core.Application.Queries.Locations
{
    /// <summary>
    /// Represents a query to retrieve a list of locations within a specified distance from a given geographic
    /// coordinate.
    /// </summary>
    /// <remarks>This query is used to find nearby locations based on latitude, longitude, and a distance
    /// radius in kilometers. The result contains a list of locations that match the criteria.</remarks>
    public class GetNearbyLocationsQuery : IRequest<Result<List<LocationListDto>>>
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double DistanceKm { get; set; } = 10.0;
    }

    /// <summary>
    /// Handles queries to retrieve a list of nearby locations based on geographic coordinates and a specified distance.
    /// </summary>
    /// <remarks>This handler processes a <see cref="GetNearbyLocationsQuery"/> to fetch locations within a
    /// given radius from the specified latitude and longitude. The results are returned as a list of <see
    /// cref="LocationListDto"/> objects.</remarks>
    public class GetNearbyLocationsQueryHandler : IRequestHandler<GetNearbyLocationsQuery, Result<List<LocationListDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetNearbyLocationsQueryHandler"/> class.
        /// </summary>
        /// <param name="unitOfWork">The unit of work used to interact with the data layer. This parameter cannot be <see langword="null"/>.</param>
        /// <param name="mapper">The mapper used to transform data between domain and DTO objects. This parameter cannot be <see
        /// langword="null"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="unitOfWork"/> or <paramref name="mapper"/> is <see langword="null"/>.</exception>
        public GetNearbyLocationsQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork), AppResources.Validation_CannotBeNull);
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper), AppResources.Validation_CannotBeNull);
        }

        /// <summary>
        /// Handles the query to retrieve a list of nearby locations based on the specified geographic coordinates and
        /// distance.
        /// </summary>
        /// <param name="request">The query containing the latitude, longitude, and distance in kilometers to search for nearby locations.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A <see cref="Result{T}"/> containing a list of <see cref="LocationListDto"/> objects representing the nearby
        /// locations. If no locations are found, the result contains an empty list. If an error occurs, the result
        /// contains an error message.</returns>
        public async Task<Result<List<LocationListDto>>> Handle(GetNearbyLocationsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _unitOfWork.Locations.GetNearbyAsync(
                    request.Latitude,
                    request.Longitude,
                    request.DistanceKm,
                    cancellationToken);

                if (!result.IsSuccess)
                {
                    return Result<List<LocationListDto>>.Failure(result.ErrorMessage);
                }

                var locations = result.Data;
                if (locations == null)
                {
                    return Result<List<LocationListDto>>.Success(new List<LocationListDto>());
                }

                var locationDtos = _mapper.Map<List<LocationListDto>>(locations);
                return Result<List<LocationListDto>>.Success(locationDtos);
            }
            catch (Exception ex)
            {
                return Result<List<LocationListDto>>.Failure(string.Format(AppResources.Location_Error_NearbyRetrieveFailed, ex.Message));
            }
        }
    }
}