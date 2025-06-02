// Location.Photography.Domain/Services/ISunCalculatorService.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Location.Photography.Domain.Services
{
    /// <summary>
    /// Service for calculating sun positions and times using astronomical algorithms
    /// </summary>
    public interface ISunCalculatorService
    {
        /// <summary>
        /// Gets the sunrise time for a specific date and location (returns UTC)
        /// </summary>
        DateTime GetSunrise(DateTime date, double latitude, double longitude, string timezone);
        DateTime GetSunriseEnd(DateTime date, double latitude, double longitude, string timezone);
        DateTime GetSunsetStart(DateTime date, double latitude, double longitude, string timezone);
        /// <summary>
        /// Gets the sunset time for a specific date and location (returns UTC)
        /// </summary>
        DateTime GetSunset(DateTime date, double latitude, double longitude, string timezone);

        /// <summary>
        /// Gets the solar noon time for a specific date and location (returns UTC)
        /// </summary>
        DateTime GetSolarNoon(DateTime date, double latitude, double longitude, string timezone);

        /// <summary>
        /// Gets the solar nadir time (opposite of solar noon) for a specific date and location (returns UTC)
        /// </summary>
        DateTime GetNadir(DateTime date, double latitude, double longitude, string timezone);

        /// <summary>
        /// Gets the civil dawn time for a specific date and location (returns UTC)
        /// </summary>
        DateTime GetCivilDawn(DateTime date, double latitude, double longitude, string timezone);

        /// <summary>
        /// Gets the civil dusk time for a specific date and location (returns UTC)
        /// </summary>
        DateTime GetCivilDusk(DateTime date, double latitude, double longitude, string timezone);

        /// <summary>
        /// Gets the nautical dawn time for a specific date and location (returns UTC)
        /// </summary>
        DateTime GetNauticalDawn(DateTime date, double latitude, double longitude, string timezone);

        /// <summary>
        /// Gets the nautical dusk time for a specific date and location (returns UTC)
        /// </summary>
        DateTime GetNauticalDusk(DateTime date, double latitude, double longitude, string timezone);

        /// <summary>
        /// Gets the astronomical dawn time for a specific date and location (returns UTC)
        /// </summary>
        DateTime GetAstronomicalDawn(DateTime date, double latitude, double longitude, string timezone);

        /// <summary>
        /// Gets the astronomical dusk time for a specific date and location (returns UTC)
        /// </summary>
        DateTime GetAstronomicalDusk(DateTime date, double latitude, double longitude, string timezone);

        /// <summary>
        /// Gets the golden hour start time for a specific date and location (returns UTC)
        /// </summary>
        DateTime GetGoldenHour(DateTime date, double latitude, double longitude, string timezone);

        /// <summary>
        /// Gets the golden hour end time for a specific date and location (returns UTC)
        /// </summary>
        DateTime GetGoldenHourEnd(DateTime date, double latitude, double longitude, string timezone);

        /// <summary>
        /// Gets the night end time (end of astronomical night) for a specific date and location (returns UTC)
        /// </summary>
        DateTime GetNightEnd(DateTime date, double latitude, double longitude, string timezone);

        /// <summary>
        /// Gets the night start time (beginning of astronomical night) for a specific date and location (returns UTC)
        /// </summary>
        DateTime GetNight(DateTime date, double latitude, double longitude, string timezone);

        /// <summary>
        /// Gets the solar azimuth angle for a specific date, time and location (in degrees)
        /// </summary>
        double GetSolarAzimuth(DateTime dateTime, double latitude, double longitude, string timezone);

        /// <summary>
        /// Gets the solar elevation angle for a specific date, time and location (in degrees)
        /// </summary>
        double GetSolarElevation(DateTime dateTime, double latitude, double longitude, string timezone);

        /// <summary>
        /// Batch calculation method for multiple sun times to improve performance
        /// </summary>
        Task<Dictionary<string, DateTime>> GetBatchSunTimesAsync(
            DateTime date,
            double latitude,
            double longitude,
            string timezone,
            params string[] requestedTimes);

        /// <summary>
        /// Cleanup expired cache entries to prevent memory leaks
        /// </summary>
        void CleanupExpiredCache();

        /// <summary>
        /// Preload sun calculations for a date range to improve performance for bulk operations
        /// </summary>
        Task PreloadSunCalculationsAsync(
            DateTime startDate,
            DateTime endDate,
            double latitude,
            double longitude,
            string timezone);
    }
}