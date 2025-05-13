using NUnit.Framework;
using FluentAssertions;
using Location.Core.Domain.ValueObjects;

namespace Location.Core.Domain.Tests.ValueObjects
{
    [TestFixture]
    public class AddressTests
    {
        [Test]
        public void Constructor_WithValidValues_ShouldCreateInstance()
        {
            // Arrange & Act
            var address = new Address("Seattle", "WA");

            // Assert
            address.City.Should().Be("Seattle");
            address.State.Should().Be("WA");
        }

        [Test]
        public void Constructor_WithNullValues_ShouldSetEmptyStrings()
        {
            // Arrange & Act
            var address = new Address(null, null);

            // Assert
            address.City.Should().Be(string.Empty);
            address.State.Should().Be(string.Empty);
        }

        [Test]
        public void ToString_WithBothValues_ShouldReturnFormattedString()
        {
            // Arrange
            var address = new Address("Seattle", "WA");

            // Act
            var result = address.ToString();

            // Assert
            result.Should().Be("Seattle, WA");
        }

        [Test]
        public void ToString_WithOnlyCity_ShouldReturnCity()
        {
            // Arrange
            var address = new Address("Seattle", "");

            // Act
            var result = address.ToString();

            // Assert
            result.Should().Be("Seattle");
        }

        [Test]
        public void ToString_WithOnlyState_ShouldReturnState()
        {
            // Arrange
            var address = new Address("", "WA");

            // Act
            var result = address.ToString();

            // Assert
            result.Should().Be("WA");
        }

        [Test]
        public void ToString_WithEmptyValues_ShouldReturnEmptyString()
        {
            // Arrange
            var address = new Address("", "");

            // Act
            var result = address.ToString();

            // Assert
            result.Should().Be(string.Empty);
        }

        [Test]
        public void Equals_WithSameValues_ShouldReturnTrue()
        {
            // Arrange
            var address1 = new Address("Seattle", "WA");
            var address2 = new Address("Seattle", "WA");

            // Act
            var result = address1.Equals(address2);

            // Assert
            result.Should().BeTrue();
        }

        [Test]
        public void Equals_WithDifferentCase_ShouldReturnTrue()
        {
            // Arrange
            var address1 = new Address("Seattle", "WA");
            var address2 = new Address("SEATTLE", "wa");

            // Act
            var result = address1.Equals(address2);

            // Assert
            result.Should().BeTrue();
        }

        [Test]
        public void Equals_WithDifferentValues_ShouldReturnFalse()
        {
            // Arrange
            var address1 = new Address("Seattle", "WA");
            var address2 = new Address("Portland", "OR");

            // Act
            var result = address1.Equals(address2);

            // Assert
            result.Should().BeFalse();
        }

        [Test]
        public void GetHashCode_WithSameValues_ShouldReturnSameHash()
        {
            // Arrange
            var address1 = new Address("Seattle", "WA");
            var address2 = new Address("SEATTLE", "wa");

            // Act
            var hash1 = address1.GetHashCode();
            var hash2 = address2.GetHashCode();

            // Assert
            hash1.Should().Be(hash2);
        }

        [Test]
        public void EqualsOperator_WithSameValues_ShouldReturnTrue()
        {
            // Arrange
            var address1 = new Address("Seattle", "WA");
            var address2 = new Address("Seattle", "WA");

            // Act
            var result = address1 == address2;

            // Assert
            result.Should().BeTrue();
        }

        [Test]
        public void NotEqualsOperator_WithDifferentValues_ShouldReturnTrue()
        {
            // Arrange
            var address1 = new Address("Seattle", "WA");
            var address2 = new Address("Portland", "OR");

            // Act
            var result = address1 != address2;

            // Assert
            result.Should().BeTrue();
        }
    }
}