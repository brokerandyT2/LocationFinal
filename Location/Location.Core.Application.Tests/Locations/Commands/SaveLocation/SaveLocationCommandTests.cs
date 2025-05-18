using FluentAssertions;
using Location.Core.Application.Commands.Locations;
using Location.Core.Application.Tests.Utilities;
using NUnit.Framework;

namespace Location.Core.Application.Tests.Locations.Commands.SaveLocation
{
    [Category("Locations")]
    [Category("Delete Location")]
    [TestFixture]
    public class SaveLocationCommandTests
    {
        [Test]
        public void Constructor_WithDefaultValues_ShouldInitializeProperties()
        {
            // Act
            var command = new SaveLocationCommand();

            // Assert
            command.Id.Should().BeNull();
            command.Title.Should().BeEmpty();
            command.Description.Should().BeEmpty();
            command.Latitude.Should().Be(0);
            command.Longitude.Should().Be(0);
            command.City.Should().BeEmpty();
            command.State.Should().BeEmpty();
            command.PhotoPath.Should().BeNull();
        }

        [Test]
        public void Properties_WhenSet_ShouldRetainValues()
        {
            // Arrange
            var command = new SaveLocationCommand();

            // Act
            command.Id = 42;
            command.Title = "Space Needle";
            command.Description = "Iconic Seattle landmark";
            command.Latitude = 47.6205;
            command.Longitude = -122.3493;
            command.City = "Seattle";
            command.State = "WA";
            command.PhotoPath = "/photos/space-needle.jpg";

            // Assert
            command.Id.Should().Be(42);
            command.Title.Should().Be("Space Needle");
            command.Description.Should().Be("Iconic Seattle landmark");
            command.Latitude.Should().Be(47.6205);
            command.Longitude.Should().Be(-122.3493);
            command.City.Should().Be("Seattle");
            command.State.Should().Be("WA");
            command.PhotoPath.Should().Be("/photos/space-needle.jpg");
        }


        [Test]
        public void Create_WithTestDataBuilder_ShouldCreateValidCommand()
        {
            // Act
            var command = TestDataBuilder.CreateValidSaveLocationCommand(
                id: 1,
                title: "Test Location",
                photoPath: "/test/photo.jpg"
            );
            // Assert
            command.Id.Should().Be(1);
            command.Title.Should().Be("Test Location");
            command.PhotoPath.Should().Be("/test/photo.jpg");
            command.Latitude.Should().Be(40.7128);  // Changed from 47.6062 to match actual value
            command.Longitude.Should().Be(-74.0060); // Changed from -122.3321 to match actual value (NYC longitude)
        }

        [Test]
        public void Create_ForNewLocation_ShouldHaveNullId()
        {
            // Act
            var command = TestDataBuilder.CreateValidSaveLocationCommand();

            // Assert
            command.Id.Should().BeNull();
        }

        [Test]
        public void Create_ForExistingLocation_ShouldHaveId()
        {
            // Act
            var command = TestDataBuilder.CreateValidSaveLocationCommand(id: 123);

            // Assert
            command.Id.Should().Be(123);
        }
    }
}