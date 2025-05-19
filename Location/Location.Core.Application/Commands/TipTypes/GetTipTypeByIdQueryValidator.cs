using FluentValidation;

namespace Location.Core.Application.Queries.TipTypes
{
    /// <summary>
    /// Validates the <see cref="GetTipTypeByIdQuery"/> to ensure it meets the required criteria.
    /// </summary>
    /// <remarks>This validator enforces that the <c>Id</c> property of the query is greater than 0.</remarks>
    public class GetTipTypeByIdQueryValidator : AbstractValidator<GetTipTypeByIdQuery>
    {
        /// <summary>
        /// Validates the <see cref="GetTipTypeByIdQuery"/> to ensure its properties meet the required conditions.
        /// </summary>
        /// <remarks>This validator enforces that the <c>Id</c> property of the query must be greater than
        /// 0. If the validation fails, an appropriate error message is provided.</remarks>
        public GetTipTypeByIdQueryValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("Id must be greater than 0");
        }
    }
}