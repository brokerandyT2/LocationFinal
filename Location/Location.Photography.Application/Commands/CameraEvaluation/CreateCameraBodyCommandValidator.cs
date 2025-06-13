using FluentValidation;
using Location.Photography.Application.Resources;

namespace Location.Photography.Application.Commands.CameraEvaluation
{
    public class CreateCameraBodyCommandValidator : AbstractValidator<CreateCameraBodyCommand>
    {
        public CreateCameraBodyCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage(AppResources.CameraEvaluation_ValidationError_NameRequired);

            RuleFor(x => x.SensorType)
                .NotEmpty()
                .WithMessage(AppResources.CameraEvaluation_ValidationError_SensorTypeRequired);

            RuleFor(x => x.SensorWidth)
                .GreaterThan(0)
                .WithMessage(AppResources.CameraEvaluation_ValidationError_SensorWidthRequired);

            RuleFor(x => x.SensorHeight)
                .GreaterThan(0)
                .WithMessage(AppResources.CameraEvaluation_ValidationError_SensorHeightRequired);

            RuleFor(x => x.MountType)
                .IsInEnum()
                .WithMessage(AppResources.CameraEvaluation_Error_GettingMountTypes);
        }
    }
}