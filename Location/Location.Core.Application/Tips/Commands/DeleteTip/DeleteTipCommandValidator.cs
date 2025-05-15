using FluentValidation;

namespace Location.Core.Application.Tips.Commands.DeleteTip
{
    public class DeleteTipCommandValidator : AbstractValidator<DeleteTipCommand>
    {
        public DeleteTipCommandValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("Id must be greater than 0");
        }
    }
}