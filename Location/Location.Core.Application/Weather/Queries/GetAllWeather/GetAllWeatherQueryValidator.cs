using FluentValidation;

namespace Location.Core.Application.Weather.Queries.GetAllWeather
{
    public class GetAllWeatherQueryValidator : AbstractValidator<GetAllWeatherQuery>
    {
        public GetAllWeatherQueryValidator()
        {
            // No validation rules needed for this query
        }
    }
}