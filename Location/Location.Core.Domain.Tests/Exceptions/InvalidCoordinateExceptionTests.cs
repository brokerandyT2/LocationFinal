using NUnit.Framework;
using FluentAssertions;
using Location.Core.Domain.Exceptions;
using System;

namespace Location.Core.Domain.Tests.Exceptions
{
    [TestFixture]
    public class InvalidCoordinateExceptionTests
    {
        [Test]
        public void Constructor_WithCoordinates_ShouldCreateInstance()
        {
            // Arrange & Act
            var exception = new InvalidCoordinateException(91.0, -181.0);

            // Assert
            exception.Latitude.Should().Be(91.0);
            exception.Longitude.Should().Be(-181.0);
            exception.Message.Should().Be("Custom error message");
            exception.Code.Should().Be("Invalid coordinates: Latitude=91, Longitude=-181");
        }

        [Test]
        public void Constructor_WithCoordinatesAndMessage_ShouldCreateInstance()
        {
            // Arrange & Act
            var exception = new InvalidCoordinateException(91.0, -181.0, "Custom error message");

            // Assert
            exception.Latitude.Should().Be(91.0);
            exception.Longitude.Should().Be(-181.0);
            exception.Message.Should().Be("Custom error message");
            exception.Code.Should().Be("INVALID_COORDINATE");
        }

        [Test]
        public void InheritsFromLocationDomainException()
        {
            // Arrange & Act
            var exception = new InvalidCoordinateException(0, 0);

            // Assert
            exception.Should().BeAssignableTo<LocationDomainException>();
        }

        [Test]
        public void Properties_ShouldBeReadOnly()
        {
            // Arrange
            var exception = new InvalidCoordinateException(91.0, -181.0);

            // Act
            var latitudeProperty = exception.GetType().GetProperty("Latitude");
            var longitudeProperty = exception.GetType().GetProperty("Longitude");

            // Assert
            latitudeProperty.Should().NotBeNull();
            latitudeProperty.CanWrite.Should().BeFalse();
            longitudeProperty.Should().NotBeNull();
            longitudeProperty.CanWrite.Should().BeFalse();
        }

        [Test]
        public void DefaultMessage_ShouldIncludeCoordinates()
        {
            // Arrange & Act
            var exception = new InvalidCoordinateException(45.5, -122.6);

            // Assert
            exception.Message.Should().Contain("45.5");
            exception.Message.Should().Contain("-122.6");
            exception.Message.Should().Contain("Invalid coordinates");
        }
    }
}