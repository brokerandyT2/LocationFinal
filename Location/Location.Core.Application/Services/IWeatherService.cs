using System.Threading;
using System.Threading.Tasks;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Weather.DTOs;

namespace Location.Core.Application.Services
{
    /// <summary>
    /// Interface for weather service that integrates with external weather API
    /// </summary>
    public interface IWeatherService
    {
        /// <summary>
        /// Gets current weather for specified coordinates
        /// </summary>
        Task<Result<WeatherDto>> GetWeatherAsync(double latitude, double longitude, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates weather data for a specific location
        /// </summary>
        Task<Result<WeatherDto>> UpdateWeatherForLocationAsync(int locationId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets weather forecast for specified coordinates
        /// </summary>
        Task<Result<WeatherForecastDto>> GetForecastAsync(double latitude, double longitude, int days = 7, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates weather for all active locations
        /// </summary>
        Task<Result<int>> UpdateAllWeatherAsync(CancellationToken cancellationToken = default);
    }
}