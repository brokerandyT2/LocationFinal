using Location.Core.Application.Common.Models;
using Location.Photography.Domain.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Photography.Application.Services
{
    public class SunService : ISunService
    {
        private readonly ISunCalculatorService _sunCalculatorService;

        public SunService(ISunCalculatorService sunCalculatorService)
        {
            _sunCalculatorService = sunCalculatorService ?? throw new ArgumentNullException(nameof(sunCalculatorService));
        }

        public async Task<Result<SunPositionDto>> GetSunPositionAsync(double latitude, double longitude, DateTime dateTime, CancellationToken cancellationToken = default)
        {
            try
            {
                var azimuth = _sunCalculatorService.GetSolarAzimuth(dateTime, latitude, longitude);
                var elevation = _sunCalculatorService.GetSolarElevation(dateTime, latitude, longitude);

                var result = new SunPositionDto
                {
                    Azimuth = azimuth,
                    Elevation = elevation,
                    DateTime = dateTime,
                    Latitude = latitude,
                    Longitude = longitude
                };

                // Use Task.FromResult since the operation is synchronous
                return Result<SunPositionDto>.Success(result);
            }
            catch (Exception ex)
            {
                return Result<SunPositionDto>.Failure($"Error calculating sun position: {ex.Message}");
            }
        }

        public async Task<Result<SunTimesDto>> GetSunTimesAsync(double latitude, double longitude, DateTime date, CancellationToken cancellationToken = default)
        {
            try
            {
                var sunrise = _sunCalculatorService.GetSunrise(date, latitude, longitude);
                var sunset = _sunCalculatorService.GetSunset(date, latitude, longitude);
                var solarNoon = _sunCalculatorService.GetSolarNoon(date, latitude, longitude);
                var astronomicalDawn = _sunCalculatorService.GetAstronomicalDawn(date, latitude, longitude);
                var astronomicalDusk = _sunCalculatorService.GetAstronomicalDusk(date, latitude, longitude);
                var nauticalDawn = _sunCalculatorService.GetNauticalDawn(date, latitude, longitude);
                var nauticalDusk = _sunCalculatorService.GetNauticalDusk(date, latitude, longitude);
                var civilDawn = _sunCalculatorService.GetCivilDawn(date, latitude, longitude);
                var civilDusk = _sunCalculatorService.GetCivilDusk(date, latitude, longitude);

                // Calculate golden hour (typically 1 hour after sunrise and 1 hour before sunset)
                var goldenHourMorningStart = sunrise;
                var goldenHourMorningEnd = sunrise.AddHours(1);
                var goldenHourEveningStart = sunset.AddHours(-1);
                var goldenHourEveningEnd = sunset;

                var result = new SunTimesDto
                {
                    Date = date,
                    Latitude = latitude,
                    Longitude = longitude,
                    Sunrise = sunrise,
                    Sunset = sunset,
                    SolarNoon = solarNoon,
                    AstronomicalDawn = astronomicalDawn,
                    AstronomicalDusk = astronomicalDusk,
                    NauticalDawn = nauticalDawn,
                    NauticalDusk = nauticalDusk,
                    CivilDawn = civilDawn,
                    CivilDusk = civilDusk,
                    GoldenHourMorningStart = goldenHourMorningStart,
                    GoldenHourMorningEnd = goldenHourMorningEnd,
                    GoldenHourEveningStart = goldenHourEveningStart,
                    GoldenHourEveningEnd = goldenHourEveningEnd
                };

                // Use Task.FromResult since the operation is synchronous
                return Result<SunTimesDto>.Success(result);
            }
            catch (Exception ex)
            {
                return Result<SunTimesDto>.Failure($"Error calculating sun times: {ex.Message}");
            }
        }
    }
}