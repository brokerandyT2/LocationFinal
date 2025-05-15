using AutoMapper;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Locations.DTOs;
using Location.Core.Application.Locations.Queries.GetLocationById;
using Location.Core.Domain.ValueObjects;
using Moq;
using Xunit;

namespace Location.Core.Application.Tests.Locations.Queries.GetLocationById
{
    public class GetLocationByIdQueryHandlerTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<Location.Core.Application.Common.Interfaces.ILocationRepository> _locationRepositoryMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly GetLocationByIdQueryHandler _handler;

        public GetLocationByIdQueryHandlerTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _locationRepositoryMock = new Mock<Location.Core.Application.Common.Interfaces.ILocationRepository>();
            _mapperMock = new Mock<IMapper>();
            _unitOfWorkMock.Setup(u => u.Locations).Returns(_locationRepositoryMock.Object);
            _handler = new GetLocationByIdQueryHandler(_unitOfWorkMock.Object, _mapperMock.Object);
        }

        [Fact]
        public async Task Handle_ValidId_ReturnsSuccessResult()
        {
            // Arrange
            var locationId = 1;
            var location = new Location.Core.Domain.Entities.Location(
                "Test Location",
                "Test Description",
                new Coordinate(40.7128, -74.0060),
                new Address("New York", "NY"));

            _locationRepositoryMock
                .Setup(x => x.GetByIdAsync(locationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Location.Core.Domain.Entities.Location>.Success(location));

            var locationDto = new LocationDto { Id = locationId, Title = "Test Location" };
            _mapperMock
                .Setup(m => m.Map<LocationDto>(It.IsAny<Location.Core.Domain.Entities.Location>()))
                .Returns(locationDto);

            var query = new GetLocationByIdQuery { Id = locationId };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.IsTrue(result.IsSuccess);
            Assert.IsNotNull(result.Data);
            Assert.IsTrue(locationId == result.Data.Id);
        }

        [Fact]
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
            Assert.IsFalse(result.IsSuccess);
            Assert.IsTrue(result.ErrorMessage.Contains("Location not found"));
        }

        [Fact]
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
            Assert.IsFalse(result.IsSuccess);
            Assert.IsTrue(result.ErrorMessage.Contains("Failed to retrieve location"));
        }
    }
}