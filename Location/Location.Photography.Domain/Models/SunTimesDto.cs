// Location.Photography.Domain/Models/SunTimesDto.cs
using System;

namespace Location.Photography.Domain.Models
{
    public class SunTimesDto
    {
        public DateTime Date { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime Sunrise { get; set; }
        public DateTime Sunset { get; set; }
        public DateTime SolarNoon { get; set; }
        public DateTime AstronomicalDawn { get; set; }
        public DateTime AstronomicalDusk { get; set; }
        public DateTime NauticalDawn { get; set; }
        public DateTime NauticalDusk { get; set; }
        public DateTime CivilDawn { get; set; }
        public DateTime CivilDusk { get; set; }
        public DateTime GoldenHourMorningStart { get; set; }
        public DateTime GoldenHourMorningEnd { get; set; }
        public DateTime GoldenHourEveningStart { get; set; }
        public DateTime GoldenHourEveningEnd { get; set; }
    }
}