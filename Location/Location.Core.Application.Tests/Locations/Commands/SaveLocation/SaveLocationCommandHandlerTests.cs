using NUnit.Framework;
using FluentAssertions;
using Moq;
using System.Threading;
using System.Threading.Tasks;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Locations.Commands.SaveLocation;
using Location.Core.Application.Tests.Helpers;
using AutoMapper;
using Location.Core.Application.Locations.DTOs;
using Location.Core.Domain.ValueObjects;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Common.Interfaces.Persistence;

namespace Location.Core.Application.Tests.Locations.Commands.SaveLocation
{
    [TestFixture]
    public class SaveLocationCommandHandlerTests
    {
        private Mock<IUnitOfWork> _unitOfWorkMock;
        private Mock<IMapper> _mapperMock;
        private Mock<IEventBus> _eventBusMock;
        private SaveLocationCommandHandler _handler;

        [SetUp]
        public void Setup()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _mapperMock = new Mock<IMapper>();
            _eventBusMock = new Mock<IEventBus>();

            // Setup mock repository
            var locationRepoMock = new Mock<ILocationRepository>();
            _unitOfWorkMock.Setup(x => x.Locations).Returns(locationRepoMock.Object);

            _handler = new SaveLocationCommandHandler(
                _unitOfWorkMock.Object,
                _mapperMock.Object,
                _eventBusMock.Object
            );
        }

        [Test]
        public async Task Handle_WithNewLocation_ShouldCreateLocation()
        {
            // Arrange
            var command = TestDataBuilder.CreateValidSaveLocationCommand();
            var createdLocation = TestDataBuilder.CreateValidLocation();
            var locationDto = TestDataBuilder.CreateValidLocationDto();

            _unitOfWorkMock.Setup(x => x.Locations.AddAsync(
                It.IsAny<Domain.Entities.Location>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(createdLocation);

            _mapperMock.Setup(x => x.Map<LocationDto>(It.IsAny<Domain.Entities.Location>()))
                .Returns(locationDto);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().BeEquivalentTo(locationDto);

            _unitOfWorkMock.Verify(x => x.Locations.AddAsync(
                It.IsAny<Domain.Entities.Location>(),
                It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_WithExistingLocation_ShouldUpdateLocation()
        {
            // Arrange
            var command = TestDataBuilder.CreateValidSaveLocationCommand(id: 1);
            var existingLocation = TestDataBuilder.CreateValidLocation();
            var locationDto = TestDataBuilder.CreateValidLocationDto(id: 1);

            _unitOfWorkMock.Setup(x => x.Locations.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingLocation);

            _mapperMock.Setup(x => x.Map<LocationDto>(It.IsAny<Domain.Entities.Location>()))
                .Returns(locationDto);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Id.Should().Be(1);

            _unitOfWorkMock.Verify(x => x.Locations.Update(It.IsAny<Domain.Entities.Location>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_WithNonExistentLocationId_ShouldReturnNotFoundError()
        {
            // Arrange
            var command = TestDataBuilder.CreateValidSaveLocationCommand(id: 999);

            _unitOfWorkMock.Setup(x => x.Locations.GetByIdAsync(999, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Domain.Entities.Location?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().ContainSingle();
            result.Errors.Should().Contain(x => x.Code == "NOT_FOUND");
        }

        [Test]
        public async Task Handle_WithPhotoPath_ShouldAttachPhoto()
        {
            // Arrange
            var command = TestDataBuilder.CreateValidSaveLocationCommand(photoPath: "/photos/test.jpg");
            var createdLocation = TestDataBuilder.CreateValidLocation();
            var locationDto = TestDataBuilder.CreateValidLocationDto(photoPath: "/photos/test.jpg");

            _unitOfWorkMock.Setup(x => x.Locations.AddAsync(
                It.IsAny<Domain.Entities.Location>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(createdLocation);

            _mapperMock.Setup(x => x.Map<LocationDto>(It.IsAny<Domain.Entities.Location>()))
                .Returns(locationDto);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.PhotoPath.Should().Be("/photos/test.jpg");
        }

        [Test]
        public async Task Handle_WithInvalidCoordinates_ShouldReturnFailure()
        {
            // Arrange
            var command = TestDataBuilder.CreateValidSaveLocationCommand(latitude: 91, longitude: -181);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().NotBeEmpty();
        }

        [Test]
        public async Task Handle_WithDomainEvents_ShouldPublishEvents()
        {
            // Arrange
            var command = TestDataBuilder.CreateValidSaveLocationCommand();
            var createdLocation = TestDataBuilder.CreateValidLocation();
            createdLocation.AddDomainEvent(new Domain.Events.LocationSavedEvent(createdLocation));

            _unitOfWorkMock.Setup(x => x.Locations.AddAsync(
                It.IsAny<Domain.Entities.Location>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(createdLocation);

            _mapperMock.Setup(x => x.Map<LocationDto>(It.IsAny<Domain.Entities.Location>()))
                .Returns(TestDataBuilder.CreateValidLocationDto());

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _eventBusMock.Verify(x => x.PublishAllAsync(
                It.IsAny<Domain.Interfaces.IDomainEvent[]>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_WithDatabaseException_ShouldReturnDatabaseError()
        {
            // Arrange
            var command = TestDataBuilder.CreateValidSaveLocationCommand();

            _unitOfWorkMock.Setup(x => x.Locations.AddAsync(
                It.IsAny<Domain.Entities.Location>(),
                It.IsAny<CancellationToken>()))
                .ThrowsAsync(new System.Exception("Database connection failed"));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().ContainSingle();
            result.Errors.Should().Contain(x => x.Code == "DATABASE_ERROR");
            result.Errors.Should().Contain(x => x.Message.Contains("Database connection failed"));
        }
    }
}