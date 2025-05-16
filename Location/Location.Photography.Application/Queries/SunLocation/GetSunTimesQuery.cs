// Location.Photography.Application/Queries/SunLocation/GetSunTimesQuery.cs
using Location.Core.Application.Common.Models;
using Location.Photography.Application.Services;
using Location.Photography.Domain.Models;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Photography.Application.Queries.SunLocation
{
    public class GetSunTimesQuery : IRequest<Result<SunTimesDto>>
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime Date { get; set; }

        public class GetSunTimesQueryHandler : IRequestHandler<GetSunTimesQuery, Result<SunTimesDto>>
        {
            private readonly ISunService _sunService;

            public GetSunTimesQueryHandler(ISunService sunService)
            {
                _sunService = sunService ?? throw new ArgumentNullException(nameof(sunService));
            }

            public async Task<Result<SunTimesDto>> Handle(GetSunTimesQuery request, CancellationToken cancellationToken)
            {
                return await _sunService.GetSunTimesAsync(
                    request.Latitude,
                    request.Longitude,
                    request.Date,
                    cancellationToken);
            }
        }
    }
}