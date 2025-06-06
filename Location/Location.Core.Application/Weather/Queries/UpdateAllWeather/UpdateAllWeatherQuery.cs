using Location.Core.Application.Common.Models;
using Location.Core.Application.Events.Errors;
using Location.Core.Application.Services;
using MediatR;

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
        private readonly IMediator _mediator;
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateAllWeatherQueryHandler"/> class.
        /// </summary>
        /// <param name="weatherService">The weather service used to retrieve and update weather data.  This parameter cannot be <see
        /// langword="null"/>.</param>
        /// <param name="mediator">The mediator used to publish domain events.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="weatherService"/> is <see langword="null"/>.</exception>
        public UpdateAllWeatherQueryHandler(IWeatherService weatherService, IMediator mediator)
        {
            _weatherService = weatherService ?? throw new ArgumentNullException(nameof(weatherService));
            _mediator = mediator;
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

                if (!result.IsSuccess)
                {
                    await _mediator.Publish(new WeatherUpdateErrorEvent(0, WeatherErrorType.ApiUnavailable, result.ErrorMessage), cancellationToken);
                }

                return result;
            }
            catch (TimeoutException ex)
            {
                await _mediator.Publish(new WeatherUpdateErrorEvent(0, WeatherErrorType.NetworkTimeout, ex.Message), cancellationToken);
                return Result<int>.Failure($"Weather service timeout: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                await _mediator.Publish(new WeatherUpdateErrorEvent(0, WeatherErrorType.InvalidApiKey, ex.Message), cancellationToken);
                return Result<int>.Failure("Weather service authentication failed");
            }
            catch (Exception ex)
            {
                await _mediator.Publish(new WeatherUpdateErrorEvent(0, WeatherErrorType.ApiUnavailable, ex.Message), cancellationToken);
                return Result<int>.Failure($"Failed to update all weather data: {ex.Message}");
            }
        }
    }
}