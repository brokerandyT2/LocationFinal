using FluentValidation;

namespace Location.Core.Application.Commands.Locations
{
    public class SaveLocationCommandValidator : AbstractValidator<SaveLocationCommand>
    {
        public SaveLocationCommandValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title is required")
                .MaximumLength(100).WithMessage("Title must not exceed 100 characters");

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Description must not exceed 500 characters");

            RuleFor(x => x.Latitude)
                .InclusiveBetween(-90, 90).WithMessage("Latitude must be between -90 and 90 degrees");

            RuleFor(x => x.Longitude)
                .InclusiveBetween(-180, 180).WithMessage("Longitude must be between -180 and 180 degrees");

            // Null Island validation (0,0 coordinates)
            RuleFor(x => x)
                .Must(x => !(x.Latitude == 0 && x.Longitude == 0))
                .WithMessage("Invalid coordinates: Cannot use Null Island (0,0)")
                .WithName("Coordinates");

            RuleFor(x => x.PhotoPath)
                .Must(BeAValidPath).When(x => !string.IsNullOrEmpty(x.PhotoPath))
                .WithMessage("Photo path is not valid");
        }

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