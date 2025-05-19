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
    /// <summary>
    /// Represents a query to retrieve weather forecast data for a specific location and time period.
    /// </summary>
    /// <remarks>This query is used to request weather forecast information based on geographic coordinates 
    /// (latitude and longitude) and the number of days for which the forecast is required.  The default forecast period
    /// is 7 days if not explicitly specified.</remarks>
    public class GetWeatherForecastQuery : IRequest<Result<WeatherForecastDto>>
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int Days { get; set; } = 7;
    }
    /// <summary>
    /// Handles the query to retrieve a weather forecast for a specified location and time period.
    /// </summary>
    /// <remarks>This handler processes a <see cref="GetWeatherForecastQuery"/> request by invoking the
    /// weather service to fetch forecast data for the specified latitude, longitude, and number of days. The result is
    /// returned as a <see cref="Result{T}"/> containing a <see cref="WeatherForecastDto"/> object if successful, or an
    /// error message if the operation fails.</remarks>
    public class GetWeatherForecastQueryHandler : IRequestHandler<GetWeatherForecastQuery, Result<WeatherForecastDto>>
    {
        private readonly IWeatherService _weatherService;
        private readonly IMapper _mapper;
        /// <summary>
        /// Initializes a new instance of the <see cref="GetWeatherForecastQueryHandler"/> class.
        /// </summary>
        /// <param name="weatherService">The weather service used to retrieve weather forecast data. This parameter cannot be <see langword="null"/>.</param>
        /// <param name="mapper">The mapper used to transform weather forecast data into the desired output format. This parameter cannot be
        /// <see langword="null"/>.</param>
        public GetWeatherForecastQueryHandler(
            IWeatherService weatherService,
            IMapper mapper)
        {
            _weatherService = weatherService;
            _mapper = mapper;
        }
        /// <summary>
        /// Handles the request to retrieve a weather forecast for a specified location and time period.
        /// </summary>
        /// <param name="request">The query containing the latitude, longitude, and number of days for the forecast.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a <see cref="Result{T}"/> object
        /// with the weather forecast data if successful, or an error message if the operation fails.</returns>
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