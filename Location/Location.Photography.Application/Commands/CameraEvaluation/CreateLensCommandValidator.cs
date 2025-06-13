using FluentValidation;
using Location.Photography.Application.Resources;

namespace Location.Photography.Application.Commands.CameraEvaluation
{
    public class CreateLensCommandValidator : AbstractValidator<CreateLensCommand>
    {
        public CreateLensCommandValidator()
        {
            RuleFor(x => x.MinMM)
                .GreaterThan(0)
                .WithMessage(AppResources.CameraEvaluation_ValidationError_FocalLengthRequired);

            RuleFor(x => x.MaxMM)
                .GreaterThanOrEqualTo(x => x.MinMM)
                .WithMessage(AppResources.CameraEvaluation_ValidationError_FocalLengthRequired)
                .When(x => x.MaxMM.HasValue);

            RuleFor(x => x.MinFStop)
                .GreaterThan(0)
                .WithMessage(AppResources.ExposureCalculator_ValidationError_ApertureRequired)
                .When(x => x.MinFStop.HasValue);

            RuleFor(x => x.MaxFStop)
                .GreaterThanOrEqualTo(x => x.MinFStop)
                .WithMessage(AppResources.ExposureCalculator_ValidationError_ApertureRequired)
                .When(x => x.MaxFStop.HasValue && x.MinFStop.HasValue);

            RuleFor(x => x.CompatibleCameraIds)
                .NotEmpty()
                .WithMessage(AppResources.CameraEvaluation_ValidationError_CompatibleCameraRequired);
        }
    }
}