using FluentAssertions;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Locations.DTOs;
using Location.Core.Application.Locations.Queries.GetLocationById;
using MediatR;
using NUnit.Framework;

namespace Location.Core.Application.Tests.Locations.Queries.GetLocationById
{
    [Category("Locations")]
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
        public void ObjectInitializer_ShouldSetProperties()
        {
            // Act
            var query = new GetLocationByIdQuery
            {
                Id = 123
            };

            // Assert
            query.Id.Should().Be(123);
        }

        [Test]
        public void Create_WithValidId_ShouldHaveCorrectValue()
        {
            // Act
            var query = new GetLocationByIdQuery { Id = 999 };

            // Assert
            query.Id.Should().Be(999);
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

        [Test]
        public void Create_WithMaxIntId_ShouldBeAllowed()
        {
            // Act
            var query = new GetLocationByIdQuery { Id = int.MaxValue };

            // Assert
            query.Id.Should().Be(int.MaxValue);
        }
    }
}