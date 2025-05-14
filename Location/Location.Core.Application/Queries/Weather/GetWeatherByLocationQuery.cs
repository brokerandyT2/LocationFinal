using AutoMapper;
using Location.Core.Application.Common;
using Location.Core.Application.Common.Interfaces.Persistence;
using Location.Core.Application.Common.Models;
using Location.Core.Application.DTOs;
using Location.Core.Application.Interfaces;
using Location.Core.Application.Weather.DTOs;
using MediatR;

namespace Location.Core.Application.Queries.Weather
{
    public class GetWeatherByLocationQuery : IRequest<Result<WeatherDto>>
    {
        public int LocationId { get; set; }
    }

    public class GetWeatherByLocationQueryHandler : IRequestHandler<GetWeatherByLocationQuery, Result<WeatherDto>>
    {
        private readonly IWeatherRepository _weatherRepository;
        private readonly IMapper _mapper;

        public GetWeatherByLocationQueryHandler(
            IWeatherRepository weatherRepository,
            IMapper mapper)
        {
            _weatherRepository = weatherRepository;
            _mapper = mapper;
        }

        public async Task<Result<WeatherDto>> Handle(GetWeatherByLocationQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var weather = await _weatherRepository.GetByLocationIdAsync(request.LocationId, cancellationToken);

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