using AutoMapper;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Services;
using Location.Core.Application.Weather.DTOs;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Core.Application.Weather.Queries.GetWeatherForecast
{
    public class GetWeatherForecastQuery : IRequest<Result<WeatherForecastDto>>
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int Days { get; set; } = 7;
    }

    public class GetWeatherForecastQueryHandler : IRequestHandler<GetWeatherForecastQuery, Result<WeatherForecastDto>>
    {
        private readonly IWeatherService _weatherService;
        private readonly IMapper _mapper;

        public GetWeatherForecastQueryHandler(
            IWeatherService weatherService,
            IMapper mapper)
        {
            _weatherService = weatherService;
            _mapper = mapper;
        }

        public async Task<Result<WeatherForecastDto>> Handle(GetWeatherForecastQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var forecastResult = await _weatherService.GetForecastAsync(
                    request.Latitude,
                    request.Longitude,
                    request.Days,
                    cancellationToken);

                if (!forecastResult.IsSuccess || forecastResult.Data == null)
                {
                    return Result<WeatherForecastDto>.Failure(forecastResult.ErrorMessage ?? "Failed to get weather forecast");
                }

                return forecastResult;
            }
            catch (Exception ex)
            {
                return Result<WeatherForecastDto>.Failure($"Failed to retrieve weather forecast: {ex.Message}");
            }
        }
    }
}