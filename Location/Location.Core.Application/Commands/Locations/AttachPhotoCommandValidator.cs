using FluentValidation;
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace Location.Core.Application.Commands.Locations
{
    public class AttachPhotoCommandValidator : AbstractValidator<AttachPhotoCommand>
    {
        public AttachPhotoCommandValidator()
        {
            RuleFor(x => x.LocationId)
                .GreaterThan(0)
                .WithMessage("LocationId must be greater than 0");

            RuleFor(x => x.PhotoPath)
                .NotEmpty()
                .WithMessage("Photo path is required")
                .Must(BeValidPath)
                .WithMessage("Photo path is not valid");
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
    }
}