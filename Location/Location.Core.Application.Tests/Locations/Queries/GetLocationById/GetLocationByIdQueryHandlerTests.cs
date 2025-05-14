using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using AutoMapper;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Locations.DTOs;
using Location.Core.Application.Queries.Locations;
using Location.Core.Application.Tests.Helpers;
using FluentAssertions;
using Location.Core.Application.Locations.Queries.GetLocationById;

namespace Location.Core.Application.Tests.Locations.Queries.GetLocationById
{
    [TestFixture]
    public class GetLocationByIdQueryHandlerTests
    {
        private Mock<IUnitOfWork> _unitOfWorkMock;
        private Mock<IMapper> _mapperMock;
        private GetLocationByIdQueryHandler _handler;
        private TestDataBuilder _testDataBuilder;

        [SetUp]
        public void SetUp()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _mapperMock = new Mock<IMapper>();
            _handler = new GetLocationByIdQueryHandler(_unitOfWorkMock.Object, _mapperMock.Object);
            _testDataBuilder = new TestDataBuilder();
        }

        [Test]
        public async Task Handle_WithValidLocationId_ShouldReturnLocationDto()
        {
            // Arrange
            var location = _testDataBuilder.BuildLocation();
            var locationDto = new LocationDto { Id = location.Id, Title = location.Title };
            var query = new GetLocationByIdQuery { Id = location.Id };

            _unitOfWorkMock.Setup(x => x.Locations.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(location);

            _mapperMock.Setup(x => x.Map<LocationDto>(It.IsAny<Domain.Entities.Location>()))
                .Returns(locationDto);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeEquivalentTo(locationDto);
        }

        [Test]
        public async Task Handle_WithNonExistentLocationId_ShouldReturnFailure()
        {
            // Arrange
            var query = new GetLocationByIdQuery { Id = 999 };

            _unitOfWorkMock.Setup(x => x.Locations.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Domain.Entities.Location?)null);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Location not found");
            result.Data.Should().BeNull();
        }

        [Test]
        public async Task Handle_WhenExceptionThrown_ShouldReturnFailure()
        {
            // Arrange
            var query = new GetLocationByIdQuery { Id = 1 };

            _unitOfWorkMock.Setup(x => x.Locations.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Failed to retrieve location");
        }

        [Test]
        public async Task Handle_WithCancellationToken_ShouldPassTokenToRepository()
        {
            // Arrange
            var location = _testDataBuilder.BuildLocation();
            var query = new GetLocationByIdQuery { Id = location.Id };
            var cancellationToken = new CancellationToken();

            _unitOfWorkMock.Setup(x => x.Locations.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(location);

            _mapperMock.Setup(x => x.Map<LocationDto>(It.IsAny<Domain.Entities.Location>()))
                .Returns(new LocationDto());

            // Act
            await _handler.Handle(query, cancellationToken);

            // Assert
            _unitOfWorkMock.Verify(x => x.Locations.GetByIdAsync(query.Id, cancellationToken), Times.Once);
        }

        [Test]
        public async Task Handle_WithValidLocation_ShouldCallMapperWithCorrectLocation()
        {
            // Arrange
            var location = _testDataBuilder.BuildLocation();
            var query = new GetLocationByIdQuery { Id = location.Id };

            _unitOfWorkMock.Setup(x => x.Locations.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(location);

            _mapperMock.Setup(x => x.Map<LocationDto>(It.IsAny<Domain.Entities.Location>()))
                .Returns(new LocationDto());

            // Act
            await _handler.Handle(query, CancellationToken.None);

            // Assert
            _mapperMock.Verify(x => x.Map<LocationDto>(location), Times.Once);
        }

        [Test]
        public async Task Handle_WithNullLocationId_ShouldHandleGracefully()
        {
            // Arrange
            var query = new GetLocationByIdQuery { Id = 0 };

            _unitOfWorkMock.Setup(x => x.Locations.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Domain.Entities.Location?)null);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Location not found");
        }
    }
}