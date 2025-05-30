// Location.Photography.Domain.Services.ISunCalculatorService.cs
using System;

namespace Location.Photography.Domain.Services
{
    public interface ISunCalculatorService
    {
        DateTime GetSunrise(DateTime date, double latitude, double longitude, string timezone);
        DateTime GetSunset(DateTime date, double latitude, double longitude, string timezone);
        DateTime GetSolarNoon(DateTime date, double latitude, double longitude, string timezone);
        DateTime GetCivilDawn(DateTime date, double latitude, double longitude, string timezone);
        DateTime GetCivilDusk(DateTime date, double latitude, double longitude, string timezone);
        DateTime GetNauticalDawn(DateTime date, double latitude, double longitude, string timezone      );
        DateTime GetNauticalDusk(DateTime date, double latitude, double longitude, string timezone);
        DateTime GetAstronomicalDawn(DateTime date, double latitude, double longitude, string timezone);
        DateTime GetAstronomicalDusk(DateTime date, double latitude, double longitude, string timezone);
        double GetSolarAzimuth(DateTime dateTime, double latitude, double longitude, string timezone);
        double GetSolarElevation(DateTime dateTime, double latitude, double longitude, string timezone);
    }
}