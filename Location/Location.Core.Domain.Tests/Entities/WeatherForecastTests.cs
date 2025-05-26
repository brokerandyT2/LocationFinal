using NUnit.Framework;
using FluentAssertions;
using Location.Core.Domain.Entities;
using Location.Core.Domain.ValueObjects;
using System;

namespace Location.Core.Domain.Tests.Entities
{
    [TestFixture]
    public class WeatherForecastTests
    {
        private double _validTemp;
        private WindInfo _validWind;

        [SetUp]
        public void Setup()
        {
            _validTemp = 20;
            _validWind = new WindInfo(10, 180);
        }

        [Test]
        public void Constructor_WithValidValues_ShouldCreateInstance()
        {
            // Arrange & Act
            var forecast = new WeatherForecast(
                1,
                DateTime.Today,
                DateTime.Today.AddHours(6),
                DateTime.Today.AddHours(18),
                _validTemp,
                15,
                25,
                "Clear sky",
                "01d",
                _validWind,
                65,
                1013,
                10,
                5.0
            );

            // Assert
            forecast.WeatherId.Should().Be(1);
            forecast.Date.Should().Be(DateTime.Today.Date);
            forecast.Temperature.Should().Be(_validTemp);
            forecast.Description.Should().Be("Clear sky");
            forecast.Icon.Should().Be("01d");
            forecast.Humidity.Should().Be(65);
            forecast.Pressure.Should().Be(1013);
            forecast.Clouds.Should().Be(10);
            forecast.UvIndex.Should().Be(5.0);
        }

        [Test]
        public void Constructor_WithNullTemperature_ShouldThrowException()
        {
            // Arrange & Act
            Action act = () => new WeatherForecast(
                1,
                DateTime.Today,
                DateTime.Today.AddHours(6),
                DateTime.Today.AddHours(18),
                _validTemp,
                15,
               25,
                "Clear sky",
                "01d",
                _validWind,
                65,
                1013,
                10,
                5.0
            );

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("temperature");
        }

        [Test]
        public void Constructor_WithNullWind_ShouldThrowException()
        {
            // Arrange & Act
            Action act = () => new WeatherForecast(
                1,
                DateTime.Today,
                DateTime.Today.AddHours(6),
                DateTime.Today.AddHours(18),
                _validTemp,
                15,
                25,
                "Clear sky",
                "01d",
                null,
                65,
                1013,
                10,
                5.0
            );

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("wind");
        }

        [TestCase(-1)]
        [TestCase(101)]
        public void Constructor_WithInvalidHumidity_ShouldThrowException(int humidity)
        {
            // Arrange & Act
            Action act = () => new WeatherForecast(
                1,
                DateTime.Today,
                DateTime.Today.AddHours(6),
                DateTime.Today.AddHours(18),
                _validTemp,
                15 ,
                25,
                "Clear sky",
                "01d",
                _validWind,
                humidity,
                1013,
                10,
                5.0
            );

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>()
                .WithParameterName("humidity");
        }

        [TestCase(-1)]
        [TestCase(101)]
        public void Constructor_WithInvalidClouds_ShouldThrowException(int clouds)
        {
            // Arrange & Act
            Action act = () => new WeatherForecast(
                1,
                DateTime.Today,
                DateTime.Today.AddHours(6),
                DateTime.Today.AddHours(18),
                _validTemp,
                15,
                25,
                "Clear sky",
                "01d",
                _validWind,
                65,
                1013,
                clouds,
                5.0
            );

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>()
                .WithParameterName("clouds");
        }

        [Test]
        public void SetMoonData_WithValidValues_ShouldSetProperties()
        {
            // Arrange
            var forecast = CreateValidForecast();
            var moonRise = DateTime.Today.AddHours(20);
            var moonSet = DateTime.Today.AddDays(1).AddHours(7);

            // Act
            forecast.SetMoonData(moonRise, moonSet, 0.75);

            // Assert
            forecast.MoonRise.Should().Be(moonRise);
            forecast.MoonSet.Should().Be(moonSet);
            forecast.MoonPhase.Should().Be(0.75);
        }

        [TestCase(-0.1, 0)]
        [TestCase(1.1, 1)]
        public void SetMoonData_WithOutOfRangeMoonPhase_ShouldClamp(double input, double expected)
        {
            // Arrange
            var forecast = CreateValidForecast();

            // Act
            forecast.SetMoonData(null, null, input);

            // Assert
            forecast.MoonPhase.Should().Be(expected);
        }

        [Test]
        public void SetPrecipitation_WithValidValue_ShouldSetProperty()
        {
            // Arrange
            var forecast = CreateValidForecast();

            // Act
            forecast.SetPrecipitation(15.5);

            // Assert
            forecast.Precipitation.Should().Be(15.5);
        }

        [Test]
        public void SetPrecipitation_WithNegativeValue_ShouldSetToZero()
        {
            // Arrange
            var forecast = CreateValidForecast();

            // Act
            forecast.SetPrecipitation(-5);

            // Assert
            forecast.Precipitation.Should().Be(0);
        }

        [TestCase(0, "New Moon")]
        [TestCase(0.02, "New Moon")]
        [TestCase(0.1, "Waxing Crescent")]
        [TestCase(0.25, "First Quarter")]
        [TestCase(0.4, "Waxing Gibbous")]
        [TestCase(0.5, "Full Moon")]
        [TestCase(0.6, "Waning Gibbous")]
        [TestCase(0.75, "Last Quarter")]
        [TestCase(0.9, "Waning Crescent")]
        [TestCase(0.98, "New Moon")]
        public void GetMoonPhaseDescription_ShouldReturnCorrectDescription(double phase, string expected)
        {
            // Arrange
            var forecast = CreateValidForecast();
            forecast.SetMoonData(null, null, phase);

            // Act
            var result = forecast.GetMoonPhaseDescription();

            // Assert
            result.Should().Be(expected);
        }

        private WeatherForecast CreateValidForecast()
        {
            return new WeatherForecast(
                1,
                DateTime.Today,
                DateTime.Today.AddHours(6),
                DateTime.Today.AddHours(18),
                _validTemp,
                15,
                25,
                "Clear sky",
                "01d",
                _validWind,
                65,
                1013,
                10,
                5.0
            );
        }
    }
}