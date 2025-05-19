using AutoMapper;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Locations.DTOs;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Core.Application.Queries.Locations
{
    /// <summary>
    /// Represents a query to retrieve a location by its title.
    /// </summary>
    /// <remarks>This query is used to fetch a location that matches the specified title.  The result contains
    /// a <see cref="LocationDto"/> object if a matching location is found.</remarks>
    public class GetLocationByTitleQuery : IRequest<Result<LocationDto>>
    {
        public string Title { get; set; } = string.Empty;
    }
    /// <summary>
    /// Handles queries to retrieve a location by its title.
    /// </summary>
    /// <remarks>This handler processes <see cref="GetLocationByTitleQuery"/> requests to fetch a location
    /// from the data source based on the provided title. If a matching location is found, it is mapped to a <see
    /// cref="LocationDto"/> and returned as part of a successful result. If no matching location is found, a failure
    /// result is returned with an appropriate error message.</remarks>
    public class GetLocationByTitleQueryHandler : IRequestHandler<GetLocationByTitleQuery, Result<LocationDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public GetLocationByTitleQueryHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }
        /// <summary>
        /// Handles the retrieval of a location by its title.
        /// </summary>
        /// <remarks>If the location with the specified title does not exist, the method returns a failure
        /// result with a message indicating that the location was not found. If an unexpected error occurs during
        /// execution, the method returns a failure result with the error message.</remarks>
        /// <param name="request">The query containing the title of the location to retrieve.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A <see cref="Result{T}"/> containing a <see cref="LocationDto"/> if the location is found; otherwise, a
        /// failure result with an appropriate error message.</returns>
        public async Task<Result<LocationDto>> Handle(GetLocationByTitleQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var location = await _unitOfWork.Locations.GetByTitleAsync(request.Title, cancellationToken);

                if (location == null)
                {
                    return Result<LocationDto>.Failure($"Location with title '{request.Title}' not found");
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