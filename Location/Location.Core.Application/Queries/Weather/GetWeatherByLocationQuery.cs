
using AutoMapper;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Weather.DTOs;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
namespace Location.Core.Application.Queries.Weather
{
    public class GetWeatherByLocationQuery : IRequest<Result<WeatherDto>>
    {
        public int LocationId { get; set; }
    }
    public class GetWeatherByLocationQueryHandler : IRequestHandler<GetWeatherByLocationQuery, Result<WeatherDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public GetWeatherByLocationQueryHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<Result<WeatherDto>> Handle(GetWeatherByLocationQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var weather = await _unitOfWork.Weather.GetByLocationIdAsync(request.LocationId, cancellationToken);

                if (weather == null)
                {
                    return Result<WeatherDto>.Failure("Weather data not found for this location");
                }

                var weatherDto = _mapper.Map<WeatherDto>(weather);
                return Result<WeatherDto>.Success(weatherDto);
            }
            catch (Exception ex)
            {
                return Result<WeatherDto>.Failure($"Failed to retrieve weather data: {ex.Message}");
            }
        }
    }
}
