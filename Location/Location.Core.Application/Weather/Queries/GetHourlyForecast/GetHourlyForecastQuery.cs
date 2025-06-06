using AutoMapper;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Weather.DTOs;
using MediatR;

namespace Location.Core.Application.Weather.Queries.GetHourlyForecast
{
    /// <summary>
    /// Represents a query to retrieve hourly weather forecast data for a specific location.
    /// </summary>
    /// <remarks>This query is used to request hourly weather forecast information based on location ID.
    /// The result includes 48 hours of forecast data if available.</remarks>
    public class GetHourlyForecastQuery : IRequest<Result<HourlyWeatherForecastDto>>
    {
        public int LocationId { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
    }

    /// <summary>
    /// Handles the query to retrieve hourly weather forecast for a specified location.
    /// </summary>
    /// <remarks>This handler processes a <see cref="GetHourlyForecastQuery"/> request by retrieving
    /// weather data from the database and transforming wind direction based on user preferences.</remarks>
    public class GetHourlyForecastQueryHandler : IRequestHandler<GetHourlyForecastQuery, Result<HourlyWeatherForecastDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetHourlyForecastQueryHandler"/> class.
        /// </summary>
        /// <param name="unitOfWork">The unit of work used to access weather data and user settings. This parameter cannot be <see langword="null"/>.</param>
        /// <param name="mapper">The mapper used to transform weather forecast data into the desired output format. This parameter cannot be
        /// <see langword="null"/>.</param>
        public GetHourlyForecastQueryHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        /// <summary>
        /// Handles the request to retrieve hourly weather forecast for a specified location.
        /// </summary>
        /// <param name="request">The query containing the location ID and optional time range filters.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a <see cref="Result{T}"/> object
        /// with the hourly weather forecast data if successful, or an error message if the operation fails.</returns>
        public async Task<Result<HourlyWeatherForecastDto>> Handle(GetHourlyForecastQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var weather = await _unitOfWork.Weather.GetByLocationIdAsync(request.LocationId, cancellationToken);

                if (weather == null)
                {
                    return Result<HourlyWeatherForecastDto>.Failure("Weather data not found for location");
                }

                var hourlyForecasts = weather.HourlyForecasts.AsEnumerable();

                // Apply time range filters if provided
                if (request.StartTime.HasValue)
                {
                    hourlyForecasts = hourlyForecasts.Where(h => h.DateTime >= request.StartTime.Value);
                }

                if (request.EndTime.HasValue)
                {
                    hourlyForecasts = hourlyForecasts.Where(h => h.DateTime <= request.EndTime.Value);
                }

                var hourlyForecastDtos = new List<HourlyForecastDto>();

                foreach (var hourlyForecast in hourlyForecasts)
                {
                    var dto = new HourlyForecastDto
                    {
                        DateTime = hourlyForecast.DateTime,
                        Temperature = hourlyForecast.Temperature,
                        FeelsLike = hourlyForecast.FeelsLike,
                        Description = hourlyForecast.Description,
                        Icon = hourlyForecast.Icon,
                        WindSpeed = hourlyForecast.Wind.Speed,
                        WindDirection = await GetDisplayWindDirectionAsync(hourlyForecast.Wind.Direction, cancellationToken),
                        WindGust = hourlyForecast.Wind.Gust,
                        Humidity = hourlyForecast.Humidity,
                        Pressure = hourlyForecast.Pressure,
                        Clouds = hourlyForecast.Clouds,
                        UvIndex = hourlyForecast.UvIndex,
                        ProbabilityOfPrecipitation = hourlyForecast.ProbabilityOfPrecipitation,
                        Visibility = hourlyForecast.Visibility,
                        DewPoint = hourlyForecast.DewPoint
                    };

                    hourlyForecastDtos.Add(dto);
                }

                var result = new HourlyWeatherForecastDto
                {
                    WeatherId = weather.Id,
                    LastUpdate = weather.LastUpdate,
                    Timezone = weather.Timezone,
                    TimezoneOffset = weather.TimezoneOffset,
                    HourlyForecasts = hourlyForecastDtos
                };

                return Result<HourlyWeatherForecastDto>.Success(result);
            }
            catch (Exception ex)
            {
                return Result<HourlyWeatherForecastDto>.Failure($"Failed to retrieve hourly weather forecast: {ex.Message}");
            }
        }

        /// <summary>
        /// Transforms wind direction based on user's wind direction preference setting.
        /// </summary>
        /// <param name="rawDirection">The raw wind direction from stored data.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>The wind direction adjusted for user preference.</returns>
        private async Task<double> GetDisplayWindDirectionAsync(double rawDirection, CancellationToken cancellationToken)
        {
            try
            {
                var windDirectionSetting = await _unitOfWork.Settings.GetByKeyAsync("WindDirection", cancellationToken);

                if (windDirectionSetting.IsSuccess && windDirectionSetting.Data?.Value == "towardsWind")
                {
                    // Inverse the direction (add 180 degrees, wrap around)
                    return (rawDirection + 180) % 360;
                }

                // Default: withWind (use raw direction)
                return rawDirection;
            }
            catch (Exception)
            {
                // If we can't get the setting, return the raw direction unchanged
                return rawDirection;
            }
        }
    }
}