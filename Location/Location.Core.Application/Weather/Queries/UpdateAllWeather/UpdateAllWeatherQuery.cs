using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Services;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Core.Application.Weather.Queries.UpdateAllWeather
{
    /// <summary>
    /// Represents a query to update all weather data.
    /// </summary>
    /// <remarks>This query is used to trigger an update operation for all weather-related data. The result of
    /// the operation indicates the number of records successfully updated.</remarks>
    public class UpdateAllWeatherQuery : IRequest<Result<int>>
    {
    }
    /// <summary>
    /// Handles the execution of the <see cref="UpdateAllWeatherQuery"/> to update all weather data.
    /// </summary>
    /// <remarks>This handler invokes the <see cref="IWeatherService.UpdateAllWeatherAsync"/> method to
    /// perform the update operation. It returns the result of the operation, including the number of updated records or
    /// an error message in case of failure.</remarks>
    public class UpdateAllWeatherQueryHandler : IRequestHandler<UpdateAllWeatherQuery, Result<int>>
    {
        private readonly IWeatherService _weatherService;
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateAllWeatherQueryHandler"/> class.
        /// </summary>
        /// <param name="weatherService">The weather service used to retrieve and update weather data.  This parameter cannot be <see
        /// langword="null"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="weatherService"/> is <see langword="null"/>.</exception>
        public UpdateAllWeatherQueryHandler(IWeatherService weatherService)
        {
            _weatherService = weatherService ?? throw new ArgumentNullException(nameof(weatherService));
        }


        /// <summary>
        /// Handles the request to update all weather data asynchronously.
        /// </summary>
        /// <param name="request">The request object containing the details for the update operation.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A <see cref="Result{T}"/> containing the number of weather records updated if successful,  or a failure
        /// result with an error message if the operation fails.</returns>
        public async Task<Result<int>> Handle(UpdateAllWeatherQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _weatherService.UpdateAllWeatherAsync(cancellationToken);
                return result;
            }
            catch (Exception ex)
            {
                return Result<int>.Failure($"Failed to update all weather data: {ex.Message}");
            }
        }
    }
}