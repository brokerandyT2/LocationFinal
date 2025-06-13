using FluentValidation;
using Location.Photography.Application.Resources;
using Location.Photography.Application.Services;

namespace Location.Photography.Application.Commands.ExposureCalculator
{
    public class CalculateExposureCommandValidator : AbstractValidator<CalculateExposureCommand>
    {
        public CalculateExposureCommandValidator()
        {
            RuleFor(x => x.BaseExposure).NotNull().WithMessage(AppResources.ExposureCalculator_ValidationError_BaseExposureRequired);

            RuleFor(x => x.BaseExposure.ShutterSpeed)
                .NotEmpty().WithMessage(AppResources.ExposureCalculator_ValidationError_ShutterSpeedRequired)
                .When(x => x.BaseExposure != null);

            RuleFor(x => x.BaseExposure.Aperture)
                .NotEmpty().WithMessage(AppResources.ExposureCalculator_ValidationError_ApertureRequired)
                .When(x => x.BaseExposure != null);

            RuleFor(x => x.BaseExposure.Iso)
                .NotEmpty().WithMessage(AppResources.ExposureCalculator_ValidationError_ISORequired)
                .When(x => x.BaseExposure != null);

            RuleFor(x => x.Increments)
                .IsInEnum()
                .WithMessage(AppResources.ExposureCalculator_ValidationError_IncrementValue);

            RuleFor(x => x.ToCalculate)
                .IsInEnum()
                .WithMessage(AppResources.ExposureCalculator_ValidationError_CalculationType);

            RuleFor(x => x.EvCompensation)
                .InclusiveBetween(-5.0, 5.0)
                .WithMessage(AppResources.ExposureCalculator_ValidationError_EVCompensationRange);

            // Validate target values based on what's being calculated
            When(x => x.ToCalculate == FixedValue.ShutterSpeeds, () =>
            {
                RuleFor(x => x.TargetAperture).NotEmpty().WithMessage(AppResources.ExposureCalculator_ValidationError_TargetApertureRequired);

                RuleFor(x => x.TargetAperture)
                    .Must(BeValidAperture)
                    .WithMessage(AppResources.ExposureCalculator_ValidationError_ValidAperture)
                    .When(x => !string.IsNullOrEmpty(x.TargetAperture));

                RuleFor(x => x.TargetIso).NotEmpty().WithMessage(AppResources.ExposureCalculator_ValidationError_TargetISORequired);

                RuleFor(x => x.TargetIso)
                    .Must(BeValidIso)
                    .WithMessage(AppResources.ExposureCalculator_ValidationError_ValidISO)
                    .When(x => !string.IsNullOrEmpty(x.TargetIso));
            });

            When(x => x.ToCalculate == FixedValue.Aperture, () =>
            {
                RuleFor(x => x.TargetShutterSpeed).NotEmpty().WithMessage(AppResources.ExposureCalculator_ValidationError_TargetShutterSpeedRequired);

                RuleFor(x => x.TargetShutterSpeed)
                    .Must(BeValidShutterSpeed)
                    .WithMessage(AppResources.ExposureCalculator_ValidationError_ValidShutterSpeed)
                    .When(x => !string.IsNullOrEmpty(x.TargetShutterSpeed));

                RuleFor(x => x.TargetIso).NotEmpty().WithMessage(AppResources.ExposureCalculator_ValidationError_TargetISORequired);

                RuleFor(x => x.TargetIso)
                    .Must(BeValidIso)
                    .WithMessage(AppResources.ExposureCalculator_ValidationError_ValidISO)
                    .When(x => !string.IsNullOrEmpty(x.TargetIso));
            });

            When(x => x.ToCalculate == FixedValue.ISO, () =>
            {
                RuleFor(x => x.TargetShutterSpeed).NotEmpty().WithMessage(AppResources.ExposureCalculator_ValidationError_TargetShutterSpeedRequired);

                RuleFor(x => x.TargetShutterSpeed)
                    .Must(BeValidShutterSpeed)
                    .WithMessage(AppResources.ExposureCalculator_ValidationError_ValidShutterSpeed)
                    .When(x => !string.IsNullOrEmpty(x.TargetShutterSpeed));

                RuleFor(x => x.TargetAperture).NotEmpty().WithMessage(AppResources.ExposureCalculator_ValidationError_TargetApertureRequired);

                RuleFor(x => x.TargetAperture)
                    .Must(BeValidAperture)
                    .WithMessage(AppResources.ExposureCalculator_ValidationError_ValidAperture)
                    .When(x => !string.IsNullOrEmpty(x.TargetAperture));
            });
        }

        private bool BeValidShutterSpeed(string shutterSpeed)
        {
            if (string.IsNullOrWhiteSpace(shutterSpeed))
                return false;

            // Handle fractional shutter speeds like "1/125"
            if (shutterSpeed.Contains('/'))
            {
                string[] parts = shutterSpeed.Split('/');
                return parts.Length == 2 &&
                       double.TryParse(parts[0], out double numerator) &&
                       double.TryParse(parts[1], out double denominator) &&
                       denominator > 0;
            }
            // Handle speeds with seconds mark like "30""
            else if (shutterSpeed.EndsWith("\""))
            {
                string value = shutterSpeed.TrimEnd('\"');
                return double.TryParse(value, out double seconds) && seconds > 0;
            }
            // Handle regular decimal values
            else
            {
                return double.TryParse(shutterSpeed, out double value) && value > 0;
            }
        }

        private bool BeValidAperture(string aperture)
        {
            if (string.IsNullOrWhiteSpace(aperture))
                return false;

            // Handle f-stop format like "f/2.8"
            if (aperture.StartsWith("f/"))
            {
                string value = aperture.Substring(2);
                return double.TryParse(value, out double fNumber) && fNumber > 0;
            }
            // Handle raw numbers
            else
            {
                return double.TryParse(aperture, out double value) && value > 0;
            }
        }

        private bool BeValidIso(string iso)
        {
            if (string.IsNullOrWhiteSpace(iso))
                return false;

            return int.TryParse(iso, out int value) && value > 0 && value <= 102400; // Reasonable ISO range
        }
    }
}