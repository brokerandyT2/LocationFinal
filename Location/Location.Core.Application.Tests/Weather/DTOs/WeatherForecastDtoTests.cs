using FluentAssertions;
using Location.Core.Application.Tests.Utilities;
using Location.Core.Application.Weather.DTOs;
using NUnit.Framework;

namespace Location.Core.Application.Tests.Weather.DTOs
{
    [Category("Weather")]
    [Category("DTO")]

    [TestFixture]
    public class WeatherForecastDtoTests
    {
        [Test]
        public void Constructor_WithDefaultValues_ShouldInitializeProperties()
        {
            // Act
            var dto = new WeatherForecastDto();

            // Assert
            dto.WeatherId.Should().Be(0);
            dto.LastUpdate.Should().Be(default(DateTime));
            dto.Timezone.Should().BeEmpty();
            dto.TimezoneOffset.Should().Be(0);
            dto.DailyForecasts.Should().NotBeNull();
            dto.DailyForecasts.Should().BeEmpty();
        }

        [Test]
        public void Properties_WhenSet_ShouldRetainValues()
        {
            // Arrange
            var dto = new WeatherForecastDto();
            var lastUpdate = DateTime.UtcNow;
            var dailyForecasts = TestDataBuilder.CreateValidDailyForecasts(5);

            // Act
            dto.WeatherId = 42;
            dto.LastUpdate = lastUpdate;
            dto.Timezone = "America/New_York";
            dto.TimezoneOffset = -5;
            dto.DailyForecasts = dailyForecasts;

            // Assert
            dto.WeatherId.Should().Be(42);
            dto.LastUpdate.Should().Be(lastUpdate);
            dto.Timezone.Should().Be("America/New_York");
            dto.TimezoneOffset.Should().Be(-5);
            dto.DailyForecasts.Should().HaveCount(5);
            dto.DailyForecasts.Should().BeEquivalentTo(dailyForecasts);
        }

        [Test]
        public void DailyForecasts_ShouldBeInitializedAsEmptyList()
        {
            // Act
            var dto = new WeatherForecastDto();

            // Assert
            dto.DailyForecasts.Should().NotBeNull();
            dto.DailyForecasts.Should().BeOfType<List<DailyForecastDto>>();
            dto.DailyForecasts.Should().BeEmpty();
        }

        [Test]
        public void AddDailyForecast_ShouldAddToCollection()
        {
            // Arrange
            var dto = new WeatherForecastDto();
            var dailyForecast = new DailyForecastDto
            {
                Date = DateTime.Today,
                Temperature = 20.0,
                Description = "Clear"
            };

            // Act
            dto.DailyForecasts.Add(dailyForecast);

            // Assert
            dto.DailyForecasts.Should().ContainSingle();
            dto.DailyForecasts[0].Should().Be(dailyForecast);
        }
    }

    [TestFixture]
    public class DailyForecastDtoTests
    {
        [Test]
        public void Constructor_WithDefaultValues_ShouldInitializeProperties()
        {
            // Act
            var dto = new DailyForecastDto();

            // Assert
            dto.Date.Should().Be(default(DateTime));
            dto.Sunrise.Should().Be(default(DateTime));
            dto.Sunset.Should().Be(default(DateTime));
            dto.Temperature.Should().Be(0);
            dto.MinTemperature.Should().Be(0);
            dto.MaxTemperature.Should().Be(0);
            dto.Description.Should().BeEmpty();
            dto.Icon.Should().BeEmpty();
            dto.WindSpeed.Should().Be(0);
            dto.WindDirection.Should().Be(0);
            dto.WindGust.Should().BeNull();
            dto.Humidity.Should().Be(0);
            dto.Pressure.Should().Be(0);
            dto.Clouds.Should().Be(0);
            dto.UvIndex.Should().Be(0);
            dto.Precipitation.Should().BeNull();
            dto.MoonRise.Should().BeNull();
            dto.MoonSet.Should().BeNull();
            dto.MoonPhase.Should().Be(0);
        }

        [Test]
        public void Properties_WhenSet_ShouldRetainValues()
        {
            // Arrange
            var dto = new DailyForecastDto();
            var date = DateTime.Today;
            var sunrise = date.AddHours(6.5);
            var sunset = date.AddHours(18.5);
            var moonRise = date.AddHours(21);
            var moonSet = date.AddDays(1).AddHours(8);

            // Act
            dto.Date = date;
            dto.Sunrise = sunrise;
            dto.Sunset = sunset;
            dto.Temperature = 18.0;
            dto.MinTemperature = 12.0;
            dto.MaxTemperature = 24.0;
            dto.Description = "Scattered clouds";
            dto.Icon = "03d";
            dto.WindSpeed = 8.5;
            dto.WindDirection = 225;
            dto.WindGust = 15.0;
            dto.Humidity = 68;
            dto.Pressure = 1018;
            dto.Clouds = 35;
            dto.UvIndex = 6.0;
            dto.Precipitation = 2.5;
            dto.MoonRise = moonRise;
            dto.MoonSet = moonSet;
            dto.MoonPhase = 0.625;

            // Assert
            dto.Date.Should().Be(date);
            dto.Sunrise.Should().Be(sunrise);
            dto.Sunset.Should().Be(sunset);
            dto.Temperature.Should().Be(18.0);
            dto.MinTemperature.Should().Be(12.0);
            dto.MaxTemperature.Should().Be(24.0);
            dto.Description.Should().Be("Scattered clouds");
            dto.Icon.Should().Be("03d");
            dto.WindSpeed.Should().Be(8.5);
            dto.WindDirection.Should().Be(225);
            dto.WindGust.Should().Be(15.0);
            dto.Humidity.Should().Be(68);
            dto.Pressure.Should().Be(1018);
            dto.Clouds.Should().Be(35);
            dto.UvIndex.Should().Be(6.0);
            dto.Precipitation.Should().Be(2.5);
            dto.MoonRise.Should().Be(moonRise);
            dto.MoonSet.Should().Be(moonSet);
            dto.MoonPhase.Should().Be(0.625);
        }

        [Test]
        public void NullableProperties_ShouldAcceptNull()
        {
            // Arrange & Act
            var dto = new DailyForecastDto
            {
                WindGust = null,
                Precipitation = null,
                MoonRise = null,
                MoonSet = null
            };

            // Assert
            dto.WindGust.Should().BeNull();
            dto.Precipitation.Should().BeNull();
            dto.MoonRise.Should().BeNull();
            dto.MoonSet.Should().BeNull();
        }

        [Test]
        public void ObjectInitializer_ShouldSetAllProperties()
        {
            // Arrange
            var date = DateTime.Today;

            // Act
            var dto = new DailyForecastDto
            {
                Date = date,
                Sunrise = date.AddHours(6),
                Sunset = date.AddHours(18),
                Temperature = 20.0,
                MinTemperature = 15.0,
                MaxTemperature = 25.0,
                Description = "Clear sky",
                Icon = "01d",
                WindSpeed = 10.0,
                WindDirection = 180,
                Humidity = 65,
                Pressure = 1013,
                Clouds = 10,
                UvIndex = 5.0,
                MoonPhase = 0.5
            };

            // Assert
            dto.Date.Should().Be(date);
            dto.Temperature.Should().Be(20.0);
            dto.Description.Should().Be("Clear sky");
            dto.Icon.Should().Be("01d");
        }
    }
}