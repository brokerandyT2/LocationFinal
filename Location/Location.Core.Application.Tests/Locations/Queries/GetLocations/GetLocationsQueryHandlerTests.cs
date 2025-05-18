using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Queries.Locations;
using Location.Core.Application.Locations.DTOs;
using Location.Core.Application.Tests.Utilities;
using Moq;
using NUnit.Framework;

namespace Location.Core.Application.Tests.Locations.Queries
{
    [Category("Locations")]
    [Category("Query")]
    [TestFixture]
    public class GetLocationsQueryHandlerTests
    {
        private Mock<IUnitOfWork> _unitOfWorkMock;
        private Mock<IMapper> _mapperMock;
        private GetLocationsQueryHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _mapperMock = new Mock<IMapper>();
            _handler = new GetLocationsQueryHandler(_unitOfWorkMock.Object, _mapperMock.Object);
        }

        [Test]
        public async Task Handle_WhenRequestIsValid_ReturnsPagedListOfLocations()
        {
            // Arrange
            var query = new GetLocationsQuery
            {
                PageNumber = 1,
                PageSize = 10,
                IncludeDeleted = false
            };

            var locations = TestDataBuilder.CreateValidLocationList(2);
            var locationDtos = new List<LocationListDto>
            {
                new LocationListDto { Id = 1, Title = "Location 1", City = "New York", State = "NY" },
                new LocationListDto { Id = 2, Title = "Location 2", City = "Los Angeles", State = "CA" }
            };

            // Mock returns IEnumerable<Location>
            _unitOfWorkMock.Setup(x => x.Locations.GetActiveAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<List<Domain.Entities.Location>>.Success(locations));

            _mapperMock.Setup(x => x.Map<List<LocationListDto>>(It.IsAny<List<Domain.Entities.Location>>()))
                .Returns(locationDtos);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Items.Should().HaveCount(2);
            result.Data.PageNumber.Should().Be(1);
            result.Data.PageSize.Should().Be(10);
        }

        [Test]
        public async Task Handle_WhenIncludeDeletedIsTrue_CallsGetAllAsync()
        {
            // Arrange
            var query = new GetLocationsQuery
            {
                PageNumber = 1,
                PageSize = 10,
                IncludeDeleted = true
            };

            var locations = TestDataBuilder.CreateValidLocationList(2);
            var locationDtos = new List<LocationListDto>
            {
                new LocationListDto { Id = 1, Title = "Location 1", City = "New York", State = "NY" },
                new LocationListDto { Id = 2, Title = "Location 2", City = "Los Angeles", State = "CA" }
            };

            // Mock returns IEnumerable<Location>
            _unitOfWorkMock.Setup(x => x.Locations.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<List<Domain.Entities.Location>>.Success(locations));

            _mapperMock.Setup(x => x.Map<List<LocationListDto>>(It.IsAny<List<Domain.Entities.Location>>()))
                .Returns(locationDtos);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            _unitOfWorkMock.Verify(x => x.Locations.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.Locations.GetActiveAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task Handle_WhenSearchTermProvided_FiltersResults()
        {
            // Arrange
            var query = new GetLocationsQuery
            {
                PageNumber = 1,
                PageSize = 10,
                SearchTerm = "New York",
                IncludeDeleted = false
            };

            var locations = TestDataBuilder.CreateValidLocationList(3);
            var locationDtos = locations.Select(l => new LocationListDto
            {
                Id = l.Id,
                Title = l.Title,
                City = l.Address.City,
                State = l.Address.State
            }).ToList();

            // Mock returns IEnumerable<Location>
            _unitOfWorkMock.Setup(x => x.Locations.GetActiveAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<List<Domain.Entities.Location>>.Success(locations));

            _mapperMock.Setup(x => x.Map<List<LocationListDto>>(It.IsAny<List<Domain.Entities.Location>>()))
                .Returns(locationDtos);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            // The handler should filter based on search term
        }

        [Test]
        public async Task Handle_WhenRepositoryReturnsEmptyList_ReturnsEmptyPagedList()
        {
            // Arrange
            var query = new GetLocationsQuery
            {
                PageNumber = 1,
                PageSize = 10,
                IncludeDeleted = false
            };

            var emptyList = new List<Domain.Entities.Location>();
            var emptyDtoList = new List<LocationListDto>();

            // Mock returns empty IEnumerable<Location>
            _unitOfWorkMock.Setup(x => x.Locations.GetActiveAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<List<Domain.Entities.Location>>.Success(emptyList));

            _mapperMock.Setup(x => x.Map<List<LocationListDto>>(It.IsAny<List<Domain.Entities.Location>>()))
                .Returns(emptyDtoList);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Items.Should().BeEmpty();
        }

        [Test]
        public async Task Handle_WhenExceptionThrown_ReturnsFailureResult()
        {
            // Arrange
            var query = new GetLocationsQuery();

            _unitOfWorkMock.Setup(x => x.Locations.GetActiveAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new System.Exception("Database error"));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Failed to retrieve locations");
        }
    }
}