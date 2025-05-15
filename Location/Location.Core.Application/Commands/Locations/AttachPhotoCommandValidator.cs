using FluentValidation;

namespace Location.Core.Application.Commands.Locations
{
    public class AttachPhotoCommandValidator : AbstractValidator<AttachPhotoCommand>
    {
        public AttachPhotoCommandValidator()
        {
            RuleFor(x => x.LocationId)
                .GreaterThan(0).WithMessage("LocationId must be greater than 0");

            RuleFor(x => x.PhotoPath)
                .NotEmpty().WithMessage("Photo path is required")
                .Must(BeAValidPath).WithMessage("Photo path is not valid");
        }

        private bool BeAValidPath(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            try
            {
                var fileName = System.IO.Path.GetFileName(path);
                return !string.IsNullOrEmpty(fileName);
            }
            catch
            {
                return false;
            }
        }
    }
}