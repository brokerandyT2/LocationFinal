using FluentValidation;
using Location.Core.Application.Commands.Weather;
using Location.Core.Application.Resources;

namespace Location.Core.Application.Weather.Commands.UpdateWeather
{
    /// <summary>
    /// Provides validation rules for the <see cref="UpdateWeatherCommand"/>.
    /// </summary>
    /// <remarks>This validator ensures that the properties of the <see cref="UpdateWeatherCommand"/> meet the
    /// required conditions before the command is processed. For example, it validates that the <c>LocationId</c> is
    /// greater than 0.</remarks>
    public class UpdateWeatherCommandValidator : AbstractValidator<UpdateWeatherCommand>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateWeatherCommandValidator"/> class.
        /// </summary>
        /// <remarks>This validator ensures that the properties of an <c>UpdateWeatherCommand</c> meet the
        /// required conditions. Specifically, it validates that the <c>LocationId</c> is greater than 0.</remarks>
        public UpdateWeatherCommandValidator()
        {
            RuleFor(x => x.LocationId)
                .GreaterThan(0).WithMessage(AppResources.Location_ValidationError_LocationIdRequired);
        }
    }
}