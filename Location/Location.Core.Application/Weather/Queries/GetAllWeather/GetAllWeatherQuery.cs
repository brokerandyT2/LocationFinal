using AutoMapper;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Weather.DTOs;
using MediatR;

namespace Location.Core.Application.Weather.Queries.GetAllWeather
{
    /// <summary>
    /// Represents a query to retrieve all weather data, optionally including expired entries.
    /// </summary>
    /// <remarks>This query is used to request a list of weather data records. The result includes weather
    /// data in the form of <see cref="WeatherDto"/> objects. By default, expired entries are excluded unless explicitly
    /// specified.</remarks>
    public class GetAllWeatherQuery : IRequest<Result<List<WeatherDto>>>
    {
        public bool IncludeExpired { get; set; } = false;
    }
    /// <summary>
    /// Handles the query to retrieve a list of weather data.
    /// </summary>
    /// <remarks>This handler processes the <see cref="GetAllWeatherQuery"/> to fetch weather data from the
    /// data source. The result can include expired weather data based on the <see
    /// cref="GetAllWeatherQuery.IncludeExpired"/> property.</remarks>
    public class GetAllWeatherQueryHandler : IRequestHandler<GetAllWeatherQuery, Result<List<WeatherDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        /// <summary>
        /// Initializes a new instance of the <see cref="GetAllWeatherQueryHandler"/> class.
        /// </summary>
        /// <param name="unitOfWork">The unit of work used to interact with the data layer. This parameter cannot be null.</param>
        /// <param name="mapper">The mapper used to transform data models into DTOs or other representations. This parameter cannot be null.</param>
        public GetAllWeatherQueryHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }
        /// <summary>
        /// Handles the retrieval of weather data based on the specified query parameters.
        /// </summary>
        /// <remarks>If <see cref="GetAllWeatherQuery.IncludeExpired"/> is set to <see langword="true"/>, all recent
        /// weather data is retrieved. Otherwise, only the 10 most recent entries are included.</remarks>
        /// <param name="request">The query containing parameters for retrieving weather data, including whether to include expired entries.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A <see cref="Result{T}"/> containing a list of <see cref="WeatherDto"/> objects representing the retrieved weather
        /// data. If the operation fails, the result contains an error message.</returns>
        public async Task<Result<List<WeatherDto>>> Handle(GetAllWeatherQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var weatherList = request.IncludeExpired
                    ? await _unitOfWork.Weather.GetRecentAsync(int.MaxValue, cancellationToken)
                    : await _unitOfWork.Weather.GetRecentAsync(10, cancellationToken);

                var weatherDtos = weatherList.Select(w => _mapper.Map<WeatherDto>(w)).ToList();

                return Result<List<WeatherDto>>.Success(weatherDtos);
            }
            catch (Exception ex)
            {
                return Result<List<WeatherDto>>.Failure($"Failed to retrieve weather data: {ex.Message}");
            }
        }
    }
}