using FluentValidation;

namespace Location.Core.Application.Queries.TipTypes
{
    /// <summary>
    /// Provides validation logic for the <see cref="GetAllTipTypesQuery"/>.
    /// </summary>
    /// <remarks>This validator is used to validate instances of the <see cref="GetAllTipTypesQuery"/> class. 
    /// No validation rules are defined, as the query does not require any specific constraints.</remarks>
    public class GetAllTipTypesQueryValidator : AbstractValidator<GetAllTipTypesQuery>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GetAllTipTypesQueryValidator"/> class.
        /// </summary>
        /// <remarks>This validator is used for the <c>GetAllTipTypesQuery</c>. No validation rules are
        /// defined for this query, as it does not require any specific constraints.</remarks>
        public GetAllTipTypesQueryValidator()
        {
            // No validation rules needed for this query
        }
    }
}