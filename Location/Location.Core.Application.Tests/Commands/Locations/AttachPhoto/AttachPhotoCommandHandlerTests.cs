using AutoMapper;
using FluentAssertions;
using Location.Core.Application.Commands.Locations;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Locations.DTOs;
using Location.Core.Application.Tests.Utilities;
using MediatR;
using Moq;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Core.Application.Tests.Commands.Locations.AttachPhoto
{
    [Category("Locations")]
    [Category("PHOTO Management")]
    [TestFixture]
    public class AttachPhotoCommandHandlerTests
    {
        private Mock<IUnitOfWork> _unitOfWorkMock;
        private Mock<ILocationRepository> _locationRepositoryMock;
        private Mock<IMapper> _mapperMock;
        private AttachPhotoCommandHandler _handler;
        private Mock<IMediator> _mediatorMock;
        [SetUp]
        public void SetUp()
        {
            _mediatorMock = new Mock<IMediator>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _locationRepositoryMock = new Mock<ILocationRepository>();
            _mapperMock = new Mock<IMapper>();

            _unitOfWorkMock.Setup(u => u.Locations).Returns(_locationRepositoryMock.Object);

            _handler = new AttachPhotoCommandHandler(
                _unitOfWorkMock.Object,
                _mapperMock.Object, _mediatorMock.Object);
        }

        [Test]
        public async Task Handle_WithValidLocationAndPath_ShouldAttachPhoto()
        {
            // Arrange
            var command = new AttachPhotoCommand
            {
                LocationId = 1,
                PhotoPath = "/photos/test.jpg"
            };

            var location = TestDataBuilder.CreateValidLocation(1);
            location.RemovePhoto(); // Ensure no existing photo

            _locationRepositoryMock
                .Setup(x => x.GetByIdAsync(command.LocationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Location>.Success(location));

            _locationRepositoryMock
                .Setup(x => x.UpdateAsync(It.IsAny<Domain.Entities.Location>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Location>.Success(location));

            var locationDto = new LocationDto
            {
                Id = 1,
                Title = location.Title,
                PhotoPath = command.PhotoPath
            };

            _mapperMock
                .Setup(x => x.Map<LocationDto>(It.IsAny<Domain.Entities.Location>()))
                .Returns(locationDto);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.PhotoPath.Should().Be(command.PhotoPath);

            _locationRepositoryMock.Verify(x => x.GetByIdAsync(command.LocationId, It.IsAny<CancellationToken>()), Times.Once);
            _locationRepositoryMock.Verify(x => x.UpdateAsync(It.Is<Domain.Entities.Location>(l => l.PhotoPath == command.PhotoPath), It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_WithNonExistentLocation_ShouldReturnFailure()
        {
            // Arrange
            var command = new AttachPhotoCommand
            {
                LocationId = 999,
                PhotoPath = "/photos/test.jpg"
            };

            _locationRepositoryMock
                .Setup(x => x.GetByIdAsync(command.LocationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Location>.Failure("Location not found"));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Location not found");

            _locationRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Domain.Entities.Location>(), It.IsAny<CancellationToken>()), Times.Never);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task Handle_WithExistingPhoto_ShouldReplacePhoto()
        {
            // Arrange
            var command = new AttachPhotoCommand
            {
                LocationId = 1,
                PhotoPath = "/photos/new.jpg"
            };

            var location = TestDataBuilder.CreateValidLocation(1);
            location.AttachPhoto("/photos/old.jpg"); // Add existing photo

            _locationRepositoryMock
                .Setup(x => x.GetByIdAsync(command.LocationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Location>.Success(location));

            _locationRepositoryMock
                .Setup(x => x.UpdateAsync(It.IsAny<Domain.Entities.Location>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Location>.Success(location));

            var locationDto = new LocationDto
            {
                Id = 1,
                PhotoPath = command.PhotoPath
            };

            _mapperMock
                .Setup(x => x.Map<LocationDto>(It.IsAny<Domain.Entities.Location>()))
                .Returns(locationDto);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.PhotoPath.Should().Be(command.PhotoPath);

            _locationRepositoryMock.Verify(x => x.UpdateAsync(It.Is<Domain.Entities.Location>(l => l.PhotoPath == command.PhotoPath), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_WhenUpdateFails_ShouldReturnFailure()
        {
            // Arrange
            var command = new AttachPhotoCommand
            {
                LocationId = 1,
                PhotoPath = "/photos/test.jpg"
            };

            var location = TestDataBuilder.CreateValidLocation(1);

            _locationRepositoryMock
                .Setup(x => x.GetByIdAsync(command.LocationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Location>.Success(location));

            _locationRepositoryMock
                .Setup(x => x.UpdateAsync(It.IsAny<Domain.Entities.Location>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Location>.Failure("Failed to update location"));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Failed to update location");

            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task Handle_WhenSaveChangesFails_ShouldReturnFailure()
        {
            // Arrange
            var command = new AttachPhotoCommand
            {
                LocationId = 1,
                PhotoPath = "/photos/test.jpg"
            };

            var location = TestDataBuilder.CreateValidLocation(1);

            _locationRepositoryMock
                .Setup(x => x.GetByIdAsync(command.LocationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Location>.Success(location));

            _locationRepositoryMock
                .Setup(x => x.UpdateAsync(It.IsAny<Domain.Entities.Location>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Location>.Success(location));

            _unitOfWorkMock
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Failed to attach photo");
            result.ErrorMessage.Should().Contain("Database error");
        }

        [Test]
        public async Task Handle_WithInvalidPhotoPath_ShouldFailDuringDomainValidation()
        {
            // Arrange
            var command = new AttachPhotoCommand
            {
                LocationId = 1,
                PhotoPath = string.Empty // Invalid path
            };

            var location = TestDataBuilder.CreateValidLocation(1);

            _locationRepositoryMock
                .Setup(x => x.GetByIdAsync(command.LocationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Location>.Success(location));

            // Act & Assert
            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Failed to attach photo");
        }

        [Test]
        public async Task Handle_WithCancellationToken_ShouldPassItThrough()
        {
            // Arrange
            var command = new AttachPhotoCommand
            {
                LocationId = 1,
                PhotoPath = "/photos/test.jpg"
            };

            var location = TestDataBuilder.CreateValidLocation(1);
            var cancellationToken = new CancellationToken();

            _locationRepositoryMock
                .Setup(x => x.GetByIdAsync(command.LocationId, cancellationToken))
                .ReturnsAsync(Result<Domain.Entities.Location>.Success(location));

            _locationRepositoryMock
                .Setup(x => x.UpdateAsync(It.IsAny<Domain.Entities.Location>(), cancellationToken))
                .ReturnsAsync(Result<Domain.Entities.Location>.Success(location));

            _mapperMock
                .Setup(x => x.Map<LocationDto>(It.IsAny<Domain.Entities.Location>()))
                .Returns(new LocationDto());

            // Act
            await _handler.Handle(command, cancellationToken);

            // Assert
            _locationRepositoryMock.Verify(x => x.GetByIdAsync(command.LocationId, cancellationToken), Times.Once);
            _locationRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Domain.Entities.Location>(), cancellationToken), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(cancellationToken), Times.Once);
        }
    }
}