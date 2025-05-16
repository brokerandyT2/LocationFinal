// Location.Photography.Application/Commands/ExposureCalculator/CalculateExposureCommand.cs
using Location.Core.Application.Common.Models;
using Location.Photography.Application.Services;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

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

    public enum FixedValue
    {
        ShutterSpeeds = 0,
        ISO = 1,
        Empty = 2,
        Aperture = 3
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
                // Apply EV compensation if needed (simplified implementation)
                // In a real implementation, you would modify the exposure values based on EVValue

                // Perform calculations based on what's fixed
                switch (request.ToCalculate)
                {
                    case FixedValue.ShutterSpeeds:
                        return await _exposureCalculatorService.CalculateShutterSpeedAsync(
                            request.BaseExposure, request.TargetAperture, request.TargetIso, request.Increments, cancellationToken);

                    case FixedValue.Aperture:
                        return await _exposureCalculatorService.CalculateApertureAsync(
                            request.BaseExposure, request.TargetShutterSpeed, request.TargetIso, request.Increments, cancellationToken);

                    case FixedValue.ISO:
                        return await _exposureCalculatorService.CalculateIsoAsync(
                            request.BaseExposure, request.TargetShutterSpeed, request.TargetAperture, request.Increments, cancellationToken);

                    default:
                        return Result<ExposureSettingsDto>.Failure("Invalid calculation type");
                }
            }
            catch (Exception ex)
            {
                return Result<ExposureSettingsDto>.Failure($"Error calculating exposure: {ex.Message}");
            }
        }
    }
}