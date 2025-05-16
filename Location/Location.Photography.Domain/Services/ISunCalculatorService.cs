// Location.Photography.Domain.Services.ISunCalculatorService.cs
using System;

namespace Location.Photography.Domain.Services
{
    public interface ISunCalculatorService
    {
        DateTime GetSunrise(DateTime date, double latitude, double longitude);
        DateTime GetSunset(DateTime date, double latitude, double longitude);
        DateTime GetSolarNoon(DateTime date, double latitude, double longitude);
        DateTime GetCivilDawn(DateTime date, double latitude, double longitude);
        DateTime GetCivilDusk(DateTime date, double latitude, double longitude);
        DateTime GetNauticalDawn(DateTime date, double latitude, double longitude);
        DateTime GetNauticalDusk(DateTime date, double latitude, double longitude);
        DateTime GetAstronomicalDawn(DateTime date, double latitude, double longitude);
        DateTime GetAstronomicalDusk(DateTime date, double latitude, double longitude);
        double GetSolarAzimuth(DateTime dateTime, double latitude, double longitude);
        double GetSolarElevation(DateTime dateTime, double latitude, double longitude);
    }
}