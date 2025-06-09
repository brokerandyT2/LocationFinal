using FluentAssertions;
using Location.Core.Domain.Events;
using NUnit.Framework;

namespace Location.Core.Domain.Tests.Events
{
    [TestFixture]
    public class LocationDeletedEventTests
    {
        [Test]
        public void Constructor_WithValidLocationId_ShouldCreateInstance()
        {
            // Arrange
            var locationId = 42;

            // Act
            var eventItem = new LocationDeletedEvent(locationId);

            // Assert
            eventItem.LocationId.Should().Be(locationId);
            eventItem.DateOccurred.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Constructor_WithZeroLocationId_ShouldCreateInstance()
        {
            // Arrange
            var locationId = 0;

            // Act
            var eventItem = new LocationDeletedEvent(locationId);

            // Assert
            eventItem.LocationId.Should().Be(locationId);
        }

        [Test]
        public void Constructor_WithNegativeLocationId_ShouldCreateInstance()
        {
            // Arrange
            var locationId = -1;

            // Act
            var eventItem = new LocationDeletedEvent(locationId);

            // Assert
            eventItem.LocationId.Should().Be(locationId);
        }
    }
}