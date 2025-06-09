using FluentAssertions;
using Location.Photography.Domain.Interfaces;
using NUnit.Framework;

namespace Location.Photography.Domain.Tests.Interfaces
{
    [TestFixture]
    public class ISunLocationTests
    {
        private class SunLocationImplementation : ISunLocation
        {
            public DateTime SelectedDateTime { get; set; }
            public double Latitude { get; set; }
            public double Longitude { get; set; }
            public double NorthRotationAngle { get; set; }
            public double SunDirection { get; set; }
            public double SunElevation { get; set; }
            public double DeviceTilt { get; set; }
            public bool ElevationMatched { get; set; }
        }

        private SunLocationImplementation _sunLocation;

        [SetUp]
        public void Setup()
        {
            _sunLocation = new SunLocationImplementation();
        }

        [Test]
        public void SunLocation_ShouldImplementISunLocation()
        {
            // Arrange & Act & Assert
            _sunLocation.Should().BeAssignableTo<ISunLocation>();
        }

        [Test]
        public void SunLocation_Properties_ShouldHaveCorrectTypes()
        {
            // Arrange & Act & Assert
            typeof(ISunLocation).GetProperty("SelectedDateTime").PropertyType.Should().Be(typeof(DateTime));
            typeof(ISunLocation).GetProperty("Latitude").PropertyType.Should().Be(typeof(double));
            typeof(ISunLocation).GetProperty("Longitude").PropertyType.Should().Be(typeof(double));
            typeof(ISunLocation).GetProperty("NorthRotationAngle").PropertyType.Should().Be(typeof(double));
            typeof(ISunLocation).GetProperty("SunDirection").PropertyType.Should().Be(typeof(double));
            typeof(ISunLocation).GetProperty("SunElevation").PropertyType.Should().Be(typeof(double));
            typeof(ISunLocation).GetProperty("DeviceTilt").PropertyType.Should().Be(typeof(double));
            typeof(ISunLocation).GetProperty("ElevationMatched").PropertyType.Should().Be(typeof(bool));
        }

        [Test]
        public void SunLocation_Properties_ShouldBeReadWrite()
        {
            // Arrange
            var now = DateTime.Now;
            double latitude = 47.6062;
            double longitude = -122.3321;
            double northRotationAngle = 45.0;
            double sunDirection = 180.0;
            double sunElevation = 60.0;
            double deviceTilt = 15.0;
            bool elevationMatched = true;

            // Act
            _sunLocation.SelectedDateTime = now;
            _sunLocation.Latitude = latitude;
            _sunLocation.Longitude = longitude;
            _sunLocation.NorthRotationAngle = northRotationAngle;
            _sunLocation.SunDirection = sunDirection;
            _sunLocation.SunElevation = sunElevation;
            _sunLocation.DeviceTilt = deviceTilt;
            _sunLocation.ElevationMatched = elevationMatched;

            // Assert
            _sunLocation.SelectedDateTime.Should().Be(now);
            _sunLocation.Latitude.Should().Be(latitude);
            _sunLocation.Longitude.Should().Be(longitude);
            _sunLocation.NorthRotationAngle.Should().Be(northRotationAngle);
            _sunLocation.SunDirection.Should().Be(sunDirection);
            _sunLocation.SunElevation.Should().Be(sunElevation);
            _sunLocation.DeviceTilt.Should().Be(deviceTilt);
            _sunLocation.ElevationMatched.Should().Be(elevationMatched);
        }

        [Test]
        public void SunLocation_ShouldAllowLatitudeBetweenMinusAndPlus90()
        {
            // Arrange & Act
            Action setMinLatitude = () => _sunLocation.Latitude = -90.0;
            Action setMaxLatitude = () => _sunLocation.Latitude = 90.0;

            // Assert
            setMinLatitude.Should().NotThrow();
            setMaxLatitude.Should().NotThrow();
            _sunLocation.Latitude.Should().Be(90.0);
        }

        [Test]
        public void SunLocation_ShouldAllowLongitudeBetweenMinusAndPlus180()
        {
            // Arrange & Act
            Action setMinLongitude = () => _sunLocation.Longitude = -180.0;
            Action setMaxLongitude = () => _sunLocation.Longitude = 180.0;

            // Assert
            setMinLongitude.Should().NotThrow();
            setMaxLongitude.Should().NotThrow();
            _sunLocation.Longitude.Should().Be(180.0);
        }

        [Test]
        public void SunLocation_SunDirectionShouldBeAngleBetween0And360()
        {
            // Arrange & Act
            Action setMinDirection = () => _sunLocation.SunDirection = 0.0;
            Action setMaxDirection = () => _sunLocation.SunDirection = 359.99;

            // Assert
            setMinDirection.Should().NotThrow();
            setMaxDirection.Should().NotThrow();
            _sunLocation.SunDirection.Should().Be(359.99);
        }

        [Test]
        public void SunLocation_SunElevationShouldBeBetweenMinus90And90()
        {
            // Arrange & Act
            Action setMinElevation = () => _sunLocation.SunElevation = -90.0;
            Action setMaxElevation = () => _sunLocation.SunElevation = 90.0;

            // Assert
            setMinElevation.Should().NotThrow();
            setMaxElevation.Should().NotThrow();
            _sunLocation.SunElevation.Should().Be(90.0);
        }
    }
}