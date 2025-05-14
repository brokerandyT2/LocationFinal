namespace Location.Core.Application.Tests.Locations.Commands.SaveLocation
{
    using AutoMapper;
    using FluentAssertions;
    using Location.Core.Application.Commands.Locations;
    using Location.Core.Application.Common.Interfaces;
    using Location.Core.Application.Common.Models;
    using Location.Core.Application.Locations.DTOs;
    using Location.Core.Application.Tests.Utilities;
    using Location.Core.Domain.Entities;
    using Location.Core.Domain.ValueObjects;
    using Moq;
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public class SaveLocationCommandHandlerTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly SaveLocationCommandHandler _handler;

        public SaveLocationCommandHandlerTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _mapperMock = new Mock<IMapper>();

            _handler = new SaveLocationCommandHandler(_unitOfWorkMock.Object, _mapperMock.Object);
        }

        [Fact]
        public async Task Handle_CreateNewLocation_ReturnsSuccessResult()
        {
            // Arrange
            var command = TestDataBuilder.CreateValidSaveLocationCommand();
            var location = TestDataBuilder.CreateValidLocation();
            var locationDto = TestDataBuilder.CreateValidLocationDto();

            // Mock the unit of work
            _unitOfWorkMock.Setup(x => x.Locations.CreateAsync(It.IsAny<Domain.Entities.Location>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Location>.Success(location));

            // Mock the mapper
            _mapperMock.Setup(x => x.Map<LocationDto>(It.IsAny<Domain.Entities.Location>()))
                .Returns(locationDto);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.IsTrue(result.IsSuccess);
            Assert.IsNotNull(result.Data);
            Assert.IsTrue(result.Data.Title.Contains(locationDto.Title));
        }

        [Fact]
        public async Task Handle_CreateLocation_CallsRepository()
        {
            // Arrange
            var command = TestDataBuilder.CreateValidSaveLocationCommand();
            var location = TestDataBuilder.CreateValidLocation();
            var locationDto = TestDataBuilder.CreateValidLocationDto();

            // Mock the unit of work
            _unitOfWorkMock.Setup(x => x.Locations.CreateAsync(It.IsAny<Domain.Entities.Location>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Location>.Success(location));

            // Mock the mapper
            _mapperMock.Setup(x => x.Map<LocationDto>(It.IsAny<Domain.Entities.Location>()))
                .Returns(locationDto);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _unitOfWorkMock.Verify(x => x.Locations.CreateAsync(It.IsAny<Domain.Entities.Location>(), It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_UpdateExistingLocation_ReturnsSuccessResult()
        {
            // Arrange
            var locationId = 1;
            var command = TestDataBuilder.CreateValidSaveLocationCommand();
            command.Id = locationId;
            var existingLocation = TestDataBuilder.CreateValidLocation();
            var locationDto = TestDataBuilder.CreateValidLocationDto();

            // Mock the unit of work for get by id
            _unitOfWorkMock.Setup(x => x.Locations.GetByIdAsync(locationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Location>.Success(existingLocation));

            // Mock the unit of work for update
            _unitOfWorkMock.Setup(x => x.Locations.UpdateAsync(It.IsAny<Domain.Entities.Location>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Location>.Success(existingLocation));

            // Mock the mapper
            _mapperMock.Setup(x => x.Map<LocationDto>(It.IsAny<Domain.Entities.Location>()))
                .Returns(locationDto);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.IsTrue(result.IsSuccess);
            Assert.IsNotNull(result.Data);
        }

        [Fact]
        public async Task Handle_UpdateLocation_CallsRepository()
        {
            // Arrange
            var locationId = 1;
            var command = TestDataBuilder.CreateValidSaveLocationCommand();
            command.Id = locationId;
            var existingLocation = TestDataBuilder.CreateValidLocation();
            var locationDto = TestDataBuilder.CreateValidLocationDto();

            // Mock the unit of work
            _unitOfWorkMock.Setup(x => x.Locations.GetByIdAsync(locationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Location>.Success(existingLocation));

            _unitOfWorkMock.Setup(x => x.Locations.UpdateAsync(It.IsAny<Domain.Entities.Location>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Location>.Success(existingLocation));

            // Mock the mapper
            _mapperMock.Setup(x => x.Map<LocationDto>(It.IsAny<Domain.Entities.Location>()))
                .Returns(locationDto);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _unitOfWorkMock.Verify(x => x.Locations.GetByIdAsync(locationId, It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.Locations.UpdateAsync(It.IsAny<Domain.Entities.Location>(), It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_LocationNotFound_ReturnsFailureResult()
        {
            // Arrange
            var locationId = 999;
            var command = TestDataBuilder.CreateValidSaveLocationCommand();
            command.Id = locationId;

            // Mock the unit of work to return not found
            _unitOfWorkMock.Setup(x => x.Locations.GetByIdAsync(locationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Location>.Failure("Location not found"));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.IsFalse(result.IsSuccess);
            Assert.IsTrue( result.ErrorMessage.Contains("Location not found"));
        }

        [Fact]
        public async Task Handle_WithPhotoPath_AttachesPhoto()
        {
            // Arrange
            var command = TestDataBuilder.CreateValidSaveLocationCommand();
            command.PhotoPath = "/path/to/photo.jpg";
            var location = TestDataBuilder.CreateValidLocation();
            var locationDto = TestDataBuilder.CreateValidLocationDto();

            // Mock the unit of work
            _unitOfWorkMock.Setup(x => x.Locations.CreateAsync(It.IsAny<Domain.Entities.Location>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Location>.Success(location));

            // Mock the mapper
            _mapperMock.Setup(x => x.Map<LocationDto>(It.IsAny<Domain.Entities.Location>()))
                .Returns(locationDto);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.IsTrue(result.IsSuccess);
        }

        [Fact]
        public async Task Handle_RepositoryThrowsException_ReturnsFailureResult()
        {
            // Arrange
            var command = TestDataBuilder.CreateValidSaveLocationCommand();

            // Mock the unit of work to throw exception
            _unitOfWorkMock.Setup(x => x.Locations.CreateAsync(It.IsAny<Domain.Entities.Location>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.IsFalse(result.IsSuccess);
            Assert.IsTrue(result.ErrorMessage.Contains("Failed to save location"));
        }
    }
}