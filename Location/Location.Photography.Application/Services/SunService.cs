// Location.Photography.Infrastructure/Services/SunService.cs
using Location.Core.Application.Common.Models;
using Location.Photography.Application.Resources;
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
                return Result<SunPositionDto>.Failure(AppResources.SunLocation_Error_CalculatingSunPosition);
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
                return Result<SunTimesDto>.Failure(AppResources.SunLocation_Error_CalculatingSunTimes);
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
                return Result<List<SunPositionDto>>.Failure(AppResources.SunLocation_Error_CalculatingSunPosition);
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

                var times = await Task.WhenAll(timeTasks).ConfigureAwait(false);
                var validTimes = times.Where(t => t != null).ToList();

                return Result<List<SunTimesDto>>.Success(validTimes);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Result<List<SunTimesDto>>.Failure(AppResources.SunLocation_Error_CalculatingSunTimes);
            }
        }
    }
}