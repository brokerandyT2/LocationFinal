using FluentValidation;

namespace Location.Core.Application.Queries.TipTypes
{
    public class GetTipTypeByIdQueryValidator : AbstractValidator<GetTipTypeByIdQuery>
    {
        public GetTipTypeByIdQueryValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("Id must be greater than 0");
        }
    }
}