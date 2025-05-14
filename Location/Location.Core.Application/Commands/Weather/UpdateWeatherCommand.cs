using AutoMapper;
using Location.Core.Application.Common;
using Location.Core.Application.Common.Interfaces.Persistence;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.DTOs;
using Location.Core.Application.Interfaces;
using Location.Core.Application.Services;
using Location.Core.Application.Weather.DTOs;
using MediatR;

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
        private readonly ILocationRepository _locationRepository;
        private readonly IWeatherRepository _weatherRepository;
        private readonly IMapper _mapper;

        public UpdateWeatherCommandHandler(
            IUnitOfWork unitOfWork,
            IWeatherService weatherService,
            ILocationRepository locationRepository,
            IWeatherRepository weatherRepository,
            IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _weatherService = weatherService;
            _locationRepository = locationRepository;
            _weatherRepository = weatherRepository;
            _mapper = mapper;
        }

        public async Task<Result<WeatherDto>> Handle(UpdateWeatherCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var location = await _locationRepository.GetByIdAsync(request.LocationId, cancellationToken);
                if (location == null)
                {
                    return Result<WeatherDto>.Failure("Location not found");
                }

                // Check if we have cached weather that's still valid (7-day cache as per memory)
                var existingWeather = await _weatherRepository.GetByLocationIdAsync(request.LocationId, cancellationToken);
                if (existingWeather != null && !request.ForceUpdate)
                {
                    var daysSinceUpdate = (DateTime.UtcNow - existingWeather.LastUpdated).TotalDays;
                    if (daysSinceUpdate < 1) // Update at most once per day
                    {
                        var cachedDto = _mapper.Map<WeatherDto>(existingWeather);
                        return Result<WeatherDto>.Success(cachedDto);
                    }
                }

                // Fetch new weather data from OpenWeatherMap One Call 3.0
                var weatherData = await _weatherService.GetWeatherAsync(
                    location.Latitude,
                    location.Longitude,
                    cancellationToken);

                if (weatherData == null)
                {
                    return Result<WeatherDto>.Failure("Failed to fetch weather data");
                }

                // Update or create weather entity
                if (existingWeather != null)
                {
                    existingWeather.UpdateWeatherData(weatherData);
                }
                else
                {
                    var newWeather = new Domain.Entities.Weather(
                        request.LocationId,
                        weatherData);
                    await _weatherRepository.AddAsync(newWeather, cancellationToken);
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                var weatherDto = _mapper.Map<WeatherDto>(existingWeather ?? weatherData);
                return Result<WeatherDto>.Success(weatherDto);
            }
            catch (Exception ex)
            {
                return Result<WeatherDto>.Failure($"Failed to update weather: {ex.Message}");
            }
        }
    }
}