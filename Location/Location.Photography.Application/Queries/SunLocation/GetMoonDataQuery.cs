// Location.Photography.Application/Queries/SunLocation/GetMoonDataQuery.cs
using Location.Core.Application.Common.Models;
using Location.Photography.Application.Services;
using MediatR;
using System;

namespace Location.Photography.Application.Queries.SunLocation
{
    public class GetMoonDataQuery : IRequest<Result<MoonPhaseData>>
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime Date { get; set; }
    }
}