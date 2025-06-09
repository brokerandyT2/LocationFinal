using AutoMapper;
using Location.Core.Application.Commands.Locations;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Locations.DTOs;
using Location.Core.Domain.ValueObjects;
using MediatR;
using Moq;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Location.Core.Application.Tests.Locations.Commands.SaveLocation
{
    [Category("Locations")]
    [Category("Save")]
    public class SaveLocationCommandHandlerTests
    {
        private Mock<IUnitOfWork> _unitOfWorkMock;
        private Mock<Location.Core.Application.Common.Interfaces.ILocationRepository> _locationRepositoryMock;
        private Mock<IMapper> _mapperMock;
        private Mock<IMediator> _mediatorMock;
        private SaveLocationCommandHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _locationRepositoryMock = new Mock<ILocationRepository>();
            _mapperMock = new Mock<IMapper>();
            _mediatorMock = new Mock<IMediator>();

            _unitOfWorkMock.Setup(u => u.Locations).Returns(_locationRepositoryMock.Object);
            _handler = new SaveLocationCommandHandler(_unitOfWorkMock.Object, _mapperMock.Object, _mediatorMock.Object);
        }

        [Test]
        public async Task Handle_UpdateExistingLocation_ReturnsSuccessResult()
        {
            // Arrange
            var locationId = 1;
            var command = new SaveLocationCommand
            {
                Id = locationId,
                Title = "Updated Location",
                Description = "Updated Description",
                Latitude = 40.7128,
                Longitude = -74.0060,
                City = "New York",
                State = "NY"
            };

            var existingLocation = new Domain.Entities.Location(
                "Old Title",
                "Old Description",
                new Coordinate(40.0, -73.0),
                new Address("Old City", "Old State"));

            _locationRepositoryMock
                .Setup(x => x.GetByIdAsync(locationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Location>.Success(existingLocation));

            _locationRepositoryMock
                .Setup(x => x.UpdateAsync(It.IsAny<Domain.Entities.Location>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Location>.Success(existingLocation));

            var locationDto = new LocationDto { Id = locationId, Title = command.Title };
            _mapperMock
                .Setup(m => m.Map<LocationDto>(It.IsAny<Domain.Entities.Location>()))
                .Returns(locationDto);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Data, Is.Not.Null);
            _locationRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Domain.Entities.Location>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_RepositoryThrowsException_ReturnsFailure()
        {
            // Arrange
            var command = new SaveLocationCommand
            {
                Title = "Test Location",
                Description = "Test Description",
                Latitude = 40.7128,
                Longitude = -74.0060,
                City = "New York",
                State = "NY"
            };

            _locationRepositoryMock
                .Setup(x => x.CreateAsync(It.IsAny<Domain.Entities.Location>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("Failed to save location"));
        }
    }
}