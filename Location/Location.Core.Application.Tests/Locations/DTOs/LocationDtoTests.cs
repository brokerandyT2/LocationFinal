using NUnit.Framework;
using FluentAssertions;
using Location.Core.Application.Locations.DTOs;
using System;

namespace Location.Core.Application.Tests.Locations.DTOs
{
    [TestFixture]
    public class LocationDtoTests
    {
        [Test]
        public void Constructor_WithDefaultValues_ShouldInitializeProperties()
        {
            // Act
            var dto = new LocationDto();

            // Assert
            dto.Id.Should().Be(0);
            dto.Title.Should().BeEmpty();
            dto.Description.Should().BeEmpty();
            dto.Latitude.Should().Be(0);
            dto.Longitude.Should().Be(0);
            dto.City.Should().BeEmpty();
            dto.State.Should().BeEmpty();
            dto.PhotoPath.Should().BeNull();
            dto.Timestamp.Should().Be(default(DateTime));
            dto.IsDeleted.Should().BeFalse();
        }

        [Test]
        public void Properties_WhenSet_ShouldRetainValues()
        {
            // Arrange
            var dto = new LocationDto();
            var timestamp = DateTime.UtcNow;

            // Act
            dto.Id = 42;
            dto.Title = "Space Needle";
            dto.Description = "Iconic Seattle landmark";
            dto.Latitude = 47.6205;
            dto.Longitude = -122.3493;
            dto.City = "Seattle";
            dto.State = "WA";
            dto.PhotoPath = "/photos/space-needle.jpg";
            dto.Timestamp = timestamp;
            dto.IsDeleted = true;

            // Assert
            dto.Id.Should().Be(42);
            dto.Title.Should().Be("Space Needle");
            dto.Description.Should().Be("Iconic Seattle landmark");
            dto.Latitude.Should().Be(47.6205);
            dto.Longitude.Should().Be(-122.3493);
            dto.City.Should().Be("Seattle");
            dto.State.Should().Be("WA");
            dto.PhotoPath.Should().Be("/photos/space-needle.jpg");
            dto.Timestamp.Should().Be(timestamp);
            dto.IsDeleted.Should().BeTrue();
        }

        [Test]
        public void ObjectInitializer_ShouldSetAllProperties()
        {
            // Arrange
            var timestamp = DateTime.UtcNow;

            // Act
            var dto = new LocationDto
            {
                Id = 123,
                Title = "Test Location",
                Description = "Test Description",
                Latitude = 45.5,
                Longitude = -122.5,
                City = "Portland",
                State = "OR",
                PhotoPath = "/test/photo.jpg",
                Timestamp = timestamp,
                IsDeleted = false
            };

            // Assert
            dto.Id.Should().Be(123);
            dto.Title.Should().Be("Test Location");
            dto.Description.Should().Be("Test Description");
            dto.Latitude.Should().Be(45.5);
            dto.Longitude.Should().Be(-122.5);
            dto.City.Should().Be("Portland");
            dto.State.Should().Be("OR");
            dto.PhotoPath.Should().Be("/test/photo.jpg");
            dto.Timestamp.Should().Be(timestamp);
            dto.IsDeleted.Should().BeFalse();
        }

        [Test]
        public void NullablePhotoPath_ShouldAcceptNull()
        {
            // Arrange & Act
            var dto = new LocationDto { PhotoPath = null };

            // Assert
            dto.PhotoPath.Should().BeNull();
        }

        [Test]
        public void Timestamp_ShouldPreserveMilliseconds()
        {
            // Arrange
            var precisetime = new DateTime(2024, 1, 15, 10, 30, 45, 123);

            // Act
            var dto = new LocationDto { Timestamp = precisetime };

            // Assert
            dto.Timestamp.Should().Be(precisetime);
            dto.Timestamp.Millisecond.Should().Be(123);
        }
    }
}