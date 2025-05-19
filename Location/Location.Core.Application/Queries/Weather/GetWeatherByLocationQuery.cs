
using AutoMapper;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Weather.DTOs;
using MediatR;
using System;
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
    /// containing weather data for the specified location. If no weather data is found, the result will indicate
    /// failure.</remarks>
    public class GetWeatherByLocationQueryHandler : IRequestHandler<GetWeatherByLocationQuery, Result<WeatherDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        /// <summary>
        /// Handles queries to retrieve weather information for a specific location.
        /// </summary>
        /// <remarks>This class is responsible for processing queries related to weather data by utilizing
        /// the provided unit of work for data access and the mapper for data transformation.</remarks>
        /// <param name="unitOfWork">The unit of work used to access the data layer. This parameter cannot be null.</param>
        /// <param name="mapper">The mapper used to transform data entities into query result objects. This parameter cannot be null.</param>
        public GetWeatherByLocationQueryHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }
/// <summary>
/// Handles the retrieval of weather data for a specified location.
/// </summary>
/// <remarks>This method attempts to retrieve weather data for the specified location. If no data is found, a
/// failure result is returned. In the event of an exception, the failure result will include the exception
/// message.</remarks>
/// <param name="request">The query containing the location identifier for which to retrieve weather data.</param>
/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
/// <returns>A <see cref="Result{T}"/> containing a <see cref="WeatherDto"/> if the weather data is successfully retrieved;
/// otherwise, a failure result with an appropriate error message.</returns>
        public async Task<Result<WeatherDto>> Handle(GetWeatherByLocationQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var weather = await _unitOfWork.Weather.GetByLocationIdAsync(request.LocationId, cancellationToken);

                if (weather == null)
                {
                    return Result<WeatherDto>.Failure("Weather data not found for this location");
                }

                var weatherDto = _mapper.Map<WeatherDto>(weather);
                return Result<WeatherDto>.Success(weatherDto);
            }
            catch (Exception ex)
            {
                return Result<WeatherDto>.Failure($"Failed to retrieve weather data: {ex.Message}");
            }
        }
    }
}
