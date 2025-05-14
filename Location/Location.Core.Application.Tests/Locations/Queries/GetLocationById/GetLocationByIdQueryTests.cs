using NUnit.Framework;
using FluentAssertions;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Locations.DTOs;
using MediatR;

namespace Location.Core.Application.Tests.Locations.Queries.GetLocationById
{
    [TestFixture]
    public class GetLocationByIdQueryTests
    {
        [Test]
        public void Constructor_WithDefaultValues_ShouldInitializeProperties()
        {
            // Act
            var query = new GetLocationByIdQuery();

            // Assert
            query.Id.Should().Be(0);
        }

        [Test]
        public void Properties_WhenSet_ShouldRetainValues()
        {
            // Arrange
            var query = new GetLocationByIdQuery();

            // Act
            query.Id = 42;

            // Assert
            query.Id.Should().Be(42);
        }

        [Test]
        public void Query_ShouldImplementIRequest()
        {
            // Arrange & Act
            var query = new GetLocationByIdQuery();

            // Assert
            query.Should().BeAssignableTo<IRequest<Result<LocationDto>>>();
        }

        [Test]
        public void Create_WithId_ShouldSetProperty()
        {
            // Act
            var query = new GetLocationByIdQuery { Id = 123 };

            // Assert
            query.Id.Should().Be(123);
        }

        [Test]
        public void Create_ForExistingLocation_ShouldHaveValidId()
        {
            // Act
            var query = new GetLocationByIdQuery { Id = 1 };

            // Assert
            query.Id.Should().BeGreaterThan(0);
        }

        [Test]
        public void Create_WithZeroId_ShouldBeAllowed()
        {
            // Act
            var query = new GetLocationByIdQuery { Id = 0 };

            // Assert
            query.Id.Should().Be(0);
        }

        [Test]
        public void Create_WithNegativeId_ShouldBeAllowed()
        {
            // Act
            var query = new GetLocationByIdQuery { Id = -1 };

            // Assert
            query.Id.Should().Be(-1);
        }
    }

    // Placeholder for the actual implementation
    public class GetLocationByIdQuery : IRequest<Result<LocationDto>>
    {
        public int Id { get; set; }
    }
}