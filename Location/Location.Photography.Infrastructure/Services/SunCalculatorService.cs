// Location.Photography.Infrastructure.Services.SunCalculatorService.cs
using Location.Photography.Domain.Services;
using SunCalcNet;
using SunCalcNet.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Location.Photography.Infrastructure.Services
{
    public class SunCalculatorService : ISunCalculatorService
    {
        // Constants for conversion
        private const double DegToRad = Math.PI / 180.0;
        private const double RadToDeg = 180.0 / Math.PI;

        public DateTime GetSunrise(DateTime date, double latitude, double longitude, string timezone)
        {
            var phases = SunCalc.GetSunPhases(date, latitude, longitude);
            var sunrise = phases.FirstOrDefault(p => p.Name.Value.ToLower() == "sunrise");
            var x = sunrise.PhaseTime.ToLocalTime() != default ? sunrise.PhaseTime.ToLocalTime() : date; // Return date if no sunrise found
            return x;
        }

        public DateTime GetSunset(DateTime date, double latitude, double longitude, string timezone)
        {
            var phases = SunCalc.GetSunPhases(date, latitude, longitude);
            var sunset = phases.FirstOrDefault(p => p.Name.Value.ToLower() == "sunset");
            var x = sunset.PhaseTime.ToLocalTime() != default ? sunset.PhaseTime.ToLocalTime() : date; // Return date if no sunset found
            return x;
        }

        public DateTime GetSolarNoon(DateTime date, double latitude, double longitude, string timezone)
        {
            var phases = SunCalc.GetSunPhases(date, latitude, longitude);
            var solarNoon = phases.FirstOrDefault(p => p.Name.Value.ToLower() == "solar noon");
            var x = solarNoon.PhaseTime != default ? solarNoon.PhaseTime.ToLocalTime() : date.Date.AddHours(12);
            return x; // Return date at noon if no solar noon found
        }

        public DateTime GetCivilDawn(DateTime date, double latitude, double longitude, string timezone)
        {
            var phases = SunCalc.GetSunPhases(date, latitude, longitude);
            var dawn = phases.FirstOrDefault(p => p.Name.Value.ToLower() == "dawn");
            var x = dawn.PhaseTime.ToLocalTime() != default ? dawn.PhaseTime.ToLocalTime() : date; // Return date if not found
            return x;
        }

        public DateTime GetCivilDusk(DateTime date, double latitude, double longitude, string timezone)
        {
            var phases = SunCalc.GetSunPhases(date, latitude, longitude);
            var dusk = phases.FirstOrDefault(p => p.Name.Value.ToLower() == "dusk");
            var x = dusk.PhaseTime.ToLocalTime() != default ? dusk.PhaseTime.ToLocalTime() : date; // Return date if not found
            return x;
        }

        public DateTime GetNauticalDawn(DateTime date, double latitude, double longitude, string timezone)
        {
            var phases = SunCalc.GetSunPhases(date, latitude, longitude);
            var nauticalDawn = phases.FirstOrDefault(p => p.Name.Value.ToLower() == "nautical dawn");
            var x= nauticalDawn.PhaseTime.ToLocalTime() != default ? nauticalDawn.PhaseTime.ToLocalTime() : date; // Return date if not found
            return x;
        }

        public DateTime GetNauticalDusk(DateTime date, double latitude, double longitude, string timezone)
        {
            var phases = SunCalc.GetSunPhases(date, latitude, longitude);
            var nauticalDusk = phases.FirstOrDefault(p => p.Name.Value.ToLower() == "nautical dusk");
            var x = nauticalDusk.PhaseTime.ToLocalTime() != default ? nauticalDusk.PhaseTime.ToLocalTime() : date; // Return date if not found
        return x;
        }

        public DateTime GetAstronomicalDawn(DateTime date, double latitude, double longitude, string timezone)
        {
            var phases = SunCalc.GetSunPhases(date, latitude, longitude);
            var nightEnd = phases.FirstOrDefault(p => p.Name.Value.ToLower() == "sunrise");
            var x = nightEnd.PhaseTime.AddHours(-2).ToLocalTime() != default ? nightEnd.PhaseTime.ToLocalTime() : date; // Return date if not found
            return x;
        }

        public DateTime GetAstronomicalDusk(DateTime date, double latitude, double longitude, string timezone)
        {
            var phases = SunCalc.GetSunPhases(date, latitude, longitude);
            var night = phases.FirstOrDefault(p => p.Name.Value == "sunset");
            var x = night.PhaseTime.AddHours(2).ToLocalTime() != default ? night.PhaseTime.ToLocalTime() : date; // Return date if not found
            return x;
        }

        public double GetSolarAzimuth(DateTime dateTime, double latitude, double longitude, string timezone)
        {
            var position = SunCalc.GetSunPosition(dateTime, latitude, longitude);

            // Convert from radians to degrees and adjust from [-π, π] to [0, 360]
            double azimuthDegrees = (position.Azimuth * RadToDeg) + 180.0;

            // Ensure azimuth is in range [0, 360)
            while (azimuthDegrees >= 360.0)
                azimuthDegrees -= 360.0;
            while (azimuthDegrees < 0.0)
                azimuthDegrees += 360.0;

            return azimuthDegrees;
        }

        public double GetSolarElevation(DateTime dateTime, double latitude, double longitude, string timezone)
        {
            var position = SunCalc.GetSunPosition(dateTime, latitude, longitude);

            // Convert from radians to degrees
            return position.Altitude * RadToDeg;
        }

        // Helper method to print all sun phases for debugging
        private void PrintAllPhases(IEnumerable<SunPhase> phases)
        {
            foreach (var phase in phases)
            {
                Console.WriteLine($"{phase.Name.Value}: {phase.PhaseTime}");
            }
        }
    }
}