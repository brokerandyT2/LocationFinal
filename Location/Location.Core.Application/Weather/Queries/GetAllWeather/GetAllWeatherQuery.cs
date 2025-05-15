using AutoMapper;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Weather.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Core.Application.Weather.Queries.GetAllWeather
{
    public class GetAllWeatherQuery : IRequest<Result<List<WeatherDto>>>
    {
        public bool IncludeExpired { get; set; } = false;
    }

    public class GetAllWeatherQueryHandler : IRequestHandler<GetAllWeatherQuery, Result<List<WeatherDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public GetAllWeatherQueryHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<Result<List<WeatherDto>>> Handle(GetAllWeatherQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var weatherList = request.IncludeExpired
                    ? await _unitOfWork.Weather.GetRecentAsync(int.MaxValue, cancellationToken)
                    : await _unitOfWork.Weather.GetRecentAsync(10, cancellationToken);

                var weatherDtos = weatherList.Select(w => _mapper.Map<WeatherDto>(w)).ToList();

                return Result<List<WeatherDto>>.Success(weatherDtos);
            }
            catch (Exception ex)
            {
                return Result<List<WeatherDto>>.Failure($"Failed to retrieve weather data: {ex.Message}");
            }
        }
    }
}