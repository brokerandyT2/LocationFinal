using AutoMapper;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Locations.DTOs;
using Location.Core.Application.Locations.Queries.GetLocationById;
using Location.Core.Domain.ValueObjects;
using MediatR;
using Moq;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Location.Core.Application.Tests.Locations.Queries.GetLocationById
{
    [Category("Locations")]
    [TestFixture]
    public class GetLocationByIdQueryHandlerTests
    {
        private Mock<IUnitOfWork> _unitOfWorkMock;
        private Mock<ILocationRepository> _locationRepositoryMock;
        private Mock<IMapper> _mapperMock;
        private Mock<IMediator> _mediatorMock;
        private GetLocationByIdQueryHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _locationRepositoryMock = new Mock<ILocationRepository>();
            _mapperMock = new Mock<IMapper>();
            _mediatorMock = new Mock<IMediator>();

            _unitOfWorkMock.Setup(u => u.Locations).Returns(_locationRepositoryMock.Object);

            _handler = new GetLocationByIdQueryHandler(_unitOfWorkMock.Object, _mapperMock.Object, _mediatorMock.Object);
        }

        [Test]
        public async Task Handle_ValidId_ReturnsSuccessResult()
        {
            // Arrange
            var locationId = 1;
            var location = new Location.Core.Domain.Entities.Location(
                "Test Location",
                "Test Description",
                new Coordinate(40.7128, -74.0060),
                new Address("New York", "NY"));

            var locationResult = Result<Location.Core.Domain.Entities.Location>.Success(location);

            _locationRepositoryMock
                .Setup(x => x.GetByIdAsync(locationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(locationResult);

            var locationDto = new LocationDto { Id = locationId, Title = "Test Location" };

            // Fix: Mock mapper to work with Location entity, not Result<Location>
            _mapperMock
                .Setup(m => m.Map<LocationDto>(location))
                .Returns(locationDto);

            var query = new GetLocationByIdQuery { Id = locationId };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.That(result.IsSuccess, Is.True, "Result should be successful");
            Assert.That(result.Data, Is.Not.Null, "Result data should not be null");
            Assert.That(result.Data.Id, Is.EqualTo(locationId), "Location ID should match");
            Assert.That(result.Data.Title, Is.EqualTo("Test Location"), "Location title should match");

            _locationRepositoryMock.Verify(x => x.GetByIdAsync(locationId, It.IsAny<CancellationToken>()), Times.Once);
            _mapperMock.Verify(m => m.Map<LocationDto>(location), Times.Once);
        }

        [Test]
        public async Task Handle_InvalidId_ReturnsFailureResult()
        {
            // Arrange
            var locationId = 999;

            _locationRepositoryMock
                .Setup(x => x.GetByIdAsync(locationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Location.Core.Domain.Entities.Location>.Failure("Location not found"));

            var query = new GetLocationByIdQuery { Id = locationId };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.That(result.IsSuccess, Is.False, "Result should be failure");
            Assert.That(result.ErrorMessage, Does.Contain("Location not found"), "Error message should contain 'Location not found'");
            Assert.That(result.Data, Is.Null, "Result data should be null for failure");

            _locationRepositoryMock.Verify(x => x.GetByIdAsync(locationId, It.IsAny<CancellationToken>()), Times.Once);
            _mapperMock.Verify(m => m.Map<LocationDto>(It.IsAny<Location.Core.Domain.Entities.Location>()), Times.Never);
        }

        [Test]
        public async Task Handle_RepositoryThrowsException_ReturnsFailureResult()
        {
            // Arrange
            var locationId = 1;
            var exception = new Exception("Database error");

            _locationRepositoryMock
                .Setup(x => x.GetByIdAsync(locationId, It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            var query = new GetLocationByIdQuery { Id = locationId };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.That(result.IsSuccess, Is.False, "Result should be failure");
            Assert.That(result.ErrorMessage, Does.Contain("Failed to retrieve location"), "Error message should indicate retrieval failure");
            Assert.That(result.ErrorMessage, Does.Contain("Database error"), "Error message should contain original exception message");
            Assert.That(result.Data, Is.Null, "Result data should be null for failure");

            _locationRepositoryMock.Verify(x => x.GetByIdAsync(locationId, It.IsAny<CancellationToken>()), Times.Once);
            _mapperMock.Verify(m => m.Map<LocationDto>(It.IsAny<Location.Core.Domain.Entities.Location>()), Times.Never);
        }

        [Test]
        public async Task Handle_RepositoryReturnsNullData_ReturnsFailureResult()
        {
            // Arrange
            var locationId = 1;

            // Repository returns success but with null data
            var nullResult = Result<Location.Core.Domain.Entities.Location>.Success(null);

            _locationRepositoryMock
                .Setup(x => x.GetByIdAsync(locationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(nullResult);

            var query = new GetLocationByIdQuery { Id = locationId };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.That(result.IsSuccess, Is.False, "Result should be failure when data is null");
            Assert.That(result.ErrorMessage, Does.Contain("Location not found"), "Error message should indicate location not found");
            Assert.That(result.Data, Is.Null, "Result data should be null");

            _locationRepositoryMock.Verify(x => x.GetByIdAsync(locationId, It.IsAny<CancellationToken>()), Times.Once);
            _mapperMock.Verify(m => m.Map<LocationDto>(It.IsAny<Location.Core.Domain.Entities.Location>()), Times.Never);
        }
    }
}