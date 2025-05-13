using System;
using System.Collections.Generic;

namespace Location.Core.Domain.ValueObjects
{
    /// <summary>
    /// Value object representing temperature with unit conversions
    /// </summary>
    public class Temperature : ValueObject
    {
        private readonly double _celsius;

        public double Celsius => _celsius;
        public double Fahrenheit => (_celsius * 9 / 5) + 32;
        public double Kelvin => _celsius + 273.15;

        private Temperature(double celsius)
        {
            _celsius = Math.Round(celsius, 2);
        }

        public static Temperature FromCelsius(double celsius)
        {
            return new Temperature(celsius);
        }

        public static Temperature FromFahrenheit(double fahrenheit)
        {
            var celsius = (fahrenheit - 32) * 5 / 9;
            return new Temperature(celsius);
        }

        public static Temperature FromKelvin(double kelvin)
        {
            var celsius = kelvin - 273.15;
            return new Temperature(celsius);
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return _celsius;
        }

        public override string ToString()
        {
            return $"{_celsius:F1}°C / {Fahrenheit:F1}°F";
        }
    }
}