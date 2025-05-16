using Location.Core.Application.Common.Models;
using Location.Photography.Application.Services;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Photography.Application.Commands.SunLocation
{
    public class CalculateSunPositionCommand : IRequest<Result<SunPositionDto>>
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime DateTime { get; set; }

        public class CalculateSunPositionCommandHandler : IRequestHandler<CalculateSunPositionCommand, Result<SunPositionDto>>
        {
            private readonly ISunService _sunService;

            public CalculateSunPositionCommandHandler(ISunService sunService)
            {
                _sunService = sunService ?? throw new ArgumentNullException(nameof(sunService));
            }

            public async Task<Result<SunPositionDto>> Handle(CalculateSunPositionCommand request, CancellationToken cancellationToken)
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