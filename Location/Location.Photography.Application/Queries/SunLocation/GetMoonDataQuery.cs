// Location.Photography.Application/Queries/SunLocation/GetMoonDataQuery.cs
using Location.Core.Application.Common.Models;
using Location.Photography.Domain.Models;
using MediatR;

namespace Location.Photography.Application.Queries.SunLocation
{
    public class GetMoonDataQuery : IRequest<Result<MoonPhaseData>>
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime Date { get; set; }
    }
}