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
                .Must(BeValidShutterSpeed).WithMessage("Base shutter speed must be in valid format (e.g., 1/125, 2\", 0.5)")
                .When(x => x.BaseExposure != null && !string.IsNullOrEmpty(x.BaseExposure.ShutterSpeed));

            RuleFor(x => x.BaseExposure.Aperture)
                .NotEmpty().WithMessage("Base aperture is required")
                .Must(BeValidAperture).WithMessage("Base aperture must be in valid f-stop format (e.g., f/2.8)")
                .When(x => x.BaseExposure != null && !string.IsNullOrEmpty(x.BaseExposure.Aperture));

            RuleFor(x => x.BaseExposure.Iso)
                .NotEmpty().WithMessage("Base ISO is required")
                .Must(BeValidIso).WithMessage("Base ISO must be a valid numeric value (e.g., 100, 400, 1600)")
                .When(x => x.BaseExposure != null && !string.IsNullOrEmpty(x.BaseExposure.Iso));

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
            When(x => x.ToCalculate == FixedValue.ShutterSpeeds, () =>
            {
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

            When(x => x.ToCalculate == FixedValue.Aperture, () =>
            {
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

            When(x => x.ToCalculate == FixedValue.ISO, () =>
            {
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

            // Ensure target values are not provided for calculations they're not needed for
            When(x => x.ToCalculate == FixedValue.ShutterSpeeds, () =>
            {
                RuleFor(x => x.TargetShutterSpeed)
                    .Empty()
                    .WithMessage("Target shutter speed should not be provided when calculating shutter speed");
            });

            When(x => x.ToCalculate == FixedValue.Aperture, () =>
            {
                RuleFor(x => x.TargetAperture)
                    .Empty()
                    .WithMessage("Target aperture should not be provided when calculating aperture");
            });

            When(x => x.ToCalculate == FixedValue.ISO, () =>
            {
                RuleFor(x => x.TargetIso)
                    .Empty()
                    .WithMessage("Target ISO should not be provided when calculating ISO");
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
                if (parts.Length != 2)
                    return false;

                if (!double.TryParse(parts[0], out double numerator) ||
                    !double.TryParse(parts[1], out double denominator))
                    return false;

                return numerator > 0 && denominator > 0 &&
                       numerator <= 30 && denominator >= 1 && denominator <= 8000;
            }
            // Handle speeds with seconds mark like "30""
            else if (shutterSpeed.EndsWith("\""))
            {
                string value = shutterSpeed.TrimEnd('\"');
                if (!double.TryParse(value, out double seconds))
                    return false;

                return seconds > 0 && seconds <= 30; // Reasonable range for long exposures
            }
            // Handle regular decimal values
            else
            {
                if (!double.TryParse(shutterSpeed, out double value))
                    return false;

                return value > 0 && value <= 30; // Reasonable range
            }
        }

        private bool BeValidAperture(string aperture)
        {
            if (string.IsNullOrWhiteSpace(aperture))
                return false;

            // Handle f-stop format like "f/2.8"
            if (aperture.StartsWith("f/", StringComparison.OrdinalIgnoreCase))
            {
                string value = aperture.Substring(2);
                if (!double.TryParse(value, out double fNumber))
                    return false;

                return fNumber >= 1.0 && fNumber <= 64.0; // Reasonable f-stop range
            }
            // Handle raw numbers without f/ prefix
            else
            {
                if (!double.TryParse(aperture, out double value))
                    return false;

                return value >= 1.0 && value <= 64.0; // Reasonable f-stop range
            }
        }

        private bool BeValidIso(string iso)
        {
            if (string.IsNullOrWhiteSpace(iso))
                return false;

            if (!int.TryParse(iso, out int value))
                return false;

            // Standard ISO values range - allow for custom values within reasonable bounds
            return value >= 25 && value <= 204800 &&
                   (value == 25 || value == 50 || value == 100 || value == 200 ||
                    value == 400 || value == 800 || value == 1600 || value == 3200 ||
                    value == 6400 || value == 12800 || value == 25600 || value == 51200 ||
                    value == 102400 || value == 204800 ||
                    // Allow intermediate values that are powers of 2 or common intermediate steps
                    IsValidIntermediateIso(value));
        }

        private bool IsValidIntermediateIso(int iso)
        {
            // Allow 1/3 and 1/2 stop intermediate values commonly found on cameras
            int[] validIntermediates = { 64, 80, 125, 160, 250, 320, 500, 640, 1000, 1250,
                                       2000, 2500, 4000, 5000, 8000, 10000, 16000, 20000,
                                       32000, 40000, 64000, 80000, 128000, 160000 };

            return Array.IndexOf(validIntermediates, iso) >= 0;
        }
    }
}