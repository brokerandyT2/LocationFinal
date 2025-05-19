using FluentValidation;

namespace Location.Core.Application.Weather.Queries.GetWeatherForecast
{
    /// <summary>
    /// Validates the properties of a <see cref="GetWeatherForecastQuery"/> instance to ensure they meet the required
    /// constraints.
    /// </summary>
    /// <remarks>This validator enforces the following rules: <list type="bullet"> <item><description>The
    /// <c>Latitude</c> must be between -90 and 90 degrees.</description></item> <item><description>The <c>Longitude</c>
    /// must be between -180 and 180 degrees.</description></item> <item><description>The <c>Days</c> must be between 1
    /// and 7.</description></item> </list> If any of these constraints are violated, a validation error will be
    /// generated with an appropriate message.</remarks>
    public class GetWeatherForecastQueryValidator : AbstractValidator<GetWeatherForecastQuery>
    {
        /// <summary>
        /// Validates the parameters of a weather forecast query.
        /// </summary>
        /// <remarks>This validator ensures that the latitude, longitude, and number of days in the query
        /// meet the required constraints. Specifically: <list type="bullet"> <item><description>Latitude must be
        /// between -90 and 90 degrees.</description></item> <item><description>Longitude must be between -180 and 180
        /// degrees.</description></item> <item><description>Days must be between 1 and 7.</description></item> </list>
        /// If any of these constraints are violated, a validation error with an appropriate message is
        /// generated.</remarks>
        public GetWeatherForecastQueryValidator()
        {
            RuleFor(x => x.Latitude)
                .InclusiveBetween(-90, 90).WithMessage("Latitude must be between -90 and 90 degrees");

            RuleFor(x => x.Longitude)
                .InclusiveBetween(-180, 180).WithMessage("Longitude must be between -180 and 180 degrees");

            RuleFor(x => x.Days)
                .InclusiveBetween(1, 7).WithMessage("Days must be between 1 and 7");
        }
    }
}