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
        /// <summary>
        /// Initializes a new instance of the <see cref="Temperature"/> class with the specified temperature in Celsius.
        /// </summary>
        /// <param name="celsius">The temperature in degrees Celsius. The value is rounded to two decimal places.</param>
        private Temperature(double celsius)
        {
            _celsius = Math.Round(celsius, 2);
        }
        /// <summary>
        /// Creates a <see cref="Temperature"/> instance from a specified temperature in degrees Celsius.
        /// </summary>
        /// <param name="celsius">The temperature in degrees Celsius.</param>
        /// <returns>A <see cref="Temperature"/> instance representing the specified temperature.</returns>
        public static Temperature FromCelsius(double celsius)
        {
            return new Temperature(celsius);
        }
        /// <summary>
        /// Creates a <see cref="Temperature"/> instance from a temperature value in degrees Fahrenheit.
        /// </summary>
        /// <param name="fahrenheit">The temperature in degrees Fahrenheit to convert.</param>
        /// <returns>A <see cref="Temperature"/> instance representing the equivalent temperature in degrees Celsius.</returns>
        public static Temperature FromFahrenheit(double fahrenheit)
        {
            var celsius = (fahrenheit - 32) * 5 / 9;
            return new Temperature(celsius);
        }
        /// <summary>
        /// Creates a <see cref="Temperature"/> instance from a temperature value in Kelvin.
        /// </summary>
        /// <param name="kelvin">The temperature in Kelvin. Must be a non-negative value.</param>
        /// <returns>A <see cref="Temperature"/> instance representing the equivalent temperature in Celsius.</returns>
        public static Temperature FromKelvin(double kelvin)
        {
            var celsius = kelvin - 273.15;
            return new Temperature(celsius);
        }
        /// <summary>
        /// Provides the components used to determine equality for the current object.
        /// </summary>
        /// <remarks>This method is typically used in value object implementations to define equality
        /// based on the values of specific fields or properties.</remarks>
        /// <returns>An <see cref="IEnumerable{T}"/> containing the components that uniquely identify the object.</returns>
        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return _celsius;
        }
        /// <summary>
        /// Returns a string representation of the temperature in both Celsius and Fahrenheit.
        /// </summary>
        /// <returns>A string formatted as "{Celsius}°C / {Fahrenheit}°F", where Celsius is the temperature in degrees Celsius
        /// and Fahrenheit is the equivalent temperature in degrees Fahrenheit.</returns>
        public override string ToString()
        {
            return $"{_celsius:F1}°C / {Fahrenheit:F1}°F";
        }
    }
}