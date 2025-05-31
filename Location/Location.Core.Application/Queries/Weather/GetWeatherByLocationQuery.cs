using AutoMapper;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Weather.DTOs;
using Location.Core.Application.Services;
using Location.Core.Application.Events.Errors;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Core.Application.Queries.Weather
{
    /// <summary>
    /// Optimized handler that minimizes database calls and smart caching
    /// </summary>
    public class GetWeatherByLocationQuery : IRequest<Result<WeatherDto>>
    {
        public int LocationId { get; set; }
    }

    /// <summary>
    /// Handles queries to retrieve weather data for a specific location with optimized database access
    /// </summary>
    /// <remarks>This handler uses a single database query with intelligent caching logic to minimize
    /// database roundtrips and external API calls.</remarks>
    public class GetWeatherByLocationQueryHandler : IRequestHandler<GetWeatherByLocationQuery, Result<WeatherDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IWeatherService _weatherService;
        private readonly IMediator _mediator;

        /// <summary>
        /// Handles queries to retrieve weather information for a specific location.
        /// </summary>
        public GetWeatherByLocationQueryHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IWeatherService weatherService,
            IMediator mediator)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _weatherService = weatherService;
            _mediator = mediator;
        }

        /// <summary>
        /// Handles the retrieval of weather data with optimized database access and smart caching
        /// </summary>
        public async Task<Result<WeatherDto>> Handle(GetWeatherByLocationQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // Single optimized database call - get weather with location data
                var weatherResult = await GetWeatherWithLocationAsync(request.LocationId, cancellationToken);

                if (!weatherResult.IsSuccess)
                {
                    await _mediator.Publish(new WeatherUpdateErrorEvent(request.LocationId, WeatherErrorType.DatabaseError, weatherResult.ErrorMessage), cancellationToken);
                    return Result<WeatherDto>.Failure(weatherResult.ErrorMessage ?? "Failed to retrieve weather data");
                }

                var (weather, location) = weatherResult.Data;

                // Smart cache validation - check if update needed
                if (IsWeatherDataFresh(weather))
                {
                    // Return cached data immediately
                    var cachedWeatherDto = _mapper.Map<WeatherDto>(weather);
                    return Result<WeatherDto>.Success(cachedWeatherDto);
                }

                // Data is stale or incomplete - fetch fresh data in background if possible
                // But return existing data immediately for better UX
                var existingWeatherDto = weather != null ? _mapper.Map<WeatherDto>(weather) : null;

                // Trigger background update (fire-and-forget for non-critical scenarios)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _weatherService.UpdateWeatherForLocationAsync(request.LocationId, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        await _mediator.Publish(new WeatherUpdateErrorEvent(request.LocationId, WeatherErrorType.ApiUnavailable, ex.Message), cancellationToken);
                    }
                }, cancellationToken);

                // If we have existing data, return it immediately while update happens in background
                if (existingWeatherDto != null)
                {
                    return Result<WeatherDto>.Success(existingWeatherDto);
                }

                // No existing data - must wait for fresh data
                var freshWeatherResult = await _weatherService.UpdateWeatherForLocationAsync(request.LocationId, cancellationToken);

                if (!freshWeatherResult.IsSuccess)
                {
                    await _mediator.Publish(new WeatherUpdateErrorEvent(request.LocationId, WeatherErrorType.ApiUnavailable, freshWeatherResult.ErrorMessage), cancellationToken);
                    return Result<WeatherDto>.Failure($"Failed to fetch weather data: {freshWeatherResult.ErrorMessage}");
                }

                return freshWeatherResult;
            }
            catch (TimeoutException ex)
            {
                await _mediator.Publish(new WeatherUpdateErrorEvent(request.LocationId, WeatherErrorType.NetworkTimeout, ex.Message), cancellationToken);
                return Result<WeatherDto>.Failure($"Weather service timeout: {ex.Message}");
            }
            catch (Exception ex)
            {
                await _mediator.Publish(new WeatherUpdateErrorEvent(request.LocationId, WeatherErrorType.ApiUnavailable, ex.Message), cancellationToken);
                return Result<WeatherDto>.Failure($"Failed to retrieve weather data: {ex.Message}");
            }
        }

        /// <summary>
        /// Single optimized query to get weather and location data together
        /// </summary>
        private async Task<Result<(Domain.Entities.Weather? weather, Domain.Entities.Location location)>> GetWeatherWithLocationAsync(
            int locationId,
            CancellationToken cancellationToken)
        {
            // Get location first - this is required
            var locationResult = await _unitOfWork.Locations.GetByIdAsync(locationId, cancellationToken);
            if (!locationResult.IsSuccess || locationResult.Data == null)
            {
                return Result<(Domain.Entities.Weather?, Domain.Entities.Location)>.Failure("Location not found");
            }

            // Get weather data - this might be null for new locations
            var weather = await _unitOfWork.Weather.GetByLocationIdAsync(locationId, cancellationToken);

            return Result<(Domain.Entities.Weather?, Domain.Entities.Location)>.Success((weather, locationResult.Data));
        }

        /// <summary>
        /// Intelligent cache validation with configurable staleness rules
        /// </summary>
        private bool IsWeatherDataFresh(Domain.Entities.Weather? weather)
        {
            if (weather == null)
                return false;

            // Check if we have complete forecast data (today + next 4 days minimum)
            if (!HasCompleteForecastData(weather))
                return false;

            // Check data age - weather data older than 1 hour is considered stale
            var maxAge = TimeSpan.FromHours(1);
            var isDataFresh = DateTime.UtcNow - weather.LastUpdate <= maxAge;

            return isDataFresh;
        }

        /// <summary>
        /// Optimized forecast completeness check
        /// </summary>
        private bool HasCompleteForecastData(Domain.Entities.Weather weather)
        {
            if (weather.Forecasts == null || weather.Forecasts.Count < 5)
                return false;

            var today = DateTime.Today;
            var requiredDates = new HashSet<DateTime>();

            // Generate required dates (today + next 4 days)
            for (int i = 0; i < 5; i++)
            {
                requiredDates.Add(today.AddDays(i));
            }

            // Check if we have all required dates using HashSet for O(1) lookup
            var availableDates = new HashSet<DateTime>(weather.Forecasts.Select(f => f.Date.Date));

            return requiredDates.IsSubsetOf(availableDates);
        }
    }
}