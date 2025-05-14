using NUnit.Framework;
using FluentAssertions;
using Location.Core.Application.Weather.DTOs;
using System;

namespace Location.Core.Application.Tests.Weather.DTOs
{
    [TestFixture]
    public class WeatherDtoTests
    {
        [Test]
        public void Constructor_WithDefaultValues_ShouldInitializeProperties()
        {
            // Act
            var dto = new WeatherDto();

            // Assert
            dto.Id.Should().Be(0);
            dto.LocationId.Should().Be(0);
            dto.Latitude.Should().Be(0);
            dto.Longitude.Should().Be(0);
            dto.Timezone.Should().BeEmpty();
            dto.TimezoneOffset.Should().Be(0);
            dto.LastUpdate.Should().Be(default(DateTime));
            dto.Temperature.Should().Be(0);
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
            dto.Sunrise.Should().Be(default(DateTime));
            dto.Sunset.Should().Be(default(DateTime));
            dto.MoonRise.Should().BeNull();
            dto.MoonSet.Should().BeNull();
            dto.MoonPhase.Should().Be(0);
        }

        [Test]
        public void Properties_WhenSet_ShouldRetainValues()
        {
            // Arrange
            var dto = new WeatherDto();
            var lastUpdate = DateTime.UtcNow;
            var sunrise = DateTime.Today.AddHours(6);
            var sunset = DateTime.Today.AddHours(18);
            var moonRise = DateTime.Today.AddHours(20);
            var moonSet = DateTime.Today.AddDays(1).AddHours(7);

            // Act
            dto.Id = 1;
            dto.LocationId = 42;
            dto.Latitude = 47.6062;
            dto.Longitude = -122.3321;
            dto.Timezone = "America/Los_Angeles";
            dto.TimezoneOffset = -7;
            dto.LastUpdate = lastUpdate;
            dto.Temperature = 22.5;
            dto.Description = "Partly cloudy";
            dto.Icon = "02d";
            dto.WindSpeed = 15.5;
            dto.WindDirection = 270;
            dto.WindGust = 20.0;
            dto.Humidity = 75;
            dto.Pressure = 1015;
            dto.Clouds = 40;
            dto.UvIndex = 7.5;
            dto.Precipitation = 5.5;
            dto.Sunrise = sunrise;
            dto.Sunset = sunset;
            dto.MoonRise = moonRise;
            dto.MoonSet = moonSet;
            dto.MoonPhase = 0.75;

            // Assert
            dto.Id.Should().Be(1);
            dto.LocationId.Should().Be(42);
            dto.Latitude.Should().Be(47.6062);
            dto.Longitude.Should().Be(-122.3321);
            dto.Timezone.Should().Be("America/Los_Angeles");
            dto.TimezoneOffset.Should().Be(-7);
            dto.LastUpdate.Should().Be(lastUpdate);
            dto.Temperature.Should().Be(22.5);
            dto.Description.Should().Be("Partly cloudy");
            dto.Icon.Should().Be("02d");
            dto.WindSpeed.Should().Be(15.5);
            dto.WindDirection.Should().Be(270);
            dto.WindGust.Should().Be(20.0);
            dto.Humidity.Should().Be(75);
            dto.Pressure.Should().Be(1015);
            dto.Clouds.Should().Be(40);
            dto.UvIndex.Should().Be(7.5);
            dto.Precipitation.Should().Be(5.5);
            dto.Sunrise.Should().Be(sunrise);
            dto.Sunset.Should().Be(sunset);
            dto.MoonRise.Should().Be(moonRise);
            dto.MoonSet.Should().Be(moonSet);
            dto.MoonPhase.Should().Be(0.75);
        }

        [Test]
        public void ObjectInitializer_ShouldSetAllProperties()
        {
            // Arrange
            var lastUpdate = DateTime.UtcNow;
            var sunrise = DateTime.Today.AddHours(6);
            var sunset = DateTime.Today.AddHours(18);

            // Act
            var dto = new WeatherDto
            {
                Id = 10,
                LocationId = 5,
                Latitude = 45.5122,
                Longitude = -122.6587,
                Timezone = "America/Los_Angeles",
                TimezoneOffset = -8,
                LastUpdate = lastUpdate,
                Temperature = 18.5,
                Description = "Clear sky",
                Icon = "01d",
                WindSpeed = 12.0,
                WindDirection = 180,
                WindGust = null,
                Humidity = 60,
                Pressure = 1012,
                Clouds = 5,
                UvIndex = 4.0,
                Precipitation = null,
                Sunrise = sunrise,
                Sunset = sunset,
                MoonRise = null,
                MoonSet = null,
                MoonPhase = 0.25
            };

            // Assert
            dto.Id.Should().Be(10);
            dto.LocationId.Should().Be(5);
            dto.Latitude.Should().Be(45.5122);
            dto.Longitude.Should().Be(-122.6587);
            dto.WindGust.Should().BeNull();
            dto.Precipitation.Should().BeNull();
            dto.MoonRise.Should().BeNull();
            dto.MoonSet.Should().BeNull();
        }

        [Test]
        public void NullableProperties_ShouldAcceptNull()
        {
            // Arrange & Act
            var dto = new WeatherDto
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
        public void TimezoneOffset_ShouldAcceptNegativeValues()
        {
            // Arrange & Act
            var dto = new WeatherDto { TimezoneOffset = -12 };

            // Assert
            dto.TimezoneOffset.Should().Be(-12);
        }

        [Test]
        public void MoonPhase_ShouldAcceptDecimalValues()
        {
            // Arrange & Act
            var dto = new WeatherDto { MoonPhase = 0.125 };

            // Assert
            dto.MoonPhase.Should().Be(0.125);
        }
    }
}