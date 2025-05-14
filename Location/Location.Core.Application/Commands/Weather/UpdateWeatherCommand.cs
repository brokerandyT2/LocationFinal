using AutoMapper;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Services;
using Location.Core.Application.Weather.DTOs;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Core.Application.Commands.Weather
{
    public class UpdateWeatherCommand : IRequest<Result<WeatherDto>>
    {
        public int LocationId { get; set; }
        public bool ForceUpdate { get; set; }
    }

    public class UpdateWeatherCommandHandler : IRequestHandler<UpdateWeatherCommand, Result<WeatherDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWeatherService _weatherService;
        private readonly IMapper _mapper;

        public UpdateWeatherCommandHandler(
            IUnitOfWork unitOfWork,
            IWeatherService weatherService,
            IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _weatherService = weatherService;
            _mapper = mapper;
        }

        public async Task<Result<WeatherDto>> Handle(UpdateWeatherCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var location = await _unitOfWork.Locations.GetByIdAsync(request.LocationId, cancellationToken);

                if (location == null)
                {
                    return Result<WeatherDto>.Failure("Location not found");
                }

                // Check if we have cached weather that's still valid
                var existingWeather = await _unitOfWork.Weather.GetByLocationIdAsync(request.LocationId, cancellationToken);
                if (existingWeather != null && !request.ForceUpdate)
                {
                    var daysSinceUpdate = (DateTime.UtcNow - existingWeather.LastUpdate).TotalDays;
                    if (daysSinceUpdate < 1) // Update at most once per day
                    {
                        var cachedDto = _mapper.Map<WeatherDto>(existingWeather);
                        return Result<WeatherDto>.Success(cachedDto);
                    }
                }

                // Fetch new weather data from OpenWeatherMap API
                var weatherResult = await _weatherService.GetWeatherAsync(
                    location.Coordinate.Latitude,
                    location.Coordinate.Longitude,
                    cancellationToken);

                if (!weatherResult.IsSuccess || weatherResult.Data == null)
                {
                    return Result<WeatherDto>.Failure("Failed to fetch weather data");
                }

                return weatherResult;
            }
            catch (Exception ex)
            {
                return Result<WeatherDto>.Failure($"Failed to update weather: {ex.Message}");
            }
        }
    }
}