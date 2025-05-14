using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Location.Core.Application.Commands.Locations;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Tests.Helpers;
using FluentAssertions;

namespace Location.Core.Application.Tests.Locations.Commands.DeleteLocation
{
    [TestFixture]
    public class DeleteLocationCommandHandlerTests
    {
        private Mock<IUnitOfWork> _unitOfWorkMock;
        private DeleteLocationCommandHandler _handler;
        private TestDataBuilder _testDataBuilder;

        [SetUp]
        public void SetUp()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _handler = new DeleteLocationCommandHandler(_unitOfWorkMock.Object);
            _testDataBuilder = new TestDataBuilder();
        }

        [Test]
        public async Task Handle_WithValidLocationId_ShouldDeleteLocation()
        {
            // Arrange
            var location = _testDataBuilder.BuildLocation();
            var command = new DeleteLocationCommand { Id = location.Id };

            _unitOfWorkMock.Setup(x => x.Locations.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(location);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            _unitOfWorkMock.Verify(x => x.Locations.Update(It.IsAny<Domain.Entities.Location>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_WithNonExistentLocationId_ShouldReturnFailure()
        {
            // Arrange
            var command = new DeleteLocationCommand { Id = 999 };

            _unitOfWorkMock.Setup(x => x.Locations.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Domain.Entities.Location?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Location not found");
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task Handle_WhenExceptionThrown_ShouldReturnFailure()
        {
            // Arrange
            var command = new DeleteLocationCommand { Id = 1 };

            _unitOfWorkMock.Setup(x => x.Locations.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Failed to delete location");
        }

        [Test]
        public async Task Handle_WithValidLocation_ShouldCallDeleteMethod()
        {
            // Arrange
            var location = _testDataBuilder.BuildLocation();
            var command = new DeleteLocationCommand { Id = location.Id };

            _unitOfWorkMock.Setup(x => x.Locations.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(location);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            location.IsDeleted.Should().BeTrue();
            _unitOfWorkMock.Verify(x => x.Locations.Update(location), Times.Once);
        }

        [Test]
        public async Task Handle_WhenSaveChangesFails_ShouldReturnFailure()
        {
            // Arrange
            var location = _testDataBuilder.BuildLocation();
            var command = new DeleteLocationCommand { Id = location.Id };

            _unitOfWorkMock.Setup(x => x.Locations.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(location);

            _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Save failed"));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Failed to delete location");
        }

        [Test]
        public async Task Handle_WithCancellationToken_ShouldPassTokenToRepository()
        {
            // Arrange
            var location = _testDataBuilder.BuildLocation();
            var command = new DeleteLocationCommand { Id = location.Id };
            var cancellationToken = new CancellationToken();

            _unitOfWorkMock.Setup(x => x.Locations.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(location);

            // Act
            var result = await _handler.Handle(command, cancellationToken);

            // Assert
            _unitOfWorkMock.Verify(x => x.Locations.GetByIdAsync(command.Id, cancellationToken), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(cancellationToken), Times.Once);
        }

        [Test]
        public async Task Handle_WithDeletedLocation_ShouldGenerateDomainEvent()
        {
            // Arrange
            var location = _testDataBuilder.BuildLocation();
            var command = new DeleteLocationCommand { Id = location.Id };

            _unitOfWorkMock.Setup(x => x.Locations.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(location);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            location.DomainEvents.Should().HaveCount(1);
            location.DomainEvents.Should().Contain(e => e.GetType().Name == "LocationDeletedEvent");
        }
    }
}