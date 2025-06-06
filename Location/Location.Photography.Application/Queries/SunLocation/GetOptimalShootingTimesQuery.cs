using Location.Core.Application.Common.Models;
using Location.Photography.Domain.Models;
using MediatR;

namespace Location.Photography.Application.Queries.SunLocation
{
    public class GetOptimalShootingTimesQuery : IRequest<Result<List<OptimalShootingTime>>>
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime Date { get; set; }
        public bool IncludeWeatherForecast { get; set; } = false;
        public string TimeZone { get; set; }
    }
}