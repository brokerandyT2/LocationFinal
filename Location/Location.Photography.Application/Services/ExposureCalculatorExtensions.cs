// Location.Photography.Maui/Views/Premium/ExposureCalculatorExtensions.cs
using Location.Photography.Application.Services;

namespace Location.Photography.Maui.Views.Premium
{
    public static class ExposureCalculatorExtensions
    {
        public static Application.Services.ExposureIncrements ToApplicationEnum(this ExposureIncrements increment)
        {
            return (Application.Services.ExposureIncrements)increment;
        }

        public static Application.Services.FixedValue ToApplicationEnum(this FixedValue fixedValue)
        {
            return (Application.Services.FixedValue)fixedValue;
        }
    }
}