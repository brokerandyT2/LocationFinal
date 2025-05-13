using NUnit.Framework;
using FluentAssertions;
using Location.Core.Domain.Rules;
using Location.Core.Domain.Entities;
using Location.Core.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Location.Core.Domain.Tests.Rules
{
    [TestFixture]
    public class WeatherValidationRulesTests
    {
        private Coordinate _validCoordinate;

        [SetUp]
        public void Setup()
        {
            _validCoordinate = new Coordinate(47.6062, -122.3321);
        }

        [Test]
        public void IsValid_WithValidWeather_ShouldReturnTrue()
        {
            // Arrange
            var weather = new Weather(1, _validCoordinate, "America/Los_Angeles", -7);
            var forecasts = CreateValidForecasts(5);
            weather.UpdateForecasts(forecasts);
            List<string> errors;

            // Act
            var result = WeatherValidationRules.IsValid(weather, out errors);

            // Assert
            result.Should().BeTrue();
            errors.Should().BeEmpty();
        }

        [Test]
        public void IsValid_WithNullWeather_ShouldReturnFalse()
        {
            // Arrange
            List<string> errors;

            // Act
            var result = WeatherValidationRules.IsValid(null, out errors);

            // Assert
            result.Should().BeFalse();
            errors.Should().ContainSingle();
            errors.Should().Contain("Weather cannot be null");
        }

        [Test]
        public void IsValid_WithInvalidLocationId_ShouldReturnFalse()
        {
            // Arrange
            var weather = new Weather(0, _validCoordinate, "America/Los_Angeles", -7);
            List<string> errors;

            // Act
            var result = WeatherValidationRules.IsValid(weather, out errors);

            // Assert
            result.Should().BeFalse();
            errors.Should().ContainSingle();
            errors.Should().Contain("Weather must be associated with a valid location");
        }

        [Test]
        public void IsValid_WithNullCoordinate_ShouldReturnFalse()
        {
            // Arrange
            // Create weather using reflection to bypass constructor validation
            var weather = CreateWeatherWithPrivateConstructor(1, null, "America/Los_Angeles", -7);
            List<string> errors;

            // Act
            var result = WeatherValidationRules.IsValid(weather, out errors);

            // Assert
            result.Should().BeFalse();
            errors.Should().ContainSingle();
            errors.Should().Contain("Weather coordinates are required");
        }

        [Test]
        public void IsValid_WithEmptyTimezone_ShouldReturnFalse()
        {
            // Arrange
            var weather = new Weather(1, _validCoordinate, "", -7);
            List<string> errors;

            // Act
            var result = WeatherValidationRules.IsValid(weather, out errors);

            // Assert
            result.Should().BeFalse();
            errors.Should().ContainSingle();
            errors.Should().Contain("Weather timezone is required");
        }

        [Test]
        public void IsValid_WithMoreThan7Forecasts_ShouldReturnFalse()
        {
            // Arrange
            var weather = new Weather(1, _validCoordinate, "America/Los_Angeles", -7);
            var forecasts = CreateValidForecasts(8);

            // Force addition of all 8 forecasts using reflection
            var forecastsField = weather.GetType().GetField("_forecasts",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var forecastList = forecastsField.GetValue(weather) as List<WeatherForecast>;
            forecastList.AddRange(forecasts);

            List<string> errors;

            // Act
            var result = WeatherValidationRules.IsValid(weather, out errors);

            // Assert
            result.Should().BeFalse();
            errors.Should().ContainSingle();
            errors.Should().Contain("Weather cannot have more than 7 daily forecasts");
        }

        [Test]
        public void IsValid_WithInvalidTemperature_ShouldReturnFalse()
        {
            // Arrange
            var weather = new Weather(1, _validCoordinate, "America/Los_Angeles", -7);
            var forecasts = new[]
            {
                CreateForecastWithTemperature(-274) // Below absolute zero
            };
            weather.UpdateForecasts(forecasts);
            List<string> errors;

            // Act
            var result = WeatherValidationRules.IsValid(weather, out errors);

            // Assert
            result.Should().BeFalse();
            errors.Should().ContainSingle();
            errors.Should().Contain(e => e.Contains("Invalid temperature"));
        }

        [Test]
        public void IsValid_WithMinTempHigherThanMax_ShouldReturnFalse()
        {
            // Arrange
            var weather = new Weather(1, _validCoordinate, "America/Los_Angeles", -7);
            var date = DateTime.Today;
            var forecast = new WeatherForecast(
                1, date, date.AddHours(6), date.AddHours(18),
                Temperature.FromCelsius(20),
                Temperature.FromCelsius(25), // Min temp higher than max
                Temperature.FromCelsius(15), // Max temp
                "Clear", "01d", new WindInfo(10, 180), 65, 1013, 10, 5.0
            );
            weather.UpdateForecasts(new[] { forecast });
            List<string> errors;

            // Act
            var result = WeatherValidationRules.IsValid(weather, out errors);

            // Assert
            result.Should().BeFalse();
            errors.Should().ContainSingle();
            errors.Should().Contain(e => e.Contains("Min temperature cannot exceed max temperature"));
        }

        [TestCase(-1)]
        [TestCase(101)]
        public void IsValid_WithInvalidHumidity_ShouldReturnFalse(int humidity)
        {
            // Arrange
            var weather = new Weather(1, _validCoordinate, "America/Los_Angeles", -7);
            var forecast = CreateForecastWithHumidity(humidity);
            weather.UpdateForecasts(new[] { forecast });
            List<string> errors;

            // Act
            var result = WeatherValidationRules.IsValid(weather, out errors);

            // Assert
            result.Should().BeFalse();
            errors.Should().ContainSingle();
            errors.Should().Contain(e => e.Contains("Invalid humidity percentage"));
        }

        [TestCase(-1)]
        [TestCase(101)]
        public void IsValid_WithInvalidCloudCoverage_ShouldReturnFalse(int clouds)
        {
            // Arrange
            var weather = new Weather(1, _validCoordinate, "America/Los_Angeles", -7);
            var forecast = CreateForecastWithClouds(clouds);
            weather.UpdateForecasts(new[] { forecast });
            List<string> errors;

            // Act
            var result = WeatherValidationRules.IsValid(weather, out errors);

            // Assert
            result.Should().BeFalse();
            errors.Should().ContainSingle();
            errors.Should().Contain(e => e.Contains("Invalid cloud coverage percentage"));
        }

        [TestCase(-1)]
        [TestCase(16)]
        public void IsValid_WithInvalidUvIndex_ShouldReturnFalse(double uvIndex)
        {
            // Arrange
            var weather = new Weather(1, _validCoordinate, "America/Los_Angeles", -7);
            var forecast = CreateForecastWithUvIndex(uvIndex);
            weather.UpdateForecasts(new[] { forecast });
            List<string> errors;

            // Act
            var result = WeatherValidationRules.IsValid(weather, out errors);

            // Assert
            result.Should().BeFalse();
            errors.Should().ContainSingle();
            errors.Should().Contain(e => e.Contains("Invalid UV index"));
        }

        [TestCase(-0.1)]
        [TestCase(1.1)]
        public void IsValid_WithInvalidMoonPhase_ShouldReturnFalse(double moonPhase)
        {
            // Arrange
            var weather = new Weather(1, _validCoordinate, "America/Los_Angeles", -7);
            var forecast = CreateValidForecasts(1).First();

            // Use reflection to set invalid moon phase
            var moonPhaseProperty = forecast.GetType().GetProperty("MoonPhase");
            moonPhaseProperty.SetValue(forecast, moonPhase);

            weather.UpdateForecasts(new[] { forecast });
            List<string> errors;

            // Act
            var result = WeatherValidationRules.IsValid(weather, out errors);

            // Assert
            result.Should().BeFalse();
            errors.Should().ContainSingle();
            errors.Should().Contain(e => e.Contains("Invalid moon phase"));
        }

        [Test]
        public void IsStale_WithRecentUpdate_ShouldReturnFalse()
        {
            // Arrange
            var weather = new Weather(1, _validCoordinate, "America/Los_Angeles", -7);
            weather.UpdateForecasts(CreateValidForecasts(1));
            var maxAge = TimeSpan.FromHours(1);

            // Act
            var result = WeatherValidationRules.IsStale(weather, maxAge);

            // Assert
            result.Should().BeFalse();
        }

        [Test]
        public void IsStale_WithOldUpdate_ShouldReturnTrue()
        {
            // Arrange
            var weather = new Weather(1, _validCoordinate, "America/Los_Angeles", -7);
            weather.UpdateForecasts(CreateValidForecasts(1));

            // Set LastUpdate to 2 hours ago using reflection
            var lastUpdateProperty = weather.GetType().GetProperty("LastUpdate");
            lastUpdateProperty.SetValue(weather, DateTime.UtcNow.AddHours(-2));

            var maxAge = TimeSpan.FromHours(1);

            // Act
            var result = WeatherValidationRules.IsStale(weather, maxAge);

            // Assert
            result.Should().BeTrue();
        }

        private List<WeatherForecast> CreateValidForecasts(int count)
        {
            var forecasts = new List<WeatherForecast>();
            for (int i = 0; i < count; i++)
            {
                var date = DateTime.Today.AddDays(i);
                var forecast = new WeatherForecast(
                    1, date, date.AddHours(6), date.AddHours(18),
                    Temperature.FromCelsius(20),
                    Temperature.FromCelsius(15),
                    Temperature.FromCelsius(25),
                    "Clear", "01d", new WindInfo(10, 180), 65, 1013, 10, 5.0
                );
                forecasts.Add(forecast);
            }
            return forecasts;
        }

        private WeatherForecast CreateForecastWithTemperature(double celsius)
        {
            var date = DateTime.Today;
            return new WeatherForecast(
                1, date, date.AddHours(6), date.AddHours(18),
                Temperature.FromCelsius(celsius),
                Temperature.FromCelsius(celsius - 5),
                Temperature.FromCelsius(celsius + 5),
                "Clear", "01d", new WindInfo(10, 180), 65, 1013, 10, 5.0
            );
        }

        private WeatherForecast CreateForecastWithHumidity(int humidity)
        {
            var date = DateTime.Today;

            // Use reflection to create forecast with invalid humidity
            var forecast = System.Activator.CreateInstance(
                typeof(WeatherForecast),
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                null, new object[] { }, null) as WeatherForecast;

            var type = forecast.GetType();
            type.GetProperty("WeatherId").SetValue(forecast, 1);
            type.GetProperty("Date").SetValue(forecast, date);
            type.GetProperty("Sunrise").SetValue(forecast, date.AddHours(6));
            type.GetProperty("Sunset").SetValue(forecast, date.AddHours(18));
            type.GetProperty("Temperature").SetValue(forecast, Temperature.FromCelsius(20));
            type.GetProperty("MinTemperature").SetValue(forecast, Temperature.FromCelsius(15));
            type.GetProperty("MaxTemperature").SetValue(forecast, Temperature.FromCelsius(25));
            type.GetProperty("Description").SetValue(forecast, "Clear");
            type.GetProperty("Icon").SetValue(forecast, "01d");
            type.GetProperty("Wind").SetValue(forecast, new WindInfo(10, 180));
            type.GetProperty("Humidity").SetValue(forecast, humidity);
            type.GetProperty("Pressure").SetValue(forecast, 1013);
            type.GetProperty("Clouds").SetValue(forecast, 10);
            type.GetProperty("UvIndex").SetValue(forecast, 5.0);

            return forecast;
        }

        private WeatherForecast CreateForecastWithClouds(int clouds)
        {
            var date = DateTime.Today;

            // Use reflection to create forecast with invalid clouds
            var forecast = System.Activator.CreateInstance(
                typeof(WeatherForecast),
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                null, new object[] { }, null) as WeatherForecast;

            var type = forecast.GetType();
            type.GetProperty("WeatherId").SetValue(forecast, 1);
            type.GetProperty("Date").SetValue(forecast, date);
            type.GetProperty("Sunrise").SetValue(forecast, date.AddHours(6));
            type.GetProperty("Sunset").SetValue(forecast, date.AddHours(18));
            type.GetProperty("Temperature").SetValue(forecast, Temperature.FromCelsius(20));
            type.GetProperty("MinTemperature").SetValue(forecast, Temperature.FromCelsius(15));
            type.GetProperty("MaxTemperature").SetValue(forecast, Temperature.FromCelsius(25));
            type.GetProperty("Description").SetValue(forecast, "Clear");
            type.GetProperty("Icon").SetValue(forecast, "01d");
            type.GetProperty("Wind").SetValue(forecast, new WindInfo(10, 180));
            type.GetProperty("Humidity").SetValue(forecast, 65);
            type.GetProperty("Pressure").SetValue(forecast, 1013);
            type.GetProperty("Clouds").SetValue(forecast, clouds);
            type.GetProperty("UvIndex").SetValue(forecast, 5.0);

            return forecast;
        }

        private WeatherForecast CreateForecastWithUvIndex(double uvIndex)
        {
            var date = DateTime.Today;

            // Use reflection to create forecast with invalid UV index
            var forecast = System.Activator.CreateInstance(
                typeof(WeatherForecast),
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                null, new object[] { }, null) as WeatherForecast;

            var type = forecast.GetType();
            type.GetProperty("WeatherId").SetValue(forecast, 1);
            type.GetProperty("Date").SetValue(forecast, date);
            type.GetProperty("Sunrise").SetValue(forecast, date.AddHours(6));
            type.GetProperty("Sunset").SetValue(forecast, date.AddHours(18));
            type.GetProperty("Temperature").SetValue(forecast, Temperature.FromCelsius(20));
            type.GetProperty("MinTemperature").SetValue(forecast, Temperature.FromCelsius(15));
            type.GetProperty("MaxTemperature").SetValue(forecast, Temperature.FromCelsius(25));
            type.GetProperty("Description").SetValue(forecast, "Clear");
            type.GetProperty("Icon").SetValue(forecast, "01d");
            type.GetProperty("Wind").SetValue(forecast, new WindInfo(10, 180));
            type.GetProperty("Humidity").SetValue(forecast, 65);
            type.GetProperty("Pressure").SetValue(forecast, 1013);
            type.GetProperty("Clouds").SetValue(forecast, 10);
            type.GetProperty("UvIndex").SetValue(forecast, uvIndex);

            return forecast;
        }

        private Weather CreateWeatherWithPrivateConstructor(int locationId, Coordinate coordinate, string timezone, int timezoneOffset)
        {
            var weather = System.Activator.CreateInstance(
                typeof(Weather),
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                null, new object[] { }, null) as Weather;

            var type = weather.GetType();
            var allFields = type.GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Find and set backing fields directly
            foreach (var prop in type.GetProperties())
            {
                var backingField = allFields.FirstOrDefault(f =>
                    f.Name == $"<{prop.Name}>k__BackingField" || // Auto-property backing field
                    f.Name == $"_{char.ToLower(prop.Name[0])}{prop.Name.Substring(1)}" || // _camelCase
                    f.Name == $"m_{prop.Name}" // m_PropertyName
                );

                if (backingField != null)
                {
                    switch (prop.Name)
                    {
                        case "LocationId":
                            backingField.SetValue(weather, locationId);
                            break;
                        case "Coordinate":
                            backingField.SetValue(weather, coordinate);
                            break;
                        case "Timezone":
                            backingField.SetValue(weather, timezone);
                            break;
                        case "TimezoneOffset":
                            backingField.SetValue(weather, timezoneOffset);
                            break;
                        case "LastUpdate":
                            backingField.SetValue(weather, DateTime.UtcNow);
                            break;
                    }
                }
            }

            return weather;
        }
    }
}