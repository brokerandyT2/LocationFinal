// Location.Photography.Application/Queries/SunLocation/GetMoonDataQueryHandler.cs
using Location.Core.Application.Common.Models;
using Location.Photography.Application.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Photography.Application.Queries.SunLocation
{
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
}