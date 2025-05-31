using FluentValidation;

namespace Location.Core.Application.Weather.Queries.GetHourlyForecast
{
    /// <summary>
    /// Validates the properties of a <see cref="GetHourlyForecastQuery"/> instance to ensure they meet the required
    /// constraints.
    /// </summary>
    /// <remarks>This validator enforces the following rules: <list type="bullet"> <item><description>The
    /// <c>LocationId</c> must be greater than 0.</description></item> <item><description>If provided, <c>EndTime</c>
    /// must be greater than <c>StartTime</c>.</description></item> </list> If any of these constraints are violated, a validation error will be
    /// generated with an appropriate message.</remarks>
    public class GetHourlyForecastQueryValidator : AbstractValidator<GetHourlyForecastQuery>
    {
        /// <summary>
        /// Validates the parameters of an hourly weather forecast query.
        /// </summary>
        /// <remarks>This validator ensures that the location ID and time range parameters in the query
        /// meet the required constraints. Specifically: <list type="bullet"> <item><description>LocationId must be
        /// greater than 0.</description></item> <item><description>If both StartTime and EndTime are provided, EndTime must be
        /// greater than StartTime.</description></item> </list>
        /// If any of these constraints are violated, a validation error with an appropriate message is
        /// generated.</remarks>
        public GetHourlyForecastQueryValidator()
        {
            RuleFor(x => x.LocationId)
                .GreaterThan(0).WithMessage("LocationId must be greater than 0");

            RuleFor(x => x)
                .Must(x => !x.StartTime.HasValue || !x.EndTime.HasValue || x.EndTime.Value > x.StartTime.Value)
                .WithMessage("EndTime must be greater than StartTime when both are provided");
        }
    }
}