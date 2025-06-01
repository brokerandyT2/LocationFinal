using Location.Core.Application.Common.Models;
using Location.Photography.Domain.Models;
using Location.Photography.Domain.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Photography.Application.Queries.SunLocation
{
    public class GetOptimalShootingTimesQueryHandler : IRequestHandler<GetOptimalShootingTimesQuery, Result<List<OptimalShootingTime>>>
    {
        private readonly ISunCalculatorService _sunCalculatorService;
        private readonly ILogger<GetOptimalShootingTimesQueryHandler> _logger;

        public GetOptimalShootingTimesQueryHandler(
            ISunCalculatorService sunCalculatorService,
            ILogger<GetOptimalShootingTimesQueryHandler> logger)
        {
            _sunCalculatorService = sunCalculatorService ?? throw new ArgumentNullException(nameof(sunCalculatorService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<List<OptimalShootingTime>>> Handle(GetOptimalShootingTimesQuery request, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var optimalTimes = new List<OptimalShootingTime>();

                // Get location timezone
                TimeZoneInfo locationTimeZone;
                try
                {
                    locationTimeZone = TimeZoneInfo.FindSystemTimeZoneById(request.TimeZone);
                }
                catch
                {
                    locationTimeZone = TimeZoneInfo.Local;
                }

                // Get current time in location timezone for filtering
                var nowLocationTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, locationTimeZone);
                var filterEndTime = nowLocationTime.AddHours(24);

                // Calculate events for TODAY and TOMORROW (2 days)
                var dates = new[] { request.Date, request.Date.AddDays(1) };

                foreach (var date in dates)
                {
                    // Get all 14 sun events (UTC)
                    var sunriseUtc = _sunCalculatorService.GetSunrise(date, request.Latitude, request.Longitude, request.TimeZone);
                    var sunsetUtc = _sunCalculatorService.GetSunset(date, request.Latitude, request.Longitude, request.TimeZone);
                    var solarNoonUtc = _sunCalculatorService.GetSolarNoon(date, request.Latitude, request.Longitude, request.TimeZone);
                    var nadirUtc = _sunCalculatorService.GetNadir(date, request.Latitude, request.Longitude, request.TimeZone);
                    var civilDawnUtc = _sunCalculatorService.GetCivilDawn(date, request.Latitude, request.Longitude, request.TimeZone);
                    var civilDuskUtc = _sunCalculatorService.GetCivilDusk(date, request.Latitude, request.Longitude, request.TimeZone);
                    var nauticalDawnUtc = _sunCalculatorService.GetNauticalDawn(date, request.Latitude, request.Longitude, request.TimeZone);
                    var nauticalDuskUtc = _sunCalculatorService.GetNauticalDusk(date, request.Latitude, request.Longitude, request.TimeZone);
                    var astronomicalDawnUtc = _sunCalculatorService.GetAstronomicalDawn(date, request.Latitude, request.Longitude, request.TimeZone);
                    var astronomicalDuskUtc = _sunCalculatorService.GetAstronomicalDusk(date, request.Latitude, request.Longitude, request.TimeZone);
                    var goldenHourStartUtc = _sunCalculatorService.GetGoldenHour(date, request.Latitude, request.Longitude, request.TimeZone);
                    var goldenHourEndUtc = _sunCalculatorService.GetGoldenHourEnd(date, request.Latitude, request.Longitude, request.TimeZone);
                    var nightEndUtc = _sunCalculatorService.GetNightEnd(date, request.Latitude, request.Longitude, request.TimeZone);
                    var nightUtc = _sunCalculatorService.GetNight(date, request.Latitude, request.Longitude, request.TimeZone);

                    // Convert ALL times to location local time
                    var sunriseLocal = TimeZoneInfo.ConvertTimeFromUtc(sunriseUtc, locationTimeZone);
                    var sunsetLocal = TimeZoneInfo.ConvertTimeFromUtc(sunsetUtc, locationTimeZone);
                    var solarNoonLocal = TimeZoneInfo.ConvertTimeFromUtc(solarNoonUtc, locationTimeZone);
                    var nadirLocal = TimeZoneInfo.ConvertTimeFromUtc(nadirUtc, locationTimeZone);
                    var civilDawnLocal = TimeZoneInfo.ConvertTimeFromUtc(civilDawnUtc, locationTimeZone);
                    var civilDuskLocal = TimeZoneInfo.ConvertTimeFromUtc(civilDuskUtc, locationTimeZone);
                    var nauticalDawnLocal = TimeZoneInfo.ConvertTimeFromUtc(nauticalDawnUtc, locationTimeZone);
                    var nauticalDuskLocal = TimeZoneInfo.ConvertTimeFromUtc(nauticalDuskUtc, locationTimeZone);
                    var astronomicalDawnLocal = TimeZoneInfo.ConvertTimeFromUtc(astronomicalDawnUtc, locationTimeZone);
                    var astronomicalDuskLocal = TimeZoneInfo.ConvertTimeFromUtc(astronomicalDuskUtc, locationTimeZone);
                    var goldenHourStartLocal = TimeZoneInfo.ConvertTimeFromUtc(goldenHourStartUtc, locationTimeZone);
                    var goldenHourEndLocal = TimeZoneInfo.ConvertTimeFromUtc(goldenHourEndUtc, locationTimeZone);
                    var nightEndLocal = TimeZoneInfo.ConvertTimeFromUtc(nightEndUtc, locationTimeZone);
                    var nightLocal = TimeZoneInfo.ConvertTimeFromUtc(nightUtc, locationTimeZone);

                    // Create 14 single events
                    var events = new[]
                    {
                        new OptimalShootingTime { StartTime = goldenHourStartLocal, EndTime = goldenHourStartLocal.AddMinutes(30), LightQuality = LightQuality.GoldenHour, QualityScore = 0.95, Description = "Evening Golden Hour - Warm, soft light begins", IdealFor = new List<string> { "Portraits", "Landscapes", "Golden Hour" } },
               
                        new OptimalShootingTime { StartTime = sunsetLocal, EndTime = sunsetLocal.AddMinutes(30), LightQuality = LightQuality.GoldenHour, QualityScore = 0.95, Description = "Sunset - Sun disappears below horizon", IdealFor = new List<string> { "Sunset Photography", "Landscapes", "Silhouettes" } },
              
                        new OptimalShootingTime { StartTime = civilDuskLocal, EndTime = civilDuskLocal.AddMinutes(30), LightQuality = LightQuality.BlueHour, QualityScore = 0.9, Description = "Civil Dusk - Sun 6° below horizon", IdealFor = new List<string> { "Blue Hour", "Cityscapes", "Architecture" } },
               
                        new OptimalShootingTime { StartTime = nauticalDuskLocal, EndTime = nauticalDuskLocal.AddMinutes(30), LightQuality = LightQuality.BlueHour, QualityScore = 0.85, Description = "Nautical Dusk - Sun 12° below horizon", IdealFor = new List<string> { "Blue Hour", "Night Cityscapes" } },
              
                        new OptimalShootingTime { StartTime = astronomicalDuskLocal, EndTime = astronomicalDuskLocal.AddMinutes(30), LightQuality = LightQuality.BlueHour, QualityScore = 0.8, Description = "Astronomical Dusk - Sun 18° below horizon", IdealFor = new List<string> { "Astrophotography", "Night Photography" } },
             
                        new OptimalShootingTime { StartTime = nightLocal, EndTime = nightLocal.AddMinutes(30), LightQuality = LightQuality.Night, QualityScore = 0.7, Description = "Night - Complete darkness begins", IdealFor = new List<string> { "Astrophotography", "Night Photography", "Long Exposure" } },
             
                        new OptimalShootingTime { StartTime = nadirLocal, EndTime = nadirLocal.AddMinutes(30), LightQuality = LightQuality.Night, QualityScore = 0.3, Description = "Solar Nadir - Darkest point of night", IdealFor = new List<string> { "Astrophotography", "Night Photography" } },
              
                        new OptimalShootingTime { StartTime = nightEndLocal, EndTime = nightEndLocal.AddMinutes(30), LightQuality = LightQuality.BlueHour, QualityScore = 0.7, Description = "Night End - Beginning of astronomical twilight", IdealFor = new List<string> { "Astrophotography", "Dawn Photography" } },
             
                        new OptimalShootingTime { StartTime = astronomicalDawnLocal, EndTime = astronomicalDawnLocal.AddMinutes(30), LightQuality = LightQuality.BlueHour, QualityScore = 0.8, Description = "Astronomical Dawn - Sun 18° below horizon", IdealFor = new List<string> { "Astrophotography", "Dawn Landscapes" } },
             
                        new OptimalShootingTime { StartTime = nauticalDawnLocal, EndTime = nauticalDawnLocal.AddMinutes(30), LightQuality = LightQuality.BlueHour, QualityScore = 0.85, Description = "Nautical Dawn - Sun 12° below horizon", IdealFor = new List<string> { "Blue Hour", "Seascapes" } },
             
                        new OptimalShootingTime { StartTime = civilDawnLocal, EndTime = civilDawnLocal.AddMinutes(30), LightQuality = LightQuality.BlueHour, QualityScore = 0.9, Description = "Civil Dawn - Sun 6° below horizon", IdealFor = new List<string> { "Blue Hour", "Cityscapes", "Landscapes" } },
             
                        new OptimalShootingTime { StartTime = sunriseLocal, EndTime = sunriseLocal.AddMinutes(30), LightQuality = LightQuality.GoldenHour, QualityScore = 0.95, Description = "Sunrise - Sun appears above horizon", IdealFor = new List<string> { "Sunrise Photography", "Landscapes", "Portraits" } },
           
                        new OptimalShootingTime { StartTime = goldenHourEndLocal, EndTime = goldenHourEndLocal.AddMinutes(30), LightQuality = LightQuality.GoldenHour, QualityScore = 0.95, Description = "Morning Golden Hour End - Warm, soft light ends", IdealFor = new List<string> { "Portraits", "Landscapes", "Golden Hour" } },
            
                        new OptimalShootingTime { StartTime = solarNoonLocal, EndTime = solarNoonLocal.AddMinutes(30), LightQuality = LightQuality.Harsh, QualityScore = 0.4, Description = "Solar Noon - Sun at highest point, harsh light", IdealFor = new List<string> { "Architecture", "Minimal Shadows" } }
           };

                    optimalTimes.AddRange(events);
                }

                // Filter to next 24 hours in local time, then sort
                var filteredAndSortedTimes = optimalTimes
                    .Where(t => t.StartTime >= nowLocationTime && t.StartTime <= filterEndTime)
                    .OrderBy(t => t.StartTime)
                    .ToList();

                return await Task.FromResult(Result<List<OptimalShootingTime>>.Success(filteredAndSortedTimes));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating optimal shooting times for coordinates {Latitude}, {Longitude} on {Date}",
                    request.Latitude, request.Longitude, request.Date);
                return Result<List<OptimalShootingTime>>.Failure($"Error calculating optimal times: {ex.Message}");
            }
        }
    }
}