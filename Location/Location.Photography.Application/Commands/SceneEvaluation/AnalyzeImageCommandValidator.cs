// Location.Photography.Application/Commands/SceneEvaluation/AnalyzeImageCommandValidator.cs
using FluentValidation;
using System.IO;

namespace Location.Photography.Application.Commands.SceneEvaluation
{
    public class AnalyzeImageCommandValidator : AbstractValidator<AnalyzeImageCommand>
    {
        public AnalyzeImageCommandValidator()
        {
          /*  RuleFor(x => x.ImagePath)
                .NotEmpty()
                .WithMessage("Image path is required")
                .Must(BeValidPath)
                .WithMessage("Image path is not valid")
                .Must(BeValidImageExtension)
                .WithMessage("Image must be a valid image file (jpg, jpeg, png, bmp, gif)"); */
        }

        private bool BeValidPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            // Invalid path characters
            char[] invalidChars = Path.GetInvalidPathChars();
            if (path.IndexOfAny(invalidChars) >= 0)
                return false;

            // Check for invalid characters in paths that are not caught by GetInvalidPathChars
            if (path.Contains("|") || path.Contains("<") || path.Contains(">") || path.Contains("\"") || path.Contains("?") || path.Contains("*"))
                return false;

            return true;
        }

        private bool BeValidImageExtension(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            var extension = Path.GetExtension(path).ToLowerInvariant();
            return extension == ".jpg" || extension == ".jpeg" || extension == ".png" || 
                   extension == ".bmp" || extension == ".gif";
        }
    }
}