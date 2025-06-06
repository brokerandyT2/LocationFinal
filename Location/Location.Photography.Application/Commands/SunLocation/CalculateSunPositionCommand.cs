using Location.Core.Application.Common.Models;
using Location.Photography.Application.Services;
using Location.Photography.Domain.Models;
using MediatR;

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
                try
                {
                    return await _sunService.GetSunPositionAsync(
                        request.Latitude,
                        request.Longitude,
                        request.DateTime,
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    return Result<SunPositionDto>.Failure($"Error calculating sun position: {ex.Message}");
                }
            }
        }
    }
}