using NUnit.Framework;
using FluentAssertions;
using Location.Core.Application.Common.Models;
using System.Collections.Generic;
using System.Linq;

namespace Location.Core.Application.Tests.Common.Models
{
    [Category("View Model Support Class")]
    [Category("Paging")]
    [TestFixture]
    public class PagedListTests
    {
        [Test]
        public void Constructor_WithValidParameters_ShouldCreateInstance()
        {
            // Arrange
            var items = new List<string> { "Item1", "Item2", "Item3" };
            var totalCount = 10;
            var pageNumber = 2;
            var pageSize = 3;

            // Act
            var pagedList = new PagedList<string>(items, totalCount, pageNumber, pageSize);

            // Assert
            pagedList.Items.Should().HaveCount(3);
            pagedList.Items.Should().ContainInOrder("Item1", "Item2", "Item3");
            pagedList.TotalCount.Should().Be(10);
            pagedList.PageNumber.Should().Be(2);
            pagedList.PageSize.Should().Be(3);
            pagedList.TotalPages.Should().Be(4);
        }

        [Test]
        public void HasPreviousPage_WhenOnFirstPage_ShouldReturnFalse()
        {
            // Arrange
            var items = new List<int> { 1, 2, 3 };
            var pagedList = new PagedList<int>(items, 10, 1, 3);

            // Act & Assert
            pagedList.HasPreviousPage.Should().BeFalse();
        }

        [Test]
        public void HasPreviousPage_WhenNotOnFirstPage_ShouldReturnTrue()
        {
            // Arrange
            var items = new List<int> { 4, 5, 6 };
            var pagedList = new PagedList<int>(items, 10, 2, 3);

            // Act & Assert
            pagedList.HasPreviousPage.Should().BeTrue();
        }

        [Test]
        public void HasNextPage_WhenOnLastPage_ShouldReturnFalse()
        {
            // Arrange
            var items = new List<int> { 10 };
            var pagedList = new PagedList<int>(items, 10, 4, 3);

            // Act & Assert
            pagedList.HasNextPage.Should().BeFalse();
        }

        [Test]
        public void HasNextPage_WhenNotOnLastPage_ShouldReturnTrue()
        {
            // Arrange
            var items = new List<int> { 1, 2, 3 };
            var pagedList = new PagedList<int>(items, 10, 1, 3);

            // Act & Assert
            pagedList.HasNextPage.Should().BeTrue();
        }

        [Test]
        public void Create_WithQueryable_ShouldPaginateCorrectly()
        {
            // Arrange
            var source = Enumerable.Range(1, 20).AsQueryable();
            var pageNumber = 2;
            var pageSize = 5;

            // Act
            var pagedList = PagedList<int>.Create(source, pageNumber, pageSize);

            // Assert
            pagedList.Items.Should().HaveCount(5);
            pagedList.Items.Should().ContainInOrder(6, 7, 8, 9, 10);
            pagedList.TotalCount.Should().Be(20);
            pagedList.PageNumber.Should().Be(2);
            pagedList.PageSize.Should().Be(5);
            pagedList.TotalPages.Should().Be(4);
        }

        [Test]
        public void Create_WithList_ShouldPaginateCorrectly()
        {
            // Arrange
            var source = Enumerable.Range(1, 15).ToList();
            var pageNumber = 3;
            var pageSize = 4;

            // Act
            var pagedList = PagedList<int>.Create(source, pageNumber, pageSize);

            // Assert
            pagedList.Items.Should().HaveCount(4);
            pagedList.Items.Should().ContainInOrder(9, 10, 11, 12);
            pagedList.TotalCount.Should().Be(15);
            pagedList.PageNumber.Should().Be(3);
            pagedList.PageSize.Should().Be(4);
            pagedList.TotalPages.Should().Be(4);
        }

        [Test]
        public void Create_WithLastPagePartiallyFilled_ShouldReturnCorrectItems()
        {
            // Arrange
            var source = Enumerable.Range(1, 13).ToList();
            var pageNumber = 3;
            var pageSize = 5;

            // Act
            var pagedList = PagedList<int>.Create(source, pageNumber, pageSize);

            // Assert
            pagedList.Items.Should().HaveCount(3);
            pagedList.Items.Should().ContainInOrder(11, 12, 13);
            pagedList.HasNextPage.Should().BeFalse();
        }

        [Test]
        public void Items_ShouldBeReadOnly()
        {
            // Arrange
            var items = new List<string> { "Item1", "Item2" };
            var pagedList = new PagedList<string>(items, 2, 1, 2);

            // Act & Assert
            pagedList.Items.Should().BeAssignableTo<IReadOnlyList<string>>();
        }

        [Test]
        public void TotalPages_WithExactDivision_ShouldCalculateCorrectly()
        {
            // Arrange
            var items = new List<int> { 1, 2, 3 };
            var pagedList = new PagedList<int>(items, 9, 1, 3);

            // Act & Assert
            pagedList.TotalPages.Should().Be(3);
        }

        [Test]
        public void TotalPages_WithRemainder_ShouldRoundUp()
        {
            // Arrange
            var items = new List<int> { 1, 2, 3 };
            var pagedList = new PagedList<int>(items, 10, 1, 3);

            // Act & Assert
            pagedList.TotalPages.Should().Be(4);
        }
    }
}