
using Location.Photography.Application.Services;
using MediatR;

namespace Location.Photography.Application.Queries.SunLocation
{
    public class GetSunPositionQuery : IRequest<Result<SunPositionDto>>
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime DateTime { get; set; }

        public class GetSunPositionQueryHandler : IRequestHandler<GetSunPositionQuery, Result<SunPositionDto>>
        {
            private readonly ISunService _sunService;

            public GetSunPositionQueryHandler(ISunService sunService)
            {
                _sunService = sunService ?? throw new ArgumentNullException(nameof(sunService));
            }

            public async Task<Result<SunPositionDto>> Handle(GetSunPositionQuery request, CancellationToken cancellationToken)
            {
                return await _sunService.GetSunPositionAsync(
                    request.Latitude,
                    request.Longitude,
                    request.DateTime,
                    cancellationToken);
            }
        }
    }
}