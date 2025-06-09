using Location.Photography.Domain.Services;

namespace Location.Photography.Infrastructure.Test.Services
{
    /// <summary>
    /// Complete stub implementation of ISunCalculatorService for testing purposes
    /// Provides realistic test data that follows astronomical patterns
    /// </summary>
    internal class StubSunCalculatorService : ISunCalculatorService
    {
        private readonly Dictionary<string, object> _cache = new();

        #region Solar Data Methods

        public DateTime GetSunrise(DateTime date, double latitude, double longitude, string timezone)
        {
            // Simulate sunrise time based on latitude and season
            var baseHour = 6.0; // Base sunrise at 6 AM
            var seasonalVariation = Math.Sin((date.DayOfYear - 81) * 2 * Math.PI / 365) * 2; // ±2 hours seasonal variation
            var latitudeVariation = latitude / 90.0 * 1.5; // Latitude effect (±1.5 hours)

            var sunriseHour = baseHour - seasonalVariation - latitudeVariation;
            return date.Date.AddHours(Math.Max(0, Math.Min(24, sunriseHour)));
        }

        public DateTime GetSunset(DateTime date, double latitude, double longitude, string timezone)
        {
            // Simulate sunset time based on latitude and season
            var baseHour = 18.0; // Base sunset at 6 PM
            var seasonalVariation = Math.Sin((date.DayOfYear - 81) * 2 * Math.PI / 365) * 2; // ±2 hours seasonal variation
            var latitudeVariation = latitude / 90.0 * 1.5; // Latitude effect

            var sunsetHour = baseHour + seasonalVariation + latitudeVariation;
            return date.Date.AddHours(Math.Max(0, Math.Min(24, sunsetHour)));
        }

        public DateTime GetSunriseEnd(DateTime date, double latitude, double longitude, string timezone)
        {
            var sunrise = GetSunrise(date, latitude, longitude, timezone);
            return sunrise.AddMinutes(3); // Sun diameter crossing takes ~3 minutes
        }

        public DateTime GetSunsetStart(DateTime date, double latitude, double longitude, string timezone)
        {
            var sunset = GetSunset(date, latitude, longitude, timezone);
            return sunset.AddMinutes(-3); // Sun diameter crossing takes ~3 minutes
        }

        public DateTime GetSolarNoon(DateTime date, double latitude, double longitude, string timezone)
        {
            // Solar noon varies slightly with equation of time
            var baseHour = 12.0;
            var equationOfTime = 0.25 * Math.Sin(2 * Math.PI * (date.DayOfYear - 81) / 365); // ±15 minutes variation
            var longitudeOffset = longitude / 15.0; // 15 degrees per hour

            var noonHour = baseHour + equationOfTime - longitudeOffset;
            return date.Date.AddHours(noonHour);
        }

        public DateTime GetNadir(DateTime date, double latitude, double longitude, string timezone)
        {
            var solarNoon = GetSolarNoon(date, latitude, longitude, timezone);
            return solarNoon.AddHours(12); // Nadir is 12 hours from solar noon
        }

        public DateTime GetCivilDawn(DateTime date, double latitude, double longitude, string timezone)
        {
            var sunrise = GetSunrise(date, latitude, longitude, timezone);
            return sunrise.AddMinutes(-30); // Civil dawn ~30 minutes before sunrise
        }

        public DateTime GetCivilDusk(DateTime date, double latitude, double longitude, string timezone)
        {
            var sunset = GetSunset(date, latitude, longitude, timezone);
            return sunset.AddMinutes(30); // Civil dusk ~30 minutes after sunset
        }

        public DateTime GetNauticalDawn(DateTime date, double latitude, double longitude, string timezone)
        {
            var civilDawn = GetCivilDawn(date, latitude, longitude, timezone);
            return civilDawn.AddMinutes(-30); // Nautical dawn ~30 minutes before civil dawn
        }

        public DateTime GetNauticalDusk(DateTime date, double latitude, double longitude, string timezone)
        {
            var civilDusk = GetCivilDusk(date, latitude, longitude, timezone);
            return civilDusk.AddMinutes(30); // Nautical dusk ~30 minutes after civil dusk
        }

        public DateTime GetAstronomicalDawn(DateTime date, double latitude, double longitude, string timezone)
        {
            var nauticalDawn = GetNauticalDawn(date, latitude, longitude, timezone);
            return nauticalDawn.AddMinutes(-30); // Astronomical dawn ~30 minutes before nautical dawn
        }

        public DateTime GetAstronomicalDusk(DateTime date, double latitude, double longitude, string timezone)
        {
            var nauticalDusk = GetNauticalDusk(date, latitude, longitude, timezone);
            return nauticalDusk.AddMinutes(30); // Astronomical dusk ~30 minutes after nautical dusk
        }

        public DateTime GetGoldenHour(DateTime date, double latitude, double longitude, string timezone)
        {
            var sunset = GetSunset(date, latitude, longitude, timezone);
            return sunset.AddHours(-1); // Golden hour starts ~1 hour before sunset
        }

        public DateTime GetGoldenHourEnd(DateTime date, double latitude, double longitude, string timezone)
        {
            var sunrise = GetSunrise(date, latitude, longitude, timezone);
            return sunrise.AddHours(1); // Golden hour ends ~1 hour after sunrise
        }

        public DateTime GetBlueHourStart(DateTime date, double latitude, double longitude, string timezone)
        {
            var sunrise = GetSunrise(date, latitude, longitude, timezone);
            return sunrise.AddHours(-1); // Blue hour starts ~1 hour before sunrise
        }

        public DateTime GetBlueHourEnd(DateTime date, double latitude, double longitude, string timezone)
        {
            var sunset = GetSunset(date, latitude, longitude, timezone);
            return sunset.AddHours(1); // Blue hour ends ~1 hour after sunset
        }

        public DateTime GetNightEnd(DateTime date, double latitude, double longitude, string timezone)
        {
            return GetAstronomicalDawn(date, latitude, longitude, timezone);
        }

        public DateTime GetNight(DateTime date, double latitude, double longitude, string timezone)
        {
            return GetAstronomicalDusk(date, latitude, longitude, timezone);
        }

        public double GetSolarAzimuth(DateTime dateTime, double latitude, double longitude, string timezone)
        {
            // Simulate solar azimuth based on time of day
            var hour = dateTime.Hour + dateTime.Minute / 60.0;
            var azimuth = (hour - 6) * 15; // 15 degrees per hour, starting from east at 6 AM

            // Adjust for latitude
            azimuth += latitude * 0.5;

            // Normalize to 0-360 range
            while (azimuth < 0) azimuth += 360;
            while (azimuth >= 360) azimuth -= 360;

            return azimuth;
        }

        public double GetSolarElevation(DateTime dateTime, double latitude, double longitude, string timezone)
        {
            // Simulate solar elevation based on time of day
            var hour = dateTime.Hour + dateTime.Minute / 60.0;
            var solarNoon = GetSolarNoon(dateTime.Date, latitude, longitude, timezone);
            var noonHour = solarNoon.Hour + solarNoon.Minute / 60.0;

            // Calculate elevation as sine curve with peak at solar noon
            var hourFromNoon = Math.Abs(hour - noonHour);
            var maxElevation = 90 - Math.Abs(latitude - 23.5); // Approximate max elevation
            var elevation = maxElevation * Math.Cos(hourFromNoon * Math.PI / 12);

            return Math.Max(-90, Math.Min(90, elevation));
        }

        public double GetSolarDistance(DateTime dateTime, double latitude, double longitude, string timezone)
        {
            // Earth-Sun distance varies from ~0.983 to 1.017 AU throughout the year
            var dayOfYear = dateTime.DayOfYear;
            var distance = 1.0 + 0.017 * Math.Cos(2 * Math.PI * (dayOfYear - 4) / 365.25);
            return distance;
        }

        public string GetSunCondition(DateTime dateTime, double latitude, double longitude, string timezone)
        {
            var elevation = GetSolarElevation(dateTime, latitude, longitude, timezone);

            if (elevation > 0) return "Up";
            if (elevation > -6) return "CivilTwilight";
            if (elevation > -12) return "NauticalTwilight";
            if (elevation > -18) return "AstronomicalTwilight";
            return "Down";
        }

        #endregion

        #region Lunar Data Methods

        public DateTime? GetMoonrise(DateTime date, double latitude, double longitude, string timezone)
        {
            // Simulate moonrise - moon rises ~50 minutes later each day
            var baseTime = new DateTime(2024, 1, 1, 18, 0, 0); // Reference moonrise
            var daysSinceReference = (date - baseTime.Date).TotalDays;
            var delayMinutes = (daysSinceReference * 50) % (24 * 60); // 50 minutes delay per day

            var moonriseTime = date.Date.AddHours(18).AddMinutes(delayMinutes);

            // Sometimes moon doesn't rise on a given day (return null ~10% of time for realism)
            if (date.DayOfYear % 10 == 0) return null;

            return moonriseTime;
        }

        public DateTime? GetMoonset(DateTime date, double latitude, double longitude, string timezone)
        {
            var moonrise = GetMoonrise(date, latitude, longitude, timezone);
            if (!moonrise.HasValue) return null;

            // Moonset occurs ~12.4 hours after moonrise on average
            return moonrise.Value.AddHours(12.4);
        }

        public double GetMoonAzimuth(DateTime dateTime, double latitude, double longitude, string timezone)
        {
            // Simulate moon azimuth - moon moves ~15 degrees per hour
            var hour = dateTime.Hour + dateTime.Minute / 60.0;
            var azimuth = (hour * 15 + dateTime.DayOfYear * 13) % 360; // 13 degrees daily shift
            return azimuth;
        }

        public double GetMoonElevation(DateTime dateTime, double latitude, double longitude, string timezone)
        {
            // Simulate moon elevation
            var hour = dateTime.Hour + dateTime.Minute / 60.0;
            var elevation = 45 * Math.Sin((hour - 12) * Math.PI / 12) + latitude * 0.3;
            return Math.Max(-90, Math.Min(90, elevation));
        }

        public double GetMoonDistance(DateTime dateTime, double latitude, double longitude, string timezone)
        {
            // Moon distance varies between ~356,000 km (perigee) and ~406,000 km (apogee)
            var dayOfMonth = dateTime.Day;
            var distance = 381000 + 25000 * Math.Sin(dayOfMonth * 2 * Math.PI / 27.3); // 27.3 day cycle
            return distance;
        }

        public double GetMoonIllumination(DateTime dateTime, double latitude, double longitude, string timezone)
        {
            // Simulate lunar phase cycle (29.5 days)
            var daysSinceNewMoon = (dateTime - new DateTime(2024, 1, 11)).TotalDays % 29.5; // Reference new moon
            var phaseAngle = (daysSinceNewMoon / 29.5) * 2 * Math.PI;
            var illumination = (1 - Math.Cos(phaseAngle)) / 2;
            return Math.Max(0, Math.Min(1, illumination));
        }

        public double GetMoonPhaseAngle(DateTime dateTime, double latitude, double longitude, string timezone)
        {
            var daysSinceNewMoon = (dateTime - new DateTime(2024, 1, 11)).TotalDays % 29.5;
            return (daysSinceNewMoon / 29.5) * 360;
        }

        public string GetMoonPhaseName(DateTime dateTime, double latitude, double longitude, string timezone)
        {
            var illumination = GetMoonIllumination(dateTime, latitude, longitude, timezone);

            if (illumination < 0.05) return "NewMoon";
            if (illumination < 0.45) return "WaxingCrescent";
            if (illumination < 0.55) return "FirstQuarter";
            if (illumination < 0.95) return "WaxingGibbous";
            if (illumination >= 0.95) return "FullMoon";
            return "WaningGibbous"; // This logic is simplified for testing
        }

        public DateTime? GetNextLunarPerigee(DateTime date, double latitude, double longitude, string timezone)
        {
            // Perigee occurs roughly every 27.3 days
            var daysSinceLastPerigee = (date - new DateTime(2024, 1, 1)).TotalDays % 27.3;
            var daysToNext = 27.3 - daysSinceLastPerigee;
            return date.AddDays(daysToNext);
        }

        public DateTime? GetNextLunarApogee(DateTime date, double latitude, double longitude, string timezone)
        {
            // Apogee occurs roughly every 27.3 days, offset from perigee
            var nextPerigee = GetNextLunarPerigee(date, latitude, longitude, timezone);
            if (!nextPerigee.HasValue) return null;
            return nextPerigee.Value.AddDays(13.65); // ~half the cycle
        }

        #endregion

        #region Eclipse Data Methods

        public (DateTime? date, string type, bool isVisible) GetNextSolarEclipse(DateTime date, double latitude, double longitude, string timezone)
        {
            // Simulate next solar eclipse - eclipses are rare events
            var nextEclipseDate = new DateTime(2024, 8, 12); // Example future eclipse
            if (date >= nextEclipseDate)
                nextEclipseDate = date.AddMonths(6 + (date.Month % 18)); // Every 18 months average

            // Simulate visibility based on location
            var isVisible = Math.Abs(latitude - 40) < 30 && Math.Abs(longitude + 100) < 60; // Rough path
            var type = isVisible ? "Total" : "Partial";

            return (nextEclipseDate, type, isVisible);
        }

        public (DateTime? date, string type, bool isVisible) GetLastSolarEclipse(DateTime date, double latitude, double longitude, string timezone)
        {
            // Simulate last solar eclipse
            var lastEclipseDate = new DateTime(2024, 4, 8); // Example past eclipse
            if (date <= lastEclipseDate)
                lastEclipseDate = date.AddMonths(-(6 + (date.Month % 18)));

            var isVisible = Math.Abs(latitude - 45) < 25 && Math.Abs(longitude + 90) < 50;
            var type = isVisible ? "Total" : "Partial";

            return (lastEclipseDate, type, isVisible);
        }

        public (DateTime? date, string type, bool isVisible) GetNextLunarEclipse(DateTime date, double latitude, double longitude, string timezone)
        {
            // Lunar eclipses occur ~2-3 times per year
            var nextEclipseDate = new DateTime(2024, 9, 18); // Example future lunar eclipse
            if (date >= nextEclipseDate)
                nextEclipseDate = date.AddMonths(6);

            // Lunar eclipses are visible from entire night side of Earth
            var isVisible = true; // Simplified - assume always visible
            var type = "Total";

            return (nextEclipseDate, type, isVisible);
        }

        public (DateTime? date, string type, bool isVisible) GetLastLunarEclipse(DateTime date, double latitude, double longitude, string timezone)
        {
            // Simulate last lunar eclipse
            var lastEclipseDate = new DateTime(2024, 3, 25); // Example past lunar eclipse
            if (date <= lastEclipseDate)
                lastEclipseDate = date.AddMonths(-6);

            var isVisible = true; // Lunar eclipses widely visible
            var type = "Partial";

            return (lastEclipseDate, type, isVisible);
        }

        #endregion

        #region Performance Methods

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

                if (requestedData == null) return results;

                foreach (var dataType in requestedData)
                {
                    try
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

                            // Unknown data types are ignored (no exception thrown)
                            default:
                                // Silently ignore unknown data types for robustness
                                break;
                        }
                    }
                    catch
                    {
                        // Ignore errors for individual data types to prevent complete failure
                    }
                }

                return results;
            });
        }

        public void CleanupExpiredCache()
        {
            // Simulate cache cleanup - for stub, just clear everything
            _cache.Clear();
        }

        public async Task PreloadAstronomicalCalculationsAsync(
            DateTime startDate,
            DateTime endDate,
            double latitude,
            double longitude,
            string timezone)
        {
            await Task.Run(() =>
            {
                // Simulate preloading by calling key methods for each date in range
                var currentDate = startDate.Date;

                // Ensure end date is not before start date
                if (endDate < startDate) return;

                // Limit to reasonable range to prevent excessive processing
                var maxDays = Math.Min((endDate - startDate).TotalDays, 365);

                for (int i = 0; i <= maxDays; i++)
                {
                    var date = currentDate.AddDays(i);

                    // Preload key calculations
                    try
                    {
                        GetSunrise(date, latitude, longitude, timezone);
                        GetSunset(date, latitude, longitude, timezone);
                        GetSolarNoon(date, latitude, longitude, timezone);
                        GetMoonrise(date, latitude, longitude, timezone);
                        GetMoonset(date, latitude, longitude, timezone);
                    }
                    catch
                    {
                        // Continue with other dates if one fails
                    }
                }
            });
        }

        #endregion
    }
}