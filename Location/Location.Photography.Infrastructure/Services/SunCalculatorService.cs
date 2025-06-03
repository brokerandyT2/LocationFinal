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
        // Cache for astronomical calculations to improve performance and reduce CPU usage
        private readonly ConcurrentDictionary<string, (object result, DateTime expiry)> _calculationCache = new();
        private readonly TimeSpan _cacheTimeout = TimeSpan.FromMinutes(30); // Astronomical data changes slowly

        private DateTime GetCompleteDateTime(DateTime? coordinateSharpTime, DateTime fallback)
        {
            if (coordinateSharpTime.HasValue)
            {
                return coordinateSharpTime.Value;
            }
            return fallback;
        }

        // === SOLAR DATA METHODS ===

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

            // Golden hour evening start = sunset start - 1 hour
            var sunsetStart = GetSunsetStart(date, latitude, longitude, timezone);
            var result = sunsetStart.AddHours(-1);

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

            // Golden hour morning end = sunrise end + 1 hour
            var sunriseEnd = GetSunriseEnd(date, latitude, longitude, timezone);
            var result = sunriseEnd.AddHours(1);

            _calculationCache[cacheKey] = (result, DateTime.UtcNow.Add(_cacheTimeout));
            return result;
        }

        public DateTime GetBlueHourStart(DateTime date, double latitude, double longitude, string timezone)
        {
            var cacheKey = $"bluehourstart_{date:yyyyMMdd}_{latitude:F4}_{longitude:F4}";

            if (_calculationCache.TryGetValue(cacheKey, out var cached) && DateTime.UtcNow < cached.expiry)
            {
                return (DateTime)cached.result;
            }

            // Blue hour morning start = sunrise start - 1 hour
            var sunrise = GetSunrise(date, latitude, longitude, timezone);
            var result = sunrise.AddHours(-1);

            _calculationCache[cacheKey] = (result, DateTime.UtcNow.Add(_cacheTimeout));
            return result;
        }

        public DateTime GetBlueHourEnd(DateTime date, double latitude, double longitude, string timezone)
        {
            var cacheKey = $"bluehourend_{date:yyyyMMdd}_{latitude:F4}_{longitude:F4}";

            if (_calculationCache.TryGetValue(cacheKey, out var cached) && DateTime.UtcNow < cached.expiry)
            {
                return (DateTime)cached.result;
            }

            // Blue hour evening end = sunset end + 1 hour
            var sunset = GetSunset(date, latitude, longitude, timezone);
            var result = sunset.AddHours(1);

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

        public double GetSolarDistance(DateTime dateTime, double latitude, double longitude, string timezone)
        {
            var cacheKey = $"solardistance_{dateTime:yyyyMMddHHmm}_{latitude:F4}_{longitude:F4}";

            if (_calculationCache.TryGetValue(cacheKey, out var cached) && DateTime.UtcNow < cached.expiry)
            {
                return (double)cached.result;
            }

            // Solar distance is not available in CoordinateSharp
            // Return average Earth-Sun distance in AU as approximation
            var result = 1.0; // 1 AU (Astronomical Unit)

            _calculationCache[cacheKey] = (result, DateTime.UtcNow.Add(_cacheTimeout));
            return result;
        }

        public string GetSunCondition(DateTime dateTime, double latitude, double longitude, string timezone)
        {
            var cacheKey = $"suncondition_{dateTime:yyyyMMddHHmm}_{latitude:F4}_{longitude:F4}";

            if (_calculationCache.TryGetValue(cacheKey, out var cached) && DateTime.UtcNow < cached.expiry)
            {
                return (string)cached.result;
            }

            var coordinate = new Coordinate(latitude, longitude, dateTime);
            var condition = coordinate.CelestialInfo.SunCondition.ToString();

            _calculationCache[cacheKey] = (condition, DateTime.UtcNow.Add(_cacheTimeout));
            return condition;
        }

        // === LUNAR DATA METHODS ===

        public DateTime? GetMoonrise(DateTime date, double latitude, double longitude, string timezone)
        {
            var cacheKey = $"moonrise_{date:yyyyMMdd}_{latitude:F4}_{longitude:F4}";

            if (_calculationCache.TryGetValue(cacheKey, out var cached) && DateTime.UtcNow < cached.expiry)
            {
                return (DateTime?)cached.result;
            }

            var coordinate = new Coordinate(latitude, longitude, date);
            var result = coordinate.CelestialInfo.MoonRise;

            _calculationCache[cacheKey] = (result, DateTime.UtcNow.Add(_cacheTimeout));
            return result;
        }

        public DateTime? GetMoonset(DateTime date, double latitude, double longitude, string timezone)
        {
            var cacheKey = $"moonset_{date:yyyyMMdd}_{latitude:F4}_{longitude:F4}";

            if (_calculationCache.TryGetValue(cacheKey, out var cached) && DateTime.UtcNow < cached.expiry)
            {
                return (DateTime?)cached.result;
            }

            var coordinate = new Coordinate(latitude, longitude, date);
            var result = coordinate.CelestialInfo.MoonSet;

            _calculationCache[cacheKey] = (result, DateTime.UtcNow.Add(_cacheTimeout));
            return result;
        }

        public double GetMoonAzimuth(DateTime dateTime, double latitude, double longitude, string timezone)
        {
            var cacheKey = $"moonazimuth_{dateTime:yyyyMMddHHmm}_{latitude:F4}_{longitude:F4}";

            if (_calculationCache.TryGetValue(cacheKey, out var cached) && DateTime.UtcNow < cached.expiry)
            {
                return (double)cached.result;
            }

            var coordinate = new Coordinate(latitude, longitude, dateTime);
            var azimuth = coordinate.CelestialInfo.MoonAzimuth;

            _calculationCache[cacheKey] = (azimuth, DateTime.UtcNow.Add(_cacheTimeout));
            return azimuth;
        }

        public double GetMoonElevation(DateTime dateTime, double latitude, double longitude, string timezone)
        {
            var cacheKey = $"moonelevation_{dateTime:yyyyMMddHHmm}_{latitude:F4}_{longitude:F4}";

            if (_calculationCache.TryGetValue(cacheKey, out var cached) && DateTime.UtcNow < cached.expiry)
            {
                return (double)cached.result;
            }

            var coordinate = new Coordinate(latitude, longitude, dateTime);
            var elevation = coordinate.CelestialInfo.MoonAltitude;

            _calculationCache[cacheKey] = (elevation, DateTime.UtcNow.Add(_cacheTimeout));
            return elevation;
        }

        public double GetMoonDistance(DateTime dateTime, double latitude, double longitude, string timezone)
        {
            var cacheKey = $"moondistance_{dateTime:yyyyMMddHHmm}_{latitude:F4}_{longitude:F4}";

            if (_calculationCache.TryGetValue(cacheKey, out var cached) && DateTime.UtcNow < cached.expiry)
            {
                return (double)cached.result;
            }

            var coordinate = new Coordinate(latitude, longitude, dateTime);
            var distance = coordinate.CelestialInfo.MoonDistance.Kilometers;

            _calculationCache[cacheKey] = (distance, DateTime.UtcNow.Add(_cacheTimeout));
            return distance;
        }

        public double GetMoonIllumination(DateTime dateTime, double latitude, double longitude, string timezone)
        {
            var cacheKey = $"moonillum_{dateTime:yyyyMMddHHmm}_{latitude:F4}_{longitude:F4}";

            if (_calculationCache.TryGetValue(cacheKey, out var cached) && DateTime.UtcNow < cached.expiry)
            {
                return (double)cached.result;
            }

            var coordinate = new Coordinate(latitude, longitude, dateTime);
            var illumination = coordinate.CelestialInfo.MoonIllum.Fraction;

            _calculationCache[cacheKey] = (illumination, DateTime.UtcNow.Add(_cacheTimeout));
            return illumination;
        }

        public double GetMoonPhaseAngle(DateTime dateTime, double latitude, double longitude, string timezone)
        {
            var cacheKey = $"moonphaseangle_{dateTime:yyyyMMddHHmm}_{latitude:F4}_{longitude:F4}";

            if (_calculationCache.TryGetValue(cacheKey, out var cached) && DateTime.UtcNow < cached.expiry)
            {
                return (double)cached.result;
            }

            var coordinate = new Coordinate(latitude, longitude, dateTime);
            var phaseAngle = coordinate.CelestialInfo.MoonIllum.Angle;

            _calculationCache[cacheKey] = (phaseAngle, DateTime.UtcNow.Add(_cacheTimeout));
            return phaseAngle;
        }

        public string GetMoonPhaseName(DateTime dateTime, double latitude, double longitude, string timezone)
        {
            var cacheKey = $"moonphasename_{dateTime:yyyyMMddHHmm}_{latitude:F4}_{longitude:F4}";

            if (_calculationCache.TryGetValue(cacheKey, out var cached) && DateTime.UtcNow < cached.expiry)
            {
                return (string)cached.result;
            }

            var coordinate = new Coordinate(latitude, longitude, dateTime);
            var phaseName = coordinate.CelestialInfo.MoonIllum.PhaseName.ToString();

            _calculationCache[cacheKey] = (phaseName, DateTime.UtcNow.Add(_cacheTimeout));
            return phaseName;
        }

        public DateTime? GetNextLunarPerigee(DateTime date, double latitude, double longitude, string timezone)
        {
            var cacheKey = $"nextperigee_{date:yyyyMMdd}_{latitude:F4}_{longitude:F4}";

            if (_calculationCache.TryGetValue(cacheKey, out var cached) && DateTime.UtcNow < cached.expiry)
            {
                return (DateTime?)cached.result;
            }

            var coordinate = new Coordinate(latitude, longitude, date);
            var result = coordinate.CelestialInfo.Perigee?.NextPerigee.Date;

            _calculationCache[cacheKey] = (result, DateTime.UtcNow.Add(_cacheTimeout));
            return result;
        }

        public DateTime? GetNextLunarApogee(DateTime date, double latitude, double longitude, string timezone)
        {
            var cacheKey = $"nextapogee_{date:yyyyMMdd}_{latitude:F4}_{longitude:F4}";

            if (_calculationCache.TryGetValue(cacheKey, out var cached) && DateTime.UtcNow < cached.expiry)
            {
                return (DateTime?)cached.result;
            }

            var coordinate = new Coordinate(latitude, longitude, date);
            var result = coordinate.CelestialInfo.Apogee?.NextApogee.Date;

            _calculationCache[cacheKey] = (result, DateTime.UtcNow.Add(_cacheTimeout));
            return result;
        }

        // === ECLIPSE DATA METHODS ===

        public (DateTime? date, string type, bool isVisible) GetNextSolarEclipse(DateTime date, double latitude, double longitude, string timezone)
        {
            var cacheKey = $"nextsolareclipse_{date:yyyyMMdd}_{latitude:F4}_{longitude:F4}";

            if (_calculationCache.TryGetValue(cacheKey, out var cached) && DateTime.UtcNow < cached.expiry)
            {
                return ((DateTime?, string, bool))cached.result;
            }

            var coordinate = new Coordinate(latitude, longitude, date);
            var eclipse = coordinate.CelestialInfo.SolarEclipse?.NextEclipse;

            var result = eclipse != null
                ? (eclipse?.Date, eclipse.Type.ToString(), eclipse.HasEclipseData)
                : (null, "", false);

            _calculationCache[cacheKey] = (result, DateTime.UtcNow.Add(_cacheTimeout));
            return result;
        }

        public (DateTime? date, string type, bool isVisible) GetLastSolarEclipse(DateTime date, double latitude, double longitude, string timezone)
        {
            var cacheKey = $"lastsolareclipse_{date:yyyyMMdd}_{latitude:F4}_{longitude:F4}";

            if (_calculationCache.TryGetValue(cacheKey, out var cached) && DateTime.UtcNow < cached.expiry)
            {
                return ((DateTime?, string, bool))cached.result;
            }

            var coordinate = new Coordinate(latitude, longitude, date);
            var eclipse = coordinate.CelestialInfo.SolarEclipse?.LastEclipse;

            var result = eclipse != null
                ? (eclipse?.Date, eclipse.Type.ToString(), eclipse.HasEclipseData)
                : (null, "", false);

            _calculationCache[cacheKey] = (result, DateTime.UtcNow.Add(_cacheTimeout));
            return result;
        }

        public (DateTime? date, string type, bool isVisible) GetNextLunarEclipse(DateTime date, double latitude, double longitude, string timezone)
        {
            var cacheKey = $"nextlunareclipse_{date:yyyyMMdd}_{latitude:F4}_{longitude:F4}";

            if (_calculationCache.TryGetValue(cacheKey, out var cached) && DateTime.UtcNow < cached.expiry)
            {
                return ((DateTime?, string, bool))cached.result;
            }

            var coordinate = new Coordinate(latitude, longitude, date);
            var eclipse = coordinate.CelestialInfo.LunarEclipse?.NextEclipse;

            var result = eclipse != null
                ? (eclipse?.Date, eclipse.Type.ToString(), eclipse.HasEclipseData)
                : (null, "", false);

            _calculationCache[cacheKey] = (result, DateTime.UtcNow.Add(_cacheTimeout));
            return result;
        }

        public (DateTime? date, string type, bool isVisible) GetLastLunarEclipse(DateTime date, double latitude, double longitude, string timezone)
        {
            var cacheKey = $"lastlunareclipse_{date:yyyyMMdd}_{latitude:F4}_{longitude:F4}";

            if (_calculationCache.TryGetValue(cacheKey, out var cached) && DateTime.UtcNow < cached.expiry)
            {
                return ((DateTime?, string, bool))cached.result;
            }

            var coordinate = new Coordinate(latitude, longitude, date);
            var eclipse = coordinate.CelestialInfo.LunarEclipse?.LastEclipse;

            var result = eclipse != null
                ? (eclipse?.Date, eclipse.Type.ToString(), eclipse.HasEclipseData)
                : (null, "", false);

            _calculationCache[cacheKey] = (result, DateTime.UtcNow.Add(_cacheTimeout));
            return result;
        }

        // === PERFORMANCE METHODS ===

        /// <summary>
        /// Batch calculation method for multiple astronomical data points to improve performance
        /// </summary>
        public async Task<Dictionary<string, object>> GetBatchAstronomicalDataAsync(
            DateTime date,
            double latitude,
            double longitude,
            string timezone,
            params string[] requestedData)
        {
            return await Task.Run(() =>
            {
                var results = new Dictionary<string, object>();
                var coordinate = new Coordinate(latitude, longitude, date);

                foreach (var dataType in requestedData)
                {
                    switch (dataType.ToLower())
                    {
                        // Solar data
                        case "sunrise":
                            results[dataType] = GetSunrise(date, latitude, longitude, timezone);
                            break;
                        case "sunset":
                            results[dataType] = GetSunset(date, latitude, longitude, timezone);
                            break;
                        case "solarnoon":
                            results[dataType] = GetSolarNoon(date, latitude, longitude, timezone);
                            break;
                        case "nadir":
                            results[dataType] = GetNadir(date, latitude, longitude, timezone);
                            break;
                        case "civildawn":
                            results[dataType] = GetCivilDawn(date, latitude, longitude, timezone);
                            break;
                        case "civildusk":
                            results[dataType] = GetCivilDusk(date, latitude, longitude, timezone);
                            break;
                        case "nauticaldawn":
                            results[dataType] = GetNauticalDawn(date, latitude, longitude, timezone);
                            break;
                        case "nauticaldusk":
                            results[dataType] = GetNauticalDusk(date, latitude, longitude, timezone);
                            break;
                        case "astronomicaldawn":
                            results[dataType] = GetAstronomicalDawn(date, latitude, longitude, timezone);
                            break;
                        case "astronomicaldusk":
                            results[dataType] = GetAstronomicalDusk(date, latitude, longitude, timezone);
                            break;
                        case "goldenhour":
                            results[dataType] = GetGoldenHour(date, latitude, longitude, timezone);
                            break;
                        case "goldenhourend":
                            results[dataType] = GetGoldenHourEnd(date, latitude, longitude, timezone);
                            break;
                        case "bluehourstart":
                            results[dataType] = GetBlueHourStart(date, latitude, longitude, timezone);
                            break;
                        case "bluehourend":
                            results[dataType] = GetBlueHourEnd(date, latitude, longitude, timezone);
                            break;
                        case "nightend":
                            results[dataType] = GetNightEnd(date, latitude, longitude, timezone);
                            break;
                        case "night":
                            results[dataType] = GetNight(date, latitude, longitude, timezone);
                            break;
                        case "sunriseend":
                            results[dataType] = GetSunriseEnd(date, latitude, longitude, timezone);
                            break;
                        case "sunsetstart":
                            results[dataType] = GetSunsetStart(date, latitude, longitude, timezone);
                            break;
                        case "solarazimuth":
                            results[dataType] = GetSolarAzimuth(date, latitude, longitude, timezone);
                            break;
                        case "solarelevation":
                            results[dataType] = GetSolarElevation(date, latitude, longitude, timezone);
                            break;
                        case "solardistance":
                            results[dataType] = GetSolarDistance(date, latitude, longitude, timezone);
                            break;
                        case "suncondition":
                            results[dataType] = GetSunCondition(date, latitude, longitude, timezone);
                            break;

                        // Lunar data
                        case "moonrise":
                            results[dataType] = GetMoonrise(date, latitude, longitude, timezone);
                            break;
                        case "moonset":
                            results[dataType] = GetMoonset(date, latitude, longitude, timezone);
                            break;
                        case "moonazimuth":
                            results[dataType] = GetMoonAzimuth(date, latitude, longitude, timezone);
                            break;
                        case "moonelevation":
                            results[dataType] = GetMoonElevation(date, latitude, longitude, timezone);
                            break;
                        case "moondistance":
                            results[dataType] = GetMoonDistance(date, latitude, longitude, timezone);
                            break;
                        case "moonillumination":
                            results[dataType] = GetMoonIllumination(date, latitude, longitude, timezone);
                            break;
                        case "moonphaseangle":
                            results[dataType] = GetMoonPhaseAngle(date, latitude, longitude, timezone);
                            break;
                        case "moonphasename":
                            results[dataType] = GetMoonPhaseName(date, latitude, longitude, timezone);
                            break;
                        case "nextlunarperigee":
                            results[dataType] = GetNextLunarPerigee(date, latitude, longitude, timezone);
                            break;
                        case "nextlunarapogee":
                            results[dataType] = GetNextLunarApogee(date, latitude, longitude, timezone);
                            break;

                        // Eclipse data
                        case "nextsolareclipse":
                            results[dataType] = GetNextSolarEclipse(date, latitude, longitude, timezone);
                            break;
                        case "lastsolareclipse":
                            results[dataType] = GetLastSolarEclipse(date, latitude, longitude, timezone);
                            break;
                        case "nextlunareclipse":
                            results[dataType] = GetNextLunarEclipse(date, latitude, longitude, timezone);
                            break;
                        case "lastlunareclipse":
                            results[dataType] = GetLastLunarEclipse(date, latitude, longitude, timezone);
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
        /// Preload astronomical calculations for a date range to improve performance for bulk operations
        /// </summary>
        public async Task PreloadAstronomicalCalculationsAsync(
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
                    // Preload key solar times by creating coordinate once per day
                    var coordinate = new Coordinate(latitude, longitude, currentDate);

                    // This will populate the coordinate's celestial info and cache
                    GetSunrise(currentDate, latitude, longitude, timezone);
                    GetSunset(currentDate, latitude, longitude, timezone);
                    GetSolarNoon(currentDate, latitude, longitude, timezone);
                    GetMoonrise(currentDate, latitude, longitude, timezone);
                    GetMoonset(currentDate, latitude, longitude, timezone);

                    currentDate = currentDate.AddDays(1);
                }
            }).ConfigureAwait(false);
        }
    }
}