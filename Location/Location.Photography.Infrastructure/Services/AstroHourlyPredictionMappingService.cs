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
using Location.Photography.Application.DTOs;
using static Location.Photography.ViewModels.AstroPhotographyCalculatorViewModel;
using ServiceWeatherConditions = Location.Photography.Application.Services.WeatherConditions;
using ViewModelWeatherConditions = Location.Photography.ViewModels.AstroPhotographyCalculatorViewModel.WeatherConditions;
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

        public AstroHourlyPredictionMappingService(
            ILogger<AstroHourlyPredictionMappingService> logger,
            IAstroCalculationService astroCalculationService,
            IEquipmentRecommendationService equipmentRecommendationService,
            ISunCalculatorService sunCalculatorService,
            IExposureCalculatorService exposureCalculatorService,
            IPredictiveLightService predictiveLightService,
            IMediator mediator)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _astroCalculationService = astroCalculationService ?? throw new ArgumentNullException(nameof(astroCalculationService));
            _equipmentRecommendationService = equipmentRecommendationService ?? throw new ArgumentNullException(nameof(equipmentRecommendationService));
            _sunCalculatorService = sunCalculatorService ?? throw new ArgumentNullException(nameof(sunCalculatorService));
            _exposureCalculatorService = exposureCalculatorService ?? throw new ArgumentNullException(nameof(exposureCalculatorService));
            _predictiveLightService = predictiveLightService ?? throw new ArgumentNullException(nameof(predictiveLightService));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
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
                    var prediction = await MapSingleCalculationAsync(hourGroup.First(), latitude, longitude, selectedDate);
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

                // Create basic DTO structure
                var dto = new AstroHourlyPredictionDto
                {
                    Hour = hour,
                    TimeDisplay = hour.ToString("h:mm tt"),
                    SolarEvent = solarEvent,
                    SolarEventsDisplay = solarEvent,
                    QualityScore = CalculateQualityScore(calculationResult),
                    QualityDisplay = GetQualityDisplay(CalculateQualityScore(calculationResult)),
                    QualityDescription = GetQualityDescription(CalculateQualityScore(calculationResult)),
                    AstroEvents = new List<AstroEventDto>
                {
                    new AstroEventDto
                    {
                        TargetName = calculationResult.Target.ToString(),
                        Visibility = calculationResult.IsVisible ? $"{calculationResult.Altitude:F0}° altitude" : "Not visible",
                        RecommendedEquipment = calculationResult.Equipment ?? "Standard astrophotography equipment",
                        CameraSettings = GetCameraSettings(calculationResult.Target),
                        Notes = calculationResult.PhotographyNotes ?? "Standard astrophotography guidelines apply"
                    }
                },
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

                // Generate basic prediction
                var dto = new AstroHourlyPredictionDto
                {
                    Hour = hour,
                    TimeDisplay = hour.ToString("h:mm tt"),
                    SolarEvent = solarEvent,
                    SolarEventsDisplay = solarEvent,
                    QualityScore = await CalculateHourQualityAsync(hour, latitude, longitude),
                    AstroEvents = await GenerateAstroEventsForHourAsync(hour, latitude, longitude),
                    Weather = await GetWeatherDtoAsync(hour, latitude, longitude)
                };

                dto.QualityDisplay = GetQualityDisplay(dto.QualityScore);
                dto.QualityDescription = GetQualityDescription(dto.QualityScore);

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
                var sunAltitude = _sunCalculatorService.GetSolarElevation(hour, latitude, longitude, "UTC");

                return sunAltitude switch
                {
                    > 6 => "Daylight",
                    > 0 => "Civil Twilight",
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

        private double CalculateQualityScore(AstroCalculationResult result)
        {
            if (!result.IsVisible) return 0;

            var score = 50.0; // Base score

            // Altitude bonus
            if (result.Altitude > 60) score += 30;
            else if (result.Altitude > 30) score += 20;
            else if (result.Altitude > 15) score += 10;

            // Target-specific bonuses
            score += result.Target switch
            {
                AstroTarget.MilkyWayCore => 20,
                AstroTarget.Moon => 15,
                AstroTarget.Planets => 10,
                AstroTarget.DeepSkyObjects => 25,
                _ => 5
            };

            return Math.Min(100, Math.Max(0, score));
        }

        private async Task<double> CalculateHourQualityAsync(DateTime hour, double latitude, double longitude)
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

                return score;
            }
            catch
            {
                return 50; // Default moderate score
            }
        }

        private async Task<List<AstroEventDto>> GenerateAstroEventsForHourAsync(DateTime hour, double latitude, double longitude)
        {
            var events = new List<AstroEventDto>();

            try
            {
                var sunAltitude = _sunCalculatorService.GetSolarElevation(hour, latitude, longitude, "UTC");

                if (sunAltitude < -6) // Dark enough for astrophotography
                {
                    events.Add(new AstroEventDto
                    {
                        TargetName = "Milky Way Core",
                        Visibility = "45° altitude",
                        RecommendedEquipment = "Wide-angle lens, full-frame camera",
                        CameraSettings = "ISO 3200, f/2.8, 20\"",
                        Notes = "Best during summer months for northern hemisphere"
                    });

                    if (sunAltitude < -18) // True night
                    {
                        events.Add(new AstroEventDto
                        {
                            TargetName = "Deep Sky Objects",
                            Visibility = "Various altitudes",
                            RecommendedEquipment = "Telephoto lens or telescope",
                            CameraSettings = "ISO 1600, f/4, 5 minutes",
                            Notes = "Requires dark skies and tracking mount"
                        });
                    }
                }

                return events;
            }
            catch
            {
                return new List<AstroEventDto>();
            }
        }

        private async Task<WeatherDto> GetWeatherDtoAsync(DateTime hour, double latitude, double longitude)
        {
            try
            {
                // Default weather conditions - would integrate with actual weather service
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
                >= 80 => "Exceptional astrophotography conditions",
                >= 60 => "Good shooting opportunity with minor limitations",
                >= 40 => "Fair conditions - manageable challenges",
                >= 20 => "Poor conditions - significant obstacles",
                _ => "Very challenging conditions"
            };
        }

        private AstroHourlyPredictionDto CreateDefaultDto(DateTime hour)
        {
            return new AstroHourlyPredictionDto
            {
                Hour = hour,
                TimeDisplay = hour.ToString("h:mm tt"),
                SolarEvent = "Unknown",
                SolarEventsDisplay = "Unknown",
                QualityScore = 50,
                QualityDisplay = "Fair",
                QualityDescription = "Conditions unknown",
                AstroEvents = new List<AstroEventDto>(),
                Weather = new WeatherDto
                {
                    CloudCover = 50,
                    Humidity = 70,
                    WindSpeed = 10,
                    Visibility = 8000,
                    Description = "Unknown",
                    WeatherDisplay = "Unknown",
                    WeatherSuitability = "Unknown"
                }
            };
        }
    }
}