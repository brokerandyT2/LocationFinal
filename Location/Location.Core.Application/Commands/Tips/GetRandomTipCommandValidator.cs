using FluentValidation;

namespace Location.Core.Application.Commands.Tips
{
    public class GetRandomTipCommandValidator : AbstractValidator<GetRandomTipCommand>
    {
        public GetRandomTipCommandValidator()
        {
            RuleFor(x => x.TipTypeId)
                .GreaterThan(0).WithMessage("TipTypeId must be greater than 0");
        }
    }
}