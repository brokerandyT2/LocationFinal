using FluentValidation;

namespace Location.Core.Application.Queries.TipTypes
{
    public class GetAllTipTypesQueryValidator : AbstractValidator<GetAllTipTypesQuery>
    {
        public GetAllTipTypesQueryValidator()
        {
            // No validation rules needed for this query
        }
    }
}