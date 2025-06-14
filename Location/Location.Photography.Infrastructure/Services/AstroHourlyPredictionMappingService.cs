using Location.Core.Application.Weather.Queries.GetHourlyForecast;
using Location.Photography.Application.Common.Interfaces;
using Location.Photography.Application.DTOs;
using Location.Photography.Application.Services;
using Location.Photography.Domain.Entities;
using Location.Photography.Domain.Models;
using Location.Photography.Domain.Services;
using Location.Photography.Infrastructure.Resources;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Location.Photography.ViewModels.AstroPhotographyCalculatorViewModel;
using WeatherDto = Location.Photography.Application.DTOs.WeatherDto;

namespace Location.Photography.Infrastructure.Services
{
    public class AstroHourlyPredictionMappingService : Location.Photography.Application.Common.Interfaces.IAstroHourlyPredictionMappingService
    {
        private readonly ILogger<AstroHourlyPredictionMappingService> _logger;
        private readonly IAstroCalculationService _astroCalculationService;
        private readonly IEquipmentRecommendationService _equipmentRecommendationService;
        private readonly ISunCalculatorService _sunCalculatorService;
        private readonly IExposureCalculatorService _exposureCalculatorService;
        private readonly IPredictiveLightService _predictiveLightService;
        private readonly IMediator _mediator;
        private readonly ICameraBodyRepository _cameraBodyRepository;
        private readonly ILensRepository _lensRepository;
        public AstroHourlyPredictionMappingService(
            ILogger<AstroHourlyPredictionMappingService> logger,
            IAstroCalculationService astroCalculationService,
            IEquipmentRecommendationService equipmentRecommendationService,
            ISunCalculatorService sunCalculatorService,
            IExposureCalculatorService exposureCalculatorService,
            IPredictiveLightService predictiveLightService,
            IMediator mediator,
            ICameraBodyRepository cameraBodyRepository,  // ADD THIS
    ILensRepository lensRepository)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _astroCalculationService = astroCalculationService ?? throw new ArgumentNullException(nameof(astroCalculationService));
            _equipmentRecommendationService = equipmentRecommendationService ?? throw new ArgumentNullException(nameof(equipmentRecommendationService));
            _sunCalculatorService = sunCalculatorService ?? throw new ArgumentNullException(nameof(sunCalculatorService));
            _exposureCalculatorService = exposureCalculatorService ?? throw new ArgumentNullException(nameof(exposureCalculatorService));
            _predictiveLightService = predictiveLightService ?? throw new ArgumentNullException(nameof(predictiveLightService));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _cameraBodyRepository = cameraBodyRepository ?? throw new ArgumentNullException(nameof(cameraBodyRepository));
            _lensRepository = lensRepository ?? throw new ArgumentNullException(nameof(lensRepository));
        }

        public async Task<List<AstroHourlyPredictionDto>> MapFromDomainDataAsync(
            List<AstroCalculationResult> calculationResults,
            double latitude,
            double longitude,
            DateTime selectedDate)
        {
            var predictions = new List<AstroHourlyPredictionDto>();

            try
            {
                // Group calculation results by hour
                var groupedByHour = calculationResults.GroupBy(r => r.CalculationTime.Hour);

                foreach (var hourGroup in groupedByHour)
                {
                    var hour = new DateTime(selectedDate.Year, selectedDate.Month, selectedDate.Day, hourGroup.Key, 0, 0);
                    var prediction = await MapSingleCalculationAsync(hourGroup.ToList(), latitude, longitude, selectedDate);
                    predictions.Add(prediction);
                }

                return predictions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error mapping from domain data");
                return new List<AstroHourlyPredictionDto>();
            }
        }

        public async Task<AstroHourlyPredictionDto> MapSingleCalculationAsync(
            List<AstroCalculationResult> calculationResults,
            double latitude,
            double longitude,
            DateTime selectedDate)
        {
            try
            {
                var hour = calculationResults.First().CalculationTime;

                // Get solar events for this hour
                var solarEvent = await GetSolarEventForHourAsync(hour, latitude, longitude);

                // Calculate overall quality score
                var qualityScore = await CalculateHourQualityAsync(hour, latitude, longitude, calculationResults);

                // Generate astro events from calculation results
                var astroEvents = await GenerateAstroEventsFromCalculationsAsync(calculationResults, hour, latitude, longitude);

                // Create basic DTO structure
                var dto = new AstroHourlyPredictionDto
                {
                    Hour = hour,
                    TimeDisplay = hour.ToString("h:mm tt"),
                    SolarEvent = solarEvent,
                    SolarEventsDisplay = solarEvent,
                    QualityScore = qualityScore,
                    QualityDisplay = GetQualityDisplay(qualityScore),
                    QualityDescription = GetQualityDescription(qualityScore),
                    AstroEvents = astroEvents,
                    Weather = await GetWeatherDtoAsync(hour, latitude, longitude)
                };

                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error mapping single calculation");
                return CreateDefaultDto(calculationResults.First().CalculationTime);
            }
        }

        public async Task<List<AstroHourlyPredictionDto>> GenerateHourlyPredictionsAsync(
            DateTime startTime,
            DateTime endTime,
            double latitude,
            double longitude,
            DateTime selectedDate)
        {
            var predictions = new List<AstroHourlyPredictionDto>();

            try
            {
                var currentHour = startTime;
                while (currentHour <= endTime)
                {
                    var prediction = await GeneratePredictionForHourAsync(currentHour, latitude, longitude, selectedDate);
                    if (prediction != null)
                    {
                        predictions.Add(prediction);
                    }
                    currentHour = currentHour.AddHours(1);
                }

                return predictions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating hourly predictions");
                return new List<AstroHourlyPredictionDto>();
            }
        }

        private async Task<AstroHourlyPredictionDto> GeneratePredictionForHourAsync(
   DateTime hour,
   double latitude,
   double longitude,
   DateTime selectedDate)
        {
            try
            {
                // Get solar events
                var solarEvent = await GetSolarEventForHourAsync(hour, latitude, longitude);

                // Calculate quality score
                var qualityScore = await CalculateHourQualityAsync(hour, latitude, longitude, new List<AstroCalculationResult>());

                // Generate basic prediction using enhanced primary method with empty calculation results
                var dto = new AstroHourlyPredictionDto
                {
                    Hour = hour,
                    TimeDisplay = hour.ToString("h:mm tt"),
                    SolarEvent = solarEvent,
                    SolarEventsDisplay = solarEvent,
                    QualityScore = qualityScore,
                    QualityDisplay = GetQualityDisplay(qualityScore),
                    QualityDescription = GetQualityDescription(qualityScore),
                    AstroEvents = await GenerateAstroEventsFromCalculationsAsync(new List<AstroCalculationResult>(), hour, latitude, longitude),
                    Weather = await GetWeatherDtoAsync(hour, latitude, longitude)
                };

                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error generating prediction for hour {Hour}", hour);
                return CreateDefaultDto(hour);
            }
        }

        private async Task<string> GetSolarEventForHourAsync(DateTime hour, double latitude, double longitude)
        {
            try
            {
                var date = hour.Date;
                var nextDay = date.AddDays(1);

                // Get all twilight times for the current date
                var sunset = _sunCalculatorService.GetSunset(date, latitude, longitude, "UTC");
                var civilDusk = _sunCalculatorService.GetCivilDusk(date, latitude, longitude, "UTC");
                var nauticalDusk = _sunCalculatorService.GetNauticalDusk(date, latitude, longitude, "UTC");
                var astronomicalDusk = _sunCalculatorService.GetAstronomicalDusk(date, latitude, longitude, "UTC");

                // Get dawn times for next day
                var astronomicalDawn = _sunCalculatorService.GetAstronomicalDawn(nextDay, latitude, longitude, "UTC");
                var nauticalDawn = _sunCalculatorService.GetNauticalDawn(nextDay, latitude, longitude, "UTC");
                var civilDawn = _sunCalculatorService.GetCivilDawn(nextDay, latitude, longitude, "UTC");
                var sunrise = _sunCalculatorService.GetSunrise(nextDay, latitude, longitude, "UTC");

                // Check current sun elevation for validation
                var sunElevation = _sunCalculatorService.GetSolarElevation(hour, latitude, longitude, "UTC");

                // Determine which event period we're in based on time comparison
                if (hour < sunset)
                {
                    return "Daylight";
                }
                else if (hour >= sunset && hour < civilDusk)
                {
                    return "Sunset";
                }
                else if (hour >= civilDusk && hour < nauticalDusk)
                {
                    return "Civil Twilight";
                }
                else if (hour >= nauticalDusk && hour < astronomicalDusk)
                {
                    return "Nautical Twilight";
                }
                else if (hour >= astronomicalDusk && hour < astronomicalDawn)
                {
                    return "True Night";
                }
                else if (hour >= astronomicalDawn && hour < nauticalDawn)
                {
                    return "Astronomical Twilight";
                }
                else if (hour >= nauticalDawn && hour < civilDawn)
                {
                    return "Nautical Twilight";
                }
                else if (hour >= civilDawn && hour < sunrise)
                {
                    return "Civil Twilight";
                }
                else if (hour >= sunrise)
                {
                    return "Sunrise";
                }
                else
                {
                    // Fallback to elevation-based determination
                    return sunElevation switch
                    {
                        > 0 => "Daylight",
                        > -6 => "Civil Twilight",
                        > -12 => "Nautical Twilight",
                        > -18 => "Astronomical Twilight",
                        _ => "True Night"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error determining solar event for hour {Hour}", hour);

                // Fallback to simple elevation-based determination
                try
                {
                    var sunAltitude = _sunCalculatorService.GetSolarElevation(hour, latitude, longitude, "UTC");
                    return sunAltitude switch
                    {
                        > 0 => "Daylight",
                        > -6 => "Civil Twilight",
                        > -12 => "Nautical Twilight",
                        > -18 => "Astronomical Twilight",
                        _ => "True Night"
                    };
                }
                catch
                {
                    return "Unknown";
                }
            }
        }

        private async Task<double> CalculateHourQualityAsync(DateTime hour, double latitude, double longitude, List<AstroCalculationResult> calculationResults)
        {
            try
            {
                var sunAltitude = _sunCalculatorService.GetSolarElevation(hour, latitude, longitude, "UTC");

                // Base score from solar conditions
                var score = sunAltitude switch
                {
                    < -18 => 90, // True night
                    < -12 => 70, // Astronomical twilight
                    < -6 => 50,  // Nautical twilight
                    < 0 => 30,   // Civil twilight
                    _ => 10      // Daylight
                };

                // Bonus from visible targets
                if (calculationResults.Any())
                {
                    var visibleTargets = calculationResults.Count(r => r.IsVisible);
                    var avgAltitude = calculationResults.Where(r => r.IsVisible).Select(r => r.Altitude).DefaultIfEmpty(0).Average();

                    score += (visibleTargets * 5); // Bonus for each visible target
                    if (avgAltitude > 45) score += 10; // High altitude bonus
                    else if (avgAltitude > 20) score += 5; // Moderate altitude bonus
                }

                return Math.Min(100, Math.Max(0, score));
            }
            catch
            {
                return 50; // Default moderate score
            }
        }

        private async Task<List<AstroEventDto>> GenerateAstroEventsFromCalculationsAsync(
   List<AstroCalculationResult> calculationResults,
   DateTime hour,
   double latitude,
   double longitude)
        {
            var events = new List<AstroEventDto>();

            try
            {
                // Get sun altitude to determine what targets are viable
                var sunAltitude = _sunCalculatorService.GetSolarElevation(hour, latitude, longitude, "UTC");

                // Process provided calculation results first
                var processedTargets = new HashSet<AstroTarget>();

                foreach (var result in calculationResults.Where(r => r.IsVisible))
                {
                    var equipmentRec = await GetEquipmentRecommendationsAsync(result.Target, latitude, longitude);
                    var cameraSettings = await GetDetailedCameraSettingsAsync(result.Target, result.Altitude);
                    var notes = await GetComprehensiveNotesAsync(result.Target, result, hour, latitude, longitude);

                    events.Add(new AstroEventDto
                    {
                        TargetName = GetTargetDisplayName(result.Target),
                        Visibility = $"{result.Altitude:F0}° altitude, {result.Azimuth:F0}° azimuth",
                        RecommendedEquipment = equipmentRec,
                        CameraSettings = cameraSettings,
                        Notes = notes
                    });

                    processedTargets.Add(result.Target);
                }

                // Now check for missing targets that should be available at this hour
                if (sunAltitude < -6) // Dark enough for astrophotography
                {
                    // Check Moon if not already processed
                    if (!processedTargets.Contains(AstroTarget.Moon))
                    {
                        var moonData = await GetMoonDataForHourAsync(hour, latitude, longitude);
                        if (moonData != null)
                        {
                            events.Add(moonData);
                        }
                    }

                    // Check Milky Way if not already processed
                    if (!processedTargets.Contains(AstroTarget.MilkyWayCore) && sunAltitude < -18)
                    {
                        var milkyWayData = await GetMilkyWayDataForHourAsync(hour, latitude, longitude);
                        if (milkyWayData != null)
                        {
                            events.Add(milkyWayData);
                        }
                    }

                    // Check planets if not already processed
                    if (!processedTargets.Contains(AstroTarget.Planets))
                    {
                        var visiblePlanets = await GetRealVisiblePlanetsForHourAsync(hour, latitude, longitude);
                        events.AddRange(visiblePlanets);
                    }

                    // Check DSOs if not already processed
                    if (!processedTargets.Contains(AstroTarget.DeepSkyObjects) && sunAltitude < -18)
                    {
                        var visibleDSOs = await GetRealVisibleDSOsForHourAsync(hour, latitude, longitude);
                        events.AddRange(visibleDSOs);
                    }
                }

                // Deduplicate events by target name (case-insensitive)
                return events
                    .GroupBy(e => e.TargetName.ToLowerInvariant())
                    .Select(g => g.First())
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error generating astro events from calculations");
                return new List<AstroEventDto>();
            }
        }

        private async Task<List<AstroEventDto>> GenerateAstroEventsForHourAsync(DateTime hour, double latitude, double longitude)
        {
            var events = new List<AstroEventDto>();

            try
            {
                // This method now serves as a fallback when no calculation results are provided
                // Primary event generation is handled by GenerateAstroEventsFromCalculationsAsync

                var sunAltitude = _sunCalculatorService.GetSolarElevation(hour, latitude, longitude, "UTC");

                if (sunAltitude < -6) // Dark enough for astrophotography
                {
                    // Generate minimal event set as fallback
                    var moonData = await GetMoonDataForHourAsync(hour, latitude, longitude);
                    if (moonData != null)
                    {
                        events.Add(moonData);
                    }

                    // Only check Milky Way during true night
                    if (sunAltitude < -18)
                    {
                        var milkyWayData = await GetMilkyWayDataForHourAsync(hour, latitude, longitude);
                        if (milkyWayData != null)
                        {
                            events.Add(milkyWayData);
                        }
                    }

                    // Check for visible planets
                    var visiblePlanets = await GetRealVisiblePlanetsForHourAsync(hour, latitude, longitude);
                    events.AddRange(visiblePlanets);

                    // Check for visible DSOs during true night
                    if (sunAltitude < -18)
                    {
                        var visibleDSOs = await GetRealVisibleDSOsForHourAsync(hour, latitude, longitude);
                        events.AddRange(visibleDSOs);
                    }
                }

                return events;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error generating astro events for hour");
                return new List<AstroEventDto>();
            }
        }

        private async Task<List<AstroEventDto>> GetRealVisiblePlanetsForHourAsync(DateTime hour, double latitude, double longitude)
        {
            var planetEvents = new List<AstroEventDto>();

            try
            {
                var planets = await _astroCalculationService.GetVisiblePlanetsAsync(hour, latitude, longitude);

                // REAL filtering - only planets above horizon AND visible
                var visiblePlanets = planets.Where(p => p.IsVisible && p.Altitude > 10).ToList(); // 10° minimum for good viewing

                foreach (var planet in visiblePlanets.OrderByDescending(p => p.Altitude))
                {
                    var equipmentRec = await GetPlanetEquipmentRecommendationAsync(planet);
                    var cameraSettings = await GetPlanetCameraSettingsAsync(planet);
                    var notes = GetPlanetNotes(planet);

                    planetEvents.Add(new AstroEventDto
                    {
                        TargetName = planet.Planet.ToString(),
                        Visibility = $"{planet.Altitude:F0}° altitude, {planet.Azimuth:F0}° azimuth",
                        RecommendedEquipment = equipmentRec,
                        CameraSettings = cameraSettings,
                        Notes = notes
                    });
                }

                return planetEvents;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting real visible planets for hour");
                return new List<AstroEventDto>();
            }
        }

        private async Task<string> GetPlanetEquipmentRecommendationAsync(PlanetPositionData planet)
        {
            try
            {
                var genericRecommendation = await _equipmentRecommendationService.GetGenericRecommendationAsync(
                    AstroTarget.Planets,
                    CancellationToken.None);

                if (genericRecommendation.IsSuccess && genericRecommendation.Data != null)
                {
                    return genericRecommendation.Data.LensRecommendation;
                }

                return "Telephoto lens 300-1000mm, f/5.6 or faster";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting planet equipment recommendation for {Planet}", planet.Planet);
                return "Telephoto lens 300-1000mm, f/5.6 or faster";
            }
        }

        private async Task<string> GetPlanetCameraSettingsAsync(PlanetPositionData planet)
        {
            try
            {
                // Calculate base settings for planets
                var baseSettings = new BaseCameraSettings
                {
                    Aperture = 5.6,
                    ShutterSpeed = 0.017, // 1/60
                    ISO = 800
                };

                // Adjust for planet brightness
                if (planet.ApparentMagnitude < -2) // Very bright planets like Venus
                {
                    baseSettings.ISO = 400;
                    baseSettings.ShutterSpeed = 0.008; // 1/125
                }
                else if (planet.ApparentMagnitude > 2) // Dimmer planets
                {
                    baseSettings.ISO = 1600;
                    baseSettings.ShutterSpeed = 0.033; // 1/30
                }

                // Get standardized lists
                var allApertures = ViewModels.Interfaces.Apetures.Thirds.Select(a => Convert.ToDouble(a.Replace("f/", ""))).ToList();
                var allShutterSpeeds = ViewModels.Interfaces.ShutterSpeeds.Thirds.Select(s => ParseShutterSpeed(s)).ToList();

                // Find closest values from standard lists
                var closestAperture = allApertures.OrderBy(x => Math.Abs(x - baseSettings.Aperture)).First();
                var closestShutter = allShutterSpeeds.OrderBy(x => Math.Abs(x - baseSettings.ShutterSpeed)).First();

                // Use ExposureCalculatorService to normalize the triangle
                var exposureDto = new ExposureTriangleDto
                {
                    Aperture = $"f/{baseSettings.Aperture}",
                    Iso = baseSettings.ISO.ToString(),
                    ShutterSpeed = FormatShutterSpeed(baseSettings.ShutterSpeed)
                };

                var normalizedResult = await _exposureCalculatorService.CalculateIsoAsync(
                    exposureDto,
                    FormatShutterSpeed(closestShutter),
                    $"f/{closestAperture}",
                    ExposureIncrements.Third);

                if (normalizedResult.IsSuccess && normalizedResult.Data != null)
                {
                    return $"f/{normalizedResult.Data.Aperture}, {normalizedResult.Data.ShutterSpeed}, ISO {normalizedResult.Data.Iso}";
                }

                // Fallback to base settings
                return $"f/{closestAperture:F1}, {FormatShutterSpeed(closestShutter)}, ISO {baseSettings.ISO}";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting planet camera settings for {Planet}", planet.Planet);
                return "f/5.6, 1/60, ISO 800";
            }
        }

        private string GetPlanetNotes(PlanetPositionData planet)
        {
            try
            {
                var notes = new List<string>();

                // Planet-specific notes
                switch (planet.Planet.ToString().ToLower())
                {
                    case "venus":
                        notes.Add("Brightest planet - use faster shutter speeds");
                        notes.Add("Best during twilight when contrast is manageable");
                        break;
                    case "mars":
                        notes.Add("Reddish color - adjust white balance");
                        notes.Add("Shows surface features with long focal lengths");
                        break;
                    case "jupiter":
                        notes.Add("Look for Great Red Spot and moons");
                        notes.Add("Galilean moons visible with telephoto lenses");
                        break;
                    case "saturn":
                        notes.Add("Rings visible with 600mm+ focal length");
                        notes.Add("Golden color - beautiful against dark sky");
                        break;
                    default:
                        notes.Add("Use maximum focal length available");
                        break;
                }

                // Altitude-based notes
                if (planet.Altitude > 60)
                {
                    notes.Add("Excellent altitude - minimal atmospheric distortion");
                }
                else if (planet.Altitude > 30)
                {
                    notes.Add("Good altitude for clear imaging");
                }
                else if (planet.Altitude > 15)
                {
                    notes.Add("Low altitude - atmospheric effects may reduce clarity");
                }

                // Magnitude-based notes
                if (planet.ApparentMagnitude < -2)
                {
                    notes.Add("Very bright - easy target for beginners");
                }
                else if (planet.ApparentMagnitude > 2)
                {
                    notes.Add("Dimmer planet - requires steady mount and longer exposures");
                }

                return string.Join(". ", notes);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting planet notes for {Planet}", planet.Planet);
                return $"{planet.Planet} photography - use telephoto lens for best detail";
            }
        }

        // Helper methods
        private double ParseShutterSpeed(string shutterSpeed)
        {
            try
            {
                if (shutterSpeed.Contains("/"))
                {
                    var parts = shutterSpeed.Replace("1/", "").Split('/');
                    if (parts.Length == 1 && double.TryParse(parts[0], out var denominator))
                    {
                        return 1.0 / denominator;
                    }
                }
                else if (shutterSpeed.Contains("\""))
                {
                    var seconds = shutterSpeed.Replace("\"", "");
                    if (double.TryParse(seconds, out var sec))
                    {
                        return sec;
                    }
                }
                else if (double.TryParse(shutterSpeed, out var value))
                {
                    return value;
                }

                return 1.0 / 60.0; // Default fallback
            }
            catch
            {
                return 1.0 / 60.0;
            }
        }

        private string FormatShutterSpeed(double seconds)
        {
            if (seconds >= 1)
                return $"{seconds:F0}\"";
            else
                return $"1/{Math.Round(1.0 / seconds):F0}";
        }

        // Supporting class
        public class BaseCameraSettings
        {
            public double Aperture { get; set; }
            public double ShutterSpeed { get; set; }
            public int ISO { get; set; }
        }

        private async Task<List<AstroEventDto>> GetRealVisibleDSOsForHourAsync(DateTime hour, double latitude, double longitude)
        {
            var visibleDSOEvents = new List<AstroEventDto>();

            try
            {
                // Complete catalog of prominent DSOs to check
                var dsoList = new[]
                {
            // Messier Objects
            "M1", "M8", "M13", "M16", "M17", "M20", "M27", "M31", "M33", "M42", "M43", "M44", "M45",
            "M51", "M57", "M65", "M66", "M81", "M82", "M87", "M101", "M104", "M106", "M108", "M109",
            
            // Prominent NGC Objects
            "NGC 7000", "NGC 6960", "NGC 6992", "NGC 2024", "NGC 7635", "NGC 281", "NGC 7380",
            "NGC 6888", "NGC 7023", "NGC 1499", "NGC 2237", "NGC 6302", "NGC 7293", "NGC 246",
            "NGC 253", "NGC 5128", "NGC 4565", "NGC 891", "NGC 4631", "NGC 7331"
        };

                var visibleDSOs = new List<DeepSkyObjectData>();

                // Check each DSO for real-time visibility
                foreach (var dsoId in dsoList)
                {
                    try
                    {
                        var dso = await _astroCalculationService.GetDeepSkyObjectDataAsync(dsoId, hour, latitude, longitude);

                        // REAL visibility check - must be above horizon AND visible
                        if (dso.IsVisible && dso.Altitude > 0)
                        {
                            visibleDSOs.Add(dso);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Error checking DSO {DSO} visibility at {Hour}", dsoId, hour);
                        // Continue checking other DSOs even if one fails
                    }
                }

                // Create individual event records for each visible DSO
                foreach (var dso in visibleDSOs.OrderByDescending(d => d.Altitude)) // Show highest altitude first
                {
                    var equipmentRec = await GetDSOEquipmentRecommendationAsync(dso);
                    var cameraSettings = await GetDSOCameraSettingsAsync(dso);
                    var notes = GetDSONotes(dso);

                    visibleDSOEvents.Add(new AstroEventDto
                    {
                        TargetName = $"{dso.CatalogId} {dso.CommonName}",
                        Visibility = $"{dso.Altitude:F0}° altitude, {dso.Azimuth:F0}° azimuth",
                        RecommendedEquipment = equipmentRec,
                        CameraSettings = cameraSettings,
                        Notes = notes
                    });
                }

                _logger.LogInformation("Found {Count} visible DSOs at {Hour}: {DSOs}",
                    visibleDSOEvents.Count,
                    hour.ToString("HH:mm"),
                    string.Join(", ", visibleDSOEvents.Select(e => e.TargetName)));

                return visibleDSOEvents;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting real visible DSOs for hour {Hour}", hour);
                return new List<AstroEventDto>();
            }
        }

        private async Task<AstroEventDto> GetMoonDataForHourAsync(DateTime hour, double latitude, double longitude)
        {
            try
            {
                var moonData = await _astroCalculationService.GetEnhancedMoonDataAsync(hour, latitude, longitude);

                if (moonData.Altitude > 10) // Moon is well above horizon
                {
                    var equipmentRec = await GetEquipmentRecommendationsAsync(AstroTarget.Moon, latitude, longitude);
                    var cameraSettings = await GetDetailedCameraSettingsAsync(AstroTarget.Moon, moonData.Altitude);
                    var notes = $"Moon phase: {moonData.PhaseName} ({moonData.Illumination:F0}% illuminated). {moonData.OptimalPhotographyPhase}. {moonData.RecommendedExposureSettings}";

                    return new AstroEventDto
                    {
                        TargetName = $"Moon ({moonData.PhaseName})",
                        Visibility = $"{moonData.Altitude:F0}° altitude, {moonData.Azimuth:F0}° azimuth",
                        RecommendedEquipment = equipmentRec,
                        CameraSettings = cameraSettings,
                        Notes = notes
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting moon data for hour");
            }

            return null;
        }

        private async Task<AstroEventDto> GetMilkyWayDataForHourAsync(DateTime hour, double latitude, double longitude)
        {
            try
            {
                var milkyWayData = await _astroCalculationService.GetMilkyWayDataAsync(hour, latitude, longitude);

                if (milkyWayData.IsVisible && milkyWayData.GalacticCenterAltitude > 20)
                {
                    var equipmentRec = await GetEquipmentRecommendationsAsync(AstroTarget.MilkyWayCore, latitude, longitude);
                    var cameraSettings = await GetDetailedCameraSettingsAsync(AstroTarget.MilkyWayCore, milkyWayData.GalacticCenterAltitude);
                    var notes = $"Galactic center visible. {milkyWayData.Season}. Dark sky quality: {milkyWayData.DarkSkyQuality:P0}. {milkyWayData.PhotographyRecommendations}";

                    return new AstroEventDto
                    {
                        TargetName = "Milky Way Core",
                        Visibility = $"{milkyWayData.GalacticCenterAltitude:F0}° altitude, {milkyWayData.GalacticCenterAzimuth:F0}° azimuth",
                        RecommendedEquipment = equipmentRec,
                        CameraSettings = cameraSettings,
                        Notes = notes
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting Milky Way data for hour");
            }

            return null;
        }

        private string GetDSONotes(DeepSkyObjectData dso)
        {
            var notes = new List<string>();

            switch (dso.ObjectType)
            {
                case "Galaxy":
                    notes.Add($"Large galaxy spanning {dso.AngularSize:F0} arcminutes");
                    notes.Add("Requires dark skies and tracking mount for best results");
                    if (dso.AngularSize > 60)
                        notes.Add("Wide field target - consider panoramic imaging");
                    break;

                case "Nebula":
                    notes.Add($"Emission nebula, {dso.AngularSize:F0} arcminutes apparent size");
                    notes.Add("Consider H-alpha or OIII filters for enhanced contrast");
                    notes.Add("Multiple exposures recommended for noise reduction");
                    break;

                case "Planetary Nebula":
                    notes.Add($"Small planetary nebula, {dso.AngularSize:F1} arcminutes");
                    notes.Add("High magnification required for detail");
                    notes.Add("OIII filter highly recommended");
                    break;

                case "Open Cluster":
                    notes.Add("Star cluster - good target for beginners");
                    notes.Add("Shorter exposures prevent star saturation");
                    break;

                case "Globular Cluster":
                    notes.Add("Dense star cluster requiring moderate magnification");
                    notes.Add("Focus on outer regions for best resolution");
                    break;

                default:
                    notes.Add($"Deep sky object with {dso.AngularSize:F0} arcminute apparent size");
                    notes.Add("Standard deep sky photography techniques apply");
                    break;
            }

            // Add magnitude-based advice
            if (dso.Magnitude > 10)
                notes.Add("Faint target - requires very dark skies");
            else if (dso.Magnitude > 8)
                notes.Add("Moderately faint - suburban skies acceptable");
            else
                notes.Add("Relatively bright target - good for light-polluted areas");

            return string.Join(". ", notes) + ".";
        }

        private async Task<string> GetDSOCameraSettingsAsync(DeepSkyObjectData dso)
        {
            try
            {
                var baseSettings = dso.ObjectType switch
                {
                    "Galaxy" when dso.AngularSize > 60 => "ISO 1600, f/2.8, 3-5 minutes",
                    "Galaxy" => "ISO 1600-3200, f/4, 5-8 minutes",
                    "Nebula" when dso.AngularSize > 30 => "ISO 800-1600, f/2.8, 4-6 minutes",
                    "Nebula" => "ISO 1600-3200, f/4, 6-10 minutes",
                    "Planetary Nebula" => "ISO 800-1600, f/5.6, 8-15 minutes",
                    "Open Cluster" => "ISO 400-800, f/4, 2-4 minutes",
                    "Globular Cluster" => "ISO 800-1600, f/4, 3-6 minutes",
                    _ => "ISO 1600, f/4, 5 minutes"
                };

                // Adjust for magnitude (brightness)
                if (dso.Magnitude > 10)
                {
                    baseSettings += " (increase exposure time for faint target)";
                }
                else if (dso.Magnitude < 6)
                {
                    baseSettings += " (reduce exposure to prevent overexposure)";
                }

                return baseSettings;
            }
            catch
            {
                return "ISO 1600, f/4, 5 minutes (tracked)";
            }
        }

        private async Task<UserEquipmentMatch> FindBestUserEquipmentForDSOAsync(TargetRequirements requirements)
        {
            try
            {
                // Get actual user equipment from repositories
                var userCamerasResult = await _cameraBodyRepository.GetUserCamerasAsync(CancellationToken.None);
                var userLensesResult = await _lensRepository.GetUserLensesAsync(CancellationToken.None);

                if (!userCamerasResult.IsSuccess || !userLensesResult.IsSuccess)
                {
                    return new UserEquipmentMatch
                    {
                        Found = false,
                        RecommendationMessage = $"You need a {requirements.MinFocalLength}-{requirements.MaxFocalLength}mm lens with f/{requirements.MaxAperture} aperture"
                    };
                }

                var userCameras = userCamerasResult.Data ?? new List<CameraBody>();
                var userLenses = userLensesResult.Data ?? new List<Lens>();

                // Find best matching lens from user's collection
                var bestLens = FindBestMatchingLens(userLenses, requirements);

                if (bestLens != null)
                {
                    // Find compatible camera for this lens
                    var compatibleCameras = userCameras.Where(c => IsLensCompatibleWithCamera(bestLens, c)).ToList();
                    var selectedCamera = compatibleCameras.FirstOrDefault();

                    if (selectedCamera != null)
                    {
                        return new UserEquipmentMatch
                        {
                            Found = true,
                            CameraDisplay = selectedCamera.Name,
                            LensDisplay = bestLens.NameForLens,
                            RecommendationMessage = $"Use your {selectedCamera.Name} with {bestLens.NameForLens}"
                        };
                    }
                }

                // No suitable equipment found
                return new UserEquipmentMatch
                {
                    Found = false,
                    RecommendationMessage = $"You need a {requirements.MinFocalLength}-{requirements.MaxFocalLength}mm lens with f/{requirements.MaxAperture} aperture"
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error finding user equipment for DSO");
                return new UserEquipmentMatch
                {
                    Found = false,
                    RecommendationMessage = "Unable to access your equipment - standard telephoto lens recommended"
                };
            }
        }

        private Lens FindBestMatchingLens(List<Lens> userLenses, TargetRequirements requirements)
        {
            // Priority 1: Lens that covers optimal focal length and meets aperture requirement
            var optimalMatch = userLenses.FirstOrDefault(l =>
                l.MaxFStop <= requirements.MaxAperture &&
                ((l.IsPrime && Math.Abs(l.MinMM - requirements.OptimalFocalLength) <= 10) ||
                 (!l.IsPrime && l.MaxMM.HasValue &&
                  requirements.OptimalFocalLength >= l.MinMM &&
                  requirements.OptimalFocalLength <= l.MaxMM.Value)));

            if (optimalMatch != null) return optimalMatch;

            // Priority 2: Lens that covers focal length range and meets aperture requirement
            var rangeMatch = userLenses.FirstOrDefault(l =>
                l.MaxFStop <= requirements.MaxAperture &&
                ((!l.IsPrime && l.MaxMM.HasValue &&
                  ((requirements.MinFocalLength >= l.MinMM && requirements.MinFocalLength <= l.MaxMM.Value) ||
                   (requirements.MaxFocalLength >= l.MinMM && requirements.MaxFocalLength <= l.MaxMM.Value))) ||
                 (l.IsPrime && l.MinMM >= requirements.MinFocalLength && l.MinMM <= requirements.MaxFocalLength)));

            if (rangeMatch != null) return rangeMatch;

            // Priority 3: Any lens in focal length range (ignore aperture)
            var anyMatch = userLenses.FirstOrDefault(l =>
                ((!l.IsPrime && l.MaxMM.HasValue &&
                  ((requirements.MinFocalLength >= l.MinMM && requirements.MinFocalLength <= l.MaxMM.Value) ||
                   (requirements.MaxFocalLength >= l.MinMM && requirements.MaxFocalLength <= l.MaxMM.Value))) ||
                 (l.IsPrime && l.MinMM >= requirements.MinFocalLength && l.MinMM <= requirements.MaxFocalLength)));

            return anyMatch;
        }

        private bool IsLensCompatibleWithCamera(Lens lens, CameraBody camera)
        {
            // Real compatibility check would be more complex
            // For now, assume all user equipment is compatible
            return true;
        }
        private async Task<string> GetDSOEquipmentRecommendationAsync(DeepSkyObjectData dso)
        {
            try
            {
                // Get DSO-specific requirements
                var requirements = GetDSORequirements(dso);

                // Try to find user equipment that matches
                var userEquipment = await FindBestUserEquipmentForDSOAsync(requirements);

                if (userEquipment.Found)
                {
                    return $"{userEquipment.CameraDisplay} with {userEquipment.LensDisplay}";
                }
                else
                {
                    return $"You need a {requirements.MinFocalLength}-{requirements.MaxFocalLength}mm lens with f/{requirements.MaxAperture} aperture";
                }
            }
            catch
            {
                return "Medium telephoto lens (85-300mm) recommended for most deep sky objects";
            }
        }

        private TargetRequirements GetDSORequirements(DeepSkyObjectData dso)
        {
            return dso.ObjectType switch
            {
                "Galaxy" when dso.AngularSize > 60 => new TargetRequirements
                {
                    MinFocalLength = 85,
                    MaxFocalLength = 200,
                    OptimalFocalLength = 135,
                    MaxAperture = 4.0,
                    TargetType = "large_galaxy"
                },
                "Galaxy" => new TargetRequirements
                {
                    MinFocalLength = 200,
                    MaxFocalLength = 600,
                    OptimalFocalLength = 300,
                    MaxAperture = 5.6,
                    TargetType = "small_galaxy"
                },
                "Nebula" when dso.AngularSize > 30 => new TargetRequirements
                {
                    MinFocalLength = 50,
                    MaxFocalLength = 135,
                    OptimalFocalLength = 85,
                    MaxAperture = 2.8,
                    TargetType = "large_nebula"
                },
                "Planetary Nebula" => new TargetRequirements
                {
                    MinFocalLength = 300,
                    MaxFocalLength = 1000,
                    OptimalFocalLength = 600,
                    MaxAperture = 6.3,
                    TargetType = "planetary_nebula"
                },
                _ => new TargetRequirements
                {
                    MinFocalLength = 135,
                    MaxFocalLength = 300,
                    OptimalFocalLength = 200,
                    MaxAperture = 4.0,
                    TargetType = "standard_dso"
                }
            };
        }

        private async Task<AstroEventDto> GetPlanetDataForHourAsync(DateTime hour, double latitude, double longitude)
        {
            try
            {
                var planets = await _astroCalculationService.GetVisiblePlanetsAsync(hour, latitude, longitude);
                var visiblePlanets = planets.Where(p => p.IsVisible && p.Altitude > 15).ToList();

                if (visiblePlanets.Any())
                {
                    var bestPlanet = visiblePlanets.OrderByDescending(p => p.ApparentMagnitude).First();
                    var equipmentRec = await GetEquipmentRecommendationsAsync(AstroTarget.Planets, latitude, longitude);
                    var cameraSettings = await GetDetailedCameraSettingsAsync(AstroTarget.Planets, bestPlanet.Altitude);
                    var notes = $"{bestPlanet.Planet} is brightest (mag {bestPlanet.ApparentMagnitude:F1}). {bestPlanet.PhotographyNotes}. {bestPlanet.RecommendedEquipment}";

                    return new AstroEventDto
                    {
                        TargetName = $"Planets ({string.Join(", ", visiblePlanets.Select(p => p.Planet))})",
                        Visibility = $"{bestPlanet.Planet}: {bestPlanet.Altitude:F0}° altitude, {bestPlanet.Azimuth:F0}° azimuth",
                        RecommendedEquipment = equipmentRec,
                        CameraSettings = cameraSettings,
                        Notes = notes
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting planet data for hour");
            }

            return null;
        }

        private async Task<string> GetEquipmentRecommendationsAsync(AstroTarget target, double latitude, double longitude)
        {
            try
            {
                return target switch
                {
                    AstroTarget.MilkyWayCore => "Wide-angle lens (14-35mm), full-frame camera preferred. Fast aperture (f/1.4-f/2.8) essential. Sturdy tripod required.",
                    AstroTarget.Moon => "Telephoto lens (200-600mm), any camera body. Tripod essential. Consider neutral density filter for bright phases.",
                    AstroTarget.Planets => "Long telephoto lens (400mm+) or telescope. High-resolution camera. Tracking mount recommended for best results.",
                    AstroTarget.DeepSkyObjects => "Medium telephoto lens (85-300mm) or telescope. Tracking mount essential. Consider specialized filters (H-alpha, OIII).",
                    AstroTarget.StarTrails => "Wide-angle lens (14-50mm), any camera body. Intervalometer for multiple exposures. Extra batteries for long sessions.",
                    AstroTarget.Constellations => "Standard lens (35-135mm), any camera body. Tripod required. Consider star tracker for longer exposures.",
                    _ => "Standard astrophotography equipment - camera, lens, tripod. Specific requirements vary by target."
                };
            }
            catch
            {
                return "Standard astrophotography equipment recommended";
            }
        }

        private async Task<string> GetDetailedCameraSettingsAsync(AstroTarget target, double altitude)
        {
            try
            {
                var baseSettings = target switch
                {
                    AstroTarget.MilkyWayCore => "ISO 3200-6400, f/1.4-f/2.8, 15-25 seconds",
                    AstroTarget.Moon => "ISO 100-400, f/5.6-f/8, 1/60-1/500 second (varies by phase)",
                    AstroTarget.Planets => "ISO 800-1600, f/5.6-f/8, 1/30-1/125 second",
                    AstroTarget.DeepSkyObjects => "ISO 1600-3200, f/2.8-f/4, 2-10 minutes (tracked)",
                    AstroTarget.StarTrails => "ISO 200-800, f/2.8-f/5.6, 2-4 minute intervals",
                    AstroTarget.Constellations => "ISO 800-1600, f/2.8-f/4, 30-60 seconds",
                    _ => "ISO 1600, f/4, 30 seconds"
                };

                // Adjust for altitude
                if (altitude < 20)
                {
                    baseSettings += " (increase ISO due to low altitude)";
                }
                else if (altitude > 60)
                {
                    baseSettings += " (can use lower ISO due to high altitude)";
                }

                return baseSettings;
            }
            catch
            {
                return "ISO 1600, f/4, 30 seconds";
            }
        }

        private async Task<string> GetComprehensiveNotesAsync(AstroTarget target, AstroCalculationResult result, DateTime hour, double latitude, double longitude)
        {
            try
            {
                var notes = new List<string>();

                // Target-specific advice
                switch (target)
                {
                    case AstroTarget.MilkyWayCore:
                        notes.Add("Best photographed during new moon for darkest skies");
                        notes.Add("Point camera 45-60° from galactic center for optimal composition");
                        notes.Add("Use live view and magnification for precise focusing");
                        break;
                    case AstroTarget.Moon:
                        notes.Add("Use spot metering to avoid overexposure");
                        notes.Add("Crescent phases show best crater detail along terminator");
                        notes.Add("Consider HDR for earthshine and bright limb");
                        break;
                    case AstroTarget.Planets:
                        notes.Add("Atmospheric stability critical - avoid windy conditions");
                        notes.Add("Stack multiple images to reduce atmospheric distortion");
                        notes.Add("Use highest magnification your mount can handle");
                        break;
                    case AstroTarget.DeepSkyObjects:
                        notes.Add("Tracking mount essential for sharp stars");
                        notes.Add("Multiple exposures and stacking required");
                        notes.Add("Dark skies and narrowband filters improve results");
                        break;
                }

                // Weather and conditions advice
                var season = GetSeasonForMonth(hour.Month);
                notes.Add($"Best photographed during {season} season for optimal positioning");

                // Altitude-specific advice
                if (result.Altitude < 20)
                {
                    notes.Add("Low altitude - find elevated location with clear horizon");
                }
                else if (result.Altitude > 60)
                {
                    notes.Add("Excellent high altitude - minimal atmospheric interference");
                }

                return string.Join(". ", notes) + ".";
            }
            catch
            {
                return "Standard astrophotography guidelines apply.";
            }
        }

        private string GetSeasonForMonth(int month)
        {
            return month switch
            {
                12 or 1 or 2 => AppResources.Season_Winter,
                3 or 4 or 5 => AppResources.Season_Spring,
                6 or 7 or 8 => AppResources.Season_Summer,
                9 or 10 or 11 => AppResources.Season_Autumn,
                _ => AppResources.Season_Any
            };
        }

        private string GetTargetDisplayName(AstroTarget target)
        {
            return target switch
            {
                AstroTarget.MilkyWayCore => AppResources.AstroTarget_MilkyWayCore,
                AstroTarget.DeepSkyObjects => AppResources.AstroTarget_DeepSkyObjects,
                AstroTarget.StarTrails => AppResources.AstroTarget_StarTrails,
                AstroTarget.MeteorShowers => AppResources.AstroTarget_MeteorShowers,
                AstroTarget.PolarAlignment => AppResources.AstroTarget_PolarAlignment,
                AstroTarget.NorthernLights => AppResources.AstroTarget_NorthernLights,
                _ => target.ToString()
            };
        }

        private async Task<WeatherDto> GetWeatherDtoAsync(DateTime hour, double latitude, double longitude)
        {
            try
            {
                // Try to get actual weather data
                var hourlyQuery = new GetHourlyForecastQuery
                {
                    LocationId = 1, // Would need actual location ID
                    StartTime = hour.AddMinutes(-30),
                    EndTime = hour.AddMinutes(30)
                };

                var result = await _mediator.Send(hourlyQuery);
                if (result.IsSuccess && result.Data?.HourlyForecasts?.Any() == true)
                {
                    var forecast = result.Data.HourlyForecasts
                        .OrderBy(f => Math.Abs((f.DateTime - hour).TotalMinutes))
                        .FirstOrDefault();

                    if (forecast != null)
                    {
                        var suitability = CalculateWeatherSuitability(forecast.Clouds, forecast.ProbabilityOfPrecipitation, forecast.WindSpeed);

                        return new WeatherDto
                        {
                            CloudCover = forecast.Clouds,
                            Humidity = forecast.Humidity,
                            WindSpeed = forecast.WindSpeed,
                            Visibility = forecast.Visibility,
                            Description = forecast.Description,
                            WeatherDisplay = $"{forecast.Description} ({forecast.Clouds:F0}% clouds)",
                            WeatherSuitability = suitability
                        };
                    }
                }

                // Fallback to reasonable defaults
                return new WeatherDto
                {
                    CloudCover = 20,
                    Humidity = 60,
                    WindSpeed = 5,
                    Visibility = 10000,
                    Description = "Clear skies",
                    WeatherDisplay = "Clear (20% clouds)",
                    WeatherSuitability = "Excellent for astrophotography"
                };
            }
            catch
            {
                return new WeatherDto
                {
                    CloudCover = 50,
                    Humidity = 70,
                    WindSpeed = 10,
                    Visibility = 8000,
                    Description = "Partly cloudy",
                    WeatherDisplay = "Partly cloudy",
                    WeatherSuitability = "Fair conditions"
                };
            }
        }

        private string CalculateWeatherSuitability(double cloudCover, double precipitationProb, double windSpeed)
        {
            var score = 100.0;
            score -= cloudCover * 0.8; // Cloud cover is most important
            score -= precipitationProb * 60; // Precipitation is critical
            score -= Math.Max(0, windSpeed - 10) * 2; // Wind over 10 mph is problematic

            return score switch
            {
                >= 80 => "Excellent for astrophotography",
                >= 60 => "Good shooting conditions",
                >= 40 => "Fair conditions with challenges",
                >= 20 => "Poor conditions - not recommended",
                _ => "Very poor conditions"
            };
        }

        private string GetCameraSettings(AstroTarget target)
        {
            return target switch
            {
                AstroTarget.MilkyWayCore => "ISO 3200, f/2.8, 20\"",
                AstroTarget.Moon => "ISO 200, f/8, 1/125\"",
                AstroTarget.Planets => "ISO 800, f/5.6, 1/60\"",
                AstroTarget.DeepSkyObjects => "ISO 1600, f/4, 5 min",
                AstroTarget.StarTrails => "ISO 400, f/4, 4 min intervals",
                _ => "ISO 1600, f/4, 30\""
            };
        }

        private string GetQualityDisplay(double score)
        {
            return score switch
            {
                >= 80 => "Excellent",
                >= 60 => "Good",
                >= 40 => "Fair",
                >= 20 => "Poor",
                _ => "Very Poor"
            };
        }

        private string GetQualityDescription(double score)
        {
            return score switch
            {
                >= 80 => "Exceptional astrophotography conditions with multiple targets visible",
                >= 60 => "Good shooting opportunity with minor limitations",
                >= 40 => "Fair conditions - manageable challenges present",
                >= 20 => "Poor conditions - significant obstacles to photography",
                _ => "Very challenging conditions - not recommended for astrophotography"
            };
        }
        public async Task<AstroHourlyPredictionDto> MapSingleCalculationAsync(
    AstroCalculationResult calculationResult,
    double latitude,
    double longitude,
    DateTime selectedDate)
        {
            try
            {
                var hour = calculationResult.CalculationTime;

                // Get solar events for this hour
                var solarEvent = await GetSolarEventForHourAsync(hour, latitude, longitude);

                // Calculate quality score for single result
                var qualityScore = await CalculateHourQualityAsync(hour, latitude, longitude, new List<AstroCalculationResult> { calculationResult });

                // Generate astro events from single calculation result
                var astroEvents = await GenerateAstroEventsFromCalculationsAsync(new List<AstroCalculationResult> { calculationResult }, hour, latitude, longitude);

                // Create basic DTO structure
                var dto = new AstroHourlyPredictionDto
                {
                    Hour = hour,
                    TimeDisplay = hour.ToString("h:mm tt"),
                    SolarEvent = solarEvent,
                    SolarEventsDisplay = solarEvent,
                    QualityScore = qualityScore,
                    QualityDisplay = GetQualityDisplay(qualityScore),
                    QualityDescription = GetQualityDescription(qualityScore),
                    AstroEvents = astroEvents,
                    Weather = await GetWeatherDtoAsync(hour, latitude, longitude)
                };

                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error mapping single calculation");
                return CreateDefaultDto(calculationResult.CalculationTime);
            }
        }
        private AstroHourlyPredictionDto CreateDefaultDto(DateTime hour)
        {
            return new AstroHourlyPredictionDto
            {
                Hour = hour,
                TimeDisplay = hour.ToString("h:mm tt"),
                SolarEvent = AppResources.Weather_Unknown,
                SolarEventsDisplay = AppResources.Weather_Unknown,
                QualityScore = 50,
                QualityDisplay = AppResources.Quality_Fair,
                QualityDescription = AppResources.QualityDescription_Unknown,
                AstroEvents = new List<AstroEventDto>(),
                Weather = new WeatherDto
                {
                    CloudCover = 50,
                    Humidity = 70,
                    WindSpeed = 10,
                    Visibility = 8000,
                    Description = AppResources.Weather_Unknown,
                    WeatherDisplay = AppResources.Weather_Unknown,
                    WeatherSuitability = AppResources.Weather_Unknown
                }
            };
        }

    }
}