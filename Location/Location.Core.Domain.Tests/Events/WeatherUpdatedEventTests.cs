using FluentAssertions;
using Location.Core.Domain.Events;
using NUnit.Framework;

namespace Location.Core.Domain.Tests.Events
{
    [TestFixture]
    public class WeatherUpdatedEventTests
    {
        [Test]
        public void Constructor_WithValidValues_ShouldCreateInstance()
        {
            // Arrange
            var locationId = 1;
            var updateTime = DateTime.UtcNow;

            // Act
            var eventItem = new WeatherUpdatedEvent(locationId, updateTime);

            // Assert
            eventItem.LocationId.Should().Be(locationId);
            eventItem.UpdateTime.Should().Be(updateTime);
            eventItem.DateOccurred.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Constructor_WithZeroLocationId_ShouldCreateInstance()
        {
            // Arrange
            var locationId = 0;
            var updateTime = DateTime.UtcNow;

            // Act
            var eventItem = new WeatherUpdatedEvent(locationId, updateTime);

            // Assert
            eventItem.LocationId.Should().Be(locationId);
        }

        [Test]
        public void Constructor_WithPastUpdateTime_ShouldCreateInstance()
        {
            // Arrange
            var locationId = 1;
            var updateTime = DateTime.UtcNow.AddDays(-1);

            // Act
            var eventItem = new WeatherUpdatedEvent(locationId, updateTime);

            // Assert
            eventItem.UpdateTime.Should().Be(updateTime);
        }
    }
}