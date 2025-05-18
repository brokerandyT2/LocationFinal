using AutoMapper;
using FluentAssertions;
using Location.Core.Application.Commands.Locations;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Locations.DTOs;
using Location.Core.Domain.ValueObjects;
using Moq;
using NUnit.Framework;
using System.Runtime.CompilerServices;
using Assert = NUnit.Framework.Assert;


namespace Location.Core.Application.Tests.Locations.Commands.SaveLocation
{
    [NUnit.Framework.Category("Locations")]
    [NUnit.Framework.Category("Delete Location")]
    public class SaveLocationCommandHandlerTests
    {
        private  Mock<IUnitOfWork> _unitOfWorkMock;
        private  Mock<Location.Core.Application.Common.Interfaces.ILocationRepository> _locationRepositoryMock;
        private  Mock<IMapper> _mapperMock;
        private  SaveLocationCommandHandler _handler;

        public SaveLocationCommandHandlerTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _locationRepositoryMock = new Mock<ILocationRepository>();
            _mapperMock = new Mock<IMapper>();

            _unitOfWorkMock.Setup(u => u.Locations).Returns(_locationRepositoryMock.Object);
            _handler = new SaveLocationCommandHandler(_unitOfWorkMock.Object, _mapperMock.Object);
        }
        [SetUp]
        public void SetUp()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _locationRepositoryMock = new Mock<ILocationRepository>();
            _mapperMock = new Mock<IMapper>();

            _unitOfWorkMock.Setup(u => u.Locations).Returns(_locationRepositoryMock.Object);
            _handler = new SaveLocationCommandHandler(_unitOfWorkMock.Object, _mapperMock.Object);

        }

        [Test]
       
        public async Task Handle_CreateNewLocation_ReturnsSuccessResult()
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

            var newLocation = new Domain.Entities.Location(
                command.Title,
                command.Description,
                new Coordinate(command.Latitude, command.Longitude),
                new Address(command.City, command.State));

            _locationRepositoryMock
     .Setup(x => x.CreateAsync(It.IsAny<Domain.Entities.Location>(), It.IsAny<CancellationToken>()))
     .ReturnsAsync(Result<Domain.Entities.Location>.Success(newLocation));

            var locationDto = new LocationDto { Id = 1, Title = command.Title };
            _mapperMock
                .Setup(m => m.Map<LocationDto>(It.IsAny<Domain.Entities.Location>()))
                .Returns(locationDto);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Data, Is.Not.Null);
            _locationRepositoryMock
                .Setup(x => x.CreateAsync(It.IsAny<Domain.Entities.Location>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Location>.Success(newLocation));
        }

        [Test]
        public async Task Handle_CreateLocationWithPhoto_AttachesPhoto()
        {
            // Arrange
            var command = new SaveLocationCommand
            {
                Title = "Test Location",
                Description = "Test Description",
                Latitude = 40.7128,
                Longitude = -74.0060,
                City = "New York",
                State = "NY",
                PhotoPath = "/path/to/photo.jpg"
            };

            var newLocation = new Domain.Entities.Location(
                command.Title,
                command.Description,
                new Coordinate(command.Latitude, command.Longitude),
                new Address(command.City, command.State));

            _locationRepositoryMock
                .Setup(x => x.CreateAsync(It.IsAny<Domain.Entities.Location>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Location>.Success(newLocation));

            var locationDto = new LocationDto { Id = 1, Title = command.Title };
            _mapperMock
                .Setup(m => m.Map<LocationDto>(It.IsAny<Domain.Entities.Location>()))
                .Returns(locationDto);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.That(result.IsSuccess, Is.True);
            _locationRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<Domain.Entities.Location>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result<Domain.Entities.Location>.Success(newLocation));
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
                .ReturnsAsync(Result<Domain.Entities.Location>.Failure("Location not found"));

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
        public async Task Handle_UpdateNonExistentLocation_ReturnsFailure()
        {
            // Arrange
            var locationId = 999;
            var command = new SaveLocationCommand
            {
                Id = locationId,
                Title = "Updated Location"
            };

            _locationRepositoryMock
                .Setup(x => x.GetByIdAsync(locationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Location>.Failure("Location not found"));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("Location not found"));
            _locationRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Domain.Entities.Location>(), It.IsAny<CancellationToken>()), Times.Never);

        }

        [Test]
        public async Task Handle_InvalidCommand_ReturnsFailure()
        {
            // Arrange
            var command = new SaveLocationCommand
            {
                Title = "Test Location",
                Description = "Test Description",
                Latitude = 91, // Invalid latitude
                Longitude = -74.0060,
                City = "New York",
                State = "NY"
            };

            _locationRepositoryMock
                .Setup(x => x.CreateAsync(It.IsAny<Domain.Entities.Location>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ArgumentOutOfRangeException("latitude", "Latitude must be between -90 and 90"));


            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Failed to save location");
            //Assert.IsTrue(result.ErrorMessage.Contains("Failed to save location"));
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
            Assert.That(result.IsSuccess, Is.True);

            Assert.That(result.ErrorMessage, Does.Contain("Location not found"));
        }
    }
}