// Location.Photography.Domain/Services/ISunCalculatorService.cs
namespace Location.Photography.Domain.Services
{
    /// <summary>
    /// Service for calculating sun, moon, and astronomical data using CoordinateSharp
    /// </summary>
    public interface ISunCalculatorService
    {
        // === SOLAR DATA ===

        /// <summary>
        /// Gets the sunrise time for a specific date and location (returns UTC)
        /// </summary>
        DateTime GetSunrise(DateTime date, double latitude, double longitude, string timezone);

        /// <summary>
        /// Gets the sunrise end time for a specific date and location (returns UTC)
        /// </summary>
        DateTime GetSunriseEnd(DateTime date, double latitude, double longitude, string timezone);

        /// <summary>
        /// Gets the sunset start time for a specific date and location (returns UTC)
        /// </summary>
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
        /// Gets the blue hour start time for a specific date and location (returns UTC)
        /// </summary>
        DateTime GetBlueHourStart(DateTime date, double latitude, double longitude, string timezone);

        /// <summary>
        /// Gets the blue hour end time for a specific date and location (returns UTC)
        /// </summary>
        DateTime GetBlueHourEnd(DateTime date, double latitude, double longitude, string timezone);

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
        /// Gets the distance to the sun in Astronomical Units (AU)
        /// </summary>
        double GetSolarDistance(DateTime dateTime, double latitude, double longitude, string timezone);

        /// <summary>
        /// Gets the current sun condition (Up, Down, CivilTwilight, etc.)
        /// </summary>
        string GetSunCondition(DateTime dateTime, double latitude, double longitude, string timezone);

        // === LUNAR DATA ===

        /// <summary>
        /// Gets the moonrise time for a specific date and location (returns UTC)
        /// </summary>
        DateTime? GetMoonrise(DateTime date, double latitude, double longitude, string timezone);

        /// <summary>
        /// Gets the moonset time for a specific date and location (returns UTC)
        /// </summary>
        DateTime? GetMoonset(DateTime date, double latitude, double longitude, string timezone);

        /// <summary>
        /// Gets the lunar azimuth angle for a specific date, time and location (in degrees)
        /// </summary>
        double GetMoonAzimuth(DateTime dateTime, double latitude, double longitude, string timezone);

        /// <summary>
        /// Gets the lunar elevation angle for a specific date, time and location (in degrees)
        /// </summary>
        double GetMoonElevation(DateTime dateTime, double latitude, double longitude, string timezone);

        /// <summary>
        /// Gets the distance to the moon in kilometers
        /// </summary>
        double GetMoonDistance(DateTime dateTime, double latitude, double longitude, string timezone);

        /// <summary>
        /// Gets the moon illumination fraction (0.0 to 1.0)
        /// </summary>
        double GetMoonIllumination(DateTime dateTime, double latitude, double longitude, string timezone);

        /// <summary>
        /// Gets the moon phase angle in degrees
        /// </summary>
        double GetMoonPhaseAngle(DateTime dateTime, double latitude, double longitude, string timezone);

        /// <summary>
        /// Gets the moon phase name (New Moon, Waxing Crescent, etc.)
        /// </summary>
        string GetMoonPhaseName(DateTime dateTime, double latitude, double longitude, string timezone);

        /// <summary>
        /// Gets the next lunar perigee (closest approach to Earth)
        /// </summary>
        DateTime? GetNextLunarPerigee(DateTime date, double latitude, double longitude, string timezone);

        /// <summary>
        /// Gets the next lunar apogee (farthest point from Earth)
        /// </summary>
        DateTime? GetNextLunarApogee(DateTime date, double latitude, double longitude, string timezone);

        // === ECLIPSE DATA ===

        /// <summary>
        /// Gets the next solar eclipse data
        /// </summary>
        (DateTime? date, string type, bool isVisible) GetNextSolarEclipse(DateTime date, double latitude, double longitude, string timezone);

        /// <summary>
        /// Gets the last solar eclipse data
        /// </summary>
        (DateTime? date, string type, bool isVisible) GetLastSolarEclipse(DateTime date, double latitude, double longitude, string timezone);

        /// <summary>
        /// Gets the next lunar eclipse data
        /// </summary>
        (DateTime? date, string type, bool isVisible) GetNextLunarEclipse(DateTime date, double latitude, double longitude, string timezone);

        /// <summary>
        /// Gets the last lunar eclipse data
        /// </summary>
        (DateTime? date, string type, bool isVisible) GetLastLunarEclipse(DateTime date, double latitude, double longitude, string timezone);

        // === PERFORMANCE METHODS ===

        /// <summary>
        /// Batch calculation method for multiple sun times to improve performance
        /// </summary>
        Task<Dictionary<string, object>> GetBatchAstronomicalDataAsync(
            DateTime date,
            double latitude,
            double longitude,
            string timezone,
            params string[] requestedData);

        /// <summary>
        /// Cleanup expired cache entries to prevent memory leaks
        /// </summary>
        void CleanupExpiredCache();

        /// <summary>
        /// Preload astronomical calculations for a date range to improve performance for bulk operations
        /// </summary>
        Task PreloadAstronomicalCalculationsAsync(
            DateTime startDate,
            DateTime endDate,
            double latitude,
            double longitude,
            string timezone);
    }
}