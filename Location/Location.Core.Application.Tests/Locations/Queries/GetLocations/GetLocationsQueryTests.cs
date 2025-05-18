using NUnit.Framework;
using FluentAssertions;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Locations.DTOs;
using MediatR;

namespace Location.Core.Application.Tests.Locations.Queries.GetLocations
{
    [Category("Locations")]
    [Category("Query")]
    [TestFixture]
    public class GetLocationsQueryTests
    {
        [Test]
        public void Constructor_WithDefaultValues_ShouldInitializeProperties()
        {
            // Act
            var query = new GetLocationsQuery();

            // Assert
            query.PageNumber.Should().Be(1);
            query.PageSize.Should().Be(10);
            query.IncludeDeleted.Should().BeFalse();
        }

        [Test]
        public void Properties_WhenSet_ShouldRetainValues()
        {
            // Arrange
            var query = new GetLocationsQuery();

            // Act
            query.PageNumber = 5;
            query.PageSize = 20;
            query.IncludeDeleted = true;

            // Assert
            query.PageNumber.Should().Be(5);
            query.PageSize.Should().Be(20);
            query.IncludeDeleted.Should().BeTrue();
        }

        [Test]
        public void Query_ShouldImplementIRequest()
        {
            // Arrange & Act
            var query = new GetLocationsQuery();

            // Assert
            query.Should().BeAssignableTo<IRequest<Result<PagedList<LocationListDto>>>>();
        }

        [Test]
        public void Create_WithPagination_ShouldSetProperties()
        {
            // Act
            var query = new GetLocationsQuery
            {
                PageNumber = 2,
                PageSize = 25,
                IncludeDeleted = false
            };

            // Assert
            query.PageNumber.Should().Be(2);
            query.PageSize.Should().Be(25);
            query.IncludeDeleted.Should().BeFalse();
        }

        [Test]
        public void Create_ForFirstPage_ShouldHaveCorrectValues()
        {
            // Act
            var query = new GetLocationsQuery
            {
                PageNumber = 1,
                PageSize = 10
            };

            // Assert
            query.PageNumber.Should().Be(1);
            query.PageSize.Should().Be(10);
        }

        [Test]
        public void Create_WithIncludeDeleted_ShouldSetFlag()
        {
            // Act
            var query = new GetLocationsQuery
            {
                IncludeDeleted = true
            };

            // Assert
            query.IncludeDeleted.Should().BeTrue();
        }
    }

    // Placeholder for the actual implementation
    public class GetLocationsQuery : IRequest<Result<PagedList<LocationListDto>>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public bool IncludeDeleted { get; set; } = false;
    }
}