using FluentValidation;

namespace Location.Core.Application.Weather.Queries.UpdateAllWeather
{
    public class UpdateAllWeatherQueryValidator : AbstractValidator<UpdateAllWeatherQuery>
    {
        public UpdateAllWeatherQueryValidator()
        {
            // No validation rules needed for this query
        }
    }
}