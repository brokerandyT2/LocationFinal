// Location.Photography.Infrastructure/Services/SunCalculatorService.cs
using Location.Photography.Domain.Services;
using CoordinateSharp;
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

        private DateTime GetCompleteDateTime(DateTime? coordinateSharpTime, DateTime fallback)
        {
            if (coordinateSharpTime.HasValue)
            {
                return coordinateSharpTime.Value;
            }
            return fallback;
        }

        public DateTime GetSunrise(DateTime date, double latitude, double longitude, string timezone)
        {
            var cacheKey = $"sunrise_{date:yyyyMMdd}_{latitude:F4}_{longitude:F4}";

            if (_calculationCache.TryGetValue(cacheKey, out var cached) && DateTime.UtcNow < cached.expiry)
            {
                return (DateTime)cached.result;
            }

            var coordinate = new Coordinate(latitude, longitude, date);
            var result = GetCompleteDateTime(coordinate.CelestialInfo.SunRise, date);

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

            var coordinate = new Coordinate(latitude, longitude, date);
            var result = GetCompleteDateTime(coordinate.CelestialInfo.SunSet, date);

            _calculationCache[cacheKey] = (result, DateTime.UtcNow.Add(_cacheTimeout));
            return result;
        }

        public DateTime GetSunriseEnd(DateTime date, double latitude, double longitude, string timezone)
        {
            var cacheKey = $"sunriseend_{date:yyyyMMdd}_{latitude:F4}_{longitude:F4}";

            if (_calculationCache.TryGetValue(cacheKey, out var cached) && DateTime.UtcNow < cached.expiry)
            {
                return (DateTime)cached.result;
            }

            var sunrise = GetSunrise(date, latitude, longitude, timezone);
            // Sun diameter crossing takes approximately 2-4 minutes
            var result = sunrise.AddMinutes(3);

            _calculationCache[cacheKey] = (result, DateTime.UtcNow.Add(_cacheTimeout));
            return result;
        }

        public DateTime GetSunsetStart(DateTime date, double latitude, double longitude, string timezone)
        {
            var cacheKey = $"sunsetstart_{date:yyyyMMdd}_{latitude:F4}_{longitude:F4}";

            if (_calculationCache.TryGetValue(cacheKey, out var cached) && DateTime.UtcNow < cached.expiry)
            {
                return (DateTime)cached.result;
            }

            var sunset = GetSunset(date, latitude, longitude, timezone);
            // Sun diameter crossing takes approximately 2-4 minutes
            var result = sunset.AddMinutes(-3);

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

            var coordinate = new Coordinate(latitude, longitude, date);
            var result = GetCompleteDateTime(coordinate.CelestialInfo.SolarNoon, date.Date.AddHours(12));

            _calculationCache[cacheKey] = (result, DateTime.UtcNow.Add(_cacheTimeout));
            return result;
        }

        public DateTime GetNadir(DateTime date, double latitude, double longitude, string timezone)
        {
            var cacheKey = $"nadir_{date:yyyyMMdd}_{latitude:F4}_{longitude:F4}";

            if (_calculationCache.TryGetValue(cacheKey, out var cached) && DateTime.UtcNow < cached.expiry)
            {
                return (DateTime)cached.result;
            }

            // Nadir is 12 hours from solar noon
            var solarNoon = GetSolarNoon(date, latitude, longitude, timezone);
            var result = solarNoon.AddHours(12);

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

            var coordinate = new Coordinate(latitude, longitude, date);
            var result = GetCompleteDateTime(coordinate.CelestialInfo.AdditionalSolarTimes.CivilDawn, date);

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

            var coordinate = new Coordinate(latitude, longitude, date);
            var result = GetCompleteDateTime(coordinate.CelestialInfo.AdditionalSolarTimes.CivilDusk, date);

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

            var coordinate = new Coordinate(latitude, longitude, date);
            var result = GetCompleteDateTime(coordinate.CelestialInfo.AdditionalSolarTimes.NauticalDawn, date);

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

            var coordinate = new Coordinate(latitude, longitude, date);
            var result = GetCompleteDateTime(coordinate.CelestialInfo.AdditionalSolarTimes.NauticalDusk, date);

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

            var coordinate = new Coordinate(latitude, longitude, date);
            var result = GetCompleteDateTime(coordinate.CelestialInfo.AdditionalSolarTimes.AstronomicalDawn, date);

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

            var coordinate = new Coordinate(latitude, longitude, date);
            var result = GetCompleteDateTime(coordinate.CelestialInfo.AdditionalSolarTimes.AstronomicalDusk, date);

            _calculationCache[cacheKey] = (result, DateTime.UtcNow.Add(_cacheTimeout));
            return result;
        }

        public DateTime GetGoldenHour(DateTime date, double latitude, double longitude, string timezone)
        {
            var cacheKey = $"goldenhour_{date:yyyyMMdd}_{latitude:F4}_{longitude:F4}";

            if (_calculationCache.TryGetValue(cacheKey, out var cached) && DateTime.UtcNow < cached.expiry)
            {
                return (DateTime)cached.result;
            }

            // Golden hour starts when sun is 6° above horizon (evening)
            var sunset = GetSunset(date, latitude, longitude, timezone);
            var result = sunset.AddMinutes(-60); // Approximate 1 hour before sunset

            _calculationCache[cacheKey] = (result, DateTime.UtcNow.Add(_cacheTimeout));
            return result;
        }

        public DateTime GetGoldenHourEnd(DateTime date, double latitude, double longitude, string timezone)
        {
            var cacheKey = $"goldenhourend_{date:yyyyMMdd}_{latitude:F4}_{longitude:F4}";

            if (_calculationCache.TryGetValue(cacheKey, out var cached) && DateTime.UtcNow < cached.expiry)
            {
                return (DateTime)cached.result;
            }

            // Golden hour ends when sun is 6° above horizon (morning)
            var sunrise = GetSunrise(date, latitude, longitude, timezone);
            var result = sunrise.AddMinutes(60); // Approximate 1 hour after sunrise

            _calculationCache[cacheKey] = (result, DateTime.UtcNow.Add(_cacheTimeout));
            return result;
        }

        public DateTime GetNightEnd(DateTime date, double latitude, double longitude, string timezone)
        {
            var cacheKey = $"nightend_{date:yyyyMMdd}_{latitude:F4}_{longitude:F4}";

            if (_calculationCache.TryGetValue(cacheKey, out var cached) && DateTime.UtcNow < cached.expiry)
            {
                return (DateTime)cached.result;
            }

            // Night end is same as astronomical dawn
            var result = GetAstronomicalDawn(date, latitude, longitude, timezone);

            _calculationCache[cacheKey] = (result, DateTime.UtcNow.Add(_cacheTimeout));
            return result;
        }

        public DateTime GetNight(DateTime date, double latitude, double longitude, string timezone)
        {
            var cacheKey = $"night_{date:yyyyMMdd}_{latitude:F4}_{longitude:F4}";

            if (_calculationCache.TryGetValue(cacheKey, out var cached) && DateTime.UtcNow < cached.expiry)
            {
                return (DateTime)cached.result;
            }

            // Night starts at astronomical dusk
            var result = GetAstronomicalDusk(date, latitude, longitude, timezone);

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

            var coordinate = new Coordinate(latitude, longitude, dateTime);
            var azimuth = coordinate.CelestialInfo.SunAzimuth;

            _calculationCache[cacheKey] = (azimuth, DateTime.UtcNow.Add(_cacheTimeout));
            return azimuth;
        }

        public double GetSolarElevation(DateTime dateTime, double latitude, double longitude, string timezone)
        {
            var cacheKey = $"elevation_{dateTime:yyyyMMddHHmm}_{latitude:F4}_{longitude:F4}";

            if (_calculationCache.TryGetValue(cacheKey, out var cached) && DateTime.UtcNow < cached.expiry)
            {
                return (double)cached.result;
            }

            var coordinate = new Coordinate(latitude, longitude, dateTime);
            var elevation = coordinate.CelestialInfo.SunAltitude;

            _calculationCache[cacheKey] = (elevation, DateTime.UtcNow.Add(_cacheTimeout));
            return elevation;
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
                var coordinate = new Coordinate(latitude, longitude, date);

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
                        case "nadir":
                            results[timeType] = GetNadir(date, latitude, longitude, timezone);
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
                        case "goldenhour":
                            results[timeType] = GetGoldenHour(date, latitude, longitude, timezone);
                            break;
                        case "goldenhourend":
                            results[timeType] = GetGoldenHourEnd(date, latitude, longitude, timezone);
                            break;
                        case "nightend":
                            results[timeType] = GetNightEnd(date, latitude, longitude, timezone);
                            break;
                        case "night":
                            results[timeType] = GetNight(date, latitude, longitude, timezone);
                            break;
                        case "sunriseend":
                            results[timeType] = GetSunriseEnd(date, latitude, longitude, timezone);
                            break;
                        case "sunsetstart":
                            results[timeType] = GetSunsetStart(date, latitude, longitude, timezone);
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
                    // Preload key times by creating coordinate once per day
                    var coordinate = new Coordinate(latitude, longitude, currentDate);

                    // This will populate the coordinate's celestial info
                    GetSunrise(currentDate, latitude, longitude, timezone);
                    GetSunset(currentDate, latitude, longitude, timezone);
                    GetSolarNoon(currentDate, latitude, longitude, timezone);

                    currentDate = currentDate.AddDays(1);
                }
            }).ConfigureAwait(false);
        }
    }
}