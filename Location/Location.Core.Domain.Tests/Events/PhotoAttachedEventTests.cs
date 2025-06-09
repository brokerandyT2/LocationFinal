using FluentAssertions;
using Location.Core.Domain.Events;
using NUnit.Framework;

namespace Location.Core.Domain.Tests.Events
{
    [TestFixture]
    public class PhotoAttachedEventTests
    {
        [Test]
        public void Constructor_WithValidValues_ShouldCreateInstance()
        {
            // Arrange
            var locationId = 42;
            var photoPath = "/path/to/photo.jpg";

            // Act
            var eventItem = new PhotoAttachedEvent(locationId, photoPath);

            // Assert
            eventItem.LocationId.Should().Be(locationId);
            eventItem.PhotoPath.Should().Be(photoPath);
            eventItem.DateOccurred.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Constructor_WithNullPhotoPath_ShouldThrowException()
        {
            // Arrange
            var locationId = 42;

            // Act
            Action act = () => new PhotoAttachedEvent(locationId, null);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("photoPath");
        }

        [Test]
        public void Constructor_WithEmptyPhotoPath_ShouldCreateInstance()
        {
            // Arrange
            var locationId = 42;
            var photoPath = "";

            // Act
            var eventItem = new PhotoAttachedEvent(locationId, photoPath);

            // Assert
            eventItem.PhotoPath.Should().BeEmpty();
        }

        [Test]
        public void Constructor_WithZeroLocationId_ShouldCreateInstance()
        {
            // Arrange
            var locationId = 0;
            var photoPath = "/path/to/photo.jpg";

            // Act
            var eventItem = new PhotoAttachedEvent(locationId, photoPath);

            // Assert
            eventItem.LocationId.Should().Be(locationId);
        }
    }
}