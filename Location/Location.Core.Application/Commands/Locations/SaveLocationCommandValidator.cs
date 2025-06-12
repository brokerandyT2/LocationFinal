using FluentValidation;
using Location.Core.Application.Resources;

namespace Location.Core.Application.Commands.Locations
{
    /// <summary>
    /// Validates the properties of a <see cref="SaveLocationCommand"/> to ensure they meet the required constraints.
    /// </summary>
    /// <remarks>This validator enforces the following rules: <list type="bullet">
    /// <item><description><c>Title</c> must not be empty and must not exceed 100 characters.</description></item>
    /// <item><description><c>Description</c> must not exceed 500 characters.</description></item>
    /// <item><description><c>Latitude</c> must be between -90 and 90 degrees.</description></item>
    /// <item><description><c>Longitude</c> must be between -180 and 180 degrees.</description></item>
    /// <item><description>Coordinates cannot represent Null Island (0,0).</description></item> <item><description>If
    /// provided, <c>PhotoPath</c> must be a valid file path.</description></item> </list></remarks>
    public class SaveLocationCommandValidator : AbstractValidator<SaveLocationCommand>
    {
        /// <summary>
        /// Validates the properties of a save location command to ensure they meet the required constraints.
        /// </summary>
        /// <remarks>This validator enforces the following rules: <list type="bullet">
        /// <item><description><c>Title</c> must not be empty and must not exceed 100 characters.</description></item>
        /// <item><description><c>Description</c> must not exceed 500 characters.</description></item>
        /// <item><description><c>Latitude</c> must be between -90 and 90 degrees.</description></item>
        /// <item><description><c>Longitude</c> must be between -180 and 180 degrees.</description></item>
        /// <item><description>Coordinates cannot represent Null Island (0,0).</description></item>
        /// <item><description><c>PhotoPath</c>, if provided, must be a valid file path.</description></item>
        /// </list></remarks>
        public SaveLocationCommandValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty()
                .WithMessage(AppResources.Location_ValidationError_TitleRequired)
                .MaximumLength(100)
                .WithMessage(AppResources.Location_ValidationError_TitleMaxLength);

            RuleFor(x => x.Description)
                .MaximumLength(500)
                .WithMessage(AppResources.Location_ValidationError_DescriptionMaxLength);

            RuleFor(x => x.Latitude)
                .InclusiveBetween(-90, 90)
                .WithMessage(AppResources.Location_ValidationError_LatitudeRange);

            RuleFor(x => x.Longitude)
                .InclusiveBetween(-180, 180)
                .WithMessage(AppResources.Location_ValidationError_LongitudeRange);

            // Null Island validation (0,0 coordinates)
            RuleFor(x => x)
                .Must(x => !(x.Latitude == 0 && x.Longitude == 0))
                .WithMessage(AppResources.Location_ValidationError_NullIslandCoordinates)
                .WithName(AppResources.Coordinates_ValidationError_CoordinatesName);

            RuleFor(x => x.PhotoPath)
                .Must(BeAValidPath)
                .When(x => !string.IsNullOrEmpty(x.PhotoPath))
                .WithMessage(AppResources.Location_ValidationError_PhotoPathInvalid);
        }
        /// <summary>
        /// Determines whether the specified path is valid.
        /// </summary>
        /// <remarks>A path is considered valid if it does not contain invalid characters as defined by
        /// <see cref="System.IO.Path.GetInvalidPathChars"/> and resolves to a non-empty file name. If the path is <see
        /// langword="null"/> or consists only of whitespace, it is treated as valid.</remarks>
        /// <param name="path">The path to validate. Can be <see langword="null"/> or empty.</param>
        /// <returns><see langword="true"/> if the path is <see langword="null"/>, empty, or contains no invalid characters and
        /// resolves to a valid file name; otherwise, <see langword="false"/>.</returns>
        private bool BeAValidPath(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return true;

            try
            {
                // Check for invalid characters directly
                if (path.Any(c => Path.GetInvalidPathChars().Contains(c)))
                    return false;

                // Some paths might not throw but still return empty filenames
                var fileName = Path.GetFileName(path);
                return !string.IsNullOrEmpty(fileName);
            }
            catch
            {
                return false;
            }
        }
    }
}