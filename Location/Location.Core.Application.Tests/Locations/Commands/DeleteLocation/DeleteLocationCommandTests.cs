using NUnit.Framework;
using FluentAssertions;
using Location.Core.Application.Common.Models;
using MediatR;

namespace Location.Core.Application.Tests.Locations.Commands.DeleteLocation
{
    [TestFixture]
    public class DeleteLocationCommandTests
    {
        [Test]
        public void Constructor_WithDefaultValues_ShouldInitializeProperties()
        {
            // Act
            var command = new DeleteLocationCommand();

            // Assert
            command.Id.Should().Be(0);
        }

        [Test]
        public void Properties_WhenSet_ShouldRetainValues()
        {
            // Arrange
            var command = new DeleteLocationCommand();

            // Act
            command.Id = 42;

            // Assert
            command.Id.Should().Be(42);
        }

        [Test]
        public void Command_ShouldImplementIRequest()
        {
            // Arrange & Act
            var command = new DeleteLocationCommand();

            // Assert
            command.Should().BeAssignableTo<IRequest<Result>>();
        }

        [Test]
        public void Create_WithId_ShouldSetProperty()
        {
            // Act
            var command = new DeleteLocationCommand { Id = 123 };

            // Assert
            command.Id.Should().Be(123);
        }

        [Test]
        public void Create_ForExistingLocation_ShouldHaveValidId()
        {
            // Act
            var command = new DeleteLocationCommand { Id = 1 };

            // Assert
            command.Id.Should().BeGreaterThan(0);
        }
    }

    // This is a placeholder for the actual implementation
    public class DeleteLocationCommand : IRequest<Result>
    {
        public int Id { get; set; }
    }
}