// Location.Photography.Application/Queries/SunLocation/GetOptimalShootingTimesQueryHandler.cs
using Location.Core.Application.Common.Models;
using Location.Photography.Application.Services;
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
}