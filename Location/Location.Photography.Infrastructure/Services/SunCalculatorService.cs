// Location.Photography.Infrastructure/Services/SunCalculatorService.cs
using Location.Photography.Domain.Services;
using SunCalcNet;
using SunCalcNet.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Location.Photography.Infrastructure.Services
{
    public class SunCalculatorService : ISunCalculatorService
    {
        // Constants for conversion
        private const double DegToRad = Math.PI / 180.0;
        private const double RadToDeg = 180.0 / Math.PI;

        // Cache for sun calculations to improve performance and reduce CPU usage
        private readonly ConcurrentDictionary<string, (object result, DateTime expiry)> _calculationCache = new();
        private readonly TimeSpan _cacheTimeout = TimeSpan.FromMinutes(30); // Sun data changes slowly

        public DateTime GetSunrise(DateTime date, double latitude, double longitude, string timezone)
        {
            var cacheKey = $"sunrise_{date:yyyyMMdd}_{latitude:F4}_{longitude:F4}";

            if (_calculationCache.TryGetValue(cacheKey, out var cached) && DateTime.UtcNow < cached.expiry)
            {
                return (DateTime)cached.result;
            }

            var phases = GetSunPhasesWithCaching(date, latitude, longitude);
            var sunrise = phases.FirstOrDefault(p => p.Name.Value.ToLower() == "sunrise");
            var result = sunrise.PhaseTime.ToLocalTime() != default ? sunrise.PhaseTime.ToLocalTime() : date;

            _calculationCache[cacheKey] = (result, DateTime.UtcNow.Add(_cacheTimeout));
            return result;
        }

        public DateTime GetSunset(DateTime date, double latitude, double longitude, string timezone)
        {
            var cacheKey = $"sunset_{date:yyyyMMdd}_{latitude:F4}_{longitude:F4}";

            if (_calculationCache.TryGetValue(cacheKey, out var cached) && DateTime.UtcNow < cached.expiry)
            {
                return (DateTime)cached.result;
            }

            var phases = GetSunPhasesWithCaching(date, latitude, longitude);
            var sunset = phases.FirstOrDefault(p => p.Name.Value.ToLower() == "sunset");
            var result = sunset.PhaseTime.ToLocalTime() != default ? sunset.PhaseTime.ToLocalTime() : date;

            _calculationCache[cacheKey] = (result, DateTime.UtcNow.Add(_cacheTimeout));
            return result;
        }

        public DateTime GetSolarNoon(DateTime date, double latitude, double longitude, string timezone)
        {
            var cacheKey = $"solarnoon_{date:yyyyMMdd}_{latitude:F4}_{longitude:F4}";

            if (_calculationCache.TryGetValue(cacheKey, out var cached) && DateTime.UtcNow < cached.expiry)
            {
                return (DateTime)cached.result;
            }

            var phases = GetSunPhasesWithCaching(date, latitude, longitude);
            var solarNoon = phases.FirstOrDefault(p => p.Name.Value.ToLower() == "solar noon");
            var result = solarNoon.PhaseTime != default ? solarNoon.PhaseTime.ToLocalTime() : date.Date.AddHours(12);

            _calculationCache[cacheKey] = (result, DateTime.UtcNow.Add(_cacheTimeout));
            return result;
        }

        public DateTime GetCivilDawn(DateTime date, double latitude, double longitude, string timezone)
        {
            var cacheKey = $"civildawn_{date:yyyyMMdd}_{latitude:F4}_{longitude:F4}";

            if (_calculationCache.TryGetValue(cacheKey, out var cached) && DateTime.UtcNow < cached.expiry)
            {
                return (DateTime)cached.result;
            }

            var phases = GetSunPhasesWithCaching(date, latitude, longitude);
            var dawn = phases.FirstOrDefault(p => p.Name.Value.ToLower() == "dawn");
            var result = dawn.PhaseTime.ToLocalTime() != default ? dawn.PhaseTime.ToLocalTime() : date;

            _calculationCache[cacheKey] = (result, DateTime.UtcNow.Add(_cacheTimeout));
            return result;
        }

        public DateTime GetCivilDusk(DateTime date, double latitude, double longitude, string timezone)
        {
            var cacheKey = $"civildusk_{date:yyyyMMdd}_{latitude:F4}_{longitude:F4}";

            if (_calculationCache.TryGetValue(cacheKey, out var cached) && DateTime.UtcNow < cached.expiry)
            {
                return (DateTime)cached.result;
            }

            var phases = GetSunPhasesWithCaching(date, latitude, longitude);
            var dusk = phases.FirstOrDefault(p => p.Name.Value.ToLower() == "dusk");
            var result = dusk.PhaseTime.ToLocalTime() != default ? dusk.PhaseTime.ToLocalTime() : date;

            _calculationCache[cacheKey] = (result, DateTime.UtcNow.Add(_cacheTimeout));
            return result;
        }

        public DateTime GetNauticalDawn(DateTime date, double latitude, double longitude, string timezone)
        {
            var cacheKey = $"nauticaldawn_{date:yyyyMMdd}_{latitude:F4}_{longitude:F4}";

            if (_calculationCache.TryGetValue(cacheKey, out var cached) && DateTime.UtcNow < cached.expiry)
            {
                return (DateTime)cached.result;
            }

            var phases = GetSunPhasesWithCaching(date, latitude, longitude);
            var nauticalDawn = phases.FirstOrDefault(p => p.Name.Value.ToLower() == "nautical dawn");
            var result = nauticalDawn.PhaseTime.ToLocalTime() != default ? nauticalDawn.PhaseTime.ToLocalTime() : date;

            _calculationCache[cacheKey] = (result, DateTime.UtcNow.Add(_cacheTimeout));
            return result;
        }

        public DateTime GetNauticalDusk(DateTime date, double latitude, double longitude, string timezone)
        {
            var cacheKey = $"nauticaldusk_{date:yyyyMMdd}_{latitude:F4}_{longitude:F4}";

            if (_calculationCache.TryGetValue(cacheKey, out var cached) && DateTime.UtcNow < cached.expiry)
            {
                return (DateTime)cached.result;
            }

            var phases = GetSunPhasesWithCaching(date, latitude, longitude);
            var nauticalDusk = phases.FirstOrDefault(p => p.Name.Value.ToLower() == "nautical dusk");
            var result = nauticalDusk.PhaseTime.ToLocalTime() != default ? nauticalDusk.PhaseTime.ToLocalTime() : date;

            _calculationCache[cacheKey] = (result, DateTime.UtcNow.Add(_cacheTimeout));
            return result;
        }

        public DateTime GetAstronomicalDawn(DateTime date, double latitude, double longitude, string timezone)
        {
            var cacheKey = $"astrodawn_{date:yyyyMMdd}_{latitude:F4}_{longitude:F4}";

            if (_calculationCache.TryGetValue(cacheKey, out var cached) && DateTime.UtcNow < cached.expiry)
            {
                return (DateTime)cached.result;
            }

            var phases = GetSunPhasesWithCaching(date, latitude, longitude);
            var nightEnd = phases.FirstOrDefault(p => p.Name.Value.ToLower() == "sunrise");
            var result = nightEnd.PhaseTime.AddHours(-2).ToLocalTime() != default ? nightEnd.PhaseTime.AddHours(-2).ToLocalTime() : date;

            _calculationCache[cacheKey] = (result, DateTime.UtcNow.Add(_cacheTimeout));
            return result;
        }

        public DateTime GetAstronomicalDusk(DateTime date, double latitude, double longitude, string timezone)
        {
            var cacheKey = $"astrodusk_{date:yyyyMMdd}_{latitude:F4}_{longitude:F4}";

            if (_calculationCache.TryGetValue(cacheKey, out var cached) && DateTime.UtcNow < cached.expiry)
            {
                return (DateTime)cached.result;
            }

            var phases = GetSunPhasesWithCaching(date, latitude, longitude);
            var night = phases.FirstOrDefault(p => p.Name.Value == "sunset");
            var result = night.PhaseTime.AddHours(2).ToLocalTime() != default ? night.PhaseTime.AddHours(2).ToLocalTime() : date;

            _calculationCache[cacheKey] = (result, DateTime.UtcNow.Add(_cacheTimeout));
            return result;
        }

        public double GetSolarAzimuth(DateTime dateTime, double latitude, double longitude, string timezone)
        {
            var cacheKey = $"azimuth_{dateTime:yyyyMMddHHmm}_{latitude:F4}_{longitude:F4}";

            if (_calculationCache.TryGetValue(cacheKey, out var cached) && DateTime.UtcNow < cached.expiry)
            {
                return (double)cached.result;
            }

            var position = GetSunPositionWithCaching(dateTime, latitude, longitude);

            // Convert from radians to degrees and adjust from [-π, π] to [0, 360]
            double azimuthDegrees = (position.Azimuth * RadToDeg) + 180.0;

            // Ensure azimuth is in range [0, 360)
            while (azimuthDegrees >= 360.0)
                azimuthDegrees -= 360.0;
            while (azimuthDegrees < 0.0)
                azimuthDegrees += 360.0;

            _calculationCache[cacheKey] = (azimuthDegrees, DateTime.UtcNow.Add(_cacheTimeout));
            return azimuthDegrees;
        }

        public double GetSolarElevation(DateTime dateTime, double latitude, double longitude, string timezone)
        {
            var cacheKey = $"elevation_{dateTime:yyyyMMddHHmm}_{latitude:F4}_{longitude:F4}";

            if (_calculationCache.TryGetValue(cacheKey, out var cached) && DateTime.UtcNow < cached.expiry)
            {
                return (double)cached.result;
            }

            var position = GetSunPositionWithCaching(dateTime, latitude, longitude);

            // Convert from radians to degrees
            var elevationDegrees = position.Altitude * RadToDeg;

            _calculationCache[cacheKey] = (elevationDegrees, DateTime.UtcNow.Add(_cacheTimeout));
            return elevationDegrees;
        }

        /// <summary>
        /// Get sun phases with caching to avoid redundant calculations for the same day/location
        /// </summary>
        private IEnumerable<SunPhase> GetSunPhasesWithCaching(DateTime date, double latitude, double longitude)
        {
            var cacheKey = $"phases_{date:yyyyMMdd}_{latitude:F4}_{longitude:F4}";

            if (_calculationCache.TryGetValue(cacheKey, out var cached) && DateTime.UtcNow < cached.expiry)
            {
                return (IEnumerable<SunPhase>)cached.result;
            }

            var phases = SunCalc.GetSunPhases(date, latitude, longitude);
            _calculationCache[cacheKey] = (phases, DateTime.UtcNow.Add(_cacheTimeout));

            return phases;
        }

        /// <summary>
        /// Get sun position with caching to avoid redundant calculations for the same time/location
        /// </summary>
        private SunPosition GetSunPositionWithCaching(DateTime dateTime, double latitude, double longitude)
        {
            var cacheKey = $"position_{dateTime:yyyyMMddHHmm}_{latitude:F4}_{longitude:F4}";

            if (_calculationCache.TryGetValue(cacheKey, out var cached) && DateTime.UtcNow < cached.expiry)
            {
                return (SunPosition)cached.result;
            }

            var position = SunCalc.GetSunPosition(dateTime, latitude, longitude);

            // Use shorter cache timeout for position data since it changes more frequently
            var positionCacheTimeout = TimeSpan.FromMinutes(5);
            _calculationCache[cacheKey] = (position, DateTime.UtcNow.Add(positionCacheTimeout));

            return position;
        }

        /// <summary>
        /// Batch calculation method for multiple sun times to improve performance when calculating multiple values
        /// </summary>
        public async Task<Dictionary<string, DateTime>> GetBatchSunTimesAsync(
            DateTime date,
            double latitude,
            double longitude,
            string timezone,
            params string[] requestedTimes)
        {
            return await Task.Run(() =>
            {
                var results = new Dictionary<string, DateTime>();
                var phases = GetSunPhasesWithCaching(date, latitude, longitude);

                foreach (var timeType in requestedTimes)
                {
                    switch (timeType.ToLower())
                    {
                        case "sunrise":
                            results[timeType] = GetSunrise(date, latitude, longitude, timezone);
                            break;
                        case "sunset":
                            results[timeType] = GetSunset(date, latitude, longitude, timezone);
                            break;
                        case "solarnoon":
                            results[timeType] = GetSolarNoon(date, latitude, longitude, timezone);
                            break;
                        case "civildawn":
                            results[timeType] = GetCivilDawn(date, latitude, longitude, timezone);
                            break;
                        case "civildusk":
                            results[timeType] = GetCivilDusk(date, latitude, longitude, timezone);
                            break;
                        case "nauticaldawn":
                            results[timeType] = GetNauticalDawn(date, latitude, longitude, timezone);
                            break;
                        case "nauticaldusk":
                            results[timeType] = GetNauticalDusk(date, latitude, longitude, timezone);
                            break;
                        case "astronomicaldawn":
                            results[timeType] = GetAstronomicalDawn(date, latitude, longitude, timezone);
                            break;
                        case "astronomicaldusk":
                            results[timeType] = GetAstronomicalDusk(date, latitude, longitude, timezone);
                            break;
                    }
                }

                return results;
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Periodic cleanup of expired cache entries to prevent memory leaks
        /// </summary>
        public void CleanupExpiredCache()
        {
            var expiredKeys = _calculationCache
                .Where(kvp => DateTime.UtcNow >= kvp.Value.expiry)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                _calculationCache.TryRemove(key, out _);
            }
        }

        /// <summary>
        /// Preload sun calculations for a date range to improve performance for bulk operations
        /// </summary>
        public async Task PreloadSunCalculationsAsync(
            DateTime startDate,
            DateTime endDate,
            double latitude,
            double longitude,
            string timezone)
        {
            await Task.Run(() =>
            {
                var currentDate = startDate.Date;
                while (currentDate <= endDate.Date)
                {
                    // Preload phases for each day
                    GetSunPhasesWithCaching(currentDate, latitude, longitude);

                    // Preload key times
                    GetSunrise(currentDate, latitude, longitude, timezone);
                    GetSunset(currentDate, latitude, longitude, timezone);
                    GetSolarNoon(currentDate, latitude, longitude, timezone);

                    currentDate = currentDate.AddDays(1);
                }
            }).ConfigureAwait(false);
        }
    }
}