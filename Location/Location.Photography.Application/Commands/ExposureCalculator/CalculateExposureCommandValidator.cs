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

            RuleFor(x => x.Increments)
                .IsInEnum()
                .WithMessage("Invalid exposure increment value");

            RuleFor(x => x.ToCalculate)
                .IsInEnum()
                .WithMessage("Invalid calculation type");

            RuleFor(x => x.EvCompensation)
                .InclusiveBetween(-5.0, 5.0)
                .WithMessage("EV compensation must be between -5 and +5 stops");

            // Validate target values based on what's being calculated
            When(x => x.ToCalculate == FixedValue.ShutterSpeeds, () => {
                RuleFor(x => x.TargetAperture).NotEmpty().WithMessage("Target aperture is required");

                RuleFor(x => x.TargetAperture)
                    .Must(BeValidAperture)
                    .WithMessage("Target aperture must be in valid f-stop format (e.g., f/2.8)")
                    .When(x => !string.IsNullOrEmpty(x.TargetAperture));

                RuleFor(x => x.TargetIso).NotEmpty().WithMessage("Target ISO is required");

                RuleFor(x => x.TargetIso)
                    .Must(BeValidIso)
                    .WithMessage("Target ISO must be a valid numeric value (e.g., 100, 400, 1600)")
                    .When(x => !string.IsNullOrEmpty(x.TargetIso));
            });

            When(x => x.ToCalculate == FixedValue.Aperture, () => {
                RuleFor(x => x.TargetShutterSpeed).NotEmpty().WithMessage("Target shutter speed is required");

                RuleFor(x => x.TargetShutterSpeed)
                    .Must(BeValidShutterSpeed)
                    .WithMessage("Target shutter speed must be in valid format (e.g., 1/125, 2\", 0.5)")
                    .When(x => !string.IsNullOrEmpty(x.TargetShutterSpeed));

                RuleFor(x => x.TargetIso).NotEmpty().WithMessage("Target ISO is required");

                RuleFor(x => x.TargetIso)
                    .Must(BeValidIso)
                    .WithMessage("Target ISO must be a valid numeric value (e.g., 100, 400, 1600)")
                    .When(x => !string.IsNullOrEmpty(x.TargetIso));
            });

            When(x => x.ToCalculate == FixedValue.ISO, () => {
                RuleFor(x => x.TargetShutterSpeed).NotEmpty().WithMessage("Target shutter speed is required");

                RuleFor(x => x.TargetShutterSpeed)
                    .Must(BeValidShutterSpeed)
                    .WithMessage("Target shutter speed must be in valid format (e.g., 1/125, 2\", 0.5)")
                    .When(x => !string.IsNullOrEmpty(x.TargetShutterSpeed));

                RuleFor(x => x.TargetAperture).NotEmpty().WithMessage("Target aperture is required");

                RuleFor(x => x.TargetAperture)
                    .Must(BeValidAperture)
                    .WithMessage("Target aperture must be in valid f-stop format (e.g., f/2.8)")
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