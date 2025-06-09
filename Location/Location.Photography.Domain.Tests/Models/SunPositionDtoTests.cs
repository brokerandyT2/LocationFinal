using FluentAssertions;
using Location.Photography.Domain.Models;
using NUnit.Framework;

namespace Location.Photography.Domain.Tests.Models
{
    [TestFixture]
    public class SunPositionDtoTests
    {
        private SunPositionDto _sunPositionDto;

        [SetUp]
        public void SetUp()
        {
            _sunPositionDto = new SunPositionDto();
        }

        [Test]
        public void SunPositionDto_Properties_ShouldHaveCorrectTypes()
        {
            // Arrange & Act & Assert
            typeof(SunPositionDto).GetProperty("Azimuth").PropertyType.Should().Be(typeof(double));
            typeof(SunPositionDto).GetProperty("Elevation").PropertyType.Should().Be(typeof(double));
            typeof(SunPositionDto).GetProperty("DateTime").PropertyType.Should().Be(typeof(DateTime));
            typeof(SunPositionDto).GetProperty("Latitude").PropertyType.Should().Be(typeof(double));
            typeof(SunPositionDto).GetProperty("Longitude").PropertyType.Should().Be(typeof(double));
        }

        [Test]
        public void SunPositionDto_DefaultConstructor_ShouldInitializeDefaultValues()
        {
            // Arrange & Act - using the instance created in SetUp

            // Assert
            _sunPositionDto.Azimuth.Should().Be(0);
            _sunPositionDto.Elevation.Should().Be(0);
            _sunPositionDto.DateTime.Should().Be(default(DateTime));
            _sunPositionDto.Latitude.Should().Be(0);
            _sunPositionDto.Longitude.Should().Be(0);
        }

        [Test]
        public void SunPositionDto_Properties_ShouldBeReadWrite()
        {
            // Arrange
            var now = DateTime.Now;
            double azimuth = 180.5;
            double elevation = 60.0;
            double latitude = 47.6062;
            double longitude = -122.3321;

            // Act
            _sunPositionDto.Azimuth = azimuth;
            _sunPositionDto.Elevation = elevation;
            _sunPositionDto.DateTime = now;
            _sunPositionDto.Latitude = latitude;
            _sunPositionDto.Longitude = longitude;

            // Assert
            _sunPositionDto.Azimuth.Should().Be(azimuth);
            _sunPositionDto.Elevation.Should().Be(elevation);
            _sunPositionDto.DateTime.Should().Be(now);
            _sunPositionDto.Latitude.Should().Be(latitude);
            _sunPositionDto.Longitude.Should().Be(longitude);
        }

        [Test]
        public void SunPositionDto_WithValidValues_ShouldStoreThemCorrectly()
        {
            // Arrange
            var now = new DateTime(2024, 6, 15, 12, 0, 0);
            double latitude = 47.6062;
            double longitude = -122.3321;
            double azimuth = 180.0; // South
            double elevation = 65.3; // High in the sky

            // Act
            _sunPositionDto = new SunPositionDto
            {
                Azimuth = azimuth,
                Elevation = elevation,
                DateTime = now,
                Latitude = latitude,
                Longitude = longitude
            };

            // Assert
            _sunPositionDto.Azimuth.Should().Be(azimuth);
            _sunPositionDto.Elevation.Should().Be(elevation);
            _sunPositionDto.DateTime.Should().Be(now);
            _sunPositionDto.Latitude.Should().Be(latitude);
            _sunPositionDto.Longitude.Should().Be(longitude);
        }

        [Test]
        public void SunPositionDto_WithExtremeLatitudeLongitude_ShouldStoreThemCorrectly()
        {
            // Arrange
            var now = new DateTime(2024, 6, 21, 12, 0, 0);
            double latitude = 89.9; // Near North Pole
            double longitude = 179.9; // Near International Date Line
            double azimuth = 180.0;
            double elevation = 23.4;

            // Act
            _sunPositionDto = new SunPositionDto
            {
                Azimuth = azimuth,
                Elevation = elevation,
                DateTime = now,
                Latitude = latitude,
                Longitude = longitude
            };

            // Assert
            _sunPositionDto.Latitude.Should().Be(latitude);
            _sunPositionDto.Longitude.Should().Be(longitude);
        }

        [Test]
        public void SunPositionDto_WithNegativeElevation_ShouldStoreCorrectly()
        {
            // Arrange
            var now = new DateTime(2024, 12, 21, 0, 0, 0); // Winter midnight
            double latitude = 47.6062;
            double longitude = -122.3321;
            double azimuth = 0.0;
            double elevation = -30.0; // Below horizon

            // Act
            _sunPositionDto.Azimuth = azimuth;
            _sunPositionDto.Elevation = elevation;
            _sunPositionDto.DateTime = now;
            _sunPositionDto.Latitude = latitude;
            _sunPositionDto.Longitude = longitude;

            // Assert
            _sunPositionDto.Elevation.Should().Be(elevation);
        }

        [Test]
        public void SunPositionDto_WithHistoricalDate_ShouldStoreCorrectly()
        {
            // Arrange
            var historicalDate = new DateTime(1900, 1, 1, 12, 0, 0);

            // Act
            _sunPositionDto.DateTime = historicalDate;

            // Assert
            _sunPositionDto.DateTime.Should().Be(historicalDate);
        }

        [Test]
        public void SunPositionDto_WithFutureDate_ShouldStoreCorrectly()
        {
            // Arrange
            var futureDate = new DateTime(2050, 1, 1, 12, 0, 0);

            // Act
            _sunPositionDto.DateTime = futureDate;

            // Assert
            _sunPositionDto.DateTime.Should().Be(futureDate);
        }
    }
}