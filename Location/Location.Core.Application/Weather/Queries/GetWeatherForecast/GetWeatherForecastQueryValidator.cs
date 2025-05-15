using FluentValidation;

namespace Location.Core.Application.Weather.Queries.GetWeatherForecast
{
    public class GetWeatherForecastQueryValidator : AbstractValidator<GetWeatherForecastQuery>
    {
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