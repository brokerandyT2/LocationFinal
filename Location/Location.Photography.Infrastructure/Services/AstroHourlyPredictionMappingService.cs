using Location.Core.Application.Common.Models;
using Location.Core.Application.Weather.DTOs;
using Location.Core.Application.Weather.Queries.GetHourlyForecast;
using Location.Photography.Application.Common.Interfaces;
using Location.Photography.Application.Services;
using Location.Photography.Domain.Entities;
using Location.Photography.Domain.Models;
using Location.Photography.Domain.Services;
using Location.Photography.ViewModels;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Location.Photography.ViewModels.AstroPhotographyCalculatorViewModel;
using ServiceWeatherConditions = Location.Photography.Application.Services.WeatherConditions;
using ViewModelWeatherConditions = Location.Photography.ViewModels.AstroPhotographyCalculatorViewModel.WeatherConditions;

namespace Location.Photography.Infrastructure.Services
{
    public interface IAstroHourlyPredictionMappingService
    {
        Task<Result<AstroHourlyPredictionDisplayModel>> MapToDisplayModelAsync(
            AstroHourlyPrediction domainModel,
            int locationId,
            double latitude,
            double longitude,
            List<CameraBody> userCameras,
            List<Lens> userLenses,
            CancellationToken cancellationToken = default);

        Task<Result<List<AstroHourlyPredictionDisplayModel>>> MapCollectionToDisplayModelsAsync(
            List<AstroHourlyPrediction> domainModels,
            int locationId,
            double latitude,
            double longitude,
            List<CameraBody> userCameras,
            List<Lens> userLenses,
            CancellationToken cancellationToken = default);
    }

    public class AstroHourlyPredictionMappingService : IAstroHourlyPredictionMappingService
    {
        private readonly ILogger<AstroHourlyPredictionMappingService> _logger;
        private readonly IAstroCalculationService _astroCalculationService;
        private readonly IEquipmentRecommendationService _equipmentRecommendationService;
        private readonly ISunCalculatorService _sunCalculatorService;
        private readonly IExposureCalculatorService _exposureCalculatorService;
        private readonly ILensCameraCompatibilityRepository _compatibilityRepository;
        private readonly IPredictiveLightService _predictiveLightService;
        private readonly IMediator _mediator;

        public AstroHourlyPredictionMappingService(
            ILogger<AstroHourlyPredictionMappingService> logger,
            IAstroCalculationService astroCalculationService,
            IEquipmentRecommendationService equipmentRecommendationService,
            ISunCalculatorService sunCalculatorService,
            IExposureCalculatorService exposureCalculatorService,
            ILensCameraCompatibilityRepository compatibilityRepository,
            IPredictiveLightService predictiveLightService,
            IMediator mediator)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _astroCalculationService = astroCalculationService ?? throw new ArgumentNullException(nameof(astroCalculationService));
            _equipmentRecommendationService = equipmentRecommendationService ?? throw new ArgumentNullException(nameof(equipmentRecommendationService));
            _sunCalculatorService = sunCalculatorService ?? throw new ArgumentNullException(nameof(sunCalculatorService));
            _exposureCalculatorService = exposureCalculatorService ?? throw new ArgumentNullException(nameof(exposureCalculatorService));
            _compatibilityRepository = compatibilityRepository ?? throw new ArgumentNullException(nameof(compatibilityRepository));
            _predictiveLightService = predictiveLightService ?? throw new ArgumentNullException(nameof(predictiveLightService));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        public async Task<Result<AstroHourlyPredictionDisplayModel>> MapToDisplayModelAsync(
            AstroHourlyPrediction domainModel,
            int locationId,
            double latitude,
            double longitude,
            List<CameraBody> userCameras,
            List<Lens> userLenses,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var displayModel = new AstroHourlyPredictionDisplayModel
                {
                    Hour = domainModel.Hour,
                    DomainModel = domainModel
                };

                // Get actual weather forecast for this hour
                var actualWeather = await GetActualWeatherForHourAsync(locationId, domainModel.Hour, cancellationToken);

                // Analyze weather impact using predictive light service
                var weatherImpact = await AnalyzeWeatherImpactAsync(actualWeather, domainModel.Hour, latitude, longitude, cancellationToken);

                // Map basic header properties with weather-adjusted scoring
                await MapHeaderPropertiesAsync(displayModel, domainModel, latitude, longitude, weatherImpact, cancellationToken);

                // Map astro events with equipment recommendations and weather adjustments
                await MapAstroEventsAsync(displayModel, domainModel, userCameras, userLenses, weatherImpact, cancellationToken);

                // Map solar events
                await MapSolarEventsAsync(displayModel, domainModel.Hour, latitude, longitude, cancellationToken);

                // Calculate overall assessment with weather integration
                CalculateOverallAssessment(displayModel, weatherImpact);

                return Result<AstroHourlyPredictionDisplayModel>.Success(displayModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error mapping AstroHourlyPrediction to display model for hour {Hour}", domainModel.Hour);
                return Result<AstroHourlyPredictionDisplayModel>.Failure($"Mapping error: {ex.Message}");
            }
        }

        public async Task<Result<List<AstroHourlyPredictionDisplayModel>>> MapCollectionToDisplayModelsAsync(
            List<AstroHourlyPrediction> domainModels,
            int locationId,
            double latitude,
            double longitude,
            List<CameraBody> userCameras,
            List<Lens> userLenses,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var displayModels = new List<AstroHourlyPredictionDisplayModel>();

                foreach (var domainModel in domainModels)
                {
                    var result = await MapToDisplayModelAsync(domainModel, locationId, latitude, longitude, userCameras, userLenses, cancellationToken);
                    if (result.IsSuccess && result.Data != null)
                    {
                        displayModels.Add(result.Data);
                    }
                }

                return Result<List<AstroHourlyPredictionDisplayModel>>.Success(displayModels);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error mapping collection of AstroHourlyPredictions");
                return Result<List<AstroHourlyPredictionDisplayModel>>.Failure($"Collection mapping error: {ex.Message}");
            }
        }

        private async Task MapHeaderPropertiesAsync(
            AstroHourlyPredictionDisplayModel displayModel,
            AstroHourlyPrediction domainModel,
            double latitude,
            double longitude,
            WeatherImpactAnalysis weatherImpact,
            CancellationToken cancellationToken)
        {
            // Format time display based on user preferences (defaulting to 12-hour format)
            displayModel.TimeDisplay = domainModel.Hour.ToString("h:mm tt");

            // Get solar events for this hour
            var solarEvents = await GetSolarEventsForHourAsync(domainModel.Hour, latitude, longitude, cancellationToken);
            displayModel.SolarEventsDisplay = string.Join(", ", solarEvents.Take(2)); // Show max 2 events

            // Calculate weather-adjusted quality score
            displayModel.QualityScore = CalculateWeatherAdjustedQualityScore(domainModel, weatherImpact);

            // Determine if this is an optimal time (weather-adjusted)
            displayModel.IsOptimalTime = displayModel.QualityScore >= 75;
        }

        private async Task MapAstroEventsAsync(
            AstroHourlyPredictionDisplayModel displayModel,
            AstroHourlyPrediction domainModel,
            List<CameraBody> userCameras,
            List<Lens> userLenses,
            WeatherImpactAnalysis weatherImpact,
            CancellationToken cancellationToken)
        {
            var astroEvents = new List<AstroEventDisplayModel>();

            // Sort target events by weather-adjusted quality
            var sortedEvents = domainModel.TargetEvents
                .OrderByDescending(e => CalculateWeatherAdjustedEventQuality(e, domainModel.Hour, weatherImpact))
                .ToList();

            foreach (var targetEvent in sortedEvents)
            {
                var astroEvent = await MapTargetEventToDisplayModelAsync(
                    targetEvent, domainModel.Hour, userCameras, userLenses, weatherImpact, cancellationToken);

                if (astroEvent != null)
                {
                    astroEvents.Add(astroEvent);
                }
            }

            displayModel.AstroEvents = astroEvents;
        }

        private async Task<AstroEventDisplayModel> MapTargetEventToDisplayModelAsync(
            AstroTargetEvent targetEvent,
            DateTime hour,
            List<CameraBody> userCameras,
            List<Lens> userLenses,
            WeatherImpactAnalysis weatherImpact,
            CancellationToken cancellationToken)
        {
            try
            {
                var displayModel = new AstroEventDisplayModel
                {
                    TargetName = GetTargetDisplayName(targetEvent),
                    EventType = targetEvent.GetType().Name,
                    QualityRank = CalculateWeatherAdjustedEventQuality(targetEvent, hour, weatherImpact),
                    PhotographyNotes = GetTargetPhotographyNotes(targetEvent),
                    DifficultyLevel = GetTargetDifficultyLevel(targetEvent)
                };

                // Map position information
                MapPositionInformation(displayModel, targetEvent);

                // Map timing information
                MapTimingInformation(displayModel, targetEvent, hour);

                // Find and map equipment recommendations
                await MapEquipmentRecommendationsAsync(displayModel, targetEvent, userCameras, userLenses, cancellationToken);

                // Calculate photography settings with weather considerations
                await MapPhotographySettingsAsync(displayModel, targetEvent, weatherImpact, cancellationToken);

                return displayModel;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error mapping target event {EventType}", targetEvent.GetType().Name);
                return null;
            }
        }

        private void MapPositionInformation(AstroEventDisplayModel displayModel, AstroTargetEvent targetEvent)
        {
            // Use reflection to extract position data from any target event type
            var azimuthProp = targetEvent.GetType().GetProperty("Azimuth") ??
                             targetEvent.GetType().GetProperty("TargetAzimuth") ??
                             targetEvent.GetType().GetProperty("ObjectAzimuth") ??
                             targetEvent.GetType().GetProperty("GalacticCenterAzimuth") ??
                             targetEvent.GetType().GetProperty("MoonAzimuth") ??
                             targetEvent.GetType().GetProperty("PlanetAzimuth") ??
                             targetEvent.GetType().GetProperty("ConstellationAzimuth");

            var altitudeProp = targetEvent.GetType().GetProperty("Altitude") ??
                              targetEvent.GetType().GetProperty("TargetAltitude") ??
                              targetEvent.GetType().GetProperty("ObjectAltitude") ??
                              targetEvent.GetType().GetProperty("GalacticCenterAltitude") ??
                              targetEvent.GetType().GetProperty("MoonAltitude") ??
                              targetEvent.GetType().GetProperty("PlanetAltitude") ??
                              targetEvent.GetType().GetProperty("ConstellationAltitude");

            displayModel.Azimuth = (double)(azimuthProp?.GetValue(targetEvent) ?? 180.0);
            displayModel.Altitude = (double)(altitudeProp?.GetValue(targetEvent) ?? 45.0);
            // Note: displayModel.IsVisible is computed property, not set directly
        }

        private void MapTimingInformation(AstroEventDisplayModel displayModel, AstroTargetEvent targetEvent, DateTime hour)
        {
            // Use reflection to extract timing information from any target event type
            var riseTimeProp = targetEvent.GetType().GetProperty("RiseTime") ??
                              targetEvent.GetType().GetProperty("Rise") ??
                              targetEvent.GetType().GetProperty("TargetRise") ??
                              targetEvent.GetType().GetProperty("GalacticCenterRise") ??
                              targetEvent.GetType().GetProperty("MoonRise") ??
                              targetEvent.GetType().GetProperty("PlanetRise") ??
                              targetEvent.GetType().GetProperty("ConstellationRise");

            var setTimeProp = targetEvent.GetType().GetProperty("SetTime") ??
                             targetEvent.GetType().GetProperty("Set") ??
                             targetEvent.GetType().GetProperty("TargetSet") ??
                             targetEvent.GetType().GetProperty("GalacticCenterSet") ??
                             targetEvent.GetType().GetProperty("MoonSet") ??
                             targetEvent.GetType().GetProperty("PlanetSet") ??
                             targetEvent.GetType().GetProperty("ConstellationSet");

            var optimalTimeProp = targetEvent.GetType().GetProperty("OptimalTime") ??
                                 targetEvent.GetType().GetProperty("OptimalViewingTime") ??
                                 targetEvent.GetType().GetProperty("BestTime") ??
                                 targetEvent.GetType().GetProperty("OptimalPhaseTime") ??
                                 targetEvent.GetType().GetProperty("TransitTime");

            displayModel.RiseTime = riseTimeProp?.GetValue(targetEvent) as DateTime?;
            displayModel.SetTime = setTimeProp?.GetValue(targetEvent) as DateTime?;
            displayModel.OptimalTime = optimalTimeProp?.GetValue(targetEvent) as DateTime?;
        }

        private async Task MapEquipmentRecommendationsAsync(
            AstroEventDisplayModel displayModel,
            AstroTargetEvent targetEvent,
            List<CameraBody> userCameras,
            List<Lens> userLenses,
            CancellationToken cancellationToken)
        {
            try
            {
                // Get target requirements
                var requirements = GetTargetRequirements(targetEvent);

                // Find best matching user equipment
                var bestUserLens = await FindBestMatchingLensAsync(userLenses, requirements, cancellationToken);
                var compatibleCamera = await FindCompatibleCameraAsync(bestUserLens, userCameras, cancellationToken);

                if (bestUserLens != null && compatibleCamera != null)
                {
                    // User has suitable equipment
                    displayModel.RecommendedLens = bestUserLens.NameForLens;
                    displayModel.RecommendedCamera = compatibleCamera.Name;
                    displayModel.IsUserEquipment = true;
                    displayModel.EquipmentNote = $"Perfect match from your collection";
                }
                else
                {
                    // Generate generic recommendations
                    var genericRecommendation = await GenerateGenericEquipmentRecommendationAsync(requirements, cancellationToken);
                    displayModel.RecommendedLens = genericRecommendation.LensRecommendation;
                    displayModel.RecommendedCamera = genericRecommendation.CameraRecommendation;
                    displayModel.IsUserEquipment = false;
                    displayModel.EquipmentNote = "Generic recommendation - you may need additional equipment";
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error mapping equipment recommendations");
                displayModel.RecommendedLens = "Wide-angle lens recommended";
                displayModel.RecommendedCamera = "Full-frame camera preferred";
                displayModel.IsUserEquipment = false;
                displayModel.EquipmentNote = "Unable to analyze your equipment";
            }
        }

        private async Task MapPhotographySettingsAsync(
            AstroEventDisplayModel displayModel,
            AstroTargetEvent targetEvent,
            WeatherImpactAnalysis weatherImpact,
            CancellationToken cancellationToken)
        {
            try
            {
                var baseSettings = GetBaseSettingsForTarget(targetEvent);

                // Apply weather adjustments to settings
                var weatherAdjustedSettings = ApplyWeatherAdjustments(baseSettings, weatherImpact);

                // Use exposure calculator service to normalize settings
                var exposureDto = new ExposureTriangleDto
                {
                    Aperture = $"f/{weatherAdjustedSettings.Aperture}",
                    Iso = weatherAdjustedSettings.ISO.ToString(),
                    ShutterSpeed = FormatShutterSpeed(weatherAdjustedSettings.ShutterSpeed)
                };

                // Normalize to standard values using thirds increments
                var normalizedResult = await _exposureCalculatorService.CalculateIsoAsync(
                    exposureDto,
                    FormatShutterSpeed(weatherAdjustedSettings.ShutterSpeed),
                    $"f/{weatherAdjustedSettings.Aperture}",
                    ExposureIncrements.Third,
                    cancellationToken);

                if (normalizedResult.IsSuccess && normalizedResult.Data != null)
                {
                    displayModel.SuggestedAperture = normalizedResult.Data.Aperture.Replace("f/", "");
                    displayModel.SuggestedShutterSpeed = normalizedResult.Data.ShutterSpeed;
                    displayModel.SuggestedISO = normalizedResult.Data.Iso;
                }
                else
                {
                    // Fallback to weather-adjusted settings
                    displayModel.SuggestedAperture = weatherAdjustedSettings.Aperture.ToString("F1");
                    displayModel.SuggestedShutterSpeed = FormatShutterSpeed(weatherAdjustedSettings.ShutterSpeed);
                    displayModel.SuggestedISO = weatherAdjustedSettings.ISO.ToString();
                }

                // Calculate focal length recommendation
                var requirements = GetTargetRequirements(targetEvent);
                displayModel.FocalLengthRecommendation = $"{requirements.OptimalFocalLength:F0}mm";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error mapping photography settings");
                displayModel.SuggestedAperture = "2.8";
                displayModel.SuggestedShutterSpeed = "30\"";
                displayModel.SuggestedISO = "3200";
                displayModel.FocalLengthRecommendation = "24mm";
            }
        }

        private async Task MapSolarEventsAsync(
            AstroHourlyPredictionDisplayModel displayModel,
            DateTime hour,
            double latitude,
            double longitude,
            CancellationToken cancellationToken)
        {
            try
            {
                var solarEvents = new List<SolarEventDisplayModel>();

                // Get sun position for this hour
                var sunAzimuth = _sunCalculatorService.GetSolarAzimuth(hour, latitude, longitude, "UTC");
                var sunAltitude = _sunCalculatorService.GetSolarElevation(hour, latitude, longitude, "UTC");

                // Determine what solar events are happening
                var eventNames = await GetSolarEventsForHourAsync(hour, latitude, longitude, cancellationToken);

                foreach (var eventName in eventNames)
                {
                    var solarEvent = new SolarEventDisplayModel
                    {
                        EventName = eventName,
                        EventType = DetermineSolarEventType(eventName),
                        EventTime = hour,
                        SunAzimuth = sunAzimuth,
                        SunAltitude = sunAltitude,
                        LightQuality = GetSolarLightQuality(eventName, sunAltitude),
                        ColorTemperature = GetSolarColorTemperature(eventName, sunAltitude).ToString(),
                        ImpactOnAstro = GetAstroImpact(eventName, sunAltitude),
                        ConflictsWithAstro = DoesConflictWithAstro(eventName, sunAltitude)
                    };

                    solarEvents.Add(solarEvent);
                }

                displayModel.SolarEvents = solarEvents;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error mapping solar events for hour {Hour}", hour);
                displayModel.SolarEvents = new List<SolarEventDisplayModel>();
            }
        }

        private void CalculateOverallAssessment(AstroHourlyPredictionDisplayModel displayModel, WeatherImpactAnalysis weatherImpact)
        {
            // Overall quality based on best astro events with weather adjustments
            var bestEventScore = displayModel.AstroEvents.Any()
                ? displayModel.AstroEvents.Max(e => e.QualityRank)
                : 0;

            // Apply weather impact reduction
            var weatherReductionFactor = weatherImpact?.OverallLightReductionFactor ?? 0.8;
            var weatherAdjustedScore = bestEventScore * weatherReductionFactor;

            displayModel.QualityScore = Math.Max(0, Math.Min(100, weatherAdjustedScore));

            // Generate textual assessment
            displayModel.OverallQuality = GenerateQualityDescription(displayModel.QualityScore);
            displayModel.ConfidenceDisplay = $"Confidence: {CalculateConfidence(displayModel):F0}%";
            displayModel.Recommendations = GenerateRecommendations(displayModel, weatherImpact);
        }

        #region Weather Integration Methods

        private async Task<ViewModelWeatherConditions> GetActualWeatherForHourAsync(
            int locationId,
            DateTime hour,
            CancellationToken cancellationToken)
        {
            try
            {
                // Get hourly weather forecast from weather service
                var hourlyQuery = new GetHourlyForecastQuery
                {
                    LocationId = locationId,
                    StartTime = hour.AddMinutes(-30),
                    EndTime = hour.AddMinutes(30)
                };

                var result = await _mediator.Send(hourlyQuery, cancellationToken);
                if (result.IsSuccess && result.Data?.HourlyForecasts?.Any() == true)
                {
                    // Find closest forecast to requested hour
                    var forecast = result.Data.HourlyForecasts
                        .OrderBy(f => Math.Abs((f.DateTime - hour).TotalMinutes))
                        .FirstOrDefault();

                    if (forecast != null)
                    {
                        return new ViewModelWeatherConditions
                        {
                            CloudCover = forecast.Clouds,
                            PrecipitationProbability = forecast.ProbabilityOfPrecipitation,
                            WindSpeed = forecast.WindSpeed,
                            Humidity = forecast.Humidity,
                            Visibility = forecast.Visibility,
                            Description = forecast.Description
                        };
                    }
                }

                // Fallback to reasonable defaults if no forecast available
                return new ViewModelWeatherConditions
                {
                    CloudCover = 20,
                    PrecipitationProbability = 0.1,
                    WindSpeed = 5,
                    Humidity = 60,
                    Visibility = 10000,
                    Description = "Clear skies"
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting actual weather for hour {Hour}", hour);
                return new ViewModelWeatherConditions
                {
                    CloudCover = 30,
                    PrecipitationProbability = 0.2,
                    WindSpeed = 8,
                    Humidity = 65,
                    Visibility = 8000,
                    Description = "Partly cloudy"
                };
            }
        }

        private async Task<WeatherImpactAnalysis> AnalyzeWeatherImpactAsync(
            ViewModelWeatherConditions weather,
            DateTime hour,
            double latitude,
            double longitude,
            CancellationToken cancellationToken)
        {
            try
            {
                // Create weather impact analysis request - check actual properties of WeatherImpactAnalysisRequest
                var weatherRequest = new WeatherImpactAnalysisRequest
                {
                    WeatherForecast = CreateWeatherForecastFromConditions(weather, hour),
                    SunTimes = await GetSunTimesForWeatherAnalysisAsync(hour, latitude, longitude)
                };

                // Use predictive light service to analyze weather impact
                var weatherImpact = await _predictiveLightService.AnalyzeWeatherImpactAsync(weatherRequest, cancellationToken);

                return weatherImpact;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error analyzing weather impact for hour {Hour}", hour);

                // Create fallback weather impact analysis
                return new WeatherImpactAnalysis
                {
                    OverallLightReductionFactor = CalculateWeatherScore(weather) / 100.0,
                    Summary = "Weather impact estimated based on basic conditions",
                    CurrentConditions = ConvertToServiceWeatherConditions(weather)
                };
            }
        }

        private async Task<EnhancedSunTimes> GetSunTimesForWeatherAnalysisAsync(DateTime hour, double latitude, double longitude)
        {
            try
            {
                var date = hour.Date;

                // Get basic sun times using the sun calculator service
                var sunrise = _sunCalculatorService.GetSunrise(date, latitude, longitude, "UTC");
                var sunset = _sunCalculatorService.GetSunset(date, latitude, longitude, "UTC");
                var civilDawn = _sunCalculatorService.GetCivilDawn(date, latitude, longitude, "UTC");
                var civilDusk = _sunCalculatorService.GetCivilDusk(date, latitude, longitude, "UTC");
                var nauticalDawn = _sunCalculatorService.GetNauticalDawn(date, latitude, longitude, "UTC");
                var nauticalDusk = _sunCalculatorService.GetNauticalDusk(date, latitude, longitude, "UTC");
                var astronomicalDawn = _sunCalculatorService.GetAstronomicalDawn(date, latitude, longitude, "UTC");
                var astronomicalDusk = _sunCalculatorService.GetAstronomicalDusk(date, latitude, longitude, "UTC");

                return new EnhancedSunTimes
                {
                    Sunrise = sunrise,
                    Sunset = sunset,
                    CivilDawn = civilDawn,
                    CivilDusk = civilDusk,
                    NauticalDawn = nauticalDawn,
                    NauticalDusk = nauticalDusk,
                    AstronomicalDawn = astronomicalDawn,
                    AstronomicalDusk = astronomicalDusk
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting sun times for weather analysis");

                // Return default sun times
                var date = hour.Date;
                return new EnhancedSunTimes
                {
                    Sunrise = date.AddHours(6),
                    Sunset = date.AddHours(18),
                    CivilDawn = date.AddHours(5.5),
                    CivilDusk = date.AddHours(18.5),
                    NauticalDawn = date.AddHours(5),
                    NauticalDusk = date.AddHours(19),
                    AstronomicalDawn = date.AddHours(4.5),
                    AstronomicalDusk = date.AddHours(19.5)
                };
            }
        }

        private double CalculateWeatherAdjustedQualityScore(
            AstroHourlyPrediction domainModel,
            WeatherImpactAnalysis weatherImpact)
        {
            // Start with base score from domain model
            var baseScore = domainModel.OverallScore;

            // Apply weather reduction factor from analysis
            var weatherReductionFactor = weatherImpact?.OverallLightReductionFactor ?? 0.8;

            var adjustedScore = baseScore * weatherReductionFactor;

            return Math.Max(0, Math.Min(100, adjustedScore));
        }

        private double CalculateWeatherAdjustedEventQuality(
            AstroTargetEvent targetEvent,
            DateTime hour,
            WeatherImpactAnalysis weatherImpact)
        {
            // Calculate base event quality
            var baseQuality = CalculateEventQuality(targetEvent, hour);

            // Apply weather impact reduction
            var weatherReductionFactor = weatherImpact?.OverallLightReductionFactor ?? 0.8;

            var adjustedQuality = baseQuality * weatherReductionFactor;

            return Math.Max(0, Math.Min(100, adjustedQuality));
        }

        private BaseCameraSettings ApplyWeatherAdjustments(
            BaseCameraSettings baseSettings,
            WeatherImpactAnalysis weatherImpact)
        {
            var adjustedSettings = new BaseCameraSettings
            {
                Aperture = baseSettings.Aperture,
                ShutterSpeed = baseSettings.ShutterSpeed,
                ISO = baseSettings.ISO
            };

            // Apply weather-based adjustments
            var weatherReductionFactor = weatherImpact?.OverallLightReductionFactor ?? 1.0;

            if (weatherReductionFactor < 0.7) // Significant weather impact
            {
                // Increase ISO to compensate for light loss
                adjustedSettings.ISO = (int)(baseSettings.ISO * 1.5);

                // Open aperture slightly if possible
                adjustedSettings.Aperture = Math.Max(1.4, baseSettings.Aperture * 0.9);

                // Increase exposure time slightly for deep sky objects
                var targetType = baseSettings.GetType().Name.ToLower();
                if (baseSettings.ShutterSpeed > 60) // Long exposure targets
                {
                    adjustedSettings.ShutterSpeed = baseSettings.ShutterSpeed * 1.3;
                }
            }
            else if (weatherReductionFactor < 0.9) // Moderate weather impact
            {
                // Minor ISO increase
                adjustedSettings.ISO = (int)(baseSettings.ISO * 1.2);
            }

            // Cap ISO at reasonable limits
            adjustedSettings.ISO = Math.Min(12800, adjustedSettings.ISO);

            return adjustedSettings;
        }

        private WeatherForecastDto CreateWeatherForecastFromConditions(ViewModelWeatherConditions weather, DateTime hour)
        {
            return new WeatherForecastDto
            {
                DailyForecasts = new List<DailyForecastDto>
                {
                    new DailyForecastDto
                    {
                        Date = hour.Date,
                        Clouds = (int)weather.CloudCover,
                        Precipitation = weather.PrecipitationProbability,
                        WindSpeed = weather.WindSpeed,
                        Humidity = (int)weather.Humidity,
                        Description = weather.Description,
                        UvIndex = 0 // Not relevant for night astronomy
                    }
                }
            };
        }

        private ServiceWeatherConditions ConvertToServiceWeatherConditions(ViewModelWeatherConditions weather)
        {
            return new ServiceWeatherConditions
            {
                CloudCover = weather.CloudCover / 100.0,
                Precipitation = weather.PrecipitationProbability,
                Humidity = weather.Humidity / 100.0,
                Visibility = weather.Visibility,
                WindSpeed = weather.WindSpeed,
                Description = weather.Description
            };
        }

        #endregion

        #region Helper Methods

        private async Task<List<string>> GetSolarEventsForHourAsync(
            DateTime hour,
            double latitude,
            double longitude,
            CancellationToken cancellationToken)
        {
            var events = new List<string>();

            try
            {
                var sunCondition = _sunCalculatorService.GetSunCondition(hour, latitude, longitude, "UTC");
                var sunAltitude = _sunCalculatorService.GetSolarElevation(hour, latitude, longitude, "UTC");

                if (sunAltitude > 0)
                {
                    if (sunAltitude < 6) events.Add("Civil Twilight");
                    else if (sunAltitude < 12) events.Add("Golden Hour");
                    else events.Add("Daylight");
                }
                else
                {
                    if (sunAltitude > -6) events.Add("Civil Twilight");
                    else if (sunAltitude > -12) events.Add("Nautical Twilight");
                    else if (sunAltitude > -18) events.Add("Astronomical Twilight");
                    else events.Add("True Night");
                }

                // Add specific timing events
                var date = hour.Date;
                var sunrise = _sunCalculatorService.GetSunrise(date, latitude, longitude, "UTC");
                var sunset = _sunCalculatorService.GetSunset(date, latitude, longitude, "UTC");
                var goldenHour = _sunCalculatorService.GetGoldenHour(date, latitude, longitude, "UTC");
                var blueHourStart = _sunCalculatorService.GetBlueHourStart(date, latitude, longitude, "UTC");

                if (Math.Abs((hour - sunrise).TotalMinutes) < 30) events.Add("Sunrise");
                if (Math.Abs((hour - sunset).TotalMinutes) < 30) events.Add("Sunset");
                if (Math.Abs((hour - goldenHour).TotalMinutes) < 30) events.Add("Golden Hour Start");
                if (Math.Abs((hour - blueHourStart).TotalMinutes) < 30) events.Add("Blue Hour");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting solar events for hour {Hour}", hour);
                events.Add("Unknown");
            }

            return events;
        }

        private double CalculateQualityScore(AstroHourlyPrediction domainModel)
        {
            if (!domainModel.TargetEvents.Any()) return 0;

            // Use the overall score from domain model as base
            var baseScore = domainModel.OverallScore;

            // Apply weather adjustments
            var weatherMultiplier = CalculateWeatherMultiplier(domainModel.WeatherConditions);

            return Math.Max(0, Math.Min(100, baseScore * weatherMultiplier));
        }

        private double CalculateEventQuality(AstroTargetEvent targetEvent, DateTime hour)
        {
            // Use reflection to extract quality score or calculate based on available properties
            var qualityProp = targetEvent.GetType().GetProperty("Quality") ??
                             targetEvent.GetType().GetProperty("Score") ??
                             targetEvent.GetType().GetProperty("OptimalityScore");

            if (qualityProp?.GetValue(targetEvent) is double quality)
            {
                return quality;
            }

            // Calculate quality based on altitude and visibility
            var altitudeProp = targetEvent.GetType().GetProperty("Altitude") ??
                              targetEvent.GetType().GetProperty("TargetAltitude") ??
                              targetEvent.GetType().GetProperty("GalacticCenterAltitude") ??
                              targetEvent.GetType().GetProperty("MoonAltitude") ??
                              targetEvent.GetType().GetProperty("PlanetAltitude");

            var visibilityProp = targetEvent.GetType().GetProperty("IsVisible") ??
                                targetEvent.GetType().GetProperty("Visible");

            var altitude = (double)(altitudeProp?.GetValue(targetEvent) ?? 45.0);
            var isVisible = (bool)(visibilityProp?.GetValue(targetEvent) ?? true);

            if (!isVisible || altitude <= 0) return 0;

            // Base calculation on altitude
            var score = CalculateQualityFromAltitude(altitude);

            // Adjust for specific target characteristics
            var targetTypeName = targetEvent.GetType().Name.ToLower();

            if (targetTypeName.Contains("milkyway"))
            {
                // Summer months bonus for Milky Way
                var month = hour.Month;
                if (month >= 5 && month <= 9) score += 20;
            }
            else if (targetTypeName.Contains("moon"))
            {
                // Phase-specific bonus for Moon
                var illuminationProp = targetEvent.GetType().GetProperty("Illumination") ??
                                      targetEvent.GetType().GetProperty("Phase");
                if (illuminationProp?.GetValue(targetEvent) is double illumination)
                {
                    if (illumination > 0.2 && illumination < 0.8) score += 20; // Partial phases show detail
                }
            }
            else if (targetTypeName.Contains("planet"))
            {
                // Magnitude bonus for planets
                var magnitudeProp = targetEvent.GetType().GetProperty("ApparentMagnitude") ??
                                   targetEvent.GetType().GetProperty("Magnitude");
                if (magnitudeProp?.GetValue(targetEvent) is double magnitude)
                {
                    if (magnitude < -2) score += 30;
                    else if (magnitude < 0) score += 20;
                    else if (magnitude < 2) score += 10;
                }
            }
            else if (targetTypeName.Contains("deepsky"))
            {
                // Dark sky bonus for deep sky objects
                if (hour.Hour >= 22 || hour.Hour <= 4) score += 20;
                if (altitude > 50) score += 10; // Higher altitude critical for DSO
            }

            return Math.Min(100, Math.Max(0, score));
        }

        private double CalculateQualityFromAltitude(double altitude)
        {
            if (altitude <= 0) return 0;
            if (altitude > 70) return 90;
            if (altitude > 50) return 80;
            if (altitude > 30) return 70;
            if (altitude > 15) return 60;
            return 40;
        }

        private T GetPropertyValue<T>(object obj, params string[] propertyNames)
        {
            foreach (var propName in propertyNames)
            {
                var prop = obj.GetType().GetProperty(propName);
                if (prop != null && prop.GetValue(obj) is T value)
                {
                    return value;
                }
            }
            return default(T);
        }

        private string GetTargetDisplayName(AstroTargetEvent targetEvent)
        {
            // Use reflection to extract display name from target event properties
            var nameProp = targetEvent.GetType().GetProperty("Name") ??
                          targetEvent.GetType().GetProperty("TargetName") ??
                          targetEvent.GetType().GetProperty("DisplayName") ??
                          targetEvent.GetType().GetProperty("ObjectName") ??
                          targetEvent.GetType().GetProperty("PlanetName") ??
                          targetEvent.GetType().GetProperty("ConstellationName") ??
                          targetEvent.GetType().GetProperty("ShowerName");

            if (nameProp?.GetValue(targetEvent) is string name && !string.IsNullOrEmpty(name))
            {
                return name;
            }

            // Check for phase-specific naming (Moon)
            var phaseNameProp = targetEvent.GetType().GetProperty("PhaseName") ??
                               targetEvent.GetType().GetProperty("Phase");
            if (phaseNameProp?.GetValue(targetEvent) is string phaseName && !string.IsNullOrEmpty(phaseName))
            {
                return $"Moon ({phaseName})";
            }

            // Final fallback - clean up type name
            var typeName = targetEvent.GetType().Name;
            return typeName.Replace("TargetEvent", "").Replace("Event", "").Replace("Target", "");
        }

        private string GetTargetPhotographyNotes(AstroTargetEvent targetEvent)
        {
            var targetType = targetEvent.GetType().Name;
            return targetType switch
            {
                var name when name.Contains("MilkyWay") => "Best captured during summer months. Use wide-angle lens and track galactic center movement.",
                var name when name.Contains("Moon") => "Lunar features visible in all phases. Crater detail best during partial illumination.",
                var name when name.Contains("Planet") => "Planetary detail requires long focal length. Atmospheric stability critical.",
                var name when name.Contains("DeepSky") => "Requires dark skies and long exposures. Consider narrowband filters.",
                var name when name.Contains("StarTrail") => "Create artistic circular or linear trails. Multiple exposures recommended.",
                var name when name.Contains("Meteor") => "Point camera 45-60° from radiant. Capture multiple frames continuously.",
                var name when name.Contains("Constellation") => "Frame entire constellation. Balance star brightness with pattern visibility.",
                _ => "Optimal astrophotography target for current conditions."
            };
        }

        private string GetTargetDifficultyLevel(AstroTargetEvent targetEvent)
        {
            var targetType = targetEvent.GetType().Name;
            return targetType switch
            {
                var name when name.Contains("Moon") => "Beginner",
                var name when name.Contains("Planet") => "Intermediate",
                var name when name.Contains("MilkyWay") => "Intermediate",
                var name when name.Contains("Constellation") => "Beginner",
                var name when name.Contains("StarTrail") => "Intermediate",
                var name when name.Contains("Meteor") => "Advanced",
                var name when name.Contains("DeepSky") => "Advanced",
                _ => "Intermediate"
            };
        }

        private double GetEventAzimuth(AstroTargetEvent targetEvent)
        {
            // This method is no longer used - position extraction moved to MapPositionInformation
            throw new NotImplementedException("Position extraction moved to MapPositionInformation method");
        }

        private double GetEventAltitude(AstroTargetEvent targetEvent)
        {
            // This method is no longer used - position extraction moved to MapPositionInformation
            throw new NotImplementedException("Position extraction moved to MapPositionInformation method");
        }

        private (DateTime? RiseTime, DateTime? SetTime, DateTime? OptimalTime) GetEventTimingInfo(
            AstroTargetEvent targetEvent, DateTime hour)
        {
            // This method is no longer used - timing extraction moved to MapTimingInformation
            throw new NotImplementedException("Timing extraction moved to MapTimingInformation method");
        }

        private TargetRequirements GetTargetRequirements(AstroTargetEvent targetEvent)
        {
            var targetType = targetEvent.GetType().Name;
            return targetType switch
            {
                var name when name.Contains("MilkyWay") => new TargetRequirements
                {
                    OptimalFocalLength = 24,
                    MinFocalLength = 14,
                    MaxFocalLength = 35,
                    MaxAperture = 2.8
                },
                var name when name.Contains("Moon") => new TargetRequirements
                {
                    OptimalFocalLength = 400,
                    MinFocalLength = 200,
                    MaxFocalLength = 800,
                    MaxAperture = 8.0
                },
                var name when name.Contains("Planet") => new TargetRequirements
                {
                    OptimalFocalLength = 600,
                    MinFocalLength = 300,
                    MaxFocalLength = 1200,
                    MaxAperture = 5.6
                },
                var name when name.Contains("DeepSky") => new TargetRequirements
                {
                    OptimalFocalLength = 135,
                    MinFocalLength = 85,
                    MaxFocalLength = 300,
                    MaxAperture = 4.0
                },
                _ => new TargetRequirements
                {
                    OptimalFocalLength = 50,
                    MinFocalLength = 35,
                    MaxFocalLength = 85,
                    MaxAperture = 4.0
                }
            };
        }

        private async Task<Lens> FindBestMatchingLensAsync(
            List<Lens> userLenses,
            TargetRequirements requirements,
            CancellationToken cancellationToken)
        {
            if (!userLenses.Any()) return null;

            // Priority 1: Exact focal length match with good aperture
            var exactMatch = userLenses.FirstOrDefault(l =>
                l.MaxFStop <= requirements.MaxAperture &&
                ((l.IsPrime && Math.Abs(l.MinMM - requirements.OptimalFocalLength) <= 10) ||
                 (!l.IsPrime && l.MaxMM.HasValue &&
                  requirements.OptimalFocalLength >= l.MinMM &&
                  requirements.OptimalFocalLength <= l.MaxMM.Value)));

            if (exactMatch != null) return exactMatch;

            // Priority 2: Focal length range coverage
            var rangeMatch = userLenses.FirstOrDefault(l =>
                (!l.IsPrime && l.MaxMM.HasValue &&
                 ((requirements.MinFocalLength >= l.MinMM && requirements.MinFocalLength <= l.MaxMM.Value) ||
                  (requirements.MaxFocalLength >= l.MinMM && requirements.MaxFocalLength <= l.MaxMM.Value))));

            return rangeMatch;
        }

        private async Task<CameraBody> FindCompatibleCameraAsync(
            Lens lens,
            List<CameraBody> userCameras,
            CancellationToken cancellationToken)
        {
            if (lens == null || !userCameras.Any()) return null;

            try
            {
                // Check lens-camera compatibility
                foreach (var camera in userCameras)
                {
                    var compatibilityResult = await _compatibilityRepository.ExistsAsync(lens.Id, camera.Id, cancellationToken);
                    if (compatibilityResult.IsSuccess && compatibilityResult.Data)
                    {
                        return camera;
                    }
                }

                // Fallback to first camera if no explicit compatibility found
                return userCameras.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error finding compatible camera for lens {LensId}", lens.Id);
                return userCameras.FirstOrDefault();
            }
        }

        private async Task<GenericEquipmentRecommendation> GenerateGenericEquipmentRecommendationAsync(
            TargetRequirements requirements,
            CancellationToken cancellationToken)
        {
            return new GenericEquipmentRecommendation
            {
                LensRecommendation = $"{requirements.OptimalFocalLength:F0}mm f/{requirements.MaxAperture:F1} lens",
                CameraRecommendation = "Full-frame or APS-C camera with good high-ISO performance"
            };
        }

        private BaseCameraSettings GetBaseSettingsForTarget(AstroTargetEvent targetEvent)
        {
            var targetType = targetEvent.GetType().Name;
            return targetType switch
            {
                var name when name.Contains("MilkyWay") => new BaseCameraSettings { Aperture = 2.8, ShutterSpeed = 20, ISO = 3200 },
                var name when name.Contains("Moon") => new BaseCameraSettings { Aperture = 8.0, ShutterSpeed = 0.008, ISO = 200 },
                var name when name.Contains("Planet") => new BaseCameraSettings { Aperture = 5.6, ShutterSpeed = 0.017, ISO = 800 },
                var name when name.Contains("DeepSky") => new BaseCameraSettings { Aperture = 4.0, ShutterSpeed = 300, ISO = 1600 },
                var name when name.Contains("StarTrail") => new BaseCameraSettings { Aperture = 4.0, ShutterSpeed = 240, ISO = 400 },
                var name when name.Contains("Meteor") => new BaseCameraSettings { Aperture = 2.8, ShutterSpeed = 30, ISO = 3200 },
                var name when name.Contains("Constellation") => new BaseCameraSettings { Aperture = 4.0, ShutterSpeed = 60, ISO = 1600 },
                _ => new BaseCameraSettings { Aperture = 4.0, ShutterSpeed = 30, ISO = 1600 }
            };
        }

        private string FormatShutterSpeed(double seconds)
        {
            if (seconds >= 1)
                return $"{seconds:F0}\"";
            else
                return $"1/{Math.Round(1.0 / seconds):F0}";
        }

        private string DetermineSolarEventType(string eventName)
        {
            return eventName switch
            {
                var name when name.Contains("Twilight") => "Twilight Phase",
                var name when name.Contains("Golden") => "Golden Hour",
                var name when name.Contains("Blue") => "Blue Hour",
                var name when name.Contains("Sunrise") || name.Contains("Sunset") => "Solar Transition",
                _ => "Solar Event"
            };
        }

        private string GetSolarLightQuality(string eventName, double sunAltitude)
        {
            return eventName switch
            {
                var name when name.Contains("Golden") => "Warm, soft, directional light",
                var name when name.Contains("Blue") => "Cool, even, ambient light",
                var name when name.Contains("Civil") => "Bright twilight, good visibility",
                var name when name.Contains("Nautical") => "Dim twilight, horizon visible",
                var name when name.Contains("Astronomical") => "Very dim, stars becoming visible",
                var name when name.Contains("Night") => "Dark sky, stars fully visible",
                _ => sunAltitude > 0 ? "Daylight conditions" : "Night conditions"
            };
        }

        private int GetSolarColorTemperature(string eventName, double sunAltitude)
        {
            return eventName switch
            {
                var name when name.Contains("Golden") => 3000,
                var name when name.Contains("Blue") => 15000,
                var name when name.Contains("Civil") => 4000,
                var name when name.Contains("Nautical") => 8000,
                var name when name.Contains("Astronomical") => 12000,
                var name when name.Contains("Night") => 5000,
                _ => sunAltitude > 30 ? 5500 : 3500
            };
        }

        private string GetAstroImpact(string eventName, double sunAltitude)
        {
            return eventName switch
            {
                var name when name.Contains("Night") => "Optimal for all astrophotography targets",
                var name when name.Contains("Astronomical") => "Good for bright targets, planets, and moon",
                var name when name.Contains("Nautical") => "Limited to moon and bright planets only",
                var name when name.Contains("Civil") => "Only very bright objects visible",
                var name when name.Contains("Golden") => "Landscape astrophotography opportunities",
                var name when name.Contains("Blue") => "Twilight compositions with celestial objects",
                _ => sunAltitude > 0 ? "Daylight - astrophotography not recommended" : "Good conditions for astrophotography"
            };
        }

        private bool DoesConflictWithAstro(string eventName, double sunAltitude)
        {
            return eventName switch
            {
                var name when name.Contains("Night") => false,
                var name when name.Contains("Astronomical") => false,
                var name when name.Contains("Nautical") => true, // Partial conflict
                var name when name.Contains("Civil") => true,
                var name when name.Contains("Golden") || name.Contains("Blue") => true, // Can work for some targets
                _ => sunAltitude > -6 // Conflicts if sun is above -6 degrees
            };
        }

        private double CalculateWeatherScore(ViewModelWeatherConditions weatherConditions)
        {
            if (weatherConditions == null) return 70; // Default moderate score

            var score = 100.0;

            // Cloud cover is most critical for astrophotography
            score -= weatherConditions.CloudCover * 0.8;

            // Precipitation impact
            if (weatherConditions.PrecipitationProbability > 0.3)
                score -= weatherConditions.PrecipitationProbability * 60;

            // Wind impact (affects tracking and stability)
            if (weatherConditions.WindSpeed > 15)
                score -= (weatherConditions.WindSpeed - 15) * 2;

            // Humidity impact (affects clarity and equipment)
            if (weatherConditions.Humidity > 80)
                score -= (weatherConditions.Humidity - 80) * 0.5;

            // Visibility impact
            if (weatherConditions.Visibility < 10000)
                score -= (10000 - weatherConditions.Visibility) / 100;

            return Math.Max(0, Math.Min(100, score));
        }

        private double CalculateWeatherMultiplier(ViewModelWeatherConditions weatherConditions)
        {
            var weatherScore = CalculateWeatherScore(weatherConditions);
            return weatherScore / 100.0; // Convert to 0-1 multiplier
        }

        private string GenerateQualityDescription(double qualityScore)
        {
            return qualityScore switch
            {
                >= 90 => "Exceptional astrophotography conditions",
                >= 80 => "Excellent shooting opportunity",
                >= 70 => "Very good conditions for imaging",
                >= 60 => "Good conditions with minor limitations",
                >= 50 => "Fair conditions - manageable challenges",
                >= 40 => "Poor conditions - significant obstacles",
                >= 30 => "Very poor conditions - limited opportunities",
                _ => "Extremely challenging conditions"
            };
        }

        private double CalculateConfidence(AstroHourlyPredictionDisplayModel displayModel)
        {
            var baseConfidence = 85.0; // Base confidence level

            // Reduce confidence based on weather uncertainty
            var weatherConfidence = CalculateWeatherScore(displayModel.DomainModel.WeatherConditions);
            var weatherFactor = weatherConfidence / 100.0;

            // Reduce confidence for longer forecast periods
            var hoursFromNow = (displayModel.Hour - DateTime.Now).TotalHours;
            var timeFactor = Math.Max(0.5, 1.0 - (hoursFromNow * 0.01)); // 1% per hour decay

            // Higher confidence for well-established targets
            var targetFactor = displayModel.AstroEvents.Any() ? 1.0 : 0.8;

            var finalConfidence = baseConfidence * weatherFactor * timeFactor * targetFactor;
            return Math.Max(30, Math.Min(95, finalConfidence));
        }

        private string GenerateRecommendations(AstroHourlyPredictionDisplayModel displayModel, WeatherImpactAnalysis weatherImpact = null)
        {
            var recommendations = new List<string>();

            // Target-specific recommendations
            if (displayModel.AstroEvents.Any())
            {
                var bestEvent = displayModel.AstroEvents.OrderByDescending(e => e.QualityRank).First();
                recommendations.Add($"Focus on {bestEvent.TargetName} - {bestEvent.DifficultyLevel.ToLower()} difficulty");

                if (bestEvent.IsUserEquipment)
                {
                    recommendations.Add($"Use your {bestEvent.RecommendedCamera} with {bestEvent.RecommendedLens}");
                }
                else
                {
                    recommendations.Add($"Consider acquiring {bestEvent.RecommendedLens} for optimal results");
                }
            }

            // Weather-based recommendations using analysis
            if (weatherImpact != null)
            {
                var weatherReduction = 1.0 - weatherImpact.OverallLightReductionFactor;
                if (weatherReduction > 0.5)
                {
                    recommendations.Add("Severe weather impact - consider rescheduling or using weather protection");
                }
                else if (weatherReduction > 0.2)
                {
                    recommendations.Add("Moderate weather challenges - adjust exposure settings accordingly");
                }
                else if (weatherReduction < 0.1)
                {
                    recommendations.Add("Excellent weather window - ideal conditions for extended sessions");
                }

                // Add weather-specific alerts
                if (weatherImpact.Alerts?.Any() == true)
                {
                    var criticalAlerts = weatherImpact.Alerts.Where(a => a.Severity == AlertSeverity.Critical);
                    foreach (var alert in criticalAlerts.Take(1))
                    {
                        recommendations.Add($"Weather alert: {alert.Message}");
                    }
                }
            }
            else
            {
                // Fallback weather recommendations based on domain model
                var weatherScore = CalculateWeatherScore(displayModel.DomainModel.WeatherConditions);
                if (weatherScore < 50)
                {
                    recommendations.Add("Monitor weather conditions closely - significant challenges expected");
                }
                else if (weatherScore > 80)
                {
                    recommendations.Add("Excellent weather window - plan extended shooting session");
                }
            }

            // Solar event recommendations
            if (displayModel.SolarEvents.Any())
            {
                var conflictingEvents = displayModel.SolarEvents.Where(e => e.ConflictsWithAstro).ToList();
                if (conflictingEvents.Any())
                {
                    recommendations.Add($"Be aware of {conflictingEvents.First().EventName} - may limit target visibility");
                }
            }

            // Time-specific recommendations
            var hoursFromNow = (displayModel.Hour - DateTime.Now).TotalHours;
            if (hoursFromNow < 2)
            {
                recommendations.Add("Prepare equipment now - optimal window approaching");
            }
            else if (hoursFromNow > 12)
            {
                recommendations.Add("Plan ahead - conditions may change");
            }

            // Default recommendation if none generated
            if (!recommendations.Any())
            {
                recommendations.Add("Standard astrophotography precautions apply");
            }

            return string.Join(". ", recommendations.Take(3)) + ".";
        }

        #endregion

        #region Supporting Data Classes

        public class TargetRequirements
        {
            public double OptimalFocalLength { get; set; }
            public double MinFocalLength { get; set; }
            public double MaxFocalLength { get; set; }
            public double MaxAperture { get; set; }
        }

        public class BaseCameraSettings
        {
            public double Aperture { get; set; }
            public double ShutterSpeed { get; set; }
            public int ISO { get; set; }
        }

        #endregion
    }
}