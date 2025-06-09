using FluentAssertions;
using Location.Core.Domain.Rules;
using Location.Core.Domain.ValueObjects;
using NUnit.Framework;

namespace Location.Core.Domain.Tests.Rules
{
    [TestFixture]
    public class CoordinateValidationRulesTests
    {
        [Test]
        public void IsValid_WithValidCoordinates_ShouldReturnTrue()
        {
            // Arrange
            List<string> errors;

            // Act
            var result = CoordinateValidationRules.IsValid(45.0, -122.0, out errors);

            // Assert
            result.Should().BeTrue();
            errors.Should().BeEmpty();
        }

        [TestCase(-91)]
        [TestCase(91)]
        public void IsValid_WithInvalidLatitude_ShouldReturnFalse(double latitude)
        {
            // Arrange
            List<string> errors;

            // Act
            var result = CoordinateValidationRules.IsValid(latitude, 0, out errors);

            // Assert
            result.Should().BeFalse();
            errors.Should().ContainSingle();
            errors.Should().Contain($"Latitude {latitude} is out of valid range (-90 to 90)");
        }

        [TestCase(-181)]
        [TestCase(181)]
        public void IsValid_WithInvalidLongitude_ShouldReturnFalse(double longitude)
        {
            // Arrange
            List<string> errors;

            // Act
            var result = CoordinateValidationRules.IsValid(0, longitude, out errors);

            // Assert
            result.Should().BeFalse();
            errors.Should().ContainSingle();
            errors.Should().Contain($"Longitude {longitude} is out of valid range (-180 to 180)");
        }

        [Test]
        public void IsValid_WithNullIsland_ShouldReturnFalse()
        {
            // Arrange
            List<string> errors;

            // Act
            var result = CoordinateValidationRules.IsValid(0, 0, out errors);

            // Assert
            result.Should().BeFalse();
            errors.Should().ContainSingle();
            errors.Should().Contain("Null Island (0,0) is not a valid location");
        }

        [Test]
        public void IsValid_WithMultipleErrors_ShouldReturnAllErrors()
        {
            // Arrange
            List<string> errors;

            // Act
            var result = CoordinateValidationRules.IsValid(-91, 181, out errors);

            // Assert
            result.Should().BeFalse();
            errors.Should().HaveCount(2);
            errors.Should().Contain("Latitude -91 is out of valid range (-90 to 90)");
            errors.Should().Contain("Longitude 181 is out of valid range (-180 to 180)");
        }

        [TestCase(-90)]
        [TestCase(90)]
        public void IsValid_WithBoundaryLatitude_ShouldReturnTrue(double latitude)
        {
            // Arrange
            List<string> errors;

            // Act
            var result = CoordinateValidationRules.IsValid(latitude, 0, out errors);

            // Assert
            result.Should().BeTrue();
            errors.Should().BeEmpty();
        }

        [TestCase(-180)]
        [TestCase(180)]
        public void IsValid_WithBoundaryLongitude_ShouldReturnTrue(double longitude)
        {
            // Arrange
            List<string> errors;

            // Act
            var result = CoordinateValidationRules.IsValid(45, longitude, out errors);

            // Assert
            result.Should().BeTrue();
            errors.Should().BeEmpty();
        }

        [Test]
        public void IsValidDistance_WithValidCoordinatesWithinRange_ShouldReturnTrue()
        {
            // Arrange
            var coordinate1 = new Coordinate(47.6062, -122.3321); // Seattle
            var coordinate2 = new Coordinate(47.6088, -122.3359); // Nearby
            var maxDistanceKm = 10;

            // Act
            var result = CoordinateValidationRules.IsValidDistance(coordinate1, coordinate2, maxDistanceKm);

            // Assert
            result.Should().BeTrue();
        }

        [Test]
        public void IsValidDistance_WithValidCoordinatesOutsideRange_ShouldReturnFalse()
        {
            // Arrange
            var coordinate1 = new Coordinate(47.6062, -122.3321); // Seattle
            var coordinate2 = new Coordinate(45.5122, -122.6587); // Portland
            var maxDistanceKm = 10;

            // Act
            var result = CoordinateValidationRules.IsValidDistance(coordinate1, coordinate2, maxDistanceKm);

            // Assert
            result.Should().BeFalse();
        }

        [Test]
        public void IsValidDistance_WithNullFromCoordinate_ShouldReturnFalse()
        {
            // Arrange
            var coordinate = new Coordinate(47.6062, -122.3321);
            var maxDistanceKm = 10;

            // Act
            var result = CoordinateValidationRules.IsValidDistance(null, coordinate, maxDistanceKm);

            // Assert
            result.Should().BeFalse();
        }

        [Test]
        public void IsValidDistance_WithNullToCoordinate_ShouldReturnFalse()
        {
            // Arrange
            var coordinate = new Coordinate(47.6062, -122.3321);
            var maxDistanceKm = 10;

            // Act
            var result = CoordinateValidationRules.IsValidDistance(coordinate, null, maxDistanceKm);

            // Assert
            result.Should().BeFalse();
        }

        [Test]
        public void IsValidDistance_WithBothNullCoordinates_ShouldReturnFalse()
        {
            // Arrange
            var maxDistanceKm = 10;

            // Act
            var result = CoordinateValidationRules.IsValidDistance(null, null, maxDistanceKm);

            // Assert
            result.Should().BeFalse();
        }

        [Test]
        public void IsValidDistance_WithSameCoordinates_ShouldReturnTrue()
        {
            // Arrange
            var coordinate = new Coordinate(47.6062, -122.3321);
            var maxDistanceKm = 0;

            // Act
            var result = CoordinateValidationRules.IsValidDistance(coordinate, coordinate, maxDistanceKm);

            // Assert
            result.Should().BeTrue();
        }
    }
}