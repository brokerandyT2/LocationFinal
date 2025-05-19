using FluentValidation;

namespace Location.Core.Application.Weather.Queries.UpdateAllWeather
{
    /// <summary>
    /// Provides validation logic for the <see cref="UpdateAllWeatherQuery"/> query.
    /// </summary>
    /// <remarks>This validator currently does not define any validation rules, as the <see
    /// cref="UpdateAllWeatherQuery"/>  does not require specific constraints. It is included to maintain consistency
    /// and extensibility  within the validation framework.</remarks>
    public class UpdateAllWeatherQueryValidator : AbstractValidator<UpdateAllWeatherQuery>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateAllWeatherQueryValidator"/> class.
        /// </summary>
        /// <remarks>This validator is used for the <c>UpdateAllWeatherQuery</c>. Currently, no validation
        /// rules are defined for this query.</remarks>
        public UpdateAllWeatherQueryValidator()
        {
            // No validation rules needed for this query
        }
    }
}