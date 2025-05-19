using FluentAssertions;
using Location.Photography.Domain.Models;
using NUnit.Framework;
using System;

namespace Location.Photography.Domain.Tests.Models
{
    [TestFixture]
    public class SunTimesDtoTests
    {
        private SunTimesDto _sunTimesDto;

        [SetUp]
        public void SetUp()
        {
            _sunTimesDto = new SunTimesDto();
        }

        [Test]
        public void SunTimesDto_Properties_ShouldHaveCorrectTypes()
        {
            // Arrange & Act & Assert
            typeof(SunTimesDto).GetProperty("Date").PropertyType.Should().Be(typeof(DateTime));
            typeof(SunTimesDto).GetProperty("Latitude").PropertyType.Should().Be(typeof(double));
            typeof(SunTimesDto).GetProperty("Longitude").PropertyType.Should().Be(typeof(double));
            typeof(SunTimesDto).GetProperty("Sunrise").PropertyType.Should().Be(typeof(DateTime));
            typeof(SunTimesDto).GetProperty("Sunset").PropertyType.Should().Be(typeof(DateTime));
            typeof(SunTimesDto).GetProperty("SolarNoon").PropertyType.Should().Be(typeof(DateTime));
            typeof(SunTimesDto).GetProperty("AstronomicalDawn").PropertyType.Should().Be(typeof(DateTime));
            typeof(SunTimesDto).GetProperty("AstronomicalDusk").PropertyType.Should().Be(typeof(DateTime));
            typeof(SunTimesDto).GetProperty("NauticalDawn").PropertyType.Should().Be(typeof(DateTime));
            typeof(SunTimesDto).GetProperty("NauticalDusk").PropertyType.Should().Be(typeof(DateTime));
            typeof(SunTimesDto).GetProperty("CivilDawn").PropertyType.Should().Be(typeof(DateTime));
            typeof(SunTimesDto).GetProperty("CivilDusk").PropertyType.Should().Be(typeof(DateTime));
            typeof(SunTimesDto).GetProperty("GoldenHourMorningStart").PropertyType.Should().Be(typeof(DateTime));
            typeof(SunTimesDto).GetProperty("GoldenHourMorningEnd").PropertyType.Should().Be(typeof(DateTime));
            typeof(SunTimesDto).GetProperty("GoldenHourEveningStart").PropertyType.Should().Be(typeof(DateTime));
            typeof(SunTimesDto).GetProperty("GoldenHourEveningEnd").PropertyType.Should().Be(typeof(DateTime));
        }

        [Test]
        public void SunTimesDto_DefaultConstructor_ShouldInitializeDefaultValues()
        {
            // Arrange & Act - using the instance created in SetUp

            // Assert
            _sunTimesDto.Date.Should().Be(default(DateTime));
            _sunTimesDto.Latitude.Should().Be(0);
            _sunTimesDto.Longitude.Should().Be(0);
            _sunTimesDto.Sunrise.Should().Be(default(DateTime));
            _sunTimesDto.Sunset.Should().Be(default(DateTime));
            _sunTimesDto.SolarNoon.Should().Be(default(DateTime));
            _sunTimesDto.AstronomicalDawn.Should().Be(default(DateTime));
            _sunTimesDto.AstronomicalDusk.Should().Be(default(DateTime));
            _sunTimesDto.NauticalDawn.Should().Be(default(DateTime));
            _sunTimesDto.NauticalDusk.Should().Be(default(DateTime));
            _sunTimesDto.CivilDawn.Should().Be(default(DateTime));
            _sunTimesDto.CivilDusk.Should().Be(default(DateTime));
            _sunTimesDto.GoldenHourMorningStart.Should().Be(default(DateTime));
            _sunTimesDto.GoldenHourMorningEnd.Should().Be(default(DateTime));
            _sunTimesDto.GoldenHourEveningStart.Should().Be(default(DateTime));
            _sunTimesDto.GoldenHourEveningEnd.Should().Be(default(DateTime));
        }

        [Test]
        public void SunTimesDto_Properties_ShouldBeReadWrite()
        {
            // Arrange
            var date = new DateTime(2024, 5, 15);
            double latitude = 47.6062;
            double longitude = -122.3321;
            var sunrise = date.AddHours(5).AddMinutes(30);
            var sunset = date.AddHours(20).AddMinutes(45);
            var solarNoon = date.AddHours(13).AddMinutes(7);
            var astronomicalDawn = date.AddHours(4);
            var astronomicalDusk = date.AddHours(22).AddMinutes(15);
            var nauticalDawn = date.AddHours(4).AddMinutes(30);
            var nauticalDusk = date.AddHours(21).AddMinutes(45);
            var civilDawn = date.AddHours(5);
            var civilDusk = date.AddHours(21).AddMinutes(15);
            var goldenHourMorningStart = sunrise;
            var goldenHourMorningEnd = sunrise.AddHours(1);
            var goldenHourEveningStart = sunset.AddHours(-1);
            var goldenHourEveningEnd = sunset;

            // Act
            _sunTimesDto.Date = date;
            _sunTimesDto.Latitude = latitude;
            _sunTimesDto.Longitude = longitude;
            _sunTimesDto.Sunrise = sunrise;
            _sunTimesDto.Sunset = sunset;
            _sunTimesDto.SolarNoon = solarNoon;
            _sunTimesDto.AstronomicalDawn = astronomicalDawn;
            _sunTimesDto.AstronomicalDusk = astronomicalDusk;
            _sunTimesDto.NauticalDawn = nauticalDawn;
            _sunTimesDto.NauticalDusk = nauticalDusk;
            _sunTimesDto.CivilDawn = civilDawn;
            _sunTimesDto.CivilDusk = civilDusk;
            _sunTimesDto.GoldenHourMorningStart = goldenHourMorningStart;
            _sunTimesDto.GoldenHourMorningEnd = goldenHourMorningEnd;
            _sunTimesDto.GoldenHourEveningStart = goldenHourEveningStart;
            _sunTimesDto.GoldenHourEveningEnd = goldenHourEveningEnd;

            // Assert
            _sunTimesDto.Date.Should().Be(date);
            _sunTimesDto.Latitude.Should().Be(latitude);
            _sunTimesDto.Longitude.Should().Be(longitude);
            _sunTimesDto.Sunrise.Should().Be(sunrise);
            _sunTimesDto.Sunset.Should().Be(sunset);
            _sunTimesDto.SolarNoon.Should().Be(solarNoon);
            _sunTimesDto.AstronomicalDawn.Should().Be(astronomicalDawn);
            _sunTimesDto.AstronomicalDusk.Should().Be(astronomicalDusk);
            _sunTimesDto.NauticalDawn.Should().Be(nauticalDawn);
            _sunTimesDto.NauticalDusk.Should().Be(nauticalDusk);
            _sunTimesDto.CivilDawn.Should().Be(civilDawn);
            _sunTimesDto.CivilDusk.Should().Be(civilDusk);
            _sunTimesDto.GoldenHourMorningStart.Should().Be(goldenHourMorningStart);
            _sunTimesDto.GoldenHourMorningEnd.Should().Be(goldenHourMorningEnd);
            _sunTimesDto.GoldenHourEveningStart.Should().Be(goldenHourEveningStart);
            _sunTimesDto.GoldenHourEveningEnd.Should().Be(goldenHourEveningEnd);
        }

        [Test]
        public void SunTimesDto_WithValidValues_ShouldStoreThemCorrectly()
        {
            // Arrange
            var date = new DateTime(2024, 5, 15);
            double latitude = 47.6062;
            double longitude = -122.3321;
            var sunrise = date.AddHours(5).AddMinutes(30);
            var sunset = date.AddHours(20).AddMinutes(45);
            var solarNoon = date.AddHours(13).AddMinutes(7);
            var astronomicalDawn = date.AddHours(4);
            var astronomicalDusk = date.AddHours(22).AddMinutes(15);
            var nauticalDawn = date.AddHours(4).AddMinutes(30);
            var nauticalDusk = date.AddHours(21).AddMinutes(45);
            var civilDawn = date.AddHours(5);
            var civilDusk = date.AddHours(21).AddMinutes(15);
            var goldenHourMorningStart = sunrise;
            var goldenHourMorningEnd = sunrise.AddHours(1);
            var goldenHourEveningStart = sunset.AddHours(-1);
            var goldenHourEveningEnd = sunset;

            // Act
            _sunTimesDto = new SunTimesDto
            {
                Date = date,
                Latitude = latitude,
                Longitude = longitude,
                Sunrise = sunrise,
                Sunset = sunset,
                SolarNoon = solarNoon,
                AstronomicalDawn = astronomicalDawn,
                AstronomicalDusk = astronomicalDusk,
                NauticalDawn = nauticalDawn,
                NauticalDusk = nauticalDusk,
                CivilDawn = civilDawn,
                CivilDusk = civilDusk,
                GoldenHourMorningStart = goldenHourMorningStart,
                GoldenHourMorningEnd = goldenHourMorningEnd,
                GoldenHourEveningStart = goldenHourEveningStart,
                GoldenHourEveningEnd = goldenHourEveningEnd
            };

            // Assert
            _sunTimesDto.Date.Should().Be(date);
            _sunTimesDto.Latitude.Should().Be(latitude);
            _sunTimesDto.Longitude.Should().Be(longitude);
            _sunTimesDto.Sunrise.Should().Be(sunrise);
            _sunTimesDto.Sunset.Should().Be(sunset);
            _sunTimesDto.SolarNoon.Should().Be(solarNoon);
            _sunTimesDto.AstronomicalDawn.Should().Be(astronomicalDawn);
            _sunTimesDto.AstronomicalDusk.Should().Be(astronomicalDusk);
            _sunTimesDto.NauticalDawn.Should().Be(nauticalDawn);
            _sunTimesDto.NauticalDusk.Should().Be(nauticalDusk);
            _sunTimesDto.CivilDawn.Should().Be(civilDawn);
            _sunTimesDto.CivilDusk.Should().Be(civilDusk);
            _sunTimesDto.GoldenHourMorningStart.Should().Be(goldenHourMorningStart);
            _sunTimesDto.GoldenHourMorningEnd.Should().Be(goldenHourMorningEnd);
            _sunTimesDto.GoldenHourEveningStart.Should().Be(goldenHourEveningStart);
            _sunTimesDto.GoldenHourEveningEnd.Should().Be(goldenHourEveningEnd);
        }

        [Test]
        public void SunTimesDto_GoldenHourMorningStartEndTimes_ShouldBeOneHourApart()
        {
            // Arrange
            var date = new DateTime(2024, 5, 15);
            var sunrise = date.AddHours(5).AddMinutes(30);

            // Act
            _sunTimesDto.Sunrise = sunrise;
            _sunTimesDto.GoldenHourMorningStart = sunrise;
            _sunTimesDto.GoldenHourMorningEnd = sunrise.AddHours(1);

            // Assert
            (_sunTimesDto.GoldenHourMorningEnd - _sunTimesDto.GoldenHourMorningStart).Should().Be(TimeSpan.FromHours(1));
        }

        [Test]
        public void SunTimesDto_GoldenHourEveningStartEndTimes_ShouldBeOneHourApart()
        {
            // Arrange
            var date = new DateTime(2024, 5, 15);
            var sunset = date.AddHours(20).AddMinutes(45);

            // Act
            _sunTimesDto.Sunset = sunset;
            _sunTimesDto.GoldenHourEveningStart = sunset.AddHours(-1);
            _sunTimesDto.GoldenHourEveningEnd = sunset;

            // Assert
            (_sunTimesDto.GoldenHourEveningEnd - _sunTimesDto.GoldenHourEveningStart).Should().Be(TimeSpan.FromHours(1));
        }

        [Test]
        public void SunTimesDto_WithExtremeLatitudeLongitude_ShouldStoreThemCorrectly()
        {
            // Arrange
            var date = new DateTime(2024, 6, 21); // Summer solstice
            double latitude = 78.0; // Far north
            double longitude = 179.9; // Near International Date Line
            var mockTime = date.AddHours(12); // Same time for all values in polar day

            // Act
            _sunTimesDto = new SunTimesDto
            {
                Date = date,
                Latitude = latitude,
                Longitude = longitude,
                Sunrise = mockTime,
                Sunset = mockTime,
                SolarNoon = mockTime,
                AstronomicalDawn = mockTime,
                AstronomicalDusk = mockTime,
                NauticalDawn = mockTime,
                NauticalDusk = mockTime,
                CivilDawn = mockTime,
                CivilDusk = mockTime,
                GoldenHourMorningStart = mockTime,
                GoldenHourMorningEnd = mockTime.AddHours(1),
                GoldenHourEveningStart = mockTime.AddHours(23),
                GoldenHourEveningEnd = mockTime
            };

            // Assert
            _sunTimesDto.Latitude.Should().Be(latitude);
            _sunTimesDto.Longitude.Should().Be(longitude);
            // Verify polar day conditions where sun doesn't set
            _sunTimesDto.Sunrise.Should().Be(_sunTimesDto.Sunset);
        }

        [Test]
        public void SunTimesDto_WithHistoricalDate_ShouldStoreCorrectly()
        {
            // Arrange
            var historicalDate = new DateTime(1900, 1, 1);

            // Act
            _sunTimesDto.Date = historicalDate;

            // Assert
            _sunTimesDto.Date.Should().Be(historicalDate);
        }

        [Test]
        public void SunTimesDto_WithFutureDate_ShouldStoreCorrectly()
        {
            // Arrange
            var futureDate = new DateTime(2050, 1, 1);

            // Act
            _sunTimesDto.Date = futureDate;

            // Assert
            _sunTimesDto.Date.Should().Be(futureDate);
        }

        [Test]
        public void SunTimesDto_WithPolarNightValues_ShouldStoreCorrectly()
        {
            // Arrange - Antarctic Circle in winter
            var date = new DateTime(2024, 6, 21); // Winter in southern hemisphere
            double latitude = -78.0; // Far south
            double longitude = 0.0;
            var mockTime = date.AddHours(12); // Same time for all values in polar night

            // Act
            _sunTimesDto = new SunTimesDto
            {
                Date = date,
                Latitude = latitude,
                Longitude = longitude,
                Sunrise = mockTime,
                Sunset = mockTime,
                SolarNoon = mockTime,
                AstronomicalDawn = mockTime,
                AstronomicalDusk = mockTime,
                NauticalDawn = mockTime,
                NauticalDusk = mockTime,
                CivilDawn = mockTime,
                CivilDusk = mockTime,
                GoldenHourMorningStart = mockTime,
                GoldenHourMorningEnd = mockTime.AddHours(1),
                GoldenHourEveningStart = mockTime.AddHours(-1),
                GoldenHourEveningEnd = mockTime
            };

            // Assert
            _sunTimesDto.Latitude.Should().Be(latitude);
            // Verify polar night conditions
            _sunTimesDto.Sunrise.Should().Be(_sunTimesDto.Sunset);
            _sunTimesDto.Sunrise.Should().Be(_sunTimesDto.SolarNoon);
        }
    }
}