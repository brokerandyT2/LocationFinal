using AutoMapper;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Locations.DTOs;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Core.Application.Locations.Queries.GetLocationById
{
    /// <summary>
    /// Handles queries to retrieve a location by its unique identifier.
    /// </summary>
    /// <remarks>This handler processes a <see cref="GetLocationByIdQuery"/> and returns a <see
    /// cref="Result{T}"/> containing     a <see cref="LocationDto"/> if the location is found. If the location is not
    /// found, a failure result is returned.</remarks>
    public class GetLocationByIdQueryHandler : IRequestHandler<GetLocationByIdQuery, Result<LocationDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        /// <summary>
        /// Initializes a new instance of the <see cref="GetLocationByIdQueryHandler"/> class.
        /// </summary>
        /// <param name="unitOfWork">The unit of work used to interact with the data layer. This parameter cannot be null.</param>
        /// <param name="mapper">The mapper used to transform data between domain and DTO objects. This parameter cannot be null.</param>
        public GetLocationByIdQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }
        /// <summary>
        /// Handles the retrieval of a location by its unique identifier.
        /// </summary>
        /// <param name="request">The query containing the unique identifier of the location to retrieve.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A <see cref="Result{T}"/> containing a <see cref="LocationDto"/> if the location is found; otherwise, a
        /// failure result with an appropriate error message.</returns>
        public async Task<Result<LocationDto>> Handle(GetLocationByIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var location = await _unitOfWork.Locations.GetByIdAsync(request.Id, cancellationToken);

                if (location == null)
                {
                    return Result<LocationDto>.Failure("Location not found");
                }

                var dto = _mapper.Map<LocationDto>(location);
                return Result<LocationDto>.Success(dto);
            }
            catch (Exception ex)
            {
                return Result<LocationDto>.Failure($"Failed to retrieve location: {ex.Message}");
            }
        }
    }
}