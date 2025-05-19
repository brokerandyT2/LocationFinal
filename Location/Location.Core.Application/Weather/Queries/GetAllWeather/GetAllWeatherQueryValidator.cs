using FluentValidation;

namespace Location.Core.Application.Weather.Queries.GetAllWeather
{
    /// <summary>
    /// Provides validation logic for the <see cref="GetAllWeatherQuery"/> query.
    /// </summary>
    /// <remarks>This validator is currently a placeholder and does not define any validation rules. It can be
    /// extended in the future to enforce constraints on the <see cref="GetAllWeatherQuery"/>.</remarks>
    public class GetAllWeatherQueryValidator : AbstractValidator<GetAllWeatherQuery>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GetAllWeatherQueryValidator"/> class.
        /// </summary>
        /// <remarks>This validator is used for the <c>GetAllWeatherQuery</c>. Currently, no validation
        /// rules are required for this query.</remarks>
        public GetAllWeatherQueryValidator()
        {
            // No validation rules needed for this query
        }
    }
}