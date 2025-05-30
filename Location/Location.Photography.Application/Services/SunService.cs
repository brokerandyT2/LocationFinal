// Location.Photography.Application/Services/SunService.cs
using Location.Core.Application.Common.Models;
using Location.Photography.Domain.Models;
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
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var azimuth = _sunCalculatorService.GetSolarAzimuth(dateTime, latitude, longitude, TimeZoneInfo.Local.ToString());
                var elevation = _sunCalculatorService.GetSolarElevation(dateTime, latitude, longitude, TimeZoneInfo.Local.ToString());

                var result = new SunPositionDto
                {
                    Azimuth = azimuth,
                    Elevation = elevation,
                    DateTime = dateTime,
                    Latitude = latitude,
                    Longitude = longitude
                };

                return Result<SunPositionDto>.Success(result);
            }
            catch (Exception ex)
            {
                return Result<SunPositionDto>.Failure($"Error calculating sun position: {ex.Message}");
            }
        }

        public async Task<Result<SunTimesDto>> GetSunTimesAsync(double latitude, double longitude, DateTime date, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var result = new SunTimesDto
                {
                    Date = date,
                    Latitude = latitude,
                    Longitude = longitude,
                    Sunrise = _sunCalculatorService.GetSunrise(date, latitude, longitude, TimeZoneInfo.Local.ToString()),
                    Sunset = _sunCalculatorService.GetSunset(date, latitude, longitude, TimeZoneInfo.Local.ToString()),
                    SolarNoon = _sunCalculatorService.GetSolarNoon(date, latitude, longitude, TimeZoneInfo.Local.ToString()),
                    AstronomicalDawn = _sunCalculatorService.GetAstronomicalDawn(date, latitude, longitude, TimeZoneInfo.Local.ToString()),
                    AstronomicalDusk = _sunCalculatorService.GetAstronomicalDusk(date, latitude, longitude, TimeZoneInfo.Local.ToString()),
                    NauticalDawn = _sunCalculatorService.GetNauticalDawn(date, latitude, longitude, TimeZoneInfo.Local.ToString()),
                    NauticalDusk = _sunCalculatorService.GetNauticalDusk(date, latitude, longitude, TimeZoneInfo.Local.ToString()),
                    CivilDawn = _sunCalculatorService.GetCivilDawn(date, latitude, longitude, TimeZoneInfo.Local.ToString()),
                    CivilDusk = _sunCalculatorService.GetCivilDusk(date, latitude, longitude, TimeZoneInfo.Local.ToString()),
                    GoldenHourMorningStart = _sunCalculatorService.GetSunrise(date, latitude, longitude, TimeZoneInfo.Local.ToString()),
                    GoldenHourMorningEnd = _sunCalculatorService.GetSunrise(date, latitude, longitude, TimeZoneInfo.Local.ToString()).AddHours(1),
                    GoldenHourEveningStart = _sunCalculatorService.GetSunset(date, latitude, longitude, TimeZoneInfo.Local.ToString()).AddHours(-1),
                    GoldenHourEveningEnd = _sunCalculatorService.GetSunset(date, latitude, longitude, TimeZoneInfo.Local.ToString())
                };

                return Result<SunTimesDto>.Success(result);
            }
            catch (Exception ex)
            {
                return Result<SunTimesDto>.Failure($"Error calculating sun times: {ex.Message}");
            }
        }
    }
}