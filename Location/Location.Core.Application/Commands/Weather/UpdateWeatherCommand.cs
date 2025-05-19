using AutoMapper;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Services;
using Location.Core.Application.Weather.DTOs;
using MediatR;

namespace Location.Core.Application.Commands.Weather
{
    /// <summary>
    /// Represents a command to update weather information for a specific location.
    /// </summary>
    /// <remarks>This command is used to request an update of weather data for a given location.  The update
    /// can be forced regardless of existing data by setting the <see cref="ForceUpdate"/> property to <see
    /// langword="true"/>.</remarks>
    public class UpdateWeatherCommand : IRequest<Result<WeatherDto>>
    {
        public int LocationId { get; set; }
        public bool ForceUpdate { get; set; }
    }
    /// <summary>
    /// Handles the execution of the <see cref="UpdateWeatherCommand"/> to update weather information for a specific
    /// location.
    /// </summary>
    /// <remarks>This handler retrieves the location by its identifier, checks for cached weather data, and
    /// optionally fetches updated weather information from an external weather service. If cached data is available and
    /// still valid, it is returned unless the <see cref="UpdateWeatherCommand.ForceUpdate"/> flag is set to <see
    /// langword="true"/>.</remarks>
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
        /// <summary>
        /// Handles the update of weather data for a specified location.
        /// </summary>
        /// <remarks>This method retrieves weather data for the specified location. If cached weather data
        /// exists and is still valid, it returns the cached data unless the <see
        /// cref="UpdateWeatherCommand.ForceUpdate"/> flag is set to true. Otherwise, it fetches new weather data from
        /// an external weather service.</remarks>
        /// <param name="request">The command containing the location ID and update options.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A <see cref="Result{T}"/> containing a <see cref="WeatherDto"/> with the updated weather data if successful;
        /// otherwise, a failure result with an error message.</returns>
        public async Task<Result<WeatherDto>> Handle(UpdateWeatherCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var locationResult = await _unitOfWork.Locations.GetByIdAsync(request.LocationId, cancellationToken);

                if (!locationResult.IsSuccess || locationResult.Data == null)
                {
                    return Result<WeatherDto>.Failure("Location not found");
                }

                var location = locationResult.Data;

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