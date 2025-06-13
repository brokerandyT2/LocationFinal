using FluentValidation;
using Location.Photography.Application.Resources;

namespace Location.Photography.Application.Commands.SceneEvaluation
{
    public class AnalyzeImageCommandValidator : AbstractValidator<AnalyzeImageCommand>
    {
        public AnalyzeImageCommandValidator()
        {
            RuleFor(x => x.ImagePath)
                .NotEmpty()
                .WithMessage(AppResources.CameraEvaluation_ValidationError_ImagePathRequired)
                .Must(BeValidPath)
                .WithMessage(AppResources.CameraEvaluation_ValidationError_ImagePathRequired)
                .Must(BeValidImageExtension)
                .WithMessage(AppResources.CameraEvaluation_ValidationError_ImagePathRequired);
        }

        private bool BeValidPath(string path)
        {
            return !string.IsNullOrWhiteSpace(path);
        }

        private bool BeValidImageExtension(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            var validExtensions = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif" };
            var extension = System.IO.Path.GetExtension(path).ToLowerInvariant();
            return validExtensions.Contains(extension);
        }
    }
}