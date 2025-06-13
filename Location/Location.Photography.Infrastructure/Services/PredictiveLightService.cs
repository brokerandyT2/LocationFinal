// Location.Photography.Infrastructure/Services/PredictiveLightService.cs
using Location.Core.Application.Weather.DTOs;
using Location.Photography.Application.Services;
using Location.Photography.Domain.Models;
using Location.Photography.Domain.Services;
using Location.Photography.Infrastructure.Resources;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExposureTriangle = Location.Photography.Domain.Models.ExposureTriangle;
using HourlyLightPrediction = Location.Photography.Domain.Models.HourlyLightPrediction;
using LightCharacteristics = Location.Photography.Domain.Models.LightCharacteristics;

namespace Location.Photography.Infrastructure.Services
{
    public class PredictiveLightService : IPredictiveLightService
    {
        private readonly ISunCalculatorService _sunCalculatorService;
        private readonly IExposureTriangleService _exposureTriangleService;
        private readonly ILogger<PredictiveLightService> _logger;

        private readonly Dictionary<int, CalibrationData> _locationCalibrations = new();

        // Enhanced lux-based constants
        private const double BaseLuxDirectSunlight = 100000.0; // Lux at noon, clear sky, sea level
        private const double CalibrationWeight = 0.7;
        private const double HistoricalWeight = 0.3;

        public PredictiveLightService(
            ISunCalculatorService sunCalculatorService,
            IExposureTriangleService exposureTriangleService,
            ILogger<PredictiveLightService> logger)
        {
            _sunCalculatorService = sunCalculatorService ?? throw new ArgumentNullException(nameof(sunCalculatorService));
            _exposureTriangleService = exposureTriangleService ?? throw new ArgumentNullException(nameof(exposureTriangleService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<WeatherImpactAnalysis> AnalyzeWeatherImpactAsync(
            WeatherImpactAnalysisRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var analysis = new WeatherImpactAnalysis();

                if (request.WeatherForecast?.DailyForecasts?.Any() == true)
                {
                    var currentForecast = request.WeatherForecast.DailyForecasts.First();
                    analysis.CurrentConditions = MapToWeatherConditions(currentForecast);

                    analysis.HourlyImpacts = await CalculateHourlyWeatherImpactsAsync(
                        request.WeatherForecast, request.SunTimes, cancellationToken).ConfigureAwait(false);

                    analysis.OverallLightReductionFactor = CalculateOverallLightReduction(analysis.HourlyImpacts);

                    analysis.Summary = GenerateWeatherSummary(analysis);
                    analysis.Alerts = GenerateWeatherAlerts(analysis);
                }

                return analysis;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing weather impact");
                return new WeatherImpactAnalysis
                {
                    Summary = AppResources.PredictiveLight_Error_WeatherAnalysisUnavailable,
                    OverallLightReductionFactor = 0.8
                };
            }
        }

        public async Task<List<HourlyLightPrediction>> GenerateHourlyPredictionsAsync(
            PredictiveLightRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var predictions = new List<HourlyLightPrediction>();
                var startTime = request.TargetDate.Date.ToLocalTime();

                // Parallelize prediction generation to improve performance
                var predictionTasks = new List<Task<HourlyLightPrediction>>();

                for (int hour = 0; hour < request.PredictionWindowHours; hour++)
                {
                    var targetTime = startTime.AddHours(hour);
                    predictionTasks.Add(GenerateSingleHourPredictionAsync(request, targetTime, cancellationToken));
                }

                // Process all predictions in parallel
                var allPredictions = await Task.WhenAll(predictionTasks).ConfigureAwait(false);
                predictions.AddRange(allPredictions);

                return predictions.OrderBy(x => x.DateTime).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating hourly predictions");
                return new List<HourlyLightPrediction>();
            }
        }

        public async Task<PredictiveLightRecommendation> GenerateRecommendationAsync(
            PredictiveLightRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var hourlyPredictions = await GenerateHourlyPredictionsAsync(request, cancellationToken).ConfigureAwait(false);

                var recommendation = new PredictiveLightRecommendation
                {
                    GeneratedAt = DateTime.Now
                };

                recommendation.BestTimeWindow = FindBestShootingWindow(hourlyPredictions);

                recommendation.AlternativeWindows = FindAlternativeShootingWindows(hourlyPredictions)
                    .Take(3).ToList();

                recommendation.OverallRecommendation = GenerateOverallRecommendation(
                    recommendation.BestTimeWindow, recommendation.AlternativeWindows);

                recommendation.KeyInsights = GenerateKeyInsights(hourlyPredictions, request.WeatherImpact);

                if (_locationCalibrations.TryGetValue(request.LocationId, out var calibration))
                {
                    recommendation.CalibrationAccuracy = CalculateCalibrationAccuracy(calibration);
                    recommendation.RequiresRecalibration =
                        DateTime.Now.Subtract(calibration.LastCalibration).TotalHours > 12;
                }

                return recommendation;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating recommendation");
                return new PredictiveLightRecommendation
                {
                    GeneratedAt = DateTime.Now,
                    OverallRecommendation = AppResources.PredictiveLight_Error_ServiceTemporarilyUnavailable
                };
            }
        }

        public async Task CalibrateWithActualReadingAsync(
            LightMeterCalibrationRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var sunPosition = GetSunPosition(request.DateTime, request.Latitude, request.Longitude);

                // Enhanced: Use lux-based theoretical calculation
                var theoreticalLux = CalculateLuxFromConditions(sunPosition, request.WeatherConditions);
                var theoreticalEV = ConvertLuxToEV(theoreticalLux);

                var calibrationOffset = request.ActualEV - theoreticalEV;

                if (!_locationCalibrations.TryGetValue(request.LocationId, out var calibration))
                {
                    calibration = new CalibrationData { LocationId = request.LocationId };
                    _locationCalibrations[request.LocationId] = calibration;
                }

                if (calibration.CalibrationReadings.Any())
                {
                    var recentReadings = calibration.CalibrationReadings
                        .Where(r => DateTime.Now.Subtract(r.DateTime).TotalHours <= 24)
                        .ToList();

                    if (recentReadings.Any())
                    {
                        var historicalAverage = recentReadings.Average(r => r.CalibrationOffset);
                        calibrationOffset = (calibrationOffset * CalibrationWeight) +
                                          (historicalAverage * HistoricalWeight);
                    }
                }

                calibration.CalibrationReadings.Add(new CalibrationReading
                {
                    DateTime = request.DateTime,
                    ActualEV = request.ActualEV,
                    TheoreticalEV = theoreticalEV,
                    CalibrationOffset = calibrationOffset,
                    WeatherConditions = request.WeatherConditions,
                    TheoreticalLux = theoreticalLux
                });

                calibration.LastCalibration = DateTime.Now;
                calibration.CurrentOffset = calibrationOffset;

                calibration.CalibrationReadings = calibration.CalibrationReadings
                    .Where(r => DateTime.Now.Subtract(r.DateTime).TotalDays <= 7)
                    .ToList();

                _logger.LogInformation(
                    "Calibrated location {LocationId} with offset {Offset:F2} EV (Theoretical: {TheoreticalLux:F0} lux)",
                    request.LocationId, calibrationOffset, theoreticalLux);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calibrating with actual reading");
            }
        }

        #region Enhanced Lux-Based Calculations

        /// <summary>
        /// Calculate actual lux based on sun position and weather conditions
        /// Using environmental data from lux guidelines: 100k lux direct sun, with atmospheric corrections
        /// </summary>
        private double CalculateLuxFromConditions(SunPositionDto sunPosition, WeatherConditions? weather)
        {
            // Start with base lux depending on sun position
            double baseLux = BaseLuxDirectSunlight;

            if (!sunPosition.IsAboveHorizon)
            {
                // Night/twilight - minimal ambient light
                return Math.Max(0.1, 10 * Math.Max(0, sunPosition.Elevation + 18) / 18); // Civil twilight formula
            }

            // 1. Adjust for sun elevation (atmospheric path length)
            // Lower sun = more atmosphere to penetrate = less light
            var elevationFactor = Math.Sin(Math.Max(0, sunPosition.Elevation) * Math.PI / 180.0);

            // Apply atmospheric absorption based on air mass
            var airMass = CalculateAirMass(sunPosition.Elevation);
            var atmosphericTransmission = Math.Pow(0.7, airMass); // Standard atmospheric model
            baseLux *= elevationFactor * atmosphericTransmission;

            // 2. Apply weather-based reductions
            if (weather != null)
            {
                baseLux = ApplyWeatherReductions(baseLux, weather);
            }

            return Math.Max(0.1, baseLux); // Never go below 0.1 lux
        }

        /// <summary>
        /// Apply weather-based light reductions using actual environmental data
        /// </summary>
        private double ApplyWeatherReductions(double baseLux, WeatherConditions weather)
        {
            double currentLux = baseLux;

            // Cloud cover impact (progressive reduction)
            // 0% clouds = no reduction, 100% clouds = 90% reduction (overcast ~1000 lux vs 100k clear)
            var cloudReduction = 1.0 - (weather.CloudCover * 0.9);
            currentLux *= Math.Max(0.1, cloudReduction);

            // Precipitation impact (from environmental data)
            if (weather.Precipitation > 0)
            {
                // Light rain = 50% reduction, heavy rain/snow = 80% reduction
                var precipitationReduction = weather.Precipitation > 0.5 ? 0.2 : 0.5;
                currentLux *= precipitationReduction;
            }

            // Atmospheric clarity (visibility + humidity)
            var atmosphericReduction = CalculateAtmosphericReduction(weather);
            currentLux *= atmosphericReduction;

            return currentLux;
        }

        /// <summary>
        /// Calculate atmospheric light reduction based on visibility and humidity
        /// </summary>
        private double CalculateAtmosphericReduction(WeatherConditions weather)
        {
            double reduction = 1.0;

            // Visibility impact (from HourlyForecastEntity.Visibility in meters)
            if (weather.Visibility < 10000) // Normal visibility is 10km+
            {
                // Poor visibility = haze/pollution/fog
                var visibilityFactor = Math.Max(0.3, weather.Visibility / 10000.0);
                reduction *= visibilityFactor;
            }

            // Humidity impact (creates atmospheric haze)
            // High humidity = light scattering = reduced direct light
            var humidityReduction = 1.0 - (weather.Humidity * 0.1); // Up to 10% reduction at 100% humidity
            reduction *= Math.Max(0.9, humidityReduction);

            return Math.Max(0.3, reduction); // Never reduce below 30% for atmospheric effects
        }

        /// <summary>
        /// Calculate air mass for atmospheric absorption
        /// </summary>
        private double CalculateAirMass(double elevationDegrees)
        {
            if (elevationDegrees <= 0) return 40; // Below horizon = maximum air mass

            var elevationRadians = elevationDegrees * Math.PI / 180.0;

            // Simplified air mass formula for photography purposes
            // At zenith (90°): air mass = 1, at horizon (0°): air mass = ~40
            return 1.0 / Math.Sin(elevationRadians);
        }

        /// <summary>
        /// Convert lux to EV using standard photography formula
        /// EV = log2(Lux / 2.5) - standard conversion for ISO 100, f/1.0
        /// </summary>
        private double ConvertLuxToEV(double lux)
        {
            if (lux <= 0) return -10; // Very dark conditions

            // Standard formula: EV = log2(Lux / 2.5)
            // This assumes ISO 100 and accounts for camera sensor efficiency
            return Math.Log(lux / 2.5) / Math.Log(2);
        }

        /// <summary>
        /// Generate enhanced exposure settings using calculated EV and ExposureTriangleService
        /// </summary>
        private async Task<ExposureTriangle> GenerateEnhancedExposureSettingsAsync(double calculatedEV, CancellationToken cancellationToken = default)
        {
            try
            {
                // Base exposure at calculated EV (ISO 100, f/8 as starting point)
                var baseISO = "100";
                var baseAperture = "f/8";

                // Calculate base shutter speed for EV
                // EV = log2(N²/t) where N=aperture, t=shutter time in seconds
                // For f/8: EV = log2(64/t), so t = 64/2^EV
                var shutterTime = 64.0 / Math.Pow(2, calculatedEV);
                var baseShutterSpeed = FormatShutterSpeed(shutterTime);

                // For optimal photography, prefer certain settings based on light level
                if (calculatedEV > 12) // Bright light
                {
                    // Use smaller aperture for sharpness, faster shutter
                    return new ExposureTriangle
                    {
                        Aperture = "f/11",
                        ShutterSpeed = FormatShutterSpeed(64.0 / Math.Pow(2, calculatedEV) * (11 * 11) / (8 * 8)),
                        ISO = "100"
                    };
                }
                else if (calculatedEV > 8) // Moderate light
                {
                    // Balanced settings
                    return new ExposureTriangle
                    {
                        Aperture = "f/8",
                        ShutterSpeed = baseShutterSpeed,
                        ISO = "100"
                    };
                }
                else if (calculatedEV > 4) // Low light
                {
                    // Open aperture, increase ISO to maintain reasonable shutter speed
                    return new ExposureTriangle
                    {
                        Aperture = "f/4",
                        ShutterSpeed = FormatShutterSpeed(16.0 / Math.Pow(2, calculatedEV)),
                        ISO = "400"
                    };
                }
                else // Very low light
                {
                    // Wide aperture, high ISO
                    return new ExposureTriangle
                    {
                        Aperture = "f/2.8",
                        ShutterSpeed = FormatShutterSpeed(8.0 / Math.Pow(2, calculatedEV)),
                        ISO = "1600"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error generating enhanced exposure settings, using fallback");
                return GenerateExposureSettings(calculatedEV);
            }
        }

        /// <summary>
        /// Format shutter speed as photography notation
        /// </summary>
        private string FormatShutterSpeed(double seconds)
        {
            if (seconds >= 1)
            {
                if (seconds >= 10) return $"{seconds:F0}\"";
                return $"{seconds:F1}\"";
            }
            else
            {
                var fraction = 1.0 / seconds;
                if (fraction < 10) return $"1/{fraction:F1}";
                return $"1/{fraction:F0}";
            }
        }

        /// <summary>
        /// Validate calculated lux against UV Index for accuracy
        /// </summary>
        private double ValidateWithUVIndex(double calculatedLux, double uvIndex, SunPositionDto sunPosition)
        {
            if (!sunPosition.IsAboveHorizon || uvIndex <= 0) return calculatedLux;

            // UV Index correlates with solar radiation intensity
            // UV Index 10+ = very high sun intensity = should have high lux
            // UV Index 1-2 = low sun intensity = should have reduced lux

            var expectedUVBasedOnLux = Math.Max(0, (calculatedLux / BaseLuxDirectSunlight) * 10);
            var uvDiscrepancy = Math.Abs(uvIndex - expectedUVBasedOnLux) / Math.Max(uvIndex, expectedUVBasedOnLux);

            // If there's a significant discrepancy (>50%), adjust our calculation
            if (uvDiscrepancy > 0.5)
            {
                var correctionFactor = uvIndex / Math.Max(1, expectedUVBasedOnLux);
                correctionFactor = Math.Max(0.5, Math.Min(2.0, correctionFactor)); // Limit correction

                _logger.LogDebug("UV Index validation: Expected {Expected:F1}, Actual {Actual:F1}, Correction {Correction:F2}",
                    expectedUVBasedOnLux, uvIndex, correctionFactor);

                return calculatedLux * correctionFactor;
            }

            return calculatedLux;
        }

        #endregion

        #region Enhanced Prediction Methods

        private async Task<HourlyLightPrediction> GenerateSingleHourPredictionAsync(
               PredictiveLightRequest request,
               DateTime targetTime,
               CancellationToken cancellationToken)
        {
            // Move intensive calculations to background thread to prevent UI blocking
            return await Task.Run(async () =>
            {
                var prediction = new HourlyLightPrediction { DateTime = targetTime };

                prediction.SunPosition = GetSunPosition(targetTime, request.Latitude, request.Longitude);

                // Enhanced: Use lux-based calculation instead of simple theoretical EV
                var weatherConditions = GetWeatherConditionsForHour(request.WeatherImpact, targetTime);
                var calculatedLux = CalculateLuxFromConditions(prediction.SunPosition, weatherConditions);

                // Validate with UV Index if available
                if (weatherConditions?.UvIndex > 0)
                {
                    calculatedLux = ValidateWithUVIndex(calculatedLux, weatherConditions.UvIndex, prediction.SunPosition);
                }

                var calculatedEV = ConvertLuxToEV(calculatedLux);

                // Enhanced confidence calculation with time decay
                bool hasCalibration = _locationCalibrations.TryGetValue(request.LocationId, out var calibration) &&
                                     request.LastCalibrationReading.HasValue;

                // Base confidence
                var baseConfidence = hasCalibration ? 0.95 : 0.85;

                // Time decay: 0.5% per hour
                var hoursFromNow = (targetTime - DateTime.Now).TotalHours;
                var timeDecayFactor = Math.Max(0.2, 1.0 - (hoursFromNow * 0.005)); // 0.5% per hour

                // Weather data freshness factor
                var weatherFreshnessFactor = CalculateWeatherFreshnessFactor(weatherConditions, hoursFromNow);

                // Apply calibration if available
                if (hasCalibration)
                {
                    calculatedEV += calibration.CurrentOffset;
                    prediction.ConfidenceReason = string.Format(AppResources.PredictiveLight_Insight_OptimalHours, hoursFromNow);
                }
                else
                {
                    prediction.ConfidenceReason = string.Format(AppResources.PredictiveLight_Insight_OptimalHours, hoursFromNow);
                }

                // Final confidence calculation
                var finalConfidence = baseConfidence * timeDecayFactor * weatherFreshnessFactor;
                prediction.ConfidenceLevel = Math.Max(0.2, Math.Min(0.95, finalConfidence));

                // Add time decay information to confidence reason
                if (hoursFromNow > 48)
                {
                    prediction.ConfidenceReason += AppResources.PredictiveLight_ConfidenceModifier_LongRangeForecast;
                }
                else if (hoursFromNow > 24)
                {
                    prediction.ConfidenceReason += AppResources.PredictiveLight_ConfidenceModifier_MediumRangeForecast;
                }

                prediction.PredictedEV = Math.Round(calculatedEV, 1);
                prediction.EVConfidenceMargin = CalculateConfidenceMargin(prediction.ConfidenceLevel);

                // Enhanced: Use new exposure calculation method
                prediction.SuggestedSettings = await GenerateEnhancedExposureSettingsAsync(calculatedEV, cancellationToken).ConfigureAwait(false);

                prediction.LightQuality = CalculateLightCharacteristics(
                    prediction.SunPosition, calculatedLux, targetTime, request.SunTimes, weatherConditions);

                prediction.Recommendations = GenerateHourlyRecommendations(
                    prediction.LightQuality, prediction.SunPosition, calculatedLux, weatherConditions);

                prediction.IsOptimalForPhotography = IsOptimalForPhotography(
                    prediction.LightQuality, prediction.SunPosition, calculatedLux, weatherConditions);

                return prediction;

            }, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Calculate weather data freshness factor for confidence adjustments
        /// Recent weather data decays slower than distant forecasts
        /// </summary>
        /// <param name="weather">Weather conditions</param>
        /// <param name="hoursFromNow">Hours into the future</param>
        /// <returns>Freshness factor (0.85-1.0)</returns>
        private double CalculateWeatherFreshnessFactor(WeatherConditions? weather, double hoursFromNow)
        {
            // No weather data = reduced confidence
            if (weather == null) return 0.8;

            // Recent weather data (0-24h) decays slower than distant forecasts
            if (hoursFromNow <= 24)
                return 1.0; // No additional decay for first 24h - most reliable

            if (hoursFromNow <= 48)
                return 0.95; // Slight decay for 24-48h - still quite reliable

            if (hoursFromNow <= 72)
                return 0.90; // Moderate decay for 48-72h - good reliability

            if (hoursFromNow <= 120) // 5 days (business rule limit)
                return 0.85; // More decay for 72h+ forecasts - weather becomes less predictable

            // Beyond 5 days (shouldn't happen with business rules, but safety)
            return 0.75;
        }
        #endregion

        #region Private Helper Methods

        private async Task<List<HourlyWeatherImpact>> CalculateHourlyWeatherImpactsAsync(
            WeatherForecastDto forecast,
            Location.Photography.Domain.Models.EnhancedSunTimes sunTimes,
            CancellationToken cancellationToken)
        {
            var impacts = new List<HourlyWeatherImpact>();

            if (forecast.DailyForecasts.Any())
            {
                var dailyForecast = forecast.DailyForecasts.First();
                var baseDate = dailyForecast.Date.Date;

                // Parallelize weather impact calculations to improve performance
                var impactTasks = new List<Task<HourlyWeatherImpact>>();

                for (int hour = 0; hour < 24; hour++)
                {
                    var hourTime = baseDate.AddHours(hour);
                    impactTasks.Add(Task.Run(() => CalculateHourlyWeatherImpact(dailyForecast, hourTime, sunTimes), cancellationToken));
                }

                var hourlyImpacts = await Task.WhenAll(impactTasks).ConfigureAwait(false);
                impacts.AddRange(hourlyImpacts);
            }

            return impacts;
        }

        private HourlyWeatherImpact CalculateHourlyWeatherImpact(
            DailyForecastDto forecast,
            DateTime hour,
            Location.Photography.Domain.Models.EnhancedSunTimes sunTimes)
        {
            var impact = new HourlyWeatherImpact { Hour = hour };

            // Enhanced: Use lux-based calculations
            var sunPosition = GetSunPosition(hour, 0, 0); // Dummy coordinates for relative calculation
            var weatherConditions = new WeatherConditions
            {
                CloudCover = forecast.Clouds / 100.0,
                Precipitation = forecast.Precipitation ?? 0,
                Humidity = forecast.Humidity / 100.0,
                Visibility = 10000, // Default visibility
                UvIndex = forecast.UvIndex
            };

            var baseLux = CalculateLuxFromConditions(sunPosition, null);
            var weatherAdjustedLux = CalculateLuxFromConditions(sunPosition, weatherConditions);

            impact.LightReductionFactor = Math.Max(0.1, weatherAdjustedLux / baseLux);

            impact.ColorTemperatureShift = CalculateColorTemperatureShift(forecast, hour, sunTimes);
            impact.PredictedQuality = DetermineLightQuality(forecast, hour, sunTimes);
            impact.Reasoning = GenerateHourlyReasoning(forecast, hour, sunTimes, impact.LightReductionFactor);

            return impact;
        }

        private SunPositionDto GetSunPosition(DateTime dateTime, double latitude, double longitude)
        {
            var azimuth = _sunCalculatorService.GetSolarAzimuth(dateTime, latitude, longitude, TimeZoneInfo.Local.ToString());
            var elevation = _sunCalculatorService.GetSolarElevation(dateTime, latitude, longitude, TimeZoneInfo.Local.ToString());

            return new SunPositionDto
            {
                Azimuth = azimuth,
                Elevation = elevation,
                Distance = 1.0
            };
        }

        private WeatherConditions? GetWeatherConditionsForHour(WeatherImpactAnalysis? impact, DateTime hour)
        {
            if (impact?.HourlyImpacts == null) return null;

            var hourlyImpact = impact.HourlyImpacts.FirstOrDefault(h => h.Hour.Hour == hour.Hour);
            return impact.CurrentConditions; // Simplified - use current conditions for all hours
        }

        private WeatherConditions MapToWeatherConditions(DailyForecastDto forecast)
        {
            return new WeatherConditions
            {
                CloudCover = forecast.Clouds / 100.0,
                Precipitation = forecast.Precipitation ?? 0,
                Humidity = forecast.Humidity / 100.0,
                Visibility = 10.0, // Default to 10km visibility
                AirQualityIndex = 50, // Default moderate air quality
                WindSpeed = forecast.WindSpeed,
                Description = forecast.Description,
                UvIndex = forecast.UvIndex
            };
        }

        private double CalculateOverallLightReduction(List<HourlyWeatherImpact> impacts)
        {
            if (!impacts.Any()) return 0.8;

            var daylight = impacts.Where(i => i.Hour.Hour >= 6 && i.Hour.Hour <= 20);
            return daylight.Any() ? daylight.Average(i => i.LightReductionFactor) : 0.8;
        }

        private string GenerateWeatherSummary(WeatherImpactAnalysis analysis)
        {
            var reduction = (1.0 - analysis.OverallLightReductionFactor) * 100;

            if (reduction < 10)
                return "Excellent lighting conditions with minimal weather impact";
            else if (reduction < 30)
                return $"Good conditions with {reduction:F0}% light reduction from weather";
            else if (reduction < 60)
                return $"Moderate impact with {reduction:F0}% light reduction - plan accordingly";
            else
                return $"Challenging conditions with {reduction:F0}% light reduction";
        }

        private List<WeatherAlert> GenerateWeatherAlerts(WeatherImpactAnalysis analysis)
        {
            var alerts = new List<WeatherAlert>();

            if (analysis.OverallLightReductionFactor < 0.4)
            {
                alerts.Add(new WeatherAlert
                {
                    Type = AlertType.Weather,
                    Message = AppResources.PredictiveLight_Recommendation_ChallengingConditions,
                    Severity = AlertSeverity.Critical,
                    ValidFrom = DateTime.Now,
                    ValidTo = DateTime.Now.AddHours(24)
                });
            }

            return alerts;
        }

        private double CalculateConfidenceMargin(double confidenceLevel)
        {
            return (1.0 - confidenceLevel) * 2.0;
        }

        private ExposureTriangle GenerateExposureSettings(double ev)
        {
            var fStop = "f/8";
            var iso = "ISO 100";

            var shutterSpeed = Math.Pow(2, ev - 6);

            string shutterSpeedStr;
            if (shutterSpeed >= 1)
                shutterSpeedStr = $"1/{(int)Math.Round(1 / shutterSpeed)}s";
            else
                shutterSpeedStr = $"{shutterSpeed:F1}s";

            return new ExposureTriangle
            {
                Aperture = fStop,
                ShutterSpeed = shutterSpeedStr,
                ISO = iso
            };
        }

        private double CalculateColorTemperatureShift(DailyForecastDto forecast, DateTime hour, Location.Photography.Domain.Models.EnhancedSunTimes sunTimes)
        {
            return 0;
        }

        private LightQuality DetermineLightQuality(DailyForecastDto forecast, DateTime hour, Location.Photography.Domain.Models.EnhancedSunTimes sunTimes)
        {
            return LightQuality.Soft;
        }

        private string GenerateHourlyReasoning(DailyForecastDto forecast, DateTime hour, Location.Photography.Domain.Models.EnhancedSunTimes sunTimes, double reduction)
        {
            return  $"Light reduced by {(1 - reduction) * 100:F0}% due to weather conditions";
        }

        private LightCharacteristics CalculateLightCharacteristics(SunPositionDto sunPosition, double lux, DateTime time, Location.Photography.Domain.Models.EnhancedSunTimes sunTimes, WeatherConditions? weather)
        {
            var colorTemp = CalculateColorTemperatureFromConditions(sunPosition, weather);
            var optimalFor = DetermineOptimalPhotographyType(sunPosition, lux, weather);

            return new LightCharacteristics
            {
                ColorTemperature = colorTemp,
                SoftnessFactor = CalculateSoftnessFactor(sunPosition, weather),
                OptimalFor = optimalFor
            };
        }

        private double CalculateColorTemperatureFromConditions(SunPositionDto sunPosition, WeatherConditions? weather)
        {
            var baseTemp = 5500; // Daylight

            // Sun elevation affects color temperature
            if (sunPosition.Elevation < 10) baseTemp = 3000; // Golden hour
            else if (sunPosition.Elevation < 20) baseTemp = 4000; // Early morning/late afternoon

            // Clouds make light cooler
            if (weather?.CloudCover > 0)
            {
                var cloudAdjustment = weather.CloudCover * 500; // Up to 500K cooler with full clouds
                baseTemp += (int)cloudAdjustment;
            }

            return Math.Max(2500, Math.Min(7000, baseTemp));
        }

        private double CalculateSoftnessFactor(SunPositionDto sunPosition, WeatherConditions? weather)
        {
            var softness = 0.3; // Base hardness

            // Clouds act as giant softbox
            if (weather?.CloudCover > 0)
            {
                softness += weather.CloudCover * 0.6; // Up to 60% increase in softness
            }

            // Lower sun angle = softer light due to atmospheric scattering
            if (sunPosition.Elevation < 30)
            {
                softness += (30 - sunPosition.Elevation) / 30.0 * 0.3;
            }

            return Math.Max(0.1, Math.Min(1.0, softness));
        }

        private string DetermineOptimalPhotographyType(SunPositionDto sunPosition, double lux, WeatherConditions? weather)
        {
            if (weather?.Precipitation > 0.5) return "Moody/dramatic photography";
            if (sunPosition.Elevation < 10 && lux > 1000) return "Portraits, golden hour shots";
            if (weather?.CloudCover > 0.7) return "Even lighting, portraits";
            if (lux > 50000) return "Landscapes, architecture";
            if (lux < 1000) return "Low light, indoor photography";
            return "General photography";
        }

        private List<string> GenerateHourlyRecommendations(LightCharacteristics quality, SunPositionDto position, double lux, WeatherConditions? weather)
        {
            var recommendations = new List<string>();

            // Lux-based recommendations
            if (lux > 80000)
                recommendations.Add("Excellent light - consider using ND filters for motion blur effects");
            else if (lux > 50000)
                recommendations.Add("Bright conditions - great for landscape photography");
            else if (lux > 20000)
                recommendations.Add("Good indirect light - ideal for portraits");
            else if (lux > 1000)
                recommendations.Add("Overcast conditions - even lighting for portraits");
            else if (lux > 100)
                recommendations.Add("Low light - consider tripod and higher ISO");
            else
                recommendations.Add("Very low light - requires artificial lighting or long exposure");

            // Weather-specific recommendations
            if (weather != null)
            {
                if (weather.Precipitation > 0.3)
                    recommendations.Add("Bring weather protection for gear");

                if (weather.WindSpeed > 10)
                    recommendations.Add("Use faster shutter speeds for stability");

                if (weather.CloudCover > 0.7)
                    recommendations.Add("Great for even, soft lighting");

                if (weather.Humidity > 0.8)
                    recommendations.Add("Watch for lens condensation in high humidity");

                if (weather.Visibility < 5000)
                    recommendations.Add("Reduced visibility - consider closer subjects");

                if (weather.UvIndex > 7)
                    recommendations.Add("Consider UV filter and sun protection");
            }

            // Sun position recommendations
            if (position.IsAboveHorizon)
            {
                if (position.Elevation < 15 && lux > 5000)
                    recommendations.Add("Perfect golden hour conditions");
                else if (position.Elevation > 60)
                    recommendations.Add("High sun - watch for harsh shadows");
                else if (position.Elevation < 5)
                    recommendations.Add("Blue hour approaching - great for cityscapes");
            }

            return recommendations.Any() ? recommendations : new List<string> { "Standard photography conditions" };
        }

        private bool IsOptimalForPhotography(LightCharacteristics quality, SunPositionDto position, double lux, WeatherConditions? weather)
        {
            // Optimal conditions: sufficient light, manageable weather, good sun position
            var hasSufficientLight = lux > 100; // Above minimum for handheld photography
            var weatherIsManageable = weather?.Precipitation < 0.7 && weather?.WindSpeed < 25;
            var isGoodSunPosition = position.IsAboveHorizon && position.Elevation > 5;
            var isGoldenHour = position.Elevation < 15 && position.Elevation > 0 && lux > 1000;

            // Golden hour is always optimal regardless of other factors
            if (isGoldenHour) return true;

            // Otherwise, need all conditions to be favorable
            return hasSufficientLight && weatherIsManageable && isGoodSunPosition;
        }

        private OptimalShootingWindow FindBestShootingWindow(List<HourlyLightPrediction> predictions)
        {
            var optimalPredictions = predictions
                .Where(p => p.IsOptimalForPhotography && p.SunPosition.IsAboveHorizon)
                .OrderByDescending(p => p.ConfidenceLevel)
                .ThenByDescending(p => p.PredictedEV)
                .ToList();

            if (optimalPredictions.Any())
            {
                var best = optimalPredictions.First();
                return new OptimalShootingWindow
                {
                    StartTime = best.DateTime.AddMinutes(-30),
                    EndTime = best.DateTime.AddMinutes(30),
                    LightQuality = DetermineLightQualityEnum(best.LightQuality),
                    OptimalityScore = best.ConfidenceLevel,
                    Description = $"Optimal conditions: EV {best.PredictedEV:F1}, {best.LightQuality.OptimalFor}"
                };
            }

            // Fallback: find best available conditions
            var bestAvailable = predictions
                .Where(p => p.SunPosition.IsAboveHorizon)
                .OrderByDescending(p => p.PredictedEV)
                .FirstOrDefault();

            if (bestAvailable != null)
            {
                return new OptimalShootingWindow
                {
                    StartTime = bestAvailable.DateTime,
                    EndTime = bestAvailable.DateTime.AddHours(1),
                    LightQuality = DetermineLightQualityEnum(bestAvailable.LightQuality),
                    OptimalityScore = bestAvailable.ConfidenceLevel * 0.7, // Reduced score for non-optimal
                    Description = $"Best available: EV {bestAvailable.PredictedEV:F1}"
                };
            }

            return new OptimalShootingWindow
            {
                StartTime = DateTime.Now.AddHours(1),
                EndTime = DateTime.Now.AddHours(2),
                LightQuality = LightQuality.Soft,
                OptimalityScore = 0.3,
                Description = "No optimal conditions found"
            };
        }

        private List<OptimalShootingWindow> FindAlternativeShootingWindows(List<HourlyLightPrediction> predictions)
        {
            var alternatives = new List<OptimalShootingWindow>();

            // Find blue hour windows (elevation between -6 and 6 degrees)
            var blueHourPredictions = predictions
                .Where(p => p.SunPosition.Elevation >= -6 && p.SunPosition.Elevation <= 6)
                .ToList();

            if (blueHourPredictions.Any())
            {
                var bestBlueHour = blueHourPredictions.OrderByDescending(p => p.ConfidenceLevel).First();
                alternatives.Add(new OptimalShootingWindow
                {
                    StartTime = bestBlueHour.DateTime.AddMinutes(-15),
                    EndTime = bestBlueHour.DateTime.AddMinutes(15),
                    LightQuality = LightQuality.BlueHour,
                    OptimalityScore = bestBlueHour.ConfidenceLevel * 0.8,
                    Description = $"Blue hour: EV {bestBlueHour.PredictedEV:F1}"
                });
            }

            // Find overcast conditions (good for portraits)
            var overcastPredictions = predictions
                .Where(p => p.LightQuality.SoftnessFactor > 0.7 && p.SunPosition.IsAboveHorizon)
                .ToList();

            if (overcastPredictions.Any())
            {
                var bestOvercast = overcastPredictions.OrderByDescending(p => p.PredictedEV).First();
                alternatives.Add(new OptimalShootingWindow
                {
                    StartTime = bestOvercast.DateTime,
                    EndTime = bestOvercast.DateTime.AddHours(1),
                    LightQuality = LightQuality.Soft,
                    OptimalityScore = bestOvercast.ConfidenceLevel * 0.6,
                    Description = $"Soft overcast light: EV {bestOvercast.PredictedEV:F1}"
                });
            }

            return alternatives.Take(3).ToList();
        }

        private LightQuality DetermineLightQualityEnum(LightCharacteristics characteristics)
        {
            if (characteristics.OptimalFor.Contains("golden hour"))
                return LightQuality.GoldenHour;
            else if (characteristics.SoftnessFactor > 0.7)
                return LightQuality.Soft;
            else if (characteristics.ColorTemperature < 4000)
                return LightQuality.GoldenHour;
            else
                return LightQuality.Direct;
        }

        private string GenerateOverallRecommendation(OptimalShootingWindow best, List<OptimalShootingWindow> alternatives)
        {
            if (best.OptimalityScore > 0.8)
            {
                return $"Excellent shooting conditions expected at {best.StartTime:HH:mm}. {best.Description}";
            }
            else if (best.OptimalityScore > 0.6)
            {
                return $"Good conditions at {best.StartTime:HH:mm}. {best.Description}. " +
                       (alternatives.Any() ? $"Alternative: {alternatives.First().Description}" : "");
            }
            else
            {
                return "Challenging conditions predicted. Consider indoor photography or wait for better weather.";
            }
        }

        private List<string> GenerateKeyInsights(List<HourlyLightPrediction> predictions, WeatherImpactAnalysis weather)
        {
            var insights = new List<string>();

            var optimalCount = predictions.Count(p => p.IsOptimalForPhotography);
            if (optimalCount > 0)
            {
                insights.Add(string.Format(AppResources.PredictiveLight_Insight_OptimalHours, optimalCount));
            }

            var maxEV = predictions.Max(p => p.PredictedEV);
            var minEV = predictions.Min(p => p.PredictedEV);
            insights.Add(string.Format(AppResources.PredictiveLight_Insight_LightRange, minEV, maxEV));

            if (weather.OverallLightReductionFactor < 0.5)
            {
                insights.Add(AppResources.PredictiveLight_Insight_WeatherImpact);
            }

            var goldenHourPredictions = predictions.Count(p =>
                p.SunPosition.Elevation < 15 && p.SunPosition.Elevation > 0);
            if (goldenHourPredictions > 0)
            {
                insights.Add(string.Format(AppResources.PredictiveLight_Insight_GoldenHourAvailable, goldenHourPredictions));
            }
            return insights;
        }

        private double CalculateCalibrationAccuracy(CalibrationData calibration)
        {
            if (!calibration.CalibrationReadings.Any()) return 0.5;

            var recentReadings = calibration.CalibrationReadings
                .Where(r => DateTime.Now.Subtract(r.DateTime).TotalHours <= 24)
                .ToList();

            if (!recentReadings.Any()) return 0.6;

            // Calculate accuracy based on consistency of calibration offsets
            var offsets = recentReadings.Select(r => r.CalibrationOffset).ToList();
            var standardDeviation = CalculateStandardDeviation(offsets);

            // Lower standard deviation = higher accuracy
            return Math.Max(0.3, Math.Min(0.95, 0.9 - (standardDeviation * 0.1)));
        }

        private double CalculateStandardDeviation(List<double> values)
        {
            if (values.Count <= 1) return 0;

            var average = values.Average();
            var sumOfSquares = values.Sum(x => Math.Pow(x - average, 2));
            return Math.Sqrt(sumOfSquares / values.Count);
        }

        #endregion

        private class CalibrationData
        {
            public int LocationId { get; set; }
            public DateTime LastCalibration { get; set; }
            public double CurrentOffset { get; set; }
            public List<CalibrationReading> CalibrationReadings { get; set; } = new();
        }

        private class CalibrationReading
        {
            public DateTime DateTime { get; set; }
            public double ActualEV { get; set; }
            public double TheoreticalEV { get; set; }
            public double CalibrationOffset { get; set; }
            public WeatherConditions? WeatherConditions { get; set; }
            public double TheoreticalLux { get; set; }
        }
    }
}