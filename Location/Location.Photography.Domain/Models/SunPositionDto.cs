// Location.Photography.Domain/Models/SunPositionDto.cs
using System;

namespace Location.Photography.Domain.Models
{
    public class SunPositionDto
    {
        public double Azimuth { get; set; }
        public double Elevation { get; set; }
        public DateTime DateTime { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public bool IsAboveHorizon => Elevation > 0;
        public double Distance { get; set; }
    }
}