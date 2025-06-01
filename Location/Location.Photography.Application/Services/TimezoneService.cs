// Location.Photography.Infrastructure/Services/TimezoneService.cs
using Location.Core.Application.Common.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Photography.Application.Services
{
    public interface ITimezoneService
    {
        Task<Result<string>> GetTimezoneFromCoordinatesAsync(double latitude, double longitude, CancellationToken cancellationToken = default);
        TimeZoneInfo GetTimeZoneInfo(string timezoneId);
        Task<Result<Dictionary<string, string>>> GetBatchTimezonesFromCoordinatesAsync(
            List<(double latitude, double longitude)> coordinates,
            CancellationToken cancellationToken = default);
    }

    public class TimezoneService : ITimezoneService
    {
        private readonly Dictionary<string, TimezoneBounds> _timezoneBounds;

        // Cache for timezone lookups to improve performance and reduce CPU usage
        private readonly ConcurrentDictionary<string, (string timezone, DateTime expiry)> _timezoneCache = new();
        private readonly TimeSpan _cacheTimeout = TimeSpan.FromHours(24); // Timezones don't change frequently

        public TimezoneService()
        {
            _timezoneBounds = InitializeTimezoneBounds();
        }

        public async Task<Result<string>> GetTimezoneFromCoordinatesAsync(double latitude, double longitude, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Create cache key for coordinate pair
                var cacheKey = $"{latitude:F4}_{longitude:F4}";

                // Check cache first to avoid repeated calculations
                if (_timezoneCache.TryGetValue(cacheKey, out var cached) && DateTime.UtcNow < cached.expiry)
                {
                    return Result<string>.Success(cached.timezone);
                }

                // Move timezone calculation to background thread to prevent UI blocking
                var timezoneId = await Task.Run(() => FindTimezoneForCoordinates(latitude, longitude), cancellationToken).ConfigureAwait(false);

                if (string.IsNullOrEmpty(timezoneId))
                {
                    return Result<string>.Failure("Unable to determine timezone for coordinates");
                }

                // Cache the result for future lookups
                _timezoneCache[cacheKey] = (timezoneId, DateTime.UtcNow.Add(_cacheTimeout));

                return Result<string>.Success(timezoneId);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Result<string>.Failure($"Error determining timezone: {ex.Message}");
            }
        }

        public TimeZoneInfo GetTimeZoneInfo(string timezoneId)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
            }
            catch
            {
                return TimeZoneInfo.Local;
            }
        }

        public async Task<Result<Dictionary<string, string>>> GetBatchTimezonesFromCoordinatesAsync(
            List<(double latitude, double longitude)> coordinates,
            CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (coordinates == null || coordinates.Count == 0)
                {
                    return Result<Dictionary<string, string>>.Success(new Dictionary<string, string>());
                }

                // Process timezone lookups in parallel for better performance
                var timezoneTasks = coordinates.Select(async coord =>
                {
                    var result = await GetTimezoneFromCoordinatesAsync(coord.latitude, coord.longitude, cancellationToken).ConfigureAwait(false);
                    var key = $"{coord.latitude:F4}_{coord.longitude:F4}";
                    var timezone = result.IsSuccess ? result.Data : "UTC";
                    return (key, timezone);
                });

                var timezoneResults = await Task.WhenAll(timezoneTasks).ConfigureAwait(false);

                var resultDict = new Dictionary<string, string>();
                foreach (var (key, timezone) in timezoneResults)
                {
                    resultDict[key] = timezone;
                }

                return Result<Dictionary<string, string>>.Success(resultDict);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Result<Dictionary<string, string>>.Failure($"Error determining batch timezones: {ex.Message}");
            }
        }

        private string FindTimezoneForCoordinates(double latitude, double longitude)
        {
            // Use parallel processing for faster timezone boundary checking
            var matchingTimezone = _timezoneBounds.AsParallel()
                .Where(kvp =>
                {
                    var bounds = kvp.Value;
                    return latitude >= bounds.MinLatitude && latitude <= bounds.MaxLatitude &&
                           longitude >= bounds.MinLongitude && longitude <= bounds.MaxLongitude;
                })
                .FirstOrDefault();

            if (!matchingTimezone.Equals(default(KeyValuePair<string, TimezoneBounds>)))
            {
                return matchingTimezone.Key;
            }

            // Fallback: rough timezone calculation based on longitude
            var utcOffset = Math.Round(longitude / 15.0);
            return GetTimezoneByUtcOffset(utcOffset);
        }

        private string GetTimezoneByUtcOffset(double utcOffset)
        {
            return utcOffset switch
            {
                -10 => "Pacific/Honolulu",
                -9 => "America/Anchorage",
                -8 => "America/Los_Angeles",
                -7 => "America/Denver",
                -6 => "America/Chicago",
                -5 => "America/New_York",
                -4 => "America/Halifax",
                -3 => "America/Argentina/Buenos_Aires",
                0 => "UTC",
                1 => "Europe/London",
                2 => "Europe/Berlin",
                3 => "Europe/Moscow",
                6 => "Asia/Dhaka",
                8 => "Asia/Shanghai",
                9 => "Asia/Tokyo",
                10 => "Australia/Sydney",
                _ => "UTC"
            };
        }

        private Dictionary<string, TimezoneBounds> InitializeTimezoneBounds()
        {
            return new Dictionary<string, TimezoneBounds>
            {
                // US Timezones
                ["America/New_York"] = new TimezoneBounds(25.0, 49.0, -84.0, -67.0),
                ["America/Chicago"] = new TimezoneBounds(25.0, 49.0, -104.0, -84.0), // Gateway Arch should fall here
                ["America/Denver"] = new TimezoneBounds(25.0, 49.0, -114.0, -104.0),
                ["America/Los_Angeles"] = new TimezoneBounds(25.0, 49.0, -125.0, -114.0),
                ["America/Anchorage"] = new TimezoneBounds(55.0, 71.0, -180.0, -130.0),
                ["Pacific/Honolulu"] = new TimezoneBounds(18.0, 23.0, -162.0, -154.0),

                // Major international zones
                ["Europe/London"] = new TimezoneBounds(49.0, 61.0, -8.0, 2.0),
                ["Europe/Berlin"] = new TimezoneBounds(47.0, 55.0, 6.0, 15.0),
                ["Europe/Moscow"] = new TimezoneBounds(55.0, 68.0, 37.0, 40.0),
                ["Asia/Tokyo"] = new TimezoneBounds(30.0, 46.0, 129.0, 146.0),
                ["Asia/Shanghai"] = new TimezoneBounds(18.0, 54.0, 73.0, 135.0),
                ["Australia/Sydney"] = new TimezoneBounds(-44.0, -10.0, 113.0, 154.0),

                // Additional global coverage for improved accuracy
                ["America/Mexico_City"] = new TimezoneBounds(14.0, 33.0, -118.0, -86.0),
                ["America/Toronto"] = new TimezoneBounds(42.0, 84.0, -95.0, -74.0),
                ["Europe/Paris"] = new TimezoneBounds(41.0, 51.0, -5.0, 10.0),
                ["Asia/Kolkata"] = new TimezoneBounds(6.0, 38.0, 68.0, 97.0),
                ["Africa/Cairo"] = new TimezoneBounds(22.0, 32.0, 25.0, 35.0),
                ["America/Sao_Paulo"] = new TimezoneBounds(-34.0, 5.0, -74.0, -35.0),
                ["Asia/Dubai"] = new TimezoneBounds(22.0, 26.0, 51.0, 56.0),
                ["Pacific/Auckland"] = new TimezoneBounds(-47.0, -34.0, 166.0, 179.0),
                ["America/Vancouver"] = new TimezoneBounds(49.0, 60.0, -139.0, -114.0),
                ["Europe/Rome"] = new TimezoneBounds(36.0, 47.0, 6.0, 19.0),
                ["Asia/Seoul"] = new TimezoneBounds(33.0, 39.0, 124.0, 132.0),
                ["America/Lima"] = new TimezoneBounds(-18.0, 0.0, -81.0, -69.0),
                ["Africa/Johannesburg"] = new TimezoneBounds(-35.0, -22.0, 16.0, 33.0),
                ["Asia/Bangkok"] = new TimezoneBounds(5.0, 21.0, 97.0, 106.0),
                ["Europe/Stockholm"] = new TimezoneBounds(55.0, 70.0, 11.0, 24.0)
            };
        }

        /// <summary>
        /// Validate timezone against system timezone list
        /// </summary>
        public bool IsValidTimezoneId(string timezoneId)
        {
            if (string.IsNullOrWhiteSpace(timezoneId))
                return false;

            try
            {
                TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get all available system timezones for picker/selection scenarios
        /// </summary>
        public async Task<List<(string Id, string DisplayName, TimeSpan Offset)>> GetAvailableTimezonesAsync(CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                var timezones = new List<(string Id, string DisplayName, TimeSpan Offset)>();
                var now = DateTime.UtcNow;

                foreach (var tz in TimeZoneInfo.GetSystemTimeZones())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    timezones.Add((
                        tz.Id,
                        tz.DisplayName,
                        tz.GetUtcOffset(now)
                    ));
                }

                return timezones.OrderBy(t => t.Offset).ToList();
            }, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Cleanup expired cache entries to prevent memory leaks
        /// </summary>
        public void CleanupExpiredCache()
        {
            var expiredKeys = _timezoneCache
                .Where(kvp => DateTime.UtcNow >= kvp.Value.expiry)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                _timezoneCache.TryRemove(key, out _);
            }
        }

        /// <summary>
        /// Preload common timezone lookups for better performance
        /// </summary>
        public async Task PreloadCommonTimezonesAsync(CancellationToken cancellationToken = default)
        {
            // Common coordinate pairs for major cities
            var commonCoordinates = new List<(double lat, double lon, string expectedTimezone)>
           {
               (40.7128, -74.0060, "America/New_York"), // New York
               (34.0522, -118.2437, "America/Los_Angeles"), // Los Angeles
               (41.8781, -87.6298, "America/Chicago"), // Chicago
               (51.5074, -0.1278, "Europe/London"), // London
               (48.8566, 2.3522, "Europe/Paris"), // Paris
               (35.6762, 139.6503, "Asia/Tokyo"), // Tokyo
               (-33.8688, 151.2093, "Australia/Sydney"), // Sydney
               (55.7558, 37.6176, "Europe/Moscow"), // Moscow
           };

            await Task.Run(async () =>
            {
                var preloadTasks = commonCoordinates.Select(async coord =>
                {
                    await GetTimezoneFromCoordinatesAsync(coord.lat, coord.lon, cancellationToken).ConfigureAwait(false);
                });

                await Task.WhenAll(preloadTasks).ConfigureAwait(false);
            }, cancellationToken).ConfigureAwait(false);
        }
    }

    public class TimezoneBounds
    {
        public double MinLatitude { get; }
        public double MaxLatitude { get; }
        public double MinLongitude { get; }
        public double MaxLongitude { get; }

        public TimezoneBounds(double minLat, double maxLat, double minLon, double maxLon)
        {
            MinLatitude = minLat;
            MaxLatitude = maxLat;
            MinLongitude = minLon;
            MaxLongitude = maxLon;
        }

        /// <summary>
        /// Check if coordinates fall within these bounds
        /// </summary>
        public bool Contains(double latitude, double longitude)
        {
            return latitude >= MinLatitude && latitude <= MaxLatitude &&
                   longitude >= MinLongitude && longitude <= MaxLongitude;
        }

        /// <summary>
        /// Calculate the area of this timezone bounds for priority sorting
        /// </summary>
        public double Area => (MaxLatitude - MinLatitude) * (MaxLongitude - MinLongitude);
    }
}