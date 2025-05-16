using Location.Photography.Application.Common.Models;
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

    public class SunPositionDto
    {
        public double Azimuth { get; set; }
        public double Elevation { get; set; }
        public DateTime DateTime { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class SunTimesDto
    {
        public DateTime Date { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime Sunrise { get; set; }
        public DateTime Sunset { get; set; }
        public DateTime SolarNoon { get; set; }
        public DateTime AstronomicalDawn { get; set; }
        public DateTime AstronomicalDusk { get; set; }
        public DateTime NauticalDawn { get; set; }
        public DateTime NauticalDusk { get; set; }
        public DateTime CivilDawn { get; set; }
        public DateTime CivilDusk { get; set; }
        public DateTime GoldenHourMorningStart { get; set; }
        public DateTime GoldenHourMorningEnd { get; set; }
        public DateTime GoldenHourEveningStart { get; set; }
        public DateTime GoldenHourEveningEnd { get; set; }
    }
}