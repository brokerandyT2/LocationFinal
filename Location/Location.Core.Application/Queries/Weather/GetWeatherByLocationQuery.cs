using AutoMapper;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Weather.DTOs;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Core.Application.Queries.Weather
{
    /// <summary>
    /// 
    /// </summary>
    public class GetWeatherByLocationQuery : IRequest<Result<WeatherDto>>
    {
        public int LocationId { get; set; }
    }
    /// <summary>
    /// Handles queries to retrieve weather data for a specific location.
    /// </summary>
    /// <remarks>This handler processes a <see cref="GetWeatherByLocationQuery"/> and returns a result
    /// containing weather data for the specified location. It checks for complete forecast data (today + next 4 days)
    /// and triggers an update if data is incomplete.</remarks>
    public class GetWeatherByLocationQueryHandler : IRequestHandler<GetWeatherByLocationQuery, Result<WeatherDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IMediator _mediator;

        /// <summary>
        /// Handles queries to retrieve weather information for a specific location.
        /// </summary>
        /// <remarks>This class is responsible for processing queries related to weather data by utilizing
        /// the provided unit of work for data access and the mapper for data transformation.</remarks>
        /// <param name="unitOfWork">The unit of work used to access the data layer. This parameter cannot be null.</param>
        /// <param name="mapper">The mapper used to transform data entities into query result objects. This parameter cannot be null.</param>
        /// <param name="mediator">The mediator used to send commands for data updates. This parameter cannot be null.</param>
        public GetWeatherByLocationQueryHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IMediator mediator)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _mediator = mediator;
        }

        /// <summary>
        /// Handles the retrieval of weather data for a specified location.
        /// </summary>
        /// <remarks>This method checks if complete forecast data exists (today + next 4 days). If incomplete,
        /// it triggers an update command to fetch fresh data from the API.</remarks>
        /// <param name="request">The query containing the location identifier for which to retrieve weather data.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A <see cref="Result{T}"/> containing a <see cref="WeatherDto"/> if the weather data is successfully retrieved;
        /// otherwise, a failure result with an appropriate error message.</returns>
        public async Task<Result<WeatherDto>> Handle(GetWeatherByLocationQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var weather = await _unitOfWork.Weather.GetByLocationIdAsync(request.LocationId, cancellationToken);

                // Check if we have complete forecast data (today + next 4 days)
                if (weather != null && HasCompleteForecastData(weather))
                {
                    var weatherDto = _mapper.Map<WeatherDto>(weather);
                    return Result<WeatherDto>.Success(weatherDto);
                }

                // Data is incomplete, trigger update command
                var updateCommand = new Commands.Weather.UpdateWeatherCommand
                {
                    LocationId = request.LocationId,
                    ForceUpdate = true
                };

                var updateResult = await _mediator.Send(updateCommand, cancellationToken);

                if (!updateResult.IsSuccess)
                {
                    return Result<WeatherDto>.Failure($"Failed to update weather data: {updateResult.ErrorMessage}");
                }

                return updateResult;
            }
            catch (Exception ex)
            {
                return Result<WeatherDto>.Failure($"Failed to retrieve weather data: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks if the weather entity has complete forecast data for today and the next 4 days.
        /// </summary>
        /// <param name="weather">The weather entity to check.</param>
        /// <returns>True if forecast data exists for today + next 4 days; otherwise, false.</returns>
        private bool HasCompleteForecastData(Domain.Entities.Weather weather)
        {
            if (weather.Forecasts == null || weather.Forecasts.Count == 0)
                return false;

            var today = DateTime.Today;
            var requiredDates = Enumerable.Range(0, 5)
                .Select(i => today.AddDays(i))
                .ToList();

            // Check if we have forecast data for all required dates
            foreach (var requiredDate in requiredDates)
            {
                if (!weather.Forecasts.Any(f => f.Date.Date == requiredDate.Date))
                {
                    return false;
                }
            }

            return true;
        }
    }
}