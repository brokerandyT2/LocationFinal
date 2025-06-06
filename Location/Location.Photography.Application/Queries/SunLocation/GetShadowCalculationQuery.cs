// Location.Photography.Application/Queries/SunLocation/GetShadowCalculationQuery.cs
using Location.Core.Application.Common.Models;
using Location.Photography.Domain.Models;
using MediatR;

namespace Location.Photography.Application.Queries.SunLocation
{
    public class GetShadowCalculationQuery : IRequest<Result<ShadowCalculationResult>>
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime DateTime { get; set; }
        public double ObjectHeight { get; set; } = 6.0; // Default 6 feet
        public TerrainType TerrainType { get; set; } = TerrainType.Flat;
    }
}