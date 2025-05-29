// Location.Photography.Application/Queries/SunLocation/GetSunPathDataQuery.cs
using Location.Core.Application.Common.Models;
using Location.Photography.Application.Services;
using MediatR;
using System;

namespace Location.Photography.Application.Queries.SunLocation
{
    public class GetSunPathDataQuery : IRequest<Result<SunPathDataResult>>
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime Date { get; set; }
        public int IntervalMinutes { get; set; } = 15;
    }

    public class SunPathDataResult
    {
        public List<SunPathPoint> PathPoints { get; set; } = new();
        public SunPathPoint CurrentPosition { get; set; } = new();
        public DateTime Date { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public SunPathMetrics Metrics { get; set; } = new();
    }

    public class SunPathMetrics
    {
        public TimeSpan DaylightDuration { get; set; }
        public double MaxElevation { get; set; }
        public DateTime MaxElevationTime { get; set; }
        public double SunriseAzimuth { get; set; }
        public double SunsetAzimuth { get; set; }
        public string SeasonalNote { get; set; } = string.Empty;
    }
}