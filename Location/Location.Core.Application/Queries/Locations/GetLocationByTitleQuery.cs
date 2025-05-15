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
    public class GetLocationByTitleQuery : IRequest<Result<LocationDto>>
    {
        public string Title { get; set; } = string.Empty;
    }

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