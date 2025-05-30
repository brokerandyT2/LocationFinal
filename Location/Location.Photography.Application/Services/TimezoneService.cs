
using Location.Core.Application.Common.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
namespace Location.Photography.Application.Services
{
    public interface ITimezoneService
    {
        Task<Result<string>> GetTimezoneFromCoordinatesAsync(double latitude, double longitude, CancellationToken cancellationToken = default);
        TimeZoneInfo GetTimeZoneInfo(string timezoneId);
    }
    public class TimezoneService : ITimezoneService
    {
        private readonly Dictionary<string, TimezoneBounds> _timezoneBounds;

        public TimezoneService()
        {
            _timezoneBounds = InitializeTimezoneBounds();
        }

        public async Task<Result<string>> GetTimezoneFromCoordinatesAsync(double latitude, double longitude, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Gateway Arch coordinates: 38.6247, -90.1848 should return "America/Chicago"
                var timezoneId = await Task.Run(() => FindTimezoneForCoordinates(latitude, longitude), cancellationToken);

                if (string.IsNullOrEmpty(timezoneId))
                {
                    return Result<string>.Failure("Unable to determine timezone for coordinates");
                }

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

        private string FindTimezoneForCoordinates(double latitude, double longitude)
        {
            foreach (var timezone in _timezoneBounds)
            {
                var bounds = timezone.Value;
                if (latitude >= bounds.MinLatitude && latitude <= bounds.MaxLatitude &&
                    longitude >= bounds.MinLongitude && longitude <= bounds.MaxLongitude)
                {
                    return timezone.Key;
                }
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

                // Add more as needed for global coverage
                ["America/Mexico_City"] = new TimezoneBounds(14.0, 33.0, -118.0, -86.0),
                ["America/Toronto"] = new TimezoneBounds(42.0, 84.0, -95.0, -74.0),
                ["Europe/Paris"] = new TimezoneBounds(41.0, 51.0, -5.0, 10.0),
                ["Asia/Kolkata"] = new TimezoneBounds(6.0, 38.0, 68.0, 97.0)
            };
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
    }
}
