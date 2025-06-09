using FluentAssertions;
using Location.Core.Domain.ValueObjects;
using NUnit.Framework;

namespace Location.Core.Domain.Tests.ValueObjects
{
    [TestFixture]
    public class WindInfoTests
    {
        [Test]
        public void Constructor_WithValidValues_ShouldCreateInstance()
        {
            // Arrange & Act
            var windInfo = new WindInfo(10.5, 180, 15.5);

            // Assert
            windInfo.Speed.Should().Be(10.5);
            windInfo.Direction.Should().Be(180);
            windInfo.Gust.Should().Be(15.5);
        }

        [Test]
        public void Constructor_WithoutGust_ShouldCreateInstance()
        {
            // Arrange & Act
            var windInfo = new WindInfo(10.5, 180);

            // Assert
            windInfo.Speed.Should().Be(10.5);
            windInfo.Direction.Should().Be(180);
            windInfo.Gust.Should().BeNull();
        }

        [Test]
        public void Constructor_ShouldRoundValues()
        {
            // Arrange & Act
            var windInfo = new WindInfo(10.556789, 180.789, 15.123);

            // Assert
            windInfo.Speed.Should().Be(10.56);
            windInfo.Direction.Should().Be(181);
            windInfo.Gust.Should().Be(15.12);
        }

        [Test]
        public void Constructor_WithNegativeSpeed_ShouldThrowException()
        {
            // Arrange & Act
            Action act = () => new WindInfo(-1, 180);

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>()
                .WithParameterName("speed");
        }

        [TestCase(-1)]
        [TestCase(361)]
        public void Constructor_WithInvalidDirection_ShouldThrowException(double direction)
        {
            // Arrange & Act
            Action act = () => new WindInfo(10, direction);

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>()
                .WithParameterName("direction");
        }

        [TestCase(0, "N")]
        [TestCase(11, "N")]
        [TestCase(12, "NNE")]
        [TestCase(45, "NE")]
        [TestCase(90, "E")]
        [TestCase(135, "SE")]
        [TestCase(180, "S")]
        [TestCase(225, "SW")]
        [TestCase(270, "W")]
        [TestCase(315, "NW")]
        [TestCase(360, "N")]
        public void GetCardinalDirection_ShouldReturnCorrectDirection(double direction, string expected)
        {
            // Arrange
            var windInfo = new WindInfo(10, direction);

            // Act
            var result = windInfo.GetCardinalDirection();

            // Assert
            result.Should().Be(expected);
        }

        [Test]
        public void ToString_WithGust_ShouldReturnFormattedString()
        {
            // Arrange
            var windInfo = new WindInfo(10.5, 180, 15.5);

            // Act
            var result = windInfo.ToString();

            // Assert
            result.Should().Be("10.5 mph from S (180°), Gust: 15.5");
        }

        [Test]
        public void ToString_WithoutGust_ShouldReturnFormattedString()
        {
            // Arrange
            var windInfo = new WindInfo(10.5, 180);

            // Act
            var result = windInfo.ToString();

            // Assert
            result.Should().Be("10.5 mph from S (180°)");
        }

        [Test]
        public void Equals_WithSameValues_ShouldReturnTrue()
        {
            // Arrange
            var windInfo1 = new WindInfo(10.5, 180, 15.5);
            var windInfo2 = new WindInfo(10.5, 180, 15.5);

            // Act
            var result = windInfo1.Equals(windInfo2);

            // Assert
            result.Should().BeTrue();
        }

        [Test]
        public void Equals_WithDifferentValues_ShouldReturnFalse()
        {
            // Arrange
            var windInfo1 = new WindInfo(10.5, 180, 15.5);
            var windInfo2 = new WindInfo(10.5, 181, 15.5);

            // Act
            var result = windInfo1.Equals(windInfo2);

            // Assert
            result.Should().BeFalse();
        }

        [Test]
        public void Equals_WithNullGust_ShouldHandleCorrectly()
        {
            // Arrange
            var windInfo1 = new WindInfo(10.5, 180, 15.5);
            var windInfo2 = new WindInfo(10.5, 180);

            // Act
            var result = windInfo1.Equals(windInfo2);

            // Assert
            result.Should().BeFalse();
        }

        [Test]
        public void GetHashCode_WithSameValues_ShouldReturnSameHash()
        {
            // Arrange
            var windInfo1 = new WindInfo(10.5, 180, 15.5);
            var windInfo2 = new WindInfo(10.5, 180, 15.5);

            // Act
            var hash1 = windInfo1.GetHashCode();
            var hash2 = windInfo2.GetHashCode();

            // Assert
            hash1.Should().Be(hash2);
        }

        [Test]
        public void EqualsOperator_WithSameValues_ShouldReturnTrue()
        {
            // Arrange
            var windInfo1 = new WindInfo(10.5, 180, 15.5);
            var windInfo2 = new WindInfo(10.5, 180, 15.5);

            // Act
            var result = windInfo1 == windInfo2;

            // Assert
            result.Should().BeTrue();
        }

        [Test]
        public void NotEqualsOperator_WithDifferentValues_ShouldReturnTrue()
        {
            // Arrange
            var windInfo1 = new WindInfo(10.5, 180, 15.5);
            var windInfo2 = new WindInfo(10.5, 181, 15.5);

            // Act
            var result = windInfo1 != windInfo2;

            // Assert
            result.Should().BeTrue();
        }
    }
}