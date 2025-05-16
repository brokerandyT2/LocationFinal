// Location.Photography.Infrastructure.Services.SunCalculatorService.cs
using Location.Photography.Domain.Services;
using System;

namespace Location.Photography.Infrastructure.Services
{
    public class SunCalculatorService : ISunCalculatorService
    {
        // Constants for astronomical calculations
        private const double DegToRad = Math.PI / 180.0;
        private const double RadToDeg = 180.0 / Math.PI;

        // Depression angles for different twilight times
        private const double CivilTwilightDegrees = 6.0;
        private const double NauticalTwilightDegrees = 12.0;
        private const double AstronomicalTwilightDegrees = 18.0;

        public DateTime GetSunrise(DateTime date, double latitude, double longitude)
        {
            return CalculateSunriseSet(date, latitude, longitude, true, 0.833);
        }

        public DateTime GetSunset(DateTime date, double latitude, double longitude)
        {
            return CalculateSunriseSet(date, latitude, longitude, false, 0.833);
        }

        public DateTime GetSolarNoon(DateTime date, double latitude, double longitude)
        {
            // Solar noon occurs when the sun reaches its highest point in the sky
            var eqTime = CalculateEquationOfTime(date);
            var solarNoonOffset = 720 - (longitude * 4) - eqTime; // in minutes

            var solarNoonTime = date.Date.AddMinutes(solarNoonOffset);

            return ConvertToLocalTime(solarNoonTime, longitude);
        }

        public DateTime GetCivilDawn(DateTime date, double latitude, double longitude)
        {
            return CalculateSunriseSet(date, latitude, longitude, true, CivilTwilightDegrees);
        }

        public DateTime GetCivilDusk(DateTime date, double latitude, double longitude)
        {
            return CalculateSunriseSet(date, latitude, longitude, false, CivilTwilightDegrees);
        }

        public DateTime GetNauticalDawn(DateTime date, double latitude, double longitude)
        {
            return CalculateSunriseSet(date, latitude, longitude, true, NauticalTwilightDegrees);
        }

        public DateTime GetNauticalDusk(DateTime date, double latitude, double longitude)
        {
            return CalculateSunriseSet(date, latitude, longitude, false, NauticalTwilightDegrees);
        }

        public DateTime GetAstronomicalDawn(DateTime date, double latitude, double longitude)
        {
            return CalculateSunriseSet(date, latitude, longitude, true, AstronomicalTwilightDegrees);
        }

        public DateTime GetAstronomicalDusk(DateTime date, double latitude, double longitude)
        {
            return CalculateSunriseSet(date, latitude, longitude, false, AstronomicalTwilightDegrees);
        }

        public double GetSolarAzimuth(DateTime dateTime, double latitude, double longitude)
        {
            double jd = CalculateJulianDay(dateTime);
            double t = CalculateJulianCentury(jd);
            double solarDec = CalculateSunDeclination(t);
            double eqTime = CalculateEquationOfTime(t);

            double hourAngle = CalculateHourAngle(dateTime, longitude, eqTime);
            double azimuth = CalculateAzimuth(hourAngle, latitude, solarDec);

            return azimuth;
        }

        public double GetSolarElevation(DateTime dateTime, double latitude, double longitude)
        {
            double jd = CalculateJulianDay(dateTime);
            double t = CalculateJulianCentury(jd);
            double solarDec = CalculateSunDeclination(t);
            double eqTime = CalculateEquationOfTime(t);

            double hourAngle = CalculateHourAngle(dateTime, longitude, eqTime);
            double elevation = CalculateElevation(hourAngle, latitude, solarDec);

            return elevation;
        }

        #region Helper Methods

        private DateTime CalculateSunriseSet(DateTime date, double latitude, double longitude, bool isSunrise, double zenith)
        {
            // Get Julian day
            double jd = CalculateJulianDay(date);
            double t = CalculateJulianCentury(jd);

            // Get solar declination and equation of time
            double solarDec = CalculateSunDeclination(t);
            double eqTime = CalculateEquationOfTime(t);

            // Calculate hour angle
            double hourAngle = CalculateHourAngleAtHorizon(latitude, solarDec, zenith);

            if (Double.IsNaN(hourAngle))
                return isSunrise ? DateTime.MinValue : DateTime.MaxValue; // No sunrise/sunset

            if (!isSunrise)
                hourAngle = -hourAngle;

            // Calculate minutes
            double delta = longitude + (hourAngle * RadToDeg);
            double timeUTC = 720 - (4.0 * delta) - eqTime; // in minutes

            // Convert to DateTime
            var result = date.Date.AddMinutes(timeUTC);

            // Convert to local time
            return ConvertToLocalTime(result, longitude);
        }

        private DateTime ConvertToLocalTime(DateTime utcTime, double longitude)
        {
            // Calculate timezone offset based on longitude
            // Each 15 degrees of longitude corresponds to 1 hour of time difference
            double tzOffset = Math.Round(longitude / 15.0);

            // Return UTC time adjusted by timezone offset
            return utcTime.AddHours(tzOffset);
        }

        private double CalculateJulianDay(DateTime date)
        {
            // Calculate the Julian day from a date
            int month = date.Month;
            int day = date.Day;
            int year = date.Year;

            if (month <= 2)
            {
                year -= 1;
                month += 12;
            }

            int A = year / 100;
            int B = 2 - A + (A / 4);

            double jd = Math.Floor(365.25 * (year + 4716)) +
                        Math.Floor(30.6001 * (month + 1)) +
                        day + B - 1524.5;

            // Adjust for time of day
            jd += (date.Hour - 12) / 24.0 + date.Minute / 1440.0 + date.Second / 86400.0;

            return jd;
        }

        private double CalculateJulianCentury(double jd)
        {
            // Calculate the Julian century
            return (jd - 2451545.0) / 36525.0;
        }

        private double CalculateSunDeclination(double t)
        {
            // Calculate the sun's declination
            double e = CalculateObliquityCorrection(t);
            double lambda = CalculateSunTrueLongitude(t);
            double sint = Math.Sin(DegToRad * e) * Math.Sin(DegToRad * lambda);

            return RadToDeg * Math.Asin(sint);
        }

        private double CalculateEquationOfTime(double t)
        {
            // Calculate the equation of time in minutes
            double epsilon = CalculateObliquityCorrection(t);
            double l0 = CalculateSunGeometricMeanLongitude(t);
            double e = CalculateEarthOrbitEccentricity(t);
            double m = CalculateSunGeometricMeanAnomaly(t);

            double y = Math.Tan(DegToRad * epsilon / 2.0);
            y *= y;

            double sin2l0 = Math.Sin(2.0 * DegToRad * l0);
            double sinm = Math.Sin(DegToRad * m);
            double cos2l0 = Math.Cos(2.0 * DegToRad * l0);
            double sin4l0 = Math.Sin(4.0 * DegToRad * l0);
            double sin2m = Math.Sin(2.0 * DegToRad * m);

            double Etime = y * sin2l0 - 2.0 * e * sinm + 4.0 * e * y * sinm * cos2l0 -
                          0.5 * y * y * sin4l0 - 1.25 * e * e * sin2m;

            return RadToDeg * Etime * 4.0; // Convert to minutes
        }

        private double CalculateEquationOfTime(DateTime date)
        {
            double jd = CalculateJulianDay(date);
            double t = CalculateJulianCentury(jd);
            return CalculateEquationOfTime(t);
        }

        private double CalculateObliquityCorrection(double t)
        {
            // Calculate the obliquity of the ecliptic
            double obliquity = 23.439291 - 0.0130042 * t;
            return obliquity;
        }

        private double CalculateSunTrueLongitude(double t)
        {
            // Calculate the sun's true longitude
            double m = CalculateSunGeometricMeanAnomaly(t);
            double l0 = CalculateSunGeometricMeanLongitude(t);
            double c = CalculateSunEquationOfCenter(t, m);

            return l0 + c;
        }

        private double CalculateSunGeometricMeanLongitude(double t)
        {
            // Calculate the sun's geometric mean longitude
            double L0 = 280.46646 + 36000.76983 * t + 0.0003032 * t * t;

            while (L0 > 360.0)
                L0 -= 360.0;
            while (L0 < 0.0)
                L0 += 360.0;

            return L0;
        }

        private double CalculateSunGeometricMeanAnomaly(double t)
        {
            // Calculate the sun's geometric mean anomaly
            return 357.52911 + 35999.05029 * t - 0.0001537 * t * t;
        }

        private double CalculateEarthOrbitEccentricity(double t)
        {
            // Calculate the eccentricity of earth's orbit
            return 0.016708634 - 0.000042037 * t - 0.0000001267 * t * t;
        }

        private double CalculateSunEquationOfCenter(double t, double m)
        {
            // Calculate the equation of center for the Sun
            double mrad = DegToRad * m;
            double sinm = Math.Sin(mrad);
            double sin2m = Math.Sin(2 * mrad);
            double sin3m = Math.Sin(3 * mrad);

            return (sinm * (1.914602 - 0.004817 * t - 0.000014 * t * t) +
                   sin2m * (0.019993 - 0.000101 * t) +
                   sin3m * 0.000289);
        }

        private double CalculateHourAngleAtHorizon(double lat, double solarDec, double zenith)
        {
            // Calculate the hour angle at the horizon
            double latRad = DegToRad * lat;
            double sdRad = DegToRad * solarDec;

            double cosHourAngle = (Math.Cos(DegToRad * zenith) - (Math.Sin(latRad) * Math.Sin(sdRad))) /
                                (Math.Cos(latRad) * Math.Cos(sdRad));

            if (cosHourAngle > 1.0) // Sun never rises
                return Double.NaN;
            if (cosHourAngle < -1.0) // Sun never sets
                return Double.NaN;

            return Math.Acos(cosHourAngle);
        }

        private double CalculateHourAngle(DateTime time, double longitude, double eqTime)
        {
            // Calculate the hour angle for a specific time
            double minutes = time.Hour * 60 + time.Minute + time.Second / 60.0;
            double hourAngle = (minutes / 4.0 + longitude - 180 + eqTime) * DegToRad;

            return hourAngle;
        }

        private double CalculateAzimuth(double hourAngle, double latitude, double declination)
        {
            // Calculate the azimuth of the sun
            double latRad = DegToRad * latitude;
            double decRad = DegToRad * declination;

            double azimuthRad = Math.Atan2(
                Math.Sin(hourAngle),
                Math.Cos(hourAngle) * Math.Sin(latRad) - Math.Tan(decRad) * Math.Cos(latRad)
            );

            double azimuth = RadToDeg * azimuthRad;
            azimuth += 180.0; // Adjust to 0-360 degrees

            while (azimuth < 0)
                azimuth += 360;
            while (azimuth >= 360)
                azimuth -= 360;

            return azimuth;
        }

        private double CalculateElevation(double hourAngle, double latitude, double declination)
        {
            // Calculate the elevation angle of the sun
            double latRad = DegToRad * latitude;
            double decRad = DegToRad * declination;

            double elevationRad = Math.Asin(
                Math.Sin(latRad) * Math.Sin(decRad) +
                Math.Cos(latRad) * Math.Cos(decRad) * Math.Cos(hourAngle)
            );

            return RadToDeg * elevationRad;
        }

        #endregion
    }
}