using Location.Core.Application.Common.Models;
using Location.Photography.Application.Services;
using Location.Photography.Domain.Services;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Location.Photography.Application.Queries.SunLocation
{
    public class GetEnhancedSunTimesQueryHandler : IRequestHandler<GetSunTimesQuery, Result<SunTimesResult>>
    {
        private readonly ISunCalculatorService _sunCalculatorService;
        private readonly ILogger<GetEnhancedSunTimesQueryHandler> _logger;

        public GetEnhancedSunTimesQueryHandler(
            ISunCalculatorService sunCalculatorService,
            ILogger<GetEnhancedSunTimesQueryHandler> logger)
        {
            _sunCalculatorService = sunCalculatorService ?? throw new ArgumentNullException(nameof(sunCalculatorService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<SunTimesResult>> Handle(GetSunTimesQuery request, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Get timezone for coordinates (simplified - in production would use proper timezone lookup)
                var timezone = TimeZoneInfo.Local; // TODO: Implement proper coordinate to timezone lookup

                // Calculate all sun times with enhanced precision
                var sunTimes = new SunTimesResult
                {
                    Sunrise = _sunCalculatorService.GetSunrise(request.Date, request.Latitude, request.Longitude),
                    Sunset = _sunCalculatorService.GetSunset(request.Date, request.Latitude, request.Longitude),
                    SolarNoon = _sunCalculatorService.GetSolarNoon(request.Date, request.Latitude, request.Longitude),
                    CivilDawn = _sunCalculatorService.GetCivilDawn(request.Date, request.Latitude, request.Longitude),
                    CivilDusk = _sunCalculatorService.GetCivilDusk(request.Date, request.Latitude, request.Longitude),
                    NauticalDawn = _sunCalculatorService.GetNauticalDawn(request.Date, request.Latitude, request.Longitude),
                    NauticalDusk = _sunCalculatorService.GetNauticalDusk(request.Date, request.Latitude, request.Longitude),
                    AstronomicalDawn = _sunCalculatorService.GetAstronomicalDawn(request.Date, request.Latitude, request.Longitude),
                    AstronomicalDusk = _sunCalculatorService.GetAstronomicalDusk(request.Date, request.Latitude, request.Longitude),
                    TimeZone = timezone
                };

                return await Task.FromResult(Result<SunTimesResult>.Success(sunTimes));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating enhanced sun times for coordinates {Latitude}, {Longitude} on {Date}",
                    request.Latitude, request.Longitude, request.Date);
                return Result<SunTimesResult>.Failure($"Error calculating sun times: {ex.Message}");
            }
        }
    }

    public class GetMoonDataQueryHandler : IRequestHandler<GetMoonDataQuery, Result<MoonPhaseData>>
    {
        private readonly ILogger<GetMoonDataQueryHandler> _logger;

        public GetMoonDataQueryHandler(ILogger<GetMoonDataQueryHandler> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<MoonPhaseData>> Handle(GetMoonDataQuery request, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Simplified moon calculation - in production would use proper lunar algorithm
                var moonData = new MoonPhaseData
                {
                    Date = request.Date,
                    Phase = CalculateMoonPhase(request.Date),
                    PhaseName = GetMoonPhaseName(CalculateMoonPhase(request.Date)),
                    IlluminationPercentage = CalculateMoonIllumination(request.Date),
                    MoonRise = CalculateMoonRise(request.Date, request.Latitude, request.Longitude),
                    MoonSet = CalculateMoonSet(request.Date, request.Latitude, request.Longitude),
                    Position = CalculateMoonPosition(request.Date, request.Latitude, request.Longitude),
                    Brightness = CalculateMoonBrightness(CalculateMoonPhase(request.Date))
                };

                return await Task.FromResult(Result<MoonPhaseData>.Success(moonData));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating moon data for coordinates {Latitude}, {Longitude} on {Date}",
                    request.Latitude, request.Longitude, request.Date);
                return Result<MoonPhaseData>.Failure($"Error calculating moon data: {ex.Message}");
            }
        }

        private double CalculateMoonPhase(DateTime date)
        {
            // Simplified lunar phase calculation
            var newMoonDate = new DateTime(2024, 1, 11); // Known new moon date
            var daysSinceNewMoon = (date - newMoonDate).TotalDays;
            var lunarCycle = 29.53058867; // Average lunar cycle length
            var phase = (daysSinceNewMoon % lunarCycle) / lunarCycle;
            return phase < 0 ? phase + 1 : phase;
        }

        private string GetMoonPhaseName(double phase)
        {
            return phase switch
            {
                < 0.03 or > 0.97 => "New Moon",
                < 0.22 => "Waxing Crescent",
                < 0.28 => "First Quarter",
                < 0.47 => "Waxing Gibbous",
                < 0.53 => "Full Moon",
                < 0.72 => "Waning Gibbous",
                < 0.78 => "Third Quarter",
                _ => "Waning Crescent"
            };
        }

        private double CalculateMoonIllumination(DateTime date)
        {
            var phase = CalculateMoonPhase(date);
            return phase <= 0.5 ? phase * 2 * 100 : (1 - phase) * 2 * 100;
        }

        private DateTime? CalculateMoonRise(DateTime date, double latitude, double longitude)
        {
            // Simplified - would use proper lunar position algorithm
            return date.Date.AddHours(20.5 + (latitude / 15)); // Rough approximation
        }

        private DateTime? CalculateMoonSet(DateTime date, double latitude, double longitude)
        {
            // Simplified - would use proper lunar position algorithm
            return date.Date.AddHours(6.5 + (latitude / 15)); // Rough approximation
        }

        private MoonPosition CalculateMoonPosition(DateTime date, double latitude, double longitude)
        {
            // Simplified moon position calculation
            var phase = CalculateMoonPhase(date);
            return new MoonPosition
            {
                Azimuth = phase * 360, // Simplified
                Elevation = 45 - Math.Abs(latitude) / 2, // Simplified
                Distance = 384400, // Average distance to moon in km
                IsAboveHorizon = true // Simplified
            };
        }

        private double CalculateMoonBrightness(double phase)
        {
            // Moon magnitude calculation (simplified)
            return -12.74 + 0.026 * Math.Abs(phase - 0.5) * 180; // Simplified magnitude
        }
    }

    public class GetSunPathDataQueryHandler : IRequestHandler<GetSunPathDataQuery, Result<SunPathDataResult>>
    {
        private readonly ISunCalculatorService _sunCalculatorService;
        private readonly ILogger<GetSunPathDataQueryHandler> _logger;

        public GetSunPathDataQueryHandler(
            ISunCalculatorService sunCalculatorService,
            ILogger<GetSunPathDataQueryHandler> logger)
        {
            _sunCalculatorService = sunCalculatorService ?? throw new ArgumentNullException(nameof(sunCalculatorService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<SunPathDataResult>> Handle(GetSunPathDataQuery request, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var pathPoints = new List<SunPathPoint>();
                var startTime = request.Date.Date;
                var endTime = startTime.AddDays(1);
                var interval = TimeSpan.FromMinutes(request.IntervalMinutes);

                double maxElevation = -90;
                DateTime maxElevationTime = startTime;
                double sunriseAzimuth = 0;
                double sunsetAzimuth = 0;

                // Calculate sun position for each interval
                for (var time = startTime; time < endTime; time = time.Add(interval))
                {
                    var azimuth = _sunCalculatorService.GetSolarAzimuth(time, request.Latitude, request.Longitude);
                    var elevation = _sunCalculatorService.GetSolarElevation(time, request.Latitude, request.Longitude);

                    pathPoints.Add(new SunPathPoint
                    {
                        Time = time,
                        Azimuth = azimuth,
                        Elevation = elevation,
                        IsVisible = elevation > 0
                    });

                    // Track maximum elevation
                    if (elevation > maxElevation)
                    {
                        maxElevation = elevation;
                        maxElevationTime = time;
                    }

                    // Capture sunrise/sunset azimuths
                    if (Math.Abs(elevation) < 0.5) // Near horizon
                    {
                        if (time.Hour < 12)
                            sunriseAzimuth = azimuth;
                        else
                            sunsetAzimuth = azimuth;
                    }
                }

                // Current sun position
                var currentPosition = new SunPathPoint
                {
                    Time = DateTime.Now,
                    Azimuth = _sunCalculatorService.GetSolarAzimuth(DateTime.Now, request.Latitude, request.Longitude),
                    Elevation = _sunCalculatorService.GetSolarElevation(DateTime.Now, request.Latitude, request.Longitude),
                    IsVisible = _sunCalculatorService.GetSolarElevation(DateTime.Now, request.Latitude, request.Longitude) > 0
                };

                // Calculate daylight duration
                var sunrise = _sunCalculatorService.GetSunrise(request.Date, request.Latitude, request.Longitude);
                var sunset = _sunCalculatorService.GetSunset(request.Date, request.Latitude, request.Longitude);
                var daylightDuration = sunset - sunrise;

                var result = new SunPathDataResult
                {
                    PathPoints = pathPoints,
                    CurrentPosition = currentPosition,
                    Date = request.Date,
                    Latitude = request.Latitude,
                    Longitude = request.Longitude,
                    Metrics = new SunPathMetrics
                    {
                        DaylightDuration = daylightDuration,
                        MaxElevation = maxElevation,
                        MaxElevationTime = maxElevationTime,
                        SunriseAzimuth = sunriseAzimuth,
                        SunsetAzimuth = sunsetAzimuth,
                        SeasonalNote = GenerateSeasonalNote(request.Date, request.Latitude)
                    }
                };

                return await Task.FromResult(Result<SunPathDataResult>.Success(result));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating sun path data for coordinates {Latitude}, {Longitude} on {Date}",
                    request.Latitude, request.Longitude, request.Date);
                return Result<SunPathDataResult>.Failure($"Error calculating sun path: {ex.Message}");
            }
        }

        private string GenerateSeasonalNote(DateTime date, double latitude)
        {
            bool isNorthernHemisphere = latitude > 0;
            int dayOfYear = date.DayOfYear;

            return (dayOfYear, isNorthernHemisphere) switch
            {
                ( < 80 or > 355, true) => "Winter: Short days, low sun angle",
                ( < 80 or > 355, false) => "Summer: Long days, high sun angle",
                ( >= 80 and < 172, true) => "Spring: Days getting longer",
                ( >= 80 and < 172, false) => "Autumn: Days getting shorter",
                ( >= 172 and < 266, true) => "Summer: Long days, high sun angle",
                ( >= 172 and < 266, false) => "Winter: Short days, low sun angle",
                ( >= 266 and <= 355, true) => "Autumn: Days getting shorter",
                _ => "Spring: Days getting longer"
            };
        }
    }

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

                // Calculate key sun times
                var sunrise = _sunCalculatorService.GetSunrise(request.Date, request.Latitude, request.Longitude);
                var sunset = _sunCalculatorService.GetSunset(request.Date, request.Latitude, request.Longitude);
                var civilDawn = _sunCalculatorService.GetCivilDawn(request.Date, request.Latitude, request.Longitude);
                var civilDusk = _sunCalculatorService.GetCivilDusk(request.Date, request.Latitude, request.Longitude);

                // Blue Hour Morning
                optimalTimes.Add(new OptimalShootingTime
                {
                    StartTime = civilDawn,
                    EndTime = sunrise,
                    LightQuality = LightQuality.BlueHour,
                    QualityScore = 0.9,
                    Description = "Blue Hour - Even, soft blue light ideal for cityscapes and landscapes",
                    IdealFor = new List<string> { "Cityscapes", "Landscapes", "Architecture" }
                });

                // Golden Hour Morning
                optimalTimes.Add(new OptimalShootingTime
                {
                    StartTime = sunrise,
                    EndTime = sunrise.AddMinutes(60),
                    LightQuality = LightQuality.GoldenHour,
                    QualityScore = 0.95,
                    Description = "Golden Hour - Warm, soft light with long shadows",
                    IdealFor = new List<string> { "Portraits", "Landscapes", "Nature" }
                });

                // Golden Hour Evening
                optimalTimes.Add(new OptimalShootingTime
                {
                    StartTime = sunset.AddMinutes(-60),
                    EndTime = sunset,
                    LightQuality = LightQuality.GoldenHour,
                    QualityScore = 0.95,
                    Description = "Golden Hour - Warm, dramatic lighting",
                    IdealFor = new List<string> { "Portraits", "Landscapes", "Silhouettes" }
                });

                // Blue Hour Evening
                optimalTimes.Add(new OptimalShootingTime
                {
                    StartTime = sunset,
                    EndTime = civilDusk,
                    LightQuality = LightQuality.BlueHour,
                    QualityScore = 0.9,
                    Description = "Blue Hour - Deep blue sky with artificial lights beginning to show",
                    IdealFor = new List<string> { "Cityscapes", "Architecture", "Street Photography" }
                });

                // Sort by start time
                optimalTimes = optimalTimes.OrderBy(t => t.StartTime).ToList();

                return await Task.FromResult(Result<List<OptimalShootingTime>>.Success(optimalTimes));
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

    public class GetShadowCalculationQueryHandler : IRequestHandler<GetShadowCalculationQuery, Result<ShadowCalculationResult>>
    {
        private readonly ISunCalculatorService _sunCalculatorService;
        private readonly ILogger<GetShadowCalculationQueryHandler> _logger;

        public GetShadowCalculationQueryHandler(
            ISunCalculatorService sunCalculatorService,
            ILogger<GetShadowCalculationQueryHandler> logger)
        {
            _sunCalculatorService = sunCalculatorService ?? throw new ArgumentNullException(nameof(sunCalculatorService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<ShadowCalculationResult>> Handle(GetShadowCalculationQuery request, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var sunElevation = _sunCalculatorService.GetSolarElevation(request.DateTime, request.Latitude, request.Longitude);
                var sunAzimuth = _sunCalculatorService.GetSolarAzimuth(request.DateTime, request.Latitude, request.Longitude);

                // Calculate shadow length using trigonometry
                var shadowLength = sunElevation > 0
                    ? request.ObjectHeight / Math.Tan(sunElevation * Math.PI / 180.0)
                    : double.MaxValue; // No shadow when sun is below horizon

                // Shadow direction is opposite to sun azimuth
                var shadowDirection = (sunAzimuth + 180) % 360;

                // Apply terrain factor
                var terrainMultiplier = GetTerrainMultiplier(request.TerrainType);
                shadowLength *= terrainMultiplier;

                // Calculate shadow progression throughout the day
                var shadowProgression = new List<ShadowTimePoint>();
                var startTime = request.DateTime.Date.AddHours(6);
                var endTime = request.DateTime.Date.AddHours(18);

                for (var time = startTime; time <= endTime; time = time.AddHours(1))
                {
                    var elevation = _sunCalculatorService.GetSolarElevation(time, request.Latitude, request.Longitude);
                    var azimuth = _sunCalculatorService.GetSolarAzimuth(time, request.Latitude, request.Longitude);

                    if (elevation > 0)
                    {
                        shadowProgression.Add(new ShadowTimePoint
                        {
                            Time = time,
                            Length = request.ObjectHeight / Math.Tan(elevation * Math.PI / 180.0) * terrainMultiplier,
                            Direction = (azimuth + 180) % 360
                        });
                    }
                }

                var result = new ShadowCalculationResult
                {
                    ShadowLength = shadowLength,
                    ShadowDirection = shadowDirection,
                    ObjectHeight = request.ObjectHeight,
                    CalculationTime = request.DateTime,
                    Terrain = request.TerrainType,
                    ShadowProgression = shadowProgression
                };

                return await Task.FromResult(Result<ShadowCalculationResult>.Success(result));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating shadow data for coordinates {Latitude}, {Longitude} at {DateTime}",
                    request.Latitude, request.Longitude, request.DateTime);
                return Result<ShadowCalculationResult>.Failure($"Error calculating shadows: {ex.Message}");
            }
        }

        private double GetTerrainMultiplier(TerrainType terrain)
        {
            return terrain switch
            {
                TerrainType.Flat => 1.0,
                TerrainType.Urban => 0.8, // Buildings create partial shadows
                TerrainType.Forest => 0.6, // Trees create dappled shadows  
                TerrainType.Mountain => 1.2, // Higher elevation can extend shadows
                TerrainType.Beach => 1.1, // Reflective surfaces can affect shadow intensity
                _ => 1.0
            };
        }
    }
}