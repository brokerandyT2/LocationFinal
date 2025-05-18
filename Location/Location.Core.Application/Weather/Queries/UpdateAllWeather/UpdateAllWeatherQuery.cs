using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Services;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Core.Application.Weather.Queries.UpdateAllWeather
{
    public class UpdateAllWeatherQuery : IRequest<Result<int>>
    {
    }

    public class UpdateAllWeatherQueryHandler : IRequestHandler<UpdateAllWeatherQuery, Result<int>>
    {
        private readonly IWeatherService _weatherService;

        public UpdateAllWeatherQueryHandler(IWeatherService weatherService)
        {
            _weatherService = weatherService ?? throw new ArgumentNullException(nameof(weatherService));
        }



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