// Location.Photography.Application/Services/ISunService.cs
using Location.Core.Application.Common.Models;
using Location.Photography.Domain.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Photography.Application.Services
{
    public interface ISunService
    {
        /// <summary>
        /// Calculates sun position (azimuth and elevation) for the specified coordinates and date/time
        /// </summary>
        Task<Result<SunPositionDto>> GetSunPositionAsync(double latitude, double longitude, DateTime dateTime, CancellationToken cancellationToken = default);

        /// <summary>
        /// Calculates sun times (sunrise, sunset, dawn, dusk, etc.) for the specified coordinates and date
        /// </summary>
        Task<Result<SunTimesDto>> GetSunTimesAsync(double latitude, double longitude, DateTime date, CancellationToken cancellationToken = default);
    }
}