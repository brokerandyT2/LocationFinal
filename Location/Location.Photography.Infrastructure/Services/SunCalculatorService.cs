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

        public DateTime GetSunrise(DateTime date, double latitude, double longitude)
        {
            var phases = SunCalc.GetSunPhases(date, latitude, longitude);
            var sunrise = phases.FirstOrDefault(p => p.Name.Value.ToLower() == "sunrise");
            return sunrise.PhaseTime != default ? sunrise.PhaseTime : date; // Return date if no sunrise found
        }

        public DateTime GetSunset(DateTime date, double latitude, double longitude)
        {
            var phases = SunCalc.GetSunPhases(date, latitude, longitude);
            var sunset = phases.FirstOrDefault(p => p.Name.Value.ToLower() == "sunset");
            return sunset.PhaseTime != default ? sunset.PhaseTime : date; // Return date if no sunset found
        }

        public DateTime GetSolarNoon(DateTime date, double latitude, double longitude)
        {
            var phases = SunCalc.GetSunPhases(date, latitude, longitude);
            var solarNoon = phases.FirstOrDefault(p => p.Name.Value.ToLower() == "solar noon");
            return solarNoon.PhaseTime != default ? solarNoon.PhaseTime : date.Date.AddHours(12); // Noon as fallback
        }

        public DateTime GetCivilDawn(DateTime date, double latitude, double longitude)
        {
            var phases = SunCalc.GetSunPhases(date, latitude, longitude);
            var dawn = phases.FirstOrDefault(p => p.Name.Value.ToLower() == "dawn");
            return dawn.PhaseTime != default ? dawn.PhaseTime : date; // Return date if not found
        }

        public DateTime GetCivilDusk(DateTime date, double latitude, double longitude)
        {
            var phases = SunCalc.GetSunPhases(date, latitude, longitude);
            var dusk = phases.FirstOrDefault(p => p.Name.Value.ToLower() == "dusk");
            return dusk.PhaseTime != default ? dusk.PhaseTime : date; // Return date if not found
        }

        public DateTime GetNauticalDawn(DateTime date, double latitude, double longitude)
        {
            var phases = SunCalc.GetSunPhases(date, latitude, longitude);
            var nauticalDawn = phases.FirstOrDefault(p => p.Name.Value.ToLower() == "nautical dawn");
            return nauticalDawn.PhaseTime != default ? nauticalDawn.PhaseTime : date; // Return date if not found
        }

        public DateTime GetNauticalDusk(DateTime date, double latitude, double longitude)
        {
            var phases = SunCalc.GetSunPhases(date, latitude, longitude);
            var nauticalDusk = phases.FirstOrDefault(p => p.Name.Value.ToLower() == "nautical dusk");
            return nauticalDusk.PhaseTime != default ? nauticalDusk.PhaseTime : date; // Return date if not found
        }

        public DateTime GetAstronomicalDawn(DateTime date, double latitude, double longitude)
        {
            var phases = SunCalc.GetSunPhases(date, latitude, longitude);
            var nightEnd = phases.FirstOrDefault(p => p.Name.Value.ToLower() == "night end");
            return nightEnd.PhaseTime != default ? nightEnd.PhaseTime : date; // Return date if not found
        }

        public DateTime GetAstronomicalDusk(DateTime date, double latitude, double longitude)
        {
            var phases = SunCalc.GetSunPhases(date, latitude, longitude);
            var night = phases.FirstOrDefault(p => p.Name.Value == "night");
            return night.PhaseTime != default ? night.PhaseTime : date; // Return date if not found
        }

        public double GetSolarAzimuth(DateTime dateTime, double latitude, double longitude)
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

        public double GetSolarElevation(DateTime dateTime, double latitude, double longitude)
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