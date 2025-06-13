using Location.Core.Application.Common.Models;
using Location.Photography.Application.Services;
using Location.Photography.Application.Resources;
using MediatR;

namespace Location.Photography.Application.Commands.ExposureCalculator
{
    public class CalculateExposureCommand : IRequest<Result<ExposureSettingsDto>>
    {
        public ExposureTriangleDto BaseExposure { get; set; }
        public string TargetAperture { get; set; }
        public string TargetShutterSpeed { get; set; }
        public string TargetIso { get; set; }
        public ExposureIncrements Increments { get; set; }
        public FixedValue ToCalculate { get; set; }
        public double EvCompensation { get; set; }
    }

    public class CalculateExposureCommandHandler : IRequestHandler<CalculateExposureCommand, Result<ExposureSettingsDto>>
    {
        private readonly IExposureCalculatorService _exposureCalculatorService;

        public CalculateExposureCommandHandler(IExposureCalculatorService exposureCalculatorService)
        {
            _exposureCalculatorService = exposureCalculatorService ?? throw new ArgumentNullException(nameof(exposureCalculatorService));
        }

        public async Task<Result<ExposureSettingsDto>> Handle(CalculateExposureCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // Perform calculations based on what's fixed
                switch (request.ToCalculate)
                {
                    case FixedValue.ShutterSpeeds:
                        return await _exposureCalculatorService.CalculateShutterSpeedAsync(
                            request.BaseExposure, request.TargetAperture, request.TargetIso, request.Increments,
                            cancellationToken, request.EvCompensation);

                    case FixedValue.Aperture:
                        return await _exposureCalculatorService.CalculateApertureAsync(
                            request.BaseExposure, request.TargetShutterSpeed, request.TargetIso, request.Increments,
                            cancellationToken, request.EvCompensation);

                    case FixedValue.ISO:
                        return await _exposureCalculatorService.CalculateIsoAsync(
                            request.BaseExposure, request.TargetShutterSpeed, request.TargetAperture, request.Increments,
                            cancellationToken, request.EvCompensation);

                    default:
                        return Result<ExposureSettingsDto>.Failure(AppResources.ExposureCalculator_Error_InvalidCalculationType);
                }
            }
            catch (Exception ex)
            {
                return Result<ExposureSettingsDto>.Failure(string.Format(AppResources.ExposureCalculator_Error_CalculatingExposure + ": {0}", ex.Message));
            }
        }
    }
}