using FluentValidation;
using Location.Core.Application.Commands.Weather;

namespace Location.Core.Application.Weather.Commands.UpdateWeather
{
    public class UpdateWeatherCommandValidator : AbstractValidator<UpdateWeatherCommand>
    {
        public UpdateWeatherCommandValidator()
        {
            RuleFor(x => x.LocationId)
                .GreaterThan(0).WithMessage("LocationId must be greater than 0");
        }
    }
}