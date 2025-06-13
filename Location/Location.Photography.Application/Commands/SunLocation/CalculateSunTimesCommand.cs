using Location.Core.Application.Common.Models;
using Location.Photography.Application.Services;
using Location.Photography.Domain.Models;
using Location.Photography.Application.Resources;
using MediatR;

namespace Location.Photography.Application.Commands.SunLocation
{
    public class CalculateSunTimesCommand : IRequest<Result<SunTimesDto>>
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime Date { get; set; }

        public class CalculateSunTimesCommandHandler : IRequestHandler<CalculateSunTimesCommand, Result<SunTimesDto>>
        {
            private readonly ISunService _sunService;

            public CalculateSunTimesCommandHandler(ISunService sunService)
            {
                _sunService = sunService ?? throw new ArgumentNullException(nameof(sunService));
            }

            public async Task<Result<SunTimesDto>> Handle(CalculateSunTimesCommand request, CancellationToken cancellationToken)
            {
                try
                {
                    // Check for cancellation
                    cancellationToken.ThrowIfCancellationRequested();

                    // Call the sun service to get the sun times
                    return await _sunService.GetSunTimesAsync(
                        request.Latitude,
                        request.Longitude,
                        request.Date,
                        cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    throw; // Re-throw cancellation exceptions
                }
                catch (Exception ex)
                {
                    // Handle unexpected exceptions by returning a failure result
                    return Result<SunTimesDto>.Failure(AppResources.SunLocation_Error_CalculatingSunTimes);
                }
            }
        }
    }
}