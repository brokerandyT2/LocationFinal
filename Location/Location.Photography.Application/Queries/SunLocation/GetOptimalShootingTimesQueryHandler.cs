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
                var currentDate = request.Date;

                // Get Sunrise with auto-advance
                var sunrise = _sunCalculatorService.GetSunrise(currentDate, request.Latitude, request.Longitude, request.TimeZone);
                if (sunrise < DateTime.UtcNow)
                {
                    sunrise = _sunCalculatorService.GetSunrise(currentDate.AddDays(1), request.Latitude, request.Longitude, request.TimeZone);
                }

                // Get SunriseEnd with auto-advance
                var sunriseEnd = _sunCalculatorService.GetSunriseEnd(currentDate, request.Latitude, request.Longitude, request.TimeZone);
                if (sunriseEnd < DateTime.UtcNow)
                {
                    sunriseEnd = _sunCalculatorService.GetSunriseEnd(currentDate.AddDays(1), request.Latitude, request.Longitude, request.TimeZone);
                }

                // Get GoldenHourEnd with auto-advance
                var goldenHourEnd = _sunCalculatorService.GetGoldenHourEnd(currentDate, request.Latitude, request.Longitude, request.TimeZone);
                if (goldenHourEnd < DateTime.UtcNow)
                {
                    goldenHourEnd = _sunCalculatorService.GetGoldenHourEnd(currentDate.AddDays(1), request.Latitude, request.Longitude, request.TimeZone);
                }

                // Get SolarNoon with auto-advance
                var solarNoon = _sunCalculatorService.GetSolarNoon(currentDate, request.Latitude, request.Longitude, request.TimeZone);
                if (solarNoon < DateTime.UtcNow)
                {
                    solarNoon = _sunCalculatorService.GetSolarNoon(currentDate.AddDays(1), request.Latitude, request.Longitude, request.TimeZone);
                }

                // Get GoldenHour with auto-advance
                var goldenHour = _sunCalculatorService.GetGoldenHour(currentDate, request.Latitude, request.Longitude, request.TimeZone);
                if (goldenHour < DateTime.UtcNow)
                {
                    goldenHour = _sunCalculatorService.GetGoldenHour(currentDate.AddDays(1), request.Latitude, request.Longitude, request.TimeZone);
                }

                // Get SunsetStart with auto-advance
                var sunsetStart = _sunCalculatorService.GetSunsetStart(currentDate, request.Latitude, request.Longitude, request.TimeZone);
                if (sunsetStart < DateTime.UtcNow)
                {
                    sunsetStart = _sunCalculatorService.GetSunsetStart(currentDate.AddDays(1), request.Latitude, request.Longitude, request.TimeZone);
                }

                // Get Sunset with auto-advance
                var sunset = _sunCalculatorService.GetSunset(currentDate, request.Latitude, request.Longitude, request.TimeZone);
                if (sunset < DateTime.UtcNow)
                {
                    sunset = _sunCalculatorService.GetSunset(currentDate.AddDays(1), request.Latitude, request.Longitude, request.TimeZone);
                }

                // Get CivilDusk with auto-advance
                var dusk = _sunCalculatorService.GetCivilDusk(currentDate, request.Latitude, request.Longitude, request.TimeZone);
                if (dusk < DateTime.UtcNow)
                {
                    dusk = _sunCalculatorService.GetCivilDusk(currentDate.AddDays(1), request.Latitude, request.Longitude, request.TimeZone);
                }

                // Get NauticalDusk with auto-advance
                var nauticalDusk = _sunCalculatorService.GetNauticalDusk(currentDate, request.Latitude, request.Longitude, request.TimeZone);
                if (nauticalDusk < DateTime.UtcNow)
                {
                    nauticalDusk = _sunCalculatorService.GetNauticalDusk(currentDate.AddDays(1), request.Latitude, request.Longitude, request.TimeZone);
                }

                // Get Night with auto-advance
                var night = _sunCalculatorService.GetNight(currentDate, request.Latitude, request.Longitude, request.TimeZone);
                if (night < DateTime.UtcNow)
                {
                    night = _sunCalculatorService.GetNight(currentDate.AddDays(1), request.Latitude, request.Longitude, request.TimeZone);
                }

                // Get NauticalDawn with auto-advance
                var nauticalDawn = _sunCalculatorService.GetNauticalDawn(currentDate, request.Latitude, request.Longitude, request.TimeZone);
                if (nauticalDawn < DateTime.UtcNow)
                {
                    nauticalDawn = _sunCalculatorService.GetNauticalDawn(currentDate.AddDays(1), request.Latitude, request.Longitude, request.TimeZone);
                }

                // Get CivilDawn with auto-advance
                var dawn = _sunCalculatorService.GetCivilDawn(currentDate, request.Latitude, request.Longitude, request.TimeZone);
                if (dawn < DateTime.UtcNow)
                {
                    dawn = _sunCalculatorService.GetCivilDawn(currentDate.AddDays(1), request.Latitude, request.Longitude, request.TimeZone);
                }
                sunrise = DateTime.SpecifyKind(sunrise, DateTimeKind.Utc);
                sunriseEnd = DateTime.SpecifyKind(sunriseEnd, DateTimeKind.Utc);
                goldenHourEnd = DateTime.SpecifyKind(goldenHourEnd, DateTimeKind.Utc);
                solarNoon = DateTime.SpecifyKind(solarNoon, DateTimeKind.Utc);
                goldenHour = DateTime.SpecifyKind(goldenHour, DateTimeKind.Utc);
                sunsetStart = DateTime.SpecifyKind(sunsetStart, DateTimeKind.Utc);
                sunset = DateTime.SpecifyKind(sunset, DateTimeKind.Utc);
                dusk = DateTime.SpecifyKind(dusk, DateTimeKind.Utc);
                nauticalDusk = DateTime.SpecifyKind(nauticalDusk, DateTimeKind.Utc);
                night = DateTime.SpecifyKind(night, DateTimeKind.Utc);
                nauticalDawn = DateTime.SpecifyKind(nauticalDawn, DateTimeKind.Utc);
                dawn = DateTime.SpecifyKind(dawn, DateTimeKind.Utc);

                // Convert all times to local time
                var sunriseLocal = TimeZoneInfo.ConvertTimeFromUtc(sunrise, TimeZoneInfo.Local);
                var sunriseEndLocal = TimeZoneInfo.ConvertTimeFromUtc(sunriseEnd, TimeZoneInfo.Local);
                var goldenHourEndLocal = TimeZoneInfo.ConvertTimeFromUtc(goldenHourEnd, TimeZoneInfo.Local);
                var solarNoonLocal = TimeZoneInfo.ConvertTimeFromUtc(solarNoon, TimeZoneInfo.Local);
                var goldenHourLocal = TimeZoneInfo.ConvertTimeFromUtc(goldenHour, TimeZoneInfo.Local);
                var sunsetStartLocal = TimeZoneInfo.ConvertTimeFromUtc(sunsetStart, TimeZoneInfo.Local);
                var sunsetLocal = TimeZoneInfo.ConvertTimeFromUtc(sunset, TimeZoneInfo.Local);
                var duskLocal = TimeZoneInfo.ConvertTimeFromUtc(dusk, TimeZoneInfo.Local);
                var nauticalDuskLocal = TimeZoneInfo.ConvertTimeFromUtc(nauticalDusk, TimeZoneInfo.Local);
                var nightLocal = TimeZoneInfo.ConvertTimeFromUtc(night, TimeZoneInfo.Local);
                var nauticalDawnLocal = TimeZoneInfo.ConvertTimeFromUtc(nauticalDawn, TimeZoneInfo.Local);
                var dawnLocal = TimeZoneInfo.ConvertTimeFromUtc(dawn, TimeZoneInfo.Local);
                // Sunrise: Sunrise → SunriseEnd
                var sunriseWindow = new OptimalShootingTime
                {
                    StartTime = sunriseLocal,
                    EndTime = sunriseEndLocal,
                    LightQuality = LightQuality.GoldenHour,
                    QualityScore = 0.95,
                    Description = "Sunrise",
                    IdealFor = new List<string> { "Sunrise Photography", "Landscapes", "Portraits" }
                };
                optimalTimes.Add(sunriseWindow);
                // Golden Hour Morning: SunriseEnd → SunriseEnd + 1 hour
                var goldenHourMorningWindow = new OptimalShootingTime
                {
                    StartTime = sunriseEndLocal,
                    EndTime = sunriseEndLocal.AddHours(1),
                    LightQuality = LightQuality.GoldenHour,
                    QualityScore = 0.95,
                    Description = "Golden Hour Morning",
                    IdealFor = new List<string> { "Portraits", "Landscapes", "Golden Hour" }
                };
                // Solar Noon: SolarNoon (point event)
                var solarNoonWindow = new OptimalShootingTime
                {
                    StartTime = solarNoonLocal,
                    EndTime = solarNoonLocal.AddMinutes(30),
                    LightQuality = LightQuality.Harsh,
                    QualityScore = 0.4,
                    Description = "Solar Noon",
                    IdealFor = new List<string> { "Architecture", "Minimal Shadows" }
                };
                optimalTimes.Add(solarNoonWindow);
                optimalTimes.Add(goldenHourMorningWindow);

                // Golden Hour Evening: GoldenHour → SunsetStart
                var goldenHourEveningWindow = new OptimalShootingTime
                {
                    StartTime = goldenHourLocal,
                    EndTime = sunsetStartLocal,
                    LightQuality = LightQuality.GoldenHour,
                    QualityScore = 0.95,
                    Description = "Golden Hour Evening",
                    IdealFor = new List<string> { "Portraits", "Landscapes", "Golden Hour" }
                };
                optimalTimes.Add(goldenHourEveningWindow);
                // Sunset: SunsetStart → Sunset
                var sunsetWindow = new OptimalShootingTime
                {
                    StartTime = sunsetStartLocal,
                    EndTime = sunsetLocal,
                    LightQuality = LightQuality.GoldenHour,
                    QualityScore = 0.95,
                    Description = "Sunset",
                    IdealFor = new List<string> { "Sunset Photography", "Landscapes", "Silhouettes" }
                };
                optimalTimes.Add(sunsetWindow);
                // Blue Hour Morning: Sunrise - 1 hour → Sunrise
                var blueHourMorningWindow = new OptimalShootingTime
                {
                    StartTime = sunriseLocal.AddHours(-1),
                    EndTime = sunriseLocal,
                    LightQuality = LightQuality.BlueHour,
                    QualityScore = 0.9,
                    Description = "Blue Hour Morning",
                    IdealFor = new List<string> { "Blue Hour", "Cityscapes", "Landscapes" }
                };
                // Blue Hour Evening: Sunset → Sunset + 1 hour
                var blueHourEveningWindow = new OptimalShootingTime
                {
                    StartTime = sunsetLocal,
                    EndTime = sunsetLocal.AddHours(1),
                    LightQuality = LightQuality.BlueHour,
                    QualityScore = 0.9,
                    Description = "Blue Hour Evening",
                    IdealFor = new List<string> { "Blue Hour", "Cityscapes", "Architecture" }
                };
                optimalTimes.Add(blueHourEveningWindow);
                // Dusk: Dusk → NauticalDusk
                var duskWindow = new OptimalShootingTime
                {
                    StartTime = duskLocal,
                    EndTime = nauticalDuskLocal,
                    LightQuality = LightQuality.BlueHour,
                    QualityScore = 0.85,
                    Description = "Dusk",
                    IdealFor = new List<string> { "Blue Hour", "Night Cityscapes" }
                };
                optimalTimes.Add(duskWindow);

                // Nautical Dusk: NauticalDusk → Night
                var nauticalDuskWindow = new OptimalShootingTime
                {
                    StartTime = nauticalDuskLocal,
                    EndTime = nightLocal,
                    LightQuality = LightQuality.BlueHour,
                    QualityScore = 0.8,
                    Description = "Nautical Dusk",
                    IdealFor = new List<string> { "Astrophotography", "Night Photography" }
                };
                optimalTimes.Add(nauticalDuskWindow);

                // Night: Night → NauticalDawn
                var nightWindow = new OptimalShootingTime
                {
                    StartTime = nightLocal,
                    EndTime = nauticalDawnLocal,
                    LightQuality = LightQuality.Night,
                    QualityScore = 0.7,
                    Description = "Night",
                    IdealFor = new List<string> { "Astrophotography", "Night Photography", "Long Exposure" }
                };
                optimalTimes.Add(nightWindow);

                // Nautical Dawn: NauticalDawn → Dawn
                var nauticalDawnWindow = new OptimalShootingTime
                {
                    StartTime = nauticalDawnLocal,
                    EndTime = dawnLocal,
                    LightQuality = LightQuality.BlueHour,
                    QualityScore = 0.85,
                    Description = "Nautical Dawn",
                    IdealFor = new List<string> { "Blue Hour", "Dawn Photography" }
                };
                optimalTimes.Add(nauticalDawnWindow);



                optimalTimes.Add(blueHourMorningWindow);
                // Order by StartTime
                var sortedTimes = optimalTimes
                    .OrderBy(t => t.StartTime)
                    .ToList();

                return await Task.FromResult(Result<List<OptimalShootingTime>>.Success(sortedTimes));
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