// Location.Photography.Application/Commands/ExposureCalculator/CalculateExposureCommandValidator.cs
using FluentValidation;
using Location.Photography.Application.Services;

namespace Location.Photography.Application.Commands.ExposureCalculator
{
    public class CalculateExposureCommandValidator : AbstractValidator<CalculateExposureCommand>
    {
        public CalculateExposureCommandValidator()
        {
            RuleFor(x => x.BaseExposure).NotNull().WithMessage("Base exposure settings are required");

            RuleFor(x => x.BaseExposure.ShutterSpeed)
                .NotEmpty().WithMessage("Base shutter speed is required")
                .When(x => x.BaseExposure != null);

            RuleFor(x => x.BaseExposure.Aperture)
                .NotEmpty().WithMessage("Base aperture is required")
                .When(x => x.BaseExposure != null);

            RuleFor(x => x.BaseExposure.Iso)
                .NotEmpty().WithMessage("Base ISO is required")
                .When(x => x.BaseExposure != null);

            // Validate target values based on what's being calculated
            When(x => x.ToCalculate == FixedValue.ShutterSpeeds, () => {
                RuleFor(x => x.TargetAperture).NotEmpty().WithMessage("Target aperture is required");
                RuleFor(x => x.TargetIso).NotEmpty().WithMessage("Target ISO is required");
            });

            When(x => x.ToCalculate == FixedValue.Aperture, () => {
                RuleFor(x => x.TargetShutterSpeed).NotEmpty().WithMessage("Target shutter speed is required");
                RuleFor(x => x.TargetIso).NotEmpty().WithMessage("Target ISO is required");
            });

            When(x => x.ToCalculate == FixedValue.ISO, () => {
                RuleFor(x => x.TargetShutterSpeed).NotEmpty().WithMessage("Target shutter speed is required");
                RuleFor(x => x.TargetAperture).NotEmpty().WithMessage("Target aperture is required");
            });
        }
    }
}