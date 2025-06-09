using FluentAssertions;
using Location.Core.Application.Commands.Locations;
using Location.Core.Application.Common.Models;
using MediatR;
using NUnit.Framework;

namespace Location.Core.Application.Tests.Locations.Commands.DeleteLocation
{
    [Category("Locations")]
    [Category("Delete Location")]
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
            command.Should().BeAssignableTo<IRequest<Result<bool>>>();
        }

        [Test]
        public void ObjectInitializer_ShouldSetProperties()
        {
            // Act
            var command = new DeleteLocationCommand
            {
                Id = 123
            };

            // Assert
            command.Id.Should().Be(123);
        }

        [Test]
        public void Create_WithValidId_ShouldHaveCorrectValue()
        {
            // Act
            var command = new DeleteLocationCommand { Id = 999 };

            // Assert
            command.Id.Should().Be(999);
        }

        [Test]
        public void Create_WithZeroId_ShouldBeAllowed()
        {
            // Act
            var command = new DeleteLocationCommand { Id = 0 };

            // Assert
            command.Id.Should().Be(0);
        }

        [Test]
        public void Create_WithNegativeId_ShouldBeAllowed()
        {
            // Act
            var command = new DeleteLocationCommand { Id = -1 };

            // Assert
            command.Id.Should().Be(-1);
        }

        [Test]
        public void Create_WithMaxIntId_ShouldBeAllowed()
        {
            // Act
            var command = new DeleteLocationCommand { Id = int.MaxValue };

            // Assert
            command.Id.Should().Be(int.MaxValue);
        }
    }
}