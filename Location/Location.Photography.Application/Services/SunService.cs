// Location.Photography.Infrastructure/Services/SunService.cs
using Location.Core.Application.Common.Models;
using Location.Photography.Domain.Models;
using Location.Photography.Domain.Services;
using System.Collections.Concurrent;

namespace Location.Photography.Application.Services
{
    public class SunService : ISunService
    {
        private readonly ISunCalculatorService _sunCalculatorService;

        // Cache for sun calculations to improve performance and reduce redundant calculations
        private readonly ConcurrentDictionary<string, (object result, DateTime expiry)> _sunDataCache = new();
        private readonly TimeSpan _cacheTimeout = TimeSpan.FromMinutes(15); // Sun data changes relatively slowly

        public SunService(ISunCalculatorService sunCalculatorService)
        {
            _sunCalculatorService = sunCalculatorService ?? throw new ArgumentNullException(nameof(sunCalculatorService));
        }

        public async Task<Result<SunPositionDto>> GetSunPositionAsync(double latitude, double longitude, DateTime dateTime, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                // Create cache key for sun position
                var cacheKey = $"position_{dateTime:yyyyMMddHHmm}_{latitude:F4}_{longitude:F4}";

                // Check cache first to avoid redundant calculations
                if (_sunDataCache.TryGetValue(cacheKey, out var cached) && DateTime.UtcNow < cached.expiry)
                {
                    return Result<SunPositionDto>.Success((SunPositionDto)cached.result);
                }

                // Move sun calculations to background thread to prevent UI blocking
                var result = await Task.Run(() =>
                {
                    var azimuth = _sunCalculatorService.GetSolarAzimuth(dateTime, latitude, longitude, TimeZoneInfo.Local.ToString());
                    var elevation = _sunCalculatorService.GetSolarElevation(dateTime, latitude, longitude, TimeZoneInfo.Local.ToString());

                    return new SunPositionDto
                    {
                        Azimuth = azimuth,
                        Elevation = elevation,
                        DateTime = dateTime,
                        Latitude = latitude,
                        Longitude = longitude
                    };
                }, cancellationToken).ConfigureAwait(false);

                // Cache the result for future requests
                _sunDataCache[cacheKey] = (result, DateTime.UtcNow.Add(_cacheTimeout));

                return Result<SunPositionDto>.Success(result);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Result<SunPositionDto>.Failure($"Error calculating sun position: {ex.Message}");
            }
        }

        public async Task<Result<SunTimesDto>> GetSunTimesAsync(double latitude, double longitude, DateTime date, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                // Create cache key for sun times (daily data)
                var cacheKey = $"times_{date:yyyyMMdd}_{latitude:F4}_{longitude:F4}";

                // Check cache first to avoid redundant calculations
                if (_sunDataCache.TryGetValue(cacheKey, out var cached) && DateTime.UtcNow < cached.expiry)
                {
                    return Result<SunTimesDto>.Success((SunTimesDto)cached.result);
                }

                // Move sun calculations to background thread to prevent UI blocking
                var result = await Task.Run(() =>
                {
                    var timezone = TimeZoneInfo.Local.ToString();

                    var sunTimes = new SunTimesDto
                    {
                        Date = date,
                        Latitude = latitude,
                        Longitude = longitude,
                        Sunrise = _sunCalculatorService.GetSunrise(date, latitude, longitude, timezone),
                        Sunset = _sunCalculatorService.GetSunset(date, latitude, longitude, timezone),
                        SolarNoon = _sunCalculatorService.GetSolarNoon(date, latitude, longitude, timezone),
                        AstronomicalDawn = _sunCalculatorService.GetAstronomicalDawn(date, latitude, longitude, timezone),
                        AstronomicalDusk = _sunCalculatorService.GetAstronomicalDusk(date, latitude, longitude, timezone),
                        NauticalDawn = _sunCalculatorService.GetNauticalDawn(date, latitude, longitude, timezone),
                        NauticalDusk = _sunCalculatorService.GetNauticalDusk(date, latitude, longitude, timezone),
                        CivilDawn = _sunCalculatorService.GetCivilDawn(date, latitude, longitude, timezone),
                        CivilDusk = _sunCalculatorService.GetCivilDusk(date, latitude, longitude, timezone)
                    };

                    // Calculate golden hour times based on sunrise/sunset
                    sunTimes.GoldenHourMorningStart = sunTimes.Sunrise;
                    sunTimes.GoldenHourMorningEnd = sunTimes.Sunrise.AddHours(1);
                    sunTimes.GoldenHourEveningStart = sunTimes.Sunset.AddHours(-1);
                    sunTimes.GoldenHourEveningEnd = sunTimes.Sunset;

                    return sunTimes;
                }, cancellationToken).ConfigureAwait(false);

                // Cache with longer timeout for daily sun times (they don't change as frequently)
                var dailyCacheTimeout = TimeSpan.FromHours(2);
                _sunDataCache[cacheKey] = (result, DateTime.UtcNow.Add(dailyCacheTimeout));

                return Result<SunTimesDto>.Success(result);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Result<SunTimesDto>.Failure($"Error calculating sun times: {ex.Message}");
            }
        }

        /// <summary>
        /// Batch calculation for multiple sun positions to improve performance
        /// </summary>
        public async Task<Result<List<SunPositionDto>>> GetBatchSunPositionsAsync(
            List<(double latitude, double longitude, DateTime dateTime)> requests,
            CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (requests == null || requests.Count == 0)
                {
                    return Result<List<SunPositionDto>>.Success(new List<SunPositionDto>());
                }

                // Process sun position calculations in parallel for better performance
                var positionTasks = requests.Select(async request =>
                {
                    var result = await GetSunPositionAsync(request.latitude, request.longitude, request.dateTime, cancellationToken).ConfigureAwait(false);
                    return result.IsSuccess ? result.Data : null;
                });

                var positions = await Task.WhenAll(positionTasks).ConfigureAwait(false);
                var validPositions = positions.Where(p => p != null).ToList();

                return Result<List<SunPositionDto>>.Success(validPositions);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Result<List<SunPositionDto>>.Failure($"Error calculating batch sun positions: {ex.Message}");
            }
        }

        /// <summary>
        /// Batch calculation for multiple sun times to improve performance
        /// </summary>
        public async Task<Result<List<SunTimesDto>>> GetBatchSunTimesAsync(
            List<(double latitude, double longitude, DateTime date)> requests,
            CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (requests == null || requests.Count == 0)
                {
                    return Result<List<SunTimesDto>>.Success(new List<SunTimesDto>());
                }

                // Process sun times calculations in parallel for better performance
                var timeTasks = requests.Select(async request =>
                {
                    var result = await GetSunTimesAsync(request.latitude, request.longitude, request.date, cancellationToken).ConfigureAwait(false);
                    return result.IsSuccess ? result.Data : null;
                });

                var sunTimes = await Task.WhenAll(timeTasks).ConfigureAwait(false);
                var validSunTimes = sunTimes.Where(s => s != null).ToList();

                return Result<List<SunTimesDto>>.Success(validSunTimes);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Result<List<SunTimesDto>>.Failure($"Error calculating batch sun times: {ex.Message}");
            }
        }

        /// <summary>
        /// Get sun data for a date range to support calendar/planning features
        /// </summary>
        public async Task<Result<Dictionary<DateTime, SunTimesDto>>> GetSunTimesRangeAsync(
            double latitude,
            double longitude,
            DateTime startDate,
            DateTime endDate,
            CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var result = new Dictionary<DateTime, SunTimesDto>();
                var dateRequests = new List<(double latitude, double longitude, DateTime date)>();

                // Generate date range requests
                var currentDate = startDate.Date;
                while (currentDate <= endDate.Date)
                {
                    dateRequests.Add((latitude, longitude, currentDate));
                    currentDate = currentDate.AddDays(1);
                }

                // Use batch processing for efficiency
                var batchResult = await GetBatchSunTimesAsync(dateRequests, cancellationToken).ConfigureAwait(false);

                if (!batchResult.IsSuccess)
                {
                    return Result<Dictionary<DateTime, SunTimesDto>>.Failure(batchResult.ErrorMessage);
                }

                // Organize results by date
                foreach (var sunTimes in batchResult.Data)
                {
                    result[sunTimes.Date.Date] = sunTimes;
                }

                return Result<Dictionary<DateTime, SunTimesDto>>.Success(result);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Result<Dictionary<DateTime, SunTimesDto>>.Failure($"Error calculating sun times range: {ex.Message}");
            }
        }

        /// <summary>
        /// Calculate optimal photography times based on sun position and lighting conditions
        /// </summary>
        public async Task<Result<List<OptimalPhotoTime>>> GetOptimalPhotoTimesAsync(
            double latitude,
            double longitude,
            DateTime date,
            CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var sunTimesResult = await GetSunTimesAsync(latitude, longitude, date, cancellationToken).ConfigureAwait(false);
                if (!sunTimesResult.IsSuccess)
                {
                    return Result<List<OptimalPhotoTime>>.Failure(sunTimesResult.ErrorMessage);
                }

                var sunTimes = sunTimesResult.Data;
                var optimalTimes = new List<OptimalPhotoTime>();

                // Calculate optimal photography windows based on sun data
                await Task.Run(() =>
                {
                    // Blue hour morning
                    optimalTimes.Add(new OptimalPhotoTime
                    {
                        StartTime = sunTimes.CivilDawn,
                        EndTime = sunTimes.Sunrise,
                        Type = "Blue Hour",
                        Quality = "Excellent",
                        Description = "Soft, even blue light ideal for cityscapes and landscapes",
                        SunElevation = -6.0 // Civil twilight
                    });

                    // Golden hour morning
                    optimalTimes.Add(new OptimalPhotoTime
                    {
                        StartTime = sunTimes.Sunrise,
                        EndTime = sunTimes.Sunrise.AddHours(1),
                        Type = "Golden Hour",
                        Quality = "Excellent",
                        Description = "Warm, soft light with long shadows perfect for portraits",
                        SunElevation = 10.0 // Low sun angle
                    });

                    // Golden hour evening
                    optimalTimes.Add(new OptimalPhotoTime
                    {
                        StartTime = sunTimes.Sunset.AddHours(-1),
                        EndTime = sunTimes.Sunset,
                        Type = "Golden Hour",
                        Quality = "Excellent",
                        Description = "Warm, dramatic lighting ideal for portraits and landscapes",
                        SunElevation = 10.0 // Low sun angle
                    });

                    // Blue hour evening
                    optimalTimes.Add(new OptimalPhotoTime
                    {
                        StartTime = sunTimes.Sunset,
                        EndTime = sunTimes.CivilDusk,
                        Type = "Blue Hour",
                        Quality = "Excellent",
                        Description = "Deep blue sky with city lights beginning to show",
                        SunElevation = -6.0 // Civil twilight
                    });

                }, cancellationToken).ConfigureAwait(false);

                return Result<List<OptimalPhotoTime>>.Success(optimalTimes.OrderBy(t => t.StartTime).ToList());
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Result<List<OptimalPhotoTime>>.Failure($"Error calculating optimal photo times: {ex.Message}");
            }
        }

        /// <summary>
        /// Cleanup expired cache entries to prevent memory leaks
        /// </summary>
        public void CleanupExpiredCache()
        {
            var expiredKeys = _sunDataCache
                .Where(kvp => DateTime.UtcNow >= kvp.Value.expiry)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                _sunDataCache.TryRemove(key, out _);
            }
        }

        /// <summary>
        /// Preload sun calculations for upcoming days to improve performance
        /// </summary>
        public async Task PreloadUpcomingSunDataAsync(
            double latitude,
            double longitude,
            int daysAhead = 7,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var today = DateTime.Today;
                var preloadRequests = new List<(double latitude, double longitude, DateTime date)>();

                for (int i = 0; i <= daysAhead; i++)
                {
                    preloadRequests.Add((latitude, longitude, today.AddDays(i)));
                }

                // Preload sun times for the next week
                await GetBatchSunTimesAsync(preloadRequests, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Preloading is optional, don't fail if it doesn't work
            }
        }
    }

    /// <summary>
    /// Represents an optimal time window for photography based on sun position
    /// </summary>
    public class OptimalPhotoTime
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Quality { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double SunElevation { get; set; }
        public TimeSpan Duration => EndTime - StartTime;
    }
}