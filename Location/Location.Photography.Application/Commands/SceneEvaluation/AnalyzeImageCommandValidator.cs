// Location.Photography.Application/Commands/SceneEvaluation/AnalyzeImageCommandValidator.cs
using FluentValidation;

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

       
    }
}