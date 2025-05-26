using AutoMapper;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Events.Errors;
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
    /// <remarks>This handler retrieves the location by its identifier, fetches updated weather information 
    /// from an external weather service, and persists it to the local database for offline-first capability.</remarks>
    public class UpdateWeatherCommandHandler : IRequestHandler<UpdateWeatherCommand, Result<WeatherDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWeatherService _weatherService;
        private readonly IMapper _mapper;
        private readonly IMediator _mediator;

        public UpdateWeatherCommandHandler(
            IUnitOfWork unitOfWork,
            IWeatherService weatherService,
            IMapper mapper,
            IMediator mediator)
        {
            _unitOfWork = unitOfWork;
            _weatherService = weatherService;
            _mapper = mapper;
            _mediator = mediator;
        }
        /// <summary>
        /// Handles the update of weather data for a specified location.
        /// </summary>
        /// <remarks>This method fetches fresh weather data from the external API and persists it to the local 
        /// database, ensuring offline-first capability by always storing data locally.</remarks>
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
                    await _mediator.Publish(new WeatherUpdateErrorEvent(request.LocationId, WeatherErrorType.InvalidLocation, "Location not found"), cancellationToken);
                    return Result<WeatherDto>.Failure("Location not found");
                }

                // Fetch new weather data from API and persist to database
                var weatherResult = await _weatherService.UpdateWeatherForLocationAsync(
                    request.LocationId,
                    cancellationToken);

                if (!weatherResult.IsSuccess || weatherResult.Data == null)
                {
                    var errorType = weatherResult.ErrorMessage?.Contains("API") == true
                        ? WeatherErrorType.ApiUnavailable
                        : WeatherErrorType.NetworkTimeout;

                    await _mediator.Publish(new WeatherUpdateErrorEvent(request.LocationId, errorType, weatherResult.ErrorMessage), cancellationToken);
                    return Result<WeatherDto>.Failure("Failed to fetch and persist weather data");
                }

                return weatherResult;
            }
            catch (TimeoutException ex)
            {
                await _mediator.Publish(new WeatherUpdateErrorEvent(request.LocationId, WeatherErrorType.NetworkTimeout, ex.Message), cancellationToken);
                return Result<WeatherDto>.Failure($"Weather service timeout: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                await _mediator.Publish(new WeatherUpdateErrorEvent(request.LocationId, WeatherErrorType.InvalidApiKey, ex.Message), cancellationToken);
                return Result<WeatherDto>.Failure("Weather service authentication failed");
            }
            catch (Exception ex)
            {
                await _mediator.Publish(new WeatherUpdateErrorEvent(request.LocationId, WeatherErrorType.ApiUnavailable, ex.Message), cancellationToken);
                return Result<WeatherDto>.Failure($"Failed to update weather: {ex.Message}");
            }
        }
    }
}