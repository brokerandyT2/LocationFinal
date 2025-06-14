﻿using Location.Core.Application.Common.Models;
using Location.Photography.Domain.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Location.Photography.Application.Queries.SunLocation
{
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

                for (var time = startTime; time < endTime; time = time.Add(interval))
                {
                    var azimuth = _sunCalculatorService.GetSolarAzimuth(time, request.Latitude, request.Longitude, TimeZoneInfo.Local.ToString());
                    var elevation = _sunCalculatorService.GetSolarElevation(time, request.Latitude, request.Longitude, TimeZoneInfo.Local.ToString());

                    pathPoints.Add(new SunPathPoint
                    {
                        Time = time,
                        Azimuth = azimuth,
                        Elevation = elevation,
                        IsVisible = elevation > 0
                    });

                    if (elevation > maxElevation)
                    {
                        maxElevation = elevation;
                        maxElevationTime = time;
                    }

                    if (Math.Abs(elevation) < 0.5)
                    {
                        if (time.Hour < 12)
                            sunriseAzimuth = azimuth;
                        else
                            sunsetAzimuth = azimuth;
                    }
                }

                var currentPosition = new SunPathPoint
                {
                    Time = DateTime.Now,
                    Azimuth = _sunCalculatorService.GetSolarAzimuth(DateTime.Now, request.Latitude, request.Longitude, TimeZoneInfo.Local.ToString()),
                    Elevation = _sunCalculatorService.GetSolarElevation(DateTime.Now, request.Latitude, request.Longitude, TimeZoneInfo.Local.ToString()),
                    IsVisible = _sunCalculatorService.GetSolarElevation(DateTime.Now, request.Latitude, request.Longitude, TimeZoneInfo.Local.ToString()) > 0
                };

                var sunrise = _sunCalculatorService.GetSunrise(request.Date, request.Latitude, request.Longitude, TimeZoneInfo.Local.ToString());
                var sunset = _sunCalculatorService.GetSunset(request.Date, request.Latitude, request.Longitude, TimeZoneInfo.Local.ToString());
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
}