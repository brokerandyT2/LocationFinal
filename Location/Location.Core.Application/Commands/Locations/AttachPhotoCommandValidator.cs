using FluentValidation;
using Location.Core.Application.Resources;

namespace Location.Core.Application.Commands.Locations
{
    /// <summary>
    /// Validates the <see cref="AttachPhotoCommand"/> to ensure that all required properties meet the specified rules.
    /// </summary>
    /// <remarks>This validator enforces the following rules: <list type="bullet">
    /// <item><description><c>LocationId</c> must be greater than 0.</description></item>
    /// <item><description><c>PhotoPath</c> must be non-empty and a valid file path.</description></item> </list> Use
    /// this validator to ensure that an <see cref="AttachPhotoCommand"/> instance is properly configured before
    /// processing.</remarks>
    public class AttachPhotoCommandValidator : AbstractValidator<AttachPhotoCommand>
    {
        /// <summary>
        /// Validates the properties of the <see cref="AttachPhotoCommand"/> to ensure they meet the required criteria.
        /// </summary>
        /// <remarks>This validator enforces the following rules: <list type="bullet">
        /// <item><description><c>LocationId</c> must be greater than 0.</description></item>
        /// <item><description><c>PhotoPath</c> must not be empty and must represent a valid file
        /// path.</description></item> </list></remarks>
        public AttachPhotoCommandValidator()
        {
            RuleFor(x => x.LocationId)
                .GreaterThan(0)
                .WithMessage(AppResources.Location_ValidationError_LocationIdRequired);

            RuleFor(x => x.PhotoPath)
                .NotEmpty()
                .WithMessage(AppResources.Location_ValidationError_PhotoPathRequired)
                .Must(BeValidPath)
                .WithMessage(AppResources.Location_ValidationError_PhotoPathInvalid);
        }
        /// <summary>
        /// Determines whether the specified path is valid.
        /// </summary>
        /// <remarks>A valid path is non-empty, non-whitespace, and does not contain any invalid path
        /// characters  as defined by <see cref="System.IO.Path.GetInvalidPathChars"/> or additional restricted
        /// characters  such as '|', '<', '>', '"', '?', or '*'.</remarks>
        /// <param name="path">The path to validate. This can be a file or directory path.</param>
        /// <returns><see langword="true"/> if the specified path is valid and does not contain invalid characters;  otherwise,
        /// <see langword="false"/>.</returns>
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