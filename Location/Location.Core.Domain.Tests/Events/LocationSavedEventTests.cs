using FluentAssertions;
using Location.Core.Domain.Events;
using Location.Core.Domain.ValueObjects;
using NUnit.Framework;

namespace Location.Core.Domain.Tests.Events
{
    [TestFixture]
    public class LocationSavedEventTests
    {
        [Test]
        public void Constructor_WithValidLocation_ShouldCreateInstance()
        {
            // Arrange
            var coordinate = new Coordinate(47.6062, -122.3321);
            var address = new Address("Seattle", "WA");
            var location = new Location.Core.Domain.Entities.Location("Space Needle", "Landmark", coordinate, address);

            // Act
            var eventItem = new LocationSavedEvent(location);

            // Assert
            eventItem.Location.Should().Be(location);
            eventItem.DateOccurred.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Constructor_WithNullLocation_ShouldThrowException()
        {
            // Arrange & Act
            Action act = () => new LocationSavedEvent(null);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("location");
        }
    }
}