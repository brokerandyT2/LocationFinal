// Location.Photography.Application/Queries/SunLocation/GetEnhancedSunTimesQuery.cs
using Location.Core.Application.Common.Models;
using Location.Photography.Domain.Models;
using MediatR;

namespace Location.Photography.Application.Queries.SunLocation
{
    public class GetEnhancedSunTimesQuery : IRequest<Result<EnhancedSunTimes>>
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime Date { get; set; }
        public bool UseHighPrecision { get; set; } = true;
    }
}