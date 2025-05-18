using AutoMapper;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Locations.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Core.Application.Queries.Locations
{
    public class GetNearbyLocationsQuery : IRequest<Result<List<LocationListDto>>>
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double DistanceKm { get; set; } = 10.0;
    }

    public class GetNearbyLocationsQueryHandler : IRequestHandler<GetNearbyLocationsQuery, Result<List<LocationListDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public GetNearbyLocationsQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

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
                return Result<List<LocationListDto>>.Failure($"Failed to retrieve nearby locations: {ex.Message}");
            }
        }
    }
}