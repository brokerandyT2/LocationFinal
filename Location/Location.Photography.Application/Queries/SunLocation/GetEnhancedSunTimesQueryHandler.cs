// Location.Photography.Application/Queries/SunLocation/GetEnhancedSunTimesQueryHandler.cs
using Location.Core.Application.Common.Models;
using Location.Photography.Domain.Models;
using Location.Photography.Domain.Services;
using MediatR;
using Microsoft.Extensions.Logging;


namespace Location.Photography.Application.Queries.SunLocation
{
    public class GetEnhancedSunTimesQueryHandler : IRequestHandler<GetEnhancedSunTimesQuery, Result<EnhancedSunTimes>>
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

        public async Task<Result<EnhancedSunTimes>> Handle(GetEnhancedSunTimesQuery request, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Get timezone for coordinates (simplified - using device timezone per requirements)
                var timezone = TimeZoneInfo.Local;

                // Calculate all sun times with enhanced precision
                var sunTimes = new EnhancedSunTimes
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
                    TimeZone = timezone,
                    IsDaylightSavingTime = timezone.IsDaylightSavingTime(request.Date),
                    UtcOffset = timezone.GetUtcOffset(request.Date)
                };

                // Calculate enhanced times
                var sunrise = sunTimes.Sunrise;
                var sunset = sunTimes.Sunset;

                // Blue hour calculations
                sunTimes.BlueHourMorning = sunTimes.CivilDawn;
                sunTimes.BlueHourEvening = sunTimes.CivilDusk;

                // Golden hour calculations  
                sunTimes.GoldenHourMorningStart = sunrise;
                sunTimes.GoldenHourMorningEnd = sunrise.AddHours(1);
                sunTimes.GoldenHourEveningStart = sunset.AddHours(-1);
                sunTimes.GoldenHourEveningEnd = sunset;

                // Solar time offset calculation
                var solarNoon = sunTimes.SolarNoon;
                var clockNoon = request.Date.Date.AddHours(12);
                sunTimes.SolarTimeOffset = solarNoon - clockNoon;

                return await Task.FromResult(Result<EnhancedSunTimes>.Success(sunTimes));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating enhanced sun times for coordinates {Latitude}, {Longitude} on {Date}",
                    request.Latitude, request.Longitude, request.Date);
                return Result<EnhancedSunTimes>.Failure($"Error calculating sun times: {ex.Message}");
            }
        }
    }
}