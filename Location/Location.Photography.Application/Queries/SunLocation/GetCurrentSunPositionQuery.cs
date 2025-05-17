using Location.Core.Application.Common.Models;
using Location.Photography.Domain.Models;
using Location.Photography.Domain.Services;
using MediatR;

namespace Location.Photography.Application.Queries.SunLocation
{
    public class GetCurrentSunPositionQuery : IRequest<Result<SunPositionDto>>
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime DateTime { get; set; }

        public class GetCurrentSunPositionQueryHandler : IRequestHandler<GetCurrentSunPositionQuery, Result<SunPositionDto>>
        {
            private readonly ISunCalculatorService _sunCalculatorService;

            public GetCurrentSunPositionQueryHandler(ISunCalculatorService sunCalculatorService)
            {
                _sunCalculatorService = sunCalculatorService ?? throw new ArgumentNullException(nameof(sunCalculatorService));
            }

            public async Task<Result<SunPositionDto>> Handle(GetCurrentSunPositionQuery request, CancellationToken cancellationToken)
            {
                try
                {
                    var azimuth = _sunCalculatorService.GetSolarAzimuth(request.DateTime, request.Latitude, request.Longitude);
                    var elevation = _sunCalculatorService.GetSolarElevation(request.DateTime, request.Latitude, request.Longitude);

                    var result = new SunPositionDto
                    {
                        Azimuth = azimuth,
                        Elevation = elevation,
                        DateTime = request.DateTime,
                        Latitude = request.Latitude,
                        Longitude = request.Longitude
                    };

                    return Result<SunPositionDto>.Success(result);
                }
                catch (Exception ex)
                {
                    return Result<SunPositionDto>.Failure($"Error calculating sun position: {ex.Message}");
                }
            }
        }
    }
}