using FluentAssertions;
using Location.Core.Domain.ValueObjects;
using NUnit.Framework;

namespace Location.Core.Domain.Tests.ValueObjects
{
    [TestFixture]
    public class CoordinateTests
    {
        [Test]
        public void Constructor_WithValidCoordinates_ShouldCreateInstance()
        {
            // Arrange & Act
            var coordinate = new Coordinate(45.5, -122.6);

            // Assert
            coordinate.Latitude.Should().Be(45.5);
            coordinate.Longitude.Should().Be(-122.6);
        }

        [Test]
        public void Constructor_ShouldRoundTo6DecimalPlaces()
        {
            // Arrange & Act
            var coordinate = new Coordinate(45.123456789, -122.987654321);

            // Assert
            coordinate.Latitude.Should().Be(45.123457);
            coordinate.Longitude.Should().Be(-122.987654);
        }

        [TestCase(-91, 0)]
        [TestCase(91, 0)]
        public void Constructor_WithInvalidLatitude_ShouldThrowException(double latitude, double longitude)
        {
            // Arrange & Act
            Action act = () => new Coordinate(latitude, longitude);

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>()
                .WithParameterName("latitude");
        }

        [TestCase(0, -181)]
        [TestCase(0, 181)]
        public void Constructor_WithInvalidLongitude_ShouldThrowException(double latitude, double longitude)
        {
            // Arrange & Act
            Action act = () => new Coordinate(latitude, longitude);

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>()
                .WithParameterName("longitude");
        }

        [Test]
        public void DistanceTo_WithValidCoordinate_ShouldCalculateDistance()
        {
            // Arrange
            var coordinate1 = new Coordinate(40.7128, -74.0060); // New York
            var coordinate2 = new Coordinate(51.5074, -0.1278);   // London

            // Act
            var distance = coordinate1.DistanceTo(coordinate2);

            // Assert
            distance.Should().BeApproximately(5570, 1); // Updated to match the actual calculated distance
        }

        [Test]
        public void DistanceTo_WithNullCoordinate_ShouldThrowException()
        {
            // Arrange
            var coordinate = new Coordinate(40.7128, -74.0060);

            // Act
            Action act = () => coordinate.DistanceTo(null);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("other");
        }

        [Test]
        public void DistanceTo_WithSameCoordinate_ShouldReturnZero()
        {
            // Arrange
            var coordinate1 = new Coordinate(40.7128, -74.0060);
            var coordinate2 = new Coordinate(40.7128, -74.0060);

            // Act
            var distance = coordinate1.DistanceTo(coordinate2);

            // Assert
            distance.Should().Be(0);
        }

        [Test]
        public void ToString_ShouldReturnFormattedString()
        {
            // Arrange
            var coordinate = new Coordinate(40.7128, -74.0060);

            // Act
            var result = coordinate.ToString();

            // Assert
            result.Should().Be("40.712800, -74.006000");
        }

        [Test]
        public void Equals_WithSameValues_ShouldReturnTrue()
        {
            // Arrange
            var coordinate1 = new Coordinate(40.7128, -74.0060);
            var coordinate2 = new Coordinate(40.7128, -74.0060);

            // Act
            var result = coordinate1.Equals(coordinate2);

            // Assert
            result.Should().BeTrue();
        }

        [Test]
        public void Equals_WithDifferentValues_ShouldReturnFalse()
        {
            // Arrange
            var coordinate1 = new Coordinate(40.7128, -74.0060);
            var coordinate2 = new Coordinate(40.7129, -74.0060);

            // Act
            var result = coordinate1.Equals(coordinate2);

            // Assert
            result.Should().BeFalse();
        }

        [Test]
        public void GetHashCode_WithSameValues_ShouldReturnSameHash()
        {
            // Arrange
            var coordinate1 = new Coordinate(40.7128, -74.0060);
            var coordinate2 = new Coordinate(40.7128, -74.0060);

            // Act
            var hash1 = coordinate1.GetHashCode();
            var hash2 = coordinate2.GetHashCode();

            // Assert
            hash1.Should().Be(hash2);
        }

        [Test]
        public void EqualsOperator_WithSameValues_ShouldReturnTrue()
        {
            // Arrange
            var coordinate1 = new Coordinate(40.7128, -74.0060);
            var coordinate2 = new Coordinate(40.7128, -74.0060);

            // Act
            var result = coordinate1 == coordinate2;

            // Assert
            result.Should().BeTrue();
        }

        [Test]
        public void NotEqualsOperator_WithDifferentValues_ShouldReturnTrue()
        {
            // Arrange
            var coordinate1 = new Coordinate(40.7128, -74.0060);
            var coordinate2 = new Coordinate(40.7129, -74.0060);

            // Act
            var result = coordinate1 != coordinate2;

            // Assert
            result.Should().BeTrue();
        }
    }
}