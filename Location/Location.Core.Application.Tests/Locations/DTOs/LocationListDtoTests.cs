using NUnit.Framework;
using FluentAssertions;
using Location.Core.Application.Locations.DTOs;
using System;

namespace Location.Core.Application.Tests.Locations.DTOs
{
    [TestFixture]
    public class LocationListDtoTests
    {
        [Test]
        public void Constructor_WithDefaultValues_ShouldInitializeProperties()
        {
            // Act
            var dto = new LocationListDto();

            // Assert
            dto.Id.Should().Be(0);
            dto.Title.Should().BeEmpty();
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
            var dto = new LocationListDto();
            var timestamp = DateTime.UtcNow;

            // Act
            dto.Id = 42;
            dto.Title = "Space Needle";
            dto.City = "Seattle";
            dto.State = "WA";
            dto.PhotoPath = "/photos/space-needle.jpg";
            dto.Timestamp = timestamp;
            dto.IsDeleted = true;

            // Assert
            dto.Id.Should().Be(42);
            dto.Title.Should().Be("Space Needle");
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
            var dto = new LocationListDto
            {
                Id = 123,
                Title = "Test Location",
                City = "Portland",
                State = "OR",
                PhotoPath = "/test/photo.jpg",
                Timestamp = timestamp,
                IsDeleted = false
            };

            // Assert
            dto.Id.Should().Be(123);
            dto.Title.Should().Be("Test Location");
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
            var dto = new LocationListDto { PhotoPath = null };

            // Assert
            dto.PhotoPath.Should().BeNull();
        }

        [Test]
        public void Timestamp_ShouldPreserveMilliseconds()
        {
            // Arrange
            var precisetime = new DateTime(2024, 1, 15, 10, 30, 45, 123);

            // Act
            var dto = new LocationListDto { Timestamp = precisetime };

            // Assert
            dto.Timestamp.Should().Be(precisetime);
            dto.Timestamp.Millisecond.Should().Be(123);
        }

        [Test]
        public void IsDeleted_DefaultValue_ShouldBeFalse()
        {
            // Act
            var dto = new LocationListDto();

            // Assert
            dto.IsDeleted.Should().BeFalse();
        }

        [Test]
        public void Comparison_WithLocationDto_ShouldHaveSubsetOfProperties()
        {
            // Arrange
            var listDto = new LocationListDto
            {
                Id = 1,
                Title = "Test",
                City = "Seattle",
                State = "WA",
                PhotoPath = "/photo.jpg",
                IsDeleted = false
            };

            var fullDto = new LocationDto
            {
                Id = 1,
                Title = "Test",
                Description = "Test Description",
                Latitude = 47.6062,
                Longitude = -122.3321,
                City = "Seattle",
                State = "WA",
                PhotoPath = "/photo.jpg",
                IsDeleted = false
            };

            // Assert - LocationListDto should have fewer properties
            listDto.GetType().GetProperties().Length.Should().BeLessThan(
                fullDto.GetType().GetProperties().Length);
        }
    }
}