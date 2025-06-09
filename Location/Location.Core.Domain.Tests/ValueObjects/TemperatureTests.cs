using FluentAssertions;
using Location.Core.Domain.ValueObjects;
using NUnit.Framework;

namespace Location.Core.Domain.Tests.ValueObjects
{
    [TestFixture]
    public class TemperatureTests
    {
        [Test]
        public void FromCelsius_ShouldCreateInstance()
        {
            // Arrange & Act
            var temperature = Temperature.FromCelsius(25);

            // Assert
            temperature.Celsius.Should().Be(25);
        }

        [Test]
        public void FromCelsius_ShouldRoundToTwoDecimalPlaces()
        {
            // Arrange & Act
            var temperature = Temperature.FromCelsius(25.456789);

            // Assert
            temperature.Celsius.Should().Be(25.46);
        }

        [Test]
        public void FromFahrenheit_ShouldConvertCorrectly()
        {
            // Arrange & Act
            var temperature = Temperature.FromFahrenheit(77);

            // Assert
            temperature.Celsius.Should().BeApproximately(25, 0.01);
            temperature.Fahrenheit.Should().BeApproximately(77, 0.01);
        }

        [Test]
        public void FromKelvin_ShouldConvertCorrectly()
        {
            // Arrange & Act
            var temperature = Temperature.FromKelvin(298.15);

            // Assert
            temperature.Celsius.Should().BeApproximately(25, 0.01);
            temperature.Kelvin.Should().BeApproximately(298.15, 0.01);
        }

        [Test]
        public void Celsius_ToFahrenheit_ShouldConvertCorrectly()
        {
            // Arrange
            var temperature = Temperature.FromCelsius(0);

            // Act & Assert
            temperature.Fahrenheit.Should().Be(32);
        }

        [Test]
        public void Celsius_ToKelvin_ShouldConvertCorrectly()
        {
            // Arrange
            var temperature = Temperature.FromCelsius(0);

            // Act & Assert
            temperature.Kelvin.Should().Be(273.15);
        }

        [Test]
        public void ToString_ShouldReturnFormattedString()
        {
            // Arrange
            var temperature = Temperature.FromCelsius(25);

            // Act
            var result = temperature.ToString();

            // Assert
            result.Should().Be("25.0°C / 77.0°F");
        }

        [Test]
        public void Equals_WithSameValue_ShouldReturnTrue()
        {
            // Arrange
            var temperature1 = Temperature.FromCelsius(25);
            var temperature2 = Temperature.FromCelsius(25);

            // Act
            var result = temperature1.Equals(temperature2);

            // Assert
            result.Should().BeTrue();
        }

        [Test]
        public void Equals_WithDifferentValue_ShouldReturnFalse()
        {
            // Arrange
            var temperature1 = Temperature.FromCelsius(25);
            var temperature2 = Temperature.FromCelsius(26);

            // Act
            var result = temperature1.Equals(temperature2);

            // Assert
            result.Should().BeFalse();
        }

        [Test]
        public void GetHashCode_WithSameValue_ShouldReturnSameHash()
        {
            // Arrange
            var temperature1 = Temperature.FromCelsius(25);
            var temperature2 = Temperature.FromCelsius(25);

            // Act
            var hash1 = temperature1.GetHashCode();
            var hash2 = temperature2.GetHashCode();

            // Assert
            hash1.Should().Be(hash2);
        }

        [Test]
        public void EqualsOperator_WithSameValue_ShouldReturnTrue()
        {
            // Arrange
            var temperature1 = Temperature.FromCelsius(25);
            var temperature2 = Temperature.FromCelsius(25);

            // Act
            var result = temperature1 == temperature2;

            // Assert
            result.Should().BeTrue();
        }

        [Test]
        public void NotEqualsOperator_WithDifferentValue_ShouldReturnTrue()
        {
            // Arrange
            var temperature1 = Temperature.FromCelsius(25);
            var temperature2 = Temperature.FromCelsius(26);

            // Act
            var result = temperature1 != temperature2;

            // Assert
            result.Should().BeTrue();
        }

        [Test]
        public void ConversionChain_ShouldMaintainPrecision()
        {
            // Arrange
            var originalCelsius = 25.0;

            // Act
            var fromFahrenheit = Temperature.FromFahrenheit(Temperature.FromCelsius(originalCelsius).Fahrenheit);
            var fromKelvin = Temperature.FromKelvin(Temperature.FromCelsius(originalCelsius).Kelvin);

            // Assert
            fromFahrenheit.Celsius.Should().BeApproximately(originalCelsius, 0.01);
            fromKelvin.Celsius.Should().BeApproximately(originalCelsius, 0.01);
        }
    }
}