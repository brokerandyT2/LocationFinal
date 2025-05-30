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
using ExposureTriangle = Location.Photography.Domain.Models.ExposureTriangle;
using HourlyLightPrediction = Location.Photography.Domain.Models.HourlyLightPrediction;
using LightCharacteristics = Location.Photography.Domain.Models.LightCharacteristics;
using SunPosition = Location.Photography.Domain.Models.SunPosition;

namespace Location.Photography.Infrastructure.Services
{
    public class PredictiveLightService : IPredictiveLightService
    {
        private readonly ISunCalculatorService _sunCalculatorService;
        private readonly ILogger<PredictiveLightService> _logger;

        private readonly Dictionary<int, CalibrationData> _locationCalibrations = new();

        private const double BaselineEV = 15.0;
        private const double CalibrationWeight = 0.7;
        private const double HistoricalWeight = 0.3;

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
                    var currentForecast = request.WeatherForecast.DailyForecasts.First();
                    analysis.CurrentConditions = MapToWeatherConditions(currentForecast);

                    analysis.HourlyImpacts = await CalculateHourlyWeatherImpactsAsync(
                        request.WeatherForecast, request.SunTimes, cancellationToken);

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
                    Summary = "Weather analysis unavailable",
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
                var sunPosition = GetSunPosition(request.DateTime, request.Latitude, request.Longitude);
                var theoreticalEV = CalculateTheoreticalEV(sunPosition, request.WeatherConditions);

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
                    WeatherConditions = request.WeatherConditions
                });

                calibration.LastCalibration = DateTime.Now;
                calibration.CurrentOffset = calibrationOffset;

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
            Location.Photography.Domain.Models.EnhancedSunTimes sunTimes,
            CancellationToken cancellationToken)
        {
            var impacts = new List<HourlyWeatherImpact>();

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
            Location.Photography.Domain.Models.EnhancedSunTimes sunTimes)
        {
            var impact = new HourlyWeatherImpact { Hour = hour };

            double reduction = 1.0;

            reduction *= (1.0 - (forecast.Clouds * 0.008));

            if (forecast.Precipitation.HasValue && forecast.Precipitation > 0)
            {
                reduction *= 0.3;
            }

            reduction *= (1.0 - (forecast.Humidity * 0.001));

            impact.LightReductionFactor = Math.Max(0.1, reduction);

            impact.ColorTemperatureShift = CalculateColorTemperatureShift(forecast, hour, sunTimes);

            impact.PredictedQuality = DetermineLightQuality(forecast, hour, sunTimes);

            impact.Reasoning = GenerateHourlyReasoning(forecast, hour, sunTimes, reduction);

            return impact;
        }

        private async Task<HourlyLightPrediction> GenerateSingleHourPredictionAsync(
            PredictiveLightRequest request,
            DateTime targetTime,
            CancellationToken cancellationToken)
        {
            var prediction = new HourlyLightPrediction { DateTime = targetTime };

            prediction.SunPosition = GetSunPosition(targetTime, request.Latitude, request.Longitude);

            var theoreticalEV = CalculateTheoreticalEV(prediction.SunPosition, null);

            var weatherImpact = GetWeatherImpactForHour(request.WeatherImpact, targetTime);
            theoreticalEV += Math.Log(weatherImpact, 2);

            if (_locationCalibrations.TryGetValue(request.LocationId, out var calibration) &&
                request.LastCalibrationReading.HasValue)
            {
                theoreticalEV += calibration.CurrentOffset;
                prediction.ConfidenceLevel = 0.9;
                prediction.ConfidenceReason = "based on recent light meter calibration";
            }
            else
            {
                prediction.ConfidenceLevel = 0.7;
                prediction.ConfidenceReason = "based on theoretical calculations";
            }

            prediction.PredictedEV = theoreticalEV;
            prediction.EVConfidenceMargin = CalculateConfidenceMargin(prediction.ConfidenceLevel);

            prediction.SuggestedSettings = GenerateExposureSettings(theoreticalEV);

            prediction.LightQuality = CalculateLightCharacteristics(
                prediction.SunPosition, weatherImpact, targetTime, request.SunTimes);

            prediction.Recommendations = GenerateHourlyRecommendations(
                prediction.LightQuality, prediction.SunPosition, weatherImpact);

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
                Distance = 1.0
            };
        }

        private double CalculateTheoreticalEV(SunPosition sunPosition, WeatherConditions? weather)
        {
            if (!sunPosition.IsAboveHorizon)
                return -5;

            var baseEV = BaselineEV * Math.Sin(sunPosition.Elevation * Math.PI / 180.0);

            if (sunPosition.Elevation < 30)
            {
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
                Visibility = 10.0,
                AirQualityIndex = 50,
                WindSpeed = forecast.WindSpeed,
                Description = forecast.Description
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
            return $"Light reduced by {(1 - reduction) * 100:F0}% due to weather conditions";
        }

        private LightCharacteristics CalculateLightCharacteristics(SunPosition sunPosition, double weatherImpact, DateTime time, Location.Photography.Domain.Models.EnhancedSunTimes sunTimes)
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
            return 0.85;
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