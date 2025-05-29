using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Location.Core.Application.Weather.DTOs;
using Location.Photography.Application.Services;
using Location.Photography.Domain.Models;
using Location.Photography.Domain.Services;
using Microsoft.Extensions.Logging;
using EnhancedSunTimes = Location.Photography.Application.Services.EnhancedSunTimes;
using ExposureTriangle = Location.Photography.Application.Services.ExposureTriangle;
using HourlyLightPrediction = Location.Photography.Application.Services.HourlyLightPrediction;
using LightCharacteristics = Location.Photography.Application.Services.LightCharacteristics;
using SunPosition = Location.Photography.Application.Services.SunPosition;

namespace Location.Photography.Infrastructure.Services
{
    public class PredictiveLightService : IPredictiveLightService
    {
        private readonly ISunCalculatorService _sunCalculatorService;
        private readonly ILogger<PredictiveLightService> _logger;

        // Calibration data - in production would be stored in database
        private readonly Dictionary<int, CalibrationData> _locationCalibrations = new();

        // Constants for light calculations
        private const double BaselineEV = 15.0; // EV for clear sky at solar noon
        private const double CalibrationWeight = 0.7; // Weight for recent calibration data
        private const double HistoricalWeight = 0.3; // Weight for historical data

        public PredictiveLightService(
            ISunCalculatorService sunCalculatorService,
            ILogger<PredictiveLightService> logger)
        {
            _sunCalculatorService = sunCalculatorService ?? throw new ArgumentNullException(nameof(sunCalculatorService));
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
                    // Analyze current conditions from first forecast day
                    var currentForecast = request.WeatherForecast.DailyForecasts.First();
                    analysis.CurrentConditions = MapToWeatherConditions(currentForecast);

                    // Calculate hourly impacts for the next 24 hours
                    analysis.HourlyImpacts = await CalculateHourlyWeatherImpactsAsync(
                        request.WeatherForecast, request.SunTimes, cancellationToken);

                    // Calculate overall light reduction factor
                    analysis.OverallLightReductionFactor = CalculateOverallLightReduction(analysis.HourlyImpacts);

                    // Generate summary and alerts
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
                    Summary = "Weather analysis unavailable",
                    OverallLightReductionFactor = 0.8 // Conservative estimate
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
                var startTime = request.TargetDate.Date;

                for (int hour = 0; hour < request.PredictionWindowHours; hour++)
                {
                    var targetTime = startTime.AddHours(hour);
                    var prediction = await GenerateSingleHourPredictionAsync(request, targetTime, cancellationToken);
                    predictions.Add(prediction);
                }

                return predictions;
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
                var hourlyPredictions = await GenerateHourlyPredictionsAsync(request, cancellationToken);

                var recommendation = new PredictiveLightRecommendation
                {
                    GeneratedAt = DateTime.Now
                };

                // Find best shooting window
                recommendation.BestTimeWindow = FindBestShootingWindow(hourlyPredictions);

                // Find alternative windows
                recommendation.AlternativeWindows = FindAlternativeShootingWindows(hourlyPredictions)
                    .Take(3).ToList();

                // Generate overall recommendation
                recommendation.OverallRecommendation = GenerateOverallRecommendation(
                    recommendation.BestTimeWindow, recommendation.AlternativeWindows);

                // Generate key insights
                recommendation.KeyInsights = GenerateKeyInsights(hourlyPredictions, request.WeatherImpact);

                // Check calibration status
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
                    OverallRecommendation = "Prediction service temporarily unavailable"
                };
            }
        }

        public async Task CalibrateWithActualReadingAsync(
            LightMeterCalibrationRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Calculate theoretical EV for comparison
                var sunPosition = GetSunPosition(request.DateTime, request.Latitude, request.Longitude);
                var theoreticalEV = CalculateTheoreticalEV(sunPosition, request.WeatherConditions);

                // Calculate calibration offset
                var calibrationOffset = request.ActualEV - theoreticalEV;

                // Update or create calibration data for this location
                if (!_locationCalibrations.TryGetValue(request.LocationId, out var calibration))
                {
                    calibration = new CalibrationData { LocationId = request.LocationId };
                    _locationCalibrations[request.LocationId] = calibration;
                }

                // Apply weighted average with historical data
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

                // Store calibration reading
                calibration.CalibrationReadings.Add(new CalibrationReading
                {
                    DateTime = request.DateTime,
                    ActualEV = request.ActualEV,
                    TheoreticalEV = theoreticalEV,
                    CalibrationOffset = calibrationOffset,
                    WeatherConditions = request.WeatherConditions
                });

                calibration.LastCalibration = DateTime.Now;
                calibration.CurrentOffset = calibrationOffset;

                // Keep only recent readings (last 7 days)
                calibration.CalibrationReadings = calibration.CalibrationReadings
                    .Where(r => DateTime.Now.Subtract(r.DateTime).TotalDays <= 7)
                    .ToList();

                _logger.LogInformation(
                    "Calibrated location {LocationId} with offset {Offset:F2} EV",
                    request.LocationId, calibrationOffset);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calibrating with actual reading");
            }
        }

        #region Private Methods

        private async Task<List<HourlyWeatherImpact>> CalculateHourlyWeatherImpactsAsync(
            WeatherForecastDto forecast,
            EnhancedSunTimes sunTimes,
            CancellationToken cancellationToken)
        {
            var impacts = new List<HourlyWeatherImpact>();

            // Use first day's forecast for hourly breakdown
            if (forecast.DailyForecasts.Any())
            {
                var dailyForecast = forecast.DailyForecasts.First();
                var baseDate = dailyForecast.Date.Date;

                for (int hour = 0; hour < 24; hour++)
                {
                    var hourTime = baseDate.AddHours(hour);
                    var impact = CalculateHourlyWeatherImpact(dailyForecast, hourTime, sunTimes);
                    impacts.Add(impact);
                }
            }

            return impacts;
        }

        private HourlyWeatherImpact CalculateHourlyWeatherImpact(
            DailyForecastDto forecast,
            DateTime hour,
            EnhancedSunTimes sunTimes)
        {
            var impact = new HourlyWeatherImpact { Hour = hour };

            // Calculate light reduction based on weather conditions
            double reduction = 1.0;

            // Cloud cover impact (most significant factor)
            reduction *= (1.0 - (forecast.Clouds * 0.008)); // 10% clouds = 8% reduction

            // Precipitation impact
            if (forecast.Precipitation.HasValue && forecast.Precipitation > 0)
            {
                reduction *= 0.3; // Significant reduction during precipitation
            }

            // Humidity/haze impact
            reduction *= (1.0 - (forecast.Humidity * 0.001)); // Subtle haze effect

            impact.LightReductionFactor = Math.Max(0.1, reduction);

            // Color temperature shift based on conditions
            impact.ColorTemperatureShift = CalculateColorTemperatureShift(forecast, hour, sunTimes);

            // Determine light quality
            impact.PredictedQuality = DetermineLightQuality(forecast, hour, sunTimes);

            // Generate reasoning
            impact.Reasoning = GenerateHourlyReasoning(forecast, hour, sunTimes, reduction);

            return impact;
        }

        private async Task<HourlyLightPrediction> GenerateSingleHourPredictionAsync(
            PredictiveLightRequest request,
            DateTime targetTime,
            CancellationToken cancellationToken)
        {
            var prediction = new HourlyLightPrediction { DateTime = targetTime };

            // Calculate sun position
            prediction.SunPosition = GetSunPosition(targetTime, request.Latitude, request.Longitude);

            // Calculate theoretical EV
            var theoreticalEV = CalculateTheoreticalEV(prediction.SunPosition, null);

            // Apply weather impact
            var weatherImpact = GetWeatherImpactForHour(request.WeatherImpact, targetTime);
            theoreticalEV += Math.Log(weatherImpact, 2); // Convert reduction factor to EV stops

            // Apply calibration if available
            if (_locationCalibrations.TryGetValue(request.LocationId, out var calibration) &&
                request.LastCalibrationReading.HasValue)
            {
                theoreticalEV += calibration.CurrentOffset;
                prediction.ConfidenceLevel = 0.9; // High confidence with calibration
                prediction.ConfidenceReason = "based on recent light meter calibration";
            }
            else
            {
                prediction.ConfidenceLevel = 0.7; // Lower confidence without calibration
                prediction.ConfidenceReason = "based on theoretical calculations";
            }

            prediction.PredictedEV = theoreticalEV;
            prediction.EVConfidenceMargin = CalculateConfidenceMargin(prediction.ConfidenceLevel);

            // Generate exposure settings
            prediction.SuggestedSettings = GenerateExposureSettings(theoreticalEV);

            // Calculate light characteristics
            prediction.LightQuality = CalculateLightCharacteristics(
                prediction.SunPosition, weatherImpact, targetTime, request.SunTimes);

            // Generate recommendations
            prediction.Recommendations = GenerateHourlyRecommendations(
                prediction.LightQuality, prediction.SunPosition, weatherImpact);

            // Determine if optimal for photography
            prediction.IsOptimalForPhotography = IsOptimalForPhotography(
                prediction.LightQuality, prediction.SunPosition, weatherImpact);

            return prediction;
        }

        private SunPosition GetSunPosition(DateTime dateTime, double latitude, double longitude)
        {
            var azimuth = _sunCalculatorService.GetSolarAzimuth(dateTime, latitude, longitude);
            var elevation = _sunCalculatorService.GetSolarElevation(dateTime, latitude, longitude);

            return new SunPosition
            {
                Azimuth = azimuth,
                Elevation = elevation,
                IsAboveHorizon = elevation > 0,
                Distance = 1.0 // AU - could be calculated for seasonal variations
            };
        }

        private double CalculateTheoreticalEV(SunPosition sunPosition, WeatherConditions? weather)
        {
            if (!sunPosition.IsAboveHorizon)
                return -5; // Very low light

            // Base EV calculation using sun elevation
            var baseEV = BaselineEV * Math.Sin(sunPosition.Elevation * Math.PI / 180.0);

            // Adjust for atmospheric conditions
            if (sunPosition.Elevation < 30)
            {
                // Atmospheric absorption increases at low angles
                var atmosphericLoss = (30 - sunPosition.Elevation) * 0.1;
                baseEV -= atmosphericLoss;
            }

            return Math.Max(-5, baseEV);
        }

        private WeatherConditions MapToWeatherConditions(DailyForecastDto forecast)
        {
            return new WeatherConditions
            {
                CloudCover = forecast.Clouds / 100.0,
                Precipitation = forecast.Precipitation ?? 0,
                Humidity = forecast.Humidity / 100.0,
                Visibility = 10.0, // Default - would need additional weather data
                AirQualityIndex = 50, // Default - would need additional data
                WindSpeed = forecast.WindSpeed,
                Description = forecast.Description
            };
        }

        private double CalculateOverallLightReduction(List<HourlyWeatherImpact> impacts)
        {
            if (!impacts.Any()) return 0.8;

            // Weight daylight hours more heavily
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
                    Message = "Severe weather impact expected - consider rescheduling outdoor shoots",
                    Severity = AlertSeverity.Critical,
                    ValidFrom = DateTime.Now,
                    ValidTo = DateTime.Now.AddHours(24)
                });
            }

            return alerts;
        }

        private double GetWeatherImpactForHour(WeatherImpactAnalysis impact, DateTime hour)
        {
            var hourlyImpact = impact.HourlyImpacts?.FirstOrDefault(h => h.Hour.Hour == hour.Hour);
            return hourlyImpact?.LightReductionFactor ?? impact.OverallLightReductionFactor;
        }

        private double CalculateConfidenceMargin(double confidenceLevel)
        {
            // Higher confidence = smaller margin
            return (1.0 - confidenceLevel) * 2.0; // 0.9 confidence = ±0.2 EV
        }

        private ExposureTriangle GenerateExposureSettings(double ev)
        {
            // Simple exposure calculation - could be enhanced with user preferences
            var fStop = "f/8"; // Default for landscape
            var iso = "ISO 100"; // Base ISO

            // Calculate shutter speed for given EV, f-stop, and ISO
            // EV = log₂(N²/t) where N=f-number, t=shutter speed (at ISO 100)
            var shutterSpeed = Math.Pow(2, ev - 6); // f/8 = 2³, so EV - 3*2 = EV - 6

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

        // Additional helper methods would continue here...
        // Implementing remaining private methods for brevity in this response

        private double CalculateColorTemperatureShift(DailyForecastDto forecast, DateTime hour, EnhancedSunTimes sunTimes)
        {
            // Stub implementation
            return 0;
        }

        private LightQuality DetermineLightQuality(DailyForecastDto forecast, DateTime hour, EnhancedSunTimes sunTimes)
        {
            // Stub implementation  
            return LightQuality.Soft;
        }

        private string GenerateHourlyReasoning(DailyForecastDto forecast, DateTime hour, EnhancedSunTimes sunTimes, double reduction)
        {
            return $"Light reduced by {(1 - reduction) * 100:F0}% due to weather conditions";
        }

        private LightCharacteristics CalculateLightCharacteristics(SunPosition sunPosition, double weatherImpact, DateTime time, EnhancedSunTimes sunTimes)
        {
            return new LightCharacteristics
            {
                ColorTemperature = 5500,
                SoftnessFactor = 0.7,
                OptimalFor = "General photography"
            };
        }

        private List<string> GenerateHourlyRecommendations(LightCharacteristics quality, SunPosition position, double impact)
        {
            return new List<string> { "Good for outdoor photography" };
        }

        private bool IsOptimalForPhotography(LightCharacteristics quality, SunPosition position, double impact)
        {
            return position.IsAboveHorizon && impact > 0.6;
        }

        private OptimalShootingWindow FindBestShootingWindow(List<HourlyLightPrediction> predictions)
        {
            return new OptimalShootingWindow
            {
                StartTime = DateTime.Now.AddHours(1),
                EndTime = DateTime.Now.AddHours(2),
                LightQuality = LightQuality.GoldenHour,
                OptimalityScore = 0.9
            };
        }

        private List<OptimalShootingWindow> FindAlternativeShootingWindows(List<HourlyLightPrediction> predictions)
        {
            return new List<OptimalShootingWindow>();
        }

        private string GenerateOverallRecommendation(OptimalShootingWindow best, List<OptimalShootingWindow> alternatives)
        {
            return "Optimal shooting conditions expected during golden hour";
        }

        private List<string> GenerateKeyInsights(List<HourlyLightPrediction> predictions, WeatherImpactAnalysis weather)
        {
            return new List<string> { "Weather conditions favorable for photography" };
        }

        private double CalculateCalibrationAccuracy(CalibrationData calibration)
        {
            return 0.85; // Stub - would calculate based on prediction vs actual variance
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
        }
    }
}