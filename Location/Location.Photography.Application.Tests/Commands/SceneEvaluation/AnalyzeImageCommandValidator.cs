// Location.Photography.Application/Commands/SceneEvaluation/AnalyzeImageCommandValidator.cs
using FluentValidation;
using System.IO;

namespace Location.Photography.Application.Commands.SceneEvaluation
{
    public class AnalyzeImageCommandValidator : AbstractValidator<AnalyzeImageCommand>
    {
        private static readonly string[] SupportedImageExtensions = { ".jpg", ".jpeg", ".png", ".bmp", ".gif" };
        private static readonly string[] UnsupportedVideoExtensions = { ".mp4", ".avi", ".mov", ".wmv", ".flv", ".mkv" };

        public AnalyzeImageCommandValidator()
        {
            RuleFor(x => x.ImagePath)
                .NotNull()
                .WithMessage("Image path cannot be null")
                .NotEmpty()
                .WithMessage("Image path is required")
                .Must(BeValidPath)
                .WithMessage("Image path is not valid")
                .Must(BeValidImageExtension)
                .WithMessage("Image must be a valid image file (jpg, jpeg, png, bmp, gif)");
        }

        private bool BeValidPath(string imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath))
                return false;

            // Check for invalid path characters
            var invalidChars = Path.GetInvalidPathChars();
            if (imagePath.IndexOfAny(invalidChars) >= 0)
                return false;

            // Check path length (Windows MAX_PATH is 260, but we'll be more restrictive)
            if (imagePath.Length > 250)
                return false;

            // Must have an extension
            if (!Path.HasExtension(imagePath))
                return false;

            return true;
        }

        private bool BeValidImageExtension(string imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath))
                return false;

            var extension = Path.GetExtension(imagePath)?.ToLowerInvariant();

            if (string.IsNullOrEmpty(extension))
                return false;

            // Check if it's a supported image extension
            if (!SupportedImageExtensions.Contains(extension))
                return false;

            // Explicitly reject video extensions
            if (UnsupportedVideoExtensions.Contains(extension))
                return false;

            return true;
        }
    }
}