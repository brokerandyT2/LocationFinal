using FluentAssertions;
using Location.Core.Application.Commands.Locations;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Tests.Utilities;
using MediatR;
using Moq;
using NUnit.Framework;

namespace Location.Core.Application.Tests.Locations.Commands.DeleteLocation
{
    [Category("Locations")]
    [Category("Delete Location")]
    [TestFixture]
    public class DeleteLocationCommandHandlerTests
    {
        private Mock<IUnitOfWork> _unitOfWorkMock;
        private Mock<ILocationRepository> _locationRepositoryMock;
        private Mock<IMediator> _mediatorMock;
        private DeleteLocationCommandHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _locationRepositoryMock = new Mock<ILocationRepository>();
            _mediatorMock = new Mock<IMediator>();
            _unitOfWorkMock.Setup(u => u.Locations).Returns(_locationRepositoryMock.Object);
            _handler = new DeleteLocationCommandHandler(_unitOfWorkMock.Object, _mediatorMock.Object);
        }

        [Test]
        public async Task Handle_WithValidLocationId_ShouldMarkLocationAsDeleted()
        {
            // Arrange
            var command = new DeleteLocationCommand { Id = 1 };
            var location = TestDataBuilder.CreateValidLocation(1);

            _locationRepositoryMock
                .Setup(x => x.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Location>.Success(location));

            _locationRepositoryMock
                .Setup(x => x.UpdateAsync(It.IsAny<Domain.Entities.Location>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Location>.Success(location));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();

            _locationRepositoryMock.Verify(x => x.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()), Times.Once);
            _locationRepositoryMock.Verify(x => x.UpdateAsync(It.Is<Domain.Entities.Location>(l => l.IsDeleted == true), It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_WithNonExistentLocationId_ShouldReturnFailure()
        {
            // Arrange
            var command = new DeleteLocationCommand { Id = 999 };

            _locationRepositoryMock
                .Setup(x => x.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Location>.Failure("Location not found"));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Location not found");

            _locationRepositoryMock.Verify(x => x.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()), Times.Once);
            _locationRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Domain.Entities.Location>(), It.IsAny<CancellationToken>()), Times.Never);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task Handle_WithAlreadyDeletedLocation_ShouldStillReturnSuccess()
        {
            // Arrange
            var command = new DeleteLocationCommand { Id = 1 };
            var location = TestDataBuilder.CreateValidLocation(1);
            location.Delete(); // Location is already deleted

            _locationRepositoryMock
                .Setup(x => x.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Location>.Success(location));

            _locationRepositoryMock
                .Setup(x => x.UpdateAsync(It.IsAny<Domain.Entities.Location>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Location>.Success(location));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();

            _locationRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Domain.Entities.Location>(), It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_WhenRepositoryThrowsException_ShouldReturnFailure()
        {
            // Arrange
            var command = new DeleteLocationCommand { Id = 1 };
            var exception = new Exception("Database error");

            _locationRepositoryMock
                .Setup(x => x.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Failed to delete location");
            result.ErrorMessage.Should().Contain("Database error");

            _locationRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Domain.Entities.Location>(), It.IsAny<CancellationToken>()), Times.Never);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task Handle_WhenSaveChangesThrowsException_ShouldReturnFailure()
        {
            // Arrange
            var command = new DeleteLocationCommand { Id = 1 };
            var location = TestDataBuilder.CreateValidLocation(1);
            var exception = new Exception("Save failed");

            _locationRepositoryMock
                .Setup(x => x.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Location>.Success(location));

            _locationRepositoryMock
                .Setup(x => x.UpdateAsync(It.IsAny<Domain.Entities.Location>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Location>.Success(location));

            _unitOfWorkMock
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Failed to delete location");
            result.ErrorMessage.Should().Contain("Save failed");

            _locationRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Domain.Entities.Location>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_ShouldCallDeleteMethodOnLocation()
        {
            // Arrange
            var command = new DeleteLocationCommand { Id = 1 };
            var location = TestDataBuilder.CreateValidLocation(1);
            Domain.Entities.Location capturedLocation = null;

            _locationRepositoryMock
                .Setup(x => x.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Location>.Success(location));

            _locationRepositoryMock
                .Setup(x => x.UpdateAsync(It.IsAny<Domain.Entities.Location>(), It.IsAny<CancellationToken>()))
                .Callback<Domain.Entities.Location, CancellationToken>((l, ct) => capturedLocation = l)
                .ReturnsAsync(Result<Domain.Entities.Location>.Success(location));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            capturedLocation.Should().NotBeNull();
            capturedLocation.IsDeleted.Should().BeTrue();
        }

        [Test]
        public async Task Handle_WithCancellationToken_ShouldPassItThrough()
        {
            // Arrange
            var command = new DeleteLocationCommand { Id = 1 };
            var location = TestDataBuilder.CreateValidLocation(1);
            var cancellationToken = new CancellationToken();

            _locationRepositoryMock
                .Setup(x => x.GetByIdAsync(command.Id, cancellationToken))
                .ReturnsAsync(Result<Domain.Entities.Location>.Success(location));

            _locationRepositoryMock
                .Setup(x => x.UpdateAsync(It.IsAny<Domain.Entities.Location>(), cancellationToken))
                .ReturnsAsync(Result<Domain.Entities.Location>.Success(location));

            // Act
            await _handler.Handle(command, cancellationToken);

            // Assert
            _locationRepositoryMock.Verify(x => x.GetByIdAsync(command.Id, cancellationToken), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(cancellationToken), Times.Once);
        }
    }
}